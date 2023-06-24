namespace BlabberCord.Models.Gpt
{
    public class GptResponse
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public int Created { get; set; }
        public string Model { get; set; }
        public GptUsage Usage { get; set; }
        public List<GptResponseChoice> Choices { get; set; }
    }

    public class GptErrorResponse
    {
        public GptError Error { get; set; }
    }

    public class GptError
    {
        public string Message { get; set; }
        public string Type { get; set; }
        public string Param { get; set; }
        public string Code { get; set; }
    }

    public class GptResponseChoice
    {
        public GptMessage Message { get; set; }
        public string FinishReason { get; set; }
        public int Index { get; set; }
    }

    public class GptUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
