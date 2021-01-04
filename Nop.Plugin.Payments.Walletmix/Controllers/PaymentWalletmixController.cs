using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Walletmix.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Walletmix.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class PaymentWalletmixController : BasePaymentController
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPermissionService _permissionService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger _logger;
        private readonly INotificationService _notificationService;
        private readonly IWebHelper _webHelper;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        #endregion

        #region Ctor

        public PaymentWalletmixController(IWorkContext workContext,
            ISettingService settingService,
            IPaymentService paymentService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IPermissionService permissionService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IStoreContext storeContext,
            ILogger logger,
            INotificationService notificationService,
            IWebHelper webHelper,
            ShoppingCartSettings shoppingCartSettings)
        {
            _workContext = workContext;
            _settingService = settingService;
            _paymentService = paymentService;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _permissionService = permissionService;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _storeContext = storeContext;
            _logger = logger;
            _notificationService = notificationService;
            _webHelper = webHelper;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var walletmixPaymentSettings = _settingService.LoadSetting<WalletmixPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                UseSandbox = walletmixPaymentSettings.UseSandbox,
                SandboxURL = walletmixPaymentSettings.SandboxURL,
                MerchantID = walletmixPaymentSettings.MerchantID,
                WebsiteName = walletmixPaymentSettings.WebsiteName,
                Currency = walletmixPaymentSettings.Currency,
                AccessUsername = walletmixPaymentSettings.AccessUsername,
                AccessPassword = walletmixPaymentSettings.AccessPassword,
                CallbackURL = walletmixPaymentSettings.CallbackURL,
                AccessAppKey = walletmixPaymentSettings.AccessAppKey,
                AdditionalFee = walletmixPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = walletmixPaymentSettings.AdditionalFeePercentage,
                ActiveStoreScopeConfiguration = storeScope
            };
            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(walletmixPaymentSettings, x => x.UseSandbox, storeScope);
                model.SandboxURL_OverrideForStore = _settingService.SettingExists(walletmixPaymentSettings, x => x.SandboxURL, storeScope);
                model.MerchantID_OverrideForStore = _settingService.SettingExists(walletmixPaymentSettings, x => x.MerchantID, storeScope);
                model.WebsiteName_OverrideForStore = _settingService.SettingExists(walletmixPaymentSettings, x => x.WebsiteName, storeScope);
                model.Currency_OverrideForStore = _settingService.SettingExists(walletmixPaymentSettings, x => x.Currency, storeScope);
                model.AccessUsername_OverrideForStore = _settingService.SettingExists(walletmixPaymentSettings, x => x.AccessUsername, storeScope);
                model.AccessPassword_OverrideForStore = _settingService.SettingExists(walletmixPaymentSettings, x => x.AccessPassword, storeScope);
                model.CallbackURL_OverrideForStore = _settingService.SettingExists(walletmixPaymentSettings, x => x.CallbackURL, storeScope);
                model.AccessAppKey_OverrideForStore = _settingService.SettingExists(walletmixPaymentSettings, x => x.AccessAppKey, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(walletmixPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(walletmixPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            }

            return View("~/Plugins/Payments.Walletmix/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var walletmixPaymentSettings = _settingService.LoadSetting<WalletmixPaymentSettings>(storeScope);

            //save settings
            walletmixPaymentSettings.UseSandbox = model.UseSandbox;
            walletmixPaymentSettings.SandboxURL = model.SandboxURL;
            walletmixPaymentSettings.MerchantID = model.MerchantID;
            walletmixPaymentSettings.WebsiteName = model.WebsiteName;
            walletmixPaymentSettings.Currency = model.Currency;
            walletmixPaymentSettings.AccessUsername = model.AccessUsername;
            walletmixPaymentSettings.AccessPassword = model.AccessPassword;
            walletmixPaymentSettings.CallbackURL = model.CallbackURL;
            walletmixPaymentSettings.AccessAppKey = model.AccessAppKey;
            walletmixPaymentSettings.AdditionalFee = model.AdditionalFee;
            walletmixPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(walletmixPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(walletmixPaymentSettings, x => x.SandboxURL, model.SandboxURL_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(walletmixPaymentSettings, x => x.MerchantID, model.MerchantID_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(walletmixPaymentSettings, x => x.WebsiteName, model.WebsiteName_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(walletmixPaymentSettings, x => x.Currency, model.Currency_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(walletmixPaymentSettings, x => x.AccessUsername, model.AccessUsername_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(walletmixPaymentSettings, x => x.AccessPassword, model.AccessPassword_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(walletmixPaymentSettings, x => x.CallbackURL, model.CallbackURL_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(walletmixPaymentSettings, x => x.AccessAppKey, model.AccessAppKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(walletmixPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(walletmixPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        //action displaying notification (warning) to a store owner about inaccurate Walletmix rounding
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult RoundingWarning(bool passProductNamesAndTotals)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //prices and total aren't rounded, so display warning
            if (passProductNamesAndTotals && !_shoppingCartSettings.RoundPricesDuringCalculation)
                return Json(new { Result = _localizationService.GetResource("Plugins.Payments.Walletmix.RoundingWarning") });

            return Json(new { Result = string.Empty });
        }
        
        public IActionResult CancelOrder()
        {
            var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1).FirstOrDefault();
            if (order != null)
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });

            return RedirectToRoute("HomePage");
        }

        #endregion
    }
}