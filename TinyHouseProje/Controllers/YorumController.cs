using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Data.SqlClient;
using TinyHouseProje.Models;
using TinyHouseProje.DAL;

namespace TinyHouseProje.Controllers
{
    public class YorumController : Controller
    {
        public ActionResult Ekle(int rezervasyonId)
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "3")
                return RedirectToAction("Giris", "Kullanici");

            ViewBag.RezervasyonID = rezervasyonId;
            return View();
        }

        [HttpPost]
        public ActionResult Ekle(Yorum model)
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "3")
                return RedirectToAction("Giris", "Kullanici");

            model.KiraciID = Convert.ToInt32(Session["KullaniciID"]);
            model.YorumTarihi = DateTime.Now;

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                conn.Open();

                SqlCommand evCmd = new SqlCommand("SELECT EvID FROM Rezervasyonlar WHERE RezervasyonID = @ID", conn);
                evCmd.Parameters.AddWithValue("@ID", model.RezervasyonID);
                model.EvID = (int)evCmd.ExecuteScalar();

                SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO Yorumlar (EvID, KiraciID, RezervasyonID, Puan, Yorum, YorumTarihi)
                    VALUES (@EvID, @KiraciID, @RezervasyonID, @Puan, @Yorum, @YorumTarihi)", conn);

                cmd.Parameters.AddWithValue("@EvID", model.EvID);
                cmd.Parameters.AddWithValue("@KiraciID", model.KiraciID);
                cmd.Parameters.AddWithValue("@RezervasyonID", model.RezervasyonID);
                cmd.Parameters.AddWithValue("@Puan", model.Puan);
                cmd.Parameters.AddWithValue("@Yorum", model.YorumMetni);
                cmd.Parameters.AddWithValue("@YorumTarihi", model.YorumTarihi);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("BenimRezervasyonlarim", "Rezervasyon");
        }

        public ActionResult GelenYorumlar()
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "2")
                return RedirectToAction("Giris", "Kullanici");

            int evSahibiID = Convert.ToInt32(Session["KullaniciID"]);
            List<Yorum> yorumlar = new List<Yorum>();

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand(@"
                    SELECT y.*, k.Ad + ' ' + k.Soyad AS KiraciAdSoyad, e.Baslik AS EvBaslik
                    FROM Yorumlar y
                    INNER JOIN TinyHouses e ON y.EvID = e.EvID
                    INNER JOIN Kullanicilar k ON y.KiraciID = k.KullaniciID
                    WHERE e.SahipID = @SahipID", conn);

                cmd.Parameters.AddWithValue("@SahipID", evSahibiID);

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    yorumlar.Add(new Yorum
                    {
                        YorumID = Convert.ToInt32(dr["YorumID"]),
                        Puan = Convert.ToInt32(dr["Puan"]),
                        YorumMetni = dr["Yorum"].ToString(),
                        Cevap = dr["Cevap"] != DBNull.Value ? dr["Cevap"].ToString() : null,
                        YorumTarihi = Convert.ToDateTime(dr["YorumTarihi"]),
                        KiraciAdSoyad = dr["KiraciAdSoyad"].ToString(),
                        EvBaslik = dr["EvBaslik"].ToString()
                    });
                }
            }

            return View(yorumlar);
        }

        [HttpPost]
        public ActionResult Cevapla(int yorumId, string cevap)
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "2")
                return RedirectToAction("Giris", "Kullanici");

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand("UPDATE Yorumlar SET Cevap = @Cevap, CevapTarihi = GETDATE() WHERE YorumID = @ID", conn);
                cmd.Parameters.AddWithValue("@Cevap", cevap);
                cmd.Parameters.AddWithValue("@ID", yorumId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("GelenYorumlar");
        }
    }
}
