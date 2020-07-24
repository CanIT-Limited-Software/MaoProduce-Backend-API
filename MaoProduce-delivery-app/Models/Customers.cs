using System;

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MaoProduce_delivery_app
{
    public class Customers
    {
        public string Id { get; set; }
        public string Name { get; set; } 
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime CreatedTimestamp { get; set; }
    }
}
