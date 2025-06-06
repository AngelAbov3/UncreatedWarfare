using System.Globalization;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("inventory", "inv"), SubCommandOf(typeof(ClearCommand))]
internal sealed class ClearInventoryCommand : IExecutableCommand
{
    private readonly ClearTranslations _translations;

    public required CommandContext Context { get; init; }

    public ClearInventoryCommand(TranslationInjection<ClearTranslations> translations)
    {
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        (CSteamID? steamId, WarfarePlayer? pl) = await Context.TryGetPlayer(1).ConfigureAwait(false);
        
        await UniTask.SwitchToMainThread(token);

        if (steamId.HasValue || Context.HasArgs(2))
        {
            // clear inv <player>
            if (pl == null)
                throw Context.SendPlayerNotFound();
            
            ItemUtility.ClearInventoryAndSlots(pl);

            // todo: Context.LogAction(ActionLogType.ClearInventory, "CLEARED INVENTORY OF " + pl.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture));
            Context.Reply(_translations.ClearInventoryOther, pl);
        }
        else if (!Context.Caller.IsTerminal)
        {
            // clear inv
            ItemUtility.ClearInventoryAndSlots(Context.Player);
            // todo: Context.LogAction(ActionLogType.ClearInventory, "CLEARED PERSONAL INVENTORY");
            Context.Reply(_translations.ClearInventorySelf);
        }
        else
        {
            throw Context.Reply(_translations.ClearNoPlayerConsole);
        }
    }
}