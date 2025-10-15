using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TinyHouseProje.DAL;
using TinyHouseProje.Models;

namespace TinyHouseProje.Controllers
{
    public class KullaniciController : Controller
    {

        // GET: Kullanici/Giris
        public ActionResult Giris()
        {
            return View();
        }

        // POST: Kullanici/Giris
        [HttpPost]
        public ActionResult Giris(string eposta, string sifre)
        {
            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM Kullanicilar WHERE Eposta = @Eposta AND Sifre = @Sifre AND Aktif = 1", conn);
                cmd.Parameters.AddWithValue("@Eposta", eposta);
                cmd.Parameters.AddWithValue("@Sifre", sifre);

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    Session["KullaniciID"] = dr["KullaniciID"];
                    Session["AdSoyad"] = dr["Ad"].ToString() + " " + dr["Soyad"].ToString();
                    Session["RolID"] = dr["RolID"];

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.Hata = "E-posta veya şifre hatalı!";
                    return View();
                }
            }
        }





        // GET: Kullanici/Kayit
        public ActionResult Kayit()
        {
            return View();
        }

        // POST: Kullanici/Kayit
        [HttpPost]
        public ActionResult Kayit(Kullanici model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool basarili = KullaniciDAL.KullaniciEkle(model);
                    if (basarili)
                    {
                        ViewBag.Mesaj = "Kayıt başarıyla tamamlandı.";
                        ModelState.Clear();
                    }
                    else
                    {
                        ViewBag.Hata = "Kayıt sırasında hata oluştu.";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Hata = "HATA: " + ex.Message;
            }

            return View();
        }


        public ActionResult Cikis()
        {
            Session.Clear(); // Tüm oturum bilgilerini temizle
            return RedirectToAction("Giris", "Kullanici"); // Giriş sayfasına yönlendir
        }


    }
}