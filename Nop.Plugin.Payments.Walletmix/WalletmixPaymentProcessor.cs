using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.Walletmix.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Nop.Plugin.Payments.Walletmix
{
    /// <summary>
    /// Walletmix payment processor
    /// </summary>
    public class WalletmixPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICurrencyService _currencyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly IPaymentService _paymentService;
        private readonly ISettingService _settingService;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly WalletmixPaymentSettings _walletmixPaymentSettings;

        private readonly IStoreContext _storeContext;

        #endregion Fields

        #region Ctor

        public WalletmixPaymentProcessor(CurrencySettings currencySettings,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            IPaymentService paymentService,
            ISettingService settingService,
            ITaxService taxService,
            IWebHelper webHelper,
            WalletmixPaymentSettings walletmixPaymentSettings,

            IStoreContext storeContext)
        {
            this._currencySettings = currencySettings;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._currencyService = currencyService;
            this._genericAttributeService = genericAttributeService;
            this._httpContextAccessor = httpContextAccessor;
            this._localizationService = localizationService;
            this._paymentService = paymentService;
            this._settingService = settingService;
            this._taxService = taxService;
            this._webHelper = webHelper;
            this._walletmixPaymentSettings = walletmixPaymentSettings;

            this._storeContext = storeContext;
        }

        #endregion Ctor

        #region Utilities

        /// <summary>
        /// Gets Walletmix URL
        /// </summary>
        /// <returns></returns>
        private string GetWalletmixUrl()
        {
            return _walletmixPaymentSettings.UseSandbox ?
                _walletmixPaymentSettings.SandboxURL :
                "";
        }
        
        /// <summary>
        /// Gets PDT details
        /// </summary>
        /// <param name="tx">TX</param>
        /// <param name="values">Values</param>
        /// <param name="response">Response</param>
        /// <returns>Result</returns>
        //public bool GetPdtDetails(string tx, out Dictionary<string, string> values, out string response)
        //{
        //    var req = (HttpWebRequest)WebRequest.Create(GetWalletmixUrl());
        //    req.Method = WebRequestMethods.Http.Post;
        //    req.ContentType = MimeTypes.ApplicationXWwwFormUrlencoded;
        //    //now Walletmix requires user-agent. otherwise, we can get 403 error
        //    req.UserAgent = _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.UserAgent];

        //    var formContent = $"cmd=_notify-synch&at={_walletmixPaymentSettings.PdtToken}&tx={tx}";
        //    req.ContentLength = formContent.Length;

        //    using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
        //        sw.Write(formContent);

        //    using (var sr = new StreamReader(req.GetResponse().GetResponseStream()))
        //        response = WebUtility.UrlDecode(sr.ReadToEnd());

        //    values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        //    bool firstLine = true, success = false;
        //    foreach (var l in response.Split('\n'))
        //    {
        //        var line = l.Trim();
        //        if (firstLine)
        //        {
        //            success = line.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
        //            firstLine = false;
        //        }
        //        else
        //        {
        //            var equalPox = line.IndexOf('=');
        //            if (equalPox >= 0)
        //                values.Add(line.Substring(0, equalPox), line.Substring(equalPox + 1));
        //        }
        //    }

        //    return success;
        //}
        
        /// <summary>
        /// Create common query parameters for the request
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Created query parameters</returns>
        private IDictionary<string, string> CreateQueryParameters(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //get store location
            //var storeLocation = _webHelper.GetStoreLocation();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var walletmixPaymentSettings = _settingService.LoadSetting<WalletmixPaymentSettings>(storeScope);

            //base64 encode
            var options = Base64_Encode(walletmixPaymentSettings.AccessUsername, walletmixPaymentSettings.AccessPassword);

            //create query parameters
            return new Dictionary<string, string>
            {
                //walletmix id
                ["wmx_id"] = walletmixPaymentSettings.MerchantID,

                //order identifier
                ["merchant_order_id"] = "MSDSL" + postProcessPaymentRequest.Order.CustomOrderNumber,
                ["merchant_ref_id"] = "MSDSL" + postProcessPaymentRequest.Order.CustomOrderNumber,

                //app info
                ["app_name"] = walletmixPaymentSettings.WebsiteName,
                ["cart_info"] = walletmixPaymentSettings.MerchantID + "," + walletmixPaymentSettings.WebsiteName,

                //PDT, IPN and cancel URL
                //["return"] = $"{storeLocation}Plugins/Walletmix/PDTHandler",
                //["notify_url"] = $"{storeLocation}Plugins/Walletmix/IPNHandler",
                //["cancel_return"] = $"{storeLocation}Plugins/Walletmix/CancelOrder",

                //shipping address
                ["customer_name"] = postProcessPaymentRequest.Order.ShippingAddress?.FirstName + postProcessPaymentRequest.Order.ShippingAddress?.LastName,
                ["customer_email"] = postProcessPaymentRequest.Order.ShippingAddress?.Email,
                ["customer_add"] = postProcessPaymentRequest.Order.ShippingAddress?.Address1,
                ["customer_phone"] = postProcessPaymentRequest.Order.ShippingAddress?.PhoneNumber,

                //product description //problem
                ["product_desc"] = postProcessPaymentRequest.Order.OrderTotal.ToString(),
                ["amount"] = postProcessPaymentRequest.Order.OrderTotal.ToString(),

                //currency
                ["currency"] = walletmixPaymentSettings.Currency,

                //base64 encode
                ["options"] = options,

                //callback URL
                ["callback_url"] = walletmixPaymentSettings.CallbackURL,

                //access app key
                ["access_app_key"] = walletmixPaymentSettings.AccessAppKey,

                //authorization
                ["authorization"] = "Basic " + options,
            };
        }

        public string Base64_Encode(string UserName, string Password)
        {
            var byteArray = Encoding.ASCII.GetBytes($"{UserName}:{Password}");
            var clientAuthrizationHeader = new AuthenticationHeaderValue(
                                                          Convert.ToBase64String(byteArray));

            return Convert.ToString(clientAuthrizationHeader);
        }

        /// <summary>
        /// Add order items to the request query parameters
        /// </summary>
        /// <param name="parameters">Query parameters</param>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        private void AddItemsParameters(IDictionary<string, string> parameters, PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //upload order items
            parameters.Add("cmd", "_cart");
            parameters.Add("upload", "1");

            var cartTotal = decimal.Zero;
            var roundedCartTotal = decimal.Zero;
            var itemCount = 1;

            //add shopping cart items
            foreach (var item in postProcessPaymentRequest.Order.OrderItems)
            {
                var roundedItemPrice = Math.Round(item.UnitPriceExclTax, 2);

                //add query parameters
                parameters.Add($"item_name_{itemCount}", item.Product.Name);
                parameters.Add($"amount_{itemCount}", roundedItemPrice.ToString("0.00", CultureInfo.InvariantCulture));
                parameters.Add($"quantity_{itemCount}", item.Quantity.ToString());

                cartTotal += item.PriceExclTax;
                roundedCartTotal += roundedItemPrice * item.Quantity;
                itemCount++;
            }

            //add checkout attributes as order items
            var checkoutAttributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(postProcessPaymentRequest.Order.CheckoutAttributesXml);
            foreach (var attributeValue in checkoutAttributeValues)
            {
                var attributePrice = _taxService.GetCheckoutAttributePrice(attributeValue, false, postProcessPaymentRequest.Order.Customer);
                var roundedAttributePrice = Math.Round(attributePrice, 2);

                //add query parameters
                if (attributeValue.CheckoutAttribute != null)
                {
                    parameters.Add($"item_name_{itemCount}", attributeValue.CheckoutAttribute.Name);
                    parameters.Add($"amount_{itemCount}", roundedAttributePrice.ToString("0.00", CultureInfo.InvariantCulture));
                    parameters.Add($"quantity_{itemCount}", "1");

                    cartTotal += attributePrice;
                    roundedCartTotal += roundedAttributePrice;
                    itemCount++;
                }
            }

            //add shipping fee as a separate order item, if it has price
            var roundedShippingPrice = Math.Round(postProcessPaymentRequest.Order.OrderShippingExclTax, 2);
            if (roundedShippingPrice > decimal.Zero)
            {
                parameters.Add($"item_name_{itemCount}", "Shipping fee");
                parameters.Add($"amount_{itemCount}", roundedShippingPrice.ToString("0.00", CultureInfo.InvariantCulture));
                parameters.Add($"quantity_{itemCount}", "1");

                cartTotal += postProcessPaymentRequest.Order.OrderShippingExclTax;
                roundedCartTotal += roundedShippingPrice;
                itemCount++;
            }

            //add payment method additional fee as a separate order item, if it has price
            var roundedPaymentMethodPrice = Math.Round(postProcessPaymentRequest.Order.PaymentMethodAdditionalFeeExclTax, 2);
            if (roundedPaymentMethodPrice > decimal.Zero)
            {
                parameters.Add($"item_name_{itemCount}", "Payment method fee");
                parameters.Add($"amount_{itemCount}", roundedPaymentMethodPrice.ToString("0.00", CultureInfo.InvariantCulture));
                parameters.Add($"quantity_{itemCount}", "1");

                cartTotal += postProcessPaymentRequest.Order.PaymentMethodAdditionalFeeExclTax;
                roundedCartTotal += roundedPaymentMethodPrice;
                itemCount++;
            }

            //add tax as a separate order item, if it has positive amount
            var roundedTaxAmount = Math.Round(postProcessPaymentRequest.Order.OrderTax, 2);
            if (roundedTaxAmount > decimal.Zero)
            {
                parameters.Add($"item_name_{itemCount}", "Tax amount");
                parameters.Add($"amount_{itemCount}", roundedTaxAmount.ToString("0.00", CultureInfo.InvariantCulture));
                parameters.Add($"quantity_{itemCount}", "1");

                cartTotal += postProcessPaymentRequest.Order.OrderTax;
                roundedCartTotal += roundedTaxAmount;
                itemCount++;
            }

            if (cartTotal > postProcessPaymentRequest.Order.OrderTotal)
            {
                //get the difference between what the order total is and what it should be and use that as the "discount"
                var discountTotal = Math.Round(cartTotal - postProcessPaymentRequest.Order.OrderTotal, 2);
                roundedCartTotal -= discountTotal;

                //gift card or rewarded point amount applied to cart in nopCommerce - shows in Walletmix as "discount"
                parameters.Add("discount_amount_cart", discountTotal.ToString("0.00", CultureInfo.InvariantCulture));
            }

            //save order total that actually sent to Walletmix (used for PDT order total validation)
            //_genericAttributeService.SaveAttribute(postProcessPaymentRequest.Order, WalletmixHelper.OrderTotalSentToWalletmix, roundedCartTotal);
        }

        /// <summary>
        /// Add order total to the request query parameters
        /// </summary>
        /// <param name="parameters">Query parameters</param>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        private void AddOrderTotalParameters(IDictionary<string, string> parameters, PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //round order total
            var roundedOrderTotal = Math.Round(postProcessPaymentRequest.Order.OrderTotal, 2);

            parameters.Add("cmd", "_xclick");
            parameters.Add("item_name", $"Order Number {postProcessPaymentRequest.Order.CustomOrderNumber}");
            parameters.Add("amount", roundedOrderTotal.ToString("0.00", CultureInfo.InvariantCulture));

            //save order total that actually sent to Walletmix (used for PDT order total validation)
            //_genericAttributeService.SaveAttribute(postProcessPaymentRequest.Order, WalletmixHelper.OrderTotalSentToWalletmix, roundedOrderTotal);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult();
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var client = new HttpClient();
            var checkServer = client.GetAsync(GetWalletmixUrl()).GetAwaiter().GetResult();
            var checkServerContent = checkServer.Content.ReadAsStringAsync();
            checkServerContent.Wait();
            var resultContent = checkServerContent.Result;
            ResponseModel responseModel = JsonConvert.DeserializeObject<ResponseModel>(resultContent);
            
            if (responseModel.SelectedServer)
            {
                //create common query parameters for the request
                var queryParameters = CreateQueryParameters(postProcessPaymentRequest);

                var httpContent = new FormUrlEncodedContent(queryParameters);
                var curlRequest = client.PostAsync(responseModel.URL, httpContent).GetAwaiter().GetResult();

                AddItemsParameters(queryParameters, postProcessPaymentRequest);

                //remove null values from parameters
                //parameters = parameters.Where(parameter => !string.IsNullOrEmpty(parameter.Value))
                //.ToDictionary(parameter => parameter.Key, parameter => parameter.Value);

                if (curlRequest.StatusCode == HttpStatusCode.OK)
                {
                    var curlRequestContent = curlRequest.Content.ReadAsStringAsync();
                    curlRequestContent.Wait();
                    var contentResult = curlRequestContent.Result;

                    ResponseModel responseModel2 = JsonConvert.DeserializeObject<ResponseModel>(contentResult);

                    //ensure redirect URL doesn't exceed 2K chars to avoid "too long URL" exception
                    var redirectUrl = responseModel.BankPaymentURL + "/" + responseModel2.Token;
                    //var redirectUrl = QueryHelpers.AddQueryString(GetWalletmixUrl(), responseModel.Token);
                    if (redirectUrl.Length <= 2048)
                    {
                        _httpContextAccessor.HttpContext.Response.Redirect(redirectUrl);
                        return;
                    }

                    //or add only an order total query parameters to the request
                    AddOrderTotalParameters(queryParameters, postProcessPaymentRequest);

                    //remove null values from parameters
                    //queryParameters = queryParameters.Where(parameter => !string.IsNullOrEmpty(parameter.Value))
                    //    .ToDictionary(parameter => parameter.Key, parameter => parameter.Value);

                    var url = responseModel.BankPaymentURL + "/" + responseModel2.Token;
                    _httpContextAccessor.HttpContext.Response.Redirect(url);
                }
            }
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return _paymentService.CalculateAdditionalFee(cart,
                _walletmixPaymentSettings.AdditionalFee, _walletmixPaymentSettings.AdditionalFeePercentage);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            return new CapturePaymentResult { Errors = new[] { "Capture method not supported" } };
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            return new RefundPaymentResult { Errors = new[] { "Refund method not supported" } };
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult { Errors = new[] { "Void method not supported" } };
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return false;

            return true;
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            return new List<string>();
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentWalletmix/Configure";
        }

        /// <summary>
        /// Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return "PaymentWalletmix";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new WalletmixPaymentSettings
            {
                UseSandbox = true
            });

            //locales
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AccessAppKey", "Access app key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AccessAppKey.Hint", "Enter your access app key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AdditionalFee", "Additional fee");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AccessPassword", "Access password");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AccessPassword.Hint", "Enter access password");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AccessUsername", "Access username");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AccessUsername.Hint", "Enter access username");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.CallbackURL", "Call back URL");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.CallbackURL.Hint", "Enter call back URL");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.Currency", "Currency");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.Currency.Hint", "Enter currency");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.MerchantID", "Merchant ID");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.MerchantID.Hint", "Enter merchant id");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.WebsiteName", "Website name");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.WebsiteName.Hint", "Enter website name");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.RedirectionTip", "You will be redirected to Walletmix site to complete the order.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.SandboxURL", "Sandbox URL");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.SandboxURL.Hint", "Enter Sandbox URL");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.UseSandbox", "Use Sandbox");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.Instructions", "<p><b>If you're using this gateway ensure that your primary store currency is supported by Walletmix.</b><br /><br />To use PDT, you must activate PDT and Auto Return in your PayPal account profile. You must also acquire a PDT identity token, which is used in all PDT communication you send to PayPal. Follow these steps to configure your account for PDT:<br /><br />1. Log in to your PayPal account (click <a href=\"https://www.paypal.com/us/webapps/mpp/referral/paypal-business-account2?partner_id=9JJPJNNPQ7PZ8\" target=\"_blank\">here</a> to create your account).<br />2. Click the Profile subtab.<br />3. Click Website Payment Preferences in the Seller Preferences column.<br />4. Under Auto Return for Website Payments, click the On radio button.<br />5. For the Return URL, enter the URL on your site that will receive the transaction ID posted by PayPal after a customer payment ({0}).<br />6. Under Payment Data Transfer, click the On radio button.<br />7. Click Save.<br />8. Click Website Payment Preferences in the Seller Preferences column.<br />9. Scroll down to the Payment Data Transfer section of the page to view your PDT identity token.<br /><br /></p>");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.PaymentMethodDescription", "Pay by Online");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Walletmix.RoundingWarning", "It looks like you have \"ShoppingCartSettings.RoundPricesDuringCalculation\" setting disabled. Keep in mind that this can lead to a discrepancy of the order total amount, as Walletmix only rounds to two decimals.");

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<WalletmixPaymentSettings>();

            //locales
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AccessAppKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AccessAppKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AdditionalFee");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AdditionalFee.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AdditionalFeePercentage");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AdditionalFeePercentage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AccessPassword");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AccessPassword.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AccessUsername");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.AccessUsername.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.CallbackURL");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.CallbackURL.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.Currency");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.Currency.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.MerchantID");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.MerchantID.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.WebsiteName");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.WebsiteName.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.RedirectionTip");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.SandboxURL");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.SandboxURL.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.UseSandbox");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Fields.UseSandbox.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.Instructions");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.PaymentMethodDescription");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Walletmix.RoundingWarning");

            base.Uninstall();
        }

        #endregion Methods

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Redirection; }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to Walletmix site to complete the payment"
            get { return _localizationService.GetResource("Plugins.Payments.Walletmix.PaymentMethodDescription"); }
        }

        #endregion Properties
    }
}