# Forte Store Information API Integration

## Overview

This document describes how to fetch merchant store information from Forte Payment Systems API to auto-populate receipt settings in the IPS application.

---

## Table of Contents

1. [API Endpoints](#api-endpoints)
2. [Authentication](#authentication)
3. [Implementation](#implementation)
4. [Data Models](#data-models)
5. [Usage in Application](#usage-in-application)
6. [Testing with External Tools](#testing-with-external-tools)
7. [Troubleshooting](#troubleshooting)

---

## API Endpoints

### Base URLs

- **Sandbox**: `https://sandbox.forte.net/api/v3`
- **Production**: `https://api.forte.net/api/v3`

### Available Endpoints

#### 1. Get Organization Information

```
GET /api/v3/organizations/{organization_id}
```

**Description**: Retrieves organization-level information including business name, phone, and address.

**Response Fields**:
- `organization_id` - Unique organization identifier
- `name` - Organization name
- `company_name` - Legal company name
- `dba_name` - "Doing Business As" name
- `phone` - Organization phone number
- `email` - Organization email
- `address` - Organization address (object)

#### 2. Get Location Information

```
GET /api/v3/organizations/{organization_id}/locations/{location_id}
```

**Description**: Retrieves location-specific information including DBA name, customer service phone, tax ID, and address. **This endpoint is preferred** as it contains more detailed merchant information.

**Response Fields**:
- `location_id` - Unique location identifier
- `dba_name` - "Doing Business As" name (recommended for receipts)
- `merchant_category_code` - MCC code
- `customer_service_phone` - Customer-facing phone number
- `tax_id` - Business Tax ID / EIN
- `address` - Location address (object)

**Address Object**:
- `street_line1` - Street address line 1
- `street_line2` - Street address line 2 (optional)
- `locality` - City
- `region` - State/Province
- `postal_code` - ZIP/Postal code
- `country` - Country code

---

## Authentication

### Method: HTTP Basic Authentication

All Forte API requests require HTTP Basic Authentication with two custom headers.

### Required Headers

```http
Authorization: Basic {base64_encoded_credentials}
X-Forte-Auth-Organization-Id: {organization_id}
Accept: application/json
```

**Important**: The `X-Forte-Auth-Organization-Id` header is **REQUIRED** and verifies the account included in the API call. This must match your Organization ID from your Forte merchant account.

### Credential Encoding

```
Credentials Format: {api_access_id}:{api_secure_key}
Encoding: Base64(api_access_id:api_secure_key)
```

### Example

```
API Access ID: 03a04fee3e438b44ef168052227cf9ac
API Secure Key: c24eadad0838c40a8bb469c67d71eceb
Encoded: MDNhMDRmZWUzZTQzOGI0NGVmMTY4MDUyMjI3Y2Y5YWM6YzI0ZWFkYWQwODM4YzQwYThiYjQ2OWM2N2Q3MWVjZWI=
```

---

## Implementation

### C# Implementation

#### Service Class: `FortePaymentService.cs`

Located at: `Services/FortePaymentService.cs`

#### Method 1: Get Organization Info

```csharp
public async Task<ForteOrganizationInfo?> GetOrganizationInfoAsync()
{
    var config = _configService.GetConfiguration();

    // Validate credentials
    if (string.IsNullOrWhiteSpace(config.ForteApiAccessId) ||
        string.IsNullOrWhiteSpace(config.ForteApiSecureKey) ||
        string.IsNullOrWhiteSpace(config.ForteOrganizationId))
    {
        Console.WriteLine("[FortePaymentService] Credentials not configured");
        return null;
    }

    // Build endpoint URL
    var baseUrl = config.ForteSandboxMode ? SANDBOX_BASE_URL : PRODUCTION_BASE_URL;
    var endpoint = $"{baseUrl}/organizations/{config.ForteOrganizationId}";

    // Create HTTP request with Basic Auth
    var httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
    var authBytes = Encoding.UTF8.GetBytes($"{config.ForteApiAccessId}:{config.ForteApiSecureKey}");
    var authBase64 = Convert.ToBase64String(authBytes);
    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);
    httpRequest.Headers.Add("X-Forte-Auth-Organization-Id", config.ForteOrganizationId);
    httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    // Send request
    var response = await _httpClient.SendAsync(httpRequest);
    var responseBody = await response.Content.ReadAsStringAsync();

    // Parse response
    if (response.IsSuccessStatusCode)
    {
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var orgInfo = JsonSerializer.Deserialize<ForteOrganizationInfo>(responseBody, jsonOptions);
        return orgInfo;
    }

    return null;
}
```

#### Method 2: Get Location Info (Recommended)

```csharp
public async Task<ForteLocationInfo?> GetLocationInfoAsync()
{
    var config = _configService.GetConfiguration();

    // Validate credentials + location ID
    if (string.IsNullOrWhiteSpace(config.ForteApiAccessId) ||
        string.IsNullOrWhiteSpace(config.ForteApiSecureKey) ||
        string.IsNullOrWhiteSpace(config.ForteOrganizationId) ||
        string.IsNullOrWhiteSpace(config.ForteLocationId))
    {
        Console.WriteLine("[FortePaymentService] Credentials or Location ID not configured");
        return null;
    }

    // Build endpoint URL
    var baseUrl = config.ForteSandboxMode ? SANDBOX_BASE_URL : PRODUCTION_BASE_URL;
    var endpoint = $"{baseUrl}/organizations/{config.ForteOrganizationId}/locations/{config.ForteLocationId}";

    // Create HTTP request with Basic Auth
    var httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
    var authBytes = Encoding.UTF8.GetBytes($"{config.ForteApiAccessId}:{config.ForteApiSecureKey}");
    var authBase64 = Convert.ToBase64String(authBytes);
    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);
    httpRequest.Headers.Add("X-Forte-Auth-Organization-Id", config.ForteOrganizationId);
    httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    // Send request
    var response = await _httpClient.SendAsync(httpRequest);
    var responseBody = await response.Content.ReadAsStringAsync();

    // Parse response
    if (response.IsSuccessStatusCode)
    {
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var locationInfo = JsonSerializer.Deserialize<ForteLocationInfo>(responseBody, jsonOptions);
        return locationInfo;
    }

    return null;
}
```

---

## Data Models

### ForteOrganizationInfo

Located at: `Services/FortePaymentModels.cs:174`

```csharp
public class ForteOrganizationInfo
{
    [JsonPropertyName("organization_id")]
    public string OrganizationId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string OrganizationName { get; set; } = string.Empty;

    [JsonPropertyName("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [JsonPropertyName("dba_name")]
    public string DbaName { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public ForteAddress? Address { get; set; }
}
```

### ForteLocationInfo

Located at: `Services/FortePaymentModels.cs:201`

```csharp
public class ForteLocationInfo
{
    [JsonPropertyName("location_id")]
    public string LocationId { get; set; } = string.Empty;

    [JsonPropertyName("dba_name")]
    public string DbaName { get; set; } = string.Empty;

    [JsonPropertyName("merchant_category_code")]
    public string MerchantCategoryCode { get; set; } = string.Empty;

    [JsonPropertyName("customer_service_phone")]
    public string CustomerServicePhone { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public ForteAddress? Address { get; set; }

    [JsonPropertyName("tax_id")]
    public string TaxId { get; set; } = string.Empty;
}
```

### ForteAddress

Located at: `Services/FortePaymentModels.cs:225`

```csharp
public class ForteAddress
{
    [JsonPropertyName("street_line1")]
    public string StreetLine1 { get; set; } = string.Empty;

    [JsonPropertyName("street_line2")]
    public string? StreetLine2 { get; set; }

    [JsonPropertyName("locality")]
    public string Locality { get; set; } = string.Empty;

    [JsonPropertyName("region")]
    public string Region { get; set; } = string.Empty;

    [JsonPropertyName("postal_code")]
    public string PostalCode { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;
}
```

---

## Usage in Application

### Admin UI Integration

**Location**: Admin Page → Systems Section → Business Information

**File**: `MainApp/ViewModels/AdminViewModel.cs:1626`

### "Fetch from Forte" Button Workflow

1. **User Action**: Click "Fetch from Forte" button in Admin → Systems → Business Information section
2. **Service Call**: `OnFetchFromForteAsync()` method is invoked
3. **API Request**: Calls `FortePaymentService.GetLocationInfoAsync()`
4. **Data Mapping**: Location info is mapped to receipt settings:

```csharp
private async Task OnFetchFromForteAsync()
{
    IsFetchingFromForte = true;
    ForteStoreInfoStatus = "Fetching store information from Forte...";

    var forteService = new FortePaymentService(_configService);
    var locationInfo = await forteService.GetLocationInfoAsync();

    if (locationInfo != null)
    {
        // Map business name
        BusinessName = locationInfo.DbaName;

        // Map phone
        BusinessPhone = locationInfo.CustomerServicePhone;

        // Map Tax ID
        BusinessTaxId = locationInfo.TaxId;

        // Map address
        if (locationInfo.Address != null)
        {
            var addr = locationInfo.Address;

            // Address Line 1: Street address
            if (!string.IsNullOrWhiteSpace(addr.StreetLine2))
                BusinessAddressLine1 = $"{addr.StreetLine1}, {addr.StreetLine2}";
            else
                BusinessAddressLine1 = addr.StreetLine1;

            // Address Line 2: City, State ZIP
            BusinessAddressLine2 = $"{addr.Locality}, {addr.Region} {addr.PostalCode}";
        }

        ForteStoreInfoStatus = $"✅ Successfully loaded: {locationInfo.DbaName}";
    }
    else
    {
        ForteStoreInfoStatus = "❌ Failed to fetch store information. Check credentials.";
    }

    IsFetchingFromForte = false;
}
```

### Configuration Fields Populated

| Forte Field | IPS Receipt Setting | Example |
|-------------|---------------------|---------|
| `dba_name` | Business Name | "Joe's Coffee Shop" |
| `customer_service_phone` | Business Phone | "(555) 123-4567" |
| `tax_id` | Business Tax ID | "12-3456789" |
| `address.street_line1` + `street_line2` | Business Address Line 1 | "123 Main St, Suite 100" |
| `address.locality` + `region` + `postal_code` | Business Address Line 2 | "Seattle, WA 98101" |

---

## Testing with External Tools

### Using PowerShell

```powershell
# Set credentials
$apiAccessId = "03a04fee3e438b44ef168052227cf9ac"
$apiSecureKey = "c24eadad0838c40a8bb469c67d71eceb"
$orgId = "507890"
$locationId = "loc_123456"

# Encode credentials
$cred = "${apiAccessId}:${apiSecureKey}"
$bytes = [Text.Encoding]::ASCII.GetBytes($cred)
$auth = [Convert]::ToBase64String($bytes)

# Test Organization endpoint
Invoke-WebRequest `
    -Uri "https://sandbox.forte.net/api/v3/organizations/$orgId" `
    -Headers @{
        'Authorization' = "Basic $auth"
        'X-Forte-Auth-Organization-Id' = $orgId
        'Accept' = 'application/json'
    } `
    -Method GET

# Test Location endpoint (Recommended)
Invoke-WebRequest `
    -Uri "https://sandbox.forte.net/api/v3/organizations/$orgId/locations/$locationId" `
    -Headers @{
        'Authorization' = "Basic $auth"
        'X-Forte-Auth-Organization-Id' = $orgId
        'Accept' = 'application/json'
    } `
    -Method GET
```

### Using cURL

```bash
# Set variables
API_ACCESS_ID="03a04fee3e438b44ef168052227cf9ac"
API_SECURE_KEY="c24eadad0838c40a8bb469c67d71eceb"
ORG_ID="507890"
LOCATION_ID="loc_123456"

# Encode credentials (Linux/Mac)
AUTH=$(echo -n "$API_ACCESS_ID:$API_SECURE_KEY" | base64)

# Test Organization endpoint
curl -X GET \
  "https://sandbox.forte.net/api/v3/organizations/$ORG_ID" \
  -H "Authorization: Basic $AUTH" \
  -H "X-Forte-Auth-Organization-Id: $ORG_ID" \
  -H "Accept: application/json"

# Test Location endpoint (Recommended)
curl -X GET \
  "https://sandbox.forte.net/api/v3/organizations/$ORG_ID/locations/$LOCATION_ID" \
  -H "Authorization: Basic $AUTH" \
  -H "X-Forte-Auth-Organization-Id: $ORG_ID" \
  -H "Accept: application/json"
```

### Using Postman

1. **Create New Request**:
   - Method: `GET`
   - URL: `https://sandbox.forte.net/api/v3/organizations/{organization_id}/locations/{location_id}`

2. **Headers**:
   ```
   X-Forte-Auth-Organization-Id: 507890
   Accept: application/json
   ```

3. **Authorization**:
   - Type: `Basic Auth`
   - Username: `03a04fee3e438b44ef168052227cf9ac` (API Access ID)
   - Password: `c24eadad0838c40a8bb469c67d71eceb` (API Secure Key)

4. **Send Request**

### Using Insomnia

1. **Create New Request**:
   - Method: `GET`
   - URL: `https://sandbox.forte.net/api/v3/organizations/{organization_id}/locations/{location_id}`

2. **Auth Tab**:
   - Type: `Basic Auth`
   - Username: Your API Access ID
   - Password: Your API Secure Key

3. **Header Tab**:
   ```
   X-Forte-Auth-Organization-Id: {organization_id}
   Accept: application/json
   ```

4. **Send Request**

---

## Troubleshooting

### Common Errors

#### 401 Unauthorized

**Cause**: Invalid credentials or missing authentication header

**Solution**:
- Verify API Access ID and API Secure Key are correct
- Ensure credentials are properly Base64 encoded (format: `api_access_id:api_secure_key`)
- Check that `X-Forte-Auth-Organization-Id` header is included and matches your Organization ID
- Verify you're using the correct environment (Sandbox vs Production)

#### 403 Forbidden

**Cause**: Valid credentials but insufficient permissions

**Solution**:
- Verify your API credentials have permission to access the Organization/Location API
- Contact Forte support to enable API access for your account

#### 404 Not Found

**Cause**: Organization ID or Location ID doesn't exist

**Solution**:
- Verify Organization ID is correct (format: numeric, e.g., "507890")
- Verify Location ID is correct (format: "loc_XXXXXX")
- Check you're using the correct environment (Sandbox vs Production)

#### Null or Empty Response

**Cause**: Missing required configuration in application

**Solution**:
- Navigate to Admin → Payment Settings
- Verify the following are configured:
  - API Access ID
  - API Secure Key
  - Organization ID
  - Location ID (for location endpoint)
- Check console logs for error messages

### Debugging Tips

1. **Enable Console Logging**: Check the application console for detailed API request/response logs
2. **Test Credentials Externally**: Use PowerShell/cURL to verify credentials work outside the application
3. **Check Sandbox Mode**: Ensure `ForteSandboxMode` setting matches your credentials
4. **Verify JSON Deserialization**: Check that response JSON structure matches the C# models

---

## Security Notes

### PCI DSS Compliance

- ✅ **Credentials Storage**: API credentials are stored in local configuration file only
- ✅ **No Sensitive Data in Logs**: Only organization/location info is logged, no payment card data
- ✅ **HTTPS Only**: All API calls use HTTPS (enforced by Forte)
- ✅ **Minimal Data Exposure**: Only business information is fetched, no customer payment data

### Best Practices

1. **Protect API Credentials**: Never commit credentials to version control
2. **Use Environment-Specific Keys**: Separate Sandbox and Production credentials
3. **Rotate Keys Regularly**: Update API credentials periodically
4. **Monitor API Usage**: Check Forte dashboard for unexpected API calls
5. **Validate Input**: Always validate and sanitize data from API responses before display

---

## References

- **Forte API Documentation**: https://restdocs.forte.net/
- **Forte Developer Portal**: https://developers.forte.net/
- **Forte Support**: https://support.forte.net/

---

## Related Files

### Implementation Files

- `Services/FortePaymentService.cs` - API service implementation
- `Services/FortePaymentModels.cs` - Response data models
- `MainApp/ViewModels/AdminViewModel.cs` - UI integration
- `MainApp/Views/AdminView.xaml` - "Fetch from Forte" button UI
- `Core/Models/SystemConfiguration.cs` - Receipt settings storage

### Configuration Files

- `appsettings.json` - Forte API credentials (if used)
- Local configuration file - Stores fetched business information

---

**Last Updated**: 2025-01-15
**Version**: 1.0
**Author**: IPS Development Team
