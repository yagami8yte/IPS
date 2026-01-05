using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IPS.Core.Models;

namespace IPS.Services
{
    /// <summary>
    /// Forte Payment Gateway integration service
    /// Handles credit card transactions via Forte REST API v3
    /// </summary>
    public class FortePaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigurationService _configService;
        private const string SANDBOX_BASE_URL = "https://sandbox.forte.net/api/v3";
        private const string PRODUCTION_BASE_URL = "https://api.forte.net/v3";

        public FortePaymentService(ConfigurationService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Process a credit card payment transaction
        /// </summary>
        /// <param name="amount">Transaction amount in dollars</param>
        /// <param name="cardNumber">Credit card number</param>
        /// <param name="expiryMonth">Expiration month (1-12)</param>
        /// <param name="expiryYear">Expiration year (4 digits, e.g., 2025)</param>
        /// <param name="cvv">Card verification value (CVV/CVC)</param>
        /// <param name="cardholderName">Name on card</param>
        /// <param name="billingZip">Billing ZIP code (optional, for AVS verification)</param>
        /// <param name="logCallback">Optional callback for logging</param>
        /// <returns>Payment result with transaction details</returns>
        public async Task<FortePaymentResult> ProcessPaymentAsync(
            decimal amount,
            string cardNumber,
            int expiryMonth,
            int expiryYear,
            string cvv,
            string cardholderName,
            string? billingZip = null,
            Action<string>? logCallback = null)
        {
            void Log(string message)
            {
                var timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                Console.WriteLine($"[FortePaymentService] {message}");
                logCallback?.Invoke(timestamped);
            }

            // Extract card info for response
            var cardLast4 = cardNumber.Length >= 4 ? cardNumber[^4..] : cardNumber;
            var cardType = DetectCardType(cardNumber);

            try
            {
                Log($"═══════════════════════════════════════════");
                Log($"FORTE REST API TRANSACTION");
                Log($"═══════════════════════════════════════════");
                Log($"Amount: ${amount:F2}");
                Log($"Card: ****{cardLast4} ({cardType})");
                Log($"Input Method: Manual Entry (Test Card)");

                var config = _configService.GetConfiguration();
                Log($"Mode: {(config.ForteSandboxMode ? "SANDBOX" : "PRODUCTION")}");

                // Validate configuration
                if (string.IsNullOrWhiteSpace(config.ForteApiAccessId) ||
                    string.IsNullOrWhiteSpace(config.ForteApiSecureKey) ||
                    string.IsNullOrWhiteSpace(config.ForteOrganizationId) ||
                    string.IsNullOrWhiteSpace(config.ForteLocationId))
                {
                    Log("ERROR: Forte credentials not configured");
                    return new FortePaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Forte payment gateway is not configured. Please contact administrator.",
                        CardLast4 = cardLast4,
                        CardType = cardType
                    };
                }
                Log($"Credentials: OK");

                // Log Dynaflex format info for reference
                Log($"-------------------------------------------");
                Log($"NOTE: Dynaflex encrypted format would use:");
                Log($"  card.card_reader: dynaflex");
                Log($"  card.card_data: <encrypted_swipe_data>");
                Log($"Current test mode uses manual entry format.");
                Log($"-------------------------------------------");

                // Build transaction request
                var transactionRequest = new ForteTransactionRequest
                {
                    Action = "sale",
                    AuthorizationAmount = amount,
                    Card = new ForteCard
                    {
                        NameOnCard = cardholderName,
                        AccountNumber = cardNumber,
                        ExpireMonth = expiryMonth,
                        ExpireYear = expiryYear,
                        CardVerificationValue = cvv,
                        CardType = GetForteCardTypeCode(cardType) // visa, mast, amex, disc
                    }
                };

                // Parse cardholder name into first/last name (required by Forte)
                var nameParts = cardholderName.Trim().Split(' ', 2);
                var firstName = nameParts[0];
                var lastName = nameParts.Length > 1 ? nameParts[1] : firstName; // Use first name if no last name

                // Add billing address (first_name and last_name are mandatory)
                transactionRequest.BillingAddress = new ForteBillingAddress
                {
                    FirstName = firstName,
                    LastName = lastName
                };

                // Add physical address if ZIP provided
                if (!string.IsNullOrWhiteSpace(billingZip))
                {
                    transactionRequest.BillingAddress.PhysicalAddress = new FortePhysicalAddress
                    {
                        PostalCode = billingZip,
                        Country = "US"
                    };
                }

                Log($"Building transaction request...");
                Log($"Sending to Forte API...");

                // Send transaction request
                var response = await SendTransactionAsync(config, transactionRequest, Log);

                // Add card info to response
                response.CardLast4 = cardLast4;
                response.CardType = cardType;

                if (response.Success)
                {
                    Log($"═══════════════════════════════════════════");
                    Log($"✓ TRANSACTION APPROVED");
                    Log($"Transaction ID: {response.TransactionId}");
                    Log($"Authorization Code: {response.AuthorizationCode}");
                    Log($"═══════════════════════════════════════════");
                }
                else
                {
                    Log($"═══════════════════════════════════════════");
                    Log($"✗ TRANSACTION DECLINED/FAILED");
                    Log($"Error: {response.ErrorMessage}");
                    Log($"═══════════════════════════════════════════");
                }

                return response;
            }
            catch (Exception ex)
            {
                Log($"EXCEPTION: {ex.Message}");
                Console.WriteLine($"[FortePaymentService] Stack trace: {ex.StackTrace}");
                return new FortePaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Payment processing failed: {ex.Message}",
                    CardLast4 = cardLast4,
                    CardType = cardType
                };
            }
        }

        /// <summary>
        /// Process payment using Dynaflex encrypted swipe/tap data
        /// This is the format used when actual Dynaflex hardware is connected
        /// </summary>
        /// <param name="amount">Transaction amount in dollars</param>
        /// <param name="encryptedCardData">Encrypted card data from Dynaflex reader</param>
        /// <param name="logCallback">Optional callback for logging</param>
        /// <returns>Payment result with transaction details</returns>
        public async Task<FortePaymentResult> ProcessDynaflexPaymentAsync(
            decimal amount,
            string encryptedCardData,
            Action<string>? logCallback = null)
        {
            void Log(string message)
            {
                var timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                Console.WriteLine($"[FortePaymentService] {message}");
                logCallback?.Invoke(timestamped);
            }

            try
            {
                Log($"═══════════════════════════════════════════");
                Log($"FORTE REST API - DYNAFLEX TRANSACTION");
                Log($"═══════════════════════════════════════════");
                Log($"Amount: ${amount:F2}");
                Log($"Input Method: Dynaflex Encrypted Swipe/Tap");
                Log($"Card Data Length: {encryptedCardData?.Length ?? 0} chars");

                var config = _configService.GetConfiguration();
                Log($"Mode: {(config.ForteSandboxMode ? "SANDBOX" : "PRODUCTION")}");

                // Validate configuration
                if (string.IsNullOrWhiteSpace(config.ForteApiAccessId) ||
                    string.IsNullOrWhiteSpace(config.ForteApiSecureKey) ||
                    string.IsNullOrWhiteSpace(config.ForteOrganizationId) ||
                    string.IsNullOrWhiteSpace(config.ForteLocationId))
                {
                    Log("ERROR: Forte credentials not configured");
                    return new FortePaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Forte payment gateway is not configured. Please contact administrator."
                    };
                }
                Log($"Credentials: OK");

                // Build Dynaflex transaction request
                var transactionRequest = new ForteDynaflexTransactionRequest
                {
                    Action = "sale",
                    AuthorizationAmount = amount,
                    Card = new ForteDynaflexCard
                    {
                        CardReader = "dynaflex2go", // Forte requires "dynaflex2go" for Dynaflex II Go
                        CardData = encryptedCardData
                    }
                };

                Log($"Building Dynaflex transaction request...");
                Log($"Sending to Forte API...");

                // Send transaction request
                var response = await SendDynaflexTransactionAsync(config, transactionRequest, Log);

                if (response.Success)
                {
                    Log($"═══════════════════════════════════════════");
                    Log($"✓ DYNAFLEX TRANSACTION APPROVED");
                    Log($"Transaction ID: {response.TransactionId}");
                    Log($"Authorization Code: {response.AuthorizationCode}");
                    Log($"═══════════════════════════════════════════");
                }
                else
                {
                    Log($"═══════════════════════════════════════════");
                    Log($"✗ DYNAFLEX TRANSACTION DECLINED/FAILED");
                    Log($"Error: {response.ErrorMessage}");
                    Log($"═══════════════════════════════════════════");
                }

                return response;
            }
            catch (Exception ex)
            {
                Log($"EXCEPTION: {ex.Message}");
                Console.WriteLine($"[FortePaymentService] Stack trace: {ex.StackTrace}");
                return new FortePaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Payment processing failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Detect card type from card number (display name)
        /// </summary>
        private static string DetectCardType(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber)) return "Unknown";

            var cleanNumber = cardNumber.Replace(" ", "").Replace("-", "");

            if (cleanNumber.StartsWith("4")) return "Visa";
            if (cleanNumber.StartsWith("5") && cleanNumber.Length >= 2)
            {
                var secondDigit = cleanNumber[1] - '0';
                if (secondDigit >= 1 && secondDigit <= 5) return "Mastercard";
            }
            if (cleanNumber.StartsWith("34") || cleanNumber.StartsWith("37")) return "Amex";
            if (cleanNumber.StartsWith("6011") || cleanNumber.StartsWith("65")) return "Discover";

            return "Card";
        }

        /// <summary>
        /// Get Forte API card type code (visa, mast, amex, disc)
        /// </summary>
        private static string GetForteCardTypeCode(string cardType)
        {
            return cardType.ToLower() switch
            {
                "visa" => "visa",
                "mastercard" => "mast",
                "amex" => "amex",
                "discover" => "disc",
                _ => "visa" // default
            };
        }

        /// <summary>
        /// Send transaction request to Forte API
        /// </summary>
        private async Task<FortePaymentResult> SendTransactionAsync(
            AppConfiguration config,
            ForteTransactionRequest request,
            Action<string>? log = null)
        {
            void Log(string message) => log?.Invoke(message);

            try
            {
                // Build endpoint URL - REST API requires org_/loc_ prefixes
                var baseUrl = config.ForteSandboxMode ? SANDBOX_BASE_URL : PRODUCTION_BASE_URL;
                var orgId = config.ForteOrganizationId.StartsWith("org_")
                    ? config.ForteOrganizationId
                    : $"org_{config.ForteOrganizationId}";
                var locId = config.ForteLocationId.StartsWith("loc_")
                    ? config.ForteLocationId
                    : $"loc_{config.ForteLocationId}";

                var endpoint = $"{baseUrl}/organizations/{orgId}/locations/{locId}/transactions";

                Log($"Endpoint: {endpoint}");
                Log($"Organization ID: {orgId}");
                Log($"Location ID: {locId}");
                Console.WriteLine($"[FortePaymentService] Endpoint: {endpoint}");

                // Create HTTP request
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);

                // Add authentication header (Basic Auth)
                var authBytes = Encoding.UTF8.GetBytes($"{config.ForteApiAccessId}:{config.ForteApiSecureKey}");
                var authBase64 = Convert.ToBase64String(authBytes);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);

                // Add required headers
                httpRequest.Headers.Add("X-Forte-Auth-Organization-Id", orgId);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Serialize request body
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                var jsonBody = JsonSerializer.Serialize(request, jsonOptions);
                httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Log request (without sensitive data)
                Log($"Request action: {request.Action}");
                Log($"Request amount: ${request.AuthorizationAmount:F2}");
                Console.WriteLine($"[FortePaymentService] Request Body: {jsonBody}");

                // Send request
                Log($"Sending HTTP POST...");
                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseBody = await httpResponse.Content.ReadAsStringAsync();

                Log($"Response Status: {(int)httpResponse.StatusCode} {httpResponse.StatusCode}");
                Console.WriteLine($"[FortePaymentService] Response Status: {httpResponse.StatusCode}");
                Console.WriteLine($"[FortePaymentService] Response Body: {responseBody}");

                // Parse response
                if (httpResponse.IsSuccessStatusCode)
                {
                    var transactionResponse = JsonSerializer.Deserialize<ForteTransactionResponse>(responseBody, jsonOptions);

                    if (transactionResponse == null)
                    {
                        return new FortePaymentResult
                        {
                            Success = false,
                            ErrorMessage = "Failed to parse transaction response"
                        };
                    }

                    // Check transaction status
                    bool isApproved = transactionResponse.Response?.ResponseCode == "A01" ||
                                     transactionResponse.Status?.ToLower() == "complete";

                    return new FortePaymentResult
                    {
                        Success = isApproved,
                        TransactionId = transactionResponse.TransactionId,
                        AuthorizationCode = transactionResponse.AuthorizationCode ?? transactionResponse.Response?.AuthorizationCode,
                        Amount = transactionResponse.AuthorizationAmount,
                        ResponseCode = transactionResponse.Response?.ResponseCode ?? "",
                        ResponseMessage = transactionResponse.Response?.ResponseDesc ?? transactionResponse.Status,
                        ErrorMessage = isApproved ? null : transactionResponse.Response?.ResponseDesc
                    };
                }
                else
                {
                    // Parse error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ForteErrorResponse>(responseBody, jsonOptions);
                        return new FortePaymentResult
                        {
                            Success = false,
                            ErrorMessage = errorResponse?.Response?.ResponseDesc ?? $"Payment failed with status {httpResponse.StatusCode}"
                        };
                    }
                    catch
                    {
                        return new FortePaymentResult
                        {
                            Success = false,
                            ErrorMessage = $"Payment failed: {httpResponse.StatusCode} - {responseBody}"
                        };
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[FortePaymentService] HTTP Error: {ex.Message}");
                return new FortePaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Network error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FortePaymentService] Error: {ex.Message}");
                return new FortePaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Transaction error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Send Dynaflex transaction request to Forte API
        /// </summary>
        private async Task<FortePaymentResult> SendDynaflexTransactionAsync(
            AppConfiguration config,
            ForteDynaflexTransactionRequest request,
            Action<string>? log = null)
        {
            void Log(string message) => log?.Invoke(message);

            try
            {
                // Build endpoint URL - REST API requires org_/loc_ prefixes
                var baseUrl = config.ForteSandboxMode ? SANDBOX_BASE_URL : PRODUCTION_BASE_URL;
                var orgId = config.ForteOrganizationId.StartsWith("org_")
                    ? config.ForteOrganizationId
                    : $"org_{config.ForteOrganizationId}";
                var locId = config.ForteLocationId.StartsWith("loc_")
                    ? config.ForteLocationId
                    : $"loc_{config.ForteLocationId}";

                var endpoint = $"{baseUrl}/organizations/{orgId}/locations/{locId}/transactions";

                Log($"Endpoint: {endpoint}");
                Log($"Organization ID: {orgId}");
                Log($"Location ID: {locId}");
                Console.WriteLine($"[FortePaymentService] Dynaflex Endpoint: {endpoint}");

                // Create HTTP request
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);

                // Add authentication header (Basic Auth)
                var authBytes = Encoding.UTF8.GetBytes($"{config.ForteApiAccessId}:{config.ForteApiSecureKey}");
                var authBase64 = Convert.ToBase64String(authBytes);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);

                // Add required headers
                httpRequest.Headers.Add("X-Forte-Auth-Organization-Id", orgId);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Serialize request body with snake_case naming
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    WriteIndented = true
                };
                var jsonBody = JsonSerializer.Serialize(request, jsonOptions);
                httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Log request
                Log($"Request action: {request.Action}");
                Log($"Request amount: ${request.AuthorizationAmount:F2}");
                Log($"Card reader: {request.Card?.CardReader}");
                Log($"Card data preview: {request.Card?.CardData?.Substring(0, Math.Min(30, request.Card?.CardData?.Length ?? 0))}...");
                Console.WriteLine($"[FortePaymentService] Dynaflex Request Body: {jsonBody}");

                // Send request
                Log($"Sending HTTP POST...");
                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseBody = await httpResponse.Content.ReadAsStringAsync();

                Log($"Response Status: {(int)httpResponse.StatusCode} {httpResponse.StatusCode}");
                Console.WriteLine($"[FortePaymentService] Dynaflex Response Status: {httpResponse.StatusCode}");
                Console.WriteLine($"[FortePaymentService] Dynaflex Response Body: {responseBody}");

                // Parse response
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var transactionResponse = JsonSerializer.Deserialize<ForteTransactionResponse>(responseBody, responseOptions);

                    if (transactionResponse == null)
                    {
                        return new FortePaymentResult
                        {
                            Success = false,
                            ErrorMessage = "Failed to parse transaction response"
                        };
                    }

                    // Check transaction status
                    bool isApproved = transactionResponse.Response?.ResponseCode == "A01" ||
                                     transactionResponse.Status?.ToLower() == "complete";

                    return new FortePaymentResult
                    {
                        Success = isApproved,
                        TransactionId = transactionResponse.TransactionId,
                        AuthorizationCode = transactionResponse.AuthorizationCode ?? transactionResponse.Response?.AuthorizationCode,
                        Amount = transactionResponse.AuthorizationAmount,
                        ResponseCode = transactionResponse.Response?.ResponseCode ?? "",
                        ResponseMessage = transactionResponse.Response?.ResponseDesc ?? transactionResponse.Status,
                        ErrorMessage = isApproved ? null : transactionResponse.Response?.ResponseDesc
                    };
                }
                else
                {
                    // Parse error response
                    try
                    {
                        var responseOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var errorResponse = JsonSerializer.Deserialize<ForteErrorResponse>(responseBody, responseOptions);
                        return new FortePaymentResult
                        {
                            Success = false,
                            ErrorMessage = errorResponse?.Response?.ResponseDesc ?? $"Payment failed with status {httpResponse.StatusCode}"
                        };
                    }
                    catch
                    {
                        return new FortePaymentResult
                        {
                            Success = false,
                            ErrorMessage = $"Payment failed: {httpResponse.StatusCode} - {responseBody}"
                        };
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[FortePaymentService] Dynaflex HTTP Error: {ex.Message}");
                return new FortePaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Network error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FortePaymentService] Dynaflex Error: {ex.Message}");
                return new FortePaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Transaction error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Fetch organization information from Forte API
        /// </summary>
        public async Task<ForteOrganizationInfo?> GetOrganizationInfoAsync()
        {
            try
            {
                var config = _configService.GetConfiguration();

                if (string.IsNullOrWhiteSpace(config.ForteApiAccessId) ||
                    string.IsNullOrWhiteSpace(config.ForteApiSecureKey) ||
                    string.IsNullOrWhiteSpace(config.ForteOrganizationId))
                {
                    Console.WriteLine("[FortePaymentService] Credentials not configured");
                    return null;
                }

                // REST API requires org_ prefix
                var baseUrl = config.ForteSandboxMode ? SANDBOX_BASE_URL : PRODUCTION_BASE_URL;
                var orgId = config.ForteOrganizationId.StartsWith("org_")
                    ? config.ForteOrganizationId
                    : $"org_{config.ForteOrganizationId}";

                var endpoint = $"{baseUrl}/organizations/{orgId}";

                Console.WriteLine($"[FortePaymentService] Fetching organization info from: {endpoint}");

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
                var authBytes = Encoding.UTF8.GetBytes($"{config.ForteApiAccessId}:{config.ForteApiSecureKey}");
                var authBase64 = Convert.ToBase64String(authBytes);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);
                httpRequest.Headers.Add("X-Forte-Auth-Organization-Id", orgId);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                Console.WriteLine($"[FortePaymentService] Request Headers:");
                Console.WriteLine($"  Authorization: Basic {authBase64.Substring(0, Math.Min(20, authBase64.Length))}...");
                Console.WriteLine($"  X-Forte-Auth-Organization-Id: {orgId}");
                Console.WriteLine($"  Accept: application/json");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[FortePaymentService] Organization API Response:");
                Console.WriteLine($"  Status Code: {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"  Response Body Length: {responseBody.Length} characters");
                Console.WriteLine($"  Response Body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var orgInfo = JsonSerializer.Deserialize<ForteOrganizationInfo>(responseBody, jsonOptions);
                    Console.WriteLine($"[FortePaymentService] ✓ Organization fetched successfully: {orgInfo?.OrganizationName}");
                    return orgInfo;
                }
                else
                {
                    Console.WriteLine($"[FortePaymentService] ✗ Failed to fetch organization - Status: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FortePaymentService] Error fetching organization: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fetch location information from Forte API
        /// </summary>
        public async Task<ForteLocationInfo?> GetLocationInfoAsync()
        {
            try
            {
                var config = _configService.GetConfiguration();

                if (string.IsNullOrWhiteSpace(config.ForteApiAccessId) ||
                    string.IsNullOrWhiteSpace(config.ForteApiSecureKey) ||
                    string.IsNullOrWhiteSpace(config.ForteOrganizationId) ||
                    string.IsNullOrWhiteSpace(config.ForteLocationId))
                {
                    Console.WriteLine("[FortePaymentService] Credentials or Location ID not configured");
                    return null;
                }

                // REST API requires org_/loc_ prefixes
                var baseUrl = config.ForteSandboxMode ? SANDBOX_BASE_URL : PRODUCTION_BASE_URL;
                var orgId = config.ForteOrganizationId.StartsWith("org_")
                    ? config.ForteOrganizationId
                    : $"org_{config.ForteOrganizationId}";
                var locId = config.ForteLocationId.StartsWith("loc_")
                    ? config.ForteLocationId
                    : $"loc_{config.ForteLocationId}";

                var endpoint = $"{baseUrl}/organizations/{orgId}/locations/{locId}";

                Console.WriteLine($"[FortePaymentService] Fetching location info from: {endpoint}");

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
                var authBytes = Encoding.UTF8.GetBytes($"{config.ForteApiAccessId}:{config.ForteApiSecureKey}");
                var authBase64 = Convert.ToBase64String(authBytes);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);
                httpRequest.Headers.Add("X-Forte-Auth-Organization-Id", orgId);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                Console.WriteLine($"[FortePaymentService] Request Headers:");
                Console.WriteLine($"  Authorization: Basic {authBase64.Substring(0, Math.Min(20, authBase64.Length))}...");
                Console.WriteLine($"  X-Forte-Auth-Organization-Id: {orgId}");
                Console.WriteLine($"  Accept: application/json");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[FortePaymentService] Location API Response:");
                Console.WriteLine($"  Status Code: {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"  Response Body Length: {responseBody.Length} characters");
                Console.WriteLine($"  Response Body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var locationInfo = JsonSerializer.Deserialize<ForteLocationInfo>(responseBody, jsonOptions);
                    Console.WriteLine($"[FortePaymentService] ✓ Location fetched successfully: {locationInfo?.DbaName}");
                    return locationInfo;
                }
                else
                {
                    Console.WriteLine($"[FortePaymentService] ✗ Failed to fetch location - Status: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FortePaymentService] Error fetching location: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Test connection to Forte API with current credentials
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var config = _configService.GetConfiguration();

                if (string.IsNullOrWhiteSpace(config.ForteApiAccessId) ||
                    string.IsNullOrWhiteSpace(config.ForteApiSecureKey) ||
                    string.IsNullOrWhiteSpace(config.ForteOrganizationId))
                {
                    Console.WriteLine("[FortePaymentService] Credentials not configured");
                    return false;
                }

                // REST API requires org_ prefix
                var baseUrl = config.ForteSandboxMode ? SANDBOX_BASE_URL : PRODUCTION_BASE_URL;
                var orgId = config.ForteOrganizationId.StartsWith("org_")
                    ? config.ForteOrganizationId
                    : $"org_{config.ForteOrganizationId}";

                var testEndpoint = $"{baseUrl}/organizations/{orgId}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, testEndpoint);
                var authBytes = Encoding.UTF8.GetBytes($"{config.ForteApiAccessId}:{config.ForteApiSecureKey}");
                var authBase64 = Convert.ToBase64String(authBytes);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);
                httpRequest.Headers.Add("X-Forte-Auth-Organization-Id", orgId);

                var response = await _httpClient.SendAsync(httpRequest);
                Console.WriteLine($"[FortePaymentService] Test connection result: {response.StatusCode}");

                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Forbidden;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FortePaymentService] Test connection failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Forte payment transaction result
    /// </summary>
    public class FortePaymentResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? AuthorizationCode { get; set; }
        public decimal Amount { get; set; }
        public string? ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CardLast4 { get; set; }
        public string? CardType { get; set; }
    }

}
