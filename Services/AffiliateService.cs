using System;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;

namespace OfertaBot.Services
{
    /// <summary>
    /// Serviço de transformação de links em links afiliados Shopee
    /// </summary>
    public class ShopeeAffiliateService
    {
        /// <summary>
        /// Transforma um link Shopee em um link afiliado
        /// </summary>
        public string TransformToAffiliateLink(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL não pode estar vazia");

            if (!url.Contains("shopee", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("URL deve ser um link Shopee");

            return TransformShopee(url);
        }

        private string TransformShopee(string url)
        {
            try
            {
                var cleanUrl = RemoveTrackingParams(url);
                var uri = new UriBuilder(cleanUrl);
                var query = QueryHelpers.ParseQuery(uri.Query);
                var parameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

                foreach (var pair in query)
                {
                    if (pair.Value.Count > 0)
                    {
                        parameters[pair.Key] = pair.Value[0];
                    }
                }

                parameters["affiliate_id"] = Config.ShopeeAffiliateId;
                uri.Query = QueryHelpers.AddQueryString(string.Empty, parameters).TrimStart('?');

                var result = uri.ToString();
                Console.WriteLine($"[AFFILIATE] Link transformado com sucesso");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AFFILIATE] Erro ao transformar link: {ex.Message}");
                throw;
            }
        }

        private string RemoveTrackingParams(string url)
        {
            var uri = new UriBuilder(url);
            var query = QueryHelpers.ParseQuery(uri.Query);
            var parameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in query)
            {
                if (pair.Key.Equals("utm_source", StringComparison.OrdinalIgnoreCase) ||
                    pair.Key.Equals("utm_medium", StringComparison.OrdinalIgnoreCase) ||
                    pair.Key.Equals("utm_campaign", StringComparison.OrdinalIgnoreCase) ||
                    pair.Key.Equals("affiliate_id", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (pair.Value.Count > 0)
                {
                    parameters[pair.Key] = pair.Value[0];
                }
            }

            uri.Query = QueryHelpers.AddQueryString(string.Empty, parameters).TrimStart('?');
            return uri.ToString();
        }
    }
}