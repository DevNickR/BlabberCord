using BlabberCord.Services;
using Discord.Commands;

namespace BlabberCord.Modules
{
    public class GptModule : ModuleBase<SocketCommandContext>
    {
        private readonly GptService _gptService;

        public GptModule(GptService gptService)
        {
            _gptService = gptService;
        }

        [Command("gpt")]
        [Summary("Generates a GPT response for the given input.")]
        public async Task GptAsync([Remainder] string input)
        {
            string response = await _gptService.GenerateResponseAsync(Context.Channel.Id, input);
            await ReplyAsync(response);
        }

        [Command("reset")]
        [Summary("Resets the GPT conversation context.")]
        public async Task ResetAsync()
        {
            _gptService.ResetChannelMessages(Context.Channel.Id);
            await ReplyAsync("Conversation context has been reset.");
        }
    }
}
