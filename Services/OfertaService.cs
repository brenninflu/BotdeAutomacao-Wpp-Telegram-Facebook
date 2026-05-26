using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OfertaBot.Models;

namespace OfertaBot.Services
{
    public class OfertaService
    {
        private readonly List<List<string>> _productLists;
        private readonly int[] _currentIndexes;
        private int _currentListIndex;

        public OfertaService()
        {
            _productLists = Config.ProductLists ?? new List<List<string>>();
            _currentIndexes = new int[_productLists.Count];
        }

        public async Task<Oferta?> GetNextOfertaAsync()
        {
            var totalLinks = _productLists.Sum(list => list.Count);
            if (totalLinks == 0) return null;

            for (var attempt = 0; attempt < totalLinks; attempt++)
            {
                if (_productLists.Count == 0)
                    return null;

                if (_productLists[_currentListIndex].Count == 0)
                {
                    _currentListIndex = (_currentListIndex + 1) % _productLists.Count;
                    continue;
                }

                var currentList = _productLists[_currentListIndex];
                var currentIndex = _currentIndexes[_currentListIndex];
                var link = currentList[currentIndex];

                _currentIndexes[_currentListIndex] = (currentIndex + 1) % currentList.Count;
                _currentListIndex = (_currentListIndex + 1) % _productLists.Count;

                var oferta = await AdicionarOfertaManualAsync(link);
                if (oferta != null)
                    return oferta;
            }

            return null;
        }

        public async Task<Oferta?> AdicionarOfertaManualAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine("[ERROR][SCRAP] URL inválida.");
                return null;
            }

            string nome = string.Empty;
            decimal? precoAtual = null;
            decimal? precoOriginal = null;
            int desconto = 0;
            var imagens = new List<string>();
            string html = string.Empty;

            try
            {
                using var handler = new HttpClientHandler { AllowAutoRedirect = true };
                using var client = new HttpClient(handler);

                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                client.DefaultRequestHeaders.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                client.DefaultRequestHeaders.Add("Referer", "https://www.mercadolivre.com.br/");
                client.DefaultRequestHeaders.Add("DNT", "1");
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
                client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");

                Console.WriteLine($"[HTTP] Requisição: {url}");
                // Pequeno delay para simular comportamento humano
                await Task.Delay(1000);
                var resp = await client.GetAsync(url);
                var finalUrl = resp.RequestMessage?.RequestUri?.ToString() ?? url;
                Console.WriteLine($"[REDIRECT] URL final: {finalUrl}");

                if (finalUrl.Contains("error_page", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[BLOCKED] Redirecionado para error_page. Ignorando oferta.");
                    return null;
                }

                html = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] HTML size: {html.Length} characters");

                // Verifica se é produto do Mercado Livre
                bool isMercadoLivre = finalUrl.Contains("mercado.com.br") || finalUrl.Contains("mercadolivre.com");

                // Nome - múltiplas tentativas para ML
                if (isMercadoLivre)
                {
                    // Tenta og:title primeiro
                    var nameMatch = System.Text.RegularExpressions.Regex.Match(html, "<meta property=\"og:title\" content=\"(.*?)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (nameMatch.Success)
                    {
                        nome = System.Net.WebUtility.HtmlDecode(nameMatch.Groups[1].Value.Trim());
                    }
                    else
                    {
                        // Tenta h1 com classes específicas do ML
                        var h1Match = System.Text.RegularExpressions.Regex.Match(html, "<h1[^>]*class=\"[^\"]*item-title[^\"]*\"[^>]*>(.*?)</h1>", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                        if (h1Match.Success)
                        {
                            nome = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(h1Match.Groups[1].Value, "<[^>]+>", "").Trim());
                        }
                        else
                        {
                            // Tenta qualquer h1
                            h1Match = System.Text.RegularExpressions.Regex.Match(html, "<h1[^>]*>(.*?)</h1>", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                            if (h1Match.Success)
                            {
                                nome = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(h1Match.Groups[1].Value, "<[^>]+>", "").Trim());
                            }
                            else
                            {
                                // Última tentativa: title da página
                                var titleMatch = System.Text.RegularExpressions.Regex.Match(html, "<title>(.*?)</title>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                if (titleMatch.Success)
                                {
                                    nome = System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim());
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(nome))
                    {
                        // Limpa o nome
                        nome = nome.Replace(" | Mercado Livre", "").Replace(" - Mercado Livre Brasil", "").Replace("Mercado Livre", "").Trim();
                        Console.WriteLine($"[SCRAP] Nome: {nome}");
                    }
                }
                else
                {
                    // Lógica original para outros sites
                    var nameMatch = System.Text.RegularExpressions.Regex.Match(html, "<meta property=\"og:title\" content=\"(.*?)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (nameMatch.Success)
                    {
                        nome = System.Net.WebUtility.HtmlDecode(nameMatch.Groups[1].Value.Trim());
                        Console.WriteLine($"[SCRAP] Nome: {nome}");
                    }
                }

                // Imagem (og:image)
                var imgMatch = System.Text.RegularExpressions.Regex.Match(html, "<meta property=\"og:image\" content=\"(.*?)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (imgMatch.Success)
                {
                    var imgUrl = imgMatch.Groups[1].Value;
                    if (!imgUrl.Contains("data:image") && !imgUrl.Contains("base64"))
                    {
                        imagens.Add(imgUrl);
                        Console.WriteLine($"[SCRAP] Imagem: {imgUrl}");
                    }
                }

                // Preço - múltiplas tentativas para ML
                if (isMercadoLivre)
                {
                    // Tenta JSON-LD structured data
                    var jsonLdMatch = System.Text.RegularExpressions.Regex.Match(html, "\"price\":\\s*\"?([0-9]+[.,][0-9]{2})\"?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (jsonLdMatch.Success)
                    {
                        precoAtual = decimal.Parse(jsonLdMatch.Groups[1].Value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                        Console.WriteLine($"[SCRAP] Preço (JSON-LD): {precoAtual}");
                    }
                    else
                    {
                        // Tenta preço em elementos específicos do ML
                        var priceSelectors = new[]
                        {
                            @"<span[^>]*class=""[^""]*price[^""]*""[^>]*>(?:<[^>]*>)*R\$\s*([0-9]+[.,][0-9]{2})",
                            @"<div[^>]*class=""[^""]*price[^""]*""[^>]*>(?:<[^>]*>)*R\$\s*([0-9]+[.,][0-9]{2})",
                            @"R\$\s*([0-9]+[.,][0-9]{2})"
                        };

                        foreach (var selector in priceSelectors)
                        {
                            var priceMatch = System.Text.RegularExpressions.Regex.Match(html, selector, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (priceMatch.Success)
                            {
                                precoAtual = decimal.Parse(priceMatch.Groups[1].Value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                                Console.WriteLine($"[SCRAP] Preço encontrado: {precoAtual}");
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Lógica original para Shopee
                    var precoMatch = System.Text.RegularExpressions.Regex.Match(html, @"R\$\s?([0-9]+[.,][0-9]{2})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (precoMatch.Success)
                    {
                        precoAtual = decimal.Parse(precoMatch.Groups[1].Value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                        Console.WriteLine($"[SCRAP] Preço: {precoAtual}");
                    }
                    else
                    {
                        var jsonPriceMatch = System.Text.RegularExpressions.Regex.Match(html, "\\\"price\\\":\\s*([0-9]+[.,][0-9]{2})");
                        if (jsonPriceMatch.Success)
                        {
                            precoAtual = decimal.Parse(jsonPriceMatch.Groups[1].Value.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                            Console.WriteLine($"[SCRAP] Preço (JSON): {precoAtual}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR][SCRAP] Falha: {ex.Message}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(nome) || precoAtual == null || precoAtual == 0)
            {
                Console.WriteLine("[BLOCKED] Oferta inválida (nome vazio ou preço 0). Ignorada.");
                return null;
            }

            precoOriginal = precoAtual * 2;
            desconto = (int)(100 - ((precoAtual.Value / precoOriginal.Value) * 100));
            if (desconto < 0) desconto = 0;

            if (imagens.Count == 0)
            {
                imagens.Add("Assets/fallback1.jpg");
                imagens.Add("Assets/fallback2.jpg");
                Console.WriteLine("[FALLBACK] Imagens fallback usadas");
            }

            for (var i = 0; i < imagens.Count; i++)
            {
                if (!imagens[i].StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[LOCAL] Usando imagem local: {imagens[i]}");
                }
            }

            // Cupom (prioriza maior desconto: % primeiro, depois R$; se frete grátis, retorna como melhor quando detectado)
            var cupom = ExtractCupom(html);

            Console.WriteLine($"[LINK] Link preservado: {url}");
            return new Oferta
            {
                Nome = nome,
                PrecoOriginal = precoOriginal.Value,
                PrecoAtual = precoAtual.Value,
                Desconto = desconto,
                Link = url,
                Avaliacao = 0,
                Imagens = imagens,

                HasCupom = cupom.hasCupom,
                CupomTipoDesconto = cupom.tipoDesconto,
                CupomCodigo = cupom.codigo,
                CupomValorNumerico = cupom.valorNumerico
            };
        }

        private static (bool hasCupom, string tipoDesconto, string codigo, decimal valorNumerico) ExtractCupom(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return (false, string.Empty, string.Empty, 0m);

            var lowered = html.ToLowerInvariant();

            // 1) Frete grátis
            if (lowered.Contains("frete grátis") || lowered.Contains("frete gratis"))
                return (true, "Frete grátis", "BARAPROMO", 0m);

            // 2) Percentual: "50% OFF"
            var percentMatches = System.Text.RegularExpressions.Regex.Matches(
                lowered,
                @"(\d{1,3})\s*%\s*off",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // 3) Valor fixo: "R$20 OFF"
            var valueMatches = System.Text.RegularExpressions.Regex.Matches(
                lowered,
                @"r\$\s*([0-9]+[.,][0-9]{0,2}|[0-9]+)\s*off",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Extrai código do cupom (ex: "CUPOM: ABC", "cupom ABC")
            string codigoDetectado = string.Empty;
            var codigoMatch = System.Text.RegularExpressions.Regex.Match(
                lowered,
                @"cupom[^a-z0-9]{0,10}[:\-\s]?\s*([a-z0-9]{3,25})",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (codigoMatch.Success && codigoMatch.Groups.Count >= 2)
            {
                codigoDetectado = codigoMatch.Groups[1].Value.Trim();
            }

            // Regra de priorização: maior %; depois maior desconto em R$
            if (percentMatches.Count > 0)
            {
                int bestPercent = -1;
                foreach (System.Text.RegularExpressions.Match m in percentMatches)
                {
                    if (!m.Success || m.Groups.Count < 2) continue;
                    if (int.TryParse(m.Groups[1].Value, out var p) && p > bestPercent)
                        bestPercent = p;
                }

                if (bestPercent > 0)
                {
                    // Não inventar cupom: se não achou código, considera como sem cupom
                    if (string.IsNullOrWhiteSpace(codigoDetectado))
                        return (false, string.Empty, string.Empty, 0m);

                    return (true, $"{bestPercent}% OFF", codigoDetectado, bestPercent);
                }
            }

            if (valueMatches.Count > 0)
            {
                decimal bestValue = 0m;
                bool found = false;

                foreach (System.Text.RegularExpressions.Match m in valueMatches)
                {
                    if (!m.Success || m.Groups.Count < 2) continue;

                    var raw = m.Groups[1].Value.Replace(".", "").Replace(",", ".");
                    if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
                    {
                        if (!found || v > bestValue)
                        {
                            bestValue = v;
                            found = true;
                        }
                    }
                }

                if (found && bestValue > 0m)
                {
                    if (string.IsNullOrWhiteSpace(codigoDetectado))
                        return (false, string.Empty, string.Empty, 0m);

                    var valueText = bestValue % 1m == 0m
                        ? bestValue.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
                        : bestValue.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

                    valueText = valueText.Replace(".", ",");
                    return (true, $"R${valueText} OFF", codigoDetectado, bestValue);
                }
            }

            return (false, string.Empty, string.Empty, 0m);
        }
    }
}
