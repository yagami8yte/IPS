# Test Forte GET Organization API

$apiAccessId = "03a04fee3e438b44ef168052227cf9ac"
$apiSecureKey = "c24eadad0838c40a8bb469c67d71eceb"
$orgId = "507890"

# Create Basic Auth header
$cred = "${apiAccessId}:${apiSecureKey}"
$bytes = [Text.Encoding]::ASCII.GetBytes($cred)
$auth = [Convert]::ToBase64String($bytes)

Write-Host "Testing GET Organization endpoint..." -ForegroundColor Cyan
Write-Host "URL: https://sandbox.forte.net/api/v3/organizations/$orgId" -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest `
        -Uri "https://sandbox.forte.net/api/v3/organizations/$orgId" `
        -Headers @{
            'Authorization' = "Basic $auth"
            'X-Forte-Auth-Organization-Id' = $orgId
            'Accept' = 'application/json'
        } `
        -Method GET `
        -UseBasicParsing

    Write-Host "`nStatus: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "`nResponse Body:" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
}
catch {
    Write-Host "`nError: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
}

Write-Host "`n---" -ForegroundColor Gray

# Test GET Locations endpoint
Write-Host "`nTesting GET Locations endpoint..." -ForegroundColor Cyan
Write-Host "URL: https://sandbox.forte.net/api/v3/organizations/$orgId/locations" -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest `
        -Uri "https://sandbox.forte.net/api/v3/organizations/$orgId/locations" `
        -Headers @{
            'Authorization' = "Basic $auth"
            'X-Forte-Auth-Organization-Id' = $orgId
            'Accept' = 'application/json'
        } `
        -Method GET `
        -UseBasicParsing

    Write-Host "`nStatus: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "`nResponse Body:" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
}
catch {
    Write-Host "`nError: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Red
    }
}
