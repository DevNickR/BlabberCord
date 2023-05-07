namespace BlabberCord.Models.Gpt
{
    public class GptRequest
    {
        public string Model { get; set; }
        public List<GptMessage> Messages { get; set; }
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public double TopP { get; set; }
        public double FrequencyPenalty { get; set; }
        public double PresencePenalty { get; set; }
        public bool Stream { get; set; }
    }
}
