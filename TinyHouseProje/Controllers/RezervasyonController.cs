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
    public class RezervasyonController : Controller
    {
        // GET: Rezervasyon
        public ActionResult Rezervasyon(int id)
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "3")
                return RedirectToAction("Giris", "Kullanici");

            ViewBag.EvID = id;
            return View(new Rezervasyon { EvID = id });
        }

        // POST: Rezervasyon
        [HttpPost]
        public ActionResult Rezervasyon(Rezervasyon model)
        {
            try
            {
                if (model.EvID == 0)
                {
                    ViewBag.Hata = "HATALI İSTEK: EvID alınamadı.";
                    return View(model);
                }

                model.KiraciID = Convert.ToInt32(Session["KullaniciID"]);
                model.OdemeDurumu = true;
                model.OnayDurumu = false;

                int rezervasyonID = 0;

                using (SqlConnection conn = Veritabani.BaglantiGetir())
                {
                    SqlCommand kontrolCmd = new SqlCommand(@"
                        SELECT COUNT(*) 
                        FROM Rezervasyonlar 
                        WHERE EvID = @EvID 
                        AND OnayDurumu = 1 
                        AND (
                            (@Baslangic BETWEEN BaslangicTarihi AND BitisTarihi) OR
                            (@Bitis BETWEEN BaslangicTarihi AND BitisTarihi) OR
                            (BaslangicTarihi BETWEEN @Baslangic AND @Bitis)
                        )", conn);

                    kontrolCmd.Parameters.AddWithValue("@EvID", model.EvID);
                    kontrolCmd.Parameters.AddWithValue("@Baslangic", model.BaslangicTarihi);
                    kontrolCmd.Parameters.AddWithValue("@Bitis", model.BitisTarihi);

                    conn.Open();
                    int cakisan = (int)kontrolCmd.ExecuteScalar();
                    conn.Close();

                    if (cakisan > 0)
                    {
                        ViewBag.Hata = "Bu tarihlerde bu ev zaten rezerve edilmiş.";
                        return View(model);
                    }
                }

                using (SqlConnection conn = Veritabani.BaglantiGetir())
                {
                    SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO Rezervasyonlar 
                        (EvID, KiraciID, BaslangicTarihi, BitisTarihi, OdemeDurumu, OnayDurumu) 
                        OUTPUT INSERTED.RezervasyonID
                        VALUES (@EvID, @KiraciID, @Baslangic, @Bitis, 1, 0)", conn);

                    cmd.Parameters.AddWithValue("@EvID", model.EvID);
                    cmd.Parameters.AddWithValue("@KiraciID", model.KiraciID);
                    cmd.Parameters.AddWithValue("@Baslangic", model.BaslangicTarihi);
                    cmd.Parameters.AddWithValue("@Bitis", model.BitisTarihi);

                    conn.Open();
                    rezervasyonID = (int)cmd.ExecuteScalar();
                }

                decimal tutar = 0;
                using (SqlConnection conn = Veritabani.BaglantiGetir())
                {
                    SqlCommand cmd = new SqlCommand("SELECT dbo.UcretHesapla(@EvID, @Baslangic, @Bitis)", conn);
                    cmd.Parameters.AddWithValue("@EvID", model.EvID);
                    cmd.Parameters.AddWithValue("@Baslangic", model.BaslangicTarihi);
                    cmd.Parameters.AddWithValue("@Bitis", model.BitisTarihi);

                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                        tutar = Convert.ToDecimal(result);
                }

                using (SqlConnection conn = Veritabani.BaglantiGetir())
                {
                    SqlCommand check = new SqlCommand("SELECT COUNT(*) FROM Odemeler WHERE RezervasyonID = @ID", conn);
                    check.Parameters.AddWithValue("@ID", rezervasyonID);
                    conn.Open();
                    int varMi = (int)check.ExecuteScalar();

                    if (varMi == 0)
                    {
                        SqlCommand odemeCmd = new SqlCommand(@"
                            INSERT INTO Odemeler (RezervasyonID, Tutar, OdemeTarihi)
                            VALUES (@ID, @Tutar, GETDATE())", conn);

                        odemeCmd.Parameters.AddWithValue("@ID", rezervasyonID);
                        odemeCmd.Parameters.AddWithValue("@Tutar", tutar);
                        odemeCmd.ExecuteNonQuery();
                    }
                }

                ViewBag.Mesaj = $"Rezervasyon ve ödeme başarıyla tamamlandı. Toplam Tutar: {tutar:C2}";
            }
            catch (Exception ex)
            {
                ViewBag.Hata = "HATA: " + ex.Message;
            }

            return View(model);
        }





        public ActionResult GelenTalepler()
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "2")
                return RedirectToAction("Giris", "Kullanici");

            List<Tuple<Rezervasyon, string, string>> talepler = new List<Tuple<Rezervasyon, string, string>>();
            int evSahibiID = Convert.ToInt32(Session["KullaniciID"]);

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT r.*, k.Ad, k.Soyad, e.Baslik
            FROM Rezervasyonlar r
            INNER JOIN TinyHouses e ON r.EvID = e.EvID
            INNER JOIN Kullanicilar k ON r.KiraciID = k.KullaniciID
            WHERE e.SahipID = @SahipID", conn);

                cmd.Parameters.AddWithValue("@SahipID", evSahibiID);

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    Rezervasyon rezerv = new Rezervasyon
                    {
                        RezervasyonID = Convert.ToInt32(dr["RezervasyonID"]),
                        EvID = Convert.ToInt32(dr["EvID"]),
                        KiraciID = Convert.ToInt32(dr["KiraciID"]),
                        BaslangicTarihi = Convert.ToDateTime(dr["BaslangicTarihi"]),
                        BitisTarihi = Convert.ToDateTime(dr["BitisTarihi"]),
                        OdemeDurumu = Convert.ToBoolean(dr["OdemeDurumu"]),
                        OnayDurumu = Convert.ToBoolean(dr["OnayDurumu"])
                    };

                    string kiraciAdi = dr["Ad"].ToString() + " " + dr["Soyad"].ToString();
                    string evBaslik = dr["Baslik"].ToString();
                    talepler.Add(new Tuple<Rezervasyon, string, string>(rezerv, kiraciAdi, evBaslik));
                }
            }

            return View(talepler);
        }



        public ActionResult DurumGuncelle(int id, int durum)
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "2")
                return RedirectToAction("Giris", "Kullanici");

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                conn.Open();

                // 1. OnayDurumu güncelle
                SqlCommand cmd = new SqlCommand("UPDATE Rezervasyonlar SET OnayDurumu = @Durum WHERE RezervasyonID = @ID", conn);
                cmd.Parameters.AddWithValue("@Durum", durum);
                cmd.Parameters.AddWithValue("@ID", id);
                cmd.ExecuteNonQuery();

                // 2. Onaylandıysa devam et
                if (durum == 1)
                {
                    // Kiracının e-posta ve tarih bilgileri
                    SqlCommand cmd2 = new SqlCommand(@"
                SELECT k.Eposta, r.BaslangicTarihi, r.BitisTarihi
                FROM Rezervasyonlar r
                INNER JOIN Kullanicilar k ON r.KiraciID = k.KullaniciID
                WHERE r.RezervasyonID = @ID", conn);
                    cmd2.Parameters.AddWithValue("@ID", id);

                    string alici = null;
                    DateTime bas = DateTime.Now;
                    DateTime bit = DateTime.Now;

                    SqlDataReader dr = cmd2.ExecuteReader();
                    if (dr.Read())
                    {
                        alici = dr["Eposta"].ToString();
                        bas = Convert.ToDateTime(dr["BaslangicTarihi"]);
                        bit = Convert.ToDateTime(dr["BitisTarihi"]);
                    }
                    dr.Close();

                    // E-posta gönder
                    if (!string.IsNullOrEmpty(alici))
                    {
                        string konu = "Rezervasyonunuz Onaylandı ✔";
                        string icerik = $@"
                    <h3>Merhaba,</h3>
                    <p>Rezervasyonunuz <b>{bas:dd.MM.yyyy} - {bit:dd.MM.yyyy}</b> tarihleri arasında onaylandı.</p>
                    <p>İyi tatiller dileriz! 🏡</p>";

                        MailGonderici.Gonder(alici, konu, icerik);
                    }

                    // Ödeme güncelle (var olan satırı güncelle)
                    SqlCommand guncelleOdeme = new SqlCommand(@"
                UPDATE Odemeler 
                SET OdemeSekli = 'Online (otomatik)'
                WHERE RezervasyonID = @ID", conn);
                    guncelleOdeme.Parameters.AddWithValue("@ID", id);
                    guncelleOdeme.ExecuteNonQuery();
                }
            }

            return RedirectToAction("GelenTalepler");
        }












        public ActionResult BenimRezervasyonlarim()
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "3")
                return RedirectToAction("Giris", "Kullanici");

            List<Tuple<Rezervasyon, string>> rezervasyonlar = new List<Tuple<Rezervasyon, string>>();
            int kiraciID = Convert.ToInt32(Session["KullaniciID"]);

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT r.*, e.Baslik
            FROM Rezervasyonlar r
            INNER JOIN TinyHouses e ON r.EvID = e.EvID
            WHERE r.KiraciID = @KiraciID", conn);

                cmd.Parameters.AddWithValue("@KiraciID", kiraciID);

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    Rezervasyon r = new Rezervasyon
                    {
                        RezervasyonID = Convert.ToInt32(dr["RezervasyonID"]),
                        EvID = Convert.ToInt32(dr["EvID"]),
                        KiraciID = Convert.ToInt32(dr["KiraciID"]),
                        BaslangicTarihi = Convert.ToDateTime(dr["BaslangicTarihi"]),
                        BitisTarihi = Convert.ToDateTime(dr["BitisTarihi"]),
                        OdemeDurumu = Convert.ToBoolean(dr["OdemeDurumu"]),
                        OnayDurumu = Convert.ToBoolean(dr["OnayDurumu"])
                    };

                    string evBaslik = dr["Baslik"].ToString();
                    rezervasyonlar.Add(new Tuple<Rezervasyon, string>(r, evBaslik));
                }
            }

            return View(rezervasyonlar);
        }





        public ActionResult Iptal(int id)
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "3")
                return RedirectToAction("Giris", "Kullanici");

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                conn.Open();

                // Önce ödemeyi sil
                SqlCommand odemeSil = new SqlCommand("DELETE FROM Odemeler WHERE RezervasyonID = @ID", conn);
                odemeSil.Parameters.AddWithValue("@ID", id);
                odemeSil.ExecuteNonQuery();

                // Sonra rezervasyonu sil
                SqlCommand cmd = new SqlCommand(@"
            DELETE FROM Rezervasyonlar 
            WHERE RezervasyonID = @ID AND KiraciID = @KiraciID AND OnayDurumu = 0", conn);

                cmd.Parameters.AddWithValue("@ID", id);
                cmd.Parameters.AddWithValue("@KiraciID", Convert.ToInt32(Session["KullaniciID"]));
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("BenimRezervasyonlarim");
        }




        public ActionResult Takvim()
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "2")
                return RedirectToAction("Giris", "Kullanici");

            List<Tuple<Rezervasyon, string>> rezervasyonlar = new List<Tuple<Rezervasyon, string>>();
            int sahipID = Convert.ToInt32(Session["KullaniciID"]);

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT r.*, e.Baslik
            FROM Rezervasyonlar r
            INNER JOIN TinyHouses e ON r.EvID = e.EvID
            WHERE e.SahipID = @SahipID AND r.OnayDurumu = 1", conn);

                cmd.Parameters.AddWithValue("@SahipID", sahipID);
                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    var rezervasyon = new Rezervasyon
                    {
                        RezervasyonID = Convert.ToInt32(dr["RezervasyonID"]),
                        EvID = Convert.ToInt32(dr["EvID"]),
                        KiraciID = Convert.ToInt32(dr["KiraciID"]),
                        BaslangicTarihi = Convert.ToDateTime(dr["BaslangicTarihi"]),
                        BitisTarihi = Convert.ToDateTime(dr["BitisTarihi"]),
                        OdemeDurumu = Convert.ToBoolean(dr["OdemeDurumu"]),
                        OnayDurumu = Convert.ToBoolean(dr["OnayDurumu"])
                    };

                    string baslik = dr["Baslik"].ToString();
                    rezervasyonlar.Add(new Tuple<Rezervasyon, string>(rezervasyon, baslik));
                }
            }

            return View(rezervasyonlar);
        }




    }
}