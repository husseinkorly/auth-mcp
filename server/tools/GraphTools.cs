using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Identity.Web;

namespace server.tools;

[McpServerToolType]
public class GraphTools(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ITokenAcquisition tokenAcquisition,
    IHttpContextAccessor httpContextAccessor,
    ILogger<GraphTools> logger)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ITokenAcquisition _tokenAcquisition = tokenAcquisition;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<GraphTools> _logger = logger;

    private async Task<string?> GetAccessToken()
    {
        // Get the current user's ClaimsPrincipal from HTTP context
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null)
        {
            _logger.LogWarning("No authenticated user found in HTTP context");
            return null;
        }

        _logger.LogInformation("Getting token for user: {UserName}", user.Identity?.Name ?? "Unknown");
        try
        {
            return await _tokenAcquisition.GetAccessTokenForUserAsync(["User.Read"], user: user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting access token");
            return null;
        }
    }

    [McpServerTool, Description("Get current user's profile information from Microsoft Graph.")]
    public async Task<string> GetMyProfile()
    {
        try
        {
            var token = await GetAccessToken();
            if (string.IsNullOrEmpty(token))
            {
                return "Error: No access token found. Please authenticate first.";
            }

            var client = _httpClientFactory.CreateClient();
            var graphApiUrl = _configuration["Services:GraphApiUrl"];
            if (string.IsNullOrEmpty(graphApiUrl))
            {
                return "Error: Graph API URL is not configured.";
            }
            client.BaseAddress = new Uri(graphApiUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("me");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return "Error: Access token is invalid or expired.";
            }

            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<JsonElement>();

            return $"""
                Display Name: {user.GetProperty("displayName").GetString() ?? "N/A"}
                Email: {user.GetProperty("mail").GetString() ?? user.GetProperty("userPrincipalName").GetString() ?? "N/A"}
                Job Title: {(user.TryGetProperty("jobTitle", out var jobTitle) ? jobTitle.GetString() : "N/A")}
                Department: {(user.TryGetProperty("department", out var dept) ? dept.GetString() : "N/A")}
                Office Location: {(user.TryGetProperty("officeLocation", out var office) ? office.GetString() : "N/A")}
                Mobile Phone: {(user.TryGetProperty("mobilePhone", out var mobile) ? mobile.GetString() : "N/A")}
                """;
        }
        catch (HttpRequestException ex)
        {
            return $"Error fetching user profile: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Unexpected error: {ex.Message}";
        }
    }
}
