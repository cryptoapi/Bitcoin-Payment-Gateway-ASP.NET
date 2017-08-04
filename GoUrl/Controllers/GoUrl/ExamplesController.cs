using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using Gourl.GoUrlCore;
using Gourl.Models;
using Gourl.Models.GoUrl;

namespace Gourl.Controllers.GoUrl
{
    public class ExamplesController : Controller
    {
        // GET: Examples
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Basic()
        {
            OptionsModel options = new OptionsModel()
            {
                public_key = "-your public key for Bitcoin box-",
                private_key = "-your private key for Bitcoin box-",
                webdev_key = "",
                orderID = "your_product1_or_signuppage1_etc",
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = 2,
                period = "24 HOUR",
                iframeID = "",
                language = "DE"
            };
            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();
                // A. Process Received Payment
                if (cryptobox.is_paid())
                {
                    ViewBag.Message = "A. User will see this message during 24 hours after payment has been made!" +
                                      "<br/>" + cryptobox.amount_paid() + " " + cryptobox.coin_label() +
                                      "  received<br/>";
                    // Your code here to handle a successful cryptocoin payment/captcha verification
                    // For example, give user 24 hour access to your member pages

                    // B. One-time Process Received Payment
                    if (!cryptobox.is_processed())
                    {
                        ViewBag.Message += "B. User will see this message one time after payment has been made!";
                        // Your code here - for example, publish order number for user
                        // ...

                        // Also you can use is_confirmed() - return true if payment confirmed 
                        // Average transaction confirmation time - 10-20min for 6 confirmations  

                        // Set Payment Status to Processed
                        cryptobox.set_status_processed();

                        // Optional, cryptobox_reset() will delete cookies/sessions with userID and 
                        // new cryptobox with new payment amount will be show after page reload.
                        // Cryptobox will recognize user as a new one with new generated userID
                        // cryptobox_reset(); 
                    }
                }
                ViewBag.Message = "The payment has not been made yet";

                return View(model);
            }
        }

        public ActionResult PayPerDownload()
        {

            ViewBag.filename = "my_file1.zip";
            string dir = "protected";
            OptionsModel options = new OptionsModel()
            {
                public_key = "-your public key for Bitcoin box-",
                private_key = "-your private key for Bitcoin box-",
                webdev_key = "",
                orderID = Calculator.md5(dir + ViewBag.filename),
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = (decimal)0.2,
                period = "24 HOUR",
                iframeID = "",
                language = "EN"
            };
            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                if (cryptobox.is_paid())
                {
                    ViewBag.DownloadLink = "href = " + Url.Action("Download");
                    cryptobox.set_status_processed();
                }
                else
                {
                    ViewBag.DownloadLink = "onclick='alert(\"You need to send " + cryptobox.coin_name() +
                                           "s first !\")' href='#a'";
                }

                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();

                return View(model);
            }
        }

        public FileResult Download()
        {
            string filename = "my_file1.zip";
            string dir = "protected";

            byte[] file = System.IO.File.ReadAllBytes(Server.MapPath(@"~/" + dir + "//" + filename));
            return File(file, System.Net.Mime.MediaTypeNames.Application.Octet, filename);
        }

        public ActionResult PayPerDownloadMulti()
        {
            ViewBag.filename = "my_file1.zip";
            string dir = "protected";

            string defPaiment = "bitcoin";

            string[] available_payments = new[] { "bitcoin", "dogecoin" };
            IDictionary<string, IDictionary<string, string>> all_keys = new Dictionary
                <string, IDictionary<string, string>>()
            {
                {
                    "bitcoin", new Dictionary<string, string>()
                    {
                        {"public_key", "-your public key for Bitcoin box-"},
                        {"private_key", "-your private key for Bitcoin box-"}
                    }
                },
                {
                    "dogecoin", new Dictionary<string, string>()
                    {
                        {"public_key", "-your public key for Dogecoin box-"},
                        {"private_key", "-your private key for Dogecoin box-"}
                    }
                }
            };

            foreach (KeyValuePair<string, IDictionary<string, string>> valuePair in all_keys)
            {
                if (valuePair.Value["public_key"] == null || valuePair.Value["private_key"] == null ||
                    valuePair.Value["public_key"] == "" || valuePair.Value["private_key"] == "")
                {
                    return
                        new ContentResult()
                        {
                            Content = "Please add your public/private keys for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!valuePair.Value["public_key"].Contains("PUB"))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Invalid public key for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!valuePair.Value["private_key"].Contains("PRV"))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Invalid private key for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!ConfigurationManager.AppSettings["PrivateKeys"].Contains(valuePair.Value["private_key"]))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Please add your private key for " + valuePair.Key +
                                      " in Web.config."
                        };
                }
            }

            string coinName = CryptoHelper.cryptobox_selcoin(available_payments, defPaiment);
            OptionsModel options = new OptionsModel()
            {
                public_key = all_keys[coinName]["public_key"],
                private_key = all_keys[coinName]["private_key"],
                webdev_key = "",
                orderID = Calculator.md5(dir + ViewBag.filename),
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = (decimal)0.01,
                period = "24 HOUR",
                language = "en"
            };
            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                if (cryptobox.is_paid())
                {
                    ViewBag.DownloadLink = "href = " + Url.Action("Download");
                    cryptobox.set_status_processed();
                }
                else
                {
                    ViewBag.DownloadLink = "onclick='alert(\"You need to send " + cryptobox.coin_name() +
                                           "s first !\")' href='#a'";
                }

                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();
                ViewBag.Coins = available_payments;
                ViewBag.DefCoin = defPaiment;
                ViewBag.DefLang = options.language;

                return View(model);
            }
        }

        public ActionResult PayPerMembership()
        {
            OptionsModel options = new OptionsModel()
            {
                public_key = "-your public key for Bitcoin box-",
                private_key = "-your private key for Bitcoin box-",
                webdev_key = "",
                orderID = "premium_membership",
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = (decimal)0.2,
                period = "1 MONTH",
                language = "en"
            };

            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                if (cryptobox.is_paid())
                {
                    if (!cryptobox.is_processed())
                    {
                        ViewBag.Message = "Thank you (order #" + options.orderID + ", payment #" +
                                          cryptobox.payment_id() + "). We upgraded your membership to Premium";
                        cryptobox.set_status_processed();
                    }
                    else
                    {
                        ViewBag.Message = "You have a Premium Membership";
                    }
                }
                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();
                return View(model);
            }
        }

        public ActionResult PayPerMembershipMulti()
        {
            string defPaiment = "bitcoin";

            string[] available_payments = new[] { "bitcoin", "dogecoin" };
            IDictionary<string, IDictionary<string, string>> all_keys = new Dictionary
                <string, IDictionary<string, string>>()
            {
                {
                    "bitcoin", new Dictionary<string, string>()
                    {
                        {"public_key", "-your public key for Bitcoin box-"},
                        {"private_key", "-your private key for Bitcoin box-"}
                    }
                },
                {
                    "dogecoin", new Dictionary<string, string>()
                    {
                        {"public_key", "-your public key for Dogecoin box-"},
                        {"private_key", "-your private key for Dogecoin box-"}
                    }
                }
            };

            foreach (KeyValuePair<string, IDictionary<string, string>> valuePair in all_keys)
            {
                if (valuePair.Value["public_key"] == null || valuePair.Value["private_key"] == null ||
                    valuePair.Value["public_key"] == "" || valuePair.Value["private_key"] == "")
                {
                    return
                        new ContentResult()
                        {
                            Content = "Please add your public/private keys for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!valuePair.Value["public_key"].Contains("PUB"))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Invalid public key for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!valuePair.Value["private_key"].Contains("PRV"))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Invalid private key for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!ConfigurationManager.AppSettings["PrivateKeys"].Contains(valuePair.Value["private_key"]))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Please add your private key for " + valuePair.Key +
                                      " in Web.config."
                        };
                }
            }

            string coinName = CryptoHelper.cryptobox_selcoin(available_payments, defPaiment);
            OptionsModel options = new OptionsModel()
            {
                public_key = all_keys[coinName]["public_key"],
                private_key = all_keys[coinName]["private_key"],
                webdev_key = "",
                orderID = "premium_membership",
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = (decimal)0.01,
                period = "1 MONTH",
                language = "en"
            };
            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                if (cryptobox.is_paid())
                {
                    if (!cryptobox.is_processed())
                    {
                        ViewBag.Message = "Thank you (order #" + options.orderID + ", payment #" +
                                          cryptobox.payment_id() + "). We upgraded your membership to Premium";
                        cryptobox.set_status_processed();
                    }
                    else
                    {
                        ViewBag.Message = "You have a Premium Membership";
                    }
                }
                else
                {
                    ViewBag.Coins = available_payments;
                    ViewBag.DefCoin = defPaiment;
                    ViewBag.DefLang = options.language;
                }

                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();

                return View(model);
            }
        }

        public ActionResult PayPerPage()
        {
            OptionsModel options = new OptionsModel()
            {
                public_key = "-your public key for Bitcoin box-",
                private_key = "-your private key for Bitcoin box-",
                webdev_key = "",
                orderID = "page1",
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = (decimal)0.01,
                period = "24 HOUR",
                language = "en"
            };
            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();

                return View(model);
            }
        }

        public ActionResult PayPerPageMulti()
        {
            string defPaiment = "bitcoin";

            string[] available_payments = new[] { "bitcoin", "dogecoin" };
            IDictionary<string, IDictionary<string, string>> all_keys = new Dictionary
                <string, IDictionary<string, string>>()
            {
                {
                    "bitcoin", new Dictionary<string, string>()
                    {
                        {"public_key", "-your public key for Bitcoin box-"},
                        {"private_key", "-your private key for Bitcoin box-"}
                    }
                },
                {
                    "dogecoin", new Dictionary<string, string>()
                    {
                        {"public_key", "-your public key for Dogecoin box-"},
                        {"private_key", "-your private key for Dogecoin box-"}
                    }
                }
            };

            foreach (KeyValuePair<string, IDictionary<string, string>> valuePair in all_keys)
            {
                if (valuePair.Value["public_key"] == null || valuePair.Value["private_key"] == null ||
                    valuePair.Value["public_key"] == "" || valuePair.Value["private_key"] == "")
                {
                    return
                        new ContentResult()
                        {
                            Content = "Please add your public/private keys for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!valuePair.Value["public_key"].Contains("PUB"))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Invalid public key for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!valuePair.Value["private_key"].Contains("PRV"))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Invalid private key for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!ConfigurationManager.AppSettings["PrivateKeys"].Contains(valuePair.Value["private_key"]))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Please add your private key for " + valuePair.Key +
                                      " in Web.config."
                        };
                }
            }

            string coinName = CryptoHelper.cryptobox_selcoin(available_payments, defPaiment);
            OptionsModel options = new OptionsModel()
            {
                public_key = all_keys[coinName]["public_key"],
                private_key = all_keys[coinName]["private_key"],
                webdev_key = "",
                orderID = "page1",
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = (decimal)0.01,
                period = "24 HOUR",
                language = "en"
            };
            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                if (!cryptobox.is_paid())
                {
                    ViewBag.Coins = available_payments;
                    ViewBag.DefCoin = defPaiment;
                    ViewBag.DefLang = options.language;
                }

                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();

                return View(model);
            }
        }

        public ActionResult PayPerPost()
        {
            OptionsModel options = new OptionsModel()
            {
                public_key = "-your public key for Bitcoin box-",
                private_key = "-your private key for Bitcoin box-",
                webdev_key = "",
                orderID = "post1",
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = (decimal)0.01,
                period = "NOEXPIRY",
                language = "en"
            };

            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                string ftitle = Request.Form["ftitle"] ?? "";
                string ftext = Request.Form["ftext"] ?? "";
                ViewBag.error = "";
                ViewBag.successful = false;
                ViewBag.ftitle = ftitle;
                ViewBag.ftext = ftext;

                if (Request.Form["ftitle"] != null && Request.Form["ftext"] != null)
                {
                    if (ftitle == "")
                        ViewBag.error += "<li>Please enter Title</li>";
                    if (ftext == "")
                        ViewBag.error += "<li>Please enter Text</li>";
                    if (!cryptobox.is_paid())
                        ViewBag.error += "<li>" + cryptobox.coin_name() + "s have not yet been received</li>";
                    if (ViewBag.error != "")
                        ViewBag.error = "<br><ul style='color:#eb4847'>" + ViewBag.error + "</ul>";

                    if (cryptobox.is_paid() && ViewBag.error == "")
                    {
                        ViewBag.successful = true;
                        cryptobox.set_status_processed();
                        cryptobox.cryptobox_reset();
                    }
                }

                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();

                return View(model);
            }
        }

        public ActionResult PayPerPostMulti()
        {
            string defPaiment = "bitcoin";

            string[] available_payments = new[] { "bitcoin", "dogecoin" };
            IDictionary<string, IDictionary<string, string>> all_keys = new Dictionary
                <string, IDictionary<string, string>>()
            {
                {
                    "bitcoin", new Dictionary<string, string>()
                    {
                        {"public_key", "-your public key for Bitcoin box-"},
                        {"private_key", "-your private key for Bitcoin box-"}
                    }
                },
                {
                    "dogecoin", new Dictionary<string, string>()
                    {
                        {"public_key", "-your public key for Dogecoin box-"},
                        {"private_key", "-your private key for Dogecoin box-"}
                    }
                }
            };

            foreach (KeyValuePair<string, IDictionary<string, string>> valuePair in all_keys)
            {
                if (valuePair.Value["public_key"] == null || valuePair.Value["private_key"] == null ||
                    valuePair.Value["public_key"] == "" || valuePair.Value["private_key"] == "")
                {
                    return
                        new ContentResult()
                        {
                            Content = "Please add your public/private keys for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!valuePair.Value["public_key"].Contains("PUB"))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Invalid public key for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!valuePair.Value["private_key"].Contains("PRV"))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Invalid private key for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!ConfigurationManager.AppSettings["PrivateKeys"].Contains(valuePair.Value["private_key"]))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Please add your private key for " + valuePair.Key +
                                      " in Web.config."
                        };
                }
            }

            string coinName = CryptoHelper.cryptobox_selcoin(available_payments, defPaiment);
            OptionsModel options = new OptionsModel()
            {
                public_key = all_keys[coinName]["public_key"],
                private_key = all_keys[coinName]["private_key"],
                webdev_key = "",
                orderID = "post1",
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = (decimal)0.01,
                period = "NOEXPIRY",
                language = "en"
            };
            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                ViewBag.Coins = available_payments;
                ViewBag.DefCoin = defPaiment;
                ViewBag.DefLang = options.language;

                string ftitle = Request.Form["ftitle"] ?? "";
                string ftext = Request.Form["ftext"] ?? "";
                ViewBag.error = "";
                ViewBag.successful = false;
                ViewBag.ftitle = ftitle;
                ViewBag.ftext = ftext;

                if (Request.Form["ftitle"] != null && Request.Form["ftext"] != null)
                {
                    if (ftitle == "")
                        ViewBag.error += "<li>Please enter Title</li>";
                    if (ftext == "")
                        ViewBag.error += "<li>Please enter Text</li>";
                    if (!cryptobox.is_paid())
                        ViewBag.error += "<li>" + cryptobox.coin_name() + "s have not yet been received</li>";
                    if (ViewBag.error != "")
                        ViewBag.error = "<br><ul style='color:#eb4847'>" + ViewBag.error + "</ul>";

                    if (cryptobox.is_paid() && ViewBag.error == "")
                    {
                        ViewBag.successful = true;
                        cryptobox.set_status_processed();
                        cryptobox.cryptobox_reset();
                    }
                }

                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();

                return View(model);
            }
        }

        public ActionResult PayPerProduct()
        {
            OptionsModel options = new OptionsModel()
            {
                public_key = "-your public key for Bitcoin box-",
                private_key = "-your private key for Bitcoin box-",
                webdev_key = "",
                orderID = "invoice000383",
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = (decimal)0.01,
                period = "NOEXPIRY",
                language = "en"
            };
            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                if (cryptobox.is_paid())
                {
                    if (!cryptobox.is_confirmed())
                    {
                        ViewBag.message = "Thank you for order (order #" + options.orderID + ", payment #" + cryptobox.payment_id() +
                                          "). Awaiting transaction/payment confirmation";
                    }
                    else
                    {
                        if (!cryptobox.is_processed())
                        {
                            ViewBag.message = "Thank you for order (order #" + options.orderID + ", payment #" + cryptobox.payment_id() + "). Payment Confirmed<br/> (User will see this message one time after payment has been made)";
                            cryptobox.set_status_processed();
                        }
                        else
                        {
                            ViewBag.message = "Thank you for order (order #" + options.orderID + ", payment #" + cryptobox.payment_id() + "). Payment Confirmed<br/> (User will see this message during " + options.period + " period after payment has been made)";
                        }
                    }
                }
                else
                {
                    ViewBag.message = "This invoice has not been paid yet";
                }

                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();

                return View(model);
            }
        }

        public ActionResult PayPerProductMulti()
        {
            string defPaiment = "bitcoin";

            string[] available_payments = new[] { "bitcoin", "dogecoin" };
            IDictionary<string, IDictionary<string, string>> all_keys = new Dictionary
                <string, IDictionary<string, string>>()
            {
                {
                    "bitcoin", new Dictionary<string, string>()
                    {
                        {"public_key", "-your public key for Bitcoin box-"},
                        {"private_key", "-your private key for Bitcoin box-"}
                    }
                },
                {
                    "dogecoin", new Dictionary<string, string>()
                    {
                        {"public_key", "-your public key for Dogecoin box-"},
                        {"private_key", "-your private key for Dogecoin box-"}
                    }
                }
            };

            foreach (KeyValuePair<string, IDictionary<string, string>> valuePair in all_keys)
            {
                if (valuePair.Value["public_key"] == null || valuePair.Value["private_key"] == null ||
                    valuePair.Value["public_key"] == "" || valuePair.Value["private_key"] == "")
                {
                    return
                        new ContentResult()
                        {
                            Content = "Please add your public/private keys for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!valuePair.Value["public_key"].Contains("PUB"))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Invalid public key for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!valuePair.Value["private_key"].Contains("PRV"))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Invalid private key for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!ConfigurationManager.AppSettings["PrivateKeys"].Contains(valuePair.Value["private_key"]))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Please add your private key for " + valuePair.Key +
                                      " in Web.config."
                        };
                }
            }

            string coinName = CryptoHelper.cryptobox_selcoin(available_payments, defPaiment);
            OptionsModel options = new OptionsModel()
            {
                public_key = all_keys[coinName]["public_key"],
                private_key = all_keys[coinName]["private_key"],
                webdev_key = "",
                orderID = "invoice000383",
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = (decimal)0.01,
                period = "NOEXPIRY",
                language = "en"
            };
            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                ViewBag.Coins = available_payments;
                ViewBag.DefCoin = defPaiment;
                ViewBag.DefLang = options.language;

                if (cryptobox.is_paid())
                {
                    if (!cryptobox.is_confirmed())
                    {
                        ViewBag.message = "Thank you for order (order #" + options.orderID + ", payment #" + cryptobox.payment_id() +
                                          "). Awaiting transaction/payment confirmation";
                    }
                    else
                    {
                        if (!cryptobox.is_processed())
                        {
                            ViewBag.message = "Thank you for order (order #" + options.orderID + ", payment #" + cryptobox.payment_id() + "). Payment Confirmed<br/> (User will see this message one time after payment has been made)";
                            cryptobox.set_status_processed();
                        }
                        else
                        {
                            ViewBag.message = "Thank you for order (order #" + options.orderID + ", payment #" + cryptobox.payment_id() + "). Payment Confirmed<br/> (User will see this message during " + options.period + " period after payment has been made)";
                        }
                    }
                }
                else
                {
                    ViewBag.message = "This invoice has not been paid yet";
                }

                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();

                return View(model);
            }
        }

        public ActionResult PayPerRegistration()
        {
            OptionsModel options = new OptionsModel()
            {
                public_key = "-your public key for Bitcoin box-",
                private_key = "-your private key for Bitcoin box-",
                webdev_key = "",
                orderID = "signuppage",
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = (decimal)0.01,
                period = "NOEXPIRY",
                language = "en"
            };
            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                string fname = Request.Form["fname"] ?? "";
                string femail = Request.Form["femail"] ?? "";
                string fpassword = Request.Form["fpassword"] ?? "";
                ViewBag.error = "";
                ViewBag.successful = false;
                ViewBag.fname = fname;
                ViewBag.femail = femail;
                ViewBag.fpassword = fpassword;

                if (Request.Form["fname"] != null && Request.Form["fname"] != "")
                {
                    if (fname == "")
                        ViewBag.error += "<li>Please enter Your Name</li>";
                    if (femail == "")
                        ViewBag.error += "<li>Please enter Your Email</li>";
                    if (fpassword == "")
                        ViewBag.error += "<li>Please enter Your Password</li>";
                    if (!cryptobox.is_paid())
                        ViewBag.error += "<li>" + cryptobox.coin_name() + "s have not yet been received</li>";
                    if (ViewBag.error != "")
                        ViewBag.error = "<br><ul style='color:#eb4847'>" + ViewBag.error + "</ul>";

                    if (cryptobox.is_paid() && ViewBag.error == "")
                    {
                        ViewBag.successful = true;
                        cryptobox.set_status_processed();
                        cryptobox.cryptobox_reset();
                    }
                }

                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();

                return View(model);
            }
        }

        public ActionResult PayPerRegistrationMulti()
        {
            string defPaiment = "bitcoin";

            string[] available_payments = new[] { "bitcoin", "dogecoin" };
            IDictionary<string, IDictionary<string, string>> all_keys = new Dictionary
                <string, IDictionary<string, string>>()
            {
                {
                    "bitcoin", new Dictionary<string, string>()
                    {
                        {"public_key", "-your public key for Bitcoin box-"},
                        {"private_key", "-your private key for Bitcoin box-"}
                    }
                },
                {
                    "dogecoin", new Dictionary<string, string>()
                    {
                        {"public_key", "-your public key for Dogecoin box-"},
                        {"private_key", "-your private key for Dogecoin box-"}
                    }
                }
            };

            foreach (KeyValuePair<string, IDictionary<string, string>> valuePair in all_keys)
            {
                if (valuePair.Value["public_key"] == null || valuePair.Value["private_key"] == null ||
                    valuePair.Value["public_key"] == "" || valuePair.Value["private_key"] == "")
                {
                    return
                        new ContentResult()
                        {
                            Content = "Please add your public/private keys for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!valuePair.Value["public_key"].Contains("PUB"))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Invalid public key for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!valuePair.Value["private_key"].Contains("PRV"))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Invalid private key for " + valuePair.Key +
                                      " in all_keys variable"
                        };
                }
                else if (!ConfigurationManager.AppSettings["PrivateKeys"].Contains(valuePair.Value["private_key"]))
                {
                    return
                        new ContentResult()
                        {
                            Content = "Please add your private key for " + valuePair.Key +
                                      " in Web.config."
                        };
                }
            }

            string coinName = CryptoHelper.cryptobox_selcoin(available_payments, defPaiment);
            OptionsModel options = new OptionsModel()
            {
                public_key = all_keys[coinName]["public_key"],
                private_key = all_keys[coinName]["private_key"],
                webdev_key = "",
                orderID = "signuppage",
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = (decimal)0.01,
                period = "NOEXPIRY",
                language = "en"
            };
            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                ViewBag.Coins = available_payments;
                ViewBag.DefCoin = defPaiment;
                ViewBag.DefLang = options.language;

                string fname = Request.Form["fname"] ?? "";
                string femail = Request.Form["femail"] ?? "";
                string fpassword = Request.Form["fpassword"] ?? "";
                ViewBag.error = "";
                ViewBag.successful = false;
                ViewBag.fname = fname;
                ViewBag.femail = femail;
                ViewBag.fpassword = fpassword;

                if (Request.Form["fname"] != null && Request.Form["fname"] != "")
                {
                    if (fname == "")
                        ViewBag.error += "<li>Please enter Your Name</li>";
                    if (femail == "")
                        ViewBag.error += "<li>Please enter Your Email</li>";
                    if (fpassword == "")
                        ViewBag.error += "<li>Please enter Your Password</li>";
                    if (!cryptobox.is_paid())
                        ViewBag.error += "<li>" + cryptobox.coin_name() + "s have not yet been received</li>";
                    if (ViewBag.error != "")
                        ViewBag.error = "<br><ul style='color:#eb4847'>" + ViewBag.error + "</ul>";

                    if (cryptobox.is_paid() && ViewBag.error == "")
                    {
                        ViewBag.successful = true;
                        cryptobox.set_status_processed();
                        cryptobox.cryptobox_reset();
                    }
                }

                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();

                return View(model);
            }
        }

        public ActionResult Payments()
        {
            GoUrlEntities Context = new GoUrlEntities();
            IEnumerable<crypto_payments> payments = Context.crypto_payments.OrderByDescending(x => x.recordCreated).Take(100);
            return View(payments);
        }

        public ActionResult PayPerJson()
        {
            OptionsModel options = new OptionsModel()
            {
                public_key = "-your gourl.io public key for Bitcoin/Dogecoin/etc box-",
                private_key = "-your gourl.io private key for Bitcoin/Dogecoin/etc box-",
                webdev_key = "",
                orderID = "invoice22",
                userID = "",
                userFormat = "COOKIE",
                amount = 0,
                amountUSD = (decimal)0.01,
                period = "NOEXPIRY",
                language = "en"
            };
            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                ViewBag.JsonUrl = cryptobox.cryptobox_json_url();
                ViewBag.Message = "";
                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();
                if (HttpContext.Request.Form["cryptobox_refresh_"] != null)
                {
                    ViewBag.Message = "<div class='gourl_msg'>";
                    if (cryptobox.is_paid())
                    {
                        ViewBag.Message += "<div style=\"margin:50px\" class=\"well\"><i class=\"fa fa-info-circle fa-3x fa-pull-left fa-border\" aria-hidden=\"true\"></i> " + Controls.localisation[model.language].MsgNotReceived.Replace("%coinName%", model.coinName)
                        .Replace("%coinNames%", model.coinLabel == "BCH" || model.coinLabel == "DASH" ? model.coinName : model.coinName + "s")
                        .Replace("%coinLabel%", model.coinLabel) + "</div>";
                    }else if (cryptobox.is_processed())
                    {
                        ViewBag.Message += "<div style=\"margin:70px\" class=\"alert alert-success\" role=\"alert\"> " + (model.boxType == "paymentbox"
                        ? Controls.localisation[model.language].MsgReceived
                        : Controls.localisation[model.language].MsgReceived2)
                        .Replace("%coinName%", model.coinName)
                        .Replace("%coinLabel%", model.coinLabel)
                        .Replace("%amountPaid%", model.amoutnPaid.ToString()) + "</div>";
                        cryptobox.set_status_processed();
                    }
                    ViewBag.Message = "</div>";
                }



                return View(model);
            }
        }
    }
}


