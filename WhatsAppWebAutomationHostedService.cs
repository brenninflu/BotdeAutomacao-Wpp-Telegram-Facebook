using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OfertaBot.Services;

namespace OfertaBot
{
    public class WhatsAppWebAutomationHostedService : IHostedService
    {
        private readonly WhatsAppWebAutomationService _automationService;
        private readonly OfferDispatcherService _offerDispatcherService;
        private CancellationTokenSource? _cts;
        private Task? _executingTask;
        private List<string> _shuffledQueue = new();
        private readonly object _queueLock = new();

        public WhatsAppWebAutomationHostedService(
            WhatsAppWebAutomationService automationService,
            OfferDispatcherService offerDispatcherService)
        {
            _automationService = automationService;
            _offerDispatcherService = offerDispatcherService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("[HOSTED-SERVICE] 🚀 Iniciando serviço de loop de ofertas...");
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _executingTask = ExecuteAsync(_cts.Token);
            return Task.CompletedTask;
        }

        private async Task ExecuteAsync(CancellationToken ct)
        {
            await _automationService.InitializeAsync();

            var isFirstRun = true;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (_automationService.IsLoggedIn)
                    {
                        if (isFirstRun)
                        {
                            Console.WriteLine("[HOSTED-SERVICE] 🚀 Primeira execução detectada! Enviando oferta imediata...");
                            isFirstRun = false;
                        }
                        else
                        {
                            Console.WriteLine("[HOSTED-SERVICE] 📢 Preparando envio de oferta agendada...");
                        }

                        var offer = GetNextOffer();
                        Console.WriteLine($"[HOSTED-SERVICE] 🔗 Oferta selecionada: {offer}");

                        await _offerDispatcherService.DispatchAsync(offer, Config.TargetGroups, ct).ConfigureAwait(false);

                        var nextRun = DateTime.Now.AddMinutes(Config.OfferIntervalMinutes);
                        Console.WriteLine($"[LOG] ⏳ PRÓXIMA EXECUÇÃO: {nextRun:HH:mm:ss} (em {Config.OfferIntervalMinutes} min)");

                        await Task.Delay(TimeSpan.FromMinutes(Config.OfferIntervalMinutes), ct).ConfigureAwait(false);
                    }
                    else
                    {
                        await _automationService.CaptureQrCodeAsync().ConfigureAwait(false);
                        await Task.Delay(15000, ct).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HOSTED-SERVICE] ⚠️ Erro no loop: {ex.Message}");
                    await Task.Delay(30000, ct).ConfigureAwait(false);
                }
            }
        }

        private string GetNextOffer()
        {
            if (Config.ManualOffers.Count == 0)
                return "⚠️ Nenhuma oferta cadastrada no Config.cs";

            lock (_queueLock)
            {
                // Se a fila estiver vazia, recria e embaralha (Ciclo completo)
                if (!_shuffledQueue.Any())
                {
                    Console.WriteLine("[HOSTED-SERVICE] 🔀 Iniciando novo ciclo de ofertas. Embaralhando lista...");
                    _shuffledQueue = Config.ManualOffers
                        .Where(url => !string.IsNullOrWhiteSpace(url))
                        .OrderBy(x => Guid.NewGuid()) // Embaralhamento randômico robusto
                        .ToList();
                }

                var nextUrl = _shuffledQueue[0];
                _shuffledQueue.RemoveAt(0); // Remove para não repetir neste ciclo
                
                Console.WriteLine($"[HOSTED-SERVICE] 🎲 Link sorteado ({Config.ManualOffers.Count - _shuffledQueue.Count}/{Config.ManualOffers.Count}): {nextUrl}");
                return nextUrl;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("[HOSTED-SERVICE] 🛑 Parando serviço de ofertas...");
            if (_cts != null) _cts.Cancel();
            if (_executingTask != null) await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);
        }
    }
}
