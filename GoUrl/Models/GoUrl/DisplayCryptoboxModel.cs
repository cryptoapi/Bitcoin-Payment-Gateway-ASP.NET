namespace Gourl.Models.GoUrl
{
    public class DisplayCryptoboxModel
    {
        public int boxID { get; set; }
        public string coinName { get; set; }
        public string public_key { get; set; }
        public decimal amount { get; set; }
        public decimal amountUSD { get; set; }
        public string period { get; set; }
        public string language { get; set; }
        public string iframeID { get; set; }
        public string userID { get; set; }
        public string userFormat { get; set; }
        public string orderID { get; set; }
        public string cookieName { get; set; }
        public string webdev_key { get; set; }
        public string private_key { get; set; }
        public bool is_paid { get; set; }
        public string coinLabel { get; set; }
        public string boxType { get; set; }
        public decimal amoutnPaid { get; set; }
    }
}