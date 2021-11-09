using System;
using System.Collections.Generic;

namespace OrderApp.Models
{
    public class Order
    {
        public string Id { get; set; }

        public string Reference { get; set; }

        public string Customer { get; set; }

        public DateTime Date { get; set; }

        public List<OrderLine> OrderLines { get; set; }
    }
}
