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
        /// <returns>Payment result with transaction details</returns>
        public async Task<FortePaymentResult> ProcessPaymentAsync(
            decimal amount,
            string cardNumber,
            int expiryMonth,
            int expiryYear,
            string cvv,
            string cardholderName,
            string? billingZip = null)
        {
            try
            {
                Console.WriteLine($"[FortePaymentService] Processing payment: Amount=${amount}");

                var config = _configService.GetConfiguration();

                // Validate configuration
                if (string.IsNullOrWhiteSpace(config.ForteApiAccessId) ||
                    string.IsNullOrWhiteSpace(config.ForteApiSecureKey) ||
                    string.IsNullOrWhiteSpace(config.ForteOrganizationId) ||
                    string.IsNullOrWhiteSpace(config.ForteLocationId))
                {
                    Console.WriteLine("[FortePaymentService] ERROR: Forte credentials not configured");
                    return new FortePaymentResult
                    {
                        Success = false,
                        ErrorMessage = "Forte payment gateway is not configured. Please contact administrator."
                    };
                }

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
                        CardVerificationValue = cvv
                    }
                };

                // Add billing address if ZIP provided
                if (!string.IsNullOrWhiteSpace(billingZip))
                {
                    transactionRequest.BillingAddress = new ForteBillingAddress
                    {
                        FirstName = cardholderName.Split(' ')[0],
                        LastName = cardholderName.Contains(' ') ? cardholderName.Split(' ')[1] : "",
                        PhysicalAddress = new FortePhysicalAddress
                        {
                            PostalCode = billingZip,
                            Country = "US"
                        }
                    };
                }

                // Send transaction request
                var response = await SendTransactionAsync(config, transactionRequest);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FortePaymentService] Exception: {ex.Message}");
                Console.WriteLine($"[FortePaymentService] Stack trace: {ex.StackTrace}");
                return new FortePaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Payment processing failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Send transaction request to Forte API
        /// </summary>
        private async Task<FortePaymentResult> SendTransactionAsync(
            AppConfiguration config,
            ForteTransactionRequest request)
        {
            try
            {
                // Build endpoint URL
                var baseUrl = config.ForteSandboxMode ? SANDBOX_BASE_URL : PRODUCTION_BASE_URL;
                var endpoint = $"{baseUrl}/organizations/{config.ForteOrganizationId}/locations/{config.ForteLocationId}/transactions";

                Console.WriteLine($"[FortePaymentService] Endpoint: {endpoint}");
                Console.WriteLine($"[FortePaymentService] Sandbox Mode: {config.ForteSandboxMode}");

                // Create HTTP request
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);

                // Add authentication header (Basic Auth)
                var authBytes = Encoding.UTF8.GetBytes($"{config.ForteApiAccessId}:{config.ForteApiSecureKey}");
                var authBase64 = Convert.ToBase64String(authBytes);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);

                // Add required headers
                httpRequest.Headers.Add("X-Forte-Auth-Organization-Id", config.ForteOrganizationId);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Serialize request body
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                var jsonBody = JsonSerializer.Serialize(request, jsonOptions);
                httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                Console.WriteLine($"[FortePaymentService] Request Body: {jsonBody}");

                // Send request
                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseBody = await httpResponse.Content.ReadAsStringAsync();

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

                var baseUrl = config.ForteSandboxMode ? SANDBOX_BASE_URL : PRODUCTION_BASE_URL;
                var testEndpoint = $"{baseUrl}/organizations/{config.ForteOrganizationId}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, testEndpoint);
                var authBytes = Encoding.UTF8.GetBytes($"{config.ForteApiAccessId}:{config.ForteApiSecureKey}");
                var authBase64 = Convert.ToBase64String(authBytes);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);
                httpRequest.Headers.Add("X-Forte-Auth-Organization-Id", config.ForteOrganizationId);

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
    }
}
