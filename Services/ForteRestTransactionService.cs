using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IPS.Services
{
    /// <summary>
    /// Service for processing card-present transactions via Forte REST API
    /// Uses encrypted card data from Dynaflex II Go
    /// </summary>
    public class ForteRestTransactionService
    {
        private readonly ConfigurationService _configService;
        private static readonly HttpClient _httpClient = new HttpClient();

        // Forte REST API endpoints
        private const string SANDBOX_BASE_URL = "https://sandbox.forte.net/api/v3";
        private const string PRODUCTION_BASE_URL = "https://api.forte.net/v3";

        public ForteRestTransactionService(ConfigurationService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        private void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [ForteREST] {message}");
        }

        /// <summary>
        /// Process a sale transaction using Dynaflex card data
        /// </summary>
        /// <param name="amount">Transaction amount in dollars</param>
        /// <param name="arqcData">ARQC data from Dynaflex</param>
        /// <param name="orderLabel">Order reference number</param>
        /// <returns>Transaction result</returns>
        public async Task<ForteTransactionResult> ProcessSaleAsync(decimal amount, DynaflexArqcData arqcData, string orderLabel)
        {
            var result = new ForteTransactionResult { OrderLabel = orderLabel };

            try
            {
                var config = _configService.GetConfiguration();

                Log($"Processing sale: ${amount:F2} for order {orderLabel}");
                Log($"Mode: {(config.ForteSandboxMode ? "SANDBOX" : "PRODUCTION")}");

                // Validate configuration
                if (string.IsNullOrWhiteSpace(config.ForteApiAccessId) ||
                    string.IsNullOrWhiteSpace(config.ForteApiSecureKey) ||
                    string.IsNullOrWhiteSpace(config.ForteOrganizationId) ||
                    string.IsNullOrWhiteSpace(config.ForteLocationId))
                {
                    result.Success = false;
                    result.ErrorMessage = "Forte API credentials not configured";
                    Log("ERROR: Missing Forte API credentials");
                    return result;
                }

                // Validate ARQC data
                if (arqcData == null || !arqcData.IsValid)
                {
                    result.Success = false;
                    result.ErrorMessage = "Invalid card data";
                    Log("ERROR: Invalid ARQC data");
                    return result;
                }

                // Build request
                var baseUrl = config.ForteSandboxMode ? SANDBOX_BASE_URL : PRODUCTION_BASE_URL;
                var endpoint = $"{baseUrl}/organizations/org_{config.ForteOrganizationId}/locations/loc_{config.ForteLocationId}/transactions";

                Log($"Endpoint: {endpoint}");

                // Build transaction request body using existing model classes
                var transactionRequest = new ForteEmvTransactionRequest
                {
                    Action = "sale",
                    AuthorizationAmount = amount,
                    Card = new ForteEmvCard
                    {
                        CardReader = "dynaflex2go",
                        CardEmvData = arqcData.ToForteTransactionOutput()
                    },
                    // Optional: billing info
                    BillingAddress = new ForteBillingAddress
                    {
                        FirstName = "IPS",
                        LastName = "Kiosk"
                    }
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var jsonBody = JsonSerializer.Serialize(transactionRequest, jsonOptions);
                Log($"Request body: {jsonBody}");

                // Setup HTTP request
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Add authentication header (Basic Auth)
                var authString = $"{config.ForteApiAccessId}:{config.ForteApiSecureKey}";
                var authBytes = Encoding.UTF8.GetBytes(authString);
                var authBase64 = Convert.ToBase64String(authBytes);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);

                // Add required headers
                request.Headers.Add("X-Forte-Auth-Organization-Id", $"org_{config.ForteOrganizationId}");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Send request
                Log("Sending transaction request...");
                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                Log($"Response status: {response.StatusCode}");
                Log($"Response body: {responseBody}");

                // Parse response
                if (response.IsSuccessStatusCode)
                {
                    var transactionResponse = JsonSerializer.Deserialize<ForteTransactionResponse>(responseBody, jsonOptions);

                    if (transactionResponse?.Response?.ResponseCode?.StartsWith("A") == true)
                    {
                        // Approved
                        result.Success = true;
                        result.TransactionId = transactionResponse.TransactionId ?? "";
                        result.AuthorizationCode = transactionResponse.AuthorizationCode ?? transactionResponse.Response?.AuthorizationCode ?? "";
                        result.ResponseCode = transactionResponse.Response?.ResponseCode ?? "";
                        result.ResponseDescription = transactionResponse.Response?.ResponseDesc ?? "APPROVED";
                        result.EmvReceiptData = transactionResponse.Response?.EmvReceiptData;

                        Log($"APPROVED: TxnId={result.TransactionId}, AuthCode={result.AuthorizationCode}");
                    }
                    else
                    {
                        // Declined or error
                        result.Success = false;
                        result.ResponseCode = transactionResponse?.Response?.ResponseCode ?? "";
                        result.ResponseDescription = transactionResponse?.Response?.ResponseDesc ?? "Transaction failed";
                        result.ErrorMessage = result.ResponseDescription;

                        Log($"DECLINED: {result.ResponseCode} - {result.ResponseDescription}");
                    }
                }
                else
                {
                    // HTTP error
                    result.Success = false;
                    result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";

                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ForteTransactionResponse>(responseBody, jsonOptions);
                        if (errorResponse?.Response != null)
                        {
                            result.ErrorMessage = errorResponse.Response.ResponseDesc ?? result.ErrorMessage;
                        }
                    }
                    catch { }

                    Log($"ERROR: {result.ErrorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Network error: {ex.Message}";
                Log($"Network error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                result.Success = false;
                result.ErrorMessage = "Request timed out";
                Log("Request timed out");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
                Log($"Unexpected error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Test Forte REST API credentials
        /// </summary>
        public async Task<(bool success, string message)> TestCredentialsAsync()
        {
            try
            {
                var config = _configService.GetConfiguration();

                Log("Testing Forte REST API credentials...");

                if (string.IsNullOrWhiteSpace(config.ForteApiAccessId) ||
                    string.IsNullOrWhiteSpace(config.ForteApiSecureKey) ||
                    string.IsNullOrWhiteSpace(config.ForteOrganizationId) ||
                    string.IsNullOrWhiteSpace(config.ForteLocationId))
                {
                    return (false, "Missing API credentials");
                }

                // Test by getting location info
                var baseUrl = config.ForteSandboxMode ? SANDBOX_BASE_URL : PRODUCTION_BASE_URL;
                var endpoint = $"{baseUrl}/organizations/org_{config.ForteOrganizationId}/locations/loc_{config.ForteLocationId}";

                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

                var authString = $"{config.ForteApiAccessId}:{config.ForteApiSecureKey}";
                var authBytes = Encoding.UTF8.GetBytes(authString);
                var authBase64 = Convert.ToBase64String(authBytes);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authBase64);
                request.Headers.Add("X-Forte-Auth-Organization-Id", $"org_{config.ForteOrganizationId}");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Log("Credentials valid!");
                    return (true, "Credentials valid - connected to Forte API");
                }
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    Log($"Credentials test failed: {response.StatusCode} - {body}");
                    return (false, $"API returned {(int)response.StatusCode}: {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Log($"Credentials test error: {ex.Message}");
                return (false, ex.Message);
            }
        }
    }

    // Note: Uses model classes from FortePaymentModels.cs (ForteEmvTransactionRequest, ForteEmvCard, etc.)

    /// <summary>
    /// Result of a Forte transaction
    /// </summary>
    public class ForteTransactionResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string AuthorizationCode { get; set; } = string.Empty;
        public string ResponseCode { get; set; } = string.Empty;
        public string ResponseDescription { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string OrderLabel { get; set; } = string.Empty;
        public string? EmvReceiptData { get; set; }

        /// <summary>
        /// Parse EMV receipt data for receipt printing
        /// Format: "application_label:Visa Debit|entry_mode:CHIP|CVM:5E0000|AID:A0000000031010|TVR:8000008000|IAD:06010A03A08000|TSI:6800|ARC:"
        /// </summary>
        public EmvReceiptInfo? ParseEmvReceiptData()
        {
            if (string.IsNullOrEmpty(EmvReceiptData))
                return null;

            var info = new EmvReceiptInfo();
            var parts = EmvReceiptData.Split('|');

            foreach (var part in parts)
            {
                var kv = part.Split(':', 2);
                if (kv.Length == 2)
                {
                    var key = kv[0].Trim().ToLower();
                    var value = kv[1].Trim();

                    switch (key)
                    {
                        case "application_label":
                            info.ApplicationLabel = value;
                            break;
                        case "entry_mode":
                            info.EntryMode = value;
                            break;
                        case "cvm":
                            info.CVM = value;
                            break;
                        case "aid":
                            info.AID = value;
                            break;
                        case "tvr":
                            info.TVR = value;
                            break;
                        case "iad":
                            info.IAD = value;
                            break;
                        case "tsi":
                            info.TSI = value;
                            break;
                        case "arc":
                            info.ARC = value;
                            break;
                    }
                }
            }

            return info;
        }
    }

    /// <summary>
    /// Parsed EMV receipt data for printing
    /// </summary>
    public class EmvReceiptInfo
    {
        public string ApplicationLabel { get; set; } = string.Empty;
        public string EntryMode { get; set; } = string.Empty;
        public string CVM { get; set; } = string.Empty;
        public string AID { get; set; } = string.Empty;
        public string TVR { get; set; } = string.Empty;
        public string IAD { get; set; } = string.Empty;
        public string TSI { get; set; } = string.Empty;
        public string ARC { get; set; } = string.Empty;
    }
}
