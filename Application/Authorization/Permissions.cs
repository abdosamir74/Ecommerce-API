using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Authorization
{
    public static class Permissions
    {
        public static class Products
        {
            public const string Read = "Products.Read";
            public const string Create = "Products.Create";
            public const string Update = "Products.Update";
            public const string Delete = "Products.Delete";
        }

        public static class Orders
        {
            public const string Read = "Orders.Read";
            public const string Update = "Orders.Update";
        }

        public static class Users
        {
            public const string Read = "Users.Read";
            public const string Update = "Users.Update";
        }
    }
}
