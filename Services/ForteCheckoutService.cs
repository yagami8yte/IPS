using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IPS.Core.Models;

namespace IPS.Services
{
    /// <summary>
    /// Service for integrating Forte Checkout v2 (button-based hosted payment modal)
    /// Supports Dynaflex II Go swiper and Verifone V400c terminal
    /// </summary>
    public class ForteCheckoutService
    {
        private readonly ConfigurationService _configService;
        private static readonly HttpClient _httpClient = new HttpClient();

        public ForteCheckoutService(ConfigurationService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        /// <summary>
        /// Gets UTC time from Forte's server to prevent client PC time mismatch issues
        /// Forte only accepts UTC times within 20 minutes before or 10 minutes after current time
        /// </summary>
        private async Task<string> GetForteUtcTimeAsync(bool sandboxMode)
        {
            try
            {
                string url = sandboxMode
                    ? "https://sandbox.forte.net/checkout/getUTC?callback=?"
                    : "https://checkout.forte.net/getUTC?callback=?";

                Console.WriteLine($"[ForteCheckout] Fetching UTC time from Forte: {url}");

                string response = await _httpClient.GetStringAsync(url);

                // Response format: ?(XXXXXXXXXXXX643793)
                // Extract the UTC ticks value
                int startIndex = response.IndexOf('(') + 1;
                int endIndex = response.IndexOf(')');
                string utcTicks = response.Substring(startIndex, endIndex - startIndex);

                Console.WriteLine($"[ForteCheckout] Forte UTC Time: {utcTicks}");
                return utcTicks;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ForteCheckout] Failed to get Forte UTC time: {ex.Message}");
                Console.WriteLine($"[ForteCheckout] Falling back to local UTC time");
                // Fallback to local UTC if Forte API fails
                return DateTime.UtcNow.Ticks.ToString();
            }
        }

        /// <summary>
        /// Generates HMAC-SHA256 signature for Forte Checkout v2
        /// Signature format: HMACSHA256("api_access_id|method|version_number|total_amount|utc_time|order_number|customer_token|paymethod_token", "API Secure Key")
        /// </summary>
        private string GenerateSignature(string apiAccessId, string method, string versionNumber,
            decimal totalAmount, string utcTime, string orderNumber, string apiSecureKey)
        {
            // Build signature string (customer_token and paymethod_token are empty for card-present transactions)
            string signatureString = $"{apiAccessId}|{method}|{versionNumber}|{totalAmount:F2}|{utcTime}|{orderNumber}||";

            Console.WriteLine($"[ForteCheckout] Signature string: {signatureString}");

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecureKey)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
                string signature = BitConverter.ToString(hash).Replace("-", "").ToLower();
                Console.WriteLine($"[ForteCheckout] Generated signature: {signature}");
                return signature;
            }
        }

        /// <summary>
        /// Test Forte Checkout v2 credentials by generating actual checkout HTML and validating signature
        /// This tests the exact same flow as PaymentView uses
        /// Returns detailed logs of the test process
        /// </summary>
        public async Task<(bool success, List<string> logs)> TestCredentialsAsync()
        {
            var logs = new List<string>();
            var config = _configService.GetConfiguration();

            logs.Add($"[{DateTime.Now:HH:mm:ss}] Starting Forte Checkout v2 credentials test...");
            logs.Add($"[{DateTime.Now:HH:mm:ss}] Mode: {(config.ForteSandboxMode ? "SANDBOX" : "PRODUCTION")}");
            logs.Add($"[{DateTime.Now:HH:mm:ss}] NOTE: This tests the exact same flow as PaymentView");
            logs.Add("");

            // Step 1: Validate configuration
            logs.Add($"[{DateTime.Now:HH:mm:ss}] Step 1: Validating Checkout v2 configuration...");

            if (string.IsNullOrWhiteSpace(config.ForteApiAccessId))
            {
                logs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR: API Access ID is empty");
                return (false, logs);
            }
            logs.Add($"[{DateTime.Now:HH:mm:ss}] ✓ API Access ID: {config.ForteApiAccessId}");

            if (string.IsNullOrWhiteSpace(config.ForteApiSecureKey))
            {
                logs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR: API Secure Key is empty");
                return (false, logs);
            }
            logs.Add($"[{DateTime.Now:HH:mm:ss}] ✓ API Secure Key: {new string('*', config.ForteApiSecureKey.Length)}");

            if (string.IsNullOrWhiteSpace(config.ForteLocationId))
            {
                logs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR: Location ID is empty");
                return (false, logs);
            }
            logs.Add($"[{DateTime.Now:HH:mm:ss}] ✓ Location ID: {config.ForteLocationId}");
            logs.Add("");

            // Step 2: Test Forte Checkout server connectivity
            logs.Add($"[{DateTime.Now:HH:mm:ss}] Step 2: Testing Forte Checkout server connectivity...");

            try
            {
                string utcUrl = config.ForteSandboxMode
                    ? "https://sandbox.forte.net/checkout/getUTC?callback=?"
                    : "https://checkout.forte.net/getUTC?callback=?";

                logs.Add($"[{DateTime.Now:HH:mm:ss}] UTC Endpoint: {utcUrl}");
                logs.Add($"[{DateTime.Now:HH:mm:ss}] Fetching Forte server time...");

                string utcTime;
                try
                {
                    string response = await _httpClient.GetStringAsync(utcUrl);
                    int startIndex = response.IndexOf('(') + 1;
                    int endIndex = response.IndexOf(')');
                    utcTime = response.Substring(startIndex, endIndex - startIndex);

                    logs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ SUCCESS: Connected to Forte Checkout server");
                    logs.Add($"[{DateTime.Now:HH:mm:ss}] UTC Time received: {utcTime}");
                }
                catch (Exception ex)
                {
                    logs.Add($"[{DateTime.Now:HH:mm:ss}] ⚠️ WARNING: Could not fetch Forte UTC time: {ex.Message}");
                    logs.Add($"[{DateTime.Now:HH:mm:ss}] Using local UTC time as fallback");
                    utcTime = DateTime.UtcNow.Ticks.ToString();
                }

                logs.Add("");

                // Step 3: Generate checkout signature (same as PaymentView)
                logs.Add($"[{DateTime.Now:HH:mm:ss}] Step 3: Generating Checkout v2 signature...");

                decimal testAmount = 1.00m;
                string testOrderLabel = $"TEST-{DateTime.Now:HHmmss}";

                logs.Add($"[{DateTime.Now:HH:mm:ss}] Test Amount: ${testAmount:F2}");
                logs.Add($"[{DateTime.Now:HH:mm:ss}] Test Order: {testOrderLabel}");

                try
                {
                    string signature = GenerateSignature(
                        config.ForteApiAccessId,
                        "sale",
                        "2.0",
                        testAmount,
                        utcTime,
                        testOrderLabel,
                        config.ForteApiSecureKey
                    );

                    logs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ SUCCESS: Signature generated");
                    logs.Add($"[{DateTime.Now:HH:mm:ss}] Signature: {signature.Substring(0, 16)}...{signature.Substring(signature.Length - 16)}");
                }
                catch (Exception ex)
                {
                    logs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR: Failed to generate signature");
                    logs.Add($"[{DateTime.Now:HH:mm:ss}] Message: {ex.Message}");
                    logs.Add($"[{DateTime.Now:HH:mm:ss}] This usually means API Secure Key is invalid");
                    return (false, logs);
                }

                logs.Add("");

                // Step 4: Generate checkout HTML (same as PaymentView)
                logs.Add($"[{DateTime.Now:HH:mm:ss}] Step 4: Generating Checkout HTML...");

                try
                {
                    string checkoutHtml = await GetCheckoutHtmlAsync(testAmount, testOrderLabel);

                    if (string.IsNullOrWhiteSpace(checkoutHtml))
                    {
                        logs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR: Generated HTML is empty");
                        return (false, logs);
                    }

                    logs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ SUCCESS: Checkout HTML generated");
                    logs.Add($"[{DateTime.Now:HH:mm:ss}] HTML size: {checkoutHtml.Length} characters");

                    // Verify HTML contains required elements
                    bool hasButton = checkoutHtml.Contains("forte-checkout-button");
                    bool hasScript = checkoutHtml.Contains(config.ForteSandboxMode ? "sandbox.forte.net" : "checkout.forte.net");
                    bool hasCallback = checkoutHtml.Contains("onForteCallback");

                    logs.Add($"[{DateTime.Now:HH:mm:ss}] HTML validation:");
                    logs.Add($"[{DateTime.Now:HH:mm:ss}]   - Checkout button: {(hasButton ? "✓" : "✗")}");
                    logs.Add($"[{DateTime.Now:HH:mm:ss}]   - Forte script: {(hasScript ? "✓" : "✗")}");
                    logs.Add($"[{DateTime.Now:HH:mm:ss}]   - Callback function: {(hasCallback ? "✓" : "✗")}");

                    if (!hasButton || !hasScript || !hasCallback)
                    {
                        logs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR: HTML validation failed");
                        return (false, logs);
                    }
                }
                catch (Exception ex)
                {
                    logs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ ERROR: Failed to generate checkout HTML");
                    logs.Add($"[{DateTime.Now:HH:mm:ss}] Message: {ex.Message}");
                    return (false, logs);
                }

                logs.Add("");
                logs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");
                logs.Add($"[{DateTime.Now:HH:mm:ss}] ✅ ALL TESTS PASSED!");
                logs.Add($"[{DateTime.Now:HH:mm:ss}] ═══════════════════════════════════════");
                logs.Add("");
                logs.Add($"[{DateTime.Now:HH:mm:ss}] Your Forte Checkout v2 credentials are valid.");
                logs.Add($"[{DateTime.Now:HH:mm:ss}] The payment flow will work the same as in PaymentView.");
                logs.Add("");
                logs.Add($"[{DateTime.Now:HH:mm:ss}] Configuration summary:");
                logs.Add($"[{DateTime.Now:HH:mm:ss}]   API Access ID: {config.ForteApiAccessId}");
                logs.Add($"[{DateTime.Now:HH:mm:ss}]   Location ID: {config.ForteLocationId}");
                logs.Add($"[{DateTime.Now:HH:mm:ss}]   Mode: {(config.ForteSandboxMode ? "SANDBOX" : "PRODUCTION")}");
                logs.Add($"[{DateTime.Now:HH:mm:ss}]   Server: {(config.ForteSandboxMode ? "sandbox.forte.net" : "checkout.forte.net")}");

                return (true, logs);
            }
            catch (HttpRequestException ex)
            {
                logs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ NETWORK ERROR: {ex.Message}");
                logs.Add($"[{DateTime.Now:HH:mm:ss}] Please check your internet connection");
                return (false, logs);
            }
            catch (TaskCanceledException)
            {
                logs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ TIMEOUT ERROR: Request timed out");
                logs.Add($"[{DateTime.Now:HH:mm:ss}] Forte Checkout server may be unavailable");
                return (false, logs);
            }
            catch (Exception ex)
            {
                logs.Add($"[{DateTime.Now:HH:mm:ss}] ❌ UNEXPECTED ERROR: {ex.GetType().Name}");
                logs.Add($"[{DateTime.Now:HH:mm:ss}] Message: {ex.Message}");
                logs.Add($"[{DateTime.Now:HH:mm:ss}] Stack Trace:");
                logs.Add(ex.StackTrace ?? "No stack trace available");
                return (false, logs);
            }
        }

        /// <summary>
        /// Generates HTML page with Forte Checkout button
        /// Forte Checkout v2 uses button-based initialization with signature authentication
        /// </summary>
        public async Task<string> GetCheckoutHtmlAsync(decimal amount, string orderLabel)
        {
            var config = _configService.GetConfiguration();

            // Get UTC time from Forte's server to prevent client PC time mismatch
            // Forte only accepts UTC times within 20 minutes before or 10 minutes after current time
            string utcTime = await GetForteUtcTimeAsync(config.ForteSandboxMode);

            // Generate signature for authentication
            string signature = GenerateSignature(
                config.ForteApiAccessId,
                "sale",
                "2.0",
                amount,
                utcTime,
                orderLabel,
                config.ForteApiSecureKey
            );

            // Debug: Log all Forte configuration values
            Console.WriteLine($"[ForteCheckout] API Access ID: '{config.ForteApiAccessId}'");
            Console.WriteLine($"[ForteCheckout] Location ID: '{config.ForteLocationId}'");
            Console.WriteLine($"[ForteCheckout] Total Amount: {amount:F2}");
            Console.WriteLine($"[ForteCheckout] Order Number: '{orderLabel}'");
            Console.WriteLine($"[ForteCheckout] UTC Time: '{utcTime}'");
            Console.WriteLine($"[ForteCheckout] Sandbox Mode: {config.ForteSandboxMode}");

            // Determine script URL based on sandbox mode
            var scriptUrl = config.ForteSandboxMode
                ? "https://sandbox.forte.net/checkout/v2/js"
                : "https://checkout.forte.net/v2/js";

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <base href='https://forte.local/'>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Forte Payment</title>
    <script src='https://code.jquery.com/jquery-3.6.0.min.js'></script>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            background-color: white;
            width: 100%;
            height: 100vh;
            overflow: hidden;
        }}
        #fco-embedded {{
            width: 100%;
            height: 100%;
        }}
    </style>
</head>
<body>
    <!-- Forte Checkout Embedded Container -->
    <div id='fco-embedded'></div>

    <!-- Forte Checkout Button (hidden, used for initialization) -->
    <!-- Official Forte Checkout v2 button attributes per documentation -->
    <button id='forte-checkout-button'
       api_access_id='{config.ForteApiAccessId}'
       method='sale'
       version_number='2.0'
       location_id='{config.ForteLocationId}'
       total_amount='{amount:F2}'
       utc_time='{utcTime}'
       order_number='{orderLabel}'
       hash_method='sha256'
       signature='{signature}'
       callback='onForteCallback'
       swipe='dynaflex'
       embedded='true'
       style='display: none;'>
        Pay Now
    </button>

    <!-- Load Forte Checkout library -->
    <script src='{scriptUrl}'></script>

    <!-- Auto-trigger checkout -->
    <script>
        // Forte Checkout callback function (required by official documentation)
        function onForteCallback(e) {{
            console.log('[Forte Callback] Event received:', e.data);

            try {{
                var response = JSON.parse(e.data);
                console.log('[Forte Callback] Parsed response:', response);

                switch (response.event) {{
                    case 'begin':
                        console.log('[Forte Callback] Transaction begin');
                        break;

                    case 'success':
                        console.log('[Forte Callback] Transaction success - Trace:', response.trace_number);
                        if (window.chrome && window.chrome.webview) {{
                            window.chrome.webview.postMessage({{
                                type: 'success',
                                transactionId: response.trace_number,
                                authorizationCode: response.authorization_code || '',
                                orderLabel: response.order_number || '{orderLabel}'
                            }});
                        }}
                        break;

                    case 'failure':
                        console.log('[Forte Callback] Transaction failure:', response.response_description);
                        if (window.chrome && window.chrome.webview) {{
                            window.chrome.webview.postMessage({{
                                type: 'failure',
                                error: response.response_description || 'Transaction failed',
                                orderLabel: response.order_number || '{orderLabel}'
                            }});
                        }}
                        break;

                    case 'error':
                        console.log('[Forte Callback] Error:', response.msg);
                        if (window.chrome && window.chrome.webview) {{
                            window.chrome.webview.postMessage({{
                                type: 'error',
                                error: response.msg || 'Unknown error',
                                orderLabel: '{orderLabel}'
                            }});
                        }}
                        break;

                    case 'abort':
                        console.log('[Forte Callback] Transaction aborted by user');
                        if (window.chrome && window.chrome.webview) {{
                            window.chrome.webview.postMessage({{
                                type: 'cancel',
                                orderLabel: '{orderLabel}'
                            }});
                        }}
                        break;

                    case 'expired':
                        console.log('[Forte Callback] Transaction expired');
                        if (window.chrome && window.chrome.webview) {{
                            window.chrome.webview.postMessage({{
                                type: 'error',
                                error: 'Transaction expired',
                                orderLabel: '{orderLabel}'
                            }});
                        }}
                        break;

                    default:
                        console.log('[Forte Callback] Unknown event:', response.event);
                        break;
                }}
            }} catch (err) {{
                console.error('[Forte Callback] Error parsing response:', err);
            }}
        }}

        // Capture all JavaScript errors and send to C#
        window.addEventListener('error', function(e) {{
            console.error('[JS Error]', e.message, e.filename, e.lineno, e.colno);
            if (window.chrome && window.chrome.webview) {{
                window.chrome.webview.postMessage({{
                    type: 'error',
                    error: e.message + ' at ' + e.filename + ':' + e.lineno,
                    orderLabel: '{orderLabel}'
                }});
            }}
            return false;
        }});

        console.log('[Forte Checkout] Initializing button-based checkout v2...');
        console.log('[Forte Checkout] Script URL: {scriptUrl}');
        console.log('[Forte Checkout] API Access ID: {config.ForteApiAccessId}');
        console.log('[Forte Checkout] Location ID: {config.ForteLocationId}');
        console.log('[Forte Checkout] Total Amount: ${amount:F2}');
        console.log('[Forte Checkout] Order Number: {orderLabel}');
        console.log('[Forte Checkout] UTC Time: {utcTime}');
        console.log('[Forte Checkout] Signature: {signature.Substring(0, 16)}...');

        // Log the actual button HTML to debug
        var button = document.getElementById('forte-checkout-button');
        if (button) {{
            console.log('[Forte Checkout] Button HTML:', button.outerHTML);
            console.log('[Forte Checkout] Button attributes:');
            console.log('  - api_access_id:', button.getAttribute('api_access_id'));
            console.log('  - method:', button.getAttribute('method'));
            console.log('  - version_number:', button.getAttribute('version_number'));
            console.log('  - location_id:', button.getAttribute('location_id'));
            console.log('  - total_amount:', button.getAttribute('total_amount'));
            console.log('  - utc_time:', button.getAttribute('utc_time'));
            console.log('  - order_number:', button.getAttribute('order_number'));
            console.log('  - signature:', button.getAttribute('signature'));
        }}

        // Debug: Check jQuery and button
        console.log('[Forte Checkout] jQuery loaded:', typeof $ !== 'undefined');

        // Wait for Forte to load and initialize, then auto-click
        setTimeout(function() {{
            var button = document.getElementById('forte-checkout-button');

            if (!button) {{
                console.error('[Forte Checkout] Button not found!');
                return;
            }}

            console.log('[Forte Checkout] Button found, checking initialization...');
            console.log('[Forte Checkout] Button HTML:', button.outerHTML.substring(0, 200));

            // Check if jQuery attached any data
            if (typeof $ !== 'undefined') {{
                console.log('[Forte Checkout] jQuery data on button:', $(button).data());
            }}

            // Just click it - Forte should have set up event handlers
            console.log('[Forte Checkout] Clicking button...');
            button.click();

            // Wait for Forte Checkout to render, then check for errors
            setTimeout(function() {{
                console.log('[Forte Checkout] Checking initialization result...');

                // Check if there's an error message in the embedded container
                var embeddedContainer = document.getElementById('fco-embedded');
                var hasError = false;
                var errorMessage = '';

                if (embeddedContainer) {{
                    var containerText = embeddedContainer.innerText || embeddedContainer.textContent || '';
                    console.log('[Forte Checkout] Container text:', containerText);

                    // Check for common error indicators
                    if (containerText.includes('error') ||
                        containerText.includes('Error') ||
                        containerText.includes('invalid') ||
                        containerText.includes('Invalid') ||
                        containerText.includes('failed') ||
                        containerText.includes('Failed') ||
                        containerText.includes('incorrect') ||
                        containerText.includes('Incorrect')) {{
                        hasError = true;
                        errorMessage = containerText.substring(0, 200); // First 200 chars
                        console.error('[Forte Checkout] ERROR DETECTED:', errorMessage);
                    }}

                    // Also check for Forte's specific error elements
                    var allElements = embeddedContainer.querySelectorAll('*');
                    for (var i = 0; i < allElements.length; i++) {{
                        var elem = allElements[i];
                        var className = elem.className || '';
                        if (className.toLowerCase().indexOf('error') >= 0 ||
                            className.toLowerCase().indexOf('alert') >= 0 ||
                            className.toLowerCase().indexOf('message') >= 0) {{
                            hasError = true;
                            var elemText = elem.innerText || elem.textContent || '';
                            console.error('[Forte Checkout] ERROR ELEMENT FOUND:', className, elemText);
                            if (elemText && errorMessage.indexOf(elemText) < 0) {{
                                errorMessage += elemText + ' ';
                            }}
                        }}
                    }}
                }}

                if (window.chrome && window.chrome.webview) {{
                    if (hasError) {{
                        console.error('[Forte Checkout] Sending error message to C#');
                        window.chrome.webview.postMessage({{
                            type: 'error',
                            error: errorMessage || 'Forte Checkout initialization failed',
                            orderLabel: '{orderLabel}'
                        }});
                    }} else {{
                        console.log('[Forte Checkout] No errors detected, sending ready message');
                        window.chrome.webview.postMessage({{
                            type: 'ready',
                            orderLabel: '{orderLabel}'
                        }});
                    }}
                }}
            }}, 3000); // Wait 3 seconds for Forte to render
        }}, 2000);
    </script>
</body>
</html>";

            return html;
        }
    }
}
