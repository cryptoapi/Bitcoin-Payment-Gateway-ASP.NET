using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using System.Web.WebPages;
using Gourl.Models.GoUrl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gourl.GoUrlCore
{
    public static partial class Controls
    {
        private static readonly string[] CryptoboxCoins = { "bitcoin", "bitcoincash", "bitcoinsv", "litecoin", "dash", "dogecoin", "speedcoin", "reddcoin", "potcoin", "feathercoin", "vertcoin", "peercoin", "monetaryunit", "universalcurrency" };

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

            string hashResult = Calculator.cryptobox_hash(model, false, width, height);
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
                    spanBuilder.InnerHtml = payment_status_text(model);
                    div0.InnerHtml = spanBuilder.ToString();
                }
                else
                {
                    TagBuilder spanBuilder = new TagBuilder("span");
                    spanBuilder.MergeAttribute("style", "color:#eb4847");
                    spanBuilder.InnerHtml = payment_status_text(model);

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
                        .Replace("%coinNames%", model.coinLabel == "BCH" || model.coinLabel == "BSV" || model.coinLabel == "DASH" ? model.coinName : model.coinName + "s")
                        .Replace("%coinLabel%", model.coinLabel) +
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
            string anchor = "gourlcryptolang", bool no_bootstrap = true)
        {
            defaultLanguage = defaultLanguage.ToLower();
            string id = ConfigurationManager.AppSettings["CryptoboxLanguageHTMLId"] ?? "gourlcryptolang";
            string lan = CryptoHelper.cryptobox_sellanguage(defaultLanguage);
            var query = HttpUtility.ParseQueryString(HttpContext.Current.Request.Url.Query);
            if (HttpContext.Current.Request.QueryString[id] != null &&
                HttpContext.Current.Request.QueryString[id] != "" &&
                localisation.ContainsKey(HttpContext.Current.Request.QueryString[id]))
            {
                query.Remove(id);
            }

            string url = HttpContext.Current.Request.Url.AbsoluteUri.Split(new[] { '?' })[0];
            TagBuilder selectBuilder;
            if (no_bootstrap)
            {
                selectBuilder = new TagBuilder("select");
                selectBuilder.MergeAttribute("name", id);
                selectBuilder.MergeAttribute("id", id);

                selectBuilder.MergeAttribute("onchange", "window.open(\"" + url + "?" +
                                                         query.ToString() +
                                                         (query.Count > 0 ? "&" : "") + id +
                                                         "=\"+this.options[this.selectedIndex].value+\"#" + anchor +
                                                         "\",\"_self\")");
                selectBuilder.MergeAttribute("style", "width:130px;font-family:Arial,Helvetica,sans-serif;font-size:12px;color:#666;border-radius:5px;-moz-border-radius:5px;border: #ccc 1px solid;margin:0;padding:3px 0 3px 6px;white-space:nowrap;overflow:hidden;display:inline;");
                foreach (string key in localisation.Keys)
                {
                    TagBuilder optionsBuilder = new TagBuilder("option");
                    if (key == lan)
                        optionsBuilder.MergeAttribute("selected", "selected");
                    optionsBuilder.MergeAttribute("value", key);
                    optionsBuilder.InnerHtml = localisation[key].Name;
                    selectBuilder.InnerHtml += optionsBuilder.ToString();
                }
            }
            else
            {
                selectBuilder = new TagBuilder("div");
                selectBuilder.MergeAttribute("class", "dropdown-menu");
                foreach (string key in localisation.Keys)
                {
                    TagBuilder aBuilder = new TagBuilder("a");
                    aBuilder.MergeAttribute("href", url + "?" +
                                                         query.ToString() +
                                                         (query.Count > 0 ? "&" : "") + id + "=" + key + "#" + anchor);
                    aBuilder.MergeAttribute("class", "dropdown-item" + (key == lan ? " active" : ""));
                    aBuilder.InnerHtml += localisation[key].Name;
                    selectBuilder.InnerHtml += aBuilder.ToString();
                }
            }

            return MvcHtmlString.Create(selectBuilder.ToString());
        }

        /// <summary>
        /// Multiple crypto currency selection list. You can accept payments in multiple crypto currencies
        /// For example you can accept payments in bitcoin, bitcoincash, litecoin, etc and use the same price in USD
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
            object style = null, string directory = "images", string anchor = "gourlcryptocoins", bool jquery = false)
        {
            if (style == null || style.ToString().IsEmpty())
            {
                style = new { style = "width:350px; margin: 10px 0 10px 320px;" };
            }
            if (coins.Length == 0)
                return MvcHtmlString.Empty;
            defCoin = defCoin.ToLower();
            defLang = defLang.ToLower();
            string id = ConfigurationManager.AppSettings["CryptoboxCoinsHTMLId"] ?? "gourlcryptocoin";

            if (!CryptoboxCoins.Contains(defCoin))
            {
                return MvcHtmlString.Create("Invalid your default value " + defCoin + " in CurrencyBox");
            }
            if (!coins.Contains(defCoin))
                coins = new[] { defCoin };

            string coinName = CryptoHelper.cryptobox_selcoin(coins, defCoin);

            var query = HttpUtility.ParseQueryString(HttpContext.Current.Request.Url.Query);
            if (HttpContext.Current.Request.QueryString[id] != null &&
                HttpContext.Current.Request.QueryString[id] != "")
            {
                query.Remove(id);
            }

            string coin_url = HttpContext.Current.Request.Url.AbsoluteUri.Split(new[] { '?' })[0] +
                         "?" + query.ToString() + (query.Count > 0 ? "&" : "") + id + "=";

            string lan = CryptoHelper.cryptobox_sellanguage(defLang);

            TagBuilder divBuilder = new TagBuilder("div");
            divBuilder.MergeAttribute("id", anchor == "gourlcryptocoins" ? anchor : "");
            divBuilder.MergeAttribute("style", style + " text-align:center");
            divBuilder.InnerHtml = "<div style='margin-bottom:15px'><b>" + localisation[lan].Payment + " -</b></div>";

            foreach (string coin1 in coins)
            {
                string coin = coin1.ToLower();
                if (!CryptoboxCoins.Contains(coin))
                {
                    return MvcHtmlString.Create("Invalid your submitted value coin in CurrencyBox");
                }

                string url = coin_url + coin + "#" + anchor;

                if (jquery)
                {
                    TagBuilder inputBuilder = new TagBuilder("input");
                    inputBuilder.MergeAttribute("type", "radio");
                    inputBuilder.MergeAttribute("class", "aradioimage");
                    inputBuilder.MergeAttribute("data-title", localisation[lan].PayIn.Replace("%coinName%", coin1));
                    if(coinName == coin)
                        inputBuilder.MergeAttribute("checked", "");
                    inputBuilder.MergeAttribute("data-url", url);
                    inputBuilder.MergeAttribute("data-width", iconWidth.ToString());
                    inputBuilder.MergeAttribute("data-alt", localisation[lan].PayIn.Replace("%coinName%", coin1));
                    inputBuilder.MergeAttribute("data-image", "/" + directory + "/" + coin + (iconWidth > 70 ? "2" : "") + ".png");
                    inputBuilder.MergeAttribute("name", "aradioname");
                    inputBuilder.MergeAttribute("value", coin);
                    inputBuilder.InnerHtml += "&#160;";
                    if (iconWidth > 70 && coins.Length < 4)
                    {
                        inputBuilder.InnerHtml += "&#160;";
                    }
                    divBuilder.InnerHtml += inputBuilder.ToString(TagRenderMode.StartTag);
                }
                else
                {
                    TagBuilder aBuilder = new TagBuilder("a");
                    aBuilder.MergeAttribute("href", url);
                    aBuilder.MergeAttribute("onclick", "location.href=" + url);
                    TagBuilder imgBuilder = new TagBuilder("img");
                    imgBuilder.MergeAttribute("style",
                        "box-shadow:none;margin:" + Math.Round((decimal)(iconWidth / 10)) + "px " +
                        Math.Round((decimal)(iconWidth / 6)) + "px;border:0;display:inline;");
                    imgBuilder.MergeAttribute("width", iconWidth.ToString());
                    imgBuilder.MergeAttribute("title", localisation[lan].PayIn.Replace("%coinName%", coin1));
                    imgBuilder.MergeAttribute("alt", localisation[lan].PayIn.Replace("%coinName%", coin1));
                    imgBuilder.MergeAttribute("src",
                        "/" + directory + "/" + coin + (iconWidth > 70 ? "2" : "") + ".png");
                    aBuilder.InnerHtml += imgBuilder.ToString();
                    divBuilder.InnerHtml += aBuilder.ToString();
                }
            }

            return MvcHtmlString.Create(divBuilder.ToString());
        }

        /// <summary>
        /// Return message from $cryptobox_localisation on current user language
        /// message payment received or not; "msg_not_received", "msg_received" or "msg_received2"
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private static string payment_status_text(DisplayCryptoboxModel model)
        {
            string txt = "";

            if (model.is_paid)
            {
                txt = (model.boxType == "paymentbox"
                        ? localisation[model.language].MsgReceived
                        : localisation[model.language].MsgReceived2)
                    .Replace("%coinName%", model.coinName)
                    .Replace("%coinLabel%", model.coinLabel)
                    .Replace("%amountPaid%", model.amoutnPaid.ToString());

            }
            else
            {
                txt = localisation[model.language].MsgNotReceived
                    .Replace("%coinName%", model.coinName)
                    .Replace("%coinNames%", model.coinLabel == "BCH" || model.coinLabel == "BSV" || model.coinLabel == "DASH" ? model.coinName : model.coinName + "s")
                    .Replace("%coinLabel%", model.coinLabel);
            }

            return txt;
        }

        /// <summary>
        /// Show Customize Mobile Friendly Payment Box and automatically displays successful payment message.
        ///  This function use bootstrap4 template; you can use your own template without this function
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="model"></param>
        /// <param name="jsonUrl">Cryptobox.cryptobox_json_url();</param>
        /// <param name="isPaid">Cryptobox.is_paid();</param>
        /// <param name="coins">list of cryptocoins which you accept for payment(bitcoin / litecoin / dash /..)</param>
        /// <param name="defCoin">default coin in payment box</param>
        /// <param name="defLanguage">default language in payment box</param>
        /// <param name="customText">your own text above payment box</param>
        /// <param name="coinImageSize">coin selection list - image sizes; default 70px</param>
        /// <param name="qrcodeSize">QRCode size; default 200px</param>
        /// <param name="showLanguages">show or hide language selection menu above payment box</param>
        /// <param name="logoimgPath">show or hide(when empty value) logo above payment box. You can use default logo or place path to your own logo</param>
        /// <param name="resultimgPath">after payment is received, you can customize successful image in payment box(image with your company text for example)</param>
        /// <param name="resultimgSize">result image size</param>
        /// <param name="redirect">redirect to another page after payment is received (3 seconds delay)</param>
        /// with ajax - user browser receive payment data directly from our server and automatically show successful payment notification message on the page(without page reload, any clicks on buttons).
        /// with curl - User browser receive payment data in json format from your server only; and your server receive json data from our server
        /// </param>
        /// <param name="debug">show raw payment data from gourl.io on the page also, for debug purposes.</param>
        /// <returns></returns>
        public static MvcHtmlString CryptoboxBootstrap(this HtmlHelper helper, DisplayCryptoboxBootstrapModel model,
            string[] coins, string defCoin = "", string defLanguage = "en", string customText = "", int coinImageSize = 70, int qrcodeSize = 200,
            bool showLanguages = true, string logoimgPath = "default", string resultimgPath = "default", int resultimgSize = 250,
            string redirect = "", bool debug = false)
        {
            string result = "";

            customText = CryptoHelper.StripTags(customText, new[] { "p", "a", "br" });

            if (coinImageSize > 200)
                coinImageSize = 70;

            if (qrcodeSize > 500)
                qrcodeSize = 200;

            if (resultimgSize > 500)
                resultimgSize = 250;

            string[] mt = { "ajax", "curl" };
            if (!mt.Contains(model.Method))
                model.Method = "curl";

            string ext = ConfigurationManager.AppSettings["CRYPTOBOX_PREFIX_HTMLID"] ?? "acrypto_";
            string ext2 = "h" + ext.Trim('_', ' ');

            string page_url = HttpContext.Current.Request.Url.AbsoluteUri.Split(new[] { '?' })[0] + "#" + ext2; // Current page url

            var urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);
            string calldir_path = urlHelper.Action("Callback", "GoUrl", null, HttpContext.Current.Request.Url.Scheme).ToString();           
            string imgdir_path = ConfigurationManager.AppSettings["CRYPTOBOX_IMG_FILES_PATH"] ?? "../images/";          // path to directory with coin image files (directory 'images' by default)
            string jsdir_path = ConfigurationManager.AppSettings["CRYPTOBOX_JS_FILES_PATH"] ?? "../scripts/";           // path to directory with files ajax.min.js/support.min.js

            // Language selection list for payment box (html code)
            MvcHtmlString languages_list = new MvcHtmlString("");
            if (showLanguages)
            {
                languages_list = helper.LanguageBox(defLanguage, ext2, false);
            }

            // ---------------------------
            // Bootstrap4 Template Start
            // ----------------------------

            TagBuilder div0Builder = new TagBuilder("div");
            div0Builder.MergeAttribute("class", "bootstrapiso");

            TagBuilder div1Builder = new TagBuilder("div");
            div1Builder.MergeAttribute("id", ext2);
            div1Builder.MergeAttribute("class", ext + "cryptobox_area mncrpt");

            //JQuery Payment Box Script, see https://github.com/cryptoapi/Payment-Gateway/blob/master/js/source/ajax.js
            if (model.Method == "ajax")
            {
                TagBuilder script0 = new TagBuilder("script");
                script0.InnerHtml = "jQuery.getScript(\"" + jsdir_path + "ajax.js\",  function() { " +
                                    "cryptobox_ajax(\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(model.JsonUrl)) 
                                                       + "\", " + (model.CryptoboxModel.is_paid ? 1 : 0)
                                                       + ", " + (model.IsConfirmed ? 1 : 0)
                                                       + ", \"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(calldir_path))
                                                       + "\", \"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(imgdir_path))
                                                       + "\", \"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(logoimgPath))
                                                       + "\", \"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(ext))
                                                       + "\", \"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(redirect)) 
                                                       + "\"); }).done(function(script, textStatus) {console.log(textStatus);})";
                div1Builder.InnerHtml += script0;
            }
            else
            {
                if (model.JsonValues != null)
                {
                    JObject data = model.JsonValues;
                    data.Remove("public_key");
                    data["texts"].Children<JProperty>().FirstOrDefault(x => x.Name == "website")?.Remove();
                    if (data["private_key"] != null)
                        data.Remove("private_key");
                    if (data["private_key_hash"] != null)
                        data.Remove("private_key_hash");
                    data.Remove("data_hash");
                    string json = CryptoHelper.UnEscapeText(JsonConvert.SerializeObject(data));
                    TagBuilder script0 = new TagBuilder("script");
                    script0.InnerHtml += "jQuery(document).ready(function(){ cryptobox_update_page(\""
                                         + Convert.ToBase64String(Encoding.ASCII.GetBytes(json))
                                         + "\", \"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(imgdir_path))
                                         + "\", \"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(logoimgPath))
                                         + "\", \"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(ext))
                                         + "\") })";

                    div1Builder.InnerHtml += script0;
                    if (model.CryptoboxModel.is_paid && redirect != "")
                    {
                        div1Builder.InnerHtml += "<script>setTimeout(function() { window.location = '" + redirect + "'; }, 3000);</script>";
                    }
                    
                }
            }

            // ----------------------------------
            // Text - Pay now + Custom User text 
            // ----------------------------------

            TagBuilder div2Builder = new TagBuilder("div");
            div2Builder.MergeAttribute("class", ext + "header px-3 py-3 pt-md-5 pb-md-4 mx-auto my-4 text-center");
            div2Builder.MergeAttribute("style", "max-width:700px");

            TagBuilder h10Builder = new TagBuilder("h1");
            h10Builder.MergeAttribute("class", "display-4 " + ext + "texts_pay_now");

            TagBuilder span0Builder = new TagBuilder("span");
            span0Builder.MergeAttribute("class", ext + "texts_pay_now");
            span0Builder.InnerHtml = "&#160;";
            h10Builder.InnerHtml = span0Builder.ToString();
            div2Builder.InnerHtml += h10Builder;

            if (!customText.IsEmpty())
            {
                div2Builder.InnerHtml += "<br>";
                if (!customText.Contains("<p"))
                {
                    div2Builder.InnerHtml += "<p class='lead'>" + customText + "</p>";
                }
                else
                {
                    div2Builder.InnerHtml += customText;
                }
            }
            div1Builder.InnerHtml += div2Builder;

            // Coin selection list (bitcoin/litecoin/etc)
            // --------------------

            string coins_list_html = "";
            if (!model.CryptoboxModel.is_paid)
            {
                // Coin selection list (html code)
                string coins_list = CurrencyBox(helper, coins, defCoin, defLanguage, coinImageSize, "margin: 20px 0 80px 0;", imgdir_path, ext2, true).ToString();

                TagBuilder div3Builder = new TagBuilder("div");
                div3Builder.MergeAttribute("class", "container " + ext + "coins_list");
                TagBuilder div4Builder = new TagBuilder("div");
                div4Builder.MergeAttribute("class", "row");
                TagBuilder div5Builder = new TagBuilder("div");
                div5Builder.MergeAttribute("class", "col-12 text-center col-sm-10 offset-sm-1 col-md-8 offset-md-2 text-center");
                div5Builder.InnerHtml = coins_list;
                div4Builder.InnerHtml = div5Builder.ToString();
                div3Builder.InnerHtml = div4Builder.ToString();
                coins_list_html = div3Builder.ToString();
            }

            // ------------------------------
            // Payment Box Ajax Loading ...
            // ------------------------------

            TagBuilder divloaderBuilder = new TagBuilder("div");
            divloaderBuilder.MergeAttribute("class", ext + "loader");
            divloaderBuilder.MergeAttribute("style", "height:700px");
            TagBuilder formBuilder = new TagBuilder("form");
            formBuilder.MergeAttribute("action", page_url);
            formBuilder.MergeAttribute("method", "post");
            TagBuilder divloaderbtnBuilder = new TagBuilder("div");
            divloaderbtnBuilder.MergeAttribute("class", "container text-center " + ext + "loader_button pt-5 mt-5");
            divloaderbtnBuilder.InnerHtml = "<br><br><br><br><br>";
            TagBuilder btnBuilder = new TagBuilder("button");
            btnBuilder.MergeAttribute("type", "submit");
            btnBuilder.MergeAttribute("title", "Click to Reload Page");
            btnBuilder.MergeAttribute("class", "btn btn-outline-secondary btn-lg");
            btnBuilder.InnerHtml = "<i class='fas fa-spinner fa-spin'></i> &#160; " + model.CryptoboxModel.coinName + " " + localisation[model.CryptoboxModel.language].Loading;
            divloaderbtnBuilder.InnerHtml += btnBuilder.ToString();
            formBuilder.InnerHtml += divloaderbtnBuilder.ToString();

            TagBuilder containerBuilder = new TagBuilder("div");
            containerBuilder.MergeAttribute("class", "container");
            TagBuilder divrowBuilder = new TagBuilder("div");
            divrowBuilder.MergeAttribute("class", "row");
            TagBuilder divcolBuilder = new TagBuilder("div");
            divcolBuilder.MergeAttribute("class", "col-12 text-center col-sm-10 offset-sm-1 col-md-8 offset-md-2");
            TagBuilder diverrBuilder = new TagBuilder("div");
            diverrBuilder.MergeAttribute("class", ext + "cryptobox_error");
            diverrBuilder.MergeAttribute("style", "display:none");
            diverrBuilder.InnerHtml += coins_list_html;
            TagBuilder divcardBuilder = new TagBuilder("div");
            divcardBuilder.MergeAttribute("class", "card box-shadow");
            TagBuilder divcardhBuilder = new TagBuilder("div");
            divcardhBuilder.MergeAttribute("class", "card-header");
            TagBuilder h4Builder = new TagBuilder("h4");
            h4Builder.MergeAttribute("class", "my-0 font-weight-normal");
            h4Builder.InnerHtml = "Error Message";
            TagBuilder span1Builder = new TagBuilder("span");
            span1Builder.MergeAttribute("class", "loading_icon mr-3 float-left ");
            span1Builder.MergeAttribute("style", "display:none");
            span1Builder.InnerHtml = "<i class='fas fa-laptop'></i>";
            h4Builder.InnerHtml += span1Builder;
            TagBuilder span2Builder = new TagBuilder("span");
            span2Builder.MergeAttribute("class", "loading_icon mr-3 float-left ");
            span2Builder.MergeAttribute("style", "display:none");
            span2Builder.InnerHtml = "<i class='fas fa-sync-alt fa-spin'></i>";
            h4Builder.InnerHtml += span2Builder;
            divcardhBuilder.InnerHtml += h4Builder;
            TagBuilder divcardbodyBuilder = new TagBuilder("div");
            divcardbodyBuilder.MergeAttribute("class", "card-body");
            TagBuilder h11Builder = new TagBuilder("h1");
            h11Builder.MergeAttribute("class", "card-title");
            h11Builder.InnerHtml = model.CryptoboxModel.coinName + " " + localisation[model.CryptoboxModel.language].Loading;
            divcardbodyBuilder.InnerHtml = h11Builder.ToString() + "<br>";
            TagBuilder divleadBuilder = new TagBuilder("div");
            divleadBuilder.MergeAttribute("class", "lead " + ext + "error_message");
            divcardbodyBuilder.InnerHtml += divleadBuilder + "<br><br>";
            TagBuilder btnsubmitBuilder = new TagBuilder("button");
            btnsubmitBuilder.MergeAttribute("type", "submit");
            btnsubmitBuilder.MergeAttribute("class", ext + "button_error btn btn-outline-primary btn-block btn-lg");
            btnsubmitBuilder.InnerHtml = "<i class='fas fa-sync'></i> &#160; Reload Page";
            divcardbodyBuilder.InnerHtml += btnsubmitBuilder + "<br>";
            divcardBuilder.InnerHtml += divcardhBuilder;
            divcardBuilder.InnerHtml += divcardbodyBuilder;
            diverrBuilder.InnerHtml += divcardBuilder.ToString() + "<br><br><br><br><br>";
            divcolBuilder.InnerHtml = diverrBuilder.ToString();
            divrowBuilder.InnerHtml = divcolBuilder.ToString();
            containerBuilder.InnerHtml = divrowBuilder.ToString();
            formBuilder.InnerHtml += containerBuilder.ToString();
            divloaderBuilder.InnerHtml = formBuilder.ToString();
            div1Builder.InnerHtml += divloaderBuilder;

            // End - Payment Box Ajax Loading ...

            // ----------------------------
            // Area above Payment Box
            // ----------------------------

            TagBuilder divtopboxBuilder = new TagBuilder("div");
            divtopboxBuilder.MergeAttribute("class", ext + "cryptobox_top");
            divtopboxBuilder.MergeAttribute("style", "display:none");
            // A1. Notification payment received or not; when user click 'Refresh' button below payment form
            // --------------------
            if (helper.ViewContext.HttpContext.Request[ext + "refresh_"] != null || helper.ViewContext.HttpContext.Request[ext + "refresh2_"] != null)
            {
                TagBuilder div11Builder = new TagBuilder("div");
                div11Builder.MergeAttribute("class", "row " + ext + "msg mx-2");
                TagBuilder div12Builder = new TagBuilder("div");
                div12Builder.MergeAttribute("class", "container");
                TagBuilder div13Builder = new TagBuilder("div");
                div13Builder.MergeAttribute("class", "row");
                TagBuilder div14Builder = new TagBuilder("div");
                div14Builder.MergeAttribute("class", "col-12 col-sm-10 offset-sm-1 mb-5 mt-2 text-left");
                if (model.CryptoboxModel.is_paid)
                {
                    div14Builder.InnerHtml = "<span class='badge badge-success " + ext + "paymentcaptcha_statustext'>Successfully Received</span>";
                }
                else
                {
                    div14Builder.InnerHtml = "<span class='badge badge-danger " + ext + "paymentcaptcha_statustext'>Not Received</span>";
                }
                TagBuilder div15Builder = new TagBuilder("div");
                div15Builder.MergeAttribute("class", "jumbotron jumbotron-fluid text-center");
                TagBuilder div16Builder = new TagBuilder("div");
                div16Builder.MergeAttribute("class", "container");
                string t = payment_status_text(model.CryptoboxModel);
                if (t.Contains("<br>"))
                {
                    div16Builder.InnerHtml = "<h3 class='display-5'>" + t.Substring(0, t.IndexOf("<br>")) + "</h3><br>";
                    t = t.Substring(t.IndexOf("<br>") + 4);
                }
                div16Builder.InnerHtml += "<p class='lead'>" + t + "</p>";
                div15Builder.InnerHtml += div16Builder;
                div14Builder.InnerHtml += div15Builder;
                div13Builder.InnerHtml += div14Builder;
                div12Builder.InnerHtml += div13Builder;
                div11Builder.InnerHtml += div12Builder;
                divtopboxBuilder.InnerHtml += div11Builder;
            }

            // A2. Coin selection list (bitcoin/litecoin/etc)
            // --------------------
            if (!model.CryptoboxModel.is_paid)
            {
                if (customText.IsEmpty())
                    divtopboxBuilder.InnerHtml += "<br>";
                divtopboxBuilder.InnerHtml += coins_list_html;
            }

            // Language / logo Row
            if (showLanguages || logoimgPath != String.Empty)
            {
                TagBuilder div17Builder = new TagBuilder("div");
                div17Builder.MergeAttribute("class", "container");
                TagBuilder div18Builder = new TagBuilder("div");
                div18Builder.MergeAttribute("class", "row");

                // A3. Box Language
                // --------------------
                if (showLanguages)
                {
                    var offset = logoimgPath != String.Empty ? "text-center mb-2" : "mb-3";
                    TagBuilder div19Builder = new TagBuilder("div");
                    div19Builder.MergeAttribute("class", ext + "box_language col-12 col-sm-4 offset-sm-1 text-sm-left col-md-4 offset-md-2 text-md-left mt-sm-4 " + offset);
                    TagBuilder div20Builder = new TagBuilder("div");
                    div20Builder.MergeAttribute("class", "btn-group");
                    TagBuilder btn2Builder = new TagBuilder("button");
                    btn2Builder.MergeAttribute("type", "button");
                    btn2Builder.MergeAttribute("class", "btn btn-outline-secondary dropdown-toggle");
                    btn2Builder.MergeAttribute("data-toggle", "dropdown");
                    btn2Builder.MergeAttribute("aria-haspopup", "true");
                    btn2Builder.MergeAttribute("aria-expanded", "false");
                    btn2Builder.InnerHtml = "Language" + " - " + localisation[model.CryptoboxModel.language].Name;
                    div20Builder.InnerHtml += btn2Builder;
                    div20Builder.InnerHtml += languages_list;
                    div19Builder.InnerHtml += div20Builder;
                    div18Builder.InnerHtml += div19Builder;
                }
                // End - A3. Box Language

                // A4. Logo
                // --------------------
                if (logoimgPath != String.Empty)
                {
                    var offset = showLanguages ? "" : "offset-sm-5 offset-md-6";
                    TagBuilder div21Builder = new TagBuilder("div");
                    div21Builder.MergeAttribute("class", ext + "box_logo col-12 col-sm-6 col-md-4 mt-4 " + offset);
                    TagBuilder div22Builder = new TagBuilder("div");
                    div22Builder.MergeAttribute("class", "text-right");
                    div22Builder.InnerHtml = "<img style='max-width:200px;max-height:40px;' class='" + ext + "logo_image' alt='logo' src='#'>";
                    div21Builder.InnerHtml += div22Builder + "<br>";
                    div18Builder.InnerHtml += div21Builder;
                }
                // End - A4. Logo

                div17Builder.InnerHtml += div18Builder;
                divtopboxBuilder.InnerHtml += div17Builder;
            }
            else
            {
                divtopboxBuilder.InnerHtml += "<br><br>";
            }

            div1Builder.InnerHtml += divtopboxBuilder;
            // --------------------
            // End - Area above Payment Box

            // -----------------------------------------------------------------------------------------------
            // Two visual types of payment box - payment not received (type1) and payment received (type2)
            // -----------------------------------------------------------------------------------------------

            // Type1 - Crypto Payment Box - Payment Not Received
            TagBuilder div23Builder = new TagBuilder("div");
            div23Builder.MergeAttribute("class", "container " + ext + "cryptobox_unpaid");
            div23Builder.MergeAttribute("style", "display:none");
            TagBuilder div24Builder = new TagBuilder("div");
            div24Builder.MergeAttribute("class", "row");
            TagBuilder div25Builder = new TagBuilder("div");
            div25Builder.MergeAttribute("class", "col-12 text-center col-sm-10 offset-sm-1 col-md-8 offset-md-2");
            TagBuilder form2Builder = new TagBuilder("form");
            form2Builder.MergeAttribute("action", page_url);
            form2Builder.MergeAttribute("method", "post");
            TagBuilder div26Builder = new TagBuilder("div");
            div26Builder.MergeAttribute("class", "card box-shadow");
            TagBuilder div27Builder = new TagBuilder("div");
            div27Builder.MergeAttribute("class", "card-header");
            TagBuilder h42Builder = new TagBuilder("h4");
            h42Builder.MergeAttribute("class", "my-0 font-weight-normal " + ext + "addr_title");
            TagBuilder span3Builder = new TagBuilder("span");
            span3Builder.MergeAttribute("class", ext + "texts_coin_address");
            span3Builder.InnerHtml += "&#160;";
            h42Builder.InnerHtml += span3Builder;
            TagBuilder btn3Builder = new TagBuilder("button");
            btn3Builder.MergeAttribute("type", "submit");
            btn3Builder.MergeAttribute("class", ext + "refresh btn btn-sm btn-outline-secondary float-right");
            btn3Builder.InnerHtml += "<i class='fas fa-sync-alt'></i>";
            h42Builder.InnerHtml += btn3Builder;
            TagBuilder span4Builder = new TagBuilder("span");
            span4Builder.MergeAttribute("class", ext + "loading_icon mr-3 float-left");
            span4Builder.MergeAttribute("style", "display:none");
            span4Builder.InnerHtml += "<i class='fas fa-laptop'></i>";
            h42Builder.InnerHtml += span4Builder;
            TagBuilder span5Builder = new TagBuilder("span");
            span5Builder.MergeAttribute("class", ext + "loading_icon mr-3 float-left");
            span5Builder.MergeAttribute("style", "display:none");
            span5Builder.InnerHtml += "<i class='fas fa-sync-alt fa-spin'></i>";
            h42Builder.InnerHtml += span5Builder;
            div27Builder.InnerHtml += h42Builder;
            div26Builder.InnerHtml += div27Builder;
            TagBuilder div28Builder = new TagBuilder("div");
            div28Builder.MergeAttribute("class", "card-body");

            if (qrcodeSize != 0)
            {
                TagBuilder div29Builder = new TagBuilder("div");
                div29Builder.MergeAttribute("class", ext + "copy_address");
                div29Builder.InnerHtml += "<a href='#a'><img class='" + ext + "qrcode_image' style='max-width:" + qrcodeSize +"px; height:auto; width:auto;' alt='qrcode' data-size='" + qrcodeSize + "' src='#'></a>";
                div28Builder.InnerHtml += div29Builder;
            }

            TagBuilder h12Builder = new TagBuilder("h1");
            h12Builder.MergeAttribute("class", "mt-3 mb-4 pb-1 card-title " + ext + "copy_amount");
            h12Builder.InnerHtml += "<span class='" + ext + "amount'>&#160;</span> <small class='text-muted'><span class='" + ext + "coinlabel'></span></small>";
            div28Builder.InnerHtml += h12Builder;
            div28Builder.InnerHtml += "<div class='lead " + ext + "copy_amount " + ext + "texts_send'></div>";
            div28Builder.InnerHtml += "<div class='lead " + ext + "texts_no_include_fee'></div>";
            div28Builder.InnerHtml += "<br>";

            TagBuilder h43Builder = new TagBuilder("h4");
            h43Builder.MergeAttribute("class", "card-title");
            h43Builder.InnerHtml += "<a class='" + ext + "wallet_address' style='line-height:1.5;' href='#a'></a> &#160;&#160;" +
                          "<a class='" + ext + "copy_address' href='#a'><i class='fas fa-copy'></i></a> &#160;&#160;" +
                          "<a class='" + ext + "wallet_open' href='#a'><i class='fas fa-external-link-alt'></i></a>";
            div28Builder.InnerHtml += h43Builder + "<br>";

            TagBuilder btn4Builder = new TagBuilder("button");
            btn4Builder.MergeAttribute("type", "submit");
            btn4Builder.MergeAttribute("class", ext + "button_wait btn btn-lg btn-block btn-outline-primary");
            btn4Builder.MergeAttribute("style", "white-space:normal");
            div28Builder.InnerHtml += btn4Builder + "<br>";
            TagBuilder p0Builder = new TagBuilder("p");
            p0Builder.MergeAttribute("class", "lead " + ext + "texts_intro3");
            div28Builder.InnerHtml += p0Builder;
            div26Builder.InnerHtml += div28Builder;
            form2Builder.InnerHtml += div26Builder;

            TagBuilder hid0Builder = new TagBuilder("input");
            hid0Builder.MergeAttribute("type", "hidden");
            hid0Builder.GenerateId(ext + "refresh_");
            hid0Builder.MergeAttribute("name", ext + "refresh_");
            hid0Builder.MergeAttribute("value", ext + "1");
            form2Builder.InnerHtml += hid0Builder;
            TagBuilder btn5Builder = new TagBuilder("button");
            btn5Builder.MergeAttribute("type", "submit");
            btn5Builder.MergeAttribute("class", ext + "button_refresh btn btn-lg btn-block btn-primary mt-3");
            btn5Builder.MergeAttribute("style", "display:none");
            form2Builder.InnerHtml += btn5Builder;

            div25Builder.InnerHtml += form2Builder;
            div24Builder.InnerHtml += div25Builder;

            if (model.Method != "ajax" && !model.CryptoboxModel.is_paid)
            {
                TagBuilder div30Builder = new TagBuilder("div");
                div30Builder.MergeAttribute("class", "col-12 text-center col-sm-10 offset-sm-1 col-md-8 offset-md-2");
                TagBuilder form3Builder = new TagBuilder("form");
                form3Builder.MergeAttribute("action", page_url);
                form3Builder.MergeAttribute("method", "post");
                TagBuilder hid1Builder = new TagBuilder("input");
                hid1Builder.MergeAttribute("type", "hidden");
                hid1Builder.GenerateId(ext + "refresh2_");
                hid1Builder.MergeAttribute("name", ext + "refresh2_");
                hid1Builder.MergeAttribute("value", "1");
                form3Builder.InnerHtml += hid1Builder + "<br>";
                TagBuilder btn6Builder = new TagBuilder("button");
                btn6Builder.MergeAttribute("type", "submit");
                btn6Builder.MergeAttribute("class", ext + "button_confirm btn btn-lg btn-block btn-primary my-2");
                btn6Builder.MergeAttribute("style", "white-space:normal");
                btn6Builder.InnerHtml += "<i class='fas fa-angle-double-right'></i> &#160; " + localisation[model.CryptoboxModel.language].Button
                    .Replace("%coinName%", model.CryptoboxModel.coinName)
                    .Replace("%coinNames%", model.CryptoboxModel.coinLabel == "BCH" || model.CryptoboxModel.coinLabel == "BSV" || model.CryptoboxModel.coinLabel == "DASH" ? model.CryptoboxModel.coinName : model.CryptoboxModel.coinName + "s")
                    .Replace("%coinLabel%", model.CryptoboxModel.coinLabel) + " &#160; <i class='fas fa-angle-double-right'></i>";
                form3Builder.InnerHtml += btn6Builder;
                div30Builder.InnerHtml += form3Builder;
                div24Builder.InnerHtml += div30Builder;
            }

            div23Builder.InnerHtml += div24Builder;
            div1Builder.InnerHtml += div23Builder;

            // -----------------------------------------------
            // End Type1 - Payment Box - Payment Not Received
            // -----------------------------------------------


            // -------------------------------------------------------------------------
            // Type2 - Crypto Payment Box - Payment Received/Successful Result 
            // -------------------------------------------------------------------------

            TagBuilder div31Builder = new TagBuilder("div");
            div31Builder.MergeAttribute("class", "container " + ext + "cryptobox_paid");
            div31Builder.MergeAttribute("style", "display:none");
            TagBuilder div32Builder = new TagBuilder("div");
            div32Builder.MergeAttribute("class", "row");
            TagBuilder div33Builder = new TagBuilder("div");
            div33Builder.MergeAttribute("class", "col-12 col-sm-10 offset-sm-1 col-md-8 offset-md-2 text-center");
            TagBuilder div34Builder = new TagBuilder("div");
            div34Builder.MergeAttribute("class", "card box-shadow");
            TagBuilder div35Builder = new TagBuilder("div");
            div35Builder.MergeAttribute("class", "card-header");
            TagBuilder h44Builder = new TagBuilder("h4");
            h44Builder.MergeAttribute("class", "my-0 font-weight-normal " + ext + "addr_title");
            h44Builder.InnerHtml += "<span class='" + ext + "texts_title'>&#160;</span>";
            TagBuilder span6Builder = new TagBuilder("span");
            span6Builder.MergeAttribute("class", ext + "loading_icon mr-3 float-left");
            span6Builder.MergeAttribute("style", "display:none");
            span6Builder.InnerHtml += "<i class='fas fa-laptop'></i>";
            h44Builder.InnerHtml += span6Builder;
            TagBuilder span7Builder = new TagBuilder("span");
            span7Builder.MergeAttribute("class", ext + "loading_icon mr-3 float-left");
            span7Builder.MergeAttribute("style", "display:none");
            span7Builder.InnerHtml += "<i class='fas fa-sync-alt fa-spin'></i>";
            h44Builder.InnerHtml += span7Builder;
            div35Builder.InnerHtml += h44Builder;
            div34Builder.InnerHtml += div35Builder;

            TagBuilder div36Builder = new TagBuilder("div");
            div36Builder.MergeAttribute("class", "card-body");
            TagBuilder div37Builder = new TagBuilder("div");
            div37Builder.MergeAttribute("class", ext + "paid_total");
            TagBuilder h13Builder = new TagBuilder("h1");
            h13Builder.MergeAttribute("class", "card-title " + ext + "copy_amount");
            h13Builder.MergeAttribute("style", "margin-top:10px");
            h13Builder.InnerHtml += "<span class='" + ext + "amount'>&#160;</span> <small class='text-muted'><span class='" + ext + "coinlabel'></span></small>";
            div37Builder.InnerHtml += h13Builder;
            div36Builder.InnerHtml += div37Builder + "<br>";

            if (resultimgPath == String.Empty || resultimgPath == "default")
            {
                resultimgPath = imgdir_path + "paid.png";
            }

            if (resultimgSize != 0)
            {
                TagBuilder div38Builder = new TagBuilder("div");
                div38Builder.MergeAttribute("class", ext + "copy_transaction");
                div38Builder.InnerHtml += "<img class='" + ext + "paidimg' style='max-width: 100%; width: " + resultimgSize + "px; height: auto;' src='" + resultimgPath + "' alt='Paid'>";
                div36Builder.InnerHtml += div38Builder + "<br><br>";
            }
            TagBuilder h14Builder = new TagBuilder("h1");
            h14Builder.MergeAttribute("class", "display-4 " + ext + "paymentcaptcha_successful");
            h14Builder.MergeAttribute("style", "line-height:1.5;");
            h14Builder.InnerHtml += ".";
            div36Builder.InnerHtml += h14Builder + "<br>";
            TagBuilder div39Builder = new TagBuilder("div");
            div39Builder.MergeAttribute("class", "lead " + ext + "paymentcaptcha_date");
            div36Builder.InnerHtml += div39Builder + "<br><br>";
            TagBuilder a0Builder = new TagBuilder("a");
            a0Builder.MergeAttribute("href", "#a");
            a0Builder.MergeAttribute("class", ext + "button_details btn btn-lg btn-block btn-outline-primary");
            a0Builder.MergeAttribute("style", "white-space:normal");
            div36Builder.InnerHtml += a0Builder;
            div34Builder.InnerHtml += div36Builder;
            div33Builder.InnerHtml += div34Builder;
            div32Builder.InnerHtml += div33Builder;
            div31Builder.InnerHtml += div32Builder;
            div1Builder.InnerHtml += div31Builder + "<br><br><br>";

            // -----------------------------------------------
            // End Type2 - Payment Received/Successful Result 
            // -----------------------------------------------



            // -------------------------------------------------------------------------------------------
            // Debug Raw JSON Payment Data from gourl.io
            // -------------------------------------------------------------------------------------------

            if (debug)
            {
                TagBuilder div40Builder = new TagBuilder("div");
                div40Builder.MergeAttribute("class", "mncrpt_debug container " + ext + "cryptobox_rawdata px-4 py-3");
                div40Builder.MergeAttribute("style", "overflow-wrap: break-word; display:none;");
                TagBuilder div41Builder = new TagBuilder("div");
                div41Builder.MergeAttribute("class", "row");
                TagBuilder div42Builder = new TagBuilder("div");
                div42Builder.MergeAttribute("class", "col-12");
                div42Builder.InnerHtml += "<br><br><br><br><br>" +
                                          "<h1 class='display-4'>Raw JSON Data (from GoUrl.io payment gateway) -</h1>" +
                                          "<br>" +
                                          "<p class='lead'><b>PHP Language</b> - Please use function <a target='_blank' href='https://github.com/cryptoapi/Payment-Gateway/blob/master/lib/cryptobox.class.php#L754'>$box->display_cryptobox_bootstrap (...)</a>; it generate customize mobile friendly bitcoin/altcoin payment box and automatically displays successful payment message (bootstrap4, json, your own logo, white label product, etc)</p>" +
                                          "<p class='lead'><b>ASP/Other Languages</b> - You can use function <a target='_blank' href='https://github.com/cryptoapi/Payment-Gateway/blob/master/lib/cryptobox.class.php#L320'>$box->cryptobox_json_url()</a>; It generates url with your parameters to gourl.io payment gateway. " +
                                          "Using this url you can get bitcoin/altcoin payment box values in JSON format and use it on html page with Jquery/Ajax (on the user side). " +
                                          "Or your server can receive JSON values through curl - function <a target='_blank' href='https://github.com/cryptoapi/Payment-Gateway/blob/master/lib/cryptobox.class.php#L374'>$box->get_json_values()</a>; and use it in your files/scripts directly without javascript when generating the webpage (on the server side).</p>" +
                                          "<p class='lead'><a target='_blank' href='" + model.JsonUrl +
                                          "'>JSON data source &#187;</a></p>" +
                                          "<div class='card card-body bg-light'>" +
                                          "<div class='" + ext + "jsondata'></div>";

                div41Builder.InnerHtml += div42Builder;
                div40Builder.InnerHtml += div41Builder;
                div1Builder.InnerHtml += div40Builder;
            }

            // ------------------
            // End Debug
            // ------------------

            div0Builder.InnerHtml += div1Builder + "<br><br><br>";
            result += div0Builder;

            // ---------------------------
            // Bootstrap4 Template End
            // ----------------------------

            return MvcHtmlString.Create(result);
        }
    }
}