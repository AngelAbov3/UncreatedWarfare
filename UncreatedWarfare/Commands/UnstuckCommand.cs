﻿using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("unstuck")]
[MetadataFile(nameof(GetHelpMetadata))]
public class UnstuckCommand : IExecutableCommand
{
    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Run this command if you're somehow stuck in the lobby."
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertGamemode(out ITeams t);

        if (!t.UseTeamSelector) throw Context.SendGamemodeError();

        if (TeamManager.LobbyZone.IsInside(Context.Player.Position))
        {
            t.TeamSelector?.ResetState(Context.Player);
            Context.ReplyString("Reset lobby state.");
        }
        else Context.ReplyString("You're not in the lobby.");

        return default;
    }
}
