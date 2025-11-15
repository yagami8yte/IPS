# Test Forte Credentials via REST API v3

$apiAccessId = "03a04fee3e438b44ef168052227cf9ac"
$apiSecureKey = "c24eadad0838c40a8bb469c67d71eceb"
$orgId = "507890"

Write-Host "Testing Forte Credentials..." -ForegroundColor Cyan
Write-Host "API Access ID: $apiAccessId" -ForegroundColor Yellow
Write-Host "Organization ID: $orgId" -ForegroundColor Yellow
Write-Host ""

# Create Basic Auth header
$credentials = "${apiAccessId}:${apiSecureKey}"
$bytes = [Text.Encoding]::ASCII.GetBytes($credentials)
$base64 = [Convert]::ToBase64String($bytes)

Write-Host "Authorization: Basic $base64" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-WebRequest `
        -Uri "https://sandbox.forte.net/api/v3/organizations/${orgId}/locations" `
        -Headers @{
            "Authorization" = "Basic $base64"
            "X-Forte-Auth-Organization-Id" = $orgId
        } `
        -Method GET `
        -ErrorAction Stop

    Write-Host "✅ SUCCESS! Credentials are VALID" -ForegroundColor Green
    Write-Host "HTTP Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response:" -ForegroundColor Cyan
    Write-Host $response.Content
}
catch {
    Write-Host "❌ FAILED! Credentials are INVALID" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red

    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "HTTP Status: $statusCode" -ForegroundColor Red

        if ($statusCode -eq 401) {
            Write-Host ""
            Write-Host "Authentication failed. Possible reasons:" -ForegroundColor Yellow
            Write-Host "  1. API Access ID is incorrect" -ForegroundColor Yellow
            Write-Host "  2. API Secure Key is incorrect" -ForegroundColor Yellow
            Write-Host "  3. Organization ID doesn't match the credentials" -ForegroundColor Yellow
            Write-Host "  4. Using sandbox credentials with production endpoint (or vice versa)" -ForegroundColor Yellow
        }
    }
}
