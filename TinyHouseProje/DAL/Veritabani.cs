using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Configuration;

namespace TinyHouseProje.DAL
{
    public class Veritabani
    {
        public static SqlConnection BaglantiGetir()
        {
            string connStr = ConfigurationManager.ConnectionStrings["VeritabaniBaglantisi"].ConnectionString;
            SqlConnection baglanti = new SqlConnection(connStr);
            return baglanti;
        }

    }
}