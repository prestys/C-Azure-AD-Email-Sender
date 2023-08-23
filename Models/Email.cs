using System.Text;
using System.Text.Json;

namespace CSharp_Azure_Email.Models;

public class EmailSender
{
    private string clientId = ""; // Azure Application ID
    private string clientSecret = ""; // Azure Client Secret
    private string tenantId = ""; // Azure Tenant ID
    private string tokenEndpoint = ""; // Token Endpoint
    private string emailSender = ""; // Email Sender
    
    public string Subject { get; set; }
    public string Body { get; set; }
    public string ContentType { get; set; }
    public IEnumerable<string> Addresses { get; set; }
    
    public EmailSender(string subject, string body, IEnumerable<string> addresses, string contentType = "HTML")
    {
        tokenEndpoint = tokenEndpoint.Replace("{tenantId}", tenantId);

        Subject = subject;
        Body = body;
        Addresses = addresses;
        ContentType = contentType;
    }
    
    public async Task<bool> SendEmail()
    {
        using (HttpClient client = new HttpClient())
        {
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),

            });
            
            HttpResponseMessage response = await client.PostAsync(tokenEndpoint, requestContent);
            string responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            string accessToken = tokenResponse.GetProperty("access_token").GetString();
            
            string sendMailEndpoint = $"https://graph.microsoft.com/v1.0/users/{emailSender}/sendMail";

            var emailPayload = new
            {
                message = new
                {
                    subject = Subject, // Subject
                    body = new
                    {
                        contentType = ContentType, // Type of content e.g. HTML/TEXT
                        content = Body // Body Text
                    },
                    toRecipients = Addresses.Select(address => new
                    {
                        emailAddress = new
                        {
                            address = address
                        }
                    }).ToArray()
                }
            };
            
            // Serialize payload to JSON
            var emailContent = new StringContent(JsonSerializer.Serialize(emailPayload), Encoding.UTF8, "application/json");

            // Set the authorization header
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // Send the email
            var sendMailResponse = await client.PostAsync(sendMailEndpoint, emailContent);

            return sendMailResponse.IsSuccessStatusCode;
        }
    }
}