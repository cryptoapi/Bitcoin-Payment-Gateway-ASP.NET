using System;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using Gourl.Models.GoUrl;


//* ########################################## 	

//* ###  PLEASE MODIFY THIS FILE
//* ###
//* ###  Add additional actions after a payment has been received -
//* ###  update database records, send email to user, etc

//* ##########################################

namespace Gourl.GoUrlCore
{
    public static class NewPayment
    {
        public static void Main(int paymentId, IPNModel callback, string box_status)
        {
            //PLACE YOUR CODE HERE

            //Mail(paymentId, callback, box_status);
        }

        private static void Mail(int paymentId, IPNModel callback, string box_status)
        {
            const string host = "";
            const string fromEmail = "";
            const string fromPassword = "";
            const string toEmail = "";

            if (host == "" || fromEmail == "" || fromPassword == "" || toEmail == "")
            {
                throw new ArgumentException("Please initialise smtp server for email.");
            }

            var fromAddress = new MailAddress(fromEmail, "From Test Payment Server");
            var toAddress = new MailAddress(toEmail, "To You");
            if (callback.err == null)
                callback.err = "";
            string details = Json.Encode(callback);
            Regex reg = new Regex(@"\\/Date\(\d+\)\\/");
            details = reg.Replace(details, callback.date.ToString("dd MMMM yyyy"), 1);
            details = reg.Replace(details, callback.datetime.ToString("yyyy-MM-dd HH:mm:ss"));
            const string subject = "Subject";
            string body = "Payment - " + paymentId + " - " + box_status + "\nDetails:\n" + details;

            var smtp = new SmtpClient
            {
                Host = host,
                Port = 25,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword),
                Timeout = 10000
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
        }
    }
}