using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Watchman.Modules
{
    [Group("verify", "manage verifications")]
    [DefaultPermission(false)]
    public class SlashCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public SocketRole VerifiedRole
               => Context.Guild.GetRole(935302008330653777);

        public SocketRole BotRole
               => Context.Guild.GetRole(935302116799565825);

        public SocketTextChannel LogsChannel
            => (Context.Guild.GetChannel(935492417963065354) as SocketTextChannel)!;

        [SlashCommand("add", "Verify a user")]
        public async Task VerifyAsync(
            [Summary("user", "The user id or mention to verify")]
            string rawUser,
            [Summary("reason", "The reason to verify this user")]
            string reason
            )
        {
            var userId = ResolveUser(rawUser);

            if(!userId.HasValue)
            {
                await RespondAsync($"The user \"{rawUser}\" was not reconized as a mention or a user id, sorry...", ephemeral: true);
                return;
            }

            await DeferAsync(true);

            // check if they're in the server

            var user = await Context.Client.Rest.GetGuildUserAsync(935301596793958490, userId.Value);

            if(user != null && user.IsBot && !user.RoleIds.Contains(BotRole.Id))
            {
                await user.AddRoleAsync(BotRole, new RequestOptions() { AuditLogReason = reason });

                await FollowupAsync("Successfully verified the bot", ephemeral: true);

                var log = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle("Verification Log")
                    .WithDescription($"Verification log for the bot {user}")
                    .AddField("Verified by", Context.User.ToString())
                    .AddField("Reason", reason)
                    .AddField("State", "Approved");

                await LogsChannel.SendMessageAsync(embed: log.Build());
                return;
            }

            if(user != null && user.RoleIds.Contains(VerifiedRole.Id))
            {
                var verifiedLog = ShittyDB.Current?.Get(userId.Value);

                var verifyingUser = verifiedLog.HasValue ? await Context.Client.Rest.GetGuildUserAsync(935301596793958490, verifiedLog.Value.Verification.UserId) : null;

                var embed = verifiedLog.HasValue
                    ? new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithTitle("Verification Log")
                        .WithDescription($"Verification log for {user}")
                        .AddField("Verified by", verifyingUser?.ToString() ?? $"<@{verifiedLog.Value.Verification.VerifiedBy}> ({verifiedLog.Value.Verification.VerifiedBy})")
                        .AddField("Reason", verifiedLog.Value.Verification.Reason)
                        .AddField("State", verifiedLog.Value.State)
                    : null;

                await FollowupAsync("That users already verified!", embed: embed?.Build(), ephemeral: true);
                return;
            }

            if(user != null)
            {
                await user.AddRoleAsync(VerifiedRole, new Discord.RequestOptions() { AuditLogReason = reason});
                ShittyDB.Modify(x => x?.Approved.Add(new Verification() { Reason = reason, UserId = userId.Value, VerifiedBy = Context.User.Id }));

                var log = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle("Verification Log")
                    .WithDescription($"Verification log for {user}")
                    .AddField("Verified by", Context.User.ToString())
                    .AddField("Reason", reason)
                    .AddField("State", "Approved");

                await LogsChannel.SendMessageAsync(embed: log.Build());

                await FollowupAsync($"Success, {user} is now verified", ephemeral: true);
            }
            else
            {
                ShittyDB.Modify(x => x?.Pending.Add(new Verification() { Reason = reason, UserId = userId.Value, VerifiedBy = Context.User.Id }));
                var log = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithTitle("Verification Log")
                    .WithDescription($"Verification log for {(user?.ToString() ?? $"<@{userId.Value}> ({userId.Value})")}")
                    .AddField("Verified by", Context.User.ToString())
                    .AddField("Reason", reason)
                    .AddField("State", "Pending");

                await LogsChannel.SendMessageAsync(embed: log.Build());

                await FollowupAsync($"Success, {(user?.ToString() ?? $"<@{userId.Value}> ({userId.Value})")} will be verified when they join", ephemeral: true);
            }
        }
        
        [SlashCommand("remove", "Remove a pending verification")]
        public async Task RemoveAsync(
            [Summary("user", "The user id or mention to remove")]
            string rawUser,
            [Summary("reason", "The reason to remove this user from verification")]
            string reason)
        {
            var userId = ResolveUser(rawUser);

            if (!userId.HasValue)
            {
                await RespondAsync($"The user \"{rawUser}\" was not reconized as a mention or a user id, sorry...", ephemeral: true);
                return;
            }

            var pendingVerification = ShittyDB.Current?.Pending.FirstOrDefault(x => x.UserId == userId.Value);

            if(pendingVerification == null)
            {
                await RespondAsync($"It doesn't seem like this person is awaiting verification and therefor cannot be removed...", ephemeral: true);
                return;
            }

            ShittyDB.Modify(x => x?.Pending.Remove(pendingVerification));

            await RespondAsync("Success, the pending verification was removed.", ephemeral: true);

            var verifier = await Context.Client.Rest.GetGuildUserAsync(935301596793958490, pendingVerification.VerifiedBy!.Value);

            await LogsChannel.SendMessageAsync(embed: new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Pending verification removed")
                .WithDescription($"{Context.User} removed a pending verification")
                .AddField("Pending user id", pendingVerification.UserId)
                .AddField("Pending user verifier", verifier?.ToString() ?? $"<@{pendingVerification.VerifiedBy}> ({pendingVerification.VerifiedBy})")
                .AddField("Verification reason", pendingVerification.Reason)
                .AddField("Revokation reason", reason)
                .Build());
        }

        [SlashCommand("status", "Get the status of a verification")]
        public async Task StatusAsync(
            [Summary("user", "The user id or mention to get the status of")]
            string rawUser)
        {
            var userId = ResolveUser(rawUser);

            if (!userId.HasValue)
            {
                await RespondAsync($"The user \"{rawUser}\" was not reconized as a mention or a user id, sorry...", ephemeral: true);
                return;
            }

            var result = ShittyDB.Current?.Get(userId.Value);

            if (!result.HasValue)
            {
                await RespondAsync($"Couldn't find any verification details for that user.. blame the shitty database :/", ephemeral: true);
                return;
            }

            await DeferAsync();

            var user = await Context.Client.Rest.GetGuildUserAsync(935301596793958490, result.Value.Verification.UserId);
            var verifier = await Context.Client.Rest.GetGuildUserAsync(935301596793958490, result.Value.Verification.VerifiedBy!.Value);

            var embed = new EmbedBuilder()
                        .WithColor(Color.Green)
                        .WithTitle("Verification Log")
                        .WithDescription($"Verification log for {(user?.ToString() ?? $"<@{result.Value.Verification.UserId}> ({result.Value.Verification.UserId})")}")
                        .AddField("Verified by", verifier?.ToString() ?? $"<@{result.Value.Verification.VerifiedBy}> ({result.Value.Verification.VerifiedBy})")
                        .AddField("Reason", result.Value.Verification.Reason)
                        .AddField("State", result.Value.State);

            await FollowupAsync(embed: embed.Build());
        }

        private ulong? ResolveUser(string user)
        {
            if (ulong.TryParse(user, out var result))
                return result;

            var r = Regex.Match(user, @"<@!*(&*[0-9]+)>");

            if (r.Success)
                return ulong.Parse(r.Groups[1].Value);

            return null;
        }


    }
}
