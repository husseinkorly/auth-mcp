using Microsoft.Identity.Web;
using server.tools;


var builder = WebApplication.CreateBuilder(args);

// Add MCP server with HTTP transport
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<GraphTools>();

builder.Services.AddHttpClient();
// Add CORS policy for cross-origin requests
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// add authentication service
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

var app = builder.Build();

// Use CORS
app.UseCors();

// Map MCP endpoints
app.MapMcp().RequireAuthorization();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run($"http://0.0.0.0:3001");
