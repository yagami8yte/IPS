$apiAccessId = "03a04fee3e438b44ef168052227cf9ac"
$apiSecureKey = "c24eadad0838c40a8bb469c67d71eceb"
$orgId = "507890"

$cred = "${apiAccessId}:${apiSecureKey}"
$auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($cred))

Write-Host "Testing Forte API..." -ForegroundColor Cyan
Write-Host "URL: https://sandbox.forte.net/api/v3/organizations/$orgId"
Write-Host "Auth: Basic $auth"
Write-Host ""

try {
    $response = Invoke-WebRequest `
        -Uri "https://sandbox.forte.net/api/v3/organizations/$orgId" `
        -Headers @{
            'Authorization' = "Basic $auth"
            'X-Forte-Auth-Organization-Id' = "org_$orgId"
            'Accept' = 'application/json'
        } `
        -Method GET `
        -ErrorAction Stop

    Write-Host "SUCCESS!" -ForegroundColor Green
    Write-Host "Status Code: $($response.StatusCode)"
    Write-Host ""
    Write-Host "Response Body:" -ForegroundColor Yellow
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
}
catch {
    Write-Host "FAILED!" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)"

    try {
        $result = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($result)
        $responseBody = $reader.ReadToEnd()
        Write-Host ""
        Write-Host "Error Response:" -ForegroundColor Yellow
        Write-Host $responseBody
    }
    catch {
        Write-Host "Could not read error response"
    }
}
