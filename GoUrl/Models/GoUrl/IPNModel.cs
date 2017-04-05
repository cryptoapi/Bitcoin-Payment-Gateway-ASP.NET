using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using Gourl.GoUrlCore;

namespace Gourl.Models.GoUrl
{
    public class StatusCheckAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (string.IsNullOrEmpty(value as string))
                return false;
            return value.ToString() == "payment_received" || value.ToString() == "payment_received_unrecognised";
        }
    }
    public class PrivateKeyConfirmAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            string[] ss = ConfigurationManager.AppSettings["PrivateKeys"].Split(new char[] {',', ' '},StringSplitOptions.RemoveEmptyEntries);
            List<string> s = new List<string>();
            foreach (string s1 in ss)
            {
                s.Add(Calculator.md512(s1));
            }
            return s.Contains(value);
        }
    }
    public class IPNModel
    {
        public IPNModel()
        {
            order = "";
        }

        [StatusCheck]
        [Required]
        public string status { get; set; }
        public string err { get; set; }

        [StringLength(128)]
        [RegularExpression(@"^[a-zA-Z0-9]+$")]
        [Required]
        [PrivateKeyConfirm]
        public string private_key_hash { get; set; }

        [Range(1, Int32.MaxValue)]
        [Required]
        public int box { get; set; }
        public string boxtype { get; set; }
        public string order { get; set; }
        public string user { get; set; }
        public string usercountry { get; set; }
        [Required]
        public decimal amount { get; set; }
        [Required]
        public decimal amountusd { get; set; }
        public string coinlabel { get; set; }
        public string coinname { get; set; }
        public string addr { get; set; }
        public string tx { get; set; }
        public byte confirmed { get; set; }
        public long timestamp { get; set; }
        public DateTime date { get; set; }
        public DateTime datetime { get; set; }

    }
}