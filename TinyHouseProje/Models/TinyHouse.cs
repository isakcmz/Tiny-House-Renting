using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TinyHouseProje.Models
{
    public class TinyHouse
    {
        public int EvID { get; set; }
        public int SahipID { get; set; }
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
        public string Konum { get; set; }
        public decimal Fiyat { get; set; }
        public DateTime? UygunlukBaslangic { get; set; }
        public DateTime? UygunlukBitis { get; set; }
        public bool Durum { get; set; }
        public string GorselYolu { get; set; } // İlk görsel
        public string SahipAdSoyad { get; set; }
    }
}