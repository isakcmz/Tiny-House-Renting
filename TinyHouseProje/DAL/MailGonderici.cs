using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Net.Mail;

namespace TinyHouseProje.DAL
{
    public class MailGonderici
    {
        public static void Gonder(string aliciEmail, string konu, string icerik)
        {
            var smtpEmail = ConfigurationManager.AppSettings["SmtpEmail"];
            var smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(smtpEmail, "Tiny House Sistemi");
            mail.To.Add(aliciEmail);
            mail.Subject = konu;
            mail.Body = icerik;
            mail.IsBodyHtml = true;

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.Credentials = new NetworkCredential(smtpEmail, smtpPassword);
            smtp.EnableSsl = true;
            smtp.Send(mail);
        }
    }
}