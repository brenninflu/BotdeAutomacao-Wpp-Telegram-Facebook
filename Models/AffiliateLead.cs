using System;
using System.Collections.Generic;

namespace OfertaBot.Models
{
    public class AffiliateLeadRequest
    {
        public string? AffiliateId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class AffiliateLead
    {
        public string AffiliateId { get; init; } = string.Empty;
        public string? Name { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? Message { get; init; }
        public Dictionary<string, string>? Metadata { get; init; }
        public string Source { get; init; } = "affiliate_page";
        public string? LandingPageUrl { get; init; }
    }

    /// <summary>
    /// Comissão liberada pelo Sales Cookie (integrado via Zapier)
    /// </summary>
    public class AffiliateCommission
    {
        public string AffiliateId { get; set; } = string.Empty;
        public decimal CommissionAmount { get; set; }
        public string? TransactionId { get; set; }
        public string Status { get; set; } = "released";
        public string? OrderId { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// Webhook payload do Sales Cookie para comissão liberada
    /// </summary>
    public class SalesCookieCommissionPayload
    {
        public string? affiliate_id { get; set; }
        public string? transaction_id { get; set; }
        public string? order_id { get; set; }
        public decimal? commission_amount { get; set; }
        public string? status { get; set; }
        public string? customer_email { get; set; }
        public Dictionary<string, object>? metadata { get; set; }
    }
}

