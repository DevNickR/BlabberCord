using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlabberCord.Models.Gpt
{
    public class GptSettings
    {
        public string ApiKey { get; set; }
        public string Model { get; set; }
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public double TopP { get; set; }
        public double FrequencyPenalty { get; set; }
        public double PresencePenalty { get; set; }
        public bool Stream { get; set; } = false;

        public GptSettings(string apiKey, string model, double temperature, int maxTokens, double topP, double frequencyPenalty, double presencePenalty)
        {
            ApiKey = apiKey;
            Model = model;
            Temperature = temperature;
            MaxTokens = maxTokens;
            TopP = topP;
            FrequencyPenalty = frequencyPenalty;
            PresencePenalty = presencePenalty;
        }
    }
}
