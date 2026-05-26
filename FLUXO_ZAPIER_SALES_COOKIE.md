# 🔗 Fluxo Completo: OfertaBot + Zapier + Sales Cookie

## 📊 Visão Geral do Fluxo

```
┌─────────────────────────────────────────────────────────────────────┐
│  ETAPA 1: Usuário Clica no Link de Afiliado (OfertaBot)             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Link de Afiliado (via WhatsApp):                                  │
│  http://seu-domínio.com/api/affiliate/click?affiliateId=18385910279│
│                                                                     │
│  └─→ GET /api/affiliate/click                                      │
│      └─→ Cookie (affiliate_id=18385910279) ativado por 30 dias     │
│      └─→ Redireciona para sua landing page                         │
│                                                                     │
└──────────────┬───────────────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────────────┐
│  ETAPA 2: Cookie Script Ativado (JavaScript na Landing Page)        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  <script>                                                           │
│    var affiliateId = localStorage.getItem('affiliate_id');         │
│    // Cookie já foi setado pelo servidor                           │
│    // Agora está disponível para o formulário                      │
│  </script>                                                          │
│                                                                     │
│  Ao carregar a página → Cookie está ativo                          │
│                                                                     │
└──────────────┬───────────────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────────────┐
│  ETAPA 3: Lead Se Cadastra no Formulário                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Usuário preenche formulário de cadastro:                          │
│  - Nome: João Silva                                                │
│  - Email: joao@email.com                                           │
│  - Telefone: 11999999999                                           │
│  - Mensagem: Tenho interesse em...                                │
│                                                                     │
│  Form automaticamente lê o cookie → affiliate_id                   │
│                                                                     │
│  POST /api/affiliate/lead com:                                     │
│  {                                                                 │
│    "affiliate_id": "18385910279" (do cookie)                       │
│    "name": "João Silva",                                           │
│    "email": "joao@email.com",                                      │
│    "phone": "11999999999",                                         │
│    "message": "Tenho interesse..."                                 │
│  }                                                                 │
│                                                                     │
└──────────────┬───────────────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────────────┐
│  ETAPA 4: Sistema Envia para Zapier (Zap 1)                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  AffiliateLeadService.SendLeadToZapierAsync()                      │
│                                                                     │
│  POST Config.ZapierLeadWebhookUrl                                  │
│  {                                                                 │
│    "affiliate_id": "18385910279",                                  │
│    "name": "João Silva",                                           │
│    "email": "joao@email.com",                                      │
│    "phone": "11999999999",                                         │
│    "source": "affiliate_page",                                     │
│    "landing_page_url": "https://sua-landing.com"                  │
│  }                                                                 │
│                                                                     │
│  [ZAPIER] ← Payload recebido                                       │
│                                                                     │
└──────────────┬───────────────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────────────┐
│  ETAPA 5: Zapier Zap 1 - Captura de Lead                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Trigger: Webhooks by Zapier → Catch Hook                         │
│  Webhook URL: https://hooks.zapier.com/hooks/catch/XXXXX/YYYYY   │
│                                                                     │
│  Ações:                                                            │
│  ├─ Action 1: Sales Cookie → Set/Update User                      │
│  │  └─ User ID: {affiliate_id}                                    │
│  │  └─ Name: {name}                                               │
│  │  └─ Email: {email}                                             │
│  │                                                                 │
│  └─ Action 2: Sales Cookie → Create Transaction                   │
│     └─ User ID: {affiliate_id}                                    │
│     └─ Transaction Amount: {transaction_value} (0 inicialmente)   │
│     └─ Transaction ID: {email}_{timestamp}                        │
│     └─ Status: pending                                            │
│                                                                     │
└──────────────┬───────────────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────────────┐
│  ETAPA 6: Sales Cookie Rastreia a Venda                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Sales Cookie recebe a transação:                                  │
│  ├─ Afiliado: 18385910279                                          │
│  ├─ Status da transação: PENDING → APPROVED → CONFIRMED            │
│  ├─ Calcula comissão (ex: 3% da venda)                            │
│  └─ Libera recompensa para o afiliado                             │
│                                                                     │
│  Event Trigger: "New Released Reward"                              │
│                                                                     │
└──────────────┬───────────────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────────────┐
│  ETAPA 7: Zapier Zap 2 - Recompensa Liberada                        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Trigger: Sales Cookie → New Released Reward                      │
│                                                                     │
│  Payload:                                                          │
│  {                                                                 │
│    "affiliate_id": "18385910279",                                  │
│    "commission_amount": 15.99,                                     │
│    "transaction_id": "TXN_12345",                                  │
│    "status": "released"                                            │
│  }                                                                 │
│                                                                     │
│  Action: Webhooks by Zapier → POST                                │
│  └─ URL: http://seu-domínio.com:5000/api/affiliate/commission    │
│  └─ Payload: {parâmetros acima}                                   │
│                                                                     │
└──────────────┬───────────────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────────────┐
│  ETAPA 8: Sistema Processa Comissão (OfertaBot)                    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  POST /api/affiliate/commission                                    │
│                                                                     │
│  AffiliateLeadService.ProcessCommissionWebhook()                   │
│                                                                     │
│  Resultado:                                                        │
│  {                                                                 │
│    "affiliateId": "18385910279",                                   │
│    "commissionAmount": 15.99,                                      │
│    "transactionId": "TXN_12345",                                   │
│    "status": "released",                                           │
│    "processedAt": "2026-05-12T15:30:00Z"                           │
│  }                                                                 │
│                                                                     │
│  TODO: Sistema pode:                                              │
│  ├─ Atualizar saldo do afiliado em BD                             │
│  ├─ Gerar comprovante de comissão                                 │
│  ├─ Enviar email ao afiliado                                      │
│  ├─ Registrar em relatório                                        │
│  └─ Acionar pagamento automático                                  │
│                                                                     │
└──────────────┬───────────────────────────────────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────────────────────────────────┐
│  ✅ FLUXO COMPLETO                                                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Afiliado foi rastreado → Lead foi capturado → Comissão foi        │
│  processada e pode ser paga automaticamente!                       │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🔧 Configuração Passo a Passo

### 1. Configurar Webhook do Zapier (Zap 1) - Captura de Lead

**No Zapier:**
1. Crie novo Zap
2. **Trigger**: Webhooks by Zapier → "Catch Hook"
3. Copie a URL gerada: `https://hooks.zapier.com/hooks/catch/XXXXX/YYYYY`
4. Cole em `Config.cs`:
```csharp
public static string ZapierLeadWebhookUrl = "https://hooks.zapier.com/hooks/catch/XXXXX/YYYYY";
```

**Ações do Zap 1:**
- **Action 1**: Sales Cookie → Set/Update User
  - User ID: `{affiliate_id}`
  - Name: `{name}`
  - Email: `{email}`
  - Phone: `{phone}`

- **Action 2**: Sales Cookie → Create Transaction
  - User ID: `{affiliate_id}`
  - Amount: `{transaction_value}` (pode ser 0 inicialmente)
  - Description: `{message}`

### 2. Configurar Webhook de Retorno (Zap 2) - Comissão Liberada

**No Zapier:**
1. Crie novo Zap 2
2. **Trigger**: Sales Cookie → "New Released Reward"
3. **Action**: Webhooks by Zapier → POST
4. **URL do Post**: `http://seu-domínio.com:5000/api/affiliate/commission`
5. **Método**: POST
6. **Payload**:
```json
{
  "affiliate_id": "{affiliate_id}",
  "transaction_id": "{transaction_id}",
  "order_id": "{order_id}",
  "commission_amount": "{commission_amount}",
  "status": "{status}",
  "customer_email": "{customer_email}"
}
```

### 3. Configurar Config.cs

```csharp
public static string ShopeeAffiliateId = "18385910279";
public static string AffiliateCookieName = "affiliate_id";
public static int AffiliateCookieDurationDays = 30;
public static string AffiliateLandingPageUrl = "https://seu-domínio.com";
public static string ZapierLeadWebhookUrl = "https://hooks.zapier.com/hooks/catch/XXXXX/YYYYY";
```

### 4. Adicionar Script de Cookie na Landing Page

Cole no `<head>` da sua landing page:

```html
<!-- OfertaBot Affiliate Cookie Tracker -->
<script>
(function() {
  const affiliateId = 'SEU_ID_DO_AFILIADO';
  const cookieName = 'affiliate_id';
  
  // Ler cookie se já existir (foi setado pelo servidor)
  function getAffiliateCookie() {
    const name = cookieName + '=';
    const decodedCookie = decodeURIComponent(document.cookie);
    const cookieArray = decodedCookie.split(';');
    for(let cookie of cookieArray) {
      cookie = cookie.trim();
      if (cookie.indexOf(name) === 0) {
        return cookie.substring(name.length);
      }
    }
    return null;
  }
  
  // Ao submeter o formulário, incluir o affiliate_id
  document.addEventListener('DOMContentLoaded', function() {
    const currentAffId = getAffiliateCookie();
    console.log('[Affiliate] ID do afiliado:', currentAffId);
    
    // Adicionar ao formulário automaticamente
    const form = document.querySelector('form');
    if (form && currentAffId) {
      const input = document.createElement('input');
      input.type = 'hidden';
      input.name = 'affiliate_id';
      input.value = currentAffId;
      form.appendChild(input);
    }
  });
})();
</script>
<!-- End OfertaBot Affiliate Cookie Tracker -->
```

### 5. Formulário HTML da Landing Page

```html
<form id="lead-form">
  <input type="text" name="name" placeholder="Seu nome" required>
  <input type="email" name="email" placeholder="Seu email" required>
  <input type="tel" name="phone" placeholder="Seu telefone">
  <textarea name="message" placeholder="Sua mensagem..."></textarea>
  <!-- affiliate_id será adicionado automaticamente pelo script acima -->
  <button type="submit">Enviar</button>
</form>

<script>
document.getElementById('lead-form').addEventListener('submit', async (e) => {
  e.preventDefault();
  
  const formData = new FormData(e.target);
  const data = Object.fromEntries(formData);
  
  const response = await fetch('http://seu-domínio.com:5000/api/affiliate/lead', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
  });
  
  const result = await response.json();
  console.log('Lead enviado:', result);
  alert('Obrigado! Entraremos em contato em breve.');
});
</script>
```

---

## 🔗 Endpoints da API

### 1. Rastrear Clique do Afiliado
```
GET /api/affiliate/click?affiliateId=18385910279&redirectUrl=https://seu-dominio.com
```
- **Função**: Ativa cookie do afiliado e redireciona
- **Resposta**: Redirect (302)

### 2. Enviar Lead
```
POST /api/affiliate/lead
Content-Type: application/json

{
  "affiliate_id": "18385910279",
  "name": "João Silva",
  "email": "joao@email.com",
  "phone": "11999999999",
  "message": "Tenho interesse em..."
}
```
- **Função**: Recebe dados do lead e envia para Zapier
- **Resposta**: `{ "message": "Lead recebido...", "timestamp": "..." }`

### 3. Receber Comissão Liberada
```
POST /api/affiliate/commission
Content-Type: application/json

{
  "affiliate_id": "18385910279",
  "transaction_id": "TXN_12345",
  "commission_amount": 15.99,
  "status": "released",
  "customer_email": "cliente@email.com"
}
```
- **Função**: Processa comissão liberada pelo Sales Cookie
- **Resposta**: `{ "message": "Comissão processada...", "commissionAmount": 15.99 }`

### 4. Ver Configuração
```
GET /api/affiliate/config
```
- **Função**: Debug - mostra fluxo e endpoints
- **Resposta**: Todas as configurações e instruções

---

## 📊 Exemplo Completo de Fluxo

1. **Afiliado envia link no WhatsApp**:
```
Clique aqui: http://seu-domínio.com:5000/api/affiliate/click?affiliateId=18385910279
```

2. **Usuário clica → Cookie ativado → Redirecionado para landing page**

3. **Usuário preenche formulário → Dados enviados para `/api/affiliate/lead`**

4. **Sistema envia para Zapier Zap 1**:
```json
{
  "affiliate_id": "18385910279",
  "name": "João Silva",
  "email": "joao@email.com",
  "phone": "11999999999",
  "source": "affiliate_page"
}
```

5. **Zapier Zap 1 integra com Sales Cookie**:
   - Cria usuário afiliado
   - Cria transação pendente

6. **Lead (João) efetua compra → Venda confirmada**

7. **Sales Cookie calcula comissão (3%) = R$ 15,99**

8. **Sales Cookie libera recompensa → Zapier Zap 2 acionado**

9. **Zapier Zap 2 envia webhook de volta para `/api/affiliate/commission`**:
```json
{
  "affiliate_id": "18385910279",
  "transaction_id": "TXN_ABC123",
  "commission_amount": 15.99,
  "status": "released"
}
```

10. **Sistema processa comissão**:
    - ✅ Registra no BD
    - ✅ Gera comprovante
    - ✅ Notifica afiliado
    - ✅ Aciona pagamento

---

## 🛠️ Próximos Passos

1. **Configure o Zapier Webhook URL** em `Config.cs`
2. **Teste localmente** com ngrok para expor seu servidor
3. **Crie os Zaps 1 e 2** no Zapier
4. **Teste o fluxo completo** enviando um lead de teste
5. **Configure relatórios** de comissões e pagamentos

---

**Fluxo OfertaBot + Zapier + Sales Cookie Completo! 🎉**
