using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OfertaBot.Services
{
    public class LinkDetectionService
    {
        public bool ContainsLink(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var urlPattern = @"(https?://[^\s]+|www\.[^\s]+)";
            return Regex.IsMatch(text, urlPattern);
        }

        public List<string> ExtractLinks(string text)
        {
            var links = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return links;

            var urlPattern = @"(https?://[^\s]+)";
            var matches = Regex.Matches(text, urlPattern);

            foreach (Match match in matches)
            {
                links.Add(match.Value.Trim());
            }

            return links;
        }

        public bool IsShopeeLink(string link)
        {
            return !string.IsNullOrWhiteSpace(link) &&
                   (link.Contains("shopee", StringComparison.OrdinalIgnoreCase) ||
                    link.Contains("s.shopee", StringComparison.OrdinalIgnoreCase));
        }

        public bool IsAmazonLink(string link)
        {
            return !string.IsNullOrWhiteSpace(link) &&
                   (link.Contains("amazon.", StringComparison.OrdinalIgnoreCase) ||
                    link.Contains("amzn.", StringComparison.OrdinalIgnoreCase) ||
                    link.Contains("a.co", StringComparison.OrdinalIgnoreCase));
        }

        public bool IsMercadoLivreLink(string link)
        {
            return !string.IsNullOrWhiteSpace(link) &&
                   (link.Contains("mercadolivre.", StringComparison.OrdinalIgnoreCase) ||
                    link.Contains("mercado.com.br", StringComparison.OrdinalIgnoreCase) ||
                    link.Contains("meli.la", StringComparison.OrdinalIgnoreCase));
        }

        public List<string> FilterShopeeLinks(List<string> links)
        {
            return links.Where(IsShopeeLink).ToList();
        }

        public List<string> FilterAmazonLinks(List<string> links)
        {
            return links.Where(IsAmazonLink).ToList();
        }

        public List<string> FilterMercadoLivreLinks(List<string> links)
        {
            return links.Where(IsMercadoLivreLink).ToList();
        }

        public string? GetValidShopeeLink(string text)
        {
            var links = ExtractLinks(text);
            var shopeeLinks = FilterShopeeLinks(links);

            if (shopeeLinks.Count == 0)
            {
                return null;
            }

            return shopeeLinks[0];
        }

        public string? GetFirstSupportedMarketplaceLink(string text)
        {
            var links = ExtractLinks(text);

            foreach (var link in links)
            {
                if (IsAmazonLink(link) || IsMercadoLivreLink(link))
                {
                    return link;
                }
            }

            if (IsAmazonLink(text) || IsMercadoLivreLink(text))
            {
                return text.Trim();
            }

            return null;
        }
    }
}
