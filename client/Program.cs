using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;


var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
var configuration = builder.Build();


// creating MCP client
var credentials = new AzureCliCredential();
HttpClient httpClient = new();

var token = await credentials.GetTokenAsync(
    new TokenRequestContext(["api://b17cb93c-9c74-4d94-97a8-76cbb3d9ff12/.default"]));
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
var transportOptions = new SseClientTransport(new SseClientTransportOptions
{
    Name = "MCP Client",
    Endpoint = new Uri("http://localhost:3001"),
    TransportMode = HttpTransportMode.StreamableHttp
}, httpClient: httpClient, ownsHttpClient: false);
var mcpClient = await McpClientFactory.CreateAsync(transportOptions);

// querying tools from MCP server
var remoteTools = await mcpClient.ListToolsAsync();
foreach (var tool in remoteTools)
{
    Console.WriteLine($"remote tool name: {tool.Name}");
    Console.WriteLine($"remote tool description: {tool.Description}");
}


// building the kernel
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(
    deploymentName: "gpt-4o-mini",
    endpoint: "https://understand-foundry2.cognitiveservices.azure.com",
    credentials: credentials
);
Kernel kernel = kernelBuilder.Build();

//converting the tools to Kernel functions
#pragma warning disable SKEXP0001
kernel.Plugins.AddFromFunctions("Graph", remoteTools.Select(aiFunction => aiFunction.AsKernelFunction()));
// Enable automatic function calling
OpenAIPromptExecutionSettings executionSettings = new()
{
    Temperature = 0,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
};
var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

// Interactive loop
var chatHistory = new ChatHistory();
chatHistory.AddSystemMessage("You are a helpful assistant that can answer questions about user's profile");
Console.WriteLine("Assistant Ready! (Type 'exit' to quit)\n");

while (true)
{
    Console.Write("You: ");
    var prompt = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(prompt) || prompt.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Goodbye!");
        break;
    }

    chatHistory.AddUserMessage(prompt);
    Console.Write("Assistant: ");

    // Start spinner in background
    var spinnerCts = new CancellationTokenSource();
    var spinnerTask = Task.Run(() => ShowSpinner(spinnerCts.Token));

    try
    {
        // Call LLM
        var result = await chatCompletion.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);

        // Stop spinner
        spinnerCts.Cancel();
        await spinnerTask;

        // Clear spinner and show result
        Console.Write($"\rAssistant: {result}\n\n");
    }
    catch (Exception ex)
    {
        spinnerCts.Cancel();
        await spinnerTask;
        Console.Write($"\rAssistant: Error: {ex.Message}\n\n");
    }
}

// Spinner function
static void ShowSpinner(CancellationToken cancellationToken)
{
    var spinnerChars = new[] { '|', '/', '-', '\\' };
    var spinnerIndex = 0;

    while (!cancellationToken.IsCancellationRequested)
    {
        Console.Write($"\rAssistant: {spinnerChars[spinnerIndex % spinnerChars.Length]} ");
        spinnerIndex++;

        try
        {
            Task.Delay(100, cancellationToken).Wait();
        }
        catch (AggregateException)
        {
            // Task was cancelled
            break;
        }
    }

    // Clear spinner
    Console.Write("\r" + new string(' ', 20) + "\r");
}