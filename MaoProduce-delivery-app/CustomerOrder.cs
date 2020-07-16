using System;

using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MaoProduce_delivery_app
{
    public class CustomerOrder
    {
        public string CustomerId;
        public List<Orders> Orders;
    }
}