using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TinyHouseProje.Models
{
    public class Odeme
    {
        public int OdemeID { get; set; }
        public int RezervasyonID { get; set; }
        public DateTime OdemeTarihi { get; set; }
        public decimal Tutar { get; set; }
        public string OdemeSekli { get; set; }
    }
}