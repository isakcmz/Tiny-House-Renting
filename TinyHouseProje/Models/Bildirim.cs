using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TinyHouseProje.Models
{
    public class Bildirim
    {
        public int BildirimID { get; set; }
        public int HedefKullaniciID { get; set; }
        public string Mesaj { get; set; }
        public DateTime Tarih { get; set; }
        public bool Okundu { get; set; }
    }
}