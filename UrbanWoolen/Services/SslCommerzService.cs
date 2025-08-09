using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using UrbanWoolen.Models;
using UrbanWoolen.Models.ViewModels;

public class SslCommerzService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public SslCommerzService(IConfiguration config)
    {
        _config = config;
        _httpClient = new HttpClient();
    }

    public async Task<string> InitiatePaymentAsync(Order order, string successUrl, string failUrl, string cancelUrl, CheckoutViewModel customer)
    {
        var storeId = _config["SSLCommerz:StoreId"];
        var storePassword = _config["SSLCommerz:StorePassword"];
        var isSandbox = bool.Parse(_config["SSLCommerz:IsSandbox"]);

        var url = isSandbox
            ? "https://sandbox.sslcommerz.com/gwprocess/v4/api.php"
            : "https://securepay.sslcommerz.com/gwprocess/v4/api.php";

        decimal totalAmount = order.Items.Sum(i => i.Price * i.Quantity);

        var data = new Dictionary<string, string>
        {
            { "store_id", storeId },
            { "store_passwd", storePassword },
            { "total_amount", totalAmount.ToString("F2") },
            { "currency", "BDT" },
            { "tran_id", "ORDER" + order.Id },
            { "success_url", successUrl },
            { "fail_url", failUrl },
            { "cancel_url", cancelUrl },

            // ✅ REQUIRED customer fields
            { "cus_name", customer.FullName },
            { "cus_email", customer.Email },
            { "cus_add1", customer.AddressLine1 },
            { "cus_add2", customer.AddressLine2 ?? "" },
            { "cus_city", customer.City },
            { "cus_state", customer.State },
            { "cus_postcode", customer.PostalCode },
            { "cus_country", customer.Country },
            { "cus_phone", customer.Phone },
            { "cus_fax", customer.Phone },

            // Shipping fields (same as billing)
            { "shipping_method", "NO" },
            { "ship_name", customer.FullName },
            { "ship_add1", customer.AddressLine1 },
            { "ship_add2", customer.AddressLine2 ?? "" },
            { "ship_city", customer.City },
            { "ship_state", customer.State },
            { "ship_postcode", customer.PostalCode },
            { "ship_country", customer.Country },

            // Optional product info
            { "product_name", "UrbanWoolen Cart" },
            { "product_category", "Ecommerce" },
            { "product_profile", "general" }
        };

        var content = new FormUrlEncodedContent(data);
        var response = await _httpClient.PostAsync(url, content);
        var responseString = await response.Content.ReadAsStringAsync();

        // DEBUG: Print raw response
        Console.WriteLine("SSLCommerz Raw Response:");
        Console.WriteLine(responseString);

        var jsonDoc = JsonDocument.Parse(responseString);
        var root = jsonDoc.RootElement;

        if (root.TryGetProperty("GatewayPageURL", out var urlElement))
        {
            var gatewayUrl = urlElement.GetString();
            if (!string.IsNullOrEmpty(gatewayUrl))
                return gatewayUrl;
        }

        throw new Exception("Failed to receive GatewayPageURL from SSLCommerz.\nRaw Response:\n" + responseString);
    }
}
