using Application.Authorization;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Authorization
{
    public static class PermissionSeeder
    {
        public static async Task SeedAsync(
            RoleManager<IdentityRole> roleManager)
        {
            var adminRole = await roleManager.FindByNameAsync("Admin");

            if (adminRole is null)
                return;

            var permissions = new[]
            {
            Permissions.Products.Read,
            Permissions.Products.Create,
            Permissions.Products.Update,
            Permissions.Products.Delete,

            Permissions.Orders.Read,
            Permissions.Orders.Update,

            Permissions.Users.Read,
            Permissions.Users.Update
        };

            var existingClaims =
                await roleManager.GetClaimsAsync(adminRole);

            foreach (var permission in permissions)
            {
                if (!existingClaims.Any(c =>
                    c.Type == "permission" &&
                    c.Value == permission))
                {
                    await roleManager.AddClaimAsync(
                        adminRole,
                        new Claim("permission", permission));
                }
            }
        }
    }
}
