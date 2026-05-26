using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OfertaBot.Services;

namespace OfertaBot.Controllers
{
    [ApiController]
    [Route("api/manual")]
    public class ManualControlController : ControllerBase
    {
        private readonly WhatsAppWebAutomationService _automationService;

        public ManualControlController(WhatsAppWebAutomationService automationService)
        {
            _automationService = automationService;
        }

        /// <summary>
        /// Retorna o status atual da conexão do WhatsApp
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                connected = _automationService.IsLoggedIn,
                botNumber = Config.WhatsAppNumber,
                version = Config.BotVersion,
                autoPostEnabled = Config.AutoPostEnabled,
                intervalMinutes = Config.OfferIntervalMinutes,
                targetGroups = Config.TargetGroups,
                qrCodeUrl = $"{Config.ServerUrl}/qrcode"
            });
        }

        /// <summary>
        /// Lista todos os grupos que o bot "vê" na lista de conversas
        /// </summary>
        [HttpGet("list-groups")]
        public async Task<IActionResult> ListGroups()
        {
            if (!_automationService.IsLoggedIn) return BadRequest("Bot não conectado.");
            var groups = await _automationService.ListGroupsAsync();
            return Ok(new { count = groups.Count, groups });
        }

        /// <summary>
        /// Envia uma mensagem manual para um grupo ou contato específico
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendManual([FromBody] SendRequest request)
        {
            if (!_automationService.IsLoggedIn) return BadRequest("Bot não conectado.");
            if (string.IsNullOrEmpty(request.Target) || string.IsNullOrEmpty(request.Message)) 
                return BadRequest("Target e Message são obrigatórios.");

            bool success = await _automationService.SendMessageAsync(request.Target, request.Message);
            if (success) return Ok(new { status = "success", message = "Mensagem enviada!" });
            return StatusCode(500, new { status = "error", message = "Falha ao enviar." });
        }

        /// <summary>
        /// Adiciona um grupo à lista de postagem automática
        /// </summary>
        [HttpPost("groups/add")]
        public IActionResult AddGroup([FromQuery] string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) return BadRequest();
            if (!Config.TargetGroups.Contains(groupName))
            {
                Config.TargetGroups.Add(groupName);
                return Ok(new { message = $"Grupo '{groupName}' adicionado.", currentGroups = Config.TargetGroups });
            }
            return Ok(new { message = "Grupo já está na lista." });
        }

        /// <summary>
        /// Remove um grupo da lista de postagem automática
        /// </summary>
        [HttpDelete("groups/remove")]
        public IActionResult RemoveGroup([FromQuery] string groupName)
        {
            if (Config.TargetGroups.Remove(groupName))
            {
                return Ok(new { message = $"Grupo '{groupName}' removido.", currentGroups = Config.TargetGroups });
            }
            return NotFound("Grupo não encontrado na lista.");
        }

        /// <summary>
        /// Dispara uma das ofertas da lista manual imediatamente
        /// </summary>
        [HttpPost("trigger-offer")]
        public async Task<IActionResult> TriggerOffer([FromQuery] int? index)
        {
            if (!_automationService.IsLoggedIn) return BadRequest("Bot não conectado.");
            
            string offer;
            if (index.HasValue && index >= 0 && index < Config.ManualOffers.Count)
            {
                offer = Config.ManualOffers[index.Value];
            }
            else
            {
                var rand = new Random();
                offer = Config.ManualOffers[rand.Next(Config.ManualOffers.Count)];
            }

            foreach (var group in Config.TargetGroups)
            {
                await _automationService.SendMessageAsync(group, offer);
            }

            return Ok(new { message = "Oferta disparada manualmente para todos os grupos." });
        }
    }

    public class SendRequest
    {
        public string Target { get; set; } = string.Empty; // Nome do grupo ou contato
        public string Message { get; set; } = string.Empty;
    }
}
