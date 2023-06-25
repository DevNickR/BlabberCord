using Microsoft.Extensions.Logging;

namespace BlabberCord.Services
{
    public class PersonaService
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, string> _personas = new Dictionary<string, string>();
        private const string _defaultPersonaName = "default";

        public PersonaService(ILogger<PersonaService> logger)
        {
            _logger = logger;
            // Load default persona
            _personas[_defaultPersonaName] = "You are a helpful assistant.";

            // Load personas from text files in the specified folder path
            var directory = new DirectoryInfo("./Personas");
            foreach (var file in directory.GetFiles("*.txt"))
            {
                _logger.LogDebug($"Found persona file:{file.FullName}");
                var name = Path.GetFileNameWithoutExtension(file.Name);
                var value = File.ReadAllText(file.FullName);
                _personas[name] = value;
            }
        }

        public string GetPersonaValue(string name = null)
        {
            if (!string.IsNullOrEmpty(name) && _personas.TryGetValue(name, out var value))
            {
                return value;
            }

            return _personas[_defaultPersonaName];
        }

        public async Task AddPersona(string name, string prompt)
        {
            // Ensure the "Personas" folder exists in the current directory
            string folderPath = Path.Combine(Environment.CurrentDirectory, "Personas");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Combine the folder path with the user-provided filename to create the full file path
            string filePath = Path.Combine(folderPath, $"{name}.txt");

            // Write the content to the file, overwriting it if it already exists
            await File.WriteAllTextAsync(filePath, name);

            _logger.LogDebug($"Prompt saved to '{filePath}'");
            _personas.Add(name, prompt);
        }

        public List<string> GetPersonaNames()
        {
            return _personas.Keys.ToList();
        }
    }
}