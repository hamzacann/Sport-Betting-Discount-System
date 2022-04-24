using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace katarbetDiscount.Models
{
    public class Result
    {
        public string id { get; set; }
        public string name { get; set; }
        public string username { get; set; }
        public object active { get; set; }
    }

    public class Root
    {
        public bool success { get; set; }
        public List<Result> result { get; set; }
    }
}