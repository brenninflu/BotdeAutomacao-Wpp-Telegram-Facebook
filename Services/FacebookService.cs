using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OfertaBot.Models;

namespace OfertaBot.Services
{
    public class FacebookService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public FacebookService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<bool> PublishOfferAsync(OfferData offer, CancellationToken cancellationToken = default)
        {
            if (offer == null) throw new ArgumentNullException(nameof(offer));

            if (string.IsNullOrWhiteSpace(Config.FacebookPageId) ||
                string.IsNullOrWhiteSpace(Config.FacebookAccessToken))
            {
                Console.WriteLine("[FACEBOOK] Configuração ausente. Publicação ignorada.");
                return false;
            }

            // Validação básica de sanidade das credenciais
            if (Config.FacebookPageId.StartsWith("EAA"))
            {
                Console.WriteLine("[FACEBOOK] ⚠️ ALERTA: FacebookPageId parece conter um Token. Verifique Config.cs!");
            }

            var message = BuildStandardMessage(offer);

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var endpoint = $"https://graph.facebook.com/v20.0/{Config.FacebookPageId}/feed";
                    Console.WriteLine($"[FACEBOOK_REQUEST] {DateTime.Now} | POST {endpoint}");

                    var client = _httpClientFactory.CreateClient(nameof(FacebookService));
                    client.Timeout = TimeSpan.FromSeconds(60);

                    // Usando a URL Original (curta) conforme solicitado
                    var linkToPublish = !string.IsNullOrWhiteSpace(offer.OriginalUrl) 
                        ? offer.OriginalUrl 
                        : offer.ResolvedUrl;

                    Console.WriteLine($"[FACEBOOK_DEBUG] Payload Data:");
                    Console.WriteLine($" -> Message: {message}");
                    Console.WriteLine($" -> Link: {linkToPublish}");

                    var payload = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["message"] = message,
                        ["link"] = linkToPublish,
                        ["access_token"] = Config.FacebookAccessToken
                    });

                    using var response = await client.PostAsync(endpoint, payload, cancellationToken).ConfigureAwait(false);
                    var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    
                    Console.WriteLine($"[FACEBOOK_RESPONSE] Status: {(int)response.StatusCode} {response.StatusCode}");
                    Console.WriteLine($"[FACEBOOK_RESPONSE] Body: {body}");

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    
                    // Se cair aqui, logamos o motivo da falha retornado pelo Facebook
                    Console.WriteLine($"[FACEBOOK] ❌ FALHA ({response.StatusCode}): {body}");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[FACEBOOK] Publicação cancelada.");
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FACEBOOK] 🚨 EXCEPTION: {ex.Message}");
                    Console.WriteLine($"[FACEBOOK] STACK: {ex.StackTrace}");
                }

                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken).ConfigureAwait(false);
            }

            return false;
        }

        private static string BuildStandardMessage(OfferData offer)
        {
            return $"🔥 {offer.ProductName}\n\n💰 R$ {offer.Price.ToString("F2", new CultureInfo("pt-BR"))}";
        }
    }
}
