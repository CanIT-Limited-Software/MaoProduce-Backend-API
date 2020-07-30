using MaoProduce_delivery_app.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MaoProduce_delivery_app
{
    public class Orders
    {
        public string Id { get; set; }
        public string TotalPrice { get; set; }
        public DateTime DateTime { get; set; }
        public bool IsOpen { get; set; }
        public List<OrderProduct> Products { get; set; }
    }
}
