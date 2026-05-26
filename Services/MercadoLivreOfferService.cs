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
    public class MercadoLivreOfferService : IOfferScrapingService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MercadoLivreOfferService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public OfferPlatform Platform => OfferPlatform.MercadoLivre;

        public bool CanHandle(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return url.Contains("mercadolivre.", StringComparison.OrdinalIgnoreCase) ||
                   url.Contains("mercado.com.br", StringComparison.OrdinalIgnoreCase) ||
                   url.Contains("meli.la", StringComparison.OrdinalIgnoreCase);
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
                    Platform = OfferPlatform.MercadoLivre,
                    OriginalUrl = url,
                    ResolvedUrl = resolvedUrl,
                    ProductName = productName,
                    Price = price,
                    Currency = "BRL",
                    MainImageUrl = imageUrl,
                    AffiliateUrl = url,
                    RawHtml = html,
                    Coupon = coupon
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MERCADO-LIVRE] Falha ao processar oferta: {ex.Message}");
                return null;
            }
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient(nameof(MercadoLivreOfferService));
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
            client.DefaultRequestHeaders.Referrer = new Uri("https://www.mercadolivre.com.br/");
            return client;
        }

        private static async Task<string> ResolveUrlAsync(HttpClient client, string url, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
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
                return CleanName(WebUtility.HtmlDecode(ogTitle));

            var h1 = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText;
            if (!string.IsNullOrWhiteSpace(h1))
                return CleanName(WebUtility.HtmlDecode(h1));

            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText;
            return string.IsNullOrWhiteSpace(title) ? string.Empty : CleanName(WebUtility.HtmlDecode(title));
        }

        private static string CleanName(string name)
        {
            return name
                .Replace(" | Mercado Livre", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(" - Mercado Livre Brasil", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("Mercado Livre", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();
        }

        private static decimal ExtractPrice(HtmlDocument doc, string html)
        {
            var candidates = new List<PriceCandidate>();

            void AddCandidate(decimal price, string selector, int baseScore, string reason, string context = "")
            {
                if (price <= 0m)
                    return;

                if (!string.IsNullOrWhiteSpace(context) && IsInstallmentContext(context))
                {
                    Console.WriteLine($"[PRICE_IGNORED] seletor={selector} | preco={price.ToString("F2", CultureInfo.InvariantCulture)} | motivo=contexto de parcela/preço secundário");
                    return;
                }

                var plausibility = GetPlausibilityScore(price, context);
                if (plausibility < 0)
                {
                    Console.WriteLine($"[PRICE_IGNORED] seletor={selector} | preco={price.ToString("F2", CultureInfo.InvariantCulture)} | motivo=valor implausível");
                    return;
                }

                candidates.Add(new PriceCandidate(price, selector, baseScore, reason, plausibility));
                Console.WriteLine($"[PRICE_FOUND] seletor={selector} | preco={price.ToString("F2", CultureInfo.InvariantCulture)} | score={baseScore} | plausibility={plausibility} | motivo={reason}");
            }

            // 1) JSON-LD Product Offer
            var jsonLdNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (jsonLdNodes != null)
            {
                foreach (var node in jsonLdNodes)
                {
                    var script = node.InnerText ?? string.Empty;
                    if (script.IndexOf("\"price\"", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    foreach (Match match in Regex.Matches(script, "\"price\"\\s*:\\s*\"?(?<val>\\d+[.,]?\\d*)\"?", RegexOptions.IgnoreCase))
                    {
                        var price = ParsePrice(match.Groups["val"].Value);
                        if (price > 0m)
                        {
                            var context = GetContext(script, match.Index, match.Length);
                            AddCandidate(price, "json-ld", 500, "JSON-LD Product Offer", context);
                        }
                    }

                    foreach (Match match in Regex.Matches(script, @"R\$\s*(?<p>\d{1,3}([.\s]\d{3})*|\d+)(?<d>[.,]\d{2})", RegexOptions.IgnoreCase))
                    {
                        var whole = match.Groups["p"].Value.Replace(".", "").Replace(" ", "");
                        var decimals = match.Groups["d"].Value.Replace(".", "").Replace(",", ".");
                        var price = ParsePrice($"{whole}{decimals}");
                        if (price > 0m)
                        {
                            var context = GetContext(script, match.Index, match.Length);
                            AddCandidate(price, "json-ld-text", 180, "Valor textual em JSON-LD", context);
                        }
                    }
                }
            }

            // 2) Meta tags oficiais
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
                var price = ParsePrice(value);
                if (price > 0m)
                {
                    AddCandidate(price, $"meta[{metaName}]", priority, reason, value ?? string.Empty);
                }
            }

            // 3) Preço principal visível
            var visibleSelectors = new (string XPath, int Priority, string Reason)[]
            {
                ("//span[contains(@class, 'andes-money-amount__fraction')]", 340, "Preço principal visível (.andes-money-amount__fraction)"),
                ("//span[contains(@class, 'ui-pdp-price__part')]", 330, "Preço visível PDP"),
                ("//span[contains(@class, 'price-tag-fraction')]", 320, "Preço principal Mercado Livre"),
                ("//div[contains(@class, 'price-tag')]", 300, "Container de preço Mercado Livre"),
                ("//span[contains(@class, 'andes-money-amount')]", 280, "Bloco monetário visível")
            };

            foreach (var (xpath, priority, reason) in visibleSelectors)
            {
                var nodes = doc.DocumentNode.SelectNodes(xpath);
                if (nodes == null)
                    continue;

                foreach (var node in nodes)
                {
                    var text = WebUtility.HtmlDecode(node.InnerText ?? string.Empty);
                    var price = ExtractMoneyValue(text);
                    var context = node.ParentNode?.InnerHtml ?? node.OuterHtml ?? string.Empty;

                    if (price <= 0m)
                    {
                        // Se for buscar no contexto HTML, deve ser MUITO criterioso para não pegar IDs de classes (ex: 90113)
                        // Exigimos o prefixo R$ ao buscar em strings cruas de HTML para evitar capturar números que não são preços
                        price = ExtractMoneyValueWithSymbol(context);
                    }

                    if (price <= 0m)
                        continue;

                    AddCandidate(price, xpath, priority, reason, context);

                    var fractionNode = node.SelectSingleNode(".//span[contains(@class, 'andes-money-amount__fraction')]");
                    if (fractionNode != null)
                    {
                        var whole = NormalizeWhole(fractionNode.InnerText);
                        var centsNode = node.SelectSingleNode(".//span[contains(@class, 'andes-money-amount__cents')]");
                        var decimals = NormalizeFraction(centsNode?.InnerText);
                        var composed = ParsePrice($"{whole}{decimals}");

                        if (composed > 0m)
                        {
                            AddCandidate(composed, "visible-composed", priority + 10, "Preço principal composto", context);
                        }
                    }
                }
            }

            // 4) Fallback textual com validação forte
            foreach (Match match in Regex.Matches(
                         html,
                         @"R\$\s*(?<p>\d{1,3}([.\s]\d{3})*|\d+)(?<d>[.,]\d{2})",
                         RegexOptions.IgnoreCase))
            {
                var whole = match.Groups["p"].Value.Replace(".", "").Replace(" ", "");
                var decimals = match.Groups["d"].Value.Replace(".", "").Replace(",", ".");
                var price = ParsePrice($"{whole}{decimals}");
                if (price <= 0m)
                    continue;

                var context = GetContext(html, match.Index, match.Length);
                if (IsInstallmentContext(context))
                {
                    Console.WriteLine($"[PRICE_IGNORED] seletor=text-fallback | preco={price.ToString("F2", CultureInfo.InvariantCulture)} | motivo=contexto de parcela/preço secundário");
                    continue;
                }

                AddCandidate(price, "text-fallback", 120, "Fallback textual validado", context);
            }

            if (candidates.Count == 0)
            {
                Console.WriteLine("[PRICE_DEBUG] Nenhum candidato confiável encontrado.");
                return 0m;
            }

            var ordered = candidates
                .OrderByDescending(c => c.Plausibility)
                .ThenByDescending(c => c.Score)
                .ThenBy(c => c.Price)
                .ToList();

            foreach (var candidate in ordered)
            {
                Console.WriteLine($"[PRICE_CANDIDATE] seletor={candidate.Selector} | preco={candidate.Price.ToString("F2", CultureInfo.InvariantCulture)} | score={candidate.Score} | plausibility={candidate.Plausibility} | motivo={candidate.Reason}");
            }

            var chosen = ordered.First();

            Console.WriteLine($"[PRICE_SELECTED] seletor={chosen.Selector} | preco={chosen.Price.ToString("F2", CultureInfo.InvariantCulture)} | motivo={chosen.Reason} | plausibility={chosen.Plausibility}");
            return chosen.Price;
        }

        private static decimal ParsePrice(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return 0m;

            var cleaned = Regex.Replace(raw, @"[^\d,.]", "").Trim();
            if (string.IsNullOrEmpty(cleaned)) return 0m;

            if (cleaned.Contains(".") && cleaned.Contains(","))
            {
                if (cleaned.LastIndexOf(",") > cleaned.LastIndexOf("."))
                    cleaned = cleaned.Replace(".", "").Replace(",", ".");
                else
                    cleaned = cleaned.Replace(",", "");
            }
            else if (cleaned.Contains(","))
            {
                cleaned = cleaned.Replace(",", ".");
            }

            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result > 100000m ? 0m : result;
            }

            return 0m;
        }

        private static decimal ExtractMoneyValue(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return 0m;

            // Regex melhorada: exige R$ ou que o número tenha obrigatoriamente decimais (,00) 
            // para não confundir com IDs de produtos ou classes CSS
            var match = Regex.Match(
                raw,
                @"(?:R\$\s*)?(?<value>\d{1,3}([.\s]\d{3})*|\d+)(?<dec>[.,]\d{2})",
                RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                // Se não tiver decimais, tenta buscar apenas se tiver o R$ na frente
                match = Regex.Match(raw, @"R\$\s*(?<value>\d{1,3}([.\s]\d{3})*|\d+)", RegexOptions.IgnoreCase);
                if (!match.Success) return 0m;
            }

            var whole = NormalizeWhole(match.Groups["value"].Value);
            var decimals = match.Groups["dec"].Success ? NormalizeFraction(match.Groups["dec"].Value) : string.Empty;
            return ParsePrice($"{whole}{decimals}"); // Call the more robust ParsePrice
        }

        private static decimal ExtractMoneyValueWithSymbol(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return 0m;

            // Busca especificamente o padrão R$ seguido de números para evitar pegar IDs de CSS/Classes
            var match = Regex.Match(
                raw,
                @"R\$\s*(?<value>\d{1,3}([.\s]\d{3})*|\d+)(?<dec>[.,]\d{2})?",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                return 0m;

            return ParsePrice($"{match.Groups["value"].Value}{match.Groups["dec"].Value}"); // Call the more robust ParsePrice
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
                   lower.Contains("sem juros") ||
                   lower.Contains("/mês") ||
                   lower.Contains("/mes") ||
                   lower.Contains("mensal") ||
                   lower.Contains("juros") ||
                   lower.Contains("preço anterior") ||
                   lower.Contains("preco anterior") ||
                   lower.Contains("original") ||
                   lower.Contains("antigo") ||
                   lower.Contains("price-tag-old-price") ||
                   lower.Contains("old-price") ||
                   lower.Contains("frete") ||
                   lower.Contains("de r$") ||
                   lower.Contains("preço original") ||
                   lower.Contains("valor antigo") ||
                   lower.Contains("andes-money-amount--previous") ||
                   lower.Contains("ui-pdp-price__second-line") ||
                   Regex.IsMatch(lower, @"\b\d+\s*x\b") ||
                   Regex.IsMatch(lower, @"\bx\s*\d+\b") ||
                   Regex.IsMatch(lower, @"\d+\s*x\s*r\$") ||
                   Regex.IsMatch(lower, @"\b\d+x\b");
        }

        private static int GetPlausibilityScore(decimal price, string context)
        {
            if (price <= 0m)
                return -1;

            var score = 0;

            if (price >= 20m) score += 10;
            if (price >= 50m) score += 20;
            if (price >= 100m) score += 30;
            if (price >= 300m) score += 45;
            if (price >= 700m) score += 60;

            var lower = context.ToLowerInvariant();

            if (lower.Contains("por r$") || lower.Contains("por:") || lower.Contains("por apenas") || lower.Contains("à vista") || lower.Contains("a vista") || lower.Contains("preço atual") || lower.Contains("preco atual") || lower.Contains("price"))
                score += 25;

            if (lower.Contains("de r$") || lower.Contains("antes") || lower.Contains("economize") || lower.Contains("desconto"))
                score -= 15;

            if (IsInstallmentContext(lower))
                score -= 100;

            if (lower.Contains("r$ 19,90") || lower.Contains("19.90"))
                score -= 30;

            return score;
        }

        private static string GetContext(string html, int index, int length, int radius = 160)
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

            var imageMatch = Regex.Match(html, "\"secure_thumbnail\"\\s*:\\s*\"(?<url>[^\"]+)\"", RegexOptions.IgnoreCase);
            if (imageMatch.Success)
                return imageMatch.Groups["url"].Value;

            return string.Empty;
        }

        private static CouponData ExtractCoupon(string html)
        {
            var lowered = html.ToLowerInvariant();
            var coupon = new CouponData();

            if (lowered.Contains("frete grátis") || lowered.Contains("frete gratis"))
            {
                coupon.HasCoupon = true;
                coupon.Description = "frete gratis";
                coupon.DiscountType = "frete gratis";
            }

            var percentMatch = Regex.Match(lowered, @"(?<value>\d{1,3})\s*%\s*off", RegexOptions.IgnoreCase);
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

            var valueMatch = Regex.Match(lowered, @"r\$\s*(?<value>\d+[.,]\d{0,2}|\d+)\s*off", RegexOptions.IgnoreCase);
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

            var codeMatch = Regex.Match(lowered, @"cupom[^a-z0-9]{0,10}[:\-\s]?\s*(?<code>[a-z0-9]{3,25})", RegexOptions.IgnoreCase);
            if (codeMatch.Success)
            {
                coupon.HasCoupon = true;
                var code = codeMatch.Groups["code"].Value.Trim().ToUpperInvariant();
                if (!IsInvalidCouponToken(code))
                {
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

        private sealed record PriceCandidate(decimal Price, string Selector, int Score, string Reason, int Plausibility);
    }
}
