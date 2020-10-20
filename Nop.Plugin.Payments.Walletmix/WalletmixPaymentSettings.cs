using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Walletmix
{
    /// <summary>
    /// Represents settings of walletmix payment plugin
    /// </summary>
    public class WalletmixPaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox (testing environment)
        /// </summary>
        public bool UseSandbox { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox url (testing environment)
        /// </summary>
        public string SandboxURL { get; set; }

        /// <summary>
        /// Gets or sets a merchant id
        /// </summary>
        public string MerchantID { get; set; }

        /// <summary>
        /// Gets or sets website name
        /// </summary>
        public string WebsiteName { get; set; }

        /// <summary>
        /// Gets or sets currency
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets access username
        /// </summary>
        public string AccessUsername { get; set; }

        /// <summary>
        /// Gets or sets access password
        /// </summary>
        public string AccessPassword { get; set; }

        /// <summary>
        /// Gets or sets callback url
        /// </summary>
        public string CallbackURL { get; set; }

        /// <summary>
        /// Gets or sets access app key
        /// </summary>
        public string AccessAppKey { get; set; }

        /// <summary>
        /// Gets or sets an additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
    }
}
