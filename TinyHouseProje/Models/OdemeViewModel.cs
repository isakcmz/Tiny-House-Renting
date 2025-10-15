using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TinyHouseProje.Models
{
    public class OdemeViewModel
    {
        public int OdemeID { get; set; }
        public string EvBasligi { get; set; }
        public string KiraciAdi { get; set; }
        public DateTime Baslangic { get; set; }
        public DateTime Bitis { get; set; }
        public decimal Tutar { get; set; }
        public DateTime OdemeTarihi { get; set; }
    }
}