using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace katarbetDiscount.Models
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class PayList
    {
        public string userName { get; set; }
        public string userId { get; set; }
        public string ownerName { get; set; }
        public string ownerId { get; set; }
        public decimal oldBalance { get; set; }
        public decimal newBalance { get; set; }
        public decimal addBalance { get; set; }
        public string ip { get; set; }
        public int upDown { get; set; }
        public int balanceType { get; set; }
        public DateTime createDate { get; set; }
        public DateTime createDateTR { get; set; }
        public string description { get; set; }
        public string masterUser { get; set; }
        public object descriptionType { get; set; }
        public string id { get; set; }
    }
    public class PayRoot
    {
        public List<PayList> payList { get; set; }
    }


}