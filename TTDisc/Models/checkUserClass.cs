using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace katarbetDiscount.Models
{
    public class CheckUserRoot
    {
        public bool? status { get; set; }
        public string errorMsg { get; set; }
    }// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class UserRoot
    {
        public string username { get; set; }
        public string fullName { get; set; }
        public string name { get; set; }
        public string surname { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public string gender { get; set; }
        public object dolarComment { get; set; }
        public string parentUserId { get; set; }
        public string casinoRulesId { get; set; }
        public string couponRulesId { get; set; }
        public string onlineMaster { get; set; }
        public object resetToken { get; set; }
        public int? affiliateId { get; set; }
        public object cannotLoginUntilDateUtc { get; set; }
        public DateTime birthDate { get; set; }
        public bool? active { get; set; }
        public bool? isOnline { get; set; }
        public bool? isLockCasino { get; set; }
        public bool? nonLimitCreate { get; set; }
        public bool? isShowUserCreateMenu { get; set; }
        public bool? deleted { get; set; }
        public string phone { get; set; }
        public int? publicCasino { get; set; }
        public bool? isRent { get; set; }
        public bool? isSystemAccount { get; set; }
        public bool? isCreateDealer { get; set; }
        public object systemName { get; set; }
        public object lastIpAddress { get; set; }
        public DateTime createdOnUtc { get; set; }
        public DateTime lastLoginDateUtc { get; set; }
        public DateTime lastActivityDateUtc { get; set; }
        public string currency { get; set; }
        public string url { get; set; }
        public string country { get; set; }
        public int? customerGroupId { get; set; }
        public int? customerCreateLimit { get; set; }
        public int? dealerCreateLimit { get; set; }
        public int? customerRoleId { get; set; }
        public double? betBalance { get; set; }
        public double? casinoBalance { get; set; }
        public double? casinoBalanceEuro { get; set; }
        public double? commission { get; set; }
        public double? commissionTypeId { get; set; }
        public string id { get; set; }
        
    }


}