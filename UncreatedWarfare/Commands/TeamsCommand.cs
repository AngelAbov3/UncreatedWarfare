﻿using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players.Permissions;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands;

[Command("teams", "team")]
[MetadataFile(nameof(GetHelpMetadata))]
public class TeamsCommand : IExecutableCommand
{
    private const string Syntax = "/teams ";
    private const string Help = "Switch teams without rejoining the server.";

    private static readonly PermissionLeaf PermissionShuffle = new PermissionLeaf("commands.teams.shuffle", unturned: false, warfare: true);

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help,
            Parameters =
            [
                new CommandParameter("Shuffle")
                {
                    Aliases = [ "sh" ],
                    Description = "Force the teams to be shuffled next game.",
                    Permission = PermissionShuffle,
                    IsOptional = true
                }
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        Context.AssertRanByPlayer();

        Context.AssertGamemode(out ITeams teamgm);
        if (Data.Is(out IImplementsLeaderboard<BasePlayerStats, BaseStatTracker<BasePlayerStats>> il) && il.IsScreenUp)
            throw Context.SendUnknownError();

        if (!teamgm.UseTeamSelector || teamgm.TeamSelector is null)
            throw Context.SendGamemodeError();

        if (Context.MatchParameter(0, "shuffle", "sh"))
        {
            await Context.AssertPermissions(PermissionShuffle, token);
            await UniTask.SwitchToMainThread(token);

            TeamSelector.ShuffleTeamsNextGame = true;
            throw Context.Reply(T.TeamsShuffleQueued);
        }

        if (!Context.Player.OnDuty() && CooldownManager.HasCooldown(Context.Player, CooldownType.ChangeTeams, out Cooldown cooldown))
        {
            throw Context.Reply(T.TeamsCooldown, cooldown);
        }

        ulong team = Context.Player.GetTeam();
        if (team is 1 or 2 && !Context.Player.UnturnedPlayer.IsInMain())
        {
            throw Context.Reply(T.NotInMain);
        }

        teamgm.TeamSelector!.JoinSelectionMenu(Context.Player, TeamSelector.JoinTeamBehavior.KeepTeam);
        throw Context.Defer();
    }
}
