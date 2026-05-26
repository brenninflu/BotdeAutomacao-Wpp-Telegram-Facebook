# 🚀 Guia Rápido - Fluxo Zapier + Sales Cookie

## ⚡ Teste em 5 Minutos

### 1. Verificar Configuração
```bash
curl http://localhost:5000/api/affiliate/config
```

**Resposta esperada:**
```json
{
  "affiliateId": "18385910279",
  "cookieName": "affiliate_id",
  "cookieDurationDays": 30,
  "landingPageUrl": "https://seu-domínio.com",
  "zapierWebhookConfigured": false,
  "endpoints": {
    "trackClick": "http://localhost:5000/api/affiliate/click?affiliateId=18385910279",
    "submitLead": "http://localhost:5000/api/affiliate/lead",
    "receiveCommission": "http://localhost:5000/api/affiliate/commission"
  }
}
```

### 2. Rastrear Clique (Simular Usuário Clicando no Link)
```bash
curl -i "http://localhost:5000/api/affiliate/click?affiliateId=18385910279"
```

**Observe:**
- Header `Set-Cookie: affiliate_id=18385910279; Expires=...; Path=/`
- Redirecionamento (302) para sua landing page

### 3. Enviar Lead (Simular Formulário Preenchido)
```bash
curl -X POST http://localhost:5000/api/affiliate/lead \
  -H "Content-Type: application/json" \
  -d '{
    "affiliate_id": "18385910279",
    "name": "João Silva",
    "email": "joao@example.com",
    "phone": "11999999999",
    "message": "Tenho interesse no produto"
  }'
```

**Resposta esperada:**
```json
{
  "message": "Lead recebido e enviado para Zapier.",
  "timestamp": "2026-05-12T15:30:00Z"
}
```

**Logs do servidor:**
```
[ZAPIER] Lead enviado com sucesso para Zapier: joao@example.com
```

### 4. Receber Comissão (Simular Zapier Zap 2)
```bash
curl -X POST http://localhost:5000/api/affiliate/commission \
  -H "Content-Type: application/json" \
  -d '{
    "affiliate_id": "18385910279",
    "transaction_id": "TXN_ABC123",
    "order_id": "ORDER_456",
    "commission_amount": 15.99,
    "status": "released",
    "customer_email": "cliente@example.com"
  }'
```

**Resposta esperada:**
```json
{
  "message": "Comissão processada com sucesso",
  "transactionId": "TXN_ABC123",
  "orderId": "ORDER_456",
  "commissionAmount": 15.99,
  "status": "released",
  "processedAt": "2026-05-12T15:31:00Z"
}
```

**Logs do servidor:**
```
[COMMISSION] ✅ Comissão recebida: R$ 15.99 | Afiliado: 18385910279 | Pedido: ORDER_456
```

---

## 📋 Checklist de Configuração Real

### No Zapier - Zap 1 (Captura de Lead)

- [ ] **Trigger**: Webhooks by Zapier → Catch Hook
- [ ] **Webhook URL**: Copiar a URL gerada e colar em `Config.cs`:
  ```csharp
  public static string ZapierLeadWebhookUrl = "https://hooks.zapier.com/hooks/catch/XXXXX/YYYYY";
  ```

- [ ] **Ação 1**: Sales Cookie → Set/Update User
  - [ ] User ID: mapeado para `{affiliate_id}`
  - [ ] Name: mapeado para `{name}`
  - [ ] Email: mapeado para `{email}`
  - [ ] Phone: mapeado para `{phone}`

- [ ] **Ação 2**: Sales Cookie → Create Transaction
  - [ ] User ID: mapeado para `{affiliate_id}`
  - [ ] Amount: mapeado para `{transaction_value}`
  - [ ] Description: mapeado para `{message}`
  - [ ] Status: "pending"

- [ ] **Testar Zap 1**: Clicar em "Test" no Zapier após enviar um lead

### No Zapier - Zap 2 (Comissão Liberada)

- [ ] **Trigger**: Sales Cookie → New Released Reward
- [ ] **Ação**: Webhooks by Zapier → POST
  - [ ] URL: `http://seu-domínio-real.com/api/affiliate/commission`
  - [ ] Método: POST
  - [ ] Tipo: JSON
  
- [ ] **Mapear campos**:
  - [ ] `affiliate_id` → {affiliate_id}
  - [ ] `transaction_id` → {transaction_id}
  - [ ] `order_id` → {order_id}
  - [ ] `commission_amount` → {commission_amount}
  - [ ] `status` → {status}
  - [ ] `customer_email` → {customer_email}

### No Seu OfertaBot

- [ ] `Config.cs` atualizado com:
  - [ ] `ShopeeAffiliateId = "18385910279"`
  - [ ] `ZapierLeadWebhookUrl = "https://hooks.zapier.com/hooks/catch/XXXXX/YYYYY"`
  - [ ] `AffiliateLandingPageUrl = "https://seu-domínio.com"`
  - [ ] `AffiliateCookieName = "affiliate_id"`
  - [ ] `AffiliateCookieDurationDays = 30`

- [ ] Código está compilando:
  ```bash
  dotnet build
  ```

- [ ] Servidor rodando:
  ```bash
  dotnet run
  ```

---

## 🔗 Links de Afiliado para Enviar

### No WhatsApp/Telegram

```
👉 Clique aqui e se cadastre para ganhar comissão:

http://seu-domínio-real.com/api/affiliate/click?affiliateId=18385910279&redirectUrl=https://seu-domínio.com

Você ganhará 3% em cada compra realizada! 💰
```

**O que acontece quando usuário clica:**
1. Cookie é ativado (30 dias)
2. Redirecionado para sua landing page
3. Se fizer compra → Comissão é rastreada
4. Quando comissão for liberada → Você é notificado via webhook

---

## 🧪 Teste de Integração Completo

### Passo 1: Rastrear Clique
```bash
# Simular usuário clicando no link
curl -i "http://localhost:5000/api/affiliate/click?affiliateId=18385910279"

# Resposta: 302 redirect + Set-Cookie
```

### Passo 2: Enviar Lead
```bash
# Simular formulário preenchido na landing page
curl -X POST http://localhost:5000/api/affiliate/lead \
  -H "Content-Type: application/json" \
  -d '{
    "affiliate_id": "18385910279",
    "name": "Test User",
    "email": "test@example.com",
    "phone": "11999999999",
    "message": "Teste de fluxo"
  }'

# Resposta: Lead enviado para Zapier
# Logs: [ZAPIER] Lead enviado com sucesso para Zapier: test@example.com
```

### Passo 3: Simular Zapier Zap 1
(No Zapier, o Zap 1 já terá recebido o webhook e enviado para Sales Cookie)

### Passo 4: Simular Venda Confirmada
(No Sales Cookie, você marca a transação como concluída)

### Passo 5: Simular Comissão Liberada (Zapier Zap 2)
```bash
# Simular webhook de volta (normalmente vem do Zapier/Sales Cookie)
curl -X POST http://localhost:5000/api/affiliate/commission \
  -H "Content-Type: application/json" \
  -d '{
    "affiliate_id": "18385910279",
    "transaction_id": "TXN_TEST_001",
    "order_id": "ORDER_TEST_001",
    "commission_amount": 15.99,
    "status": "released",
    "customer_email": "test@example.com"
  }'

# Resposta: Comissão processada
# Logs: [COMMISSION] ✅ Comissão recebida: R$ 15.99 | Afiliado: 18385910279 | Pedido: ORDER_TEST_001
```

---

## 📊 Fluxo Resumido

```
1️⃣  Afiliado envia link WhatsApp
    ↓
2️⃣  Usuário clica → Cookie ativado
    ↓
3️⃣  Usuário se cadastra na landing page
    ↓
4️⃣  Sistema envia lead → Zapier Zap 1
    ↓
5️⃣  Zapier integra com Sales Cookie
    ↓
6️⃣  Usuário compra → Venda confirmada
    ↓
7️⃣  Sales Cookie calcula comissão
    ↓
8️⃣  Sales Cookie libera recompensa → Zapier Zap 2
    ↓
9️⃣  Zapier envia webhook → Seu servidor
    ↓
🔟 Sistema processa e atualiza comissão
```

---

## 🐛 Debugging

### Ver logs do servidor
```bash
# Terminal rodando dotnet run
# Procure por:
# [ZAPIER] - eventos de lead
# [COMMISSION] - eventos de comissão
```

### Testar Zapier Zap 1
1. No Zapier, abra Zap 1
2. Clique em "Test & Review"
3. Verifique se webhook foi recebido

### Testar Zapier Zap 2
1. No Sales Cookie, marque uma transação como "Released"
2. Verifique se Zapier Zap 2 foi acionado
3. Verifique logs do servidor para o webhook recebido

### Verificar Cookie no Navegador
```javascript
// Abrir DevTools (F12) → Console
document.cookie  // Verá: "affiliate_id=18385910279"
```

---

**Fluxo testado e pronto para produção! 🎉**
