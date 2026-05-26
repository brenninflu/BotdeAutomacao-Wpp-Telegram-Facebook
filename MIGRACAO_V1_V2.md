# 📊 Comparação v1.0 vs v2.0

## Arquitetura

### v1.0 (Anterior)
```
Timer (a cada 30 min)
    ↓
OfertaService (busca de lista)
    ↓
MensagemService (formata)
    ↓
WhatsAppService (envia para número fixo)
    ↓
TelegramService (envia para canal fixo)
```

❌ **Problemas:**
- Ciclo contínuo baseado em timer
- Apenas envia para números/canais pré-configurados
- Sem recepção de mensagens do usuário
- Sem processamento de links dinâmicos
- Sem automação web

---

### v2.0 (Nova)
```
WhatsApp Cloud API
    ↓
Webhook Listener (WebhookController)
    ↓
LinkDetectionService (detecta links)
    ↓
LinkProcessingService (orquestra)
    ├─→ PlaywrightService (scraping)
    ├─→ ShopeeAffiliateService (conversão)
    └─→ Response Builder
        ↓
WhatsAppService (envia de volta ao usuário)
```

✅ **Vantagens:**
- Baseada em **webhook** (event-driven)
- Recebe mensagens em **tempo real**
- Processa **links dinâmicos**
- **Responde ao usuário** (conversacional)
- Scraping automático com **Playwright**
- Arquitetura **escalável**
- Suporte a **múltiplos usuários**

---

## Mudanças Principais

### 1. Arquitetura de Aplicação
| Aspecto | v1.0 | v2.0 |
|--------|------|------|
| Tipo | Console App + Timer | ASP.NET Core API |
| Trigger | Timer (a cada 30 min) | Webhook (em tempo real) |
| Entrada | Lista de URLs em Config | Mensagens WhatsApp |
| Saída | Envia para números fixos | Responde ao remetente |
| Framework | .NET Console | .NET 10 Web API |

### 2. Serviços Principais

#### Novos Serviços
| Serviço | Responsabilidade |
|---------|------------------|
| `LinkDetectionService` | Extrai e valida links da mensagem |
| `PlaywrightService` | Scraping de dados do Shopee |
| `LinkProcessingService` | Orquestra todo o fluxo |
| `WebhookController` | Endpoint HTTP para WhatsApp |

#### Serviços Refatorados
| Serviço | Mudança |
|---------|---------|
| `WhatsAppService` | Agora aceita número dinâmico, não fixo |
| `ShopeeAffiliateService` | Nome melhorado, lógica simplificada |

#### Serviços Legados (ainda existem, podem ser removidos)
- `OfertaService` - Usa timer, não é mais necessário
- `MensagemService` - Lógica de formatação ainda útil
- `SentOffersService` - Tracking de ofertas enviadas
- `TelegramService` - Opcional
- `VideoGenerationService` - Legado

### 3. Models

#### Novos Models
```csharp
WhatsAppMessage.cs     // Modelos de request/response WhatsApp
LinkAnalysis.cs        // Análise completa de um link
```

#### Models Existentes Mantidos
```csharp
Oferta.cs              // Dados do produto (ainda usado)
```

### 4. Endpoints da API

#### Novos Endpoints
```
GET  /health                      Health check básico
GET  /api/status                  Status detalhado da API
GET  /api/webhook/whatsapp        Verificação de webhook (Meta)
POST /api/webhook/whatsapp        Recepção de mensagens
```

### 5. Fluxo de Dados

#### v1.0
```
Config.ProductLists
    ↓
OfertaService.GetNextOfertaAsync()
    ↓
MensagemService.GerarMensagem()
    ↓
WhatsAppService.SendTextMessageAsync(recipient=fixo)
```

#### v2.0
```
WhatsApp Message (evento webhook)
    ↓
WebhookController (recebe POST)
    ↓
LinkDetectionService.GetValidShopeeLink()
    ↓
LinkProcessingService.ProcessMessageAsync()
    ├─ ShopeeAffiliateService.TransformToAffiliateLink()
    └─ PlaywrightService.GetProductDataAsync()
    ↓
WebhookController.GenerateResponse()
    ↓
WhatsAppService.SendTextMessageAsync(recipient=dinâmico)
```

### 6. Configurações (Config.cs)

#### Adicionadas
```csharp
WhatsAppVerifyToken           // Para verificar webhook
ServerUrl                     // URL do servidor
ServerPort                    // Porta HTTP
PlaywrightTimeoutMs           // Timeout de scraping
PlaywrightHeadless            // Modo headless/headed
```

#### Mantidas (legado)
```csharp
SendInterval                  // Ainda existe para compatibilidade
ProductLists                  // Ainda existe para compatibilidade
UseTelegram                   // Ainda existe para compatibilidade
```

### 7. Dependências (.csproj)

#### Adicionadas
```
Microsoft.Playwright          1.40.0   (Web scraping)
Microsoft.AspNetCore.OpenApi  10.0.0   (Web API)
Swashbuckle.AspNetCore        6.4.6    (Swagger docs)
```

#### Mantidas
```
Newtonsoft.Json              13.0.3
Telegram.Bot                 19.0.0    (opcional)
```

## Migration Guide

### Se você usava v1.0 e quer migrar para v2.0:

#### ✅ O que continua funcionando
- Modelos (`Oferta.cs`)
- Configurações básicas (`Config.cs`)
- Serviços legados continuam lá (podem ser removidos depois)

#### ⚠️ O que muda
1. **Não há mais timer** - O bot agora reage a mensagens (webhook)
2. **Não usa mais `Config.ProductLists`** - Links vêm das mensagens
3. **WhatsAppService mudou** - Você deve passar o número do destinatário
4. **Program.cs é completamente novo** - Agora é ASP.NET Core

#### 🔄 Como migrar código antigo
Se você tem código customizado que usava a v1.0:

**Antes (v1.0):**
```csharp
var whatsAppService = new WhatsAppService(phoneId, token, recipient);
await whatsAppService.SendTextMessageAsync("Olá!");
```

**Depois (v2.0):**
```csharp
var whatsAppService = new WhatsAppService(phoneId, token);
await whatsAppService.SendTextMessageAsync(recipient, "Olá!");
```

## Performance

### v1.0
- ⏱️ Envio a cada 30 minutos (configurável)
- 📊 Processa um produto por ciclo
- 🔄 Sempre rodando (mesmo sem requisições)

### v2.0
- ⚡ Processamento em tempo real
- 📊 Processa múltiplos links em paralelo
- 🔄 Apenas processa quando recebe mensagens
- 🚀 Escalável com múltiplos usuários

## Segurança

### v1.0
- ❌ Nenhuma validação de entrada (webhook)
- ❌ Sem rate limiting
- ❌ Números/canais hardcoded

### v2.0
- ✅ Validação de webhook com verify_token
- ✅ Validação de links (apenas Shopee)
- ✅ Tratamento de erros robusto
- ✅ Suporte a múltiplos usuários de forma segura

## Resumo de Benefícios

| Benefício | v1.0 | v2.0 |
|-----------|------|------|
| **Conversacional** | ❌ | ✅ |
| **Real-time** | ❌ | ✅ |
| **Dinâmico** | ❌ | ✅ |
| **Scraping Web** | ❌ | ✅ |
| **Multi-usuário** | ❌ | ✅ |
| **Escalável** | ❌ | ✅ |
| **RESTful API** | ❌ | ✅ |
| **Webhook** | ❌ | ✅ |

---

## Próximas Versões Sugeridas

### v2.1
- [ ] Fila de processamento (RabbitMQ)
- [ ] Cache de produtos
- [ ] Logging em banco de dados
- [ ] Dashboard de analytics

### v2.2
- [ ] Suporte a Mercado Livre
- [ ] Suporte a Amazon
- [ ] Processamento de imagens
- [ ] Geração automática de vídeos

### v3.0
- [ ] Machine Learning para recomendações
- [ ] Interface web de administração
- [ ] Multi-idioma
- [ ] Suporte a outros canais (Telegram, Instagram)
