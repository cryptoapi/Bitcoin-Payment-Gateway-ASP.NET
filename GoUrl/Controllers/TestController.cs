using System.Web.Mvc;
using Gourl.GoUrlCore;

namespace Gourl.Controllers
{
    public class TestController : Controller
    {
        // GET: Test
        public ActionResult Index()
        {
            ViewBag.UsdBtc = CryptoHelper.convert_currency_live("USD", "BTC", 1);
            ViewBag.BtcUsd = CryptoHelper.convert_currency_live("BTC", "USD", 1);
            ViewBag.BtcGbr = CryptoHelper.convert_currency_live("BTC", "GBP", 1);
            ViewBag.UsdGbr = CryptoHelper.convert_currency_live("USD", "GBP", 1);
            return View();
        }
    }
}