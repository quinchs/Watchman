using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watchman
{
    internal class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactionService;
        private readonly Logger _log;
        private readonly NewMemberHandler _memberHandler;

        public CommandHandler(DiscordSocketClient client, InteractionService service)
        {
            _log = Logger.GetLogger<CommandHandler>();

            _client = client;

            _interactionService = service;

            _client.Ready += _client_Ready;

            _client.InteractionCreated += _client_InteractionCreated;

            _memberHandler = new(client);
        }

        private async Task _client_InteractionCreated(SocketInteraction arg)
        {
            await _interactionService.ExecuteCommandAsync(new SocketInteractionContext(_client, arg), null);
        }

        private async Task _client_Ready()
        {
            var commands = await _interactionService.RegisterCommandsToGuildAsync(935301596793958490);

            // add permissions
            await _client.Rest.BatchEditGuildCommandPermissions(935301596793958490, commands.ToDictionary(x => x.Id, x => new Discord.ApplicationCommandPermission[] { new Discord.ApplicationCommandPermission(935302008330653777, Discord.ApplicationCommandPermissionTarget.Role, true) }));
        }
    }
}
