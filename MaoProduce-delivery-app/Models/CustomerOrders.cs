using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MaoProduce_delivery_app
{
    public class CustomerOrders
    {
        public string CustomerId { get; set; }
        public string LastOrderId { get; set; }
        public List<Orders> Orders = new List<Orders>();
        public void addList(Orders order)
        {
            Orders.Add(order);
        }
        public void removeList(Orders order)
        {
            Orders.Remove(order);
        }
    }
}