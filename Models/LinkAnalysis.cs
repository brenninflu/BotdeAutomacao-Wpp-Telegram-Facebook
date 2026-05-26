namespace OfertaBot.Models
{
    public class LinkAnalysis
    {
        public string OriginalLink { get; set; } = string.Empty;
        public string AffiliateLink { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Oferta? ProductData { get; set; }
    }
}
