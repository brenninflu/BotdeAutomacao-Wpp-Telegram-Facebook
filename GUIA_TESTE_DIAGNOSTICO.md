# 🧪 GUIA: Testar e Adicionar Bot no Grupo

## ⚠️ Problema Identificado
O bot recebe mensagens em chat privado, mas não responde. Possíveis causas:
1. **Token WhatsApp expirado** 
2. **Erro ao processar a mensagem**
3. **Erro ao enviar resposta**
4. **Webhook não está configurado corretamente**

---

## 🔧 Passo 1: Executar o Bot com Logging Detalhado

```bash
# Terminal 1: Execute o bot
dotnet run

# Espere até ver:
# ════════════════════════════════════════════════════
#            🤖 OfertaBot - Versão 2.0
# ════════════════════════════════════════════════════
```

---

## 🧪 Passo 2: Testar o Webhook Localmente

```powershell
# Terminal 2: Execute o script de teste
.\test-webhook.ps1
```

**Espere por uma destas respostas:**

### ✅ Sucesso
```
✅ Resposta do servidor:
Status Code: 200
Body: Ok
```
👉 Vá para **Passo 3**

### ❌ Erro 500
```
Status Code: 500
```
👉 Verifique os logs do bot (Terminal 1) procurando por `[ERROR]` ou `[WEBHOOK]`

### ❌ Erro 401 ou Timeout
```
Erro ao chamar webhook: Connection refused
```
👉 O bot não está rodando. Execute `dotnet run` no Terminal 1

---

## 📊 Passo 3: Monitorar Logs em Tempo Real

Procure nos logs por estes padrões:

### Fluxo correto esperado:
```
[WEBHOOK] Recebida nova mensagem do WhatsApp
[WEBHOOK] Mensagem de Usuário Teste (5561995017468): Olá bot!...
[WEBHOOK] Iniciando processamento para 5561995017468
[PROCESSING] Iniciando processamento de mensagem
[PROCESSING] Link Shopee detectado: https://s.shopee.com.br/60NIWu4A6C
[PROCESSING] Link afiliado gerado com sucesso
[WEBHOOK] Análise concluída - IsValid: True
[WEBHOOK] Resposta gerada: 🎉 Ótimo! Link processado!...
[WEBHOOK] Enviando resposta para 5561995017468
[WHATSAPP] ✅ Mensagem enviada com sucesso para 5561995017468
```

### Se ver esses logs = ✅ Bot está funcionando!
- Procure por `[WHATSAPP] ✅ Mensagem enviada com sucesso`
- Procure por `[WEBHOOK] Resposta enviada com sucesso`

### Se ver esses erros = ❌ Há um problema:

**Erro: "Nenhum link Shopee detectado"**
```
[PROCESSING] Nenhum link Shopee detectado na mensagem
```
👉 A mensagem não contém um link válido. Envie um link Shopee válido.

**Erro: "WhatsApp API retornou 400/401/403"**
```
[WHATSAPP] ❌ Falha! Status: 401
[WHATSAPP] Resposta: {"error":{"code":401,...}}
```
👉 **RENOVE SEU TOKEN!** Ver seção abaixo.

**Erro: "Conexão recusada"**
```
[WHATSAPP] ❌ Erro ao enviar mensagem: HttpRequestException - Connection refused
```
👉 Verifique se a API do WhatsApp está acessível (internet/firewall)

---

## 🔐 Renovar Token WhatsApp (se necessário)

Se o token expirou (erro 401), siga:

1. Acesse: https://developers.facebook.com/
2. Vá para: **My Apps** → Seu App → **Messenger** → **System User Access Tokens**
3. Gere um novo token com permissão `whatsapp_business_messaging`
4. Copie o token
5. Edite `Config.cs`:

```csharp
public static string WhatsAppAccessToken = "SEU_NOVO_TOKEN_AQUI";
```

6. Salve e reinicie: `dotnet run`

---

## 👥 Passo 4: Adicionar Bot ao Grupo

Uma vez que o bot responda corretamente em privado, adicione-o ao grupo:

### WhatsApp Business (Recomendado)
1. **Requisito**: O telefone do bot deve estar ativo
2. Acesse: https://www.whatsapp.com/business/
3. Configure como **Business Account**
4. Adicione o bot ao grupo como você adicionaria um contato normal
5. O bot receberá mensagens do grupo através do webhook

### Importante para Grupos
- ⚠️ O bot responde **apenas a mensagens com links Shopee**
- ⚠️ Todas as mensagens com links Shopee receberão resposta automática
- 💡 Se acha que haverá spam, implemente filtro: `if (message.From == ownerNumber) { ... }`

---

## 🐛 Checklist de Diagnóstico

- [ ] Bot está rodando (`dotnet run`)
- [ ] Logs mostram `[WEBHOOK] Recebida nova mensagem` ao enviar mensagem
- [ ] Logs mostram `[WHATSAPP] ✅ Mensagem enviada` 
- [ ] Token WhatsApp é válido (teste em https://developers.facebook.com/)
- [ ] URL ngrok está ativa e atualizada em Config.cs
- [ ] Webhook está configurado corretamente no Facebook/WhatsApp

---

## 📞 Próximos Passos

Se tudo passar ✅:
1. Teste enviando um link Shopee real em privado
2. Verifique se a resposta é enviada corretamente
3. Adicione o número ao seu grupo
4. Mencione o bot ou envie um link Shopee no grupo

Se ainda houver erro ❌:
- Compartilhe os logs completos
- Verifique qual mensagem de erro aparece
