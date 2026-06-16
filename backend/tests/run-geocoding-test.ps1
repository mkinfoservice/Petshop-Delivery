# ============================================
# Script PowerShell para testar sistema de geocoding
# Execute este script no PowerShell (Windows)
# ============================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "🧪 Teste de Geocoding - Petshop Delivery" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuração
$BASE_URL = "http://localhost:5082"
$ADMIN_USER = "admin"
$ADMIN_PASS = "admin123"
$PRODUCT_ID = "47e63cc3-7f94-459e-ad6e-2d5461e1bde6"
$DELIVERER_ID = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"

# ============================================
# Passo 1: Login Admin
# ============================================
Write-Host "🔐 PASSO 1: Fazendo login como admin..." -ForegroundColor Yellow

$loginBody = @{
    username = $ADMIN_USER
    password = $ADMIN_PASS
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$BASE_URL/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $TOKEN = $loginResponse.token
    Write-Host "✅ Login OK! Token obtido." -ForegroundColor Green
}
catch {
    Write-Host "❌ ERRO: Não foi possível fazer login. Backend está rodando?" -ForegroundColor Red
    Write-Host "   URL testada: $BASE_URL/auth/login" -ForegroundColor Red
    exit 1
}

# Headers para requests autenticadas
$headers = @{
    "Authorization" = "Bearer $TOKEN"
    "Content-Type" = "application/json"
}

# ============================================
# Passo 2: Criar pedidos com endereços reais do RJ
# ============================================
Write-Host ""
Write-Host "📦 PASSO 2: Criando 5 pedidos com endereços reais do Rio..." -ForegroundColor Yellow

$pedidos = @(
    @{
        name = "Maria Silva"
        phone = "21987654321"
        cep = "22070-001"
        address = "Avenida Atlântica 1702"
        complement = "Apto 501"
        bairro = "Copacabana"
    },
    @{
        name = "João Santos"
        phone = "21987654322"
        cep = "22411-010"
        address = "Rua Vinícius de Moraes 129"
        complement = "Loja"
        bairro = "Ipanema"
    },
    @{
        name = "Ana Costa"
        phone = "21987654323"
        cep = "22250-040"
        address = "Praia de Botafogo 300"
        complement = "Bloco A"
        bairro = "Botafogo"
    },
    @{
        name = "Carlos Oliveira"
        phone = "21987654324"
        cep = "20091-000"
        address = "Praça Pio X"
        complement = "Próximo à Candelária"
        bairro = "Centro"
    },
    @{
        name = "Beatriz Lima"
        phone = "21987654325"
        cep = "22431-050"
        address = "Rua Dias Ferreira 214"
        complement = "Casa"
        bairro = "Leblon"
    }
)

$pedidosIds = @()

foreach ($pedido in $pedidos) {
    $orderBody = @{
        name = $pedido.name
        phone = $pedido.phone
        cep = $pedido.cep
        address = $pedido.address
        complement = $pedido.complement
        paymentMethodStr = "PIX"
        items = @(
            @{
                productId = $PRODUCT_ID
                qty = 2
            }
        )
    } | ConvertTo-Json -Depth 3

    try {
        $orderResponse = Invoke-RestMethod -Uri "$BASE_URL/orders" -Method Post -Body $orderBody -ContentType "application/json"
        $pedidosIds += @{
            publicId = $orderResponse.publicId
            id = $orderResponse.id
            bairro = $pedido.bairro
            address = $pedido.address
        }
        Write-Host "  ✅ Pedido criado: $($orderResponse.publicId) - $($pedido.bairro)" -ForegroundColor Green
    }
    catch {
        Write-Host "  ❌ ERRO ao criar pedido: $($pedido.bairro)" -ForegroundColor Red
        Write-Host "     Erro: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "📋 Pedidos criados: $($pedidosIds.Count)" -ForegroundColor Cyan

# ============================================
# Passo 3: Aguardar um pouco (opcional)
# ============================================
Write-Host ""
Write-Host "⏳ Aguardando 2 segundos antes de mudar status..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

# ============================================
# Passo 4: Mudar status para PRONTO_PARA_ENTREGA
# ============================================
Write-Host ""
Write-Host "🚀 PASSO 3: Mudando status para PRONTO_PARA_ENTREGA (GEOCODING SERÁ EXECUTADO)..." -ForegroundColor Yellow

$statusBody = @{
    status = "PRONTO_PARA_ENTREGA"
} | ConvertTo-Json

foreach ($pedido in $pedidosIds) {
    try {
        $statusResponse = Invoke-RestMethod -Uri "$BASE_URL/orders/$($pedido.publicId)/status" -Method Post -Body $statusBody -Headers $headers
        Write-Host "  ✅ Status atualizado: $($pedido.publicId) - $($pedido.bairro)" -ForegroundColor Green
        Write-Host "     🔍 Verifique os LOGS do backend para ver o geocoding em ação!" -ForegroundColor Cyan
    }
    catch {
        Write-Host "  ❌ ERRO ao atualizar status: $($pedido.publicId)" -ForegroundColor Red
        Write-Host "     Erro: $_" -ForegroundColor Red
    }
}

# ============================================
# Passo 5: Aguardar geocoding
# ============================================
Write-Host ""
Write-Host "⏳ Aguardando 5 segundos para geocoding completar..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# ============================================
# Passo 6: Verificar se pedidos têm coordenadas
# ============================================
Write-Host ""
Write-Host "📍 PASSO 4: Verificando se pedidos foram geocodificados..." -ForegroundColor Yellow

$pedidosComCoords = 0
$pedidosSemCoords = 0

foreach ($pedido in $pedidosIds) {
    try {
        $orderDetails = Invoke-RestMethod -Uri "$BASE_URL/orders/$($pedido.publicId)" -Method Get -Headers $headers

        if ($orderDetails.latitude -and $orderDetails.longitude) {
            Write-Host "  ✅ $($pedido.publicId) - $($pedido.bairro)" -ForegroundColor Green
            Write-Host "     Lat: $($orderDetails.latitude), Lon: $($orderDetails.longitude)" -ForegroundColor Gray
            Write-Host "     Provider: $($orderDetails.geocodeProvider)" -ForegroundColor Gray
            $pedidosComCoords++
        }
        else {
            Write-Host "  ❌ $($pedido.publicId) - $($pedido.bairro) - SEM COORDENADAS!" -ForegroundColor Red
            Write-Host "     Provider: $($orderDetails.geocodeProvider)" -ForegroundColor Gray
            $pedidosSemCoords++
        }
    }
    catch {
        Write-Host "  ❌ ERRO ao buscar pedido: $($pedido.publicId)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "📊 RESUMO DO GEOCODING:" -ForegroundColor Cyan
Write-Host "   ✅ Com coordenadas: $pedidosComCoords" -ForegroundColor Green
Write-Host "   ❌ Sem coordenadas: $pedidosSemCoords" -ForegroundColor Red

# ============================================
# Passo 7: Reprocessar se necessário
# ============================================
if ($pedidosSemCoords -gt 0) {
    Write-Host ""
    Write-Host "⚠️  Alguns pedidos não foram geocodificados!" -ForegroundColor Yellow
    Write-Host "🔄 Tentando reprocessamento em batch..." -ForegroundColor Yellow

    try {
        $batchResponse = Invoke-RestMethod -Uri "$BASE_URL/orders/geocode-missing?limit=50" -Method Post -Headers $headers
        Write-Host "  ✅ Reprocessamento completo!" -ForegroundColor Green
        Write-Host "     Total: $($batchResponse.total)" -ForegroundColor Gray
        Write-Host "     Atualizados: $($batchResponse.updated)" -ForegroundColor Gray
        Write-Host "     Não encontrados: $($batchResponse.notFound)" -ForegroundColor Gray
        Write-Host "     Erros: $($batchResponse.errors)" -ForegroundColor Gray
    }
    catch {
        Write-Host "  ❌ ERRO no reprocessamento batch" -ForegroundColor Red
    }
}

# ============================================
# Passo 8: Criar rota (se todos têm coords)
# ============================================
if ($pedidosComCoords -eq $pedidosIds.Count) {
    Write-Host ""
    Write-Host "🗺️  PASSO 5: Criando rota inteligente..." -ForegroundColor Yellow

    $orderGuids = $pedidosIds | ForEach-Object { $_.id }

    $routeBody = @{
        delivererId = $DELIVERER_ID
        orderIds = $orderGuids
    } | ConvertTo-Json -Depth 3

    try {
        $routeResponse = Invoke-RestMethod -Uri "$BASE_URL/delivery/routes" -Method Post -Body $routeBody -Headers $headers
        Write-Host "  ✅ Rota criada: $($routeResponse.routeNumber)" -ForegroundColor Green
        Write-Host "     Total de stops: $($routeResponse.totalStops)" -ForegroundColor Gray
        Write-Host "     Status: $($routeResponse.status)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  🔍 Verifique os LOGS do backend para ver a otimização da rota!" -ForegroundColor Cyan
    }
    catch {
        Write-Host "  ❌ ERRO ao criar rota" -ForegroundColor Red
        Write-Host "     Erro: $_" -ForegroundColor Red
    }
}
else {
    Write-Host ""
    Write-Host "⏭️  PASSO 5: Pulando criação de rota (nem todos os pedidos têm coordenadas)" -ForegroundColor Yellow
}

# ============================================
# Resumo Final
# ============================================
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ TESTE COMPLETO!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 PRÓXIMOS PASSOS:" -ForegroundColor Yellow
Write-Host "   1. Verifique os LOGS do backend (console)" -ForegroundColor White
Write-Host "   2. Procure por emojis: 📍 🌍 ✅ ❌ 🔥" -ForegroundColor White
Write-Host "   3. Valide as coordenadas no banco de dados" -ForegroundColor White
Write-Host "   4. Veja a ordem da rota otimizada nos logs" -ForegroundColor White
Write-Host ""
Write-Host "🔍 QUERIES ÚTEIS (PostgreSQL):" -ForegroundColor Yellow
Write-Host @"
   -- Ver pedidos com coordenadas
   SELECT "PublicId", "Latitude", "Longitude", "Address", "GeocodeProvider"
   FROM "Orders"
   WHERE "Latitude" IS NOT NULL
   ORDER BY "CreatedAtUtc" DESC;

   -- Ver rotas criadas
   SELECT * FROM "Routes" ORDER BY "CreatedAtUtc" DESC LIMIT 5;
"@ -ForegroundColor Gray

Write-Host ""
Write-Host "🎉 Obrigado por usar o sistema de geocoding!" -ForegroundColor Green
Write-Host ""
