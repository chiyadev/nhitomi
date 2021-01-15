using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using nhitomi.Database;
using Stripe;
using Stripe.Checkout;

namespace nhitomi.Controllers
{
    public class StripeServiceOptions
    {
        /// <summary>
        /// Public API key.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Secret API key.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Webhook signing secret.
        /// </summary>
        public string WebhookSecret { get; set; }

        /// <summary>
        /// Price in USD per month of supporter.
        /// </summary>
        public double SupporterPrice { get; set; } = 3;

        /// <summary>
        /// Donation goal in USD.
        /// </summary>
        public double? DonationGoal { get; set; }
    }

    public interface IStripeService
    {
        Task<Session> CreateSupporterCheckoutAsync(DbUser user, double amount, CancellationToken cancellationToken = default);
        Task HandleSupporterCheckoutCompletedAsync(Session session, CancellationToken cancellationToken = default);

        Task<DbDonationInfo> GetDonationInfo(DateTime time, CancellationToken cancellationToken = default);
        Task<DbDonationInfo> AddDonationProgress(DateTime time, double amount, CancellationToken cancellationToken = default);
    }

    public class StripeService : IStripeService
    {
        readonly IStripeClient _stripe;
        readonly IElasticClient _client;
        readonly IOptionsMonitor<StripeServiceOptions> _options;
        readonly ILinkGenerator _link;
        readonly IWriteControl _writeControl;
        readonly IUserService _users;

        public StripeService(IHttpClientFactory http, IElasticClient client, IOptionsMonitor<StripeServiceOptions> options, ILinkGenerator link, IWriteControl writeControl, IUserService users)
        {
            _stripe       = new StripeClient(options.CurrentValue.SecretKey, null, new SystemNetHttpClient(http.CreateClient(nameof(StripeClient))));
            _client       = client;
            _options      = options;
            _link         = link;
            _writeControl = writeControl;
            _users        = users;
        }

        public async Task<Session> CreateSupporterCheckoutAsync(DbUser user, double amount, CancellationToken cancellationToken = default)
        {
            var options  = _options.CurrentValue;
            var duration = (int) Math.Floor(amount / options.SupporterPrice);

            return await new SessionService(_stripe).CreateAsync(new SessionCreateOptions
            {
                Mode               = "payment",
                PaymentMethodTypes = new List<string> { "card" },

                SuccessUrl = _link.GetWebLink("/support/pending"),
                CancelUrl  = _link.GetWebLink("/support"),

                ClientReferenceId = user.Id,
                CustomerEmail     = user.Email,

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
                                Description = $"{duration} {(duration == 1 ? "month" : "months")} of nhitomi supporter.",
                                Images      = new List<string> { _link.GetWebLink("/logo-192x192.png") }
                            }
                        },
                        Quantity = 1
                    }
                },

                Metadata = new Dictionary<string, string>
                {
                    ["duration"] = duration.ToString(),
                    ["amount"]   = amount.ToString(CultureInfo.InvariantCulture)
                }
            }, null, cancellationToken);
        }

        static string GetDonationInfoId(DateTime time) => $"{time.Year}-{time.Month}";

        public async Task<DbDonationInfo> GetDonationInfo(DateTime time, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbDonationInfo>(GetDonationInfoId(time), cancellationToken);

            do
            {
                if (entry.Value != null)
                    return entry.Value;

                entry.Value = new DbDonationInfo();
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            return entry.Value;
        }

        public async Task<DbDonationInfo> AddDonationProgress(DateTime time, double amount, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbDonationInfo>(GetDonationInfoId(time), cancellationToken);
            var goal  = _options.CurrentValue.DonationGoal ?? double.PositiveInfinity;

            do
            {
                entry.Value ??= new DbDonationInfo();

                entry.Value.Progress = Math.Max(0, Math.Min(goal, entry.Value.Progress + amount));
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            return entry.Value;
        }

        public async Task HandleSupporterCheckoutCompletedAsync(Session session, CancellationToken cancellationToken = default)
        {
            var duration = session.Metadata.TryGetValue("duration", out var x) && long.TryParse(x, out var y) ? y : 0;
            var amount   = session.Metadata.TryGetValue("amount", out var z) && double.TryParse(z, out var w) ? w : 0;

            if (duration == 0)
                return; // this shouldn't happen

            var now = DateTime.UtcNow;

            await using (await _writeControl.EnterAsync(cancellationToken))
            {
                var result = await _users.AddSupporterDurationAsync(session.ClientReferenceId, TimeSpan.FromDays(duration / 12.0 * 365.2422), amount, cancellationToken);

                if (result.IsT0)
                    await AddDonationProgress(now, amount, cancellationToken);
                else
                    throw new Exception($"User {session.ClientReferenceId} could not be found.");
            }
        }
    }
}