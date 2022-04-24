using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlTypes;
using System.Data.Sql;
using System.Data.SqlClient;

namespace katarbetDiscount.Models
{
    public class RequestClass
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public int Status { get; set; }
        public DateTime RequestTime { get; set; }/* = DateTime.Now;*/
    }
}