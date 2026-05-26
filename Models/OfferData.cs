using System.Collections.Generic;

namespace OfertaBot.Models
{
    public enum OfferPlatform
    {
        Unknown = 0,
        MercadoLivre = 1,
        Amazon = 2
    }

    public sealed class CouponData
    {
        public bool HasCoupon { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public decimal? DiscountValue { get; set; }
        public string? DiscountType { get; set; }
    }

    public sealed class OfferData
    {
        public OfferPlatform Platform { get; set; } = OfferPlatform.Unknown;
        public string OriginalUrl { get; set; } = string.Empty;
        public string ResolvedUrl { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "BRL";
        public string MainImageUrl { get; set; } = string.Empty;
        public string AffiliateUrl { get; set; } = string.Empty;
        public string RawHtml { get; set; } = string.Empty;
        public CouponData Coupon { get; set; } = new();
        public List<string> AdditionalImages { get; set; } = new();
    }
}
