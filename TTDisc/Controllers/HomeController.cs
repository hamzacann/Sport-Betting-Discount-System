using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.WebPages;
using System.Web.Mvc;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using katarbetDiscount.Models;
using RestSharp;
using Newtonsoft;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;

namespace katarbetDiscount.Controllers
{
    public class HomeController : Controller
    {
        //public string cnnstr = ConfigurationManager.ConnectionStrings["DCDatabase"].ConnectionString;
        //public string cnnstr = "Data Source=totobo-discount.database.windows.net;Initial Catalog=totoboDiscount;Persist Security Info=True;User ID=Judge;Password=Jdg12??34";
        public SqlConnection cnn;
        public SqlCommand cmd;
        public DataSet ds;
        public SqlDataAdapter da;
        public static string Message = "";

        public void LoadSettings()
        {
            using(var context = new katarbetDiscountEntities())
            {
                var data = context.generalSettings.ToList();
                HttpContext.Application.Add("Title", data[0].Title);
                Session.Add("Title", data[0].Title);
                Session.Add("ActiveDomain", data[0].ActiveDomain);

            }
        }

        public ActionResult Index()
        {
            LoadSettings();
            string msg = Message;
            ViewData.Add("Title", msg);
            return View();
        }
        
        public ActionResult Rules()
        {

            return View();
        }

        public ActionResult Error(string errMsg)
        {
            ViewBag.MSG = errMsg;
            return View();
        }

        public ActionResult Status()
        {
            //string name  = Session["RequestUsername"].ToString();
            if (Session["RequestUsername"] != null && int.Parse(Session["RequestStatus"].ToString()) != 99)
            {
                if (int.Parse(Session["RequestStatus"].ToString()) == 0) { ViewBag.StatusM = "Beklemede"; ViewBag.Class = "onstandby"; }
                if (int.Parse(Session["RequestStatus"].ToString()) > 0) { ViewBag.StatusM = "Kabul Edildi"; ViewBag.Class = "approved"; }
                if (int.Parse(Session["RequestStatus"].ToString()) < 0) { ViewBag.StatusM = "Reddedildi"; ViewBag.Class = "reject"; }
            }
            else { string errMsg = "Hata!"; return RedirectToAction("Error", "Home", errMsg); }

            return View();
        }
        public ActionResult Request()
        {

            return View();
        }
        [HttpGet]
        public ActionResult Request(string username,string submitForm)
        {
            string domain = Session["ActiveDomain"].ToString();
            requests RC = new requests();
            using (var context = new katarbetDiscountEntities())
            {
                
                var data = context.requests.Where(r => r.UserName == username.Trim()).OrderByDescending(r => r.Id).Take(1).ToList();
                if (data.Count > 0)
                {
                    RC.UserName = data[0].UserName;
                    RC.Status = data[0].Status;
                    RC.RequestTime = data[0].RequestTime;
                }
                

            }

            if (!string.IsNullOrEmpty(username))
            {
                string MessageText = String.Empty;
                
                //Talep
                if (submitForm == "formSave")
                {
                    var client = new RestClient("https://" + domain + ".com/Api/CheckCustomer?UserName=" + username + "&token=62365083e665aeb0e5433871");
                    client.Timeout = -1;
                    var request = new RestRequest(Method.POST);
                    IRestResponse response = client.Execute(request);
                    if (response != null)
                    {
                        if (response.Content != null)
                        {
                            CheckUserRoot check = new CheckUserRoot();
                            check = JsonConvert.DeserializeObject<CheckUserRoot>(response.Content);
                            if (check.status == false)
                            {
                                Session.Add("UserError", check.errorMsg);
                                return RedirectToAction("Error", "Home");
                            }
                        }
                    }
                    if (/*RC.Status != 0 || */RC.RequestTime.Date != DateTime.Now.Date)
                    {

                        RC.UserName = username.Trim();
                        RC.Status = 0;
                        RC.RequestTime = DateTime.Now;
                        RC.DiscountBalance = 0;

                        using(var context = new katarbetDiscountEntities())
                        {
                            

                            var checkAuto = context.generalSettings.ToList();
                            if (checkAuto[0].IsAuto == true)
                            {
                                var payclient = new RestClient("https://"+domain+".com/Api/PayList?UserName=" + username + "&token=62365083e665aeb0e5433871");
                                var payrequest = new RestRequest(Method.POST);
                                payrequest.AddHeader("postman-token", "c35f35eb-40b7-964a-7702-453ffa8ad528");
                                payrequest.AddHeader("cache-control", "no-cache");
                                IRestResponse payresponse = payclient.Execute(payrequest);

                                decimal? total = 0;
                                decimal? dBalance = 0;
                                var paylist = JsonConvert.DeserializeObject<List<PayList>>(payresponse.Content);
                                var bytoday = paylist.Where(p => p.createDate.Date == DateTime.Now.Date.AddDays(-1)).ToList();
                                if (bytoday.Count > 0)
                                {
                                    foreach (var item in bytoday)
                                    {
                                        total += item.addBalance;
                                    }
                                }
                                if(total >= 100)
                                {
                                    RC.Status = 1;
                                    if(total < 5000)
                                    {
                                        dBalance = (total / 100) * 10;
                                    }
                                    else if(total < 20000)
                                    {
                                        dBalance = (total / 100) * 15;
                                    }
                                    else if(total >= 20000)
                                    {
                                        dBalance = (total / 100) * 20;
                                    }
                                }
                                else
                                {
                                    RC.Status = -1;
                                }
                                RC.DiscountBalance = dBalance;
                                var aclient = new RestClient("https://"+domain+".com/Api/AddBalance?UserName=" + username + "&token=62365083e665aeb0e5433871&price=" + dBalance);
                                aclient.Timeout = -1;
                                var arequest = new RestRequest(Method.POST);
                                IRestResponse aresponse = aclient.Execute(arequest);
                                MessageText = RC.UserName+" İsimli Kullanıcıdan yeni discount talebi alındı ve talep "+GetStatusText(RC.Status)+"! Yatırılan tutar:" + dBalance.Value.ToString("0.00") + "TL ve bugün yatırılan toplam tutar:";
                            }
                            context.requests.Add(RC);
                            context.SaveChanges();
                            decimal? generalTotal = 0;
                            var data = context.requests.ToList();
                            data.Where(x => x.RequestTime.Date == DateTime.Now.Date).ToList().ForEach(x => generalTotal+=x.DiscountBalance);
                            //var dataList = context.requests.Where(r => r.RequestTime == DateTime.Now.Date).ToList();
                            //dataList.ForEach(r => generalTotal += r.DiscountBalance);

                            MessageText +=generalTotal.Value.ToString("0.00")+"TL";


                            

                            return View();

                            
                        }

                    }
                    else { return View(); }

                }
                else if(submitForm == "checkStatus")
                {
                    //Talep kontrol
                    

                    Session.Add("RequestUsername", RC.UserName);
                    if (RC.UserName == null) { RC.Status = 99; }
                    Session.Add("RequestStatus", RC.Status);

                    return RedirectToAction("Status");
                }
            }
            else
            {
                string message = "Kullanıcı adı gereklidir!";
                Message = message;
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
        public string GetStatusText(int StatusCode)
        {
            if (StatusCode == 0) return "Beklemede";
            else if (StatusCode == 1) return "Onayladı!";
            else if (StatusCode == -1) return "Reddedildi";
            else return "Geçersiz İstek Tespit Edildi!";
        }
    }
}