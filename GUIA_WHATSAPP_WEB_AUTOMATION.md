# 🤖 Guia: Automação WhatsApp Web com Playwright

## 🎯 O que faz

A automação WhatsApp Web permite que o bot:

✅ **Envie promoções automaticamente** em grupos todos os dias  
✅ **Monitore grupos** e responda mensagens com links Shopee  
✅ **Converta links** automaticamente  
✅ **Funcione sem token expirando** (usa WhatsApp Web ao invés da API Business)

---

## 🚀 Como Usar

### 1. **Habilitar a Automação**

Edite [Config.cs](Config.cs#L48-L51):

```csharp
// Ativar/desativar
public static bool EnableWhatsAppWebAutomation = true;

// Nome do grupo para enviar promoções DIÁRIAS
public static string WhatsAppGroupNameForDailyPromos = "Ofertas OfertaBot";

// Grupo para MONITORAR e responder automaticamente
public static string WhatsAppGroupNameToMonitor = "Ofertas OfertaBot";

// Ativar monitoramento automático
public static bool EnableGroupMonitoring = true;
```

### 2. **Executar o Bot**

```bash
dotnet run
```

**Na primeira execução:**
- ✅ Um navegador Chromium abrirá automaticamente
- ✅ Aparecerá um **QR Code** do WhatsApp Web
- ✅ Abra o WhatsApp no seu celular
- ✅ Vá em: **Configurações → Dispositivos Conectados → Conectar Dispositivo**
- ✅ **Escaneie o QR Code** que aparece na tela

Depois do scan, o bot estará conectado e funcionando!

---

## 📋 Funcionalidades

### 🎁 Enviar Promoção para Grupo

```csharp
var automation = new WhatsAppWebAutomationService();
await automation.InitializeAsync();
await automation.SendMessageToGroupAsync("Ofertas OfertaBot", "🎁 Link: https://s.shopee.com.br/...");
await automation.DisposeAsync();
```

### 👁️ Monitorar Grupo e Responder

O bot monitora o grupo configurado e responde automaticamente:

```
Usuário: Olá, veja esse link: https://s.shopee.com.br/60NIWu4A6C
Bot: 🎉 Link convertido!
     🔗 https://shopee.com.br/...?aff_id=18385910279
```

### 📞 Enviar para Contato Individual

```csharp
await automation.SendMessageToContactAsync("5561999999999", "Oi! Confira essa oferta!");
```

---

## ⚙️ Configuração Avançada

### Alterar Horário de Envio Diário

[Config.cs](Config.cs#L41-L42):

```csharp
public static int DailyLinkPostHourUtc = 12;      // 12 = 9h de Brasília (UTC-3)
public static int DailyLinkPostMinute = 0;        // :00
```

**Conversão de horários (UTC):**
- 7 UTC = 4h da manhã (Brasília)
- 12 UTC = 9h da manhã
- 17 UTC = 2h da tarde
- 23 UTC = 8h da noite

### Desabilitar Monitoramento

```csharp
public static bool EnableGroupMonitoring = false;
```

Assim o bot só envia promoções, não responde automaticamente.

---

## 🐛 Solução de Problemas

### ❌ "Timeout esperando login"

**Problema:** Não conseguiu escanear o QR Code

**Solução:**
1. Aguarde o QR Code aparecer (pode levar até 30 segundos)
2. Abra WhatsApp no celular
3. Vá em **Configurações → Dispositivos Conectados → Conectar Dispositivo**
4. Escaneie o código QR que aparece na tela do navegador

### ❌ "Grupo não encontrado"

**Problema:** Nome do grupo está errado ou não existe

**Solução:**
1. Verifique o nome exato do grupo (case-sensitive)
2. Certifique-se de que você é membro do grupo
3. Edite [Config.cs](Config.cs) com o nome correto

### ❌ "Não foi possível encontrar botão Enviar"

**Problema:** A interface do WhatsApp Web mudou

**Solução:**
1. Atualize o Playwright: `dotnet add package Microsoft.Playwright`
2. Inspecione a página (F12) para encontrar o novo seletor do botão

### ⚠️ "Mensagens não sendo respondidas"

**Verificar:**
- [ ] `EnableGroupMonitoring = true` em Config.cs
- [ ] Nome do grupo configurado corretamente
- [ ] Você é admin ou membro do grupo
- [ ] Links Shopee válidos nas mensagens

---

## 🔄 Fluxo Completo

```
Bot Inicia
    ↓
Abre WhatsApp Web
    ↓
Escaneia QR Code
    ↓
Aguarda horário agendado (12 UTC)
    ↓
Envia promoções diárias ao grupo
    ↓
Monitora grupo continuamente
    ↓
Quando alguém envia link Shopee
    ↓
Bot responde com link afiliado
```

---

## 📊 Logs Importantes

Procure nos logs por:

```
[WHATSAPP-WEB-SERVICE] ✅ Serviço de automação iniciado
[WHATSAPP-WEB] ✅ Login realizado com sucesso
[WHATSAPP-WEB] ✅ Mensagem enviada para 'Ofertas OfertaBot'
[WHATSAPP-WEB] ✅ Resposta automática enviada
```

---

## ⚡ Performance e Limitações

- ✅ Funciona 24/7
- ✅ Sem limite de mensagens (WhatsApp Web não tem API rate limit)
- ⚠️ Requer navegador aberto em background
- ⚠️ Mais lento que API Cloud (1-2 segundos por mensagem)
- ✅ Nunca expira token

---

## 🎓 Próximos Passos

1. **Configure** o nome do seu grupo em Config.cs
2. **Execute** `dotnet run`
3. **Escaneie** o QR Code
4. **Teste** enviando um link Shopee no grupo
5. **Veja** o bot responder automaticamente! 🎉
