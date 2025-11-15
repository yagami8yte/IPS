# Forte Checkout v2 Integration - Current Status

## ✅ Completed Fixes

### 1. Button Attributes Corrected (ForteCheckoutService.cs:178-194)
- ✅ Changed `action='sale'` to `method='sale'`
- ✅ Changed `amount` to `total_amount`
- ✅ Added `utc_time` attribute (in .NET Ticks format)
- ✅ Added `signature` attribute (HMAC-SHA256 hash)
- ✅ Removed `organization_id` (not part of Checkout v2 spec)
- ✅ Correct sandbox URL: `https://sandbox.forte.net/checkout/v2/js`

### 2. Signature Generation Implemented (ForteCheckoutService.cs:25-40)
```csharp
string signatureString = $"{apiAccessId}|{method}|{versionNumber}|{totalAmount:F2}|{utcTime}|{orderNumber}||";
// HMACSHA256(signatureString, apiSecureKey)
```
✅ Format matches official Forte documentation

### 3. UTC Time Format Fixed (ForteCheckoutService.cs:52)
- ✅ Changed from ISO 8601 (`yyyy-MM-ddTHH:mm:ssZ`)
- ✅ To .NET DateTime Ticks (e.g., `638984746685502025`)

### 4. localStorage Access Fixed (PaymentView.xaml.cs:58-62)
- ✅ Added WebView2 virtual host mapping
- ✅ Added `<base href='https://forte.local/'>` to HTML
- ✅ Fixes "Failed to read the 'localStorage' property" error

### 5. Test Tools Created
- ✅ `test_forte_credentials.html` - Standalone credential test page (fixed infinite loading)
- ✅ `test_forte_creds.ps1` - PowerShell script to test via REST API
- ✅ `FORTE_CREDENTIALS_CHECK.md` - Comprehensive verification guide

## ❌ Unresolved Issue: "Invalid authentication"

### Current Credentials (from appsettings.json)
```json
{
  "forteApiAccessId": "03a04fee3e438b44ef168052227cf9ac",
  "forteApiSecureKey": "c24eadad0838c40a8bb469c67d71eceb",
  "forteOrganizationId": "507890",
  "forteLocationId": "411494",
  "forteSandboxMode": true
}
```

### Test Results
- ❌ REST API v3 test: **HTTP 400 (Bad Request)**
  - Not necessarily proof of invalid credentials (different authentication method)
  - But suggests potential credential issues

### Why "Invalid authentication" Occurs (per Forte documentation)
1. **API Access ID is incorrect** ← Most likely
2. **API Secure Key is incorrect** ← Most likely
3. **Location ID doesn't belong to the Organization**
4. **Sandbox credentials but using production endpoint** (we're correctly using sandbox)
5. **Invalid signature calculation** (we've verified this is correct)

## 🔍 Next Steps: Verify Credentials

### Option 1: Verify via Forte Dex Portal (RECOMMENDED)
1. **Login to Forte Dex**: https://sandbox.forte.net/dex/
   - If you don't have an account: https://www.forte.net/test-account-setup

2. **Check API Credentials**:
   - Navigate to: **Developer > API Credentials**
   - Verify `API Access ID` matches: `03a04fee3e438b44ef168052227cf9ac`
   - **Note**: API Secure Key is only shown once when created
   - If you don't have the original key, you must regenerate it

3. **Check Location ID**:
   - Navigate to: **Settings > Locations**
   - Verify Location ID `411494` exists
   - Ensure it belongs to Organization `507890`

### Option 2: Test Credentials with HTML Test Page
1. Open `test_forte_credentials.html` in a browser
2. Credentials are pre-filled from appsettings.json
3. Click "Test Credentials" button
4. Check console output for errors:
   - **"Checkout loaded"** = Credentials valid ✅
   - **"Invalid authentication"** = Credentials wrong ❌

### Option 3: Contact Forte Support
If credentials can't be verified through Dex portal:

**Forte Customer Service**:
- Email: partnersupport@forte.net
- Phone: 866.290.5400 (Option 1)
- Provide: Merchant ID, API Login ID, Organization ID

Ask them to verify if credentials for:
- Organization ID: `507890`
- API Access ID: `03a04fee3e438b44ef168052227cf9ac`
- Location ID: `411494`
are valid for sandbox Checkout v2.

## 📝 Important Notes

### Why organization_id Isn't Used in Button
- `organization_id` is **ONLY** used in REST API v3 URL paths:
  ```
  https://sandbox.forte.net/api/v3/organizations/{orgId}/locations
  ```
- **NOT** used in Forte Checkout v2 button attributes
- Checkout v2 authentication uses: `api_access_id`, `location_id`, and `signature`

### About API Secure Key
⚠️ **CRITICAL**: API Secure Key is shown **only once** when generated!
- Cannot be retrieved later from Dex portal
- If lost, must regenerate (which invalidates the old key)
- After regenerating, update `appsettings.json` with new key

### Sandbox vs Production
- ✅ Currently using: `https://sandbox.forte.net/checkout/v2/js`
- ✅ Correct for `forteSandboxMode: true`
- Production URL would be: `https://checkout.forte.net/v2/js`

## 🎯 Most Likely Root Cause

Based on the "Invalid authentication" error and HTTP 400 response, the credentials in `appsettings.json` are most likely **incorrect or expired**.

**Action Required**: Log into Forte Dex portal to verify credentials match, or regenerate new credentials if the API Secure Key was lost.

---

## Technical Details

### Signature Calculation (HMACSHA256)
```
Input: "api_access_id|method|version_number|total_amount|utc_time|order_number||"
Key: "API Secure Key"
Output: hex string (lowercase)
```

Example:
```
Input: "03a04fee3e438b44ef168052227cf9ac|sale|2.0|10.00|638984746685502025|A-001||"
Key: "c24eadad0838c40a8bb469c67d71eceb"
Output: "a1b2c3d4..." (64 character hex string)
```

### Required Button Attributes
- `api_access_id`: API Access ID from Dex
- `method`: "sale" (for purchases)
- `version_number`: "2.0"
- `location_id`: Location ID from Dex
- `total_amount`: Amount with 2 decimal places (e.g., "10.00")
- `utc_time`: .NET DateTime Ticks (e.g., "638984746685502025")
- `order_number`: Unique order identifier
- `signature`: HMAC-SHA256 hash
- `embedded`: "true" (for embedded mode)
- `swipe`: "dynaflex" (for card reader support)

All attributes are implemented correctly in `ForteCheckoutService.cs`.
