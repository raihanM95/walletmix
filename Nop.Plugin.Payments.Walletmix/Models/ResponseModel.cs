using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Payments.Walletmix.Models
{
    internal class ResponseModel
    {
        [JsonProperty("selectedServer")]
        public bool SelectedServer { get; set; }

        [JsonProperty("url")]
        public string URL { get; set; }

        [JsonProperty("bank_payment_url")]
        public string BankPaymentURL { get; set; }

        [JsonProperty("statusCode")]
        public string StatusCode { get; set; }

        [JsonProperty("statusMsg")]
        public string StatusMsg { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
