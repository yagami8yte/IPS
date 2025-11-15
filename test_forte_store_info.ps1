# Forte Store Info API Test Script
# Tests the Organization and Location endpoints WITHOUT the X-Forte-Auth-Organization-Id header

# ============================================
# CONFIGURATION - Update these with your credentials
# ============================================
$apiAccessId = "03a04fee3e438b44ef168052227cf9ac"
$apiSecureKey = "c24eadad0838c40a8bb469c67d71eceb"
$orgId = "507890"
$locationId = "loc_123456"  # Update this with your actual Location ID
$useSandbox = $true  # Set to $false for production

# ============================================
# SCRIPT LOGIC - Do not modify below this line
# ============================================

# Determine base URL
if ($useSandbox) {
    $baseUrl = "https://sandbox.forte.net/api/v3"
    Write-Host "Using SANDBOX environment" -ForegroundColor Yellow
} else {
    $baseUrl = "https://api.forte.net/api/v3"
    Write-Host "Using PRODUCTION environment" -ForegroundColor Red
}

# Encode credentials
$cred = "${apiAccessId}:${apiSecureKey}"
$bytes = [Text.Encoding]::ASCII.GetBytes($cred)
$auth = [Convert]::ToBase64String($bytes)

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Forte Store Information API Test" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# ============================================
# Test 1: Organization Endpoint
# ============================================
Write-Host "[1/2] Testing Organization Endpoint..." -ForegroundColor Green
Write-Host "URL: $baseUrl/organizations/$orgId`n" -ForegroundColor Gray

try {
    $orgResponse = Invoke-WebRequest `
        -Uri "$baseUrl/organizations/$orgId" `
        -Headers @{
            'Authorization' = "Basic $auth"
            'Accept' = 'application/json'
        } `
        -Method GET `
        -ErrorAction Stop

    Write-Host "✓ SUCCESS - Status Code: $($orgResponse.StatusCode)" -ForegroundColor Green

    # Parse and display JSON
    $orgData = $orgResponse.Content | ConvertFrom-Json
    Write-Host "`nOrganization Information:" -ForegroundColor Cyan
    Write-Host "  Organization ID: $($orgData.organization_id)" -ForegroundColor White
    Write-Host "  Name: $($orgData.name)" -ForegroundColor White
    Write-Host "  DBA Name: $($orgData.dba_name)" -ForegroundColor White
    Write-Host "  Phone: $($orgData.phone)" -ForegroundColor White
    Write-Host "  Email: $($orgData.email)" -ForegroundColor White

    if ($orgData.address) {
        Write-Host "`n  Address:" -ForegroundColor Cyan
        Write-Host "    $($orgData.address.street_line1)" -ForegroundColor White
        if ($orgData.address.street_line2) {
            Write-Host "    $($orgData.address.street_line2)" -ForegroundColor White
        }
        Write-Host "    $($orgData.address.locality), $($orgData.address.region) $($orgData.address.postal_code)" -ForegroundColor White
        Write-Host "    $($orgData.address.country)" -ForegroundColor White
    }
    Write-Host ""
}
catch {
    Write-Host "✗ FAILED - Error: $($_.Exception.Message)" -ForegroundColor Red

    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
        Write-Host "  Status Code: $statusCode" -ForegroundColor Red

        # Try to read error response body
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $errorBody = $reader.ReadToEnd()
            Write-Host "  Response Body: $errorBody`n" -ForegroundColor Red
        }
        catch {
            Write-Host "  Could not read error response body`n" -ForegroundColor Red
        }
    }
}

# ============================================
# Test 2: Location Endpoint (Recommended)
# ============================================
Write-Host "[2/2] Testing Location Endpoint..." -ForegroundColor Green
Write-Host "URL: $baseUrl/organizations/$orgId/locations/$locationId`n" -ForegroundColor Gray

try {
    $locResponse = Invoke-WebRequest `
        -Uri "$baseUrl/organizations/$orgId/locations/$locationId" `
        -Headers @{
            'Authorization' = "Basic $auth"
            'Accept' = 'application/json'
        } `
        -Method GET `
        -ErrorAction Stop

    Write-Host "✓ SUCCESS - Status Code: $($locResponse.StatusCode)" -ForegroundColor Green

    # Parse and display JSON
    $locData = $locResponse.Content | ConvertFrom-Json
    Write-Host "`nLocation Information:" -ForegroundColor Cyan
    Write-Host "  Location ID: $($locData.location_id)" -ForegroundColor White
    Write-Host "  DBA Name: $($locData.dba_name)" -ForegroundColor White
    Write-Host "  Customer Service Phone: $($locData.customer_service_phone)" -ForegroundColor White
    Write-Host "  Tax ID: $($locData.tax_id)" -ForegroundColor White
    Write-Host "  Merchant Category Code: $($locData.merchant_category_code)" -ForegroundColor White

    if ($locData.address) {
        Write-Host "`n  Address:" -ForegroundColor Cyan
        Write-Host "    $($locData.address.street_line1)" -ForegroundColor White
        if ($locData.address.street_line2) {
            Write-Host "    $($locData.address.street_line2)" -ForegroundColor White
        }
        Write-Host "    $($locData.address.locality), $($locData.address.region) $($locData.address.postal_code)" -ForegroundColor White
        Write-Host "    $($locData.address.country)" -ForegroundColor White
    }
    Write-Host ""
}
catch {
    Write-Host "✗ FAILED - Error: $($_.Exception.Message)" -ForegroundColor Red

    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
        Write-Host "  Status Code: $statusCode" -ForegroundColor Red

        # Try to read error response body
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $errorBody = $reader.ReadToEnd()
            Write-Host "  Response Body: $errorBody`n" -ForegroundColor Red
        }
        catch {
            Write-Host "  Could not read error response body`n" -ForegroundColor Red
        }
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Test Complete" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
