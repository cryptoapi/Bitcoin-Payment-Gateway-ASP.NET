using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Gourl.GoUrlCore;
using Gourl.Models;
using Gourl.Models.GoUrl;

namespace Gourl.Controllers.GoUrl
{
    public class GoUrlController : Controller
    {
        private GoUrlEntities Context = new GoUrlEntities();

        [HttpGet]
        public ActionResult Callback()
        {
            // don't delete it  
            return new ContentResult() { Content = "Only POST Data Allowed" };
        }
        [HttpPost]
        public ActionResult Callback(IPNModel callback)
        {
            bool valid_key = false;
            if (!string.IsNullOrEmpty(callback.private_key_hash) && callback.private_key_hash.Length == 128 && Regex.IsMatch(callback.private_key_hash, "[a-zA-Z0-9]+"))
            {
                string[] ss = ConfigurationManager.AppSettings["PrivateKeys"].Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> s = new List<string>();
                foreach (string s1 in ss)
                {
                    s.Add(Calculator.md512(s1));
                }
                valid_key = s.Contains(callback.private_key_hash);
            }
            if (!string.IsNullOrEmpty(Request.Form["plugin_ver"]) && string.IsNullOrEmpty(callback.status) && valid_key)
            {
                return new ContentResult() { Content = "cryptoboxver_asp_1.0" };
            }

            if (callback.order == null)
                callback.order = "";
            if (callback.user == null)
                callback.user = "";
            if (callback.usercountry == null)
                callback.usercountry = "";
            
            string box_status = "cryptobox_nochanges";
            if (ModelState.IsValid)
            {
                int paymentId = 0;
                crypto_payments obj =
                    Context.crypto_payments.FirstOrDefault(x => x.boxID == callback.box && x.orderID == callback.order && x.userID == callback.user && x.txID == callback.tx && x.amount == callback.amount && x.addr == callback.addr);
                if (obj == null)
                {
                    crypto_payments newPayments =
                        new crypto_payments()
                        {
                            boxID = callback.box,
                            boxType = callback.boxtype,
                            orderID = callback.order,
                            userID = callback.user,
                            countryID = callback.usercountry,
                            coinLabel = callback.coinlabel,
                            amount = callback.amount,
                            amountUSD = callback.amountusd,
                            unrecognised = (byte)(callback.status == "payment_received_unrecognised" ? 1 : 0),
                            addr = callback.addr,
                            txID = callback.tx,
                            txDate = callback.datetime,
                            txConfirmed = callback.confirmed,
                            txCheckDate = DateTime.Now,
                            recordCreated = DateTime.Now
                        };
                    
                    try
                    {
                        Context.crypto_payments.Add(newPayments);
                        Context.SaveChanges();
                        paymentId = newPayments.paymentID;
                    }
                    catch (DbEntityValidationException dbEx)
                    {
                        var modelErrors2 = new List<string>();
                        foreach (var validationErrors in dbEx.EntityValidationErrors)
                        {
                            foreach (var validationError in validationErrors.ValidationErrors)
                            {
                                modelErrors2.Add("Property: " + validationError.PropertyName + " Error: " + validationError.ErrorMessage);
                            }
                        }
                        return new ContentResult() { Content = String.Join(", ", modelErrors2.ToArray()) };
                    }

                    obj = newPayments;
                    box_status = "cryptobox_newrecord";
                }
                else if (callback.confirmed == 1 && obj.txConfirmed == 0)
                {
                    obj.txConfirmed = 1;
                    obj.txCheckDate = DateTime.Now;
                    Context.SaveChanges();
                    paymentId = obj.paymentID;

                    box_status = "cryptobox_updated";
                }
                else
                {
                    paymentId = obj.paymentID;
                }

                NewPayment.Main(paymentId, callback, box_status);

                return new ContentResult() { Content = box_status };
            }

            //for test
            var modelErrors = new List<string>();
            foreach (ModelState err in ViewData.ModelState.Values)
            {
                foreach (ModelError error in err.Errors)
                {
                    modelErrors.Add(error.ErrorMessage);
                }
            }
            return new ContentResult() { Content = String.Join(", ", modelErrors.ToArray()) };
        }


        [ChildActionOnly]
        public ActionResult Cryptobox(OptionsModel options)
        {
            using (Cryptobox cryptobox = new Cryptobox(options))
            {
                DisplayCryptoboxModel model = cryptobox.GetDisplayCryptoboxModel();

                return PartialView("Partial/_Cryptobox", model);
            }

        }
    }
}