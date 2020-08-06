using System;
using System.Collections.Generic;
using System.Text;

namespace MaoProduce_delivery_app.Models
{
    public class Order_AllOrders : Orders
    {
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
    }
}
