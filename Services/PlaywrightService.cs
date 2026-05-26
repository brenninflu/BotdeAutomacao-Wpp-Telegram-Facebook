using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace OfertaBot.Services
{
    public class PlaywrightService : IAsyncDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private readonly int _timeoutMs = 30000;

        public async Task InitializeAsync()
        {
            try
            {
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new()
                {
                    Headless = true
                });
                Console.WriteLine("[PLAYWRIGHT] Navegador inicializado com sucesso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PLAYWRIGHT] Erro ao inicializar: {ex.Message}");
                throw;
            }
        }

        public async Task<Models.Oferta?> GetProductDataAsync(string shopeeLink)
        {
            if (_browser == null)
            {
                throw new InvalidOperationException("Playwright não foi inicializado. Chame InitializeAsync primeiro.");
            }

            try
            {
                var context = await _browser.NewContextAsync();
                var page = await context.NewPageAsync();

                await page.GotoAsync(shopeeLink, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = _timeoutMs
                });

                // Aguardar elementos do produto
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                var oferta = await ExtractProductDataAsync(page);
                oferta.Link = shopeeLink;

                await context.CloseAsync();
                return oferta;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PLAYWRIGHT] Erro ao extrair dados: {ex.Message}");
                return null;
            }
        }

        private async Task<Models.Oferta> ExtractProductDataAsync(IPage page)
        {
            var oferta = new Models.Oferta();

            try
            {
                // Tentar extrair nome do produto
                oferta.Nome = await page.EvaluateAsync<string>(@"
                    () => {
                        const titleEl = document.querySelector('h1') || document.querySelector('[class*=title]');
                        return titleEl?.textContent?.trim() || 'Produto Shopee';
                    }
                ");

                // Tentar extrair preço
                var precoText = await page.EvaluateAsync<string>(@"
                    () => {
                        const priceEl = document.querySelector('[class*=price]');
                        return priceEl?.textContent?.trim() || '0';
                    }
                ");

                if (decimal.TryParse(precoText.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim(), out var preco))
                {
                    oferta.PrecoAtual = preco;
                }

                // Tentar extrair avaliação
                var avaliacaoText = await page.EvaluateAsync<string>(@"
                    () => {
                        const ratingEl = document.querySelector('[class*=rating]');
                        return ratingEl?.textContent?.trim() || '0';
                    }
                ");

                if (double.TryParse(avaliacaoText, out var avaliacao))
                {
                    oferta.Avaliacao = avaliacao;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PLAYWRIGHT] Erro ao extrair dados da página: {ex.Message}");
            }

            return oferta;
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser != null)
            {
                await _browser.CloseAsync();
            }

            _playwright?.Dispose();
        }
    }
}
