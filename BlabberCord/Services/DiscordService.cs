using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace BlabberCord.Services
{
    public class DiscordService
    {
        private readonly IServiceProvider _services;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly GptService _gptService;
        private readonly ILogger _logger;
        private readonly PersonaService _personaService;

        public DiscordService(IServiceProvider services, ILogger<DiscordService> logger, PersonaService personaService)
        {
            _services = services;
            _client = services.GetRequiredService<DiscordSocketClient>();
            _commandService = services.GetRequiredService<CommandService>();
            _gptService = services.GetRequiredService<GptService>();
            _logger = logger;
            _personaService = personaService;

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
        }

        public async Task StartAsync(string token)
        {
            await _commandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), _services);

            _client.InteractionCreated += HandleInteractionCreated;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }
        private async Task HandleInteractionCreated(SocketInteraction interaction)
        {
            if (interaction is SocketSlashCommand slashCommand)
            {
                await HandleSlashCommand(slashCommand);
            }
        }

        private async Task HandleSlashCommand(SocketSlashCommand slashCommand)
        {
            switch (slashCommand.Data.Name)
            {
                case "select-persona":
                    await HandleSelectPersonaCommand(slashCommand);
                    break;
                case "reset-conversation":
                    await HandleResetConversationCommand(slashCommand);
                    break;
                case "add-persona":
                    await HandleAddPersonaCommand(slashCommand);
                    break;
                // Add more cases here to handle other slash commands
                default:
                    await slashCommand.RespondAsync("Unknown command", ephemeral: true);
                    break;
            }
        }
        private async Task HandleSelectPersonaCommand(SocketSlashCommand slashCommand)
        {
            string selectedPersona = slashCommand.Data.Options.FirstOrDefault(x => x.Name == "persona")?.Value.ToString();

            if (selectedPersona == null)
            {
                await slashCommand.RespondAsync("Invalid persona selected", ephemeral: true);
                return;
            }

            try
            {
                _gptService.SetConversationPersona(slashCommand.Channel.Id, selectedPersona);
            }
            catch (Exception ex)
            {
                await slashCommand.RespondAsync($"Failed to change persona to {selectedPersona}", ephemeral: true);
                return;
            }

            await slashCommand.RespondAsync($"Persona changed to {selectedPersona}", ephemeral: true);

        }
        private async Task HandleAddPersonaCommand(SocketSlashCommand slashCommand)
        {
            string personaName = slashCommand.Data.Options.FirstOrDefault(x => x.Name == "persona-name")?.Value.ToString();
            string personaPrompt = slashCommand.Data.Options.FirstOrDefault(x => x.Name == "persona-prompt")?.Value.ToString();

            if (string.IsNullOrEmpty(personaName) || string.IsNullOrEmpty(personaPrompt))
            {
                await slashCommand.RespondAsync("Invalid persona information", ephemeral: true);
                return;
            }

            try
            {
                // Ensure the "Personas" folder exists in the current directory
                string folderPath = Path.Combine(Environment.CurrentDirectory, "Personas");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Combine the folder path with the user-provided filename to create the full file path
                string filePath = Path.Combine(folderPath, $"{personaName}.txt");

                // Write the content to the file, overwriting it if it already exists
                await File.WriteAllTextAsync(filePath, personaPrompt);

                Console.WriteLine("File saved successfully.");
            }
            catch (Exception ex)
            {
                await slashCommand.RespondAsync($"Failed with message: {ex.Message}", ephemeral: true);
                return;
            }

            await slashCommand.RespondAsync($"Persona '{personaName}' created. Restart the BlabberCord service and use `/select-persona`", ephemeral: true);
        }

        private async Task HandleResetConversationCommand(SocketSlashCommand slashCommand)
        {
            string selectedPersona = slashCommand.Data.Options.FirstOrDefault(x => x.Name == "persona")?.Value.ToString();

            if (selectedPersona == null)
            {
                await slashCommand.RespondAsync("Invalid persona selected", ephemeral: true);
                return;
            }

            try
            {
                _gptService.ResetChannelMessages(slashCommand.Channel.Id);
                _gptService.SetConversationPersona(slashCommand.Channel.Id, selectedPersona);
            }
            catch (Exception ex)
            {
                await slashCommand.RespondAsync($"Failed to reset channel conversation", ephemeral: true);
                return;
            }

            await slashCommand.RespondAsync($"Reset channel conversation and set persona to {selectedPersona}", ephemeral: true);

        }

        private async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore messages from other bots and system messages
            if (!(rawMessage is SocketUserMessage message) || message.Source != MessageSource.User)
            {
                return;
            }

            // Check if the bot is mentioned or the message is a direct message
            var botMentioned = message.MentionedUsers.Any(user => user.Id == _client.CurrentUser.Id);
            var isDirectMessage = message.Channel is IDMChannel;

            // Ignore messages that are not direct messages and do not mention the bot
            if (!botMentioned && !isDirectMessage)
            {
                return;
            }

            // Extract the message content without the bot mention (if mentioned)
            string messageContent = botMentioned
                ? message.Content.Replace($"<@!{_client.CurrentUser.Id}>", "").Trim()
                : message.Content.Trim();

            if (string.IsNullOrEmpty(messageContent)) return;

            _logger.LogTrace($"User {message.Author} channel {message.Channel.Id} - {messageContent}");

            // Show the bot as typing while waiting for the GPT response
            using (message.Channel.EnterTypingState())
            {
                // Process the message with GPT and send the response
                string response = await _gptService.GenerateResponseAsync(message.Channel.Id, messageContent);

                await SendMessage(message, response);
            }
        }

        public async Task SendMessage(SocketUserMessage incomingMessage, string replyMessage)
        {
            const int maxMessageLength = 2000;
            List<string> lines = new List<string>(replyMessage.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

            StringBuilder currentMessage = new StringBuilder();
            var isAlreadyInCodeSnippet = false;
            foreach (var line in lines)
            {
                bool isCodeSnippet = line.StartsWith("```");

                if ((isCodeSnippet && !isAlreadyInCodeSnippet)
                    || currentMessage.Length + line.Length + 1 > maxMessageLength)
                {
                    // Send the current message if it's not empty
                    if (currentMessage.Length > 0)
                    {
                        await incomingMessage.Channel.SendMessageAsync(currentMessage.ToString());
                        currentMessage.Clear();
                    }
                }


                if (currentMessage.Length > 0)
                {
                    currentMessage.Append('\n');
                }
                currentMessage.Append(line);

                if (isCodeSnippet) isAlreadyInCodeSnippet = !isAlreadyInCodeSnippet;
            }

            // Send the remaining message if there's any content
            if (currentMessage.Length > 0)
            {
                await incomingMessage.Channel.SendMessageAsync(currentMessage.ToString());
            }
        }


        public List<string> SplitMessagePreservingCodeSnippets(string message)
        {
            const int maxMessageLength = 2000;
            var messageChunks = new List<string>();

            if (message.Length <= maxMessageLength)
            {
                messageChunks.Add(message);
                return messageChunks;
            }

            var startIndex = 0;
            var snippetRegex = new Regex(@"```[\s\S]*?```", RegexOptions.Compiled);

            while (startIndex < message.Length)
            {
                var endIndex = startIndex + maxMessageLength - 1;
                var hasCodeSnippet = snippetRegex.IsMatch(message.Substring(startIndex));

                if (hasCodeSnippet)
                {
                    var matches = snippetRegex.Matches(message.Substring(startIndex));

                    foreach (Match match in matches)
                    {
                        var adjustedEndIndex = startIndex + match.Index + match.Length - 1;

                        if (adjustedEndIndex <= endIndex)
                        {
                            endIndex = adjustedEndIndex;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    endIndex = Math.Min(endIndex, message.Length - 1);
                    var lastPeriodIndex = message.LastIndexOf('.', endIndex, endIndex - startIndex + 1);
                    var lastQuestionIndex = message.LastIndexOf('?', endIndex, endIndex - startIndex + 1);
                    var lastExclamationIndex = message.LastIndexOf('!', endIndex, endIndex - startIndex + 1);

                    var lastSentenceIndex = Math.Max(lastPeriodIndex, Math.Max(lastQuestionIndex, lastExclamationIndex));

                    if (lastSentenceIndex != -1)
                    {
                        endIndex = lastSentenceIndex;
                    }
                }

                messageChunks.Add(message.Substring(startIndex, endIndex - startIndex + 1));
                startIndex = endIndex + 1;
            }

            return messageChunks;
        }


        private Task LogAsync(LogMessage log)
        {
            switch (log.Severity)
            {
                case LogSeverity.Critical:
                    _logger.LogCritical(log.Exception, log.Message);
                    break;
                case LogSeverity.Error:
                    _logger.LogError(log.Exception, log.Message);
                    break;
                case LogSeverity.Warning:
                    _logger.LogWarning(log.Message);
                    break;
                case LogSeverity.Info:
                    _logger.LogInformation(log.Message);
                    break;
                case LogSeverity.Debug:
                case LogSeverity.Verbose:
                default:
                    _logger.LogDebug(log.Message);
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {

            var personaNames = _personaService.GetPersonaNames();

            var personaCommandOption = new SlashCommandOptionBuilder()
                .WithName("persona")
                .WithDescription("Select a persona you wish GPT to use and reset your conversation in this channel")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String)
                ;
            personaCommandOption.Choices = personaNames.Select(x => new ApplicationCommandOptionChoiceProperties() { Name = x, Value = x }).ToList();

            var selectPersonaCommand = new SlashCommandBuilder()
                .WithName("select-persona")
                .WithDescription("Select a GPT persona")
                .AddOption(personaCommandOption)
                ;

            var resetConverstionCommand = new SlashCommandBuilder()
                .WithName("reset-conversation")
                .WithDescription("Reset this channels conversation with chat GPT")
                .AddOption(personaCommandOption)
                ;


            var addPersonaNameOption = new SlashCommandOptionBuilder()
                .WithName("persona-name")
                .WithDescription("Set the persona name")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String)
                .WithMaxLength(30)
                .WithMinLength(1)
                ;
            var addPersonaPromptNameOption = new SlashCommandOptionBuilder()
                .WithName("persona-prompt")
                .WithDescription("Set the persona prompt")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String)
                .WithMaxLength(1500)
                .WithMinLength(1)
                ;
            var addPersonaCommand = new SlashCommandBuilder()
                .WithName("add-persona")
                .WithDescription("Create (or update) a persona")
                .AddOption(addPersonaNameOption)
                .AddOption(addPersonaPromptNameOption)
                ;

            await _client.CreateGlobalApplicationCommandAsync(selectPersonaCommand.Build());
            await _client.CreateGlobalApplicationCommandAsync(resetConverstionCommand.Build());
            await _client.CreateGlobalApplicationCommandAsync(addPersonaCommand.Build());

            _logger.LogInformation($"Connected as {_client.CurrentUser}. Available personas: {string.Join(", ", personaNames)}");
        }
    }
}
