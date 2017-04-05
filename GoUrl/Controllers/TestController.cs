using System.Web.Mvc;
using Gourl.GoUrlCore;

namespace Gourl.Controllers
{
    public class TestController : Controller
    {
        // GET: Test
        public ActionResult Index()
        {
            ViewBag.Amount = CryptoHelper.convert_currency_live("USD", "BTC", 1);
            return View();
        }
    }
}