using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using TinyHouseProje.Models;

namespace TinyHouseProje.DAL
{
    public class RezervasyonDAL
    {
        public static bool RezervasyonDurumuGuncelle(int rezervasyonID, bool onay)
        {
            using (SqlConnection conn = Veritabani.BaglantiGetir())
            {
                SqlCommand cmd = new SqlCommand("RezervasyonDurumuGuncelle", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@RezervasyonID", rezervasyonID);
                cmd.Parameters.AddWithValue("@OnayDurumu", onay);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}