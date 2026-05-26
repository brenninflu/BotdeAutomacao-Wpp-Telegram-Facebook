using OfertaBot;
using OfertaBot.Data;
using OfertaBot.Services;
using System.Text;

// Garantir que o console aceite caracteres especiais (Emojis/UTF8)
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// ====================================================
// Configuração de Serviços
// ====================================================

// Adicionar controladores
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

        // Registrar serviços de Negócio (Mantidos para Webhook/Tracking)
        builder.Services.AddSingleton<LinkDetectionService>();
        builder.Services.AddSingleton<ShopeeAffiliateService>();
        builder.Services.AddSingleton<PlaywrightService>();
        builder.Services.AddScoped<LinkProcessingService>();
        builder.Services.AddHttpClient<AffiliateLeadService>();
        builder.Services.AddDbContext<OfertaBotDbContext>();
        builder.Services.AddHttpClient();

        // Serviços de oferta multiplataforma
        builder.Services.AddSingleton<MensagemService>();
        builder.Services.AddSingleton<AmazonOfferService>();
        builder.Services.AddSingleton<MercadoLivreOfferService>();
        builder.Services.AddSingleton<IOfferScrapingService>(sp => sp.GetRequiredService<AmazonOfferService>());
        builder.Services.AddSingleton<IOfferScrapingService>(sp => sp.GetRequiredService<MercadoLivreOfferService>());
        builder.Services.AddSingleton<TelegramService>(sp => new TelegramService(
            Config.TelegramBotToken,
            Config.TelegramChannelId,
            sp.GetRequiredService<MensagemService>()));
        builder.Services.AddSingleton<FacebookService>();
        builder.Services.AddSingleton<OfferDispatcherService>();

        // Registrar Automação WhatsApp Web (Nova Principal)
        builder.Services.AddSingleton<WhatsAppWebAutomationService>();

        // Hosted Services
        builder.Services.AddHostedService<WhatsAppWebAutomationHostedService>();

// CORS para permitir requisições locais
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ====================================================
// Configuração do Pipeline HTTP
// ====================================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

// Endpoint simples para exibir o QR Code no navegador
app.MapGet("/qrcode", async (HttpContext ctx) => 
{ 
    var path = Config.WhatsAppWebQrCodePath; 
    if (!File.Exists(path)) 
    { 
        ctx.Response.StatusCode = 404; 
        await ctx.Response.WriteAsync("QR Code ainda não gerado ou o bot já está logado. Aguarde ou verifique os logs."); 
        return; 
    } 
    ctx.Response.ContentType = "image/png"; 
    await ctx.Response.SendFileAsync(path); 
});

app.UseRouting();
app.UseCors("AllowAll");

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Status endpoint
app.MapGet("/api/status", () => Results.Ok(new
{
    status = "running",
    version = Config.BotVersion,
    timestamp = DateTime.UtcNow,
    features = new[] { "WhatsApp Web Automation", "Affiliate Tracking", "Zapier Integration", "Sales Cookie", "Telegram", "Facebook", "Amazon", "Mercado Livre" }
}));

// ====================================================
// Inicialização
// ====================================================

Console.WriteLine("════════════════════════════════════════════════════");
Console.WriteLine($"           🤖 OfertaBot - Versão {Config.BotVersion}");
Console.WriteLine("════════════════════════════════════════════════════");
Console.WriteLine($"[STARTUP] Servidor ativo em {Config.ServerUrl}");
Console.WriteLine("[STARTUP] Modo: WhatsApp Real (QR Code) + Manual Offers");
Console.WriteLine("════════════════════════════════════════════════════");

await app.RunAsync();
