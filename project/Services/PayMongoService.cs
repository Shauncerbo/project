using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace project.Services
{
    public interface IPayMongoService
    {
        Task<PayMongoPaymentResponse> CreatePaymentLinkAsync(decimal amount, string description, string referenceId);
        Task<PayMongoPaymentStatus> CheckPaymentStatusAsync(string paymentId);
        Task<string> GetPaymentLinkUrlAsync(string paymentIntentId);
    }

    public class PayMongoService : IPayMongoService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "https://api.paymongo.com/v1";

        public PayMongoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private async Task<string> GetApiKeyAsync()
        {
            // Get API key from SecureStorage (you'll need to set this in settings)
            var apiKey = await SecureStorage.Default.GetAsync("paymongo_secret_key");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("PayMongo API key not configured. Please set it in Settings.");
            }
            return apiKey;
        }

        private async Task<HttpRequestMessage> CreateAuthenticatedRequestAsync(HttpMethod method, string endpoint, object? content = null)
        {
            var apiKey = await GetApiKeyAsync(); // This will throw if API key is not configured

            var request = new HttpRequestMessage(method, $"{_apiUrl}/{endpoint}");
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey + ":"));
            request.Headers.Add("Authorization", $"Basic {authValue}");
            request.Headers.Add("Accept", "application/json");

            if (content != null)
            {
                var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return request;
        }

        public async Task<PayMongoPaymentResponse> CreatePaymentLinkAsync(decimal amount, string description, string referenceId)
        {
            try
            {
                // Step 1: Create Payment Intent
                var paymentIntentData = new
                {
                    data = new
                    {
                        attributes = new
                        {
                            amount = (int)(amount * 100), // Convert to centavos
                            currency = "PHP",
                            description = description,
                            payment_method_allowed = new[] { "card", "paymaya", "gcash", "grab_pay" }
                        }
                    }
                };

                var intentRequest = await CreateAuthenticatedRequestAsync(HttpMethod.Post, "payment_intents", paymentIntentData);
                var intentResponse = await _httpClient.SendAsync(intentRequest);
                var intentContent = await intentResponse.Content.ReadAsStringAsync();

                if (!intentResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to create payment intent: {intentContent}");
                }

                var intentJson = JsonDocument.Parse(intentContent);
                var paymentIntentId = intentJson.RootElement.GetProperty("data").GetProperty("id").GetString();

                // Step 2: Create Payment Link
                var linkData = new
                {
                    data = new
                    {
                        attributes = new
                        {
                            amount = (int)(amount * 100),
                            currency = "PHP",
                            description = description,
                            reference_number = referenceId
                        }
                    }
                };

                var linkRequest = await CreateAuthenticatedRequestAsync(HttpMethod.Post, "links", linkData);
                var linkResponse = await _httpClient.SendAsync(linkRequest);
                var linkContent = await linkResponse.Content.ReadAsStringAsync();

                if (!linkResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to create payment link: {linkContent}");
                }

                var linkJson = JsonDocument.Parse(linkContent);
                var linkId = linkJson.RootElement.GetProperty("data").GetProperty("id").GetString();
                var checkoutUrl = linkJson.RootElement.GetProperty("data").GetProperty("attributes").GetProperty("checkout_url").GetString();

                return new PayMongoPaymentResponse
                {
                    Success = true,
                    PaymentIntentId = paymentIntentId ?? string.Empty,
                    PaymentLinkId = linkId ?? string.Empty,
                    CheckoutUrl = checkoutUrl ?? string.Empty,
                    Message = "Payment link created successfully"
                };
            }
            catch (Exception ex)
            {
                return new PayMongoPaymentResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<PayMongoPaymentStatus> CheckPaymentStatusAsync(string paymentId)
        {
            try
            {
                // First try to check payment link status (more accurate for payment links)
                var linkRequest = await CreateAuthenticatedRequestAsync(HttpMethod.Get, $"links/{paymentId}");
                var linkResponse = await _httpClient.SendAsync(linkRequest);
                
                if (linkResponse.IsSuccessStatusCode)
                {
                    var linkContent = await linkResponse.Content.ReadAsStringAsync();
                    var linkJson = JsonDocument.Parse(linkContent);
                    var linkData = linkJson.RootElement.GetProperty("data");
                    var linkAttributes = linkData.GetProperty("attributes");
                    
                    // Check payment link status
                    if (linkAttributes.TryGetProperty("status", out var linkStatusValue))
                    {
                        var linkStatusStr = linkStatusValue.GetString();
                        System.Diagnostics.Debug.WriteLine($"PayMongo Link Status: {linkStatusStr}");
                        
                        // If link is paid, return that status
                        if (linkStatusStr?.ToLower() == "paid" || linkStatusStr?.ToLower() == "succeeded")
                        {
                            return new PayMongoPaymentStatus
                            {
                                Success = true,
                                Status = linkStatusStr,
                                Message = "Payment link is paid"
                            };
                        }
                    }
                    
                    // Check if link has payments
                    if (linkAttributes.TryGetProperty("payments", out var linkPayments) && 
                        linkPayments.ValueKind == System.Text.Json.JsonValueKind.Array && 
                        linkPayments.GetArrayLength() > 0)
                    {
                        var latestPayment = linkPayments[linkPayments.GetArrayLength() - 1];
                        if (latestPayment.TryGetProperty("attributes", out var paymentAttributes))
                        {
                            if (paymentAttributes.TryGetProperty("status", out var paymentStatusValue))
                            {
                                var paymentStatusStr = paymentStatusValue.GetString();
                                System.Diagnostics.Debug.WriteLine($"PayMongo Link Payment Status: {paymentStatusStr}");
                                return new PayMongoPaymentStatus
                                {
                                    Success = true,
                                    Status = paymentStatusStr ?? "unknown",
                                    Message = "Payment status from link"
                                };
                            }
                        }
                    }
                }
                
                // Fallback: Check payment intent status
                var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, $"payment_intents/{paymentId}");
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"PayMongo Intent Check Failed: {content}");
                    return new PayMongoPaymentStatus
                    {
                        Success = false,
                        Status = "unknown",
                        Message = $"Failed to check payment status: {content}"
                    };
                }

                var json = JsonDocument.Parse(content);
                var data = json.RootElement.GetProperty("data");
                var attributes = data.GetProperty("attributes");
                var intentStatus = attributes.GetProperty("status").GetString();
                
                // Check if there are payments associated with this intent
                string? actualPaymentStatus = null;
                if (attributes.TryGetProperty("payments", out var payments) && payments.ValueKind == System.Text.Json.JsonValueKind.Array && payments.GetArrayLength() > 0)
                {
                    // Get the latest payment status
                    var latestPayment = payments[payments.GetArrayLength() - 1];
                    if (latestPayment.TryGetProperty("attributes", out var paymentAttributes))
                    {
                        if (paymentAttributes.TryGetProperty("status", out var paymentStatus))
                        {
                            actualPaymentStatus = paymentStatus.GetString();
                        }
                    }
                }
                
                // Use actual payment status if available, otherwise use intent status
                var finalStatus = actualPaymentStatus ?? intentStatus ?? "unknown";
                
                System.Diagnostics.Debug.WriteLine($"PayMongo Intent Status Check - Intent: {intentStatus}, Payment: {actualPaymentStatus}, Final: {finalStatus}");

                return new PayMongoPaymentStatus
                {
                    Success = true,
                    Status = finalStatus,
                    Message = "Payment status retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PayMongo Status Check Error: {ex.Message}");
                return new PayMongoPaymentStatus
                {
                    Success = false,
                    Status = "error",
                    Message = ex.Message
                };
            }
        }

        public async Task<string> GetPaymentLinkUrlAsync(string paymentIntentId)
        {
            try
            {
                var request = await CreateAuthenticatedRequestAsync(HttpMethod.Get, $"payment_intents/{paymentIntentId}");
                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var json = JsonDocument.Parse(content);
                    // Extract checkout URL if available
                    return string.Empty;
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public class PayMongoPaymentResponse
    {
        public bool Success { get; set; }
        public string PaymentIntentId { get; set; } = string.Empty;
        public string PaymentLinkId { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class PayMongoPaymentStatus
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty; // paid, pending, failed
        public string Message { get; set; } = string.Empty;
    }
}


