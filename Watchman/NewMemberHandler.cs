using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Watchman
{
    public class AuthorizedUsers
    {
        [JsonProperty("userId")]
        public ulong UserId { get; set; }

        [JsonProperty("isStaff")]
        public bool IsStaff { get; set; }
    }

    public class AuthorizedGuilds
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("guildId")]
        public ulong Id { get; set; }

        [JsonProperty("users")]
        public List<AuthorizedUsers> Users { get; set; } = new();
    }

    public class NewMemberHandler
    {
        public List<AuthorizedGuilds> AuthorizedGuilds { get; set; }

        private readonly DiscordSocketClient _client;

        public SocketGuild Guild
            => _client.GetGuild(935301596793958490);
        public SocketRole EmployeeRole
            => Guild.GetRole(935306998927478795);
        public SocketRole VerifiedRole
            => Guild.GetRole(935302008330653777);
        public SocketTextChannel LogsChannel
           => (Guild.GetChannel(935492417963065354) as SocketTextChannel)!;

        public NewMemberHandler(DiscordSocketClient client)
        {
            _client = client;

            _client.UserJoined += _client_UserJoined;

            AuthorizedGuilds = JsonConvert.DeserializeObject<List<AuthorizedGuilds>>(File.ReadAllText("./external/DevTestServerUsers.json"))!;
        }

        private async Task _client_UserJoined(SocketGuildUser arg)
        {
            if ((arg.PublicFlags & UserProperties.Staff) != 0)
            {
                await arg.AddRoleAsync(EmployeeRole, new RequestOptions() { AuditLogReason = "Staff bypass kekw"});

                await PostLogAsync(arg, "Staff bypass kekw");

                ShittyDB.Modify(x =>
                {
                    x?.Pending.RemoveAll(y => y.UserId == arg.Id);
                    x?.Approved.Add(new Verification() { Reason = "Staff bypass kekw", UserId = arg.Id, VerifiedBy = _client.CurrentUser.Id });
                });
                return;
            }

            var result = ShittyDB.Current?.Pending.FirstOrDefault(x => x.UserId == arg.Id);

            if (result != null)
            {
                await arg.AddRoleAsync(VerifiedRole, new RequestOptions() { AuditLogReason = result.Reason });

                await PostLogAsync(arg, result.Reason, result.VerifiedBy);

                ShittyDB.Modify(x =>
                {
                    x?.Pending.Remove(result);
                    x?.Approved.Add(result);
                });
            }
            else if (AuthorizedGuilds.Any(x => x.Users.Any(y => y.UserId == arg.Id)))
            {
                var guild = AuthorizedGuilds.FirstOrDefault(x => x.Users.Any(y => y.UserId == arg.Id))!;
                var user = guild.Users.FirstOrDefault(x => x.UserId == arg.Id)!;

                var reason = $"User in {guild.Name}";

                await arg.AddRoleAsync(VerifiedRole, new RequestOptions() { AuditLogReason = reason });

                await PostLogAsync(arg, reason);

                ShittyDB.Modify(x =>
                {
                    x?.Pending.RemoveAll(y => y.UserId == arg.Id);
                    x?.Approved.Add(new Verification() { Reason = reason, UserId = arg.Id, VerifiedBy = _client.CurrentUser.Id });
                });
            }

        }

        private async Task PostLogAsync(IGuildUser user, string? reason, ulong? verifiedBy = null)
        {
            verifiedBy ??= _client.CurrentUser.Id;

            var verifier = await _client.GetUserAsync(verifiedBy.Value);

            var log = new EmbedBuilder()
                   .WithColor(Color.Green)
                   .WithTitle("Verification Log")
                   .WithDescription($"Verification log for the user {user}")
                   .AddField("Verified by", verifier)
                   .AddField("Reason", reason)
                   .AddField("State", "Approved");

            await LogsChannel.SendMessageAsync(embed: log.Build());
        }
    }
}
