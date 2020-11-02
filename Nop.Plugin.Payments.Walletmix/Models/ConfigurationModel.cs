using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Walletmix.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Walletmix.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Walletmix.Fields.SandboxURL")]
        public string SandboxURL { get; set; }
        public bool SandboxURL_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Walletmix.Fields.MerchantID")]
        public string MerchantID { get; set; }
        public bool MerchantID_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Walletmix.Fields.WebsiteName")]
        public string WebsiteName { get; set; }
        public bool WebsiteName_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Walletmix.Fields.Currency")]
        public string Currency { get; set; }
        public bool Currency_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Walletmix.Fields.AccessUsername")]
        public string AccessUsername { get; set; }
        public bool AccessUsername_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Walletmix.Fields.AccessPassword")]
        public string AccessPassword { get; set; }
        public bool AccessPassword_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Walletmix.Fields.CallbackURL")]
        public string CallbackURL { get; set; }
        public bool CallbackURL_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Walletmix.Fields.AccessAppKey")]
        public string AccessAppKey { get; set; }
        public bool AccessAppKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Walletmix.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Walletmix.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }
    }
}