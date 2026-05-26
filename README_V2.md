# 🤖 OfertaBot v2.0 - Remodelação Completa

## 🔄 Novo Fluxo de Processamento

```
┌─────────────────────────────────────────────────────────────┐
│  1️⃣  BOT WHATSAPP (Webhook Listener)                        │
│      └─ Recebe mensagens do WhatsApp Cloud API             │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│  2️⃣  DETECTA LINK SHOPEE (LinkDetectionService)            │
│      └─ Extrai links da mensagem                            │
│      └─ Valida se é Shopee                                 │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│  3️⃣  SERVIDOR/API (ASP.NET Core)                            │
│      └─ Orquestra todo o processamento                      │
│      └─ Gerencia requisições paralelas                      │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│  4️⃣  PLAYWRIGHT (PlaywrightService)                         │
│      └─ Acessa a página do produto                          │
│      └─ Extrai dados (nome, preço, avaliação)              │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│  5️⃣  SHOPEE AFFILIATES (ShopeeAffiliateService)             │
│      └─ Converte link em link afiliado                      │
│      └─ Adiciona affiliate_id                              │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│  6️⃣  RECEBE LINK AFILIADO (LinkProcessingService)           │
│      └─ Retorna análise completa                            │
│      └─ Inclui dados do produto                            │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│  7️⃣  SUBSTITUI E FORMATA (WebhookController)                │
│      └─ Gera mensagem com link afiliado                     │
│      └─ Inclui dados do produto na resposta                │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│  8️⃣  ENVIA NO GRUPO (WhatsAppService)                       │
│      └─ Envia mensagem formatada ao usuário                 │
└─────────────────────────────────────────────────────────────┘
```

## 📁 Nova Estrutura do Projeto

```
OfertaBot/
├── Program.cs                           # Configuração ASP.NET Core
├── Config.cs                            # Configurações
├── OfertaBot.csproj                    # Dependências (Playwright, AspNetCore, etc)
│
├── Models/
│   ├── Oferta.cs                       # Modelo de produto
│   ├── WhatsAppMessage.cs              # Modelos WhatsApp (request/response)
│   └── LinkAnalysis.cs                 # Análise de link
│
├── Services/
│   ├── LinkDetectionService.cs         # Detecta links Shopee (novo)
│   ├── PlaywrightService.cs            # Scraping com Playwright (novo)
│   ├── ShopeeAffiliateService.cs       # Converte para link afiliado (refatorado)
│   ├── LinkProcessingService.cs        # Orquestra fluxo (novo)
│   ├── WhatsAppService.cs              # Envia mensagens (refatorado)
│   ├── MensagemService.cs              # (legado)
│   ├── OfertaService.cs                # (legado)
│   ├── SentOffersService.cs            # (legado)
│   ├── TelegramService.cs              # (legado)
│   ├── FiltroService.cs                # (legado)
│   └── VideoGenerationService.cs       # (legado)
│
├── Controllers/
│   └── WebhookController.cs            # Webhook WhatsApp (novo)
│
├── Assets/                             # Recursos estáticos
└── videos/                             # Vídeos gerados
```

## 🚀 Como Executar

### 1. Instalar Dependências
```bash
cd OfertaBot
dotnet restore
```

### 2. Instalar Playwright Browsers
```bash
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

### 3. Atualizar Configurações (Config.cs)
- `WhatsAppAccessToken`: Token da Meta/Facebook
- `WhatsAppPhoneNumberId`: ID do número WhatsApp
- `WhatsAppVerifyToken`: Token secreto para verificação de webhook
- `ShopeeAffiliateId`: Seu ID de afiliado Shopee

### 4. Executar o Servidor
```bash
dotnet run
```

O servidor iniciará em `http://localhost:5000`

### 5. Configurar Webhook no Meta Business Platform
1. Vá para [Meta App Dashboard](https://developers.facebook.com/apps/)
2. Selecione seu app
3. Em **Products**, adicione **Whatsapp**
4. Em **Configuration**, defina:
   - **Webhook URL**: `https://seu-dominio.com/api/webhook/whatsapp`
   - **Verify Token**: (o mesmo do Config.cs)
5. Em **Webhook fields**, selecione `messages` e `message_status`

## 🔌 Endpoints da API

### Health Check
```
GET http://localhost:5000/health
```

### Status do Bot
```
GET http://localhost:5000/api/status
```

### Webhook WhatsApp (Verificação)
```
GET http://localhost:5000/api/webhook/whatsapp
  ?hub.mode=subscribe
  &hub.challenge=CHALLENGE_TOKEN
  &hub.verify_token=YOUR_VERIFY_TOKEN
```

### Webhook WhatsApp (Recepcionar Mensagens)
```
POST http://localhost:5000/api/webhook/whatsapp
Content-Type: application/json

{
  "entry": [{
    "changes": [{
      "value": {
        "messages": [{
          "from": "5561995017468",
          "type": "text",
          "text": {"body": "https://shopee.com.br/..."}
        }],
        "contacts": [{
          "profile": {"name": "João"},
          "wa_id": "5561995017468"
        }]
      }
    }]
  }]
}
```

## 📦 Dependências Principais

| Pacote | Versão | Uso |
|--------|--------|-----|
| `Microsoft.Playwright` | 1.40.0 | Web scraping |
| `Microsoft.AspNetCore.*` | 10.0.0 | Framework Web API |
| `Newtonsoft.Json` | 13.0.3 | Serialização JSON |
| `Telegram.Bot` | 19.0.0 | (legado) |

## 🔑 Variáveis de Ambiente Recomendadas

```bash
WHATSAPP_ACCESS_TOKEN=seu_token
WHATSAPP_PHONE_NUMBER_ID=seu_numero
WHATSAPP_VERIFY_TOKEN=seu_token_secreto
SHOPEE_AFFILIATE_ID=seu_id
SERVER_PORT=5000
```

## 📋 Fluxo Detalhado

1. **Usuário envia mensagem com link Shopee** no WhatsApp
   ```
   Oi, achei este produto: https://s.shopee.com.br/60NIWu4A6C
   ```

2. **WhatsApp Cloud API envia webhook** para `POST /api/webhook/whatsapp`

3. **WebhookController recebe e valida** a mensagem

4. **LinkDetectionService extrai** o link Shopee

5. **LinkProcessingService orquestra**:
   - Valida link
   - **PlaywrightService acessa** a página e extrai dados
   - **ShopeeAffiliateService converte** para link afiliado
   - Retorna LinkAnalysis com tudo

6. **WebhookController gera resposta** formatada:
   ```
   🎉 Ótimo! Link processado!
   
   📦 Produto: [Nome]
   💰 Preço: R$ [Preço]
   ⭐ Avaliação: [Nota]/5
   
   🔗 Link afiliado:
   https://shopee.com.br/...?affiliate_id=18385910279
   ```

7. **WhatsAppService envia** mensagem de volta ao usuário

## 🔍 Logs e Debugging

Todos os eventos são registrados com prefixo:
- `[WEBHOOK]` - Eventos do webhook
- `[PROCESSING]` - Processamento de links
- `[LINK_DETECTION]` - Detecção de links
- `[PLAYWRIGHT]` - Operações Playwright
- `[AFFILIATE]` - Conversão de links afiliados
- `[WHATSAPP]` - Envio de mensagens

## ⚙️ Configurações Avançadas

### Timeout do Playwright
```csharp
// Config.cs
public static int PlaywrightTimeoutMs = 30000; // 30 segundos
```

### Modo Headless/Headed
```csharp
// Config.cs
public static bool PlaywrightHeadless = true; // true = sem interface gráfica
```

## 🚨 Troubleshooting

### Playwright não inicializa
```bash
# Instalar browsers
pwsh bin/Debug/net10.0/playwright.ps1 install
```

### Webhook não recebe mensagens
1. Verifique se o webhook URL é acessível publicamente
2. Verifique se o verify_token está correto
3. Verifique os logs do aplicativo
4. Teste com curl:
```bash
curl -X POST http://localhost:5000/api/webhook/whatsapp \
  -H "Content-Type: application/json" \
  -d '{"entry":[{"changes":[{"value":{"messages":[{"from":"5561995017468","type":"text","text":{"body":"https://shopee.com.br/..."}}]}}]}]}'
```

### Link afiliado não está sendo gerado
1. Verifique se `ShopeeAffiliateId` está correto no Config.cs
2. Verifique se o link é realmente do Shopee
3. Verifique logs de `[AFFILIATE]`

## 📞 Suporte

Para mais informações sobre Meta WhatsApp Cloud API:
- [Documentação Official](https://developers.facebook.com/docs/whatsapp/cloud-api/)

Para Playwright:
- [Documentação Official](https://playwright.dev/dotnet/)
