using System.Text.Json.Serialization;

namespace OfertaBot.Models
{
    public class WhatsAppWebhookRequest
    {
        [JsonPropertyName("entry")]
        public List<WhatsAppEntry> Entry { get; set; } = new();
    }

    public class WhatsAppEntry
    {
        [JsonPropertyName("changes")]
        public List<WhatsAppChange> Changes { get; set; } = new();
    }

    public class WhatsAppChange
    {
        [JsonPropertyName("value")]
        public WhatsAppValue Value { get; set; } = new();
    }

    public class WhatsAppValue
    {
        [JsonPropertyName("messages")]
        public List<WhatsAppIncomingMessage> Messages { get; set; } = new();

        [JsonPropertyName("contacts")]
        public List<WhatsAppContact> Contacts { get; set; } = new();
    }

    public class WhatsAppIncomingMessage
    {
        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public WhatsAppTextContent Text { get; set; } = new();

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class WhatsAppTextContent
    {
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;
    }

    public class WhatsAppContact
    {
        [JsonPropertyName("profile")]
        public WhatsAppProfile Profile { get; set; } = new();

        [JsonPropertyName("wa_id")]
        public string WaId { get; set; } = string.Empty;
    }

    public class WhatsAppProfile
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class WhatsAppOutgoingMessage
    {
        [JsonPropertyName("messaging_product")]
        public string MessagingProduct { get; } = "whatsapp";

        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; } = "text";

        [JsonPropertyName("text")]
        public WhatsAppOutgoingText Text { get; set; } = new();
    }

    public class WhatsAppOutgoingText
    {
        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;
    }
}
