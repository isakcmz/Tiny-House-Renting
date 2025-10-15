using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TinyHouseProje.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (Session["KullaniciID"] == null)
            {
                return RedirectToAction("Giris", "Kullanici");
            }

            ViewBag.Kullanici = Session["AdSoyad"];
            ViewBag.RolID = Session["RolID"];
            return View();
        }

        
    }
}