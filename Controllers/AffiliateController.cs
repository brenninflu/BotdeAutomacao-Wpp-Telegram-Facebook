using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OfertaBot.Models;
using OfertaBot.Services;

namespace OfertaBot.Controllers
{
    [ApiController]
    [Route("api/affiliate")]
    public class AffiliateController : ControllerBase
    {
        private readonly AffiliateLeadService _leadService;

        public AffiliateController(AffiliateLeadService leadService)
        {
            _leadService = leadService ?? throw new ArgumentNullException(nameof(leadService));
        }

        [HttpGet("click")]
        public IActionResult TrackAffiliateClick([FromQuery] string affiliateId, [FromQuery] string? redirectUrl)
        {
            if (string.IsNullOrWhiteSpace(affiliateId))
            {
                return BadRequest(new { message = "affiliateId é obrigatório." });
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = false,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(Config.AffiliateCookieDurationDays)
            };

            Response.Cookies.Append(Config.AffiliateCookieName, affiliateId, cookieOptions);
            Console.WriteLine($"[AFFILIATE] Cookie de afiliado configurado: {affiliateId}");

            var destination = !string.IsNullOrWhiteSpace(redirectUrl) ? redirectUrl : "/";
            return Redirect(destination);
        }

        [HttpPost("lead")]
        public async Task<IActionResult> CreateLead([FromBody] AffiliateLeadRequest request)
        {
            if (request == null) return BadRequest(new { message = "Requisição inválida." });

            var affiliateId = request.AffiliateId;
            if (string.IsNullOrWhiteSpace(affiliateId) && Request.Cookies.TryGetValue(Config.AffiliateCookieName, out var cookieValue))
            {
                affiliateId = cookieValue;
            }

            if (string.IsNullOrWhiteSpace(affiliateId))
            {
                return BadRequest(new { message = "AffiliateId não encontrado." });
            }

            var lead = new AffiliateLead
            {
                AffiliateId = affiliateId,
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Message = request.Message,
                Metadata = request.Metadata,
                LandingPageUrl = Request.Headers["Referer"].ToString()
            };

            var sent = await _leadService.SendLeadToZapierAsync(lead);
            if (!sent) return StatusCode(502, new { message = "Erro ao enviar lead para Zapier." });

            return Ok(new { message = "Lead recebido e enviado para Zapier." });
        }

        [HttpPost("commission")]
        public async Task<IActionResult> ReceiveCommission([FromBody] SalesCookieCommissionPayload payload)
        {
            if (payload == null) return BadRequest(new { message = "Payload é obrigatório" });

            try
            {
                var commission = await _leadService.ProcessCommissionWebhook(payload);
                if (commission == null) return BadRequest(new { message = "Erro ao processar dados" });

                Console.WriteLine($"[COMMISSION] ✅ Recebida: R$ {commission.CommissionAmount} | Afiliado: {commission.AffiliateId}");

                return Ok(new
                {
                    message = "Comissão processada com sucesso",
                    transactionId = commission.TransactionId,
                    orderId = commission.OrderId,
                    commissionAmount = commission.CommissionAmount,
                    status = commission.Status,
                    processedAt = commission.ProcessedAtUtc
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao processar comissão", error = ex.Message });
            }
        }

        [HttpGet("config")]
        public IActionResult GetAffiliateConfig()
        {
            return Ok(new
            {
                affiliateId = Config.ShopeeAffiliateId,
                cookieName = Config.AffiliateCookieName,
                zapierWebhookConfigured = !string.IsNullOrWhiteSpace(Config.ZapierLeadWebhookUrl),
                endpoints = new
                {
                    trackClick = $"{Request.Scheme}://{Request.Host}/api/affiliate/click?affiliateId={Config.ShopeeAffiliateId}",
                    submitLead = $"{Request.Scheme}://{Request.Host}/api/affiliate/lead",
                    receiveCommission = $"{Request.Scheme}://{Request.Host}/api/affiliate/commission"
                }
            });
        }
    }
}
