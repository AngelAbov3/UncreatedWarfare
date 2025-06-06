using System;
using System.Globalization;
using System.Text;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Addons;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Signs;

[SignPrefix("vbs_")]
public class VehicleBaySignInstanceProvider : ISignInstanceProvider, IRequestable<VehicleSpawner>
{
    private static readonly StringBuilder LoadoutSignBuffer = new StringBuilder(230);

    private static readonly Color32 VbsBranchColor = new Color32(155, 171, 171, 255);
    private static readonly Color32 VbsNameColor = new Color32(255, 255, 255, 255);

    private readonly VehicleSpawnerService _spawnerService;
    private readonly ITranslationValueFormatter _valueFormatter;
    private readonly VehicleBaySignTranslations _translations;
    private Guid _fallbackGuid;
    private VehicleAsset? _fallbackAsset;
    private BarricadeDrop _barricade = null!;
    public uint BarricadeInstanceId => _barricade.instanceID;

    public VehicleSpawner? Spawn { get; private set; }
    public WarfareVehicleInfo? Vehicle { get; private set; }

    /// <inheritdoc />
    bool ISignInstanceProvider.CanBatchTranslate => Vehicle == null || Spawn == null || Vehicle.Class == Class.None;

    /// <inheritdoc />
    string ISignInstanceProvider.FallbackText => _fallbackAsset != null ? _fallbackAsset.vehicleName : _fallbackGuid.ToString("N", CultureInfo.InvariantCulture);

    public VehicleBaySignInstanceProvider(
        VehicleSpawnerService spawnerService,
        ITranslationValueFormatter valueFormatter,
        TranslationInjection<VehicleBaySignTranslations> translations)
    {
        _spawnerService = spawnerService;
        _valueFormatter = valueFormatter;
        _translations = translations.Value;
    }

    public void Initialize(BarricadeDrop barricade, string extraInfo, IServiceProvider serviceProvider)
    {
        if (Guid.TryParseExact(extraInfo, "N", out _fallbackGuid))
            _fallbackAsset = Assets.find<VehicleAsset>(_fallbackGuid);

        _barricade = barricade;
    }

    public string Translate(ITranslationValueFormatter formatter, IServiceProvider serviceProvider, LanguageInfo language, CultureInfo culture, WarfarePlayer? player)
    {
        Spawn ??= _spawnerService.GetSpawner(_barricade.instanceID);
        Vehicle ??= Spawn?.VehicleInfo;

        if (Vehicle == null || Spawn == null)
        {
            return _fallbackAsset?.vehicleName ?? _fallbackGuid.ToString("N", CultureInfo.InvariantCulture);
        }

        try
        {
            TranslateVehicleBaySign(LoadoutSignBuffer, Vehicle, Spawn, language, culture, player);
            return LoadoutSignBuffer.ToString();
        }
        finally
        {
            LoadoutSignBuffer.Clear();
        }
    }

    private void TranslateVehicleBaySign(StringBuilder bldr, WarfareVehicleInfo info, VehicleSpawner spawner, LanguageInfo language, CultureInfo culture, WarfarePlayer? player)
    {
        string name = info.ShortName ?? info.VehicleAsset.GetAsset()?.FriendlyName ?? info.VehicleAsset.ToString();
        bldr.AppendColorized(name, VbsNameColor)
            .Append('\n')
            .AppendColorized(_valueFormatter.FormatEnum(info.Branch, language), VbsBranchColor)
            .Append('\n');

        if (info.TicketCost > 0)
        {
            bldr.Append(_translations.VBSTickets.Translate(info.TicketCost, language, culture, TimeZoneInfo.Utc));
        }

        bldr.Append('\n');

        bool anyUnlockReq = false;
        foreach (UnlockRequirement req in info.UnlockRequirements)
        {
            if (player != null && req.CanAccessFast(player))
                continue;

            bldr.Append(req.GetSignText(player, language, culture));
            anyUnlockReq = true;
            break;
        }

        if (info.CreditCost > 0)
        {
            if (anyUnlockReq)
                bldr.Append(' ', 4);

            bldr.Append(_translations.VBSCreditCost.Translate(info.CreditCost, language, culture, TimeZoneInfo.Utc));
        }

        bldr.Append('\n');

        switch (spawner.State)
        {
            case VehicleSpawnerState.Destroyed:
                bldr.Append(_translations.VBSStateDead.Translate(spawner.GetRespawnDueTime(), language, culture, TimeZoneInfo.Utc));
                break;

            case VehicleSpawnerState.Deployed:
                bldr.Append(_translations.VBSStateActive.Translate(spawner.GetLocation(), language, culture, TimeZoneInfo.Utc));
                break;

            case VehicleSpawnerState.Idle:
                bldr.Append(_translations.VBSStateIdle.Translate(spawner.GetRespawnDueTime(), language, culture, TimeZoneInfo.Utc));
                break;
            case VehicleSpawnerState.LayoutDelayed:
                bldr.Append(_translations.VBSStateLayoutDelayed.Translate(spawner.GetLayoutDelayTimeLeft(), language, culture, TimeZoneInfo.Utc));
                break;
            case VehicleSpawnerState.LayoutDisabled:
                bldr.Append(_translations.VBSStateLayoutDisabled.Translate(language));
                break;
            case VehicleSpawnerState.Disposed:
                bldr.Append(_translations.VBSStateDisposed.Translate(language));
                break;
            case VehicleSpawnerState.Glitched:
                bldr.Append(_translations.VBSStateGlitched.Translate(language));
                break;

            default:
                bldr.Append(_translations.VBSStateReady.Translate(language));
                break;

        }
    }
}

public class VehicleBaySignTranslations : PropertiesTranslationCollection
{
    protected override string FileName => "Vehicle Bay Signs";

    [TranslationData("Displays the ticket cost on a vehicle bay sign.")]
    public readonly Translation<int> VBSTickets = new Translation<int>("<#ffffff>{0}</color> <#f0f0f0>Tickets</color>", TranslationOptions.TMProSign);

    [TranslationData("Displays the credit cost on a vehicle bay sign.", IsPriorityTranslation = false)]
    public readonly Translation<int> VBSCreditCost = new Translation<int>("<#b8ffc1>C</color> <#fff>{0}</color>", TranslationOptions.TMProSign);
    
    [TranslationData("Displays the state of the sign when the vehicle is ready to be requested.")]
    public readonly Translation VBSStateReady = new Translation("<#33cc33>Ready!</color> <#aaa><b>/request</b></color>");

    [TranslationData("Displays the state of the sign when the vehicle is destroyed.", Parameters = [ "Minutes", "Seconds" ], IsPriorityTranslation = false)]
    public readonly Translation<TimeSpan> VBSStateDead = new Translation<TimeSpan>("<#ff0000>{0}</color>", arg0Fmt: TimeAddon.Create(TimeSpanFormatType.CountdownMinutesSeconds));

    [TranslationData("Displays the state of the sign when the vehicle is in use.", Parameters = [ "Nearest location." ], IsPriorityTranslation = false)]
    public readonly Translation<string> VBSStateActive = new Translation<string>("<#ff9933>{0}</color>");

    [TranslationData("Displays the state of the sign when the vehicle was left idle on the field.", Parameters = [ "Minutes", "Seconds" ])]
    public readonly Translation<TimeSpan> VBSStateIdle = new Translation<TimeSpan>("<#ffcc00>Idle: {0}</color>", arg0Fmt: TimeAddon.Create(TimeSpanFormatType.CountdownMinutesSeconds));

    [TranslationData("Displays the state of the sign when the vehicle spawner is currently delayed by the current layout.")]
    public readonly Translation<TimeSpan> VBSStateLayoutDelayed = new Translation<TimeSpan>("<#7094dd>Delayed: {0}</color>", arg0Fmt: TimeAddon.Create(TimeSpanFormatType.CountdownMinutesSeconds));

    [TranslationData("Displays the state of the sign when the vehicle spawner is disabled in the current layout.")]
    public readonly Translation VBSStateLayoutDisabled = new Translation("<#798082>Disabled</color>");

    [TranslationData("Displays the state of the sign when the vehicle spawner is has been disposed and is no longer useable.")]
    public readonly Translation VBSStateDisposed = new Translation("<#798082>Disposed</color>");

    [TranslationData("Displays the state of the sign when the vehicle was gliched when it tried to spawn.")]
    public readonly Translation VBSStateGlitched = new Translation("<#ff0000>Error</color>");
}