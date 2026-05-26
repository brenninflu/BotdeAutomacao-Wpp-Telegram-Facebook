# Script para testar o webhook WhatsApp localmente
# Simula uma mensagem recebida do WhatsApp

$webhookUrl = "http://localhost:5000/api/webhook/whatsapp"

# Payload que simula uma mensagem do WhatsApp
$payload = @{
    entry = @(
        @{
            changes = @(
                @{
                    value = @{
                        messages = @(
                            @{
                                from = "5561995017468"
                                id = "test-msg-id"
                                timestamp = [System.DateTime]::UtcNow.ToString("s")
                                type = "text"
                                text = @{
                                    body = "Olá bot! Teste: https://s.shopee.com.br/60NIWu4A6C"
                                }
                            }
                        )
                        contacts = @(
                            @{
                                profile = @{
                                    name = "Usuário Teste"
                                }
                                wa_id = "5561995017468"
                            }
                        )
                    }
                }
            )
        }
    )
} | ConvertTo-Json -Depth 10

Write-Host "🧪 Testando webhook WhatsApp..." -ForegroundColor Cyan
Write-Host "📍 URL: $webhookUrl" -ForegroundColor Gray
Write-Host "📦 Payload:" -ForegroundColor Gray
Write-Host ($payload | ConvertFrom-Json | ConvertTo-Json -Depth 10) -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri $webhookUrl `
        -Method POST `
        -ContentType "application/json" `
        -Body $payload `
        -UseBasicParsing

    Write-Host "✅ Resposta do servidor:" -ForegroundColor Green
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Body: $($response.Content)" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Erro ao chamar webhook:" -ForegroundColor Red
    Write-Host "Erro: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        Write-Host "Resposta: $($reader.ReadToEnd())" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "💡 Dica: Verifique os logs do bot no console para mais detalhes" -ForegroundColor Yellow
