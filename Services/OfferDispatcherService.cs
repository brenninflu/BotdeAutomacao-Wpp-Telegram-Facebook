using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OfertaBot.Models;

namespace OfertaBot.Services
{
    public class OfferDispatcherService
    {
        private readonly LinkDetectionService _linkDetectionService;
        private readonly IEnumerable<IOfferScrapingService> _scrapingServices;
        private readonly WhatsAppWebAutomationService _whatsappService;
        private readonly TelegramService _telegramService;
        private readonly FacebookService _facebookService;
        private readonly MensagemService _mensagemService;

        public OfferDispatcherService(
            LinkDetectionService linkDetectionService,
            IEnumerable<IOfferScrapingService> scrapingServices,
            WhatsAppWebAutomationService whatsappService,
            TelegramService telegramService,
            FacebookService facebookService,
            MensagemService mensagemService)
        {
            _linkDetectionService = linkDetectionService ?? throw new ArgumentNullException(nameof(linkDetectionService));
            _scrapingServices = scrapingServices ?? throw new ArgumentNullException(nameof(scrapingServices));
            _whatsappService = whatsappService ?? throw new ArgumentNullException(nameof(whatsappService));
            _telegramService = telegramService ?? throw new ArgumentNullException(nameof(telegramService));
            _facebookService = facebookService ?? throw new ArgumentNullException(nameof(facebookService));
            _mensagemService = mensagemService ?? throw new ArgumentNullException(nameof(mensagemService));
        }

        public async Task DispatchAsync(string rawMessageOrUrl, IEnumerable<string>? whatsappTargets = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawMessageOrUrl))
            {
                Console.WriteLine("[DISPATCH] Conteúdo vazio ignorado.");
                return;
            }

            var targets = (whatsappTargets ?? Config.TargetGroups).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            var offer = await ResolveOfferAsync(rawMessageOrUrl, cancellationToken).ConfigureAwait(false);

            if (offer == null)
            {
                Console.WriteLine("[DISPATCH] Nenhuma plataforma suportada detectada. Enviando conteúdo bruto para WhatsApp.");
                await SendWhatsAppRawAsync(rawMessageOrUrl, targets, cancellationToken).ConfigureAwait(false);
                return;
            }

            Console.WriteLine($"[DISPATCH] Oferta resolvida | Plataforma={offer.Platform} | Produto={offer.ProductName} | Preço={offer.Price.ToString("F2")}");

            var whatsappMessage = _mensagemService.GerarMensagem(offer);

            await SendWhatsAppAsync(whatsappMessage, targets, cancellationToken).ConfigureAwait(false);

            if (Config.UseTelegram)
            {
                await TrySendTelegramAsync(offer, cancellationToken).ConfigureAwait(false);
            }

            if (Config.UseFacebook)
            {
                await TryPublishFacebookAsync(offer, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<OfferData?> ResolveOfferAsync(string rawMessageOrUrl, CancellationToken cancellationToken = default)
        {
            var link = GetFirstSupportedLink(rawMessageOrUrl);
            if (string.IsNullOrWhiteSpace(link))
            {
                return null;
            }

            var service = ResolveService(link);
            if (service == null)
            {
                Console.WriteLine($"[DISPATCH] Nenhum serviço encontrado para o link: {link}");
                return null;
            }

            Console.WriteLine($"[DISPATCH] Processando com {service.GetType().Name}: {link}");
            return await service.ProcessAsync(link, cancellationToken).ConfigureAwait(false);
        }

        private IOfferScrapingService? ResolveService(string url)
        {
            return _scrapingServices.FirstOrDefault(service => service.CanHandle(url));
        }

        private string? GetFirstSupportedLink(string text)
        {
            var links = _linkDetectionService.ExtractLinks(text);
            if (links.Count == 0)
            {
                if (_linkDetectionService.IsAmazonLink(text) || _linkDetectionService.IsMercadoLivreLink(text))
                {
                    return text.Trim();
                }

                return null;
            }

            foreach (var link in links)
            {
                if (_linkDetectionService.IsAmazonLink(link) || _linkDetectionService.IsMercadoLivreLink(link))
                {
                    return link;
                }
            }

            return null;
        }

        private async Task SendWhatsAppRawAsync(string message, List<string> targets, CancellationToken cancellationToken)
        {
            foreach (var target in targets)
            {
                await TrySendWhatsAppAsync(target, message, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SendWhatsAppAsync(string message, List<string> targets, CancellationToken cancellationToken)
        {
            foreach (var target in targets)
            {
                await TrySendWhatsAppAsync(target, message, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task TrySendWhatsAppAsync(string target, string message, CancellationToken cancellationToken)
        {
            try
            {
                if (!_whatsappService.IsLoggedIn)
                {
                    Console.WriteLine($"[DISPATCH][WHATSAPP] Bot desconectado. Grupo '{target}' ignorado.");
                    return;
                }

                var success = await _whatsappService.SendMessageAsync(target, message).ConfigureAwait(false);
                if (success)
                {
                    Console.WriteLine($"[DISPATCH][WHATSAPP] Enviado com sucesso para '{target}'.");
                }
                else
                {
                    Console.WriteLine($"[DISPATCH][WHATSAPP] Falha ao enviar para '{target}'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISPATCH][WHATSAPP] Erro ao enviar para '{target}': {ex.Message}");
            }
        }

        private async Task TrySendTelegramAsync(OfferData offer, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _telegramService.SendOfferAsync(offer, cancellationToken).ConfigureAwait(false);
                Console.WriteLine(success
                    ? "[DISPATCH][TELEGRAM] Oferta enviada com sucesso."
                    : "[DISPATCH][TELEGRAM] Falha ao enviar oferta.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISPATCH][TELEGRAM] Erro ao enviar oferta: {ex.Message}");
            }
        }

        private async Task TryPublishFacebookAsync(OfferData offer, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _facebookService.PublishOfferAsync(offer, cancellationToken).ConfigureAwait(false);
                Console.WriteLine(success
                    ? "[DISPATCH][FACEBOOK] Oferta publicada com sucesso."
                    : "[DISPATCH][FACEBOOK] Falha ao publicar oferta.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISPATCH][FACEBOOK] Erro ao publicar oferta: {ex.Message}");
            }
        }
    }
}
