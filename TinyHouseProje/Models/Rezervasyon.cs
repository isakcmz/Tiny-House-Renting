using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace TinyHouseProje.Models
{
    public class Rezervasyon
    {
        public int RezervasyonID { get; set; }
        public int EvID { get; set; }
        public int KiraciID { get; set; }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public bool OdemeDurumu { get; set; }
        public bool OnayDurumu { get; set; }



        [NotMapped]
        public string KartSahibi { get; set; }

        [NotMapped]
        public string KartNo { get; set; }

        [NotMapped]
        public string SonKullanmaTarihi { get; set; }

        [NotMapped]
        public string CVC { get; set; }
    }
}