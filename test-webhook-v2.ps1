# Script melhorado para testar o webhook WhatsApp
$webhookUrl = "http://localhost:5000/api/webhook/whatsapp"

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
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri $webhookUrl `
        -Method POST `
        -ContentType "application/json" `
        -Body $payload `
        -UseBasicParsing `
        -ErrorAction Stop

    Write-Host "✅ Sucesso!" -ForegroundColor Green
    Write-Host "Status: $($response.StatusCode)" -ForegroundColor Green
}
catch [System.Net.Http.HttpRequestException] {
    Write-Host "❌ Erro ao conectar:" -ForegroundColor Red
    Write-Host "$($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Verifique se o bot está rodando em outro terminal com: dotnet run" -ForegroundColor Yellow
}
catch {
    Write-Host "❌ Erro $($_.Exception.Response.StatusCode):" -ForegroundColor Red
    
    try {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $responseBody = $reader.ReadToEnd()
        
        Write-Host "📋 Resposta do servidor:" -ForegroundColor Yellow
        Write-Host $responseBody -ForegroundColor Yellow
        
        $reader.Close()
        $stream.Close()
    }
    catch {
        Write-Host "Não foi possível ler a resposta de erro" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "📌 Verifique os logs do bot no outro terminal para detalhes" -ForegroundColor Cyan
