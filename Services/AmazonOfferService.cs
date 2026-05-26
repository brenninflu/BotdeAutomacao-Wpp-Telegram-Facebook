using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using OfertaBot.Models;

namespace OfertaBot.Services
{
    public class AmazonOfferService : IOfferScrapingService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AmazonOfferService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public OfferPlatform Platform => OfferPlatform.Amazon;

        public bool CanHandle(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return url.Contains("amazon.", StringComparison.OrdinalIgnoreCase) ||
                   url.Contains("amzn.", StringComparison.OrdinalIgnoreCase) ||
                   url.Contains("a.co", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<OfferData?> ProcessAsync(string url, CancellationToken cancellationToken = default)
        {
            if (!CanHandle(url))
                return null;

            try
            {
                var client = CreateClient();
                var resolvedUrl = await ResolveUrlAsync(client, url, cancellationToken).ConfigureAwait(false);
                var html = await GetHtmlAsync(client, resolvedUrl, cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(html))
                    return null;

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var productName = ExtractProductName(doc, html);
                var price = ExtractPrice(doc, html);
                var imageUrl = ExtractImageUrl(doc, html);
                var coupon = ExtractCoupon(html);

                if (string.IsNullOrWhiteSpace(productName) || price <= 0m)
                    return null;

                return new OfferData
                {
                    Platform = OfferPlatform.Amazon,
                    OriginalUrl = url,
                    ResolvedUrl = resolvedUrl,
                    ProductName = productName,
                    Price = price,
                    Currency = "BRL",
                    MainImageUrl = imageUrl,
                    AffiliateUrl = resolvedUrl,
                    RawHtml = html,
                    Coupon = coupon
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AMAZON] Falha ao processar oferta: {ex.Message}");
                return null;
            }
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient(nameof(AmazonOfferService));
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
            return client;
        }

        private static async Task<string> ResolveUrlAsync(HttpClient client, string url, CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                return response.RequestMessage?.RequestUri?.ToString() ?? url;
            }
            catch
            {
                return url;
            }
        }

        private static async Task<string> GetHtmlAsync(HttpClient client, string url, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ExtractProductName(HtmlDocument doc, string html)
        {
            var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")
                ?.GetAttributeValue("content", string.Empty);

            if (!string.IsNullOrWhiteSpace(ogTitle))
                return CleanText(WebUtility.HtmlDecode(ogTitle));

            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText;
            if (!string.IsNullOrWhiteSpace(title))
                return CleanText(WebUtility.HtmlDecode(title));

            var h1 = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText;
            return string.IsNullOrWhiteSpace(h1) ? string.Empty : CleanText(WebUtility.HtmlDecode(h1));
        }

        private static decimal ExtractPrice(HtmlDocument doc, string html)
        {
            var candidates = new List<PriceCandidate>();

            void AddCandidate(decimal price, string selector, int score, string reason, string context = "")
            {
                if (price <= 0m)
                    return;

                if (!string.IsNullOrWhiteSpace(context) && IsInstallmentContext(context))
                {
                    Console.WriteLine($"[AMAZON][PRICE_IGNORED] seletor={selector} | preco={price.ToString("F2", CultureInfo.InvariantCulture)} | motivo=contexto de parcela/preço secundário");
                    return;
                }

                candidates.Add(new PriceCandidate(price, selector, score, reason));
                Console.WriteLine($"[AMAZON][PRICE_FOUND] seletor={selector} | preco={price.ToString("F2", CultureInfo.InvariantCulture)} | score={score} | motivo={reason}");
            }

            var jsonLdNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (jsonLdNodes != null)
            {
                foreach (var node in jsonLdNodes)
                {
                    var script = node.InnerText ?? string.Empty;
                    var jsonMatches = Regex.Matches(
                        script,
                        "\"price\"\\s*:\\s*\"?(?<price>[0-9]+[.,]?[0-9]{0,2})\"?",
                        RegexOptions.IgnoreCase);

                    foreach (Match match in jsonMatches)
                    {
                        var price = ReadPrice(match.Groups["price"].Value);
                        if (price > 0m)
                        {
                            AddCandidate(price, "json-ld", 500, "JSON-LD Product Offer", script);
                        }
                    }
                }
            }

            var metaSelectors = new[]
            {
                ("product:price:amount", 450, "Meta product:price:amount"),
                ("og:price:amount", 430, "Meta og:price:amount")
            };

            foreach (var (metaName, priority, reason) in metaSelectors)
            {
                var node = doc.DocumentNode.SelectSingleNode($"//meta[@property='{metaName}']")
                           ?? doc.DocumentNode.SelectSingleNode($"//meta[@name='{metaName}']");

                var value = node?.GetAttributeValue("content", string.Empty);
                var price = ReadPrice(value);
                if (price > 0m)
                {
                    AddCandidate(price, $"meta[{metaName}]", priority, reason, value ?? string.Empty);
                }
            }

            var visibleNodes = doc.DocumentNode.SelectNodes(
                "//span[contains(@class, 'a-price')] | //span[contains(@class, 'a-offscreen')] | //span[contains(@id, 'price')] | //span[contains(@class, 'priceToPay')]");

            if (visibleNodes != null)
            {
                foreach (var node in visibleNodes)
                {
                    var text = WebUtility.HtmlDecode(node.InnerText ?? string.Empty);
                    var context = node.ParentNode?.OuterHtml ?? node.OuterHtml ?? string.Empty;
                    var price = ExtractMoneyValue(text);
                    if (price <= 0m)
                    {
                        price = ExtractMoneyValue(context);
                    }

                    if (price > 0m)
                    {
                        AddCandidate(price, "visible-price", 300, "Preço principal visível", context);
                    }

                    var fractionNode = node.SelectSingleNode(".//span[contains(@class, 'a-price-whole')]");
                    var decimalNode = node.SelectSingleNode(".//span[contains(@class, 'a-price-fraction')]");

                    if (fractionNode != null)
                    {
                        var whole = NormalizeWhole(fractionNode.InnerText);
                        var decimals = NormalizeFraction(decimalNode?.InnerText);
                        var composed = ReadPrice($"{whole}{decimals}");
                        if (composed > 0m)
                        {
                            AddCandidate(composed, "visible-price-composed", 320, "Preço principal visível composto", context);
                        }
                    }
                }
            }

            var textMatches = Regex.Matches(
                html,
                @"R\$\s*(?<p>\d{1,3}([.\s]\d{3})*|\d+)(?<d>[.,]\d{2})",
                RegexOptions.IgnoreCase);

            foreach (Match match in textMatches)
            {
                var whole = match.Groups["p"].Value.Replace(".", "").Replace(" ", "");
                var decimals = match.Groups["d"].Value.Replace(".", "").Replace(",", ".");
                var price = ReadPrice($"{whole}{decimals}");
                if (price <= 0m)
                    continue;

                var context = GetContext(html, match.Index, match.Length);
                if (IsInstallmentContext(context))
                {
                    Console.WriteLine($"[AMAZON][PRICE_IGNORED] seletor=text-fallback | preco={price.ToString("F2", CultureInfo.InvariantCulture)} | motivo=contexto de parcela/preço secundário");
                    continue;
                }

                AddCandidate(price, "text-fallback", 80, "Fallback textual validado", context);
            }

            if (candidates.Count == 0)
            {
                Console.WriteLine("[AMAZON][PRICE_DEBUG] Nenhum candidato confiável encontrado.");
                return 0m;
            }

            foreach (var candidate in candidates
                         .OrderByDescending(c => c.Score)
                         .ThenBy(c => c.Price)) // Prioriza o menor (promoção)
            {
                Console.WriteLine($"[AMAZON][PRICE_CANDIDATE] seletor={candidate.Selector} | preco={candidate.Price.ToString("F2", CultureInfo.InvariantCulture)} | score={candidate.Score} | motivo={candidate.Reason}");
            }

            var chosen = candidates
                .OrderByDescending(c => c.Score)
                .ThenBy(c => c.Price)
                .First();

            Console.WriteLine($"[AMAZON][PRICE_SELECTED] seletor={chosen.Selector} | preco={chosen.Price.ToString("F2", CultureInfo.InvariantCulture)} | motivo={chosen.Reason}");
            return chosen.Price;
        }

        private static decimal ReadPrice(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return 0m;

            // Remove tudo que não for dígito, vírgula ou ponto
            var cleaned = Regex.Replace(raw, @"[^\d,.]", "").Trim();
            if (string.IsNullOrEmpty(cleaned)) return 0m;

            // Lógica para detectar separador decimal: se houver os dois, o último é o decimal
            if (cleaned.Contains(".") && cleaned.Contains(","))
            {
                if (cleaned.LastIndexOf(",") > cleaned.LastIndexOf("."))
                    cleaned = cleaned.Replace(".", "").Replace(",", "."); // 1.250,50 -> 1250.50
                else
                    cleaned = cleaned.Replace(",", ""); // 1,250.50 -> 1250.50
            }
            else if (cleaned.Contains(","))
            {
                cleaned = cleaned.Replace(",", "."); // 1250,50 -> 1250.50
            }

            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result > 100000m ? 0m : result;
            }

            return 0m;
        }

        private static decimal ExtractMoneyValue(string? raw)
        {
            // This method is already calling ReadPrice, which is now more robust.
            // No changes needed here, as ReadPrice handles the parsing logic.
            return ReadPrice(raw);
        }

        private static string NormalizeWhole(string value)
        {
            return Regex.Replace(value ?? string.Empty, @"[^\d]", string.Empty);
        }

        private static string NormalizeFraction(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var digits = Regex.Replace(value, @"[^\d]", string.Empty);
            return digits.Length == 1 ? $"{digits}0" : digits.PadRight(2, '0').Substring(0, 2);
        }

        private static bool IsInstallmentContext(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var lower = raw.ToLowerInvariant();
            return lower.Contains("parcela") ||
                   lower.Contains("parcelas") ||
                   lower.Contains("x de") ||
                   lower.Contains("x r$") ||
                   lower.Contains("sem juros") ||
                   lower.Contains("/mês") ||
                   lower.Contains("/mes") ||
                   lower.Contains("mensal") ||
                   lower.Contains("preço anterior") ||
                   lower.Contains("preco anterior") ||
                   lower.Contains("original") ||
                   lower.Contains("antigo") ||
                   lower.Contains("priceblock_dealprice") ||
                   lower.Contains("dealprice") ||
                   lower.Contains("economize") ||
                   lower.Contains("desconto") ||
                   lower.Contains("off") ||
                   lower.Contains("a-text-strike") ||
                   lower.Contains("price-strike") ||
                   lower.Contains("de r$") ||
                   lower.Contains("frete");
        }

        private static string GetContext(string html, int index, int length, int radius = 140)
        {
            var start = Math.Max(0, index - radius);
            var end = Math.Min(html.Length, index + length + radius);
            return html.Substring(start, end - start).ToLowerInvariant();
        }

        private static string ExtractImageUrl(HtmlDocument doc, string html)
        {
            var ogImage = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")
                ?.GetAttributeValue("content", string.Empty);

            if (!string.IsNullOrWhiteSpace(ogImage))
                return ogImage;

            var imageMatch = Regex.Match(html, "\"largeImage\"\\s*:\\s*\"(?<url>[^\"]+)\"", RegexOptions.IgnoreCase);
            if (imageMatch.Success)
                return imageMatch.Groups["url"].Value;

            return string.Empty;
        }

        private static string CleanText(string value)
        {
            return value
                .Replace(" | Amazon", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(" - Amazon.com", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Amazon.com", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();
        }

        private static CouponData ExtractCoupon(string html)
        {
            var lower = html.ToLowerInvariant();
            var coupon = new CouponData();

            // Fallback para Frete Grátis (detecta primeiro, mas cupons reais sobrescrevem o texto)
            if (lower.Contains("frete grátis") || lower.Contains("frete gratis"))
            {
                coupon.HasCoupon = true;
                coupon.DiscountType = "frete gratis";
                coupon.Description = "frete gratis";
            }

            var percentMatch = Regex.Match(lower, @"(?<value>\d{1,3})\s*%\s*off", RegexOptions.IgnoreCase);
            if (percentMatch.Success)
            {
                var val = percentMatch.Groups["value"].Value;
                if (!IsInvalidCouponToken(val))
                {
                    coupon.HasCoupon = true;
                    coupon.DiscountType = $"{val}% OFF";
                    coupon.Description = coupon.DiscountType;
                    if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var percent))
                    {
                        coupon.DiscountValue = percent;
                    }
                }
            }

            var valueMatch = Regex.Match(lower, @"r\$\s*(?<value>\d+[.,]\d{0,2}|\d+)\s*off", RegexOptions.IgnoreCase);
            if (valueMatch.Success)
            {
                coupon.HasCoupon = true;
                var val = valueMatch.Groups["value"].Value;
                if (!IsInvalidCouponToken(val))
                {
                    coupon.DiscountType = $"R$ {val.Replace(".", ",")} OFF";
                    coupon.Description = coupon.DiscountType;

                    var raw = val.Replace(".", "").Replace(",", ".");
                    if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                    {
                        coupon.DiscountValue = value;
                    }
                }
            }

            var codeMatch = Regex.Match(lower, @"cupom[^a-z0-9]{0,10}[:\-\s]?\s*(?<code>[a-z0-9]{3,25})", RegexOptions.IgnoreCase);
            if (codeMatch.Success)
            {
                var code = codeMatch.Groups["code"].Value.Trim().ToUpperInvariant();
                if (!IsInvalidCouponToken(code))
                {
                    coupon.HasCoupon = true;
                    coupon.Code = code;
                    if (string.IsNullOrWhiteSpace(coupon.Description))
                    {
                        coupon.Description = "Cupom detectado";
                    }
                }
            }

            if (!coupon.HasCoupon && string.IsNullOrWhiteSpace(coupon.Code) && string.IsNullOrWhiteSpace(coupon.DiscountType))
            {
                return new CouponData();
            }

            return coupon;
        }

        private static bool IsInvalidCouponToken(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalized = value.Trim();
            return normalized.Equals("AMOUNT", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals("VALUE", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals("CODE", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals("TOKEN", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals("OFF", StringComparison.OrdinalIgnoreCase) || // Added
                   normalized.Equals("DESCONTO", StringComparison.OrdinalIgnoreCase) || // Added
                   normalized.Equals("EMPTY", StringComparison.OrdinalIgnoreCase); // Added
        }

        private sealed record PriceCandidate(decimal Price, string Selector, int Score, string Reason);
    }
}
