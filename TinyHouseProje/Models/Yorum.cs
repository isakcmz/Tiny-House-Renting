using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TinyHouseProje.Models
{
    public class Yorum
    {
        public int YorumID { get; set; }
        public int RezervasyonID { get; set; }
        public int EvID { get; set; }
        public int KiraciID { get; set; }
        public int Puan { get; set; }
        public string YorumMetni { get; set; }
        public string Cevap { get; set; }
        public DateTime YorumTarihi { get; set; }
        public DateTime? CevapTarihi { get; set; }

        // View'de kullanmak için
        public string KiraciAdSoyad { get; set; }
        public string EvBaslik { get; set; }
    }
}