using System;
using System.Collections.Generic;

namespace OfertaBot
{
    public static class Config
    {
        // ============================================
        // Bot Identity
        // ============================================
        public static string WhatsAppNumber = "+5561996333708"; // Número real conectado via QR Code
        public static string BotVersion = "3.0.0 (Manual Refactor)";

        // ============================================
        // Server Configuration
        // ==============================            ==============
        public static string ServerUrl = "https://garland-employer-racing.ngrok-free.dev"; 
        public static int ServerPort = 5000;

        // ============================================
        // WhatsApp Web Automation (Playwrig"ht)
        // ============================================
        public static bool EnableWhatsAppWebAutomation = true;
        public static bool UseHeadlessMode = true; // Altere para false para ver o navegador funcionando
        public static string WhatsAppWebSessionPath = "C:\\Users\\Brenno Xavier\\Documents\\OfertaBot\\whatsapp_session";
        public static string WhatsAppWebQrCodePath = "C:\\Users\\Brenno Xavier\\Documents\\OfertaBot\\wwwroot\\qrcode.png";
        
        // Grupos para enviar ofertas (nomes reais ou IDs se conhecidos)
        public static List<string> TargetGroups = new() 
        { 
            "Baratao Promoções #01" 
        };

        // Link de convite para o grupo principal            
        public static string WhatsAppGroupInviteLink = "https://chat.whatsapp.com/Hf5C11G5N84I4l6erPX31L";

        // ============================================
        // Automation Settings (Loop de Ofertas)
        // ============================================
        public static bool AutoPostEnabled = true;
        public static int OfferIntervalMinutes = 30; // Intervalo de 30 minutos entre ofertas
        public static bool RandomizeOffers = true;    // Enviar ofertas em ordem aleatória

        // ============================================
        // Manual Offers List (Links já convertidos)
        // ============================================
        public static List<string> ManualOffers = new()
        {            
           "https://amzn.to/4nErEC0",
           "https://amzn.to/4tJbk4l",
           "https://amzn.to/49ealSt",
           "https://amzn.to/4tUow6t",
           "https://amzn.to/4nGk7me",
           "https://amzn.to/4nGk7me",
           "https://amzn.to/4nLRfJB",
           "https://amzn.to/42PrSgg",
           "https://amzn.to/3RVYEtC",
           "https://amzn.to/4uYvSXG",
           "https://amzn.to/3PW7HKz",
           "https://amzn.to/49c8qho",
           "https://amzn.to/3RVZoyU",
           "https://amzn.to/3PfsatK",
           "https://amzn.to/42QcWOR",
           "https://amzn.to/4v3WyX9",
           "https://amzn.to/4uqWYa7",
           "https://amzn.to/4uwcyRO",
           "https://amzn.to/43lF10D",
           "https://amzn.to/4v1Y5gj",
           "https://amzn.to/4dz6Qsx",
           "https://amzn.to/4fHap0S",
           "https://amzn.to/4f1e5KT",
           "https://meli.la/1mr356i",
           "https://meli.la/1a5GePJ",
           "https://meli.la/2t8TQw7",
           "https://meli.la/2gP1TVP",
           "https://meli.la/2Aqj9Qt",
           "https://meli.la/1jsWCoF",
           "https://meli.la/2mSiZBW",
           "https://meli.la/2aY9nLD",
           "https://meli.la/2kmhdcv",
           "https://meli.la/1ngCbE7",
           "https://meli.la/2s7RGoT",
           "https://meli.la/2KDxLHd",
           "https://meli.la/1mWiErE",
           "https://meli.la/2wJWmv5",
           "https://meli.la/2KCZ8Bk",
           "https://meli.la/1uPaDjP",
           "https://meli.la/1Q2aA6z",
           "https://meli.la/1eTQUf6",
           "https://meli.la/2hG2ZRh",
           "https://meli.la/2cj9o1w",
           "https://meli.la/1642Ped",
           "https://meli.la/32dEics",
           "https://meli.la/2KDxLHd",
           "https://meli.la/1oB4GMf",
           "https://meli.la/2Xd839U",
           "https://meli.la/2xZDhSy",
           "https://meli.la/2p3TEge",
           "https://meli.la/1eTQUf6",
           "https://amzn.to/3PGhJiN"

           
            
        };

        // ============================================
        // Affiliate Cookie + Zapier + Sales Cookie
        // ============================================
        // Estes campos são mantidos conforme pedido (Webhook de Comissão)
        public static string AffiliateCookieName = "affiliate_id";
        public static int AffiliateCookieDurationDays = 30;
        public static string ShopeeAffiliateId = "18            385910279";
        public static string ZapierLeadWebhookUrl = "https://hooks.zapier.com/hooks/catch/27577731/4yg4gka/"; 
        public static string ZapierCommissionWebhookUrl = "https://garland-employer-racing.ngrok-free.dev/api/affiliate/commission";

        // ============================================
        // Legacy Support & Build Compatibility
        // ============================================
        public static bool UseTelegram = true;
        public static string TelegramBotToken = "8665816897:AAEFsL0HNM-08aPvxgmqbZIv6Eydzq6Xf_w";
        public static string TelegramChannelId = "-1003805797550";

        // ============================================
        // Facebook Graph API
        // ============================================
        public static bool UseFacebook = true;
        public static string FacebookPageId = "EAATU6VJzcUkBRsY3SsfEooDo2t9Dz7GB0uiVfF0epseI4iNRgxMjmap3BdhJpZAS2RnTRWetGn56Dm1Q9nc5drwyeRiGwSYoI93MfABu2RigjJqJCkFx9gcCMwjURN9Ckg09E3QH2Ytj7wtgGWAcGE9CrTpj75LbL6rCEZBFDNwVSCqEBSeZAJGcuqVpXTKT2rxaSqN3C1BfYZB5O8ZAErHEc9ZBDjuoSC52L8yeZBuqJZB2gLkIdrxs0daOUZAp3OgexyzWNRWreRBZC2NtR3d3HEYOaw0y7u4UTz1gqwJEJG0ZAatXGAhnqZA3XJSgPPSOTw4vEGFvoQXSCDVV";
        public static string FacebookAccessToken = "27807544408847872";

        public static string FfmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe";
        public static string VideosFolder = "wwwroot/videos";
        public static string BackgroundMusicPath = "wwwroot/assets/music.mp3";
        public static int DailyLinkQueueSize = 100;
        public static List<List<string>> ProductLists = new() { new List<string>() };
    }
}
