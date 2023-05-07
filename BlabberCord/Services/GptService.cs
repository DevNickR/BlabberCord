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
        private readonly string _gptApiKey;
        private readonly string _gptModel;
        private readonly string _apiEndpoint;

        private readonly Dictionary<ulong, List<GptMessage>> _conversations = new Dictionary<ulong, List<GptMessage>>();

        private const string GptSystemName = "system";


        public GptService(IConfiguration configuration, PersonaService personasService)
        {
            var apiKey = configuration["Gpt:ApiKey"];
            var model = configuration["Gpt:Model"];
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(model)) throw new ArgumentNullException("Please set \"Gpt:ApiKey\" and \"Gpt:Model\" in appsettings.json or environment variables");

            _gptApiKey = apiKey;
            _gptModel = model;
            _personaService = personasService;
            _apiEndpoint = "https://api.openai.com/v1/chat/completions";
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_gptApiKey}");
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
                Console.WriteLine($"GPT API request failed: {response.ReasonPhrase}");
                throw new Exception($"GPT API request failed: {response.ReasonPhrase}");
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
                Model = _gptModel,// "gpt-3.5-turbo",//gpt-4
                Messages = _conversations[channelId],
                Temperature = 0.7,
                MaxTokens = 2000,
                TopP = 1,
                FrequencyPenalty = 1,
                PresencePenalty = 1,
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
