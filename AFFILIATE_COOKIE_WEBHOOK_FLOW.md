# Fluxo de Afiliado com Cookie + Webhook

## Cenário comum

1. Usuário clica no seu link de afiliado.
2. Ele acessa sua página de oferta ou landing page.
3. Um cookie de rastreamento é ativado na sua página.
4. O lead se cadastra na sua página e envia os dados.
5. O lead é enviado para o Zapier.
6. O Zapier envia o ID do afiliado + dados do lead para a plataforma de vendas.

---

## Como funciona neste projeto

### 1. Clique no link de afiliado

Endpoint:
```
GET /api/affiliate/click?affiliateId=18385910279&redirectUrl=https://seu-site.com/landing
```

O que acontece:
- O serviço define um cookie `affiliate_id` no domínio da sua página
- O cookie fica válido por `Config.AffiliateCookieDurationDays` dias
- O usuário é redirecionado para a URL final fornecida (`redirectUrl`)
- Se `redirectUrl` não for válido, o usuário é redirecionado para `Config.AffiliateLandingPageUrl`

### 2. Cookie de rastreamento ativado

No navegador do usuário, o cookie `affiliate_id` permanece associado ao domínio do seu site.
Isso permite capturar o ID do afiliado quando o lead preencher o formulário.

### 3. Lead se cadastra

Quando o lead envia dados do formulário, a sua página deve chamar o endpoint:

```
POST /api/affiliate/lead
Content-Type: application/json

{
  "name": "João Silva",
  "email": "joao@example.com",
  "phone": "5511999999999",
  "message": "Quero saber mais",
  "metadata": {
    "utm_source": "instagram",
    "product": "fone bluetooth"
  }
}
```

O endpoint também tenta ler o cookie `affiliate_id` automaticamente, caso não seja enviado no corpo.

### 4. Zapier recebe o lead

A API do projeto encaminha o lead para o webhook do Zapier usando `Config.ZapierLeadWebhookUrl`.
O payload enviado contém:
- `affiliate_id`
- `name`
- `email`
- `phone`
- `message`
- `source`
- `landing_page_url`
- `metadata`

### 5. Zapier envia para a plataforma de vendas

No Zapier, configure um Webhook que receba o POST e encaminhe os dados para o CRM, sistema de vendas ou outro serviço.
Dessa forma o ID do afiliado chega junto com as informações do lead.

---

## Campos de configuração em `Config.cs`

```csharp
public static string AffiliateCookieName = "affiliate_id";
public static int AffiliateCookieDurationDays = 30;
public static string AffiliateLandingPageUrl = "https://seu-domínio.com";
public static string ZapierLeadWebhookUrl = "";
```

---

## Exemplo completo de uso

1. Montar link de afiliado:
```
https://seu-domínio.com/api/affiliate/click?affiliateId=18385910279&redirectUrl=https://seu-site.com/landing
```

2. Usuário clica e chega ao seu site.
3. Cookie `affiliate_id` é salvo no navegador.
4. Lead preenche formulário.
5. Formulário envia para `POST /api/affiliate/lead`.
6. Seu backend envia o lead para Zapier.
7. Zapier envia para plataforma de vendas.

---

## Observações

- Se o cookie não estiver disponível, você pode enviar `affiliateId` diretamente no corpo do `POST /api/affiliate/lead`.
- O cookie é definido com `SameSite=Lax` para funcionar em navegadores modernos.
- Para uso em produção, configure `Config.AffiliateLandingPageUrl` e `Config.ZapierLeadWebhookUrl`.
