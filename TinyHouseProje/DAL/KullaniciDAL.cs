using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TinyHouseProje.Models;

namespace TinyHouseProje.DAL
{
    public class KullaniciDAL
    {
        public static bool KullaniciEkle(Kullanici kullanici)
        {
            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand("KullaniciEkle", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Ad", kullanici.Ad);
                cmd.Parameters.AddWithValue("@Soyad", kullanici.Soyad);
                cmd.Parameters.AddWithValue("@Eposta", kullanici.Eposta);
                cmd.Parameters.AddWithValue("@Sifre", kullanici.Sifre);
                cmd.Parameters.AddWithValue("@RolID", kullanici.RolID);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch (SqlException ex)
                {
                    // Hata mesajını geçici olarak ViewBag ile gönder
                    throw new Exception("SQL Hatası: " + ex.Message);
                }
            }
        }
    }
}