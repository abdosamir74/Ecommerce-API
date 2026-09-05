using Application.Common.Interfaces;
using Application.DTOs.Orders;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.OrderAggregate;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Order = Domain.Entities.OrderAggregate.Order;

namespace Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBasketRepository _basketRepository;
        private readonly IMapper _mapper;
        private readonly IConnectionMultiplexer _redis;

        public OrderService(
            ApplicationDbContext context,
            IBasketRepository basketRepository,
            IMapper mapper,
            IConnectionMultiplexer redis)
        {
            _context = context;
            _basketRepository = basketRepository;
            _mapper = mapper;
            _redis = redis;
        }

        public async Task<OrderToReturnDto?> CreateOrderAsync(string buyerEmail, string basketId, AddressDto shippingAddress)
        {
            var db = _redis.GetDatabase();
            var lockKey = $"lock:checkout:{basketId}";
            var lockValue = Guid.NewGuid().ToString();

            // 1. قفل عملية الإنشاء للسلة المحددة لمنع Duplicate Orders أو Race Condition
            bool isLocked = await db.LockTakeAsync(lockKey, lockValue, TimeSpan.FromSeconds(10));

            if (!isLocked)
            {
                throw new InvalidOperationException("طلب الشراء قيد المعالجة حالياً. يرجى الانتظار.");
            }

            try
            {
                // 2. جلب السلة من Redis
                var basket = await _basketRepository.GetBasketAsync(basketId);
                if (basket == null) return null;

                if (string.IsNullOrWhiteSpace(basket.PaymentIntentId))
                    throw new InvalidOperationException("لا يمكن إنشاء الطلب قبل إنشاء PaymentIntent.");

                var existingOrder = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.PaymentIntentId == basket.PaymentIntentId && o.BuyerEmail == buyerEmail);

                if (existingOrder != null)
                {
                    await _basketRepository.DeleteBasketAsync(basketId);
                    return _mapper.Map<Order, OrderToReturnDto>(existingOrder);
                }

                // 3. بدء Transaction لضمان ذَرّية العملية (Atomic Operations)
                using var transaction = await _context.Database.BeginTransactionAsync();

                // 4. تجميع المنتجات والتحقق من الـ Stock وتخصيصه
                var items = new List<OrderItem>();
                foreach (var item in basket.Items)
                {
                    var productItem = await _context.Products.FindAsync(item.Id);
                    if (productItem != null)
                    {
                        // فحص الـ Stock لمنع Over-Selling
                        if (productItem.Stock < item.Quantity)
                        {
                            throw new InvalidOperationException($"المنتج '{productItem.Name}' لا يملك كمية كافية في المخزون.");
                        }

                        // خصم الكمية من المخزون
                        productItem.Stock -= item.Quantity;

                        var itemOrdered = new ProductItemOrdered(productItem.Id, productItem.Name, productItem.PictureUrl);
                        var orderItem = new OrderItem(itemOrdered, productItem.Price, item.Quantity);
                        items.Add(orderItem);
                    }
                }

                // 5. حساب الـ Subtotal والخصم الخاص بالكوبون
                var subtotal = items.Sum(item => item.Price * item.Quantity);
                decimal discount = basket.Discount;

                // 6. التحقق من الكوبون وزيادة مرات الاستخدام إذا كان مستخدماً
                if (!string.IsNullOrEmpty(basket.CouponCode))
                {
                    var coupon = await _context.Set<Coupon>()
                        .FirstOrDefaultAsync(c => c.Code == basket.CouponCode.ToUpper());

                    if (coupon != null && coupon.IsValid())
                    {
                        coupon.IncrementUsage();
                    }
                    else
                    {
                        // إلغاء الخصم إذا أصبح الكوبون غير صالح أثناء عملية الـ Checkout
                        discount = 0;
                    }
                }

                // 7. تحويل العنوان إلى Address Entity
                var address = _mapper.Map<AddressDto, Address>(shippingAddress);

                // 8. إنشاء الـ Order بالقيمة الصافية بعد الخصم
                var order = new Order(buyerEmail, address, items, subtotal, discount, basket.PaymentIntentId);

                _context.Orders.Add(order);

                // 9. حفظ الطلب وتحديث المخزون والكوبون مع التعامل مع Concurrency
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException("حدث تعديل متزامن على مخزون المنتجات، يرجى إعادة المحاولة.");
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                // 10. مسح السلة من Redis
                await _basketRepository.DeleteBasketAsync(basketId);

                // 11. تحويل إلى DTO للـ Response
                return _mapper.Map<Order, OrderToReturnDto>(order);
            }
            finally
            {
                // تحرير القفل فور الانتهاء من التنفيذ
                await db.LockReleaseAsync(lockKey, lockValue);
            }
        }

        public async Task<IReadOnlyList<OrderToReturnDto>> GetOrdersForUserAsync(string buyerEmail)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.BuyerEmail == buyerEmail)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return _mapper.Map<IReadOnlyList<Order>, IReadOnlyList<OrderToReturnDto>>(orders);
        }

        public async Task<Order?> GetOrderByIdAsync(int id, string buyerEmail)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id && o.BuyerEmail == buyerEmail);
        }

        public async Task<OrderToReturnDto?> GetOrderByIdForUserAsync(int id, string buyerEmail)
        {
            var order = await GetOrderByIdAsync(id, buyerEmail);

            if (order == null) return null;

            return _mapper.Map<Order, OrderToReturnDto>(order);
        }

        public async Task<OrderToReturnDto?> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return null;

            // استخدام Domain Method للتحقق من الـ State Machine
            order.UpdateStatus(newStatus);

            await _context.SaveChangesAsync();

            return _mapper.Map<Order, OrderToReturnDto>(order);
        }

        public async Task<Order?> UpdateOrderPaymentStatusAsync(string paymentIntentId, OrderStatus status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId);

            if (order == null) return null;

            if (order.Status == status) return order;

            // عند فشل الدفع نعيد الكمية المحجوزة للمخزون مرة واحدة فقط
            if (status == OrderStatus.PaymentFailed)
            {
                foreach (var orderItem in order.OrderItems)
                {
                    var product = await _context.Products
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.Id == orderItem.ItemOrdered.ProductItemId);

                    if (product != null && !product.IsDeleted)
                        product.Stock += orderItem.Quantity;
                }
            }

            order.UpdateStatus(status);
            await _context.SaveChangesAsync();
            return order;
        }
    }
}