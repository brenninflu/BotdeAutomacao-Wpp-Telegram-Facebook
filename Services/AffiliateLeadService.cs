using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using OfertaBot.Data;
using OfertaBot.Models;

namespace OfertaBot.Services
{
    public class AffiliateLeadService
    {
        private readonly HttpClient _httpClient;
        private readonly OfertaBotDbContext _dbContext;

        public AffiliateLeadService(HttpClient httpClient, OfertaBotDbContext dbContext)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<bool> SendLeadToZapierAsync(AffiliateLead lead)
        {
            if (lead == null) throw new ArgumentNullException(nameof(lead));
            if (string.IsNullOrWhiteSpace(Config.ZapierLeadWebhookUrl))
            {
                Console.WriteLine("[ZAPIER] Zapier webhook URL não está configurada.");
                return false;
            }

            try
            {
                var payload = new
                {
                    affiliate_id = lead.AffiliateId,
                    name = lead.Name,
                    email = lead.Email,
                    phone = lead.Phone,
                    message = lead.Message,
                    source = lead.Source,
                    landing_page_url = lead.LandingPageUrl,
                    metadata = lead.Metadata
                };

                var response = await _httpClient.PostAsJsonAsync(Config.ZapierLeadWebhookUrl, payload);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[ZAPIER] Falha ao enviar lead: {response.StatusCode} {body}");
                    return false;
                }

                Console.WriteLine($"[ZAPIER] Lead enviado com sucesso para Zapier: {lead.Email ?? "sem email"}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZAPIER] Erro ao enviar lead: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Processa webhook de comissão liberada do Sales Cookie (Zap 2)
        /// </summary>
        public async Task<AffiliateCommission?> ProcessCommissionWebhook(SalesCookieCommissionPayload payload)
        {
            if (payload == null)
            {
                Console.WriteLine("[COMMISSION] Payload de comissão é nulo");
                return null;
            }

            try
            {
                var commission = new AffiliateCommission
                {
                    AffiliateId = payload.affiliate_id ?? Config.ShopeeAffiliateId,
                    CommissionAmount = payload.commission_amount ?? 0,
                    TransactionId = payload.transaction_id,
                    OrderId = payload.order_id,
                    CustomerEmail = payload.customer_email,
                    Status = payload.status ?? "released",
                    ProcessedAtUtc = DateTime.UtcNow,
                    Metadata = new()
                };

                // Adiciona qualquer metadata adicional
                if (payload.metadata != null)
                {
                    foreach (var kvp in payload.metadata)
                    {
                        commission.Metadata[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                    }
                }

                // Salva no banco de dados
                _dbContext.Commissions.Add(commission);
                await _dbContext.SaveChangesAsync();

                Console.WriteLine($"[COMMISSION] ✅ Comissão salva no banco: R$ {commission.CommissionAmount:F2} | Afiliado: {commission.AffiliateId} | Pedido: {commission.OrderId}");
                return commission;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[COMMISSION] ❌ Erro ao processar/salvar comissão: {ex.Message}");
                return null;
            }
        }
    }
}
