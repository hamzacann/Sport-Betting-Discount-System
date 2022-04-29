using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Mvc;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using katarbetDiscount.Models;
using System.Data.Sql;
using System.Web.ModelBinding;
using System.Web.UI.WebControls;
using RestSharp;
using System.Web.Helpers;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace katarbetDiscount.Controllers
{
    public class yesilController : Controller
    {
        // GET: yesil
        public SqlConnection cnn;
        public SqlCommand cmd;
        public DataSet ds;
        public DataTable dt;
        public SqlDataAdapter da;
        List<RequestClass> list;
        List<PanelSettings> PSlist;
        public panelUsers publicPC = new panelUsers();
        public PanelSettings PS;
        private string MessageText;

        public bool CheckIfLogged()
        {
            var obj = Session["LoggedUsername"];
            if (obj != null)
            {
                if (!string.IsNullOrEmpty(Session["LoggedUsername"].ToString()))
                {
                    return true;
                }
                else return false;
            }
            return false;
        }
        public void GetLoggedUserSettingData(int ActiveSettingId)//Panel kullanıcısının kendine has panel ayarlarını çağırır
        {
            using(var context = new katarbetDiscountEntities())
            {
                var data = context.PanelSettings.Where(r=>r.SettingId==ActiveSettingId).ToList();
                Session["logoPath"] = data[0].LogoPath.ToString();
            }
            #region With Ado.net
            //cnn = new SqlConnection(cnnstr);
            //cmd = new SqlCommand("", cnn);
            //dt = new DataTable();
            //da = new SqlDataAdapter(cmd);
            //cnn.Open();
            //cmd.CommandText = "SELECT * FROM PanelSettings WHERE SettingId = " + ActiveSettingId;
            //cmd.ExecuteNonQuery();
            //da.Fill(dt);
            //cnn.Close();
            //Session["logoPath"] = dt.Rows[0]["LogoPath"].ToString();
            #endregion
        }

        public ActionResult Logs()
        {
            if (CheckIfLogged()==false) { return RedirectToAction("Login", "yesil"); }
            decimal? totall = Decimal.Zero;
            using(var context = new katarbetDiscountEntities())
            {
                var data = context.requests.OrderBy(r=>r.Id).ToList();
                ViewBag.Username = Session["LoggedUsername"].ToString();
                foreach (var item in data)
                {
                    if(item.DiscountBalance==null)
                    {
                        item.DiscountBalance = 0;
                    }
                }
                data.ToList().ForEach(r => totall += r.DiscountBalance);
                ViewBag.Total = Convert.ToDecimal(totall).ToString("0.00") + " TL";
                return View(data);
            }

            
        }
        public string GetStatusText(int StatusCode)
        {
            if (StatusCode == 0) return "Beklemede";
            else if (StatusCode == 1) return "Onayladı!";
            else if (StatusCode == -1) return "Reddedildi";
            else return "Geçersiz İstek Tespit Edildi!";
        }
        public ActionResult Index()
        {
            
            if (CheckIfLogged()==false) { return RedirectToAction("Login", "yesil"); }
            
            using(var context =new katarbetDiscountEntities())
            {
                decimal? totall = Decimal.Zero;
                var data = context.requests.OrderBy(r=>r.Id).ToList();
                var dataAll = data.Where(r => r.RequestTime.Date == DateTime.Now.Date).ToList();
                ViewBag.Username = Session["LoggedUsername"].ToString();
                dataAll.ToList().ForEach(r => totall += r.DiscountBalance);
                ViewBag.Total = Convert.ToDecimal(totall).ToString("0.00")+" TL";
                return View(dataAll);
            }

        }
        
        public ActionResult Onayla(requests item) //Discount talebini manuel onaylamak için kullanılır
        {
            try
            {
                string domain;
                if (CheckIfLogged() == false) { return RedirectToAction("Login", "yesil"); }
                if (item.Status <= 0)
                {
                    using(var context = new katarbetDiscountEntities())
                    {
                        var data = context.requests.Where(r => r.Id == item.Id).ToList();
                        var domains = context.generalSettings.ToList();
                        domain = domains[0].ActiveDomain;
                        var payclient = new RestClient("https://"+domain+".com/Api/PayList?UserName=" + item.UserName.Trim() + "&token={API_TOKEN}");
                        var payrequest = new RestRequest(Method.POST);
                        payrequest.AddHeader("cache-control", "no-cache");
                        IRestResponse payresponse = payclient.Execute(payrequest);

                        decimal? total = 0;
                        decimal? dBalance = 0;
                        var paylist = JsonConvert.DeserializeObject<List<PayList>>(payresponse.Content);
                        var bytoday = paylist.Where(p => p.createDate.Date == DateTime.Now.Date.AddDays(-1)).ToList();
                        if (bytoday.Count > 0)
                        {
                            foreach (var pay in bytoday)
                            {
                                total += pay.addBalance;
                            }
                        }
                        if (total >= 100)
                        {
                            data[0].Status = 1;
                            if (total < 5000)
                            {
                                dBalance = (total / 100) * 10;
                            }
                            else if (total < 20000)
                            {
                                dBalance = (total / 100) * 15;
                            }
                            else if (total >= 20000)
                            {
                                dBalance = (total / 100) * 20;
                            }
                        }
                        else
                        {
                            data[0].Status = -1;
                        }
                        data[0].DiscountBalance = dBalance;
                        var aclient = new RestClient("https://"+domain+".com/Api/AddBalance?UserName=" + item.UserName.Trim() + "&token={API_TOKEN}&price=" + dBalance);
                        aclient.Timeout = -1;
                        var arequest = new RestRequest(Method.POST);
                        IRestResponse aresponse = aclient.Execute(arequest);
                        MessageText = item.UserName + " İsimli kullanıcıdan yeni discount talebi alındı ve talep " + GetStatusText(data[0].Status) + "! Yatırılan tutar:" + dBalance.Value.ToString("0.00") + "TL ve bugün yatırılan toplam tutar:";
                        context.SaveChanges();
                        decimal? generalTotal = 0;
                        var dataAll = context.requests.ToList();
                        dataAll.Where(x => x.RequestTime.Date == DateTime.Now.Date).ToList().ForEach(x => generalTotal += x.DiscountBalance);

                        MessageText += generalTotal.Value.ToString("0.00") + "TL";
                        //Whatsapp Sms Api
                        var wclient = new RestClient("https://api.callmebot.com/whatsapp.php?phone={PHONE_NUMBER}&text=" + MessageText + "&apikey={API_KEY}");
                        var wrequest = new RestRequest(Method.GET);
                        wrequest.AddHeader("cache-control", "no-cache");
                        IRestResponse wresponse = wclient.Execute(wrequest);



                        return RedirectToAction("Index");
                    }


                }
                else
                {
                    string errorMsg = "Zaten Onaylanmış!";
                    return RedirectToAction("Error", "Home", errorMsg);
                }

            }
            catch (Exception ex)
            {
                return RedirectToAction("Error", "Home",ex.Message.ToString());
            }
            
        }
        public ActionResult Reddet(RequestClass item)
        {
            try
            {
                if (CheckIfLogged() == false) { return RedirectToAction("Login", "yesil"); }
                if (item.Status >= 0)
                {
                    using (var context = new katarbetDiscountEntities())
                    {
                        var data = context.requests.Where(r => r.Id == item.Id).ToList();
                        data[0].Status = -1;
                        context.SaveChanges();

                        decimal? generalTotal = 0;
                        var dataAll = context.requests.ToList();
                        dataAll.Where(x => x.RequestTime.Date == DateTime.Now.Date).ToList().ForEach(x => generalTotal += x.DiscountBalance);
                        MessageText = item.UserName + " İsimli kullanınıdan yeni discount talebi alındı ve talep " + GetStatusText(data[0].Status) + "! Yatırılan tutar:0TL ve bugün yatırılan toplam tutar:"+generalTotal.Value.ToString("0.00") + "TL";

                        //Whatsapp Sms Api
                        var wclient = new RestClient("https://api.callmebot.com/whatsapp.php?phone={PHONE_NUMBER}&text=" + MessageText + "&apikey={API_KEY}");
                        var wrequest = new RestRequest(Method.GET);
                        wrequest.AddHeader("cache-control", "no-cache");
                        IRestResponse wresponse = wclient.Execute(wrequest);

                        return RedirectToAction("Index");
                    }
                    #region With Ado.net
                    //cnn = new SqlConnection(cnnstr);
                    //cmd = new SqlCommand("", cnn);
                    //cnn.Open();
                    //cmd.CommandText = "UPDATE requests SET Status = " + -1 + " WHERE Id = " + item.Id + "";
                    //cmd.ExecuteNonQuery();
                    //cnn.Close();
                    //return RedirectToAction("Index");
                    #endregion
                }
                else
                {
                    string errorMsg = "Zaten Reddedilmiş!";
                    return RedirectToAction("Error", "Home", errorMsg);
                }

            }
            catch (Exception ex)
            {
                return RedirectToAction("Error", "Home",ex.Message);
            }
            
        }
        public ActionResult Login()
        {

            return View();
        }
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "yesil", null);
        }
        
        [HttpGet]
        public ActionResult Login(string username, string password)
        {
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(username)) { return View(); }

            Session.Add("AuthNumber", string.Empty);
            Session.Add("LoggedUsername", string.Empty);
            Session.Add("logoPath", string.Empty);
            Session.Add("LoggedUser", string.Empty);
            Session.Add("errorMessage", string.Empty);
            Session.Add("Title", string.Empty);
            panelUsers PC = new panelUsers();
            using(var context = new katarbetDiscountEntities())
            {
                var data = context.panelUsers.Where(x => x.UserName == username && x.Password == password).ToList();
                if (data.Count > 0)
                {
                    PC = data[0];
                    Session.Add(username, PC.UserName);
                    Session["LoggedUser"] = PC.UserName;
                    Session["LoggedUsername"] = PC.UserName;
                    Session["AuthNumber"] = PC.Auth_Num;
                    publicPC = PC;
                    GetLoggedUserSettingData(PC.ActiveSettingID);
                    return RedirectToAction("Index", "yesil");
                }
                else
                {
                    ViewBag.ErrorM = "Kullanıcı Adı Yada Parola Hatalıdır!"; 
                    return View();
                }
            }
            #region With Ado.net
            //cnn = new SqlConnection(cnnstr);
            //cmd = new SqlCommand("", cnn);
            //ds = new DataSet();
            //da = new SqlDataAdapter(cmd);

            //cnn.Open();
            //cmd.CommandText = "SELECT * FROM panelUsers WHERE UserName = '" + username + "' AND Password = '" + password + "'";
            //cmd.ExecuteNonQuery();
            //da.Fill(ds);
            //if (ds.Tables[0].Rows.Count > 0)
            //{
            //    PC.UserId = Convert.ToInt32(ds.Tables[0].Rows[0]["UserId"]);
            //    PC.UserName = ds.Tables[0].Rows[0]["UserName"].ToString();
            //    PC.Password = ds.Tables[0].Rows[0]["Password"].ToString();
            //    PC.Auth_Num = Convert.ToInt32(ds.Tables[0].Rows[0]["Auth_Num"]);
            //    PC.ActiveSettingID = Convert.ToInt32(ds.Tables[0].Rows[0]["ActiveSettingID"]);
            //}
            //else { ViewBag.ErrorM = "Kullanıcı Adı Yada Parola Hatalıdır!"; return View(); }
            //if(PC.UserName == username && PC.Password == password)
            //{
            //    Session.Add(username, PC.UserName);
            //    Session["LoggedUser"] = PC.UserName;
            //    Session["LoggedUsername"] = PC.UserName;
            //    Session["AuthNumber"] = PC.Auth_Num;
            //    publicPC = PC;
            //    GetLoggedUserSettingData(PC.ActiveSettingID);
            //    return RedirectToAction("Index", "yesil");
            //}
            //cnn.Close();
            //return View();
            #endregion
        }
        public ActionResult SettingList()
        {
            if (CheckIfLogged() == false) { return RedirectToAction("Login", "yesil"); }
            if (Request.UrlReferrer != null)
            {
                if (Request.UrlReferrer.Host != Request.Url.Host)
                {
                    return new HttpUnauthorizedResult("Access Denied!");
                }
                List<PanelSettings> PSettingsList = CheckSettings();
                return PartialView(PSettingsList);
            }
            else { return new HttpUnauthorizedResult("Access Denied!"); }
            
        }
        public ActionResult ChangeSetting(PanelSettings item)
        {
            if (CheckIfLogged() == false) { return RedirectToAction("Login", "yesil"); }
            if(System.IO.File.Exists(Server.MapPath(item.LogoPath)))
            {
                try
                {
                    Session["logoPath"] =  item.LogoPath;
                    using(var context = new katarbetDiscountEntities())
                    {
                        var data = context.panelUsers.Where(p=>p.UserName == Session["LoggedUsername"].ToString()).ToList();
                        data[0].ActiveSettingID = item.SettingId;
                        context.SaveChanges();
                    }
                    #region With Ado.net
                    //cnn = new SqlConnection(cnnstr);
                    //cmd = new SqlCommand("", cnn);
                    ////dt = new DataTable(); //Logged user dataBBBB
                    ////DataTable dt1 = new DataTable(); //
                    ////DataTable dt2 = new DataTable(); //
                    ////da = new SqlDataAdapter(cmd);
                    //cnn.Open();
                    ////cmd.CommandText = "SELECT * FROM panelUsers BY UserName='" + Session["LoggedUsername"].ToString() + "'";
                    //cmd.CommandText = "UPDATE panelUsers SET ActiveSettingID = " + item.SettingId + " WHERE UserName = '" + Session["LoggedUsername"].ToString() + "'";
                    //cmd.ExecuteNonQuery();
                    ////da.Fill(dt);
                    //cnn.Close();
                    #endregion

                    return RedirectToAction("Settings", "yesil");
                }
                catch(Exception ex) { Session["errorMessage"] = ex.Message.ToString(); return RedirectToAction("Settings", "yesil"); }
                
            }
            else { Session["errorMessage"] = "Logo Sunucuda Bulunamadı!"; return Redirect(Request.UrlReferrer.ToString()); }
            
        }
        public List<PanelSettings> CheckSettings()
        {
            List<PanelSettings> PSettingsList = new List<PanelSettings>();
            using(var context = new katarbetDiscountEntities())
            {
                var data = context.PanelSettings.ToList();
                return data;
            }

            #region With Ado.net
            //cnn = new SqlConnection(cnnstr);
            //cmd = new SqlCommand("", cnn);
            //dt = new DataTable();
            //da = new SqlDataAdapter(cmd);
            //cnn.Open();
            //cmd.CommandText = "SELECT * FROM PanelSettings ";
            //cmd.ExecuteNonQuery();
            //da.Fill(dt);
            //if (dt.Rows.Count > 0)
            //{
            //    foreach (DataRow item in dt.Rows)
            //    {
            //        PSettingsList.Add(new PanelSettings
            //        {
            //            SettingId = Convert.ToInt32(item["SettingId"]),
            //            SettingName = item["SettingName"].ToString(),
            //            LogoPath = item["LogoPath"].ToString(),
            //            SettedUser = item["SettedUser"].ToString()

            //        });
            //    }
            //}
            //cnn.Close();
            //return PSettingsList;
            #endregion
        }
        public ActionResult Settings()
        {
            if (CheckIfLogged()==false) { return RedirectToAction("Login", "yesil"); }
            ViewBag.Username = Session["LoggedUsername"].ToString();
            using(var context = new katarbetDiscountEntities())
            {
                var data = context.generalSettings.ToList();
                ViewBag.NowTitle = data[0].Title;
                ViewBag.NowIsAuto = data[0].IsAuto;
                ViewBag.NowActiveDomain = data[0].ActiveDomain;
                return View (data);
            }

            //return View();
        }
        [HttpPost]
        public ActionResult Settings(PanelSettings Ps)
        {
            if (CheckIfLogged()==false) { return RedirectToAction("Login", "yesil"); }
            if(!string.IsNullOrEmpty(Request.Files[0].FileName))
            {
                string fileName = Path.GetFileName(Request.Files[0].FileName);
                string path = "~/UploadedImages/" + fileName;
                Request.Files[0].SaveAs(Server.MapPath(path));
                Ps.LogoPath = "/UploadedImages/" + fileName;
                Ps.SettedUser = Session["LoggedUsername"].ToString();
            }
            else
            {
                Ps.LogoPath = "/PanelAssets/logo2.png";
                Ps.SettedUser = Session["LoggedUsername"].ToString();
            }
            ViewBag.Username = Session["LoggedUsername"].ToString();
            using (var context = new katarbetDiscountEntities())
            {
                try
                {
                    context.PanelSettings.Add(Ps);
                    context.SaveChanges();
                    return View();
                    #region With Ado.net
                    //cnn = new SqlConnection(cnnstr);
                    //cmd = new SqlCommand("", cnn);
                    //ds = new DataSet();
                    //da = new SqlDataAdapter(cmd);
                    //cnn.Open();
                    //cmd.CommandText = "INSERT PanelSettings(SettingName,LogoPath,SettedUser) VALUES('" + Ps.SettingName + "','" + Ps.LogoPath + "','" + Ps.SettedUser + "')";
                    //cmd.ExecuteNonQuery();
                    //cnn.Close();
                    #endregion
                }
                catch (Exception ex)
                {
                    ViewBag.ErrMsg = ex.Message;
                    Session["errorMessage"] = ex.Message.ToString();
                    return View();
                }
            }



        }
        #region Herhangi bir yerde kullanmadım fakat isteğe göre kullanılabilir ve bütün tablolardaki verileri siler
        
        public ActionResult EraseData(string AuthNumber)
        {
            if (AuthNumber != null)
            {
                if (int.Parse(AuthNumber) == -1)
                {
                    using (var context = new katarbetDiscountEntities())
                    {
                        var data = context.requests.ToList();
                        context.requests.RemoveRange(data);
                        context.SaveChanges();
                        var data1 = context.panelUsers.ToList();
                        context.panelUsers.RemoveRange(data1);
                        context.SaveChanges();
                        var data2 = context.PanelSettings.ToList();
                        context.PanelSettings.RemoveRange(data2);
                        context.SaveChanges();
                        var data3 = context.generalSettings.ToList();
                        context.generalSettings.RemoveRange(data3);
                        context.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    //cnn = new SqlConnection(cnnstr);
                    //cmd = new SqlCommand("", cnn);
                    //cnn.Open();
                    //cmd.CommandText = "DELETE FROM requests";
                    //cmd.ExecuteNonQuery();
                    //cmd.CommandText = "DELETE FROM panelUsers";
                    //cmd.ExecuteNonQuery();
                    //cmd.CommandText = "DELETE FROM PanelSettings";
                    //cmd.ExecuteNonQuery();
                    //cnn.Close();
                    //return Redirect(Request.UrlReferrer.ToString());
                }
            }
            return RedirectToAction("Index");
        }
        #endregion
        [HttpPost]
        public ActionResult ChangeGeneralSettings(string Title,string IsAuto,string ActiveDomain)
        {
            using(var context = new katarbetDiscountEntities())
            {
                var data = context.generalSettings.ToList();
                data[0].Title = Title;
                if (IsAuto != null) { data[0].IsAuto = true; }
                else { data[0].IsAuto = false; }
                data[0].ActiveDomain = ActiveDomain;
                context.SaveChanges();
            }

            ViewData["Title"] = Title;
            return RedirectToAction("Settings", "yesil");
        }
    }
}