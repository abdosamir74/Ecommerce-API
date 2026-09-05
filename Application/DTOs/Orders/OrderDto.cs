using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Orders
{
    public class OrderDto
    {
        public string BasketId { get; set; } = string.Empty;
        public AddressDto ShipToAddress { get; set; } = null!;
    }
}
