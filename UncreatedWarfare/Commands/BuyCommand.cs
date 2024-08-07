﻿using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Translations;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("buy")]
[MetadataFile(nameof(GetHelpMetadata))]
public class BuyCommand : IExecutableCommand
{
    private readonly RequestTranslations _translations;
    const string Help = "Must be looking at a kit request sign. Purchases a kit for credits.";
    const string Syntax = "/buy [help]";

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public BuyCommand(TranslationInjection<RequestTranslations> translations)
    {
        _translations = translations.Value;
    }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = Help
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (Context.MatchParameter(0, "help"))
        {
            throw Context.SendCorrectUsage(Syntax + " - " + Help);
        }

        Context.AssertGamemode(out IKitRequests gm);

        KitManager manager = gm.KitManager;
        if (Context.Caller is null || (Data.Gamemode.State != State.Active && Data.Gamemode.State != State.Staging))
        {
            throw Context.SendUnknownError();
        }

        if (!Context.TryGetBarricadeTarget(out BarricadeDrop? drop) || drop.interactable is not InteractableSign)
        {
            throw Context.Reply(_translations.RequestNoTarget);
        }

        if (Signs.GetKitFromSign(drop, out int ld) is { } kit)
        {
            await manager.Requests.BuyKit(Context, kit, drop.model.position, token).ConfigureAwait(false);
            return;
        }

        if (ld <= -1)
            throw Context.Reply(_translations.RequestKitNotRegistered);

        if (UCWarfare.Config.WebsiteUri == null || Data.PurchasingDataStore.LoadoutProduct == null)
            throw Context.Reply(_translations.RequestNotBuyable);

        Context.Player.UnturnedPlayer.sendBrowserRequest("Purchase loadouts on our website.", new Uri(UCWarfare.Config.WebsiteUri, "kits/loadout").OriginalString);
        throw Context.Defer();
    }
}
