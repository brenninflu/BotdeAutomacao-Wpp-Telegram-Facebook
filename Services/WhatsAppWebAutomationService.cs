using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Playwright;

namespace OfertaBot.Services
{
    public class WhatsAppWebAutomationService : IAsyncDisposable
    {
        private IPlaywright? _playwright;
        private IBrowserContext? _context;
        private IPage? _page;
        private bool _isLoggedIn = false;
        private const string WhatsAppUrl = "https://web.whatsapp.com/";

        public bool IsLoggedIn => _isLoggedIn;

        // ─────────────────────────────────────────────
        // INICIALIZAÇÃO
        // ─────────────────────────────────────────────
        public async Task InitializeAsync()
        {
            try
            {
                Console.WriteLine("[WHATSAPP-WEB] 🚀 Iniciando automação...");

                _playwright = await Playwright.CreateAsync();

                if (!Directory.Exists(Config.WhatsAppWebSessionPath))
                    Directory.CreateDirectory(Config.WhatsAppWebSessionPath);

                _context = await _playwright.Chromium.LaunchPersistentContextAsync(
                    Config.WhatsAppWebSessionPath,
                    new BrowserTypeLaunchPersistentContextOptions
                    {
                        Headless = Config.UseHeadlessMode,
                        Args = new[]
                        {
                            "--no-sandbox",
                            "--disable-setuid-sandbox",
                            "--disable-dev-shm-usage",
                            "--disable-gpu"
                        },
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                                    "AppleWebKit/537.36 (KHTML, like Gecko) " +
                                    "Chrome/122.0.0.0 Safari/537.36"
                    });

                _page = await _context.NewPageAsync();
                _page.SetDefaultTimeout(60000);

                Console.WriteLine("[WHATSAPP-WEB] 🌐 Abrindo WhatsApp Web...");
                await _page.GotoAsync(WhatsAppUrl,
                    new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                await CheckLoginStatusAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WHATSAPP-WEB] ❌ Erro fatal na inicialização: {ex.Message}");
                throw;
            }
        }

        // ─────────────────────────────────────────────
        // VERIFICAÇÃO DE LOGIN / QR CODE
        // ─────────────────────────────────────────────
        private async Task CheckLoginStatusAsync()
        {
            if (_page == null) return;

            try
            {
                Console.WriteLine("[WHATSAPP-WEB] 🔍 Verificando estado da conexão...");
                await Task.Delay(10000); // aguarda carregamento inicial

                await CaptureQrCodeAsync(); // salva screenshot para o endpoint /qrcode

                var paneSide = await _page.QuerySelectorAsync("#pane-side");
                if (paneSide != null)
                {
                    Console.WriteLine("[WHATSAPP-WEB] ✅ WhatsApp Conectado!");
                    _isLoggedIn = true;

                    // Remove QR Code antigo – não é mais necessário
                    if (File.Exists(Config.WhatsAppWebQrCodePath))
                        File.Delete(Config.WhatsAppWebQrCodePath);

                    await JoinGroupIfNotMemberAsync();
                }
                else
                {
                    Console.WriteLine("[WHATSAPP-WEB] 📱 Aguardando leitura do QR Code...");
                    _isLoggedIn = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WHATSAPP-WEB] ⚠️ Erro ao verificar login: {ex.Message}");
            }
        }

        public async Task CaptureQrCodeAsync()
        {
            if (_page == null) return;
            try
            {
                var qrDir = Path.GetDirectoryName(Config.WhatsAppWebQrCodePath);
                if (!string.IsNullOrEmpty(qrDir) && !Directory.Exists(qrDir))
                    Directory.CreateDirectory(qrDir);

                await _page.ScreenshotAsync(new PageScreenshotOptions
                    { Path = Config.WhatsAppWebQrCodePath });

                Console.WriteLine("[WHATSAPP-WEB] 📸 Screenshot salvo. Acesse /qrcode para escanear.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WHATSAPP-WEB] ❌ Erro ao capturar screenshot: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────
        // ENTRAR NO GRUPO SE AINDA NÃO FOR MEMBRO
        // ─────────────────────────────────────────────
        public async Task JoinGroupIfNotMemberAsync()
        {
            if (_page == null || !_isLoggedIn) return;

            foreach (var groupName in Config.TargetGroups)
            {
                try
                {
                    Console.WriteLine($"[WHATSAPP-WEB] 🔍 Verificando grupo: {groupName}");

                    var chat = await _page.QuerySelectorAsync($"xpath=//span[@title='{groupName}']");

                    if (chat == null)
                    {
                        Console.WriteLine(
                            $"[WHATSAPP-WEB] 🚪 Grupo '{groupName}' não encontrado. Tentando link de convite...");

                        await _page.GotoAsync(Config.WhatsAppGroupInviteLink);

                        var joinBtn = await _page.WaitForSelectorAsync(
                            "#action-button",
                            new PageWaitForSelectorOptions { Timeout = 15000 });

                        if (joinBtn != null)
                        {
                            await joinBtn.ClickAsync();
                            await Task.Delay(3000);

                            var useWeb = await _page.QuerySelectorAsync(
                                "xpath=//a[contains(text(),'use WhatsApp Web')] | //span[contains(text(),'use WhatsApp Web')]");
                            if (useWeb != null) await useWeb.ClickAsync();

                            await Task.Delay(10000);

                            var finalJoin = await _page.QuerySelectorAsync(
                                "[data-testid='popup-controls-ok']");
                            if (finalJoin != null) await finalJoin.ClickAsync();

                            Console.WriteLine(
                                $"[WHATSAPP-WEB] ✅ Solicitação para entrar em '{groupName}' enviada.");
                        }

                        // Volta para a tela principal
                        await _page.GotoAsync(WhatsAppUrl);
                        await _page.WaitForSelectorAsync("#pane-side",
                            new PageWaitForSelectorOptions { Timeout = 30000 });
                    }
                    else
                    {
                        Console.WriteLine($"[WHATSAPP-WEB] ✅ Já sou membro de '{groupName}'.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[WHATSAPP-WEB] ❌ Erro ao processar grupo '{groupName}': {ex.Message}");
                }
            }
        }

        // ─────────────────────────────────────────────
        // ENVIO DE MENSAGEM  ← CORREÇÃO PRINCIPAL
        // ─────────────────────────────────────────────
        public async Task<bool> SendMessageAsync(string targetName, string message)
        {
            if (_page == null || !_isLoggedIn) return false;

            try
            {
                Console.WriteLine(
                    $"[WHATSAPP-WEB] 📤 Iniciando processo de envio para: '{targetName}'");

                await DebugScreenshotAsync("01_inicio_envio");

                // ── PASSO 1: Clicar direto no grupo na lista lateral ──────────────
                // ❌ ANTES: abria a barra de pesquisa e buscava o grupo de novo → travava
                // ✅ AGORA: localiza o span com title exato e clica nele diretamente

                var groupElement = await FindGroupInSidebarAsync(targetName);

                if (groupElement == null)
                {
                    // Fallback: tenta via barra de pesquisa só se não encontrar na lista
                    Console.WriteLine(
                        "[WHATSAPP-WEB] ⚠️ Grupo não visível na lista. Tentando via pesquisa...");

                    var found = await OpenChatViaSearchAsync(targetName);
                    if (!found)
                    {
                        Console.WriteLine(
                            $"[WHATSAPP-WEB] ❌ Não foi possível abrir o chat '{targetName}'.");
                        await DebugScreenshotAsync("ERRO_grupo_nao_encontrado");
                        return false;
                    }
                }
                else
                {
                    await groupElement.ScrollIntoViewIfNeededAsync();
                    await groupElement.ClickAsync();
                    Console.WriteLine($"[WHATSAPP-WEB] 🎯 Grupo '{targetName}' aberto.");
                }

                await Task.Delay(800);
                await DebugScreenshotAsync("02_chat_aberto");

                // ── PASSO 2: Localizar o campo de mensagem ────────────────────────
                IElementHandle? messageInput = await FindElementWithRetriesAsync(new[]
                {
                    "div[contenteditable='true'][data-tab='10']",
                    "div[contenteditable='true'][data-tab='1']",
                    "footer div[contenteditable='true']",
                    "div[role='textbox'][aria-label='Digite uma mensagem']",
                    "#main div.lexical-rich-text-input div[role='textbox']",
                    "#main div[contenteditable='true']"
                }, "Campo de Mensagem");

                if (messageInput == null)
                {
                    Console.WriteLine("[WHATSAPP-WEB] ❌ Campo de mensagem não encontrado.");
                    await DebugScreenshotAsync("ERRO_campo_mensagem");
                    return false;
                }

                // ── PASSO 3: Digitar e enviar ─────────────────────────────────────
                await messageInput.ClickAsync();
                await Task.Delay(800);

                // Limpa qualquer texto residual
                await _page.Keyboard.PressAsync("Control+A");
                await _page.Keyboard.PressAsync("Backspace");
                await Task.Delay(800);

                // FillAsync funciona bem para textos longos; usa clipboard internamente
                await messageInput.FillAsync(message ?? string.Empty);
                await Task.Delay(800);

                await DebugScreenshotAsync("03_mensagem_digitada");

                Console.WriteLine("[WHATSAPP-WEB] ⏳ Enviando mensagem...");
                await Task.Delay(500);

                await _page.Keyboard.PressAsync("Enter");

                // Fallback: Tenta clicar no botão de enviar caso o Enter falhe (seletores comuns do botão de envio)
                var sendButton = await _page.QuerySelectorAsync("span[data-icon='send'], button:has(span[data-icon='send'])");
                if (sendButton != null)
                {
                    await sendButton.ClickAsync();
                }

                Console.WriteLine("[WHATSAPP-WEB] 🚀 Comando de envio executado.");

                await Task.Delay(5000);
                await DebugScreenshotAsync("04_apos_envio");

                Console.WriteLine(
                    $"[WHATSAPP-WEB] ✅ Mensagem enviada com sucesso para '{targetName}'!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WHATSAPP-WEB] ❌ ERRO NO ENVIO: {ex.Message}");
                await DebugScreenshotAsync("ERRO_ENVIO_MENSAGEM");
                return false;
            }
        }

        // ─────────────────────────────────────────────
        // HELPERS PRIVADOS
        // ─────────────────────────────────────────────

        /// <summary>
        /// Localiza o grupo diretamente na lista lateral sem usar a barra de pesquisa.
        /// Tenta múltiplos seletores para ser resistente a mudanças do WhatsApp Web.
        /// </summary>
        private async Task<IElementHandle?> FindGroupInSidebarAsync(string groupName)
        {
            if (_page == null) return null;

            // Seletores em ordem de preferência
            var selectors = new[]
            {
                $"xpath=//span[@title='{groupName}']",
                $"xpath=//div[@title='{groupName}']",
                $"xpath=//span[text()='{groupName}']",
            };

            foreach (var selector in selectors)
            {
                try
                {
                    var el = await _page.WaitForSelectorAsync(selector,
                        new PageWaitForSelectorOptions
                        {
                            State = WaitForSelectorState.Visible,
                            Timeout = 5000
                        });
                    if (el != null)
                    {
                        Console.WriteLine(
                            $"[WHATSAPP-WEB] ✨ Grupo encontrado na barra lateral com: {selector}");
                        return el;
                    }
                }
                catch { /* tenta próximo seletor */ }
            }

            return null;
        }

        /// <summary>
        /// Fallback: abre o chat usando a barra de pesquisa.
        /// Só é chamado se o grupo não aparecer na lista lateral visível.
        /// </summary>
        private async Task<bool> OpenChatViaSearchAsync(string targetName)
        {
            if (_page == null) return false;

            try
            {
                // Localizar barra de pesquisa
                IElementHandle? searchInput = await FindElementWithRetriesAsync(new[]
                {
                    "div[contenteditable='true'][data-tab='3']",
                    "[data-testid='chat-list-search']",
                    "div[role='textbox'][aria-label='Caixa de texto de pesquisa']",
                    "div[role='textbox'][title='Pesquisar ou começar uma conversa nova']"
                }, "Barra de Pesquisa", retries: 2);

                if (searchInput == null) return false;

                await searchInput.ClickAsync();
                await Task.Delay(500);
                await _page.Keyboard.PressAsync("Control+A");
                await _page.Keyboard.PressAsync("Backspace");
                await Task.Delay(300);
                await _page.Keyboard.TypeAsync(targetName, new KeyboardTypeOptions { Delay = 80 });

                Console.WriteLine($"[WHATSAPP-WEB] 🔎 Buscando por: '{targetName}'");
                await Task.Delay(3000);
                await DebugScreenshotAsync("fallback_busca");

                // Clicar no resultado
                var chatItemSelector =
                    $"xpath=//span[@title='{targetName}'] | //div[@role='listitem']//span[contains(text(),'{targetName}')]";

                var chatItem = await _page.WaitForSelectorAsync(chatItemSelector,
                    new PageWaitForSelectorOptions
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 10000
                    });

                if (chatItem == null) return false;

                await chatItem.ClickAsync();
                Console.WriteLine($"[WHATSAPP-WEB] 🎯 Chat '{targetName}' selecionado via pesquisa.");
                await Task.Delay(2000);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WHATSAPP-WEB] ❌ Fallback de pesquisa falhou: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tenta localizar um elemento com múltiplos seletores e N tentativas.
        /// </summary>
        private async Task<IElementHandle?> FindElementWithRetriesAsync(
            string[] selectors, string elementName, int retries = 3)
        {
            if (_page == null) return null;

            for (int i = 1; i <= retries; i++)
            {
                Console.WriteLine(
                    $"[WHATSAPP-WEB] 🔍 Localizando {elementName} (Tentativa {i}/{retries})...");

                foreach (var selector in selectors)
                {
                    try
                    {
                        var element = await _page.WaitForSelectorAsync(selector,
                            new PageWaitForSelectorOptions
                            {
                                State = WaitForSelectorState.Visible,
                                Timeout = 4000
                            });

                        if (element != null)
                        {
                            Console.WriteLine(
                                $"[WHATSAPP-WEB] ✨ {elementName} encontrado: {selector}");
                            return element;
                        }
                    }
                    catch { /* próximo seletor */ }
                }

                await Task.Delay(2000);
            }

            Console.WriteLine($"[WHATSAPP-WEB] ❌ {elementName} não encontrado após {retries} tentativas.");
            return null;
        }

        private async Task DebugScreenshotAsync(string stepName)
        {
            if (_page == null) return;
            try
            {
                string fileName = $"debug_{DateTime.Now:HHmmss}_{stepName}.png";
                string fullPath = Path.Combine(
                    Path.GetDirectoryName(Config.WhatsAppWebQrCodePath) ?? "", fileName);
                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = fullPath });
                Console.WriteLine($"[DEBUG] 📸 Screenshot: {fileName}");
            }
            catch { }
        }

        // ─────────────────────────────────────────────
        // UTILITÁRIOS PÚBLICOS
        // ─────────────────────────────────────────────
        public async Task<List<string>> ListGroupsAsync()
        {
            if (_page == null || !_isLoggedIn) return new List<string>();

            try
            {
                var groups = await _page.QuerySelectorAllAsync("span[dir='auto'][title]");
                var names = new List<string>();
                foreach (var g in groups)
                {
                    var title = await g.GetAttributeAsync("title");
                    if (!string.IsNullOrEmpty(title)) names.Add(title);
                }
                return names.Distinct().ToList();
            }
            catch { return new List<string>(); }
        }

        // ─────────────────────────────────────────────
        // DISPOSE
        // ─────────────────────────────────────────────
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_page != null) await _page.CloseAsync();
                if (_context != null) await _context.CloseAsync();
                _playwright?.Dispose();
                Console.WriteLine("[WHATSAPP-WEB] 🛑 Automação encerrada.");
            }
            catch { }
        }
    }
}
