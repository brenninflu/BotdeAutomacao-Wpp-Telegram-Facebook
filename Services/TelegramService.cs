using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using OfertaBot.Models;

namespace OfertaBot.Services
{
    public class TelegramService
    {
        private readonly TelegramBotClient _botClient;
        private readonly string _channelId;
        private readonly MensagemService _mensagemService;

        public TelegramService(string token, string channelId, MensagemService mensagemService)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Telegram bot token is missing.", nameof(token));

            if (string.IsNullOrWhiteSpace(channelId))
                throw new ArgumentException("Telegram channel ID is missing.", nameof(channelId));

            _botClient = new TelegramBotClient(token);
            _channelId = channelId.Trim();
            _mensagemService = mensagemService ?? throw new ArgumentNullException(nameof(mensagemService));
        }

        public async Task GetChatIdsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("[TELEGRAM] Consultando updates...");
                var updates = await _botClient.GetUpdatesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                if (updates == null || updates.Length == 0)
                {
                    Console.WriteLine("[TELEGRAM] Nenhum update encontrado.");
                    return;
                }

                var found = false;
                foreach (var update in updates)
                {
                    var chat = update.Message?.Chat ?? update.ChannelPost?.Chat;
                    if (chat == null) continue;

                    found = true;
                    Console.WriteLine("[TELEGRAM] Chat encontrado");
                    Console.WriteLine($"ID: {chat.Id}");
                    Console.WriteLine($"Type: {chat.Type}");
                    Console.WriteLine($"Title: {chat.Title ?? chat.Username ?? "(sem título)"}");
                    Console.WriteLine();
                }

                if (!found)
                {
                    Console.WriteLine("[TELEGRAM] Nenhuma informação de chat encontrada.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TELEGRAM] Falha ao obter chat IDs: {ex.Message}");
            }
        }

        public Task EnviarMensagem(string mensagem)
        {
            return SendTelegramMessageAsync(mensagem);
        }

        public Task EnviarVideo(string videoPath, string? caption = null)
        {
            return SendTelegramVideoAsync(videoPath, caption);
        }

        public async Task<Message?> SendTelegramMessageAsync(string mensagem, ParseMode parseMode = ParseMode.Html, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(mensagem))
            {
                Console.WriteLine("[TELEGRAM] Mensagem vazia ignorada.");
                return null;
            }

            return await ExecuteWithRetryAsync(async () =>
            {
                Console.WriteLine("[TELEGRAM] Enviando mensagem...");
                var message = await _botClient.SendTextMessageAsync(
                    chatId: GetChatId(),
                    text: mensagem,
                    parseMode: parseMode,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                Console.WriteLine($"[TELEGRAM] Mensagem enviada com sucesso. MessageId={message.MessageId}");
                return message;
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Message?> SendTelegramPhotoAsync(string imageUrlOrPath, string? caption = null, ParseMode parseMode = ParseMode.Html, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(imageUrlOrPath))
            {
                Console.WriteLine("[TELEGRAM] Imagem vazia ignorada.");
                return null;
            }

            return await ExecuteWithRetryAsync(async () =>
            {
                Console.WriteLine("[TELEGRAM] Enviando imagem...");

                if (Uri.TryCreate(imageUrlOrPath, UriKind.Absolute, out var imageUri))
                {
                    var photo = InputFile.FromUri(imageUri);
                    var message = await _botClient.SendPhotoAsync(
                        chatId: GetChatId(),
                        photo: photo,
                        caption: caption,
                        parseMode: parseMode,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    Console.WriteLine($"[TELEGRAM] Imagem enviada com sucesso. MessageId={message.MessageId}");
                    return message;
                }

                await using var stream = System.IO.File.OpenRead(imageUrlOrPath);
                var fileName = Path.GetFileName(imageUrlOrPath);
                var inputFile = InputFile.FromStream(stream, fileName);

                var localMessage = await _botClient.SendPhotoAsync(
                    chatId: GetChatId(),
                    photo: inputFile,
                    caption: caption,
                    parseMode: parseMode,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                Console.WriteLine($"[TELEGRAM] Imagem local enviada com sucesso. MessageId={localMessage.MessageId}");
                return localMessage;
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Message?> SendTelegramVideoAsync(string videoPath, string? caption = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(videoPath) || !System.IO.File.Exists(videoPath))
            {
                Console.WriteLine("[TELEGRAM] Video inválido ou não encontrado.");
                return null;
            }

            return await ExecuteWithRetryAsync(async () =>
            {
                Console.WriteLine("[TELEGRAM] Enviando vídeo...");
                await using var stream = System.IO.File.OpenRead(videoPath);
                var msg = await _botClient.SendVideoAsync(
                    chatId: GetChatId(),
                    video: InputFile.FromStream(stream, Path.GetFileName(videoPath)),
                    caption: caption,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                Console.WriteLine($"[TELEGRAM] Vídeo enviado com sucesso. MessageId={msg.MessageId}");
                return msg;
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> SendOfferAsync(OfferData offer, CancellationToken cancellationToken = default)
        {
            if (offer == null) throw new ArgumentNullException(nameof(offer));

            try
            {
                var message = _mensagemService.GerarMensagem(offer);
                Console.WriteLine("[TELEGRAM] Enviando oferta em uma única mensagem...");
                await SendTelegramMessageAsync(message, ParseMode.Html, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TELEGRAM] Falha ao enviar oferta: {ex.Message}");
                return false;
            }
        }

        private long GetChatId()
        {
            if (!long.TryParse(_channelId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var chatId))
            {
                throw new InvalidOperationException("TelegramChannelId deve ser numérico (ex: -1001234567890).");
            }

            return chatId;
        }

        private async Task<T?> ExecuteWithRetryAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
        {
            Exception? lastError = null;

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    return await action().ConfigureAwait(false);
                }
                catch (ApiRequestException ex)
                {
                    lastError = ex;
                    Console.WriteLine($"[TELEGRAM] API error na tentativa {attempt}/3: {ex.Message} (Code: {ex.ErrorCode})");
                    if (ex.ErrorCode == 401) break;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[TELEGRAM] Operação cancelada.");
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Console.WriteLine($"[TELEGRAM] Erro na tentativa {attempt}/3: {ex.Message}");
                }

                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken).ConfigureAwait(false);
                }
            }

            if (lastError != null)
            {
                Console.WriteLine($"[TELEGRAM] Falha definitiva: {lastError.Message}");
            }

            return default;
        }

        private static string BuildCaption(OfferData offer)
        {
            var price = offer.Price.ToString("F2", new CultureInfo("pt-BR"));
            return $"🔥 {offer.ProductName}\n💰 R$ {price}";
        }
    }
}
