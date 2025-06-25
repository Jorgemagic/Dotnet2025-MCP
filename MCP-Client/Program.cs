using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OllamaSharp;
using Spectre.Console;

namespace TestingMCPClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            // Enable Unicode (UTF-8) output for emojis and special icons
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            // Define client options with metadata
            var clientOptions = new McpClientOptions
            {
                ClientInfo = new Implementation
                {
                    Name = "EvergineMCP Client",
                    Version = "1.0.0"
                }
            };

            string mcpServerPath = System.Configuration.ConfigurationManager.AppSettings["mcp-server"]?? throw new Exception("mcp-server path empty");           
            var transportOptions = new StdioClientTransportOptions
            {
                Name = "Evergine MCP Server",
                Command = Path.GetFullPath(mcpServerPath),
            };            

            AnsiConsole.MarkupLine("[grey]Creating transport...[/]");
            var transport = new StdioClientTransport(transportOptions);

            AnsiConsole.MarkupLine("[grey]Attempting to connect to server...[/]");

            // Define a cancellation token to limit the connection attempt duration
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // Establish client connection with transport options
            IMcpClient mcpClient = await McpClientFactory.CreateAsync(transport, clientOptions, cancellationToken: cts.Token);

            AnsiConsole.MarkupLine("[green]✅ Connected successfully![/]");

           
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddConsole().SetMinimumLevel(LogLevel.Information));

            // Option1: Configure OpenAI LLM Client
            //string azureOpenAiApiKey = System.Configuration.ConfigurationManager.AppSettings["azure-openAi-apiKey"] ?? throw new ArgumentException("Azure openAI APIKey not found");
            //string azureOpenAiApiUrl = System.Configuration.ConfigurationManager.AppSettings["azure-openAi-apiUrl"] ?? throw new ArgumentException("Azure openAI APIUrl not found");
            //string deploymentOrModelName = System.Configuration.ConfigurationManager.AppSettings["model-deployment-name"] ?? throw new ArgumentException("Azure openAI APIKey not found");

            //IChatClient chatClient = new AzureOpenAIClient(
            //                                          new Uri(azureOpenAiApiUrl),
            //                                          new AzureKeyCredential(azureOpenAiApiKey))
            //                                    .GetChatClient(deploymentOrModelName)
            //                                    .AsIChatClient();

            // Option2: Configure OllamaSharp Client
            var uri = new Uri("http://localhost:11434");
            IChatClient chatClient = new ChatClientBuilder(
                                                         new OllamaApiClient(uri, "llama3.2:3b")
                                                         )
                                                        .UseLogging(loggerFactory)
                                                        .UseFunctionInvocation()
                                                        .Build();

            IChatClient llmClient = new ChatClientBuilder(chatClient)
                                                .UseLogging(loggerFactory)
                                                .UseFunctionInvocation()
                                                .Build();
            AnsiConsole.MarkupLine("[green]✅ OpenAI API client connected[/]");

            AnsiConsole.WriteLine();

            // Retrieve the list of available tools from the server
            AnsiConsole.MarkupLine("[grey]Fetching available tools...[/]");
            IList<McpClientTool> tools = await mcpClient.ListToolsAsync();

            AnsiConsole.MarkupLine($"[green]✅ {tools.Count} tools found:[/]");

            foreach (McpClientTool tool in tools)
            {                
                AnsiConsole.MarkupLine($"[maroon]\t-{tool.Name}: {tool.Description ?? "No description provided"}[/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();

            // Prompt loop
            var chatOptions = new ChatOptions { Tools = [.. tools] };            
            AnsiConsole.MarkupLine("[grey bold underline]Type your message below (type 'exit' to quit):[/]");

            List<ChatMessage> messages = new List<ChatMessage>
            {
                new(ChatRole.System, "You are a helpful assistant.")
            };

            while (true)
            {                
                string? userInput = AnsiConsole.Ask<string>("You:");

                // Asserts
                if (string.IsNullOrWhiteSpace(userInput)) continue;
                if (userInput.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                messages.Add(new(ChatRole.User, userInput));

                AnsiConsole.WriteLine();
                AnsiConsole.Markup("[yellow]AI:[/] ");
                List<ChatResponseUpdate> updates = [];
                await foreach (ChatResponseUpdate update in llmClient
                    .GetStreamingResponseAsync(messages, chatOptions))
                {
                    AnsiConsole.Markup($"[yellow]{update}[/]");
                    updates.Add(update);
                }
                AnsiConsole.WriteLine();

                messages.AddMessages(updates);
            }

            AnsiConsole.MarkupLine("[grey]Exiting chat...[/]");
        }
    }
}
