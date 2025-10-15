using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TinyHouseProje.Models;
using System.Data.SqlClient;
using TinyHouseProje.DAL;

namespace TinyHouseProje.Controllers
{
    public class TinyHouseController : Controller
    {
        // GET: TinyHouse/Ekle
        public ActionResult Ekle()
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "2") // Yalnızca ev sahipleri
                return RedirectToAction("Giris", "Kullanici");

            return View();
        }

        // POST: TinyHouse/Ekle
        [HttpPost]
        public ActionResult Ekle(TinyHouse model, HttpPostedFileBase gorsel)
        {
            try
            {
                if (Session["KullaniciID"] == null)
                    return RedirectToAction("Giris", "Kullanici");

                int evID = 0;

                using (SqlConnection conn = Veritabani.BaglantiGetir())
                {
                    SqlCommand cmd = new SqlCommand(@"INSERT INTO TinyHouses 
                (SahipID, Baslik, Aciklama, Konum, Fiyat, UygunlukBaslangic, UygunlukBitis, Durum) 
                OUTPUT INSERTED.EvID
                VALUES (@SahipID, @Baslik, @Aciklama, @Konum, @Fiyat, @Baslangic, @Bitis, 1)", conn);

                    cmd.Parameters.AddWithValue("@SahipID", Session["KullaniciID"]);
                    cmd.Parameters.AddWithValue("@Baslik", model.Baslik);
                    cmd.Parameters.AddWithValue("@Aciklama", model.Aciklama);
                    cmd.Parameters.AddWithValue("@Konum", model.Konum);
                    cmd.Parameters.AddWithValue("@Fiyat", model.Fiyat);
                    cmd.Parameters.AddWithValue("@Baslangic", model.UygunlukBaslangic);
                    cmd.Parameters.AddWithValue("@Bitis", model.UygunlukBitis);

                    conn.Open();
                    evID = (int)cmd.ExecuteScalar(); // Ev eklendikten sonra ID'yi al
                }

                // Görsel yükleme işlemi
                if (gorsel != null && gorsel.ContentLength > 0)
                {
                    string dosyaAdi = Guid.NewGuid() + System.IO.Path.GetExtension(gorsel.FileName);
                    string yol = "~/Uploads/" + dosyaAdi;
                    string fizikselYol = Server.MapPath(yol);
                    gorsel.SaveAs(fizikselYol);

                    using (SqlConnection conn = Veritabani.BaglantiGetir())
                    {
                        SqlCommand cmdG = new SqlCommand("INSERT INTO EvGorselleri (EvID, GorselYolu) VALUES (@EvID, @Yol)", conn);
                        cmdG.Parameters.AddWithValue("@EvID", evID);
                        cmdG.Parameters.AddWithValue("@Yol", yol);
                        conn.Open();
                        cmdG.ExecuteNonQuery();
                    }
                }

                ViewBag.Mesaj = "İlan ve görsel başarıyla eklendi.";
            }
            catch (Exception ex)
            {
                ViewBag.Hata = "HATA: " + ex.Message;
            }

            return View();
        }





        public ActionResult Listele()
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "2")
                return RedirectToAction("Giris", "Kullanici");

            List<TinyHouse> ilanlar = new List<TinyHouse>();
            int sahipID = Convert.ToInt32(Session["KullaniciID"]);

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand(@"
                 SELECT t.*, 
                     (SELECT TOP 1 GorselYolu FROM EvGorselleri g WHERE g.EvID = t.EvID) AS GorselYolu
                FROM TinyHouses t
                 WHERE t.SahipID = @SahipID", conn);

                cmd.Parameters.AddWithValue("@SahipID", sahipID);

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    ilanlar.Add(new TinyHouse
                    {
                        EvID = Convert.ToInt32(dr["EvID"]),
                        Baslik = dr["Baslik"].ToString(),
                        Aciklama = dr["Aciklama"].ToString(),
                        Konum = dr["Konum"].ToString(),
                        Fiyat = Convert.ToDecimal(dr["Fiyat"]),
                        UygunlukBaslangic = Convert.ToDateTime(dr["UygunlukBaslangic"]),
                        UygunlukBitis = Convert.ToDateTime(dr["UygunlukBitis"]),
                        Durum = Convert.ToBoolean(dr["Durum"]),
                        GorselYolu = dr["GorselYolu"] == DBNull.Value ? null : dr["GorselYolu"].ToString()


                    });
                }
            }

            return View(ilanlar);
        }



        public ActionResult Ara(string konum = "", decimal? minFiyat = null, decimal? maxFiyat = null, DateTime? baslangic = null, DateTime? bitis = null)
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "3")
                return RedirectToAction("Giris", "Kullanici");

            List<TinyHouse> ilanlar = new List<TinyHouse>();
            List<string> konumlar = new List<string>();
            List<Tuple<string, string, decimal, int>> populerEvler = new List<Tuple<string, string, decimal, int>>();

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                conn.Open();

                // ✅ 1. Konumları al
                SqlCommand konumCmd = new SqlCommand("SELECT DISTINCT Konum FROM TinyHouses WHERE Durum = 1", conn);
                SqlDataReader drKonum = konumCmd.ExecuteReader();
                while (drKonum.Read())
                {
                    konumlar.Add(drKonum["Konum"].ToString());
                }
                drKonum.Close();

                // ✅ 2. Popüler Evler
                SqlCommand populerCmd = new SqlCommand(@"
                        SELECT TOP 3 t.EvID, t.Baslik, 
                        (SELECT TOP 1 GorselYolu FROM EvGorselleri WHERE EvID = t.EvID) AS GorselYolu,
                        AVG(CAST(y.Puan AS FLOAT)) AS OrtalamaPuan
                        FROM TinyHouses t
                        INNER JOIN Yorumlar y ON t.EvID = y.EvID
                        WHERE t.Durum = 1
                        GROUP BY t.EvID, t.Baslik
                        HAVING AVG(CAST(y.Puan AS FLOAT)) >= 4
                        ORDER BY OrtalamaPuan DESC", conn);

                SqlDataReader pr = populerCmd.ExecuteReader();
                while (pr.Read())
                {
                    populerEvler.Add(new Tuple<string, string, decimal, int>(
                        pr["Baslik"].ToString(),
                        pr["GorselYolu"] != DBNull.Value ? pr["GorselYolu"].ToString() : "",
                        Convert.ToDecimal(pr["OrtalamaPuan"]),
                        Convert.ToInt32(pr["EvID"])
                    ));
                }
                pr.Close();

                // ✅ 3. Filtreli İlanları al
                string query = @"
            SELECT t.*, 
                   (SELECT TOP 1 GorselYolu FROM EvGorselleri WHERE EvID = t.EvID) AS GorselYolu
            FROM TinyHouses t
            WHERE t.Durum = 1";

                if (!string.IsNullOrEmpty(konum))
                    query += " AND t.Konum = @Konum";
                if (minFiyat.HasValue)
                    query += " AND t.Fiyat >= @MinFiyat";
                if (maxFiyat.HasValue)
                    query += " AND t.Fiyat <= @MaxFiyat";
                if (baslangic.HasValue && bitis.HasValue)
                    query += " AND t.UygunlukBaslangic <= @Baslangic AND t.UygunlukBitis >= @Bitis";

                SqlCommand cmd = new SqlCommand(query, conn);

                if (!string.IsNullOrEmpty(konum))
                    cmd.Parameters.AddWithValue("@Konum", konum);
                if (minFiyat.HasValue)
                    cmd.Parameters.AddWithValue("@MinFiyat", minFiyat);
                if (maxFiyat.HasValue)
                    cmd.Parameters.AddWithValue("@MaxFiyat", maxFiyat);
                if (baslangic.HasValue)
                    cmd.Parameters.AddWithValue("@Baslangic", baslangic);
                if (bitis.HasValue)
                    cmd.Parameters.AddWithValue("@Bitis", bitis);

                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    ilanlar.Add(new TinyHouse
                    {
                        EvID = Convert.ToInt32(dr["EvID"]),
                        Baslik = dr["Baslik"].ToString(),
                        Konum = dr["Konum"].ToString(),
                        Fiyat = Convert.ToDecimal(dr["Fiyat"]),
                        UygunlukBaslangic = Convert.ToDateTime(dr["UygunlukBaslangic"]),
                        UygunlukBitis = Convert.ToDateTime(dr["UygunlukBitis"]),
                        Durum = Convert.ToBoolean(dr["Durum"]),
                        GorselYolu = dr["GorselYolu"] != DBNull.Value ? dr["GorselYolu"].ToString() : null
                    });
                }
                dr.Close();
            }

            ViewBag.Konumlar = new SelectList(konumlar);
            ViewBag.PopulerEvler = populerEvler;
            ViewBag.SelectedKonum = konum;
            ViewBag.MinFiyat = minFiyat;
            ViewBag.MaxFiyat = maxFiyat;
            ViewBag.Baslangic = baslangic?.ToString("yyyy-MM-dd");
            ViewBag.Bitis = bitis?.ToString("yyyy-MM-dd");

            return View(ilanlar);
        }







        // GET: TinyHouse/Duzenle/5
        public ActionResult Duzenle(int id)
        {
            if (Session["RolID"] == null || Session["RolID"].ToString() != "2")
                return RedirectToAction("Giris", "Kullanici");

            TinyHouse ev = null;

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM TinyHouses WHERE EvID = @ID AND SahipID = @SahipID", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                cmd.Parameters.AddWithValue("@SahipID", Convert.ToInt32(Session["KullaniciID"]));

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    ev = new TinyHouse
                    {
                        EvID = id,
                        Baslik = dr["Baslik"].ToString(),
                        Aciklama = dr["Aciklama"].ToString(),
                        Konum = dr["Konum"].ToString(),
                        Fiyat = Convert.ToDecimal(dr["Fiyat"]),
                        UygunlukBaslangic = Convert.ToDateTime(dr["UygunlukBaslangic"]),
                        UygunlukBitis = Convert.ToDateTime(dr["UygunlukBitis"]),
                        Durum = Convert.ToBoolean(dr["Durum"])
                    };
                }
            }

            if (ev == null)
                return HttpNotFound();

            return View(ev);
        }

        // POST: TinyHouse/Duzenle
        [HttpPost]
        public ActionResult Duzenle(TinyHouse model)
        {
            if (Session["RolID"] == null || Session["RolID"].ToString() != "2")
                return RedirectToAction("Giris", "Kullanici");

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand(@"
            UPDATE TinyHouses SET 
                Baslik = @Baslik,
                Aciklama = @Aciklama,
                Konum = @Konum,
                Fiyat = @Fiyat,
                UygunlukBaslangic = @Baslangic,
                UygunlukBitis = @Bitis,
                Durum = @Durum
            WHERE EvID = @ID AND SahipID = @SahipID", conn);

                cmd.Parameters.AddWithValue("@Baslik", model.Baslik);
                cmd.Parameters.AddWithValue("@Aciklama", model.Aciklama);
                cmd.Parameters.AddWithValue("@Konum", model.Konum);
                cmd.Parameters.AddWithValue("@Fiyat", model.Fiyat);
                cmd.Parameters.AddWithValue("@Baslangic", model.UygunlukBaslangic);
                cmd.Parameters.AddWithValue("@Bitis", model.UygunlukBitis);
                cmd.Parameters.AddWithValue("@Durum", model.Durum);
                cmd.Parameters.AddWithValue("@ID", model.EvID);
                cmd.Parameters.AddWithValue("@SahipID", Convert.ToInt32(Session["KullaniciID"]));

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Listele");
        }




        [HttpPost]
        public ActionResult Sil(int id)
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "2")
                return RedirectToAction("Giris", "Kullanici");

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                conn.Open();

                // Rezervasyon varsa önce onları sil
                SqlCommand rezervSil = new SqlCommand("DELETE FROM Rezervasyonlar WHERE EvID = @ID", conn);
                rezervSil.Parameters.AddWithValue("@ID", id);
                rezervSil.ExecuteNonQuery();

                // Görselleri sil
                SqlCommand gorselSil = new SqlCommand("DELETE FROM EvGorselleri WHERE EvID = @ID", conn);
                gorselSil.Parameters.AddWithValue("@ID", id);
                gorselSil.ExecuteNonQuery();

                // Evi sil
                SqlCommand cmd = new SqlCommand("DELETE FROM TinyHouses WHERE EvID = @ID AND SahipID = @SahipID", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                cmd.Parameters.AddWithValue("@SahipID", Convert.ToInt32(Session["KullaniciID"]));
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Listele");
        }






        public ActionResult Detay(int id)
        {
            TinyHouse ilan = null;
            List<string> gorseller = new List<string>();
            List<Yorum> yorumlar = new List<Yorum>();

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM TinyHouses WHERE EvID = @ID", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    ilan = new TinyHouse
                    {
                        EvID = id,
                        Baslik = dr["Baslik"].ToString(),
                        Aciklama = dr["Aciklama"].ToString(),
                        Konum = dr["Konum"].ToString(),
                        Fiyat = Convert.ToDecimal(dr["Fiyat"]),
                        UygunlukBaslangic = Convert.ToDateTime(dr["UygunlukBaslangic"]),
                        UygunlukBitis = Convert.ToDateTime(dr["UygunlukBitis"]),
                        Durum = Convert.ToBoolean(dr["Durum"])
                    };
                }
                dr.Close();

                // Görselleri çek
                SqlCommand cmdG = new SqlCommand("SELECT GorselYolu FROM EvGorselleri WHERE EvID = @ID", conn);
                cmdG.Parameters.AddWithValue("@ID", id);
                dr = cmdG.ExecuteReader();
                while (dr.Read())
                {
                    gorseller.Add(dr["GorselYolu"].ToString());
                }
                dr.Close();

                // Yorumları çek
                SqlCommand cmdY = new SqlCommand(@"
            SELECT y.*, k.Ad + ' ' + k.Soyad AS KiraciAdSoyad
            FROM Yorumlar y
            INNER JOIN Kullanicilar k ON y.KiraciID = k.KullaniciID
            WHERE y.EvID = @ID
            ORDER BY y.YorumTarihi DESC", conn);
                cmdY.Parameters.AddWithValue("@ID", id);
                dr = cmdY.ExecuteReader();
                while (dr.Read())
                {
                    yorumlar.Add(new Yorum
                    {
                        YorumID = Convert.ToInt32(dr["YorumID"]),
                        YorumMetni = dr["Yorum"].ToString(),
                        Cevap = dr["Cevap"]?.ToString(),
                        YorumTarihi = Convert.ToDateTime(dr["YorumTarihi"]),
                        CevapTarihi = dr["CevapTarihi"] != DBNull.Value ? Convert.ToDateTime(dr["CevapTarihi"]) : (DateTime?)null,
                        Puan = Convert.ToInt32(dr["Puan"]),
                        KiraciAdSoyad = dr["KiraciAdSoyad"].ToString()
                    });
                }
            }

            ViewBag.Gorseller = gorseller;
            ViewBag.Yorumlar = yorumlar;
            return View(ilan);
        }




    }
}