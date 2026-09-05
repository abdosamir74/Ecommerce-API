using Domain.Entities.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // تحويل الـ Address إلى Owned Entity داخل نفس جدول الـ Order
            builder.OwnsOne(o => o.ShipToAddress, a =>
            {
                a.WithOwner();
            });

            // تحويل الـ OrderStatus لـ Enum نصي بدلاً من الأرقام
            builder.Property(s => s.Status)
                .HasConversion(
                    o => o.ToString(),
                    o => (OrderStatus)Enum.Parse(typeof(OrderStatus), o)
                );

            // ضبط العلاقة (Delete Cascade للـ OrderItems)
            builder.HasMany(o => o.OrderItems)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(o => o.Subtotal)
                .HasColumnType("decimal(18,2)");

            // إعداد خاصية الخصم للحفاظ على الدقة العشرية
            builder.Property(o => o.Discount)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0);
        }
    }
}