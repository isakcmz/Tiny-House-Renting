using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Web.Mvc;
using TinyHouseProje.Models;
using TinyHouseProje.DAL;
using OfficeOpenXml;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;


namespace TinyHouseProje.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin/Kullanicilar
        public ActionResult Kullanicilar(string arama = "")
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "1")
                return RedirectToAction("Giris", "Kullanici");

            List<Kullanici> kullanicilar = new List<Kullanici>();

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                string query = @"SELECT * FROM Kullanicilar 
                         WHERE Ad + ' ' + Soyad + ' ' + Eposta LIKE @arama";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@arama", "%" + arama + "%");

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    kullanicilar.Add(new Kullanici
                    {
                        KullaniciID = Convert.ToInt32(dr["KullaniciID"]),
                        Ad = dr["Ad"].ToString(),
                        Soyad = dr["Soyad"].ToString(),
                        Eposta = dr["Eposta"].ToString(),
                        RolID = Convert.ToInt32(dr["RolID"]),
                        Aktif = Convert.ToBoolean(dr["Aktif"]),
                        KayitTarihi = Convert.ToDateTime(dr["KayitTarihi"])
                    });
                }
            }

            ViewBag.Arama = arama;
            return View(kullanicilar);
        }

        // Kullanıcıyı pasif yap
        public ActionResult PasifYap(int id)
        {
            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand("UPDATE Kullanicilar SET Aktif = 0 WHERE KullaniciID = @ID", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Kullanicilar");
        }

        public ActionResult AktifYap(int id)
        {
            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand("UPDATE Kullanicilar SET Aktif = 1 WHERE KullaniciID = @ID", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Kullanicilar");
        }

        // Kullanıcıyı sil
        public ActionResult Sil(int id)
        {
            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand("DELETE FROM Kullanicilar WHERE KullaniciID = @ID", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Kullanicilar");
        }



        public ActionResult Dashboard()
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "1")
                return RedirectToAction("Giris", "Kullanici");

            int toplamKullanici = 0;
            int toplamIlan = 0;
            int toplamRezervasyon = 0;
            decimal toplamGelir = 0;
            decimal tahminiAylikGelir = 0;

            int adminSayisi = 0;
            int evSahibiSayisi = 0;
            int kiraciSayisi = 0;
            int yeniUyeSayisi = 0;

            int aktifIlanSayisi = 0;
            int pasifIlanSayisi = 0;

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                conn.Open();

                SqlCommand cmd1 = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar", conn);
                toplamKullanici = (int)cmd1.ExecuteScalar();

                SqlCommand cmd2 = new SqlCommand("SELECT COUNT(*) FROM TinyHouses WHERE Durum = 1", conn);
                toplamIlan = (int)cmd2.ExecuteScalar();

                SqlCommand cmd3 = new SqlCommand("SELECT COUNT(*) FROM Rezervasyonlar", conn);
                toplamRezervasyon = (int)cmd3.ExecuteScalar();

                SqlCommand cmd4 = new SqlCommand("SELECT ISNULL(SUM(Tutar), 0) FROM Odemeler", conn);
                toplamGelir = Convert.ToDecimal(cmd4.ExecuteScalar());

                SqlCommand cmd5 = new SqlCommand(@"
            SELECT 
                SUM(CASE WHEN RolID = 1 THEN 1 ELSE 0 END) AS Admin,
                SUM(CASE WHEN RolID = 2 THEN 1 ELSE 0 END) AS EvSahibi,
                SUM(CASE WHEN RolID = 3 THEN 1 ELSE 0 END) AS Kiraci
            FROM Kullanicilar", conn);

                SqlDataReader dr = cmd5.ExecuteReader();
                if (dr.Read())
                {
                    adminSayisi = Convert.ToInt32(dr["Admin"]);
                    evSahibiSayisi = Convert.ToInt32(dr["EvSahibi"]);
                    kiraciSayisi = Convert.ToInt32(dr["Kiraci"]);
                }
                dr.Close();

                // ✅ Yeni üye sayısı (son 7 gün)
                DateTime yediGunOnce = DateTime.Now.AddDays(-7);
                SqlCommand cmd6 = new SqlCommand("SELECT COUNT(*) FROM Kullanicilar WHERE KayitTarihi >= @Tarih", conn);
                cmd6.Parameters.AddWithValue("@Tarih", yediGunOnce);
                yeniUyeSayisi = (int)cmd6.ExecuteScalar();


                // Aktif/pasif ilan sayısı
                SqlCommand cmd7 = new SqlCommand(@"
            SELECT 
                SUM(CASE WHEN Durum = 1 THEN 1 ELSE 0 END) AS Aktif,
                SUM(CASE WHEN Durum = 0 THEN 1 ELSE 0 END) AS Pasif
            FROM TinyHouses", conn);
                SqlDataReader dr2 = cmd7.ExecuteReader();
                if (dr2.Read())
                {
                    aktifIlanSayisi = Convert.ToInt32(dr2["Aktif"]);
                    pasifIlanSayisi = Convert.ToInt32(dr2["Pasif"]);
                }
                dr2.Close();



                SqlCommand cmdTahmin = new SqlCommand(@"
                        SELECT ISNULL(AVG(dbo.AylikGelirTahmini(Fiyat, 60)), 0)
                        FROM TinyHouses
                        WHERE Durum = 1", conn);

                tahminiAylikGelir = Convert.ToDecimal(cmdTahmin.ExecuteScalar());

            }

            ViewBag.ToplamKullanici = toplamKullanici;
            ViewBag.ToplamIlan = toplamIlan;
            ViewBag.ToplamRezervasyon = toplamRezervasyon;
            ViewBag.ToplamGelir = toplamGelir;
            ViewBag.TahminiGelir = tahminiAylikGelir;

            ViewBag.AdminSayisi = adminSayisi;
            ViewBag.EvSahibiSayisi = evSahibiSayisi;
            ViewBag.KiraciSayisi = kiraciSayisi;

            ViewBag.YeniUye = yeniUyeSayisi;

            ViewBag.AktifIlan = aktifIlanSayisi;
            ViewBag.PasifIlan = pasifIlanSayisi;

            return View();
        }





        public ActionResult Odemeler(DateTime? baslangic, DateTime? bitis)
        {
            if (Session["KullaniciID"] == null || Session["RolID"] == null)
                return RedirectToAction("Giris", "Kullanici");

            int kullaniciID = Convert.ToInt32(Session["KullaniciID"]);
            string rol = Session["RolID"].ToString();

            List<OdemeViewModel> odemeler = new List<OdemeViewModel>();
            decimal toplamKazanc = 0;

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd;
                string query = @"
            SELECT o.OdemeID, o.Tutar, o.OdemeTarihi,
                   t.Baslik AS EvBasligi,
                   k.Ad + ' ' + k.Soyad AS KiraciAdi,
                   r.BaslangicTarihi, r.BitisTarihi
            FROM Odemeler o
            INNER JOIN Rezervasyonlar r ON o.RezervasyonID = r.RezervasyonID
            INNER JOIN TinyHouses t ON r.EvID = t.EvID
            INNER JOIN Kullanicilar k ON r.KiraciID = k.KullaniciID
            WHERE 1 = 1
        ";

                if (rol == "2")
                {
                    query += " AND t.SahipID = @SahipID";
                }

                if (baslangic.HasValue)
                {
                    query += " AND o.OdemeTarihi >= @Baslangic";
                }

                if (bitis.HasValue)
                {
                    query += " AND o.OdemeTarihi <= @Bitis";
                }

                cmd = new SqlCommand(query, conn);

                if (rol == "2")
                    cmd.Parameters.AddWithValue("@SahipID", kullaniciID);
                if (baslangic.HasValue)
                    cmd.Parameters.AddWithValue("@Baslangic", baslangic.Value);
                if (bitis.HasValue)
                    cmd.Parameters.AddWithValue("@Bitis", bitis.Value);

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    var odeme = new OdemeViewModel
                    {
                        OdemeID = Convert.ToInt32(dr["OdemeID"]),
                        Tutar = Convert.ToDecimal(dr["Tutar"]),
                        OdemeTarihi = Convert.ToDateTime(dr["OdemeTarihi"]),
                        EvBasligi = dr["EvBasligi"].ToString(),
                        KiraciAdi = dr["KiraciAdi"].ToString(),
                        Baslangic = Convert.ToDateTime(dr["BaslangicTarihi"]),
                        Bitis = Convert.ToDateTime(dr["BitisTarihi"])
                    };

                    odemeler.Add(odeme);
                    toplamKazanc += odeme.Tutar;
                }

                dr.Close();
            }

            ViewBag.ToplamKazanc = toplamKazanc;
            ViewBag.Baslangic = baslangic?.ToString("yyyy-MM-dd");
            ViewBag.Bitis = bitis?.ToString("yyyy-MM-dd");

            return View(odemeler);
        }






        public ActionResult Rezervasyonlar()
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "1")
                return RedirectToAction("Giris", "Kullanici");

            List<Tuple<Rezervasyon, string, string>> liste = new List<Tuple<Rezervasyon, string, string>>();

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT r.*, k.Ad + ' ' + k.Soyad AS KiraciAd, e.Baslik AS EvBaslik
            FROM Rezervasyonlar r
            INNER JOIN Kullanicilar k ON r.KiraciID = k.KullaniciID
            INNER JOIN TinyHouses e ON r.EvID = e.EvID", conn);

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

                    string kiraci = dr["KiraciAd"].ToString();
                    string ev = dr["EvBaslik"].ToString();

                    liste.Add(new Tuple<Rezervasyon, string, string>(r, kiraci, ev));
                }
            }

            return View(liste);
        }


        public ActionResult RezervasyonIptal(int id)
        {
            if (Session["KullaniciID"] == null || Session["RolID"].ToString() != "1")
                return RedirectToAction("Giris", "Kullanici");

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                conn.Open();

                // Ödemeyi sil
                SqlCommand odemeSil = new SqlCommand("DELETE FROM Odemeler WHERE RezervasyonID = @ID", conn);
                odemeSil.Parameters.AddWithValue("@ID", id);
                odemeSil.ExecuteNonQuery();

                // Rezervasyonu sil
                SqlCommand rezervSil = new SqlCommand("DELETE FROM Rezervasyonlar WHERE RezervasyonID = @ID", conn);
                rezervSil.Parameters.AddWithValue("@ID", id);
                rezervSil.ExecuteNonQuery();
            }

            return RedirectToAction("Rezervasyonlar");
        }




        public ActionResult Ilanlar()
        {
            List<TinyHouse> ilanlar = new List<TinyHouse>();

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand(@"
            SELECT e.*, k.Ad + ' ' + k.Soyad AS SahipAdSoyad 
            FROM TinyHouses e
            INNER JOIN Kullanicilar k ON e.SahipID = k.KullaniciID", conn);

                conn.Open();
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
                        Aciklama = dr["Aciklama"].ToString(),
                        SahipAdSoyad = dr["SahipAdSoyad"].ToString()
                    });
                }
            }

            return View(ilanlar);
        }

        public ActionResult IlanSil(int id)
        {
            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand("UPDATE TinyHouses SET Durum = 0 WHERE EvID = @ID", conn);
                cmd.Parameters.AddWithValue("@ID", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Ilanlar", "Admin");
        }






        public ActionResult GelirRaporuPDF()
        {
            if (Session["KullaniciID"] == null || Session["RolID"] == null)
                return RedirectToAction("Giris", "Kullanici");

            int kullaniciID = Convert.ToInt32(Session["KullaniciID"]);
            string rol = Session["RolID"].ToString();
            List<OdemeViewModel> odemeler = GetOdemeler(kullaniciID, rol);

            MemoryStream ms = new MemoryStream();
            Document doc = new Document();
            PdfWriter.GetInstance(doc, ms).CloseStream = false;
            doc.Open();

            doc.Add(new Paragraph("Gelir Raporu"));
            doc.Add(new Paragraph("Tarih: " + DateTime.Now.ToShortDateString()));
            doc.Add(new Paragraph("\n"));

            PdfPTable table = new PdfPTable(5);
            table.AddCell("Ev");
            table.AddCell("Kiracı");
            table.AddCell("Tarih");
            table.AddCell("Tutar");
            table.AddCell("Ödeme Tarihi");

            foreach (var odeme in odemeler)
            {
                table.AddCell(odeme.EvBasligi);
                table.AddCell(odeme.KiraciAdi);
                table.AddCell(odeme.Baslangic.ToShortDateString() + " - " + odeme.Bitis.ToShortDateString());
                table.AddCell(odeme.Tutar.ToString("N2"));
                table.AddCell(odeme.OdemeTarihi.ToString("dd.MM.yyyy"));
            }

            doc.Add(table);
            doc.Close();

            ms.Position = 0;
            return File(ms, "application/pdf", "GelirRaporu.pdf");
        }

        public ActionResult GelirRaporuExcel()
        {
            if (Session["KullaniciID"] == null || Session["RolID"] == null)
                return RedirectToAction("Giris", "Kullanici");


            OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;


            int kullaniciID = Convert.ToInt32(Session["KullaniciID"]);
            string rol = Session["RolID"].ToString();
            List<OdemeViewModel> odemeler = GetOdemeler(kullaniciID, rol);

            
            using (ExcelPackage pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Gelir Raporu");

                ws.Cells[1, 1].Value = "Ev";
                ws.Cells[1, 2].Value = "Kiracı";
                ws.Cells[1, 3].Value = "Tarih";
                ws.Cells[1, 4].Value = "Tutar";
                ws.Cells[1, 5].Value = "Ödeme Tarihi";

                int row = 2;
                foreach (var odeme in odemeler)
                {
                    ws.Cells[row, 1].Value = odeme.EvBasligi;
                    ws.Cells[row, 2].Value = odeme.KiraciAdi;
                    ws.Cells[row, 3].Value = odeme.Baslangic.ToShortDateString() + " - " + odeme.Bitis.ToShortDateString();
                    ws.Cells[row, 4].Value = odeme.Tutar;
                    ws.Cells[row, 5].Value = odeme.OdemeTarihi.ToString("dd.MM.yyyy");
                    row++;
                }

                var stream = new MemoryStream(pck.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "GelirRaporu.xlsx");
            }
        }

        private List<OdemeViewModel> GetOdemeler(int kullaniciID, string rol)
        {
            List<OdemeViewModel> list = new List<OdemeViewModel>();

            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand();
                if (rol == "1") // admin
                {
                    cmd = new SqlCommand(@"SELECT o.OdemeID, o.Tutar, o.OdemeTarihi,
                                         t.Baslik AS EvBasligi,
                                         k.Ad + ' ' + k.Soyad AS KiraciAdi,
                                         r.BaslangicTarihi, r.BitisTarihi
                                  FROM Odemeler o
                                  INNER JOIN Rezervasyonlar r ON o.RezervasyonID = r.RezervasyonID
                                  INNER JOIN TinyHouses t ON r.EvID = t.EvID
                                  INNER JOIN Kullanicilar k ON r.KiraciID = k.KullaniciID", conn);
                }
                else if (rol == "2") // ev sahibi
                {
                    cmd = new SqlCommand(@"SELECT o.OdemeID, o.Tutar, o.OdemeTarihi,
                                         t.Baslik AS EvBasligi,
                                         k.Ad + ' ' + k.Soyad AS KiraciAdi,
                                         r.BaslangicTarihi, r.BitisTarihi
                                  FROM Odemeler o
                                  INNER JOIN Rezervasyonlar r ON o.RezervasyonID = r.RezervasyonID
                                  INNER JOIN TinyHouses t ON r.EvID = t.EvID
                                  INNER JOIN Kullanicilar k ON r.KiraciID = k.KullaniciID
                                  WHERE t.SahipID = @SahipID", conn);
                    cmd.Parameters.AddWithValue("@SahipID", kullaniciID);
                }

                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new OdemeViewModel
                    {
                        OdemeID = Convert.ToInt32(dr["OdemeID"]),
                        Tutar = Convert.ToDecimal(dr["Tutar"]),
                        OdemeTarihi = Convert.ToDateTime(dr["OdemeTarihi"]),
                        EvBasligi = dr["EvBasligi"].ToString(),
                        KiraciAdi = dr["KiraciAdi"].ToString(),
                        Baslangic = Convert.ToDateTime(dr["BaslangicTarihi"]),
                        Bitis = Convert.ToDateTime(dr["BitisTarihi"])
                    });
                }
            }

            return list;
        }



    }
}