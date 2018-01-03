using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Helpers;
using Gourl.Models;
using Gourl.Models.GoUrl;


namespace Gourl.GoUrlCore
{
    public class Cryptobox : IDisposable
    {
	private static readonly string[] CryptoboxCoins = { "bitcoin", "bitcoincash", "litecoin", "dash", "dogecoin", "speedcoin", "reddcoin", "potcoin", "feathercoin", "vertcoin", "peercoin", "monetaryunit", "universalcurrency" };

        private OptionsModel options;
        private GoUrlEntities Context = new GoUrlEntities();
        private bool already_checked = false;

        // Internal Variables
        private int boxID = 0;        // cryptobox id, the same as on gourl.io member page. For each your cryptocoin payment boxes you will have unique public / private keys 
        private string coinLabel = "";       // current cryptocoin label (BTC, DOGE, etc.) 
        private string coinName = "";       // current cryptocoin name (Bitcoin, Dogecoin, etc.) 
        private bool paid = false;  // paid or not
        private bool confirmed = false; // transaction/payment have 6+ confirmations or not
        private int? paymentID = null; // current record id in the table crypto_payments (table stores all payments from your users)
        private DateTime paymentDate = DateTime.MinValue;       // transaction/payment datetime in GMT format
        private decimal amountPaid = 0;     // exact paid amount; for example, amount = 0.5 BTC and user paid - amountPaid = 0.50002 BTC
        private decimal amountPaidUSD = 0;        // approximate paid amount in USD; using cryptocurrency exchange rate on datetime of payment
        private string boxType = "";        // cryptobox type - 'paymentbox' or 'captchabox'
        private bool processed = false;    // optional - set flag to paid & processed	
        private string cookieName = "";       // user cookie/session name (if cookies/sessions use)


        public Cryptobox(OptionsModel options)
        {
            this.options = options;
            Random rand = new Random();

            if (!Regex.IsMatch(options.public_key, "[a-zA-Z0-9]+") ||
                options.public_key.Length != 50 ||
                !options.public_key.Contains("AA") ||
                !Int32.TryParse(options.public_key.Substring(0, options.public_key.IndexOf("AA")), out this.boxID) ||
                !options.public_key.Contains("77") ||
                !options.public_key.Contains("PUB"))
            {
                throw new FormatException("Invalid Cryptocoin Payment Box PUBLIC KEY - " + (options.public_key != "" ? options.public_key : "cannot be empty"));
            }

            if (!Regex.IsMatch(options.private_key, "[a-zA-Z0-9]+") ||
                options.private_key.Length != 50 ||
                !options.private_key.Contains("AA") ||
                !Int32.TryParse(options.private_key.Substring(0, options.private_key.IndexOf("AA")), out this.boxID) ||
                !options.private_key.Contains("77") ||
                !options.private_key.Contains("PRV"))
            {
                throw new FormatException("Invalid Cryptocoin Payment Box PRIVATE KEY - " + (options.private_key != "" ? "" : "cannot be empty"));
            }

            string[] ss = ConfigurationManager.AppSettings["PrivateKeys"].Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> slist = new List<string>();
            foreach (string s1 in ss)
            {
                slist.Add(s1);
            }
            if (!slist.Contains(options.private_key))
            {
                throw new ConfigurationException("Error.Please add your Cryptobox Private Key in file Web.config");
            }

            if (!options.webdev_key.Contains("DEV") ||
                options.webdev_key.ToUpper() != options.webdev_key)
            {
                this.options.webdev_key = "";
            }

            string c = options.public_key.Substring(options.public_key.IndexOf("AA") + 7);
            c = c.Substring(0, c.IndexOf("PUB"));
            this.coinLabel = c.Substring(c.IndexOf("77") + 2);
            this.coinName = c.Substring(0, c.IndexOf("77"));

            if ((this.options.amount <= 0 && this.options.amountUSD <= 0) || (this.options.amount > 0 && this.options.amountUSD > 0))
            {
                throw new ArgumentOutOfRangeException("You can use in cryptobox options one of variable only: amount or amountUSD. You cannot place values in that two variables together (submitted amount = '" + this.options.amount + "' and amountUSD = '" + this.options.amountUSD + "' )");
            }

            if ((this.options.amount > 0 && this.options.amount < (decimal)0.0001) || this.options.amount > 50000000)
            {
                throw new ArgumentOutOfRangeException("Invalid Amount - " + this.options.amount + " " + coinLabel + ". Allowed range: 0.0001 .. 50,000,000");
            }

            if ((this.options.amountUSD > 0 && this.options.amountUSD < (decimal)0.0001) || this.options.amountUSD > 50000000)
            {
                throw new ArgumentOutOfRangeException("Invalid amountUSD - " + options.amountUSD + "USD. Allowed range: 0.01 .. 1,000,000");
            }

            this.options.period = this.options.period.Trim().ToUpper();
            if (this.options.period.EndsWith("S"))
            {
                this.options.period = this.options.period.Remove(this.options.period.Length - 1);
            }

            string[] langArr = { "en", "es", "fr", "de", "nl", "it", "ru", "pl", "pt", "fa", "ko", "ja", "id", "tr", "ar", "cn", "zh", "hi" };
            if (!langArr.Contains(this.options.language))
            {
                this.options.language = "en";
            }
            this.options.language = CryptoHelper.cryptobox_sellanguage(this.options.language);

            if (this.options.iframeID == "cryptobox_live_" ||
                (this.options.iframeID != "" && !Regex.IsMatch(this.options.iframeID, "[a-zA-Z0-9]+")))
            {
                throw new FormatException("Invalid iframe ID - " + this.options.iframeID + ". Allowed symbols: a..Z0..9_-");
            }
            if (this.options.iframeID == String.Empty)
            {
                this.options.iframeID = iframe_id();
            }

            this.options.userID = this.options.userID.Trim();
            if (this.options.userID != "" && !Regex.IsMatch(this.options.userID, @"[a-zA-Z0-9\._@-]+"))
            {
                throw new FormatException("Invalid User ID - " + this.options.userID + ".Allowed symbols: a..Z0..9_-@.");
            }
            if (this.options.userID.Length > 50)
            {
                throw new FormatException("Invalid User ID - " + this.options.userID + ". Max: 50 symbols");
            }

            this.options.orderID = this.options.orderID.Trim();
            if (this.options.orderID != "" && !Regex.IsMatch(this.options.orderID, @"[a-zA-Z0-9\._@-]+"))
            {
                throw new FormatException("Invalid Order ID - " + this.options.orderID + ". Allowed symbols: a..Z0..9_-@.");
            }
            if (this.options.orderID == null || this.options.orderID.Length > 50)
            {
                throw new FormatException("Invalid Order ID - " + this.options.orderID + ". Max: 50 symbols");
            }


            if (this.options.userID != String.Empty)
            {
                this.options.userFormat = "MANUAL";
            }
            else
            {
                this.options.userID = "";
                HttpCookieCollection cookie = HttpContext.Current.Request.Cookies;
                var session = HttpContext.Current.Session;


                switch (this.options.userFormat)
                {
                    case "COOKIE":
                        this.cookieName = "cryptoUsr" +
                                           Calculator.icrc32(this.boxID + "*&*" + this.coinLabel + "*&*" + this.options.orderID +
                                                  "*&*" + this.options.private_key);

                        if (cookie[this.cookieName] != null && cookie[this.cookieName].Value.Contains("__"))
                        {
                            this.options.userID = cookie[this.cookieName].Value;
                        }
                        else
                        {
                            string s = HttpContext.Current.Server.MachineName.ToLower().Trim('/');
                            if (s.IndexOf("wwww.") == 0)
                            {
                                s = s.Substring(4);
                            }
                            TimeSpan t = (DateTime.Now - new DateTime(1970, 1, 1));
                            long d = (long)t.TotalSeconds;
                            if (d > 1410000000)
                                d -= 1410000000;

                            string v = (d.ToString() + "__" +
                                       Calculator.md5(rand.Next().ToString() + rand.Next().ToString() +
                                                      rand.Next().ToString()).Substring(0, 10)).Trim();
                            HttpContext.Current.Response.Cookies.Add(new HttpCookie(this.cookieName, v));
                            this.options.userID = v;
                        }
                        break;
                    case "SESSION":
                        this.cookieName = "cryptoUsr" +
                                           Calculator.icrc32(this.options.private_key + "*&*" + this.boxID + "*&*" + this.coinLabel +
                                                  "*&*" + this.options.orderID);
                        if (session[this.cookieName] != null && session[this.cookieName].ToString().Contains("--"))
                        {
                            this.options.userID = session[this.cookieName].ToString().Trim();
                        }
                        else
                        {
                            TimeSpan t = (DateTime.Now - new DateTime(1970, 1, 1));
                            long d = (long)t.TotalSeconds;
                            if (d > 1410000000)
                                d -= 1410000000;
                            string v = (d.ToString() + "__" +
                                       Calculator.md5(rand.Next().ToString() + rand.Next().ToString() +
                                                      rand.Next().ToString()).Substring(0, 10)).Trim();
                            this.options.userID = v;
                            session.Add(this.cookieName, v);
                        }
                        break;
                    case "IPADDRESS":
                        IPAddress ipAddress;
                        if (session["cryptoUserIP"] != null &&
                            IPAddress.TryParse(session["cryptoUserIP"].ToString(), out ipAddress))
                        {
                        }
                        else
                        {
                            session.Add("cryptoUserIP", HttpContext.Current.Request.UserHostAddress);
                            this.options.userID =
                                Calculator.md5(HttpContext.Current.Request.UserHostAddress + "*&*" +
                                               this.boxID + "*&*" + this.coinLabel + "*&*" + this.options.orderID);
                        }
                        break;
                    default:
                        throw new FormatException("Invalid userFormat value - $this->userFormat");
                }
            }

            if (this.options.iframeID == string.Empty)
                this.options.iframeID = iframe_id();

            this.check_payment();
        }


        public DisplayCryptoboxModel GetDisplayCryptoboxModel()
        {
            DisplayCryptoboxModel obj = new DisplayCryptoboxModel();
            obj.amount = this.options.amount;
            obj.boxID = this.boxID;
            obj.coinName = this.coinName;
            obj.public_key = this.options.public_key;
            obj.private_key = this.options.private_key;
            obj.amountUSD = this.options.amountUSD;
            obj.period = this.options.period;
            obj.language = this.options.language;
            obj.iframeID = this.options.iframeID;
            obj.userID = this.options.userID;
            obj.userFormat = this.options.userFormat;
            obj.orderID = this.options.orderID;
            obj.cookieName = this.cookieName;
            obj.webdev_key = this.options.webdev_key;
            obj.is_paid = is_paid();
            obj.coinLabel = this.coinLabel;
            obj.boxType = this.boxType;
            obj.amoutnPaid = this.amountPaid;

            return obj;
        }

        /// <summary>
        /// Function cryptobox_json_url()
        ///
        /// It generates url with your paramenters to gourl.io payment gateway.
        /// Using this url you can get bitcoin/altcoin payment box values in JSON format and use it on html page with Jquery/Ajax.

        /// See instruction https://gourl.io/bitcoin-payment-gateway-api.html#p8
        ///  
        /// JSON Values Example -
        /// Payment not received - https://coins.gourl.io/b/20/c/Bitcoin/p/20AAvZCcgBitcoin77BTCPUB0xyyeKkxMUmeTJRWj7IZrbJ0oL/a/0/au/2.21/pe/NOEXPIRY/l/en/o/invoice22/u/83412313__3bccb54769/us/COOKIE/j/1/d/ODIuMTEuOTQuMTIx/h/e889b9a07493ee96a479e471a892ae2e   
        /// Payment received successfully - https://coins.gourl.io/b/20/c/Bitcoin/p/20AAvZCcgBitcoin77BTCPUB0xyyeKkxMUmeTJRWj7IZrbJ0oL/a/0/au/0.1/pe/NOEXPIRY/l/en/o/invoice1/u/demo/us/MANUAL/j/1/d/ODIuMTEuOTQuMTIx/h/ac7733d264421c8410a218548b2d2a2a
        /// 
        /// Alternatively, you can receive JSON values through php curl on server side - function get_json_values() and use it in your php/other files without using javascript and jquery/ajax.

        ///
        /// By default the user sees bitcoin payment box as iframe in html format - function display_cryptobox().

        /// JSON data will allow you to easily customise your bitcoin payment boxes.For example, you can display payment amount and
        /// bitcoin payment address with your own text, you can also accept payments in android/windows and other applications.
        /// You get an array of values - payment amount, bitcoin address, text; and can place them in any position on your webpage/application.
        /// </summary>
        /// <returns></returns>
        public string cryptobox_json_url()
        {
            string ip = HttpContext.Current.Request.UserHostAddress;
            string hash = Calculator.cryptobox_hash(GetDisplayCryptoboxModel(), true);

            string url = String.Join("/", "https://coins.gourl.io",
                "b", boxID.ToString(),
                "c", coinName,
                "p", this.options.public_key,
                "a", this.options.amount.ToString(),
                "au", this.options.amountUSD.ToString(),
                "pe", this.options.period.Replace(" ", "_"),
                "l", this.options.language,
                "o", this.options.orderID,
                "u", this.options.userID,
                "us", this.options.userFormat,
                "j", "1",
                "d", Convert.ToBase64String(Encoding.UTF8.GetBytes(ip)),
                "h", hash);
            if (this.options.webdev_key != "")
            {
                url += "/w/" + this.options.webdev_key;
            }
            Random rand = new Random();
            url += "/z/" + rand.Next(0, 10000000);
            return url; 
        }

        public dynamic get_json_values()
        {
            string url = cryptobox_json_url();
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(20);

            HttpResponseMessage response = client.GetAsync(url).Result;
            HttpContent responseContent = response.Content;
            using (var reader = new StreamReader(responseContent.ReadAsStreamAsync().Result))
            {
                IDictionary<string, object> res = Json.Decode(reader.ReadToEndAsync().Result);
                if (res == null)
                {
                    return null;
                }
                bool f = false;
                IDictionary<string, object> arr = res;
                if (arr["data_hash"] != null)
                {
                    arr.Remove("data_hash");
                    if (arr["data_hash"].ToString() ==
                        Calculator.hash512(this.options.private_key + Json.Encode(arr) + this.options.private_key)
                            .ToLower())
                    {
                        f = true;
                    }
                }
                if (!f)
                    return null;
                    
                return res;
            }
        }

        /// <summary>
        /// Function is_paid(bool remotedb = false) -
        /// 
        /// This Checks your local database whether payment has been received and is stored on your local database.
        ///
        /// Please note that our server will also inform your server automatically every time when payment is 
        /// received through callback url: cryptobox.callback.php.I.e. if the user does not click on button,
        /// your website anyway will receive notification about a given user and save it in your database. 
        /// And when your user next time comes on your website/reload page he will automatically will see
        /// message that his payment has been received successfully.
        /// </summary>
        /// <param name="remotedb">If use $remotedb = true, it will check also on the remote cryptocoin payment server(gourl.io),
        /// and if payment is received, it saves it in your local database.Usually user will see on bottom
        /// of payment box button 'Click Here if you have already sent coins' and when they click on that button,
        /// script it will connect to our remote cryptocoin payment box server.Therefore you don't need to use
        /// remotedb = true, it will make your webpage load slowly if payment on gourl.io is checked during
        /// each of your page loadings.</param>
        /// <returns></returns>
        public bool is_paid(bool remotedb = false)
        {
            if (this.paymentID != null && remotedb)
                return this.check_payment(remotedb);
            if (this.paid)
                return true;
            return false;
        }

        public bool is_confirmed()
        {
            if (this.confirmed)
                return true;
            return false;
        }

        public decimal amount_paid()
        {
            if (this.paid)
                return this.amountPaid;
            return 0;
        }

        public decimal amount_paid_usd()
        {
            if (this.paid)
                return this.amountPaidUSD;
            return 0;
        }

        public bool set_status_processed()
        {
            if (this.paymentID != 0 && this.paid)
            {
                if (!this.processed)
                {
                    crypto_payments obj = Context.crypto_payments.FirstOrDefault(x => x.paymentID == this.paymentID);
                    obj.processed = 1;
                    obj.processedDate = DateTime.Now;
                    Context.SaveChanges();
                    this.processed = true;
                }
                return true;
            }
            return false;
        }

        public bool is_processed()
        {
            if (this.paid && this.processed)
                return true;
            return false;
        }

        public string cryptobox_type()
        {
            return this.boxType;
        }

        public int? payment_id()
        {
            return this.paymentID;
        }

        public DateTime payment_date()
        {
            return this.paymentDate;
        }

        public crypto_payments payment_info()
        {
            if (this.paymentID == null)
                return null;
            else
            {
                crypto_payments obj = Context.crypto_payments.FirstOrDefault(x => x.paymentID == this.paymentID);
                return obj;
            }
        }

        public bool cryptobox_reset()
        {
            if (options.userFormat == "COOKIE" || options.userFormat == "SESSION")
            {
                Random rand = new Random();
                this.options.iframeID = iframe_id();
                switch (options.userFormat)
                {
                    case "COOKIE":
                        string s = HttpContext.Current.Server.MachineName.ToLower().Trim('/');
                        if (s.IndexOf("www.") == 0)
                            s = s.Substring(4);
                        TimeSpan t = (DateTime.Now - new DateTime(1970, 1, 1));
                        long d = (long)t.TotalSeconds;
                        if (d > 1410000000)
                            d -= 1410000000;
                        string v = (d.ToString() + "__" +
                                       Calculator.md5(rand.Next().ToString() + rand.Next().ToString() +
                                                      rand.Next().ToString()).Substring(0, 10)).Trim();
                        HttpContext.Current.Response.Cookies.Set(new HttpCookie(this.cookieName, v)
                        {
                            Expires = DateTime.MaxValue,
                            Path = "/",
                            Domain = s
                        });
                        break;
                    case "SESSION":
                        TimeSpan tt = (DateTime.Now - new DateTime(1970, 1, 1));
                        long dd = (long)tt.TotalSeconds;
                        if (dd > 1410000000)
                            dd -= 1410000000;
                        string vv = (dd.ToString() + "__" +
                                   Calculator.md5(rand.Next().ToString() + rand.Next().ToString() +
                                                  rand.Next().ToString()).Substring(0, 10)).Trim();
                        this.options.userID = vv;
                        HttpContext.Current.Session.Add(this.cookieName, vv);
                        break;
                }
                return true;
            }
            return false;
        }

        public string coin_name()
        {
            return coinName;
        }

        public string coin_label()
        {
            return coinLabel;
        }

        public string iframe_id()
        {
            return "box" + Calculator.icrc32(this.boxID + "__" + this.options.orderID + "__" + this.options.userID + "__" + this.options.private_key);
        }

        private bool check_payment(bool remotedb = false)
        {

            TimeSpan diff = TimeSpan.Zero;
            paymentID = 0;
            crypto_payments obj =
                Context.crypto_payments.FirstOrDefault(
                    x => x.boxID == this.boxID && x.orderID == this.options.orderID && x.userID == this.options.userID);
            if (obj != null)
            {
                this.paymentID = obj.paymentID;
                if (obj.processedDate != null) this.paymentDate = (DateTime)obj.processedDate;
                this.amountPaid = obj.amount;
                this.amountPaidUSD = obj.amountUSD;
                this.paid = true;
                this.confirmed = obj.txConfirmed != 0;
                this.boxType = obj.boxType;
                this.processed = obj.processed != 0;
                if (obj.txCheckDate != null) diff = DateTime.Now - obj.txCheckDate.Value;
            }
            if (obj == null && HttpContext.Current.Request["cryptobox_live_"] != null &&
                HttpContext.Current.Request["cryptobox_live_"] ==
                Calculator.md5(options.iframeID + options.private_key + options.userID))
            {
                remotedb = true;
            }
            
            if (!this.already_checked &&
                ((obj == null && remotedb) || (obj != null && !this.confirmed && (diff > TimeSpan.FromMinutes(10)))))
            {
                check_payment_live();
                this.already_checked = true;
            }
            return true;
        }

        public bool check_payment_live()
        {
            string ip = HttpContext.Current.Request.UserHostAddress;
            string hash = Calculator.md5(boxID + options.private_key + options.userID + options.orderID + options.language + options.period + ip);
            string box_status = "";

            HttpClient client = new HttpClient();
            FormUrlEncodedContent requestContent = new FormUrlEncodedContent(new[] {new KeyValuePair<string, string>("r", this.options.private_key),
                new KeyValuePair<string, string>("b", boxID.ToString()),
                new KeyValuePair<string, string>("o", this.options.orderID),
                new KeyValuePair<string, string>("u", this.options.userID),
                new KeyValuePair<string, string>("l", this.options.language),
                new KeyValuePair<string, string>("e", this.options.period),
                new KeyValuePair<string, string>("i", ip),
                new KeyValuePair<string, string>("h", hash),
            });
            client.Timeout = TimeSpan.FromSeconds(20);

            HttpResponseMessage response = client.PostAsync("https://coins.gourl.io/result.php", requestContent).Result;

            // Get the response content.
            HttpContent responseContent = response.Content;

            // Get the stream of the content.
            using (var reader = new StreamReader(responseContent.ReadAsStreamAsync().Result))
            {
                // Write the output.
                dynamic res = Json.Decode(reader.ReadToEndAsync().Result);

                if (res["err"] != "")
                {
                    return false;
                }

                int box = 0;
                decimal amount = 0;
                if (res["status"] != "" && res["status"] == "payment_received" &&
                    res["box"] != "" && Int32.TryParse(res["box"], out box) && box > 0 &&
                    res["amount"] != "" && decimal.TryParse(res["amount"], NumberStyles.Any, CultureInfo.InvariantCulture, out amount) && amount > 0 &&
                    res["private_key"] != "" && Regex.IsMatch(res["private_key"], "[a-zA-Z0-9]+") && res["private_key"] == this.options.private_key)
                {
                    string order = res["order"];
                    string user = res["user"];
                    string tx = res["tx"];
                    string addr = res["addr"];
                    crypto_payments obj = Context.crypto_payments.FirstOrDefault(x => x.boxID == box &&
                            x.orderID == order && x.userID == user && x.txID == tx && x.amount == amount && x.addr == addr);
                    if (obj != null)
                    {
                        this.paymentID = obj.paymentID;
                        this.processed = obj.processed != 0;
                        this.confirmed = obj.txConfirmed != 0;

                        obj.boxType = res["boxtype"];
                        obj.amount = decimal.Parse(res["amount"], CultureInfo.InvariantCulture);
                        obj.amountUSD = decimal.Parse(res["amountusd"], CultureInfo.InvariantCulture);
                        obj.coinLabel = res["coinlabel"];
                        obj.unrecognised = 0;
                        obj.addr = res["addr"];
                        obj.txDate = DateTime.Parse(res["datetime"]);
                        obj.txConfirmed = byte.Parse(res["confirmed"]);
                        obj.txCheckDate = DateTime.Now;
                        Context.SaveChanges();

                        if (res["confirmed"] != "" && res["confirmed"] != "0" && !this.confirmed)
                        {
                            box_status = "cryptobox_updated";
                        }
                    }
                    else
                    {
                        // Save new payment details in local database
                        crypto_payments newPayment = new crypto_payments()
                        {
                            boxID = Int32.Parse(res["box"]),
                            boxType = res["boxtype"],
                            orderID = res["order"],
                            userID = res["user"],
                            countryID = res["usercountry"],
                            coinLabel = res["coinlabel"],
                            amount = decimal.Parse(res["amount"], CultureInfo.InvariantCulture),
                            amountUSD = decimal.Parse(res["amountusd"], CultureInfo.InvariantCulture),
                            unrecognised = 0,
                            addr = res["addr"],
                            txID = res["tx"],
                            txDate = DateTime.Parse(res["datetime"]),
                            txConfirmed = byte.Parse(res["confirmed"]),
                            txCheckDate = DateTime.Now,
                            recordCreated = DateTime.Now,
                            processed = 0
                        };
                        Context.crypto_payments.Add(newPayment);
                        Context.SaveChanges();
                        this.paymentID = newPayment.paymentID;
                        box_status = "cryptobox_newrecord";
                    }

                    paymentDate = DateTime.Parse(res["datetime"]);
                    amountPaid = decimal.Parse(res["amount"], CultureInfo.InvariantCulture);
                    amountPaidUSD = decimal.Parse(res["amountusd"], CultureInfo.InvariantCulture);
                    paid = true;
                    boxType = res["boxtype"];
                    confirmed = res["confirmed"] != "0";

                    if (box_status == "cryptobox_newrecord" || box_status == "cryptobox_updated")
                    {

                        NewPayment.Main(this.paymentID ?? 0, new IPNModel()
                        {
                            addr = res["addr"],
                            amount = decimal.Parse(res["amount"], CultureInfo.InvariantCulture),
                            amountusd = decimal.Parse(res["amountusd"], CultureInfo.InvariantCulture),
                            confirmed = byte.Parse(res["confirmed"]),
                            box = Int32.Parse(res["box"]),
                            private_key_hash = Calculator.md512(res["private_key"]),
                            user = res["user"],
                            order = res["order"],
                            tx = res["tx"],
                            datetime = DateTime.Parse(res["datetime"]),
                            coinlabel = res["coinlabel"],
                            boxtype = res["boxtype"],
                            usercountry = res["usercountry"],
                            status = res["status"],
                            err = res["err"],
                            timestamp = res["timestamp"] ?? 0,
                            coinname = res["coinname"],
                            date = DateTime.Parse(res["date"])
                        }, box_status);
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns array with history payment details of any of your users / orders / etc. (except unrecognised payments) for custom period - period
        /// </summary>
        /// <param name="boxID">your cryptobox id, the same as on gourl.io member page</param>
        /// <param name="orderID">your order id / page name / etc.</param>
        /// <param name="userID">your user identifier</param>
        /// <param name="countryID">your user's location (country) , 3 letter ISO country code</param>
        /// <param name="boxType">'paymentbox' or 'captchabox'</param>
        /// <param name="period">in format "5 MINUTE", "7 HOUR", "12 DAY", "15 WEEK", "3 MONTH"</param>
        /// <returns></returns>

        public IEnumerable<crypto_payments> payment_history(int boxID = 0, string orderID = "", string userID = "",
            string countryID = "", string boxType = "", string period = "7 DAY")
        {
            if (boxID != 0 && boxID < 1 ||
                    orderID != "" && !Regex.IsMatch(orderID, @"[a-zA-Z0-9\._@-]+") ||
                    userID != "" && !Regex.IsMatch(userID, @"[a-zA-Z0-9\._@-]+") ||
                    countryID != "" && (countryID.Length != 3 || !Regex.IsMatch(countryID, @"[a-zA-Z]+")) ||
                    boxType != "" && !(boxType == "paymentbox" || boxType == "captchabox") ||
                    period != "" || Regex.IsMatch(period, @"[a-zA-Z0-9]+")
                )
            {
                return null;
            }

            TimeSpan tm = Calculator.ConvertPeriod(period);
            return Context.crypto_payments.Where(x => x.unrecognised == 0 && (boxID == 0 || x.boxID == boxID) && (orderID == "" || x.orderID == orderID) && (userID == "" || x.userID == userID) && (countryID == "" || x.countryID == countryID.ToUpper()) && x.recordCreated > DateTime.Now.Subtract(tm)).OrderBy(x => x.txDate).Take(1000);
        }

        /// <summary>
        /// Returns array with unrecognised payments for custom period - $period.
        /// (users paid wrong amount to your internal wallet address). 
        /// You will need to process unrecognised payments manually.
        ///
        /// We forward you ALL coins received to your internal wallet address 
        /// including all possible incorrect amount/unrecognised payments 
        /// automatically every 30 minutes. 
        /// 
        /// Therefore if your user contacts us, regarding the incorrect sent payment,
        /// we will forward your user to you (because our system forwards all received payments
        /// to your wallet automatically every 30 minutes). We provide a payment gateway only.
        /// You need to deal with your user directly to resolve the situation or return the incorrect
        /// payment back to your user. In unrecognised payments statistics table you will see the
        /// original payment sum and transaction ID - when you click on that transaction's ID
        /// it will open external blockchain explorer website with wallet address/es showing
        /// that payment coming in. You can tell your user about your return of that incorrect
        /// payment to one of their sending address (which will protect you from bad claims).
        ///
        /// You will have a copy of the statistics on your gourl.io member page
        /// with details of incorrect received payments.
        /// </summary>
        /// <param name="boxID">your cryptobox id, the same as on gourl.io member page</param>
        /// <param name="period">in format "5 MINUTE", "7 HOUR", "12 DAY", "15 WEEK", "3 MONTH"</param>
        /// <returns></returns>
        public IEnumerable<crypto_payments> payment_unrecognised(int boxID = 0, string period = "7 DAY")
        {
            if (boxID == 0 || period == "" || Regex.IsMatch(period, @"[a-zA-Z0-9]+"))
            {
                return null;
            }

            TimeSpan tm = Calculator.ConvertPeriod(period);
            return Context.crypto_payments.Where(x => x.unrecognised == 1 && (boxID == 0 || x.boxID == boxID) && (period == "" || x.recordCreated > DateTime.Now.Subtract(tm))).OrderBy(x => x.txDate).Take(1000);
        }

        public void Dispose()
        {
            options = null;
            Context = null;
        }
    }

    public static class CryptoHelper
    {
	private static readonly string[] CryptoboxCoins = { "bitcoin", "bitcoincash", "litecoin", "dash", "dogecoin", "speedcoin", "reddcoin", "potcoin", "feathercoin", "vertcoin", "peercoin", "monetaryunit", "universalcurrency" };

        public static string cryptobox_selcoin(string[] coins, string defCoin = "")
        {
            if (coins.Length == 0)
                return "";
            defCoin = defCoin.ToLower();
            string id = "gourlcryptocoin";

            string coinName;

            if (!CryptoboxCoins.Any(coins.Contains))
            {
                coins = new[] { defCoin };
            }

            if (HttpContext.Current.Request.QueryString[id] != null &&
                HttpContext.Current.Request.QueryString[id] != "" &&
                CryptoboxCoins.Contains(HttpContext.Current.Request.QueryString[id]) &&
                coins.Contains(HttpContext.Current.Request.QueryString[id]))
            {
                coinName = HttpContext.Current.Request.QueryString[id];
                HttpContext.Current.Response.Cookies.Add(new HttpCookie(id, coinName) { Expires = DateTime.Now + TimeSpan.FromDays(7) });
            }
            else if (HttpContext.Current.Request.Cookies[id] != null &&
                     HttpContext.Current.Request.Cookies[id].Value != "" &&
                     CryptoboxCoins.Contains(HttpContext.Current.Request.Cookies[id].Value))
            {
                coinName = HttpContext.Current.Request.Cookies[id].Value;
            }
            else coinName = defCoin;

            return coinName;
        }

        /// <summary>
        /// Currency Converter using Google Finance live exchange rates
        ///Example - convert_currency_live("EUR", "USD", 22.37) - convert 22.37euro to usd
        /// convert_currency_live("EUR", "BTC", 22.37) - convert 22.37euro to bitcoin
        /// </summary>
        /// <param name="from_Currency"></param>
        /// <param name="to_Currency"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static decimal convert_currency_live(string from_Currency, string to_Currency, double amount)
        {
            from_Currency = from_Currency.ToUpper().Trim();
            to_Currency = to_Currency.ToUpper().Trim();
            decimal res = 0;

            if (from_Currency == "TRL") from_Currency = "TRY"; // fix for Turkish Lyra
            if (from_Currency == "ZWD") from_Currency = "ZWL"; // fix for Zimbabwe Dollar
            if (from_Currency == "RIAL") from_Currency = "IRR"; // fix for Iranian Rial

            string url = "https://www.google.com/finance/converter?a=" + amount + "&from=" + from_Currency + "&to=" + to_Currency;

            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(20);

            HttpResponseMessage response = client.GetAsync(url).Result;

            // Get the response content.
            HttpContent responseContent = response.Content;

            // Get the stream of the content.
            using (var reader = new StreamReader(responseContent.ReadAsStreamAsync().Result))
            {
                // Write the output.
                try
                {
                    string obj = reader.ReadToEndAsync().Result;
                    string num = obj.Substring(obj.IndexOf("bld>") + 4);
                    num = num.Substring(0, num.IndexOf(to_Currency) - 1);
                    res = Math.Round(decimal.Parse(num, CultureInfo.InvariantCulture), to_Currency == "BTC" ? 5 : 2);
                }
                catch (Exception)
                {
                    return -1;
                }

            }
            return res;
        }

        public static string get_country_name(string countryId)
        {
            IDictionary<string, string> arr = new Dictionary<string, string>()
            {
                {"AFG", "Afghanistan"},
                {"ALA", "Aland Islands"},
                {"ALB", "Albania"},
                {"DZA", "Algeria"},
                {"ASM", "American Samoa"},
                {"AND", "Andorra"},
                {"AGO", "Angola"},
                {"AIA", "Anguilla"},
                {"ATA", "Antarctica"},
                {"ATG", "Antigua and Barbuda"},
                {"ARG", "Argentina"},
                {"ARM", "Armenia"},
                {"ABW", "Aruba"},
                {"AUS", "Australia"},
                {"AUT", "Austria"},
                {"AZE", "Azerbaijan"},
                {"BHS", "Bahamas"},
                {"BHR", "Bahrain"},
                {"BGD", "Bangladesh"},
                {"BRB", "Barbados"},
                {"BLR", "Belarus"},
                {"BEL", "Belgium"},
                {"BLZ", "Belize"},
                {"BEN", "Benin"},
                {"BMU", "Bermuda"},
                {"BTN", "Bhutan"},
                {"BOL", "Bolivia"},
                {"BIH", "Bosnia and Herzegovina"},
                {"BWA", "Botswana"},
                {"BVT", "Bouvet Island"},
                {"BRA", "Brazil"},
                {"IOT", "British Indian Ocean Territory"},
                {"BRN", "Brunei"},
                {"BGR", "Bulgaria"},
                {"BFA", "Burkina Faso"},
                {"BDI", "Burundi"},
                {"KHM", "Cambodia"},
                {"CMR", "Cameroon"},
                {"CAN", "Canada"},
                {"CPV", "Cape Verde"},
                {"BES", "Caribbean Netherlands"},
                {"CYM", "Cayman Islands"},
                {"CAF", "Central African Republic"},
                {"TCD", "Chad"},
                {"CHL", "Chile"},
                {"CHN", "China"},
                {"CXR", "Christmas Island"},
                {"CCK", "Cocos (Keeling) Islands"},
                {"COL", "Colombia"},
                {"COM", "Comoros"},
                {"COG", "Congo"},
                {"COD", "Congo}, {Democratic Republic"},
                {"COK", "Cook Islands"},
                {"CRI", "Costa Rica"},
                {"CIV", "Côte d’Ivoire"},
                {"HRV", "Croatia"},
                {"CUB", "Cuba"},
                {"CUW", "Curacao"},
                {"CBR", "Cyberbunker"},
                {"CYP", "Cyprus"},
                {"CZE", "Czech Republic"},
                {"DNK", "Denmark"},
                {"DJI", "Djibouti"},
                {"DMA", "Dominica"},
                {"DOM", "Dominican Republic"},
                {"TMP", "East Timor"},
                {"ECU", "Ecuador"},
                {"EGY", "Egypt"},
                {"SLV", "El Salvador"},
                {"GNQ", "Equatorial Guinea"},
                {"ERI", "Eritrea"},
                {"EST", "Estonia"},
                {"ETH", "Ethiopia"},
                {"EUR", "European Union"},
                {"FLK", "Falkland Islands"},
                {"FRO", "Faroe Islands"},
                {"FJI", "Fiji Islands"},
                {"FIN", "Finland"},
                {"FRA", "France"},
                {"GUF", "French Guiana"},
                {"PYF", "French Polynesia"},
                {"ATF", "French Southern territories"},
                {"GAB", "Gabon"},
                {"GMB", "Gambia"},
                {"GEO", "Georgia"},
                {"DEU", "Germany"},
                {"GHA", "Ghana"},
                {"GIB", "Gibraltar"},
                {"GRC", "Greece"},
                {"GRL", "Greenland"},
                {"GRD", "Grenada"},
                {"GLP", "Guadeloupe"},
                {"GUM", "Guam"},
                {"GTM", "Guatemala"},
                {"GGY", "Guernsey"},
                {"GIN", "Guinea"},
                {"GNB", "Guinea-Bissau"},
                {"GUY", "Guyana"},
                {"HTI", "Haiti"},
                {"HMD", "Heard Island and McDonald Islands"},
                {"HND", "Honduras"},
                {"HKG", "Hong Kong"},
                {"HUN", "Hungary"},
                {"ISL", "Iceland"},
                {"IND", "India"},
                {"IDN", "Indonesia"},
                {"IRN", "Iran"},
                {"IRQ", "Iraq"},
                {"IRL", "Ireland"},
                {"IMN", "Isle of Man"},
                {"ISR", "Israel"},
                {"ITA", "Italy"},
                {"JAM", "Jamaica"},
                {"JPN", "Japan"},
                {"JEY", "Jersey"},
                {"JOR", "Jordan"},
                {"KAZ", "Kazakstan"},
                {"KEN", "Kenya"},
                {"KIR", "Kiribati"},
                {"KWT", "Kuwait"},
                {"KGZ", "Kyrgyzstan"},
                {"LAO", "Laos"},
                {"LVA", "Latvia"},
                {"LBN", "Lebanon"},
                {"LSO", "Lesotho"},
                {"LBR", "Liberia"},
                {"LBY", "Libya"},
                {"LIE", "Liechtenstein"},
                {"LTU", "Lithuania"},
                {"LUX", "Luxembourg"},
                {"MAC", "Macao"},
                {"MKD", "Macedonia"},
                {"MDG", "Madagascar"},
                {"MWI", "Malawi"},
                {"MYS", "Malaysia"},
                {"MDV", "Maldives"},
                {"MLI", "Mali"},
                {"MLT", "Malta"},
                {"MHL", "Marshall Islands"},
                {"MTQ", "Martinique"},
                {"MRT", "Mauritania"},
                {"MUS", "Mauritius"},
                {"MYT", "Mayotte"},
                {"MEX", "Mexico"},
                {"FSM", "Micronesia}, {Federated States"},
                {"MDA", "Moldova"},
                {"MCO", "Monaco"},
                {"MNG", "Mongolia"},
                {"MNE", "Montenegro"},
                {"MSR", "Montserrat"},
                {"MAR", "Morocco"},
                {"MOZ", "Mozambique"},
                {"MMR", "Myanmar"},
                {"NAM", "Namibia"},
                {"NRU", "Nauru"},
                {"NPL", "Nepal"},
                {"NLD", "Netherlands"},
                {"ANT", "Netherlands Antilles"},
                {"NCL", "New Caledonia"},
                {"NZL", "New Zealand"},
                {"NIC", "Nicaragua"},
                {"NER", "Niger"},
                {"NGA", "Nigeria"},
                {"NIU", "Niue"},
                {"NFK", "Norfolk Island"},
                {"PRK", "North Korea"},
                {"MNP", "Northern Mariana Islands"},
                {"NOR", "Norway"},
                {"OMN", "Oman"},
                {"PAK", "Pakistan"},
                {"PLW", "Palau"},
                {"PSE", "Palestine"},
                {"PAN", "Panama"},
                {"PNG", "Papua New Guinea"},
                {"PRY", "Paraguay"},
                {"PER", "Peru"},
                {"PHL", "Philippines"},
                {"PCN", "Pitcairn"},
                {"POL", "Poland"},
                {"PRT", "Portugal"},
                {"PRI", "Puerto Rico"},
                {"QAT", "Qatar"},
                {"REU", "Réunion"},
                {"ROM", "Romania"},
                {"RUS", "Russia"},
                {"RWA", "Rwanda"},
                {"BLM", "Saint Barthelemy"},
                {"SHN", "Saint Helena"},
                {"KNA", "Saint Kitts and Nevis"},
                {"LCA", "Saint Lucia"},
                {"MAF", "Saint Martin"},
                {"SPM", "Saint Pierre and Miquelon"},
                {"VCT", "Saint Vincent and the Grenadines"},
                {"WSM", "Samoa"},
                {"SMR", "San Marino"},
                {"STP", "Sao Tome and Principe"},
                {"SAU", "Saudi Arabia"},
                {"SEN", "Senegal"},
                {"SRB", "Serbia"},
                {"SYC", "Seychelles"},
                {"SLE", "Sierra Leone"},
                {"SGP", "Singapore"},
                {"SXM", "Sint Maarten"},
                {"SVK", "Slovakia"},
                {"SVN", "Slovenia"},
                {"SLB", "Solomon Islands"},
                {"SOM", "Somalia"},
                {"ZAF", "South Africa"},
                {"SGS", "South Georgia and the South Sandwich Islands"},
                {"KOR", "South Korea"},
                {"SSD", "South Sudan"},
                {"ESP", "Spain"},
                {"LKA", "Sri Lanka"},
                {"SDN", "Sudan"},
                {"SUR", "Suriname"},
                {"SJM", "Svalbard and Jan Mayen"},
                {"SWZ", "Swaziland"},
                {"SWE", "Sweden"},
                {"CHE", "Switzerland"},
                {"SYR", "Syria"},
                {"TWN", "Taiwan"},
                {"TJK", "Tajikistan"},
                {"TZA", "Tanzania"},
                {"THA", "Thailand"},
                {"TGO", "Togo"},
                {"TKL", "Tokelau"},
                {"TON", "Tonga"},
                {"TTO", "Trinidad and Tobago"},
                {"TUN", "Tunisia"},
                {"TUR", "Turkey"},
                {"TKM", "Turkmenistan"},
                {"TCA", "Turks and Caicos Islands"},
                {"TUV", "Tuvalu"},
                {"UGA", "Uganda"},
                {"UKR", "Ukraine"},
                {"ARE", "United Arab Emirates"},
                {"GBR", "United Kingdom"},
                {"UMI", "United States Minor Outlying Islands"},
                {"URY", "Uruguay"},
                {"USA", "USA"},
                {"UZB", "Uzbekistan"},
                {"VUT", "Vanuatu"},
                {"VAT", "Vatican (Holy See)"},
                {"VEN", "Venezuela"},
                {"VNM", "Vietnam"},
                {"VGB", "Virgin Islands}, {British"},
                {"VIR", "Virgin Islands}, {U.S."},
                {"WLF", "Wallis and Futuna"},
                {"ESH", "Western Sahara"},
                {"XKX", "Kosovo"},
                {"YEM", "Yemen"},
                {"ZMB", "Zambia"},
                {"ZWE","Zimbabwe"}
            };

            return arr[countryId.ToUpper()];
        }

        public static string cryptobox_sellanguage(string language = "en")
        {
            string lan = "en";
            string id = "gourlcryptolang";
            language = language.ToLower();
            string[] langArr = { "en", "es", "fr", "de", "nl", "it", "ru", "pl", "pt", "fa", "ko", "ja", "id", "tr", "ar", "cn", "zh", "hi" };
            
            if (HttpContext.Current.Request.QueryString[id] != null && HttpContext.Current.Request.QueryString[id] != "" && langArr.Contains(HttpContext.Current.Request.QueryString[id]))
            {
                lan = HttpContext.Current.Request.QueryString[id];
                HttpContext.Current.Response.Cookies.Add(new HttpCookie(id, lan) { Expires = DateTime.Now + TimeSpan.FromDays(7) });
            }
            else if (HttpContext.Current.Request.Cookies[id] != null &&
                     HttpContext.Current.Request.Cookies[id].Value != String.Empty &&
                     langArr.Contains(HttpContext.Current.Request.Cookies[id].Value))
            {
                lan = HttpContext.Current.Request.Cookies[id].Value;
            }
            return lan;
        }
    }

    //if you have problem with decimal binding

    /*public class DecimalModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext,
                                         ModelBindingContext bindingContext)
        {
            object result = null;

            // Don't do this here!
            // It might do bindingContext.ModelState.AddModelError
            // and there is no RemoveModelError!
            // 
            // result = base.BindModel(controllerContext, bindingContext);

            string modelName = bindingContext.ModelName;
            string attemptedValue =
                bindingContext.ValueProvider.GetValue(modelName).AttemptedValue;

            // Depending on CultureInfo, the NumberDecimalSeparator can be "," or "."
            // Both "." and "," should be accepted, but aren't.
            string wantedSeperator = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
            string alternateSeperator = (wantedSeperator == "." ? "," : ".");

            if (attemptedValue.IndexOf(wantedSeperator) == -1
                && attemptedValue.IndexOf(alternateSeperator) != -1)
            {
                attemptedValue =
                    attemptedValue.Replace(alternateSeperator, wantedSeperator);
            }

            try
            {
                if (bindingContext.ModelMetadata.IsNullableValueType
                    && string.IsNullOrWhiteSpace(attemptedValue))
                {
                    return null;
                }

                result = decimal.Parse(attemptedValue, NumberStyles.Any);
            }
            catch (FormatException e)
            {
                bindingContext.ModelState.AddModelError(modelName, e);
            }

            return result;
        }
    }*/
}