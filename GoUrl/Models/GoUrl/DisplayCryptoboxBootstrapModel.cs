using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Gourl.Models.GoUrl
{
    public class DisplayCryptoboxBootstrapModel
    {
        public DisplayCryptoboxModel CryptoboxModel { get; set; }
        public string JsonUrl { get; set; }
        public bool IsConfirmed { get; set; }
        public JObject JsonValues { get; set; }
        public string Method { get; set; }
    }
}