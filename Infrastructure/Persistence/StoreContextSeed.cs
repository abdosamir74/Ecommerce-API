using Domain.Entities;
using Domain.Entities.Identity;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public static class StoreContextSeed
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            if (!await context.Brands.AnyAsync())
            {
                var brands = new List<Brand>
                {
                    new Brand { Name = "Nike", Description = "Sportswear brand" },
                    new Brand { Name = "Apple", Description = "Tech giant" }
                };
                await context.Brands.AddRangeAsync(brands);
                await context.SaveChangesAsync();
            }

            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Shoes" },
                    new Category { Name = "Electronics" },
                    new Category { Name = "Clothing" }
                };
                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            if (!await context.Products.AnyAsync())
            {
                var nike = await context.Brands.FirstAsync(b => b.Name == "Nike");
                var apple = await context.Brands.FirstAsync(b => b.Name == "Apple");
                var shoes = await context.Categories.FirstAsync(c => c.Name == "Shoes");
                var electronics = await context.Categories.FirstAsync(c => c.Name == "Electronics");

                var products = new List<Product>
                {
                    new Product
                    {
                        Name = "Air Zoom Runner",
                        Description = "Lightweight running shoes with responsive cushioning.",
                        Price = 89.99m,
                        PictureUrl = "/images/products/nike-air-zoom.png",
                        Stock = 50,
                        BrandId = nike.Id,
                        CategoryId = shoes.Id
                    },
                    new Product
                    {
                        Name = "Court Classic",
                        Description = "Everyday casual sneaker with a clean silhouette.",
                        Price = 74.50m,
                        PictureUrl = "/images/products/nike-court-classic.png",
                        Stock = 35,
                        BrandId = nike.Id,
                        CategoryId = shoes.Id
                    },
                    new Product
                    {
                        Name = "iPhone 16",
                        Description = "Apple's latest smartphone with A18 chip.",
                        Price = 999.00m,
                        PictureUrl = "/images/products/iphone-16.png",
                        Stock = 20,
                        BrandId = apple.Id,
                        CategoryId = electronics.Id
                    },
                    new Product
                    {
                        Name = "MacBook Air M3",
                        Description = "Thin and light laptop with all-day battery life.",
                        Price = 1299.00m,
                        PictureUrl = "/images/products/macbook-air-m3.png",
                        Stock = 15,
                        BrandId = apple.Id,
                        CategoryId = electronics.Id
                    }
                };

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }
        }

        public static async Task SeedUsersAsync(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // 1. إنشاء الأدوار
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            if (!await roleManager.RoleExistsAsync("User"))
                await roleManager.CreateAsync(new IdentityRole("User"));

            // 2. إسناد دور Admin لحسابك المفضل
            var myUser = await userManager.FindByEmailAsync("AbdoSamir@gmail.com");
            if (myUser != null && !await userManager.IsInRoleAsync(myUser, "Admin"))
            {
                await userManager.AddToRoleAsync(myUser, "Admin");
            }

            // 3. إنشاء Admin افتراضي إذا كانت الجدول فارغاً
            if (!userManager.Users.Any())
            {
                var adminUser = new AppUser
                {
                    DisplayName = "Abdelrhman",
                    Email = "admin@test.com",
                    UserName = "admin@test.com",
                    Address = new Address
                    {
                        FirstName = "Abdelrhman",
                        LastName = "Samir",
                        Street = "10 Main St",
                        City = "Cairo",
                        State = "EG",
                        ZipCode = "12345"
                    }
                };

                var result = await userManager.CreateAsync(adminUser, "Pa$$w0rd");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}