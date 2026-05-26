# ⚡ CHECKLIST DE CONFIGURAÇÃO - OfertaBot v2.0

Siga este checklist para ter o bot funcionando em 5 minutos!

---

## ✅ PRÉ-REQUISITOS
- [ ] .NET 10.0 instalado
- [ ] Conta Meta Business Platform
- [ ] Número WhatsApp Business registrado
- [ ] ID de afiliado Shopee

---

## 📝 PASSO 1: CLONAR E INSTALAR

```bash
# Navegar até a pasta do projeto
cd c:\Users\Brenno Xavier\Documents\OfertaBot

# Instalar dependências .NET
dotnet restore
```

- [ ] `dotnet restore` executado com sucesso

---

## 🎮 PASSO 2: INSTALAR PLAYWRIGHT

```powershell
# Navegar até a pasta do binário
cd bin/Debug/net10.0

# Instalar Chromium (pode levar 5-10 minutos)
pwsh playwright.ps1 install chromium

# Voltar
cd ../../../
```

- [ ] Chromium instalado com sucesso
- [ ] Arquivo `playwright.ps1` encontrado

---

## 🔐 PASSO 3: OBTER CREDENCIAIS (Meta)

### 3.1 Criar App Meta (se não tiver)
1. Ir para https://developers.facebook.com/apps
2. Clicar em "Criar App"
3. Tipo: "Negócios"
4. Nome: "OfertaBot"
5. Clique em "Criar"

- [ ] App Meta criado

### 3.2 Obter Access Token
1. Em seu app → **Configurações** → **Credenciais**
2. Seção "Tokens de acesso de portador"
3. Clique em "Gerar"
4. **Copie o token** (usa em Config.cs)

```
Formato: EAAvPigRCnRo...
```

- [ ] Access Token copiado

### 3.3 Obter Phone Number ID
1. Em seu app → **WhatsApp** → **Números de Telefone**
2. Clique em seu número
3. Copie o campo **"ID da Conta"** (não o número de telefone)

```
Formato: 1064889903381893
```

- [ ] Phone Number ID copiado

### 3.4 Criar Verify Token
- [ ] Crie uma senha forte para verify token (ex: `sup3r$3cr3t0Bot2026`)

---

## ⚙️ PASSO 4: ATUALIZAR CONFIG.CS

Abra `Config.cs` e atualize:

```csharp
// Linha ~12
public static string WhatsAppAccessToken = "COLE_O_TOKEN_AQUI";

// Linha ~11  
public static string WhatsAppPhoneNumberId = "COLE_O_NUMERO_ID_AQUI";

// Linha ~13
public static string WhatsAppVerifyToken = "sua_senha_secreta_aqui";

// Linha ~17 (opcional - seu ID de afiliado Shopee)
public static string ShopeeAffiliateId = "SEU_ID_AQUI";
```

- [ ] WhatsAppAccessToken atualizado
- [ ] WhatsAppPhoneNumberId atualizado
- [ ] WhatsAppVerifyToken definido
- [ ] ShopeeAffiliateId definido (opcional)

---

## 🚀 PASSO 5: EXECUTAR O BOT

```bash
dotnet run
```

Você deve ver:

```
════════════════════════════════════════════════════
           🤖 OfertaBot - Versão 2.0
════════════════════════════════════════════════════
[STARTUP] Iniciando servidor em http://localhost:5000:5000
[STARTUP] Playwright inicializado com sucesso
[STARTUP] Webhook disponível em: http://localhost:5000/api/webhook/whatsapp
════════════════════════════════════════════════════
```

- [ ] Servidor iniciado com sucesso
- [ ] Playwright inicializado
- [ ] Nenhum erro na inicialização

---

## 🧪 PASSO 6: TESTAR LOCALMENTE (Opcional)

### Teste 1: Health Check
```bash
curl http://localhost:5000/health
```

Resposta esperada:
```json
{"status":"healthy","timestamp":"..."}
```

- [ ] Health check respondeu

### Teste 2: Simular Mensagem
```bash
curl -X POST http://localhost:5000/api/webhook/whatsapp \
  -H "Content-Type: application/json" \
  -d '{
    "entry": [{
      "changes": [{
        "value": {
          "messages": [{
            "from": "5561995017468",
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

- [ ] Webhook respondeu com sucesso

---

## 🌐 PASSO 7: CONFIGURAR WEBHOOK NO META (Importante!)

### 7.1 Usar Ngrok para Expor Localhost (Dev)
```bash
# Instale ngrok primeiro (https://ngrok.com/)

# Em outro terminal
ngrok http 5000
```

Você receberá uma URL como: `https://abc123.ngrok.io`

- [ ] Ngrok rodando e expondo URL

### 7.2 Configurar no Meta Business Platform

1. Seu app → **WhatsApp** → **Configuração**
2. Em **Webhooks** → Clique em **Editar**
3. **URL de Callback**: `https://seu-dominio.com/api/webhook/whatsapp`
   - Se dev: `https://abc123.ngrok.io/api/webhook/whatsapp`
4. **Token de Verificação**: (o valor que você criou em PASSO 4)
5. Clique em **Verificar e Salvar**

Você deve ver: ✅ "Conectado"

- [ ] Webhook configurado no Meta
- [ ] Status mostra "Conectado"

### 7.3 Configurar Webhook Fields
1. Em **Webhooks** → **Campos do Webhook**
2. Selecione `messages`
3. Clique em **Salvar**

- [ ] Campo `messages` selecionado

---

## 📱 PASSO 8: TESTAR NO WHATSAPP REAL

1. Meta Business Platform → **Números de Telefone**
2. Seu número → **Gerenciar Números de Telefone**
3. Seção **Remetentes** → **Começar**
4. Selecione seu número
5. Pronto! O bot agora recebe mensagens

### Teste:
Envie uma mensagem para seu número WhatsApp:

```
Oi! Achei este produto: https://s.shopee.com.br/60NIWu4A6C
```

Você deve receber:

```
🎉 Ótimo! Link processado!

📦 Produto: [Nome]
💰 Preço: R$ [Preço]
⭐ Avaliação: [Nota]/5

🔗 Link afiliado:
https://s.shopee.com.br/...?affiliate_id=18385910279
```

- [ ] Mensagem enviada com sucesso
- [ ] Resposta recebida automaticamente
- [ ] Link afiliado presente na resposta

---

## 📊 RESUMO DO FLUXO

```
Você envia no WhatsApp
        ↓
Webhook recebe em tempo real
        ↓
Bot detecta link Shopee
        ↓
Playwright extrai dados
        ↓
Link é convertido para afiliado
        ↓
Resposta é enviada de volta
        ↓
Você vê produto + preço + link afiliado
```

---

## 🆘 TROUBLESHOOTING RÁPIDO

| Problema | Solução |
|----------|---------|
| `Playwright não encontrado` | Execute: `cd bin/Debug/net10.0 && pwsh playwright.ps1 install chromium` |
| `Access Token inválido` | Gere novo token em Meta Business Platform |
| `Webhook retorna 401` | Verifique se verify_token está correto em Config.cs |
| `Porta 5000 já em uso` | Altere `ServerPort` em Config.cs |
| `Não recebe mensagens` | Verifique se webhook URL está correta no Meta |
| `Botão "Verificar" fica carregando` | Aguarde o servidor processar. Podem levar 30s |

---

## ✅ VERIFICAÇÃO FINAL

Tudo funcionando? Marque:

- [ ] Servidor rodando em http://localhost:5000
- [ ] Playwright inicializado com sucesso  
- [ ] Webhook configurado no Meta
- [ ] Status mostra "Conectado"
- [ ] Testou health check localmente
- [ ] Webhook configurado recebe requisições
- [ ] Testou no WhatsApp real
- [ ] Recebeu resposta automática

---

## 🎉 SUCESSO!

Se tudo foi marcado, seu OfertaBot v2.0 está **100% funcional**!

---

## 📚 PRÓXIMOS PASSOS

1. **Leia README_V2.md** - Documentação completa
2. **Configure mais opções** em Config.cs se desejar
3. **Deploy** para servidor público (AWS, Heroku, Azure)
4. **Customize** resposta em WebhookController.GenerateResponse()
5. **Monitore** com logs do servidor

---

## 💡 DICAS

- Mantenha ngrok rodando enquanto testa localmente
- Verifique logs do servidor para debugging
- Teste com links diferentes do Shopee
- Acompanhe logs com prefixos: `[WEBHOOK]`, `[PROCESSING]`, etc

---

**Desenvolvido com ❤️ para você**

Dúvidas? Leia a documentação completa!
