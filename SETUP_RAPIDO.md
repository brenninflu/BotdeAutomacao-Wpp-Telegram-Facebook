# 🚀 Guia Rápido de Setup - OfertaBot v2.0

## ⚡ Setup em 5 Minutos

### 1. Clonar e Instalar
```bash
cd c:\Users\Brenno Xavier\Documents\OfertaBot
dotnet restore
```

### 2. Instalar Playwright Browsers
```powershell
cd bin/Debug/net10.0
pwsh playwright.ps1 install chromium
cd ../../../
```

### 3. Configurar Credenciais (Config.cs)

Abra `Config.cs` e atualize:

```csharp
// WhatsApp Cloud API (obtém em Meta Business Platform)
public static string WhatsAppAccessToken = "SEU_TOKEN_AQUI";
public static string WhatsAppPhoneNumberId = "SEU_PHONE_NUMBER_ID";
public static string WhatsAppVerifyToken = "seu_token_secreto_MUDE_ISSO";

// Shopee Affiliate
public static string ShopeeAffiliateId = "SEU_ID_DE_AFILIADO";
```

### 4. Executar
```bash
dotnet run
```

Você verá:
```
════════════════════════════════════════════════════
           🤖 OfertaBot - Versão 2.0
════════════════════════════════════════════════════
[STARTUP] Iniciando servidor em http://localhost:5000:5000
[STARTUP] Webhook disponível em: http://localhost:5000/api/webhook/whatsapp
```

## 🧪 Testar Localmente

### Teste 1: Health Check
```bash
curl http://localhost:5000/health
```

Resposta esperada:
```json
{"status":"healthy","timestamp":"2026-05-12T..."}
```

### Teste 2: Status da API
```bash
curl http://localhost:5000/api/status
```

### Teste 3: Simular Webhook WhatsApp
```bash
curl -X POST http://localhost:5000/api/webhook/whatsapp \
  -H "Content-Type: application/json" \
  -d '{
    "entry": [{
      "changes": [{
        "value": {
          "messages": [{
            "from": "5561995017468",
            "id": "msg_123",
            "timestamp": "1234567890",
            "type": "text",
            "text": {"body": "https://s.shopee.com.br/60NIWu4A6C"}
          }],
          "contacts": [{
            "profile": {"name": "João"},
            "wa_id": "5561995017468"
          }]
        }
      }]
    }]
  }'
```

## 🌐 Configurar Webhook no Meta Business Platform

### Passo 1: Criar App Meta
1. Vá para [developers.facebook.com/apps](https://developers.facebook.com/apps)
2. Clique em "Criar App"
3. Escolha tipo "Negócios"
4. Nome: "OfertaBot"

### Passo 2: Adicionar Produto WhatsApp
1. No seu app, clique em "Adicionar produto"
2. Procure por "WhatsApp"
3. Clique em "Configurar"

### Passo 3: Gerar Access Token
1. Em **Configurações** → **Credenciais**, gere um "Token de Acesso de Portador"
2. Copie e cole em `Config.cs` → `WhatsAppAccessToken`

### Passo 4: Obter Phone Number ID
1. Em **WhatsApp** → **Números de Telefone**, copie o "ID da Conta"
2. Cole em `Config.cs` → `WhatsAppPhoneNumberId`

### Passo 5: Configurar Webhook
1. Em **WhatsApp** → **Configuração**, clique em "Editar"
2. Em **Webhooks**, clique em "Gerenciar webhooks"
3. Configure:
   - **URL de Callback**: `https://seu-dominio.com/api/webhook/whatsapp`
   - **Token de Verificação**: (mesmo valor em Config.cs → `WhatsAppVerifyToken`)
   - **Campos de Webhook**: Selecione `messages`

### Passo 6: Testar Webhook
1. Clique em "Testar Webhook"
2. O Meta enviará um desafio
3. Seu servidor responderá com o challenge token
4. Você verá ✅ "Conectado"

## 📱 Testar no WhatsApp Real

1. Vá em **Configurações** → **Números de Telefone** no Meta
2. Clique em "Gerenciar Números de Telefone"
3. Em "Remetentes", clique "Começar"
4. Selecione seu número WhatsApp
5. Pronto! O bot agora receberá mensagens

### Teste de Fluxo Completo
Envie uma mensagem no WhatsApp com um link:
```
Oi! Achei este produto: https://s.shopee.com.br/60NIWu4A6C
```

Você receberá:
```
🎉 Ótimo! Link processado!

📦 Produto: [Nome do Produto]
💰 Preço: R$ 00,00
⭐ Avaliação: 4.5/5

🔗 Link afiliado:
https://s.shopee.com.br/60NIWu4A6C?affiliate_id=18385910279
```

## 🐛 Debugando

### Ver Logs Detalhados
Abra o arquivo `Program.cs` e procure por `Console.WriteLine` para ver os logs em tempo real.

Prefixos úteis:
- `[WEBHOOK]` - Recepção de mensagens
- `[PROCESSING]` - Processamento de links
- `[PLAYWRIGHT]` - Scraping de dados
- `[AFFILIATE]` - Conversão de links
- `[WHATSAPP]` - Envio de mensagens

### Playwright Headed Mode (com visual)
Se você quer VER o navegador funcionando:

```csharp
// Em Config.cs
public static bool PlaywrightHeadless = false; // Muda para true depois!
```

Agora você verá uma janela do navegador fazendo o scraping!

## 📂 Estrutura de Pastas Criada

```
OfertaBot/
├── Models/
│   ├── WhatsAppMessage.cs (novo)
│   └── LinkAnalysis.cs (novo)
├── Services/
│   ├── LinkDetectionService.cs (novo)
│   ├── PlaywrightService.cs (novo)
│   └── LinkProcessingService.cs (novo)
├── Controllers/
│   └── WebhookController.cs (novo)
└── README_V2.md (novo)
```

## 🔧 Troubleshooting

| Problema | Solução |
|----------|---------|
| `Playwright não encontrado` | Execute `pwsh playwright.ps1 install chromium` |
| `Access Token inválido` | Gere um novo token no Meta Business Platform |
| `Webhook não recebe mensagens` | Verifique se a URL é pública e o verify_token está correto |
| `Link afiliado vazio` | Verifique se `ShopeeAffiliateId` está preenchido |
| `Porta 5000 já em uso` | Altere em `Config.cs` → `ServerPort` |

## 🎯 Próximos Passos

1. ✅ Deploy em servidor público (ngrok para testes, AWS/Azure/Heroku para produção)
2. ✅ Adicionar fila de processamento (para muitas requisições)
3. ✅ Cache de produtos já processados
4. ✅ Logging em banco de dados
5. ✅ Suporte a mais plataformas (Mercado Livre, Amazon)

## 📞 Documentação Completa

Leia `README_V2.md` para documentação detalhada!

---

**Desenvolvido com ❤️ para OfertaBot v2.0**
