using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Walletmix.Components
{
    [ViewComponent(Name = "PaymentWalletmix")]
    public class PaymentWalletmixViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.Walletmix/Views/PaymentInfo.cshtml");
        }
    }
}
