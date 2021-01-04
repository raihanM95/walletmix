using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Walletmix
{
    public partial class RouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="routeBuilder">Route builder</param>
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            //PDT
            routeBuilder.MapRoute("Plugin.Payments.Walletmix.PDTHandler", "Plugins/PaymentWalletmix/PDTHandler",
                 new { controller = "PaymentWalletmix", action = "PDTHandler" });

            //IPN
            routeBuilder.MapRoute("Plugin.Payments.Walletmix.IPNHandler", "Plugins/PaymentWalletmix/IPNHandler",
                 new { controller = "PaymentWalletmix", action = "IPNHandler" });

            //Cancel
            routeBuilder.MapRoute("Plugin.Payments.Walletmix.CancelOrder", "Plugins/PaymentWalletmix/CancelOrder",
                 new { controller = "PaymentWalletmix", action = "CancelOrder" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority
        {
            get { return -1; }
        }
    }
}
