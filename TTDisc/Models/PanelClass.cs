using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace katarbetDiscount.Models
{
    public class PanelClass
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Auth_Num { get; set; } //0 super yesil 1 yesil 2 moderator
        public int ActiveSettingID { get; set; } = 0; // default 0
    }
}