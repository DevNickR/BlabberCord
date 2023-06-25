using BlabberCord.Models.Gpt;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http.Json;
using System.Text;


namespace BlabberCord.Services
{
    public class GptService
    {
        private readonly HttpClient _httpClient;
        private readonly PersonaService _personaService;
        private readonly string _apiEndpoint;
        private readonly GptSettings _gptSettings;

        private readonly Dictionary<ulong, List<GptMessage>> _conversations = new Dictionary<ulong, List<GptMessage>>();

        private const string GptSystemName = "system";


        public GptService(IConfiguration configuration, PersonaService personasService)
        {
            var apiKey = configuration["Gpt:ApiKey"];
            var model = configuration["Gpt:Model"];
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(model)) throw new ArgumentNullException("Please set \"Gpt:ApiKey\" and \"Gpt:Model\" in appsettings.json or environment variables");

            var temperature = configuration.GetValue<double?>("Gpt:Temperature");
            if (!temperature.HasValue) throw new ArgumentNullException("Please set \"Gpt:Temperature\" in appsettings.json or environment variables");

            var maxTokens = configuration.GetValue<int?>("Gpt:MaxTokens");
            if (!maxTokens.HasValue) throw new ArgumentNullException("Please set \"Gpt:MaxTokens\" in appsettings.json or environment variables");

            var topP = configuration.GetValue<double?>("Gpt:TopP");
            if (!topP.HasValue) throw new ArgumentNullException("Please set \"Gpt:TopP\" in appsettings.json or environment variables");

            var frequencyPenalty = configuration.GetValue<double?>("Gpt:FrequencyPenalty");
            if (!frequencyPenalty.HasValue) throw new ArgumentNullException("Please set \"Gpt:FrequencyPenalty\" in appsettings.json or environment variables");

            var presencePenalty = configuration.GetValue<double?>("Gpt:PresencePenalty");
            if (!presencePenalty.HasValue) throw new ArgumentNullException("Please set \"Gpt:PresencePenalty\" in appsettings.json or environment variables");

            _gptSettings = new GptSettings(apiKey, model, temperature.Value, maxTokens.Value, topP.Value, frequencyPenalty.Value, presencePenalty.Value);
            _personaService = personasService;
            _apiEndpoint = "https://api.openai.com/v1/chat/completions";
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_gptSettings.ApiKey}");
        }

        public void ResetChannelMessages(ulong channelId)
        {
            _conversations.Remove(channelId);
        }

        public async Task<string> GenerateResponseAsync(ulong channelId, string message)
        {
            InitConversation(channelId);
            AddUserMessage(channelId, message);

            var requestData = CreateGptRequest(channelId);
            var content = SerializeGptRequest(requestData);

            var response = await _httpClient.PostAsync(_apiEndpoint, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadFromJsonAsync<GptErrorResponse>();
                var errMessage = $"GPT API request failed: {response.ReasonPhrase}. Code:'{errorContent?.Error?.Code}' Type:'{errorContent?.Error?.Type}' Message:'{errorContent?.Error?.Message}'";
                Console.WriteLine(errMessage);
                throw new Exception(errMessage);
            }

            var fullResponse = await response.Content.ReadFromJsonAsync<GptResponse>();
            var generatedMessage = fullResponse.Choices[0].Message.Content.Trim();
            AddAssistantMessage(channelId, generatedMessage);

            return generatedMessage;
        }

        private void InitConversation(ulong channelId, string personaName = null)
        {
            if (!_conversations.ContainsKey(channelId))
            {
                var systemMessage = _personaService.GetPersonaValue(personaName);

                _conversations[channelId] = new List<GptMessage>
                {
                    new GptMessage { Role = GptSystemName, Content = systemMessage }
                };

            }
        }

        private void AddUserMessage(ulong channelId, string message)
        {
            _conversations[channelId].Add(new GptMessage { Role = "user", Content = message });
        }

        private GptRequest CreateGptRequest(ulong channelId)
        {
            return new GptRequest
            {
                Model = _gptSettings.Model, // "gpt-3.5-turbo",//gpt-4
                Messages = _conversations[channelId],
                Temperature = _gptSettings.Temperature,
                MaxTokens = _gptSettings.MaxTokens,
                TopP = _gptSettings.TopP,
                FrequencyPenalty = _gptSettings.FrequencyPenalty,
                PresencePenalty = _gptSettings.PresencePenalty,
                Stream = false
            };
        }

        private StringContent SerializeGptRequest(GptRequest requestData)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
            var jsonContent = JsonConvert.SerializeObject(requestData, serializerSettings);
            return new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }

        private void AddAssistantMessage(ulong channelId, string generatedMessage)
        {
            _conversations[channelId].Add(new GptMessage { Role = "assistant", Content = generatedMessage });
        }

        public void SetConversationPersona(ulong channelId, string selectedPersona)
        {
            if (!_conversations.ContainsKey(channelId))
            {
                InitConversation(channelId, selectedPersona);
                return;
            }

            var newChannelConversation = _conversations[channelId]
                .Where(x => x.Role != GptSystemName)
                .ToList();

            var systemMessage = _personaService.GetPersonaValue(selectedPersona);
            var message = new GptMessage { Role = GptSystemName, Content = systemMessage };
            newChannelConversation.Insert(0, message);
            _conversations[channelId] = newChannelConversation;
        }
    }
}
