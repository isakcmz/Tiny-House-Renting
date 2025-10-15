using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TinyHouseProje.Models
{
    public class Kullanici
    {
        public int KullaniciID { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string Eposta { get; set; }
        public string Sifre { get; set; }
        public int RolID { get; set; }
        public DateTime KayitTarihi { get; set; }
        public bool Aktif { get; set; }
    }
}