using System;
using System.Threading.Tasks;
using OfertaBot.Models;

namespace OfertaBot.Services
{
    /// <summary>
    /// Serviço que orquestra todo o fluxo de processamento de links
    /// 1. Detecta link Shopee
    /// 2. Valida link
    /// 3. Extrai dados do produto (Playwright)
    /// 4. Converte para link afiliado
    /// 5. Retorna análise completa
    /// </summary>
    public class LinkProcessingService
    {
        private readonly LinkDetectionService _linkDetection;
        private readonly PlaywrightService _playwright;
        private readonly ShopeeAffiliateService _affiliateService;
        private readonly DailyLinkService _dailyLinkService;

        public LinkProcessingService(
            LinkDetectionService linkDetection,
            PlaywrightService playwright,
            ShopeeAffiliateService affiliateService,
            DailyLinkService dailyLinkService)
        {
            _linkDetection = linkDetection ?? throw new ArgumentNullException(nameof(linkDetection));
            _playwright = playwright ?? throw new ArgumentNullException(nameof(playwright));
            _affiliateService = affiliateService ?? throw new ArgumentNullException(nameof(affiliateService));
            _dailyLinkService = dailyLinkService ?? throw new ArgumentNullException(nameof(dailyLinkService));
        }

        /// <summary>
        /// Processa uma mensagem e retorna a análise do link encontrado
        /// </summary>
        public async Task<LinkAnalysis> ProcessMessageAsync(string messageText)
        {
            var analysis = new LinkAnalysis();

            try
            {
                Console.WriteLine("[PROCESSING] Iniciando processamento de mensagem");

                // Etapa 1: Detectar link
                var shopeeLink = _linkDetection.GetValidShopeeLink(messageText);
                if (shopeeLink == null)
                {
                    analysis.IsValid = false;
                    analysis.ErrorMessage = "Nenhum link Shopee detectado na mensagem";
                    Console.WriteLine("[PROCESSING] " + analysis.ErrorMessage);
                    return analysis;
                }

                analysis.OriginalLink = shopeeLink;
                Console.WriteLine($"[PROCESSING] Link Shopee detectado: {shopeeLink}");
                _dailyLinkService.RecordLink(shopeeLink, messageText);

                // Etapa 2: Converter para link afiliado
                try
                {
                    analysis.AffiliateLink = _affiliateService.TransformToAffiliateLink(shopeeLink);
                    Console.WriteLine("[PROCESSING] Link afiliado gerado com sucesso");
                }
                catch (Exception ex)
                {
                    analysis.IsValid = false;
                    analysis.ErrorMessage = $"Erro ao converter para link afiliado: {ex.Message}";
                    Console.WriteLine("[PROCESSING] " + analysis.ErrorMessage);
                    return analysis;
                }

                // Etapa 3: Extrair dados do produto (opcional)
                try
                {
                    Console.WriteLine("[PROCESSING] Extraindo dados do produto com Playwright...");
                    analysis.ProductData = await _playwright.GetProductDataAsync(shopeeLink);
                    Console.WriteLine("[PROCESSING] Dados do produto extraídos com sucesso");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PROCESSING] Aviso: Não foi possível extrair dados do produto: {ex.Message}");
                    // Não falha o processamento se não conseguir extrair dados
                }

                analysis.IsValid = true;
                Console.WriteLine("[PROCESSING] Análise completa com sucesso");
                return analysis;
            }
            catch (Exception ex)
            {
                analysis.IsValid = false;
                analysis.ErrorMessage = $"Erro inesperado no processamento: {ex.Message}";
                Console.WriteLine("[PROCESSING] " + analysis.ErrorMessage);
                return analysis;
            }
        }

        /// <summary>
        /// Processa múltiplas mensagens em paralelo
        /// </summary>
        public async Task<List<LinkAnalysis>> ProcessMultipleMessagesAsync(List<string> messages)
        {
            var tasks = messages.Select(msg => ProcessMessageAsync(msg)).ToList();
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }
    }
}
