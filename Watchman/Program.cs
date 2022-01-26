
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;
using Watchman;

Logger.AddStream(Console.OpenStandardOutput(), StreamType.StandardOut);
Logger.AddStream(Console.OpenStandardError(), StreamType.StandardError);

var logger = Logger.GetLogger<Program>();
ShittyDB.Load();

var token = Environment.GetEnvironmentVariable("BOT_TOKEN");

var client = new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = Discord.GatewayIntents.GuildMembers | GatewayIntents.Guilds,
    LogLevel = Discord.LogSeverity.Debug,
});

var logFunc = (LogMessage log) =>
{
    var msg = log.Message;

    if (log.Source.StartsWith("Audio ") && (msg?.StartsWith("Sent") ?? false))
        return Task.CompletedTask;

    Severity? sev = null;

    if (log.Source.StartsWith("Gateway"))
        sev = Severity.Socket;
    if (log.Source.StartsWith("Rest"))
        sev = Severity.Rest;

    logger.Write($"{log.Message}", sev.HasValue ? new Severity[] { sev.Value, log.Severity.ToLogSeverity() } : new Severity[] { log.Severity.ToLogSeverity() }, log.Exception);

    return Task.CompletedTask;
};


client.Log += logFunc;

await client.LoginAsync(Discord.TokenType.Bot, token);

await client.StartAsync();

var interactionService = new InteractionService(client);

interactionService.Log += logFunc;

await interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), null);

var commandHandler = new CommandHandler(client, interactionService);

await Task.Delay(-1);