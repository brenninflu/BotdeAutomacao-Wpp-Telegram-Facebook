# 🔄 Diagrama Detalhado do Fluxo v2.0

## Fluxo Completo - Sequência Temporal

```
┌─────────────────────────────────────────────────────────────────────────┐
│  📱 PASSO 1: Usuário envia mensagem no WhatsApp                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Usuário: "Oi! Achei esse produto: https://s.shopee.com.br/60NIWu4A6C" │
│                           │                                              │
└───────────────────────────┼──────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  🌐 PASSO 2: WhatsApp Cloud API envia Webhook                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  POST /api/webhook/whatsapp                                             │
│  {                                                                       │
│    entry: [{                                                            │
│      changes: [{                                                        │
│        value: {                                                         │
│          messages: [{                                                   │
│            from: "5561995017468",                                       │
│            text: { body: "https://s.shopee.com.br/60NIWu4A6C" }        │
│          }],                                                            │
│          contacts: [{ profile: { name: "João" } }]                    │
│        }                                                                │
│      }]                                                                │
│    }]                                                                  │
│  }                                                                      │
│                           │                                              │
└───────────────────────────┼──────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  🎯 PASSO 3: WebhookController recebe requisição                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  POST [HttpPost("whatsapp")]                                            │
│   ├─ Valida estrutura da mensagem                                      │
│   ├─ Extrai informações do remetente                                   │
│   ├─ Obtém texto da mensagem                                           │
│   │                                                                      │
│   ▼                                                                      │
│  [WEBHOOK] Recebida nova mensagem do WhatsApp                          │
│  [WEBHOOK] Mensagem de João (5561995017468): https://s.shopee.com.br..│
│                           │                                              │
└───────────────────────────┼──────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  🔍 PASSO 4: LinkProcessingService.ProcessMessageAsync()               │
├─────────────────────────────────────────────────────────────────────────┤
│  Função: Orquestra todo o pipeline de processamento                    │
│                                                                          │
│  Input: "https://s.shopee.com.br/60NIWu4A6C"                           │
│  Output: LinkAnalysis { ... }                                          │
│                           │                                              │
└───────────────────────────┼──────────────────────────────────────────────┘
                            │
                    ┌───────┴───────┐
                    │               │
                    ▼               ▼
        ┌──────────────────┐  ┌──────────────────┐
        │ 4.1: ETAPA 1     │  │ [PROCESSING]     │
        │ Detectar Link    │  │ Iniciando...     │
        └────────┬─────────┘  └──────────────────┘
                 │
                 ▼
        ┌──────────────────┐
        │ LinkDetection    │
        │ Service          │
        │                  │
        │ ExtractLinks()   │ Regex para encontrar URLs
        │      ↓           │
        │ FilterShopee()   │ Filtra apenas Shopee
        │      ↓           │
        │ Resultado:       │ "https://s.shopee.com.br/60NIWu4A6C"
        │ 1 link válido ✓  │
        │                  │
        │ [PROCESSING]     │
        │ Link Shopee      │
        │ detectado ✓      │
        └────────┬─────────┘
                 │
                 ▼
        ┌──────────────────┐
        │ 4.2: ETAPA 2     │
        │ Converter para   │
        │ Link Afiliado    │
        └────────┬─────────┘
                 │
                 ▼
        ┌──────────────────┐
        │ ShopeeAffiliate  │
        │ Service          │
        │                  │
        │ Transform        │ 1. Remove tracking params
        │ Link()           │ 2. Adiciona affiliate_id
        │      ↓           │ 3. Reconstrói URL
        │ URI Builder      │
        │ Parse Query      │
        │ Add Affiliate ID  │ affiliate_id=18385910279
        │      ↓           │
        │ Nova URL:        │ "https://s.shopee.com.br/...?affiliate_id=..."
        │ Link Afiliado ✓  │
        │                  │
        │ [AFFILIATE]      │
        │ Link transformado│
        │ com sucesso ✓    │
        └────────┬─────────┘
                 │
                 ▼
        ┌──────────────────┐
        │ 4.3: ETAPA 3     │
        │ Extrair Dados    │
        │ (Playwright)     │
        └────────┬─────────┘
                 │
                 ▼
        ┌──────────────────────────┐
        │ PlaywrightService        │
        │                          │
        │ LaunchBrowser()          │ Inicia Chromium
        │ GotoAsync(link)          │ Acessa página
        │      ↓                   │
        │ [PLAYWRIGHT]             │
        │ Navegador inicializado   │
        │      ↓                   │
        │ WaitForLoadState()       │ Espera carregar
        │      ↓                   │
        │ ExtractProductData()     │
        │   ├─ Nome do produto     │ "Fone Bluetooth"
        │   ├─ Preço               │ "R$ 89,90"
        │   ├─ Avaliação           │ "4.8/5"
        │   └─ Imagens             │ [...]
        │      ↓                   │
        │ Oferta {                 │
        │   Nome: "Fone Bluetooth" │
        │   PrecoAtual: 89.90m     │
        │   Avaliacao: 4.8         │
        │ }                        │
        │      ↓                   │
        │ [PLAYWRIGHT]             │
        │ Dados extraídos ✓        │
        └────────┬─────────────────┘
                 │
                 ▼
        ┌──────────────────┐
        │ Return Analysis  │
        │                  │
        │ LinkAnalysis {   │
        │   OriginalLink   │
        │   AffiliateLink  │
        │   IsValid: true  │
        │   ProductData    │
        │ }                │
        │                  │
        │ [PROCESSING]     │
        │ Análise          │
        │ completa ✓       │
        └────────┬─────────┘
                 │
                 ▼
```

---

## Continuação: Resposta ao Usuário

```
        LinkAnalysis completa
                 │
                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  📝 PASSO 5: WebhookController.GenerateResponse()                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Input:  LinkAnalysis { ... }                                           │
│  Output: string (mensagem formatada)                                    │
│                                                                          │
│  Monta mensagem:                                                        │
│  ┌──────────────────────────────────────────────┐                       │
│  │ 🎉 Ótimo! Link processado!                    │                       │
│  │                                               │                       │
│  │ 📦 Produto: Fone Bluetooth Premium            │                       │
│  │ 💰 Preço: R$ 89,90                            │                       │
│  │ ⭐ Avaliação: 4.8/5                           │                       │
│  │                                               │                       │
│  │ 🔗 Link afiliado:                             │                       │
│  │ https://s.shopee.com.br/60NIWu4A6C?          │                       │
│  │ affiliate_id=18385910279                      │                       │
│  └──────────────────────────────────────────────┘                       │
│                           │                                              │
└───────────────────────────┼──────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  📲 PASSO 6: WhatsAppService.SendTextMessageAsync()                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Input: recipientNumber="5561995017468", message="..."                  │
│                                                                          │
│  POST https://graph.facebook.com/v17.0/{phoneNumberId}/messages        │
│  {                                                                      │
│    messaging_product: "whatsapp",                                      │
│    to: "5561995017468",                                                │
│    type: "text",                                                       │
│    text: { body: "[mensagem formatada]" }                             │
│  }                                                                      │
│                           │                                              │
│  ✅ Resposta 200 OK       │                                              │
│  [WHATSAPP]               │                                              │
│  Mensagem enviada ✓       │                                              │
│                           ▼                                              │
└─────────────────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  📱 PASSO 7: Usuário recebe resposta                                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  João:                                                                  │
│  🎉 Ótimo! Link processado!                                            │
│                                                                          │
│  📦 Produto: Fone Bluetooth Premium                                     │
│  💰 Preço: R$ 89,90                                                     │
│  ⭐ Avaliação: 4.8/5                                                    │
│                                                                          │
│  🔗 Link afiliado:                                                      │
│  https://s.shopee.com.br/60NIWu4A6C?affiliate_id=18385910279          │
│                                                                          │
│  [Usuário clica no link]                                                │
│  [Compra o produto]                                                     │
│  [Você ganha comissão de afiliado! 💰]                                 │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Diagrama de Componentes

```
                        ┌─────────────────┐
                        │   WhatsApp      │
                        │  Cloud API      │
                        └────────┬────────┘
                                 │
                    POST /api/webhook/whatsapp
                                 │
                                 ▼
                    ┌────────────────────────┐
                    │ WebhookController      │
                    │ [Controllers/]         │
                    │                        │
                    │ • VerifyWebhook()      │
                    │ • HandleWhatsApp       │
                    │   Message()            │
                    │ • GenerateResponse()   │
                    └──────────┬─────────────┘
                               │
                    ┌──────────┴──────────────────┐
                    │                             │
                    ▼                             ▼
        ┌─────────────────────────┐   ┌──────────────────────┐
        │ LinkProcessing          │   │ WhatsAppService      │
        │ Service                 │   │ [Services/]          │
        │ [Services/]             │   │                      │
        │                         │   │ SendTextMessageAsync()│
        │ ProcessMessageAsync()   │   │                      │
        └──────────┬──────────────┘   └──────────────────────┘
                   │
        ┌──────────┼──────────┬─────────────┐
        │          │          │             │
        ▼          ▼          ▼             ▼
    ┌─────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
    │ Link    │ │ Shopee   │ │Playwright│ │ Config   │
    │Detection│ │ Affiliate│ │ Service  │ │ [Config] │
    │Service  │ │ Service  │ │          │ │          │
    │         │ │          │ │ Chromium │ │ Token    │
    │Extract  │ │Transform │ │ Browser  │ │ IDs      │
    │Validate │ │to        │ │          │ │ URLs     │
    │         │ │Affiliate │ │ Scrape   │ │          │
    │         │ │Link      │ │ Data     │ │          │
    └─────────┘ └──────────┘ └──────────┘ └──────────┘
        │           │             │
        └───────────┴─────────────┘
                    │
                    ▼
            LinkAnalysis {
              OriginalLink
              AffiliateLink
              ProductData
              IsValid
            }
```

---

## Exemplo de Processamento Paralelo

```
Múltiplos usuários enviando mensagens simultaneamente:

Usuario A: "https://s.shopee.com.br/ABC"
Usuario B: "https://s.shopee.com.br/DEF"
Usuario C: "https://s.shopee.com.br/GHI"
            │
            ├───────────────────────────┐
            │                           │
            ▼                           ▼
    ┌─────────────────┐        ┌─────────────────┐
    │ Processing A    │        │ Processing B    │ (Paralelo)
    │                 │        │                 │
    │ 1. Detectar     │        │ 1. Detectar     │
    │ 2. Affiliate    │        │ 2. Affiliate    │
    │ 3. Playwright   │        │ 3. Playwright   │
    │ 4. Responder    │        │ 4. Responder    │
    └────────┬────────┘        └────────┬────────┘
             │                          │
             └──────────┬───────────────┘
                        │
                        ▼
            await Task.WhenAll(tasks);
                        │
                ┌───────┴────────┐
                │                │
                ▼                ▼
            Resultado A      Resultado B
            Resposta A       Resposta B
            enviada         enviada
```

---

## Fluxo de Erro

```
Cenários de Erro:

1️⃣  Link não é Shopee
    │
    ├─ "Envie um link da Shopee por favor"
    └─ Retorna HTTP 200 (webhook OK)

2️⃣  Playwright falha (timeout)
    │
    ├─ Link afiliado ainda é gerado
    ├─ Dados do produto ficam vazios
    └─ Resposta sem dados de produto (OK)

3️⃣  Affiliate Service falha
    │
    ├─ "Erro ao converter para link afiliado"
    └─ Retorna LinkAnalysis.IsValid = false

4️⃣  WhatsApp API retorna erro
    │
    ├─ Exception lançada
    ├─ Log de erro
    └─ HTTP 500 (mas webhook já foi aceito)
```

---

## Timeline de Execução

```
T=0ms    | Webhook recebido
T=5ms    | LinkDetection
T=10ms   | LinkProcessing iniciado
T=15ms   | Playwright iniciado
T=500ms  | Página carregada
T=600ms  | Dados extraídos
T=650ms  | Link afiliado gerado
T=700ms  | Resposta construída
T=705ms  | WhatsApp API chamado
T=800ms  | Resposta enviada ✓

Total: ~800ms por requisição
```

---

Desenvolvido com ❤️ para visualização do fluxo OfertaBot v2.0
