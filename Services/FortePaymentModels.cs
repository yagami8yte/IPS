using System;
using System.Text.Json.Serialization;

namespace IPS.Services
{
    /// <summary>
    /// Forte payment transaction request
    /// </summary>
    public class ForteTransactionRequest
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = "sale";

        [JsonPropertyName("authorization_amount")]
        public decimal AuthorizationAmount { get; set; }

        [JsonPropertyName("billing_address")]
        public ForteBillingAddress? BillingAddress { get; set; }

        [JsonPropertyName("card")]
        public ForteCard? Card { get; set; }
    }

    /// <summary>
    /// Forte billing address information
    /// </summary>
    public class ForteBillingAddress
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("last_name")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("physical_address")]
        public FortePhysicalAddress? PhysicalAddress { get; set; }
    }

    /// <summary>
    /// Forte physical address
    /// </summary>
    public class FortePhysicalAddress
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
        public string Country { get; set; } = "US";
    }

    /// <summary>
    /// Forte card information
    /// </summary>
    public class ForteCard
    {
        [JsonPropertyName("name_on_card")]
        public string NameOnCard { get; set; } = string.Empty;

        [JsonPropertyName("account_number")]
        public string AccountNumber { get; set; } = string.Empty;

        [JsonPropertyName("expire_month")]
        public int ExpireMonth { get; set; }

        [JsonPropertyName("expire_year")]
        public int ExpireYear { get; set; }

        [JsonPropertyName("card_verification_value")]
        public string CardVerificationValue { get; set; } = string.Empty;

        [JsonPropertyName("card_type")]
        public string? CardType { get; set; }
    }

    /// <summary>
    /// Forte transaction response
    /// </summary>
    public class ForteTransactionResponse
    {
        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; } = string.Empty;

        [JsonPropertyName("location_id")]
        public string LocationId { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("authorization_amount")]
        public decimal AuthorizationAmount { get; set; }

        [JsonPropertyName("authorization_code")]
        public string? AuthorizationCode { get; set; }

        [JsonPropertyName("response")]
        public ForteResponse? Response { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("entered_by")]
        public string? EnteredBy { get; set; }

        [JsonPropertyName("received_date")]
        public DateTime ReceivedDate { get; set; }
    }

    /// <summary>
    /// Forte response details
    /// </summary>
    public class ForteResponse
    {
        [JsonPropertyName("environment")]
        public string Environment { get; set; } = string.Empty;

        [JsonPropertyName("response_type")]
        public string ResponseType { get; set; } = string.Empty;

        [JsonPropertyName("response_code")]
        public string ResponseCode { get; set; } = string.Empty;

        [JsonPropertyName("response_desc")]
        public string ResponseDesc { get; set; } = string.Empty;

        [JsonPropertyName("authorization_code")]
        public string? AuthorizationCode { get; set; }

        [JsonPropertyName("avs_result")]
        public string? AvsResult { get; set; }

        [JsonPropertyName("cvv_result")]
        public string? CvvResult { get; set; }
    }

    /// <summary>
    /// Forte error response
    /// </summary>
    public class ForteErrorResponse
    {
        [JsonPropertyName("response")]
        public ForteErrorDetail? Response { get; set; }
    }

    /// <summary>
    /// Forte error detail
    /// </summary>
    public class ForteErrorDetail
    {
        [JsonPropertyName("environment")]
        public string Environment { get; set; } = string.Empty;

        [JsonPropertyName("response_desc")]
        public string ResponseDesc { get; set; } = string.Empty;

        [JsonPropertyName("response_code")]
        public int ResponseCode { get; set; }
    }

    /// <summary>
    /// Forte organization information response
    /// </summary>
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

    /// <summary>
    /// Forte location information response
    /// </summary>
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

    /// <summary>
    /// Forte address information
    /// </summary>
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
}
