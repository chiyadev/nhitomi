using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace nhitomi.Controllers
{
    public class StripeServiceOptions
    {
        /// <summary>
        /// Stripe public API key.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Stripe secret API key.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Price in USD per month of supporter.
        /// </summary>
        public double SupporterPrice { get; set; } = 3;
    }

    public interface IStripeService
    {
        Task<Session> CreateSupporterCheckoutAsync(double amount, CancellationToken cancellationToken = default);
    }

    public class StripeService : IStripeService
    {
        readonly IStripeClient _client;
        readonly IOptionsMonitor<StripeServiceOptions> _options;
        readonly ILinkGenerator _link;

        public StripeService(IHttpClientFactory http, IOptionsMonitor<StripeServiceOptions> options, ILinkGenerator link)
        {
            _client  = new StripeClient(options.CurrentValue.SecretKey, null, new SystemNetHttpClient(http.CreateClient(nameof(StripeClient))));
            _options = options;
            _link    = link;
        }

        public async Task<Session> CreateSupporterCheckoutAsync(double amount, CancellationToken cancellationToken = default)
        {
            var options  = _options.CurrentValue;
            var duration = (int) Math.Floor(amount / options.SupporterPrice);

            return await new SessionService(_client).CreateAsync(new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long) Math.Floor(amount * 100),
                            Currency   = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name        = "nhitomi supporter",
                                Description = $"{duration} months of nhitomi supporter.",
                                Images      = new List<string> { _link.GetWebLink("/assets/logo-192x192.png") }
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode       = "payment",
                SuccessUrl = _link.GetWebLink("/support?checkout=success"),
                CancelUrl  = _link.GetWebLink("/support?checkout=canceled")
            }, null, cancellationToken);
        }
    }
}