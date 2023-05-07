
# Blabber-Cord

Blabber-Cord is a personal project, which is an AI-powered Discord bot using OpenAI's GPT-3.5-turbo and GPT-4.0 models. Others are free to use, fork, or run it as they please.

## Running the bot with Docker

To run the bot using Docker, use the following command:

```
docker run -d --name blabber-cord \
  -e Gpt:ApiKey=YOUR_GPT_API_KEY \
  -e Discord:Token=YOUR_DISCORD_TOKEN \
  -e Gpt:Model=MODEL_NAME \
  nrivett/blabber-cord:latest
```
Replace `YOUR_GPT_API_KEY` , `YOUR_DISCORD_TOKEN`  and `MODEL_NAME`. Alternatively you can mount and edit your own `appsettings.json` file within the container. 
`Gpt:Model` supports `gpt-3.5-turbo` and `gpt-4` and will default to gpt-4 if not specified.

## Getting a GPT API key

To obtain a GPT API key, you need to sign up for an account on the [OpenAI website](https://www.openai.com/). Once you have an account, you can access your API key from the API Keys section of the OpenAI Dashboard.
For gpt-4 api access, you will need to apply for their [waitlist](https://openai.com/waitlist/gpt-4-api).

## Getting a Discord API token

To get a Discord API token, follow these steps:

1.  Go to the [Discord Developer Portal](https://discord.com/developers/applications).
2.  Log in to your Discord account.
3.  Click "New Application" in the top right corner.
4.  Name your application and click "Create."
5.  Click on the "Bot" tab in the left sidebar.
6.  Click "Add Bot" and confirm the action.
7.  In the "Bot" section, you will see your bot token. Click "Copy" to copy it to your clipboard.

## Inviting the bot to your server

To invite the bot to your server, follow these steps:

1.  Go to the [Discord Developer Portal](https://discord.com/developers/applications) and open the application you created for the bot.
2.  In the "OAuth2" tab in the left sidebar, scroll down to the "Scopes" section.
3.  Select the "bot" scope, and optionally, any other desired permissions.
4.  A URL will be generated in the "Scopes" section. Copy this URL and paste it into a new browser tab.
5.  Choose the server you want to add the bot to and click "Authorize." Follow the prompts to complete the process.

Once the bot is added to your server, it will be able to respond to messages using the GPT-3.5-turbo or GPT-4.0 models, depending on your configuration.

## Using the bot
The main method of communication is via direct message. However you can invite it the any room it has access to and chat with it by tagging it in a message. It will not response to channel messages without being tagged.
If you use this in a public discord room with many people, you may experience issues with the bot reaching its chat history limit. Excessive use could also increase your billing. Use at your own risk.

### Channel history
The bot maintains its history in memory. A restart will clear all of its memory. Each channel is a new discussion for it. So if you wish to maintain multiple threads at once, start new chat rooms in discord, invite it and discuss a single topic in that room.

### Commands
`/select-persona` will start the personas feature and allow you to select a persona. This will only apply to the channel its used in and will reset its chat history.
`/reset-conversation` will reset the chat history of the bot.
`/add-persona` given a name and a prompt this will save the prompt to your prompts folder (if this folder is not persisted this will not work, mount the Prompts folder to a volume if using docker), a restart will be required for this to allow you to use the new persona.
## Features
### Personas
The bot has a personas feature. This utilizes the system command available via chat gpt. This is an initial prompt that directs chat gpt. To use this feature a `.txt` file with the persona name must be placed in the `/Personas` directory.
For example: /Personas/Frank.txt
```
You are a helpful assistant named Frank
```
If running via docker, its recommended you mount this folder via a volume to persist the personas.
### Message splitting
Discord has limitations on the max message size allowed, because of this chat gpt will attempt to split messages when it deems it necessary. This will occur particularly when responses are extremely long, or when code snippets are sent. Currently this feature is limited as extremely long single lines or larger code snippets will still be split incorrectly.