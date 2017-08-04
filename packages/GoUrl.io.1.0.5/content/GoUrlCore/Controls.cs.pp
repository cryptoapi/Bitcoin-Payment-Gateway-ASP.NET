using System;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.WebPages;
using $rootnamespace$.Models.GoUrl;

namespace $rootnamespace$.GoUrlCore
{
    public static partial class Controls
    {
	private static readonly string[] CryptoboxCoins = { "bitcoin", "bitcoincash", "litecoin", "dash", "dogecoin", "speedcoin", "reddcoin", "potcoin", "feathercoin", "vertcoin", "peercoin", "monetaryunit" };

        /// <summary>
        /// Display Cryptocoin Payment Box; the cryptobox will automatically displays successful message if payment has been received
        /// 
        /// Usually user will see on bottom of payment box button 'Click Here if you have already sent coins' (when submit_btn = true) 
        /// and when they click on that button, script will connect to our remote cryptocoin payment box server
        /// and check user payment.
        ///  
        /// As backup, our server will also inform your server automatically through IPN every time a payment is received
        /// (Action Callback in GoUrlController). I.e. if the user does not click on the button or you have not displayed the button, 
        /// your website will receive a notification about a given user anyway and save it to your database.

        /// Next time your user goes to your website/reloads page they will automatically see the message 
        /// that their payment has been received successfully.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="model"></param>
        /// <param name="submit_btn"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="box_style"></param>
        /// <param name="message_style"></param>
        /// <param name="anchor"></param>
        /// <returns></returns>
        public static MvcHtmlString Cryptobox(this HtmlHelper helper, DisplayCryptoboxModel model,
            bool submit_btn = true, int width = 540, int height = 230, object box_style = null, object message_style = null, string anchor = "")
        {
            Random rand = new Random();

            if (box_style == null || box_style.ToString().IsEmpty())
            {
                box_style = new { style = "border-radius:15px;box-shadow:0 0 12px #aaa;-moz-box-shadow:0 0 12px #aaa;-webkit-box-shadow:0 0 12px #aaa;padding:3px 6px;margin:10px" };
            }
            if (message_style == null || message_style.ToString().IsEmpty())
            {
                message_style = new { style = "display:inline-block;max-width:580px;padding:15px 20px;box-shadow:0 0 10px #aaa;-moz-box-shadow: 0 0 10px #aaa;margin:7px;font-size:13px;font-weight:normal;line-height:21px;font-family: Verdana, Arial, Helvetica, sans-serif;" };
            }

            string hashResult = Calculator.md5(model.boxID + model.coinName + model.public_key + model.private_key + model.webdev_key + model.amount.ToString(CultureInfo.InvariantCulture) + model.period + model.amountUSD.ToString(CultureInfo.InvariantCulture) + model.language + model.amount.ToString(CultureInfo.InvariantCulture) + model.iframeID + model.amountUSD.ToString(CultureInfo.InvariantCulture) + model.userID + model.userFormat + model.orderID + width + height);
            string val = Calculator.md5(model.iframeID + model.private_key + model.userID);
            string result = "";

            if (submit_btn && helper.ViewContext.HttpContext.Request["cryptobox_live_"] != null &&
                helper.ViewContext.HttpContext.Request["cryptobox_live_"] == val)
            {
                string id = Calculator.md5(rand.Next().ToString());
                if (!model.is_paid)
                {
                    TagBuilder aBuilder = new TagBuilder("a");
                    aBuilder.MergeAttribute("name", "c" + model.iframeID);
                    aBuilder.GenerateId("c" + model.iframeID);
                    result += aBuilder;
                }
                result += "<br />";
                TagBuilder div00 = new TagBuilder("div");
                div00.MergeAttribute("align", "center");
                div00.MergeAttribute("id", id);
                TagBuilder div0 = new TagBuilder("div");
                string[] ln = { "ar", "fa" };
                if (ln.Contains(model.language))
                    div0.MergeAttribute("dir", "rtl");
                div0.MergeAttributes(new RouteValueDictionary(message_style));
                if (model.is_paid)
                {
                    TagBuilder spanBuilder = new TagBuilder("span");
                    spanBuilder.MergeAttribute("style", "color:#339e2e;white-space:nowrap;");
                    spanBuilder.InnerHtml = (model.boxType == "paymentbox"
                        ? localisation[model.language].MsgReceived
                        : localisation[model.language].MsgReceived2)
                        .Replace("%coinName%", model.coinName)
                        .Replace("%coinLabel%", model.coinLabel == "BCH" || model.coinLabel == "DASH" ? model.coinName : model.coinName + "s")
                        .Replace("%amountPaid%", model.amoutnPaid.ToString());

                    div0.InnerHtml = spanBuilder.ToString();
                }
                else
                {
                    TagBuilder spanBuilder = new TagBuilder("span");
                    spanBuilder.MergeAttribute("style", "color:#eb4847");
                    spanBuilder.InnerHtml = localisation[model.language].MsgNotReceived
                        .Replace("%coinName%", model.coinName)
                        .Replace("%coinNames%", model.coinLabel == "BCH" || model.coinLabel == "DASH" ? model.coinName : model.coinName + "s")
                        .Replace("%coinLabel%", model.coinLabel == "BCH" || model.coinLabel == "DASH" ? model.coinName : model.coinName + "s");

                    TagBuilder scrptBuilder = new TagBuilder("script");
                    scrptBuilder.MergeAttribute("type", "text/javascript");
                    scrptBuilder.InnerHtml = "cryptobox_msghide('" + id + "')";

                    div0.InnerHtml = spanBuilder.ToString() + scrptBuilder.ToString();
                }

                div00.InnerHtml = div0.ToString();
                result += div00;
            }

            TagBuilder div1 = new TagBuilder("div");
            div1.MergeAttribute("align", "center");
            div1.Attributes["style"] = "min-width:" + width + "px";

            TagBuilder iframeBuilder = new TagBuilder("iframe");
            iframeBuilder.GenerateId(model.iframeID);
            iframeBuilder.MergeAttributes(new RouteValueDictionary(box_style));
            iframeBuilder.MergeAttribute("scrolling", "no");
            iframeBuilder.MergeAttribute("marginheight", "0");
            iframeBuilder.MergeAttribute("marginwidth", "0");
            iframeBuilder.MergeAttribute("frameborder", "0");
            iframeBuilder.MergeAttribute("width", width.ToString());
            iframeBuilder.MergeAttribute("height", height.ToString());
            div1.InnerHtml = iframeBuilder.ToString();

            result += div1;

            TagBuilder div2 = new TagBuilder("div");
            TagBuilder scriptBuilder = new TagBuilder("script");
            scriptBuilder.MergeAttribute("type", "text/javascript");
            scriptBuilder.InnerHtml = "cryptobox_show(" + model.boxID + ", '" + model.coinName + "', '" + model.public_key + "', " + model.amount.ToString(CultureInfo.InvariantCulture) + ", " + model.amountUSD.ToString(CultureInfo.InvariantCulture) + ", '" + model.period + "', '" + model.language + "', '" + model.iframeID + "', '" + model.userID + "', '" + model.userFormat + "', '" + model.orderID + "', '" + model.cookieName + "', '" + model.webdev_key + "', '" + hashResult + "', " + width + ", " + height + "); ";
            div2.InnerHtml = scriptBuilder.ToString();

            result += div2.ToString();

            if (submit_btn && !model.is_paid)
            {
                TagBuilder formBuilder = new TagBuilder("form");

                formBuilder.MergeAttribute("action",
                                    helper.ViewContext.HttpContext.Request.RawUrl + "#" +
                                    (anchor != String.Empty ? anchor : "c") + model.iframeID);
                formBuilder.MergeAttribute("method", "post");
                TagBuilder inputBuilder = new TagBuilder("input");
                inputBuilder.MergeAttribute("type", "hidden");
                inputBuilder.GenerateId("cryptobox_live_");
                inputBuilder.MergeAttribute("name", "cryptobox_live_");
                inputBuilder.MergeAttribute("value", val);
                TagBuilder div3 = new TagBuilder("div");
                div3.MergeAttribute("align", "center");
                TagBuilder buttonBuilder = new TagBuilder("button");
                if (model.language == "ar" || model.language == "fa")
                {
                    buttonBuilder.MergeAttribute("dir", "rtl");

                }
                buttonBuilder.MergeAttribute("style", "color:#555;border-color:#ccc;background:#f7f7f7;-webkit-box-shadow:inset 0 1px 0 #fff,0 1px 0 rgba(0,0,0,.08);box-shadow:inset 0 1px 0 #fff,0 1px 0 rgba(0,0,0,.08);vertical-align:top;display:inline-block;text-decoration:none;font-size:13px;line-height:26px;min-height:28px;margin:20px 0 25px 0;padding:0 10px 1px;cursor:pointer;border-width:1px;border-style:solid;-webkit-appearance:none;-webkit-border-radius:3px;border-radius:3px;white-space:nowrap;-webkit-box-sizing:border-box;-moz-box-sizing:border-box;box-sizing:border-box;font-family:\"Open Sans\",sans-serif;font-size: 13px;font-weight: normal;text-transform: none;");
                buttonBuilder.InnerHtml = localisation[model.language].Button
                        .Replace("%coinName%", model.coinName)
                        .Replace("%coinNames%", model.coinLabel == "BCH" || model.coinLabel == "DASH" ? model.coinName : model.coinName + "s")
                        .Replace("%coinLabel%", model.coinLabel == "BCH" || model.coinLabel == "DASH" ? model.coinName : model.coinName + "s") +
                        (model.language != "ar" ? " &#187;" : "") + " &#160;";

                div3.InnerHtml = buttonBuilder.ToString();
                formBuilder.InnerHtml = inputBuilder.ToString() + div3.ToString();

                result += formBuilder.ToString();
            }

            return MvcHtmlString.Create(result + "<br/>");
        }

        /// <summary>
        /// Language selection dropdown list for cryptocoin payment box
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="defaultLanguage"></param>
        /// <param name="anchor"></param>
        /// <returns></returns>
        public static MvcHtmlString LanguageBox(this HtmlHelper helper, string defaultLanguage = "en",
            string anchor = "gourlcryptolang")
        {
            defaultLanguage = defaultLanguage.ToLower();
            string id = "gourlcryptolang";
            string lan = "en";
            var query = HttpUtility.ParseQueryString(HttpContext.Current.Request.Url.Query);
            if (HttpContext.Current.Request.QueryString[id] != null &&
                HttpContext.Current.Request.QueryString[id] != "" &&
                localisation.ContainsKey(HttpContext.Current.Request.QueryString[id]))
            {
                lan = HttpContext.Current.Request.QueryString[id];
                HttpContext.Current.Response.Cookies.Add(new HttpCookie(id, lan) { Expires = DateTime.Now + TimeSpan.FromDays(7) });
                query.Remove(id);
            }
            else if (HttpContext.Current.Request.Cookies[id] != null &&
                      HttpContext.Current.Request.Cookies[id].Value != "" &&
                      localisation.ContainsKey(HttpContext.Current.Request.Cookies[id].Value))
            {
                lan = HttpContext.Current.Request.Cookies[id].Value;
            }
            else if (localisation.ContainsKey(defaultLanguage))
            {
                lan = defaultLanguage;
            }
            string url = HttpContext.Current.Request.Url.AbsoluteUri.Split(new[] { '?' })[0];
            TagBuilder selectBuilder = new TagBuilder("select");
            selectBuilder.MergeAttribute("name", id);
            selectBuilder.MergeAttribute("id", id);

            selectBuilder.MergeAttribute("onchange", "window.open(\"" + url + "?" +
                query.ToString() +
                (query.Count > 0 ? "&" : "") + id +
                "=\"+this.options[this.selectedIndex].value+\"#" + anchor + "\",\"_self\")");
            foreach (string key in localisation.Keys)
            {
                TagBuilder optionsBuilder = new TagBuilder("option");
                if (key == lan)
                    optionsBuilder.MergeAttribute("selected", "selected");
                optionsBuilder.MergeAttribute("value", key);
                optionsBuilder.InnerHtml = localisation[key].Name;
                selectBuilder.InnerHtml += optionsBuilder.ToString();
            }

            return MvcHtmlString.Create(selectBuilder.ToString());
        }

        /// <summary>
        /// Multiple crypto currency selection list. You can accept payments in multiple crypto currencies
        /// For example you can accept payments in bitcoin, litecoin, dogecoin and use the same price in USD
        /// </summary>
        /// <param name="coins"></param>
        /// <param name="defCoin"></param>
        /// <param name="defLang"></param>
        /// <param name="iconWidth"></param>
        /// <param name="style"></param>
        /// <param name="directory"></param>
        /// <param name="anchor"></param>
        /// <returns></returns>
        public static MvcHtmlString CurrencyBox(this HtmlHelper helper, string[] coins, string defCoin = "", string defLang = "en", int iconWidth = 50,
            object style = null, string directory = "images", string anchor = "gourlcryptocoins")
        {
            if (style == null || style.ToString().IsEmpty())
            {
                style = new { style = "width:350px; margin: 10px 0 10px 320px;" };
            }
            if (coins.Length == 0)
                return MvcHtmlString.Empty;
            defCoin = defCoin.ToLower();
            defLang = defLang.ToLower();

            if (!CryptoboxCoins.Contains(defCoin))
            {
                return MvcHtmlString.Create("Invalid your default value " + defCoin + " in CurrencyBox");
            }
            if (!coins.Contains(defCoin))
                coins = new[] { defCoin };

            var query = HttpUtility.ParseQueryString(HttpContext.Current.Request.Url.Query);
            if (HttpContext.Current.Request.QueryString["gourlcryptocoin"] != null &&
                HttpContext.Current.Request.QueryString["gourlcryptocoin"] != "")
            {
                query.Remove("gourlcryptocoin");
            }

            string url = HttpContext.Current.Request.Url.AbsoluteUri.Split(new[] {'?'})[0] +
                         "?" + query.ToString() + (query.Count > 0 ? "&" : "") + "gourlcryptocoin=";

            string id = "gourlcryptolang";
            string lan = defLang;
            if (HttpContext.Current.Request.QueryString[id] != null &&
                HttpContext.Current.Request.QueryString[id] != "" &&
                localisation.ContainsKey(HttpContext.Current.Request.QueryString[id]))
            {
                lan = HttpContext.Current.Request.QueryString[id];
            }
            else if (HttpContext.Current.Request.Cookies[id] != null &&
                      HttpContext.Current.Request.Cookies[id].Value != "" &&
                      localisation.ContainsKey(HttpContext.Current.Request.Cookies[id].Value))
            {
                lan = HttpContext.Current.Request.Cookies[id].Value;
            }
            id = "gourlcryptocoins";

            TagBuilder divBuilder = new TagBuilder("div");
            divBuilder.MergeAttribute("id", id);
            divBuilder.MergeAttribute("align", "center");
            divBuilder.MergeAttributes(new RouteValueDictionary(style));
            divBuilder.InnerHtml = "<div style='margin - bottom:15px'><b>" + localisation[lan].Payment + " -</b></div>";

            foreach (string coin1 in coins)
            {
                string coin = coin1.ToLower();
                if (!CryptoboxCoins.Contains(coin))
                {
                    return MvcHtmlString.Create("Invalid your submitted value coin in CurrencyBox");
                }
                TagBuilder aBuilder = new TagBuilder("a");
                aBuilder.MergeAttribute("href", url + coin + "#" + anchor);
                TagBuilder imgBuilder = new TagBuilder("img");
                imgBuilder.MergeAttribute("style", "box-shadow:none;margin:" + Math.Round((decimal)(iconWidth / 10)) + "px " + Math.Round((decimal)(iconWidth / 7)) + "px;border:0;");
                imgBuilder.MergeAttribute("width", iconWidth.ToString());
                imgBuilder.MergeAttribute("title", localisation[lan].PayIn.Replace("%coinName%", coin1));
                imgBuilder.MergeAttribute("alt", localisation[lan].PayIn.Replace("%coinName%", coin1));
                imgBuilder.MergeAttribute("src", "/" + directory + "//" + coin + (iconWidth > 70 ? "2" : "") + ".png");
                aBuilder.InnerHtml = imgBuilder.ToString();
                divBuilder.InnerHtml += aBuilder.ToString();
            }

            return MvcHtmlString.Create(divBuilder.ToString());
        }
    }
}