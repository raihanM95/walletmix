using System.Collections.Generic;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Walletmix.Models
{
    public class PaymentInfoModel : BaseNopModel
    {
        #region Ctor

        #endregion

        #region Properties

        public string MerchantID { get; set; }

        public string MerchantOrderID { get; set; }

        public string MerchantReferenceID { get; set; }

        public string OnlyDomainName { get; set; }

        public string OrderInfo { get; set; }

        public string CustomerName { get; set; }

        public string CustomerEmail { get; set; }

        public string CustomerAddress { get; set; }

        public string CustomerCity { get; set; }

        public string CustomerCountry { get; set; }

        public string CustomerPostcode { get; set; }

        public string CustomerPhone { get; set; }

        public string ShippingName { get; set; }

        public string ShippingAddress { get; set; }

        public string ShippingCity { get; set; }

        public string ShippingCountry { get; set; }

        public string ShippingPostcode { get; set; }

        public string ProductDescription { get; set; }

        public string Amount { get; set; }

        public string Currency { get; set; }

        public string Options { get; set; }

        public string CallbackURL { get; set; }

        public string AccessAppKey { get; set; }

        public string Authorization { get; set; }

        #endregion
    }
}