﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking.Async;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.ReportSystem;

namespace Uncreated.Warfare.Commands;

public class ReportCommand : AsyncCommand
{
    private const string SYNTAX = "/report <\"reasons\" | player> <reason> <custom message...>";
    private const string HELP = "Use to report a player for specific actions. Use /report reasons for examples.";
    public ReportCommand() : base("report", EAdminType.MEMBER) { }
    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        // /report john greifing keeps using the mortar on the fobs 
        // /report john teamkilling teamkilled 5 teammates

        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);
        ctx.AssertArgs(2, SYNTAX);

        if (!UCWarfare.CanUseNetCall || !UCWarfare.Config.EnableReporter)
            throw ctx.Reply(T.ReportNotConnected);

        EReportType type;
        string message;
        ulong target;
        if (ctx.HasArgsExact(2))
        {
            string inPlayer = ctx.Get(0)!;

            if (ctx.MatchParameter(1, "help"))
                goto Help;
            if (ctx.MatchParameter(1, "reports", "reasons", "types"))
                goto Types;

            bool linked = await CheckLinked(ctx.Caller, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            if (!linked)
                goto DiscordNotLinked;

            message = string.Empty;
            type = GetReportType(ctx.Get(1)!);
            if (type == EReportType.CUSTOM)
                goto Help;
            if (!(inPlayer.Length == 17 && inPlayer.StartsWith("765") && ulong.TryParse(inPlayer, NumberStyles.Any, Data.Locale, out target)))
            {
                UCPlayer.ENameSearchType search = GetNameType(type);
                target = Data.Reporter.RecentPlayerNameCheck(inPlayer, search);
                if (target == 0)
                    goto PlayerNotFound;
            }
        }
        else
        {
            string inPlayer = ctx.Get(0)!;

            bool linked = await CheckLinked(ctx.Caller, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            if (!linked)
                goto DiscordNotLinked;

            type = GetReportType(ctx.Get(1)!);
            message = type == EReportType.CUSTOM ? ctx.GetRange(1)! : ctx.GetRange(2)!;

            if (!(inPlayer.Length == 17 && inPlayer.StartsWith("765") && ulong.TryParse(inPlayer, System.Globalization.NumberStyles.Any, Data.Locale, out target)))
            {
                UCPlayer.ENameSearchType search = GetNameType(type);
                UCPlayer? temptarget = UCPlayer.FromName(inPlayer, search);
                target = temptarget == null ? Data.Reporter.RecentPlayerNameCheck(inPlayer, search) : temptarget.Steam64;
                if (target == 0)
                    goto PlayerNotFound;
            }
        }
        
        if (!UCWarfare.CanUseNetCall)
        {
            ctx.Reply(T.ReportNotConnected);
            return;
        }
        FPlayerName targetNames = await F.GetPlayerOriginalNamesAsync(target, token).ThenToUpdate(token);

        if (CooldownManager.HasCooldownNoStateCheck(ctx.Caller, ECooldownType.REPORT, out Cooldown cd) && cd.data.Length > 0 && cd.data[0] is ulong ul && ul == target)
        {
            ctx.Reply(T.ReportCooldown, targetNames);
            return;
        }

        ctx.Reply(T.ReportConfirm, target, targetNames);
        ctx.LogAction(EActionLogType.START_REPORT, string.Join(", ", ctx.Parameters));
        bool didConfirm = await CommandWaiter.WaitAsync(ctx.Caller, "confirm", 10000);
        await UCWarfare.ToUpdate();
        if (!didConfirm)
        {
            ctx.Reply(T.ReportCancelled);
            return;
        }
        if (!UCWarfare.CanUseNetCall)
        {
            ctx.Reply(T.ReportNotConnected);
            return;
        }
        CooldownManager.StartCooldown(ctx.Caller, ECooldownType.REPORT, 3600f, target);
        Report? report = type switch
        {
            EReportType.CHAT_ABUSE => Data.Reporter.CreateChatAbuseReport(ctx.CallerID, target, message),
            EReportType.VOICE_CHAT_ABUSE => Data.Reporter.CreateVoiceChatAbuseReport(ctx.CallerID, target, message),
            EReportType.SOLOING_VEHICLE => Data.Reporter.CreateSoloingReport(ctx.CallerID, target, message),
            EReportType.WASTING_ASSETS => Data.Reporter.CreateWastingAssetsReport(ctx.CallerID, target, message),
            EReportType.INTENTIONAL_TEAMKILL => Data.Reporter.CreateIntentionalTeamkillReport(ctx.CallerID, target, message),
            EReportType.GREIFING_FOBS => Data.Reporter.CreateGreifingFOBsReport(ctx.CallerID, target, message),
            EReportType.CHEATING => Data.Reporter.CreateCheatingReport(ctx.CallerID, target, message),
            _ => Data.Reporter.CreateReport(ctx.CallerID, target, message),
        };
        if (report == null)
        {
            ctx.SendUnknownError();
            return;
        }

        SteamPlayer? targetPl = PlayerTool.getSteamPlayer(target);
        await Data.DatabaseManager.AddReport(report, token).ConfigureAwait(false);
        await UCWarfare.ToUpdate();
        string typename = GetName(type);
        NotifyAdminsOfReport(targetNames, ctx.Caller.Name, report, typename);
        ctx.Reply(T.ReportSuccessMessage1, targetNames, string.IsNullOrEmpty(message) ? "---" : message, typename);
        ctx.Reply(T.ReportSuccessMessage2);
        L.Log($"{ctx.Caller.Name.PlayerName} ({ctx.CallerID}) reported {targetNames.PlayerName} ({target}) for \"{report.Message}\" as a {typename} report.", ConsoleColor.Cyan);
        byte[] jpgData =
            targetPl == null || (type != EReportType.CUSTOM && type < EReportType.SOLOING_VEHICLE)
                ? Array.Empty<byte>()
                : await SpyTask.RequestScreenshot(targetPl);
        report.JpgData = jpgData;
        if (!UCWarfare.CanUseNetCall)
        {
            ctx.Reply(T.ReportNotConnected);
            return;
        }
        RequestResponse res = await Reporter.NetCalls.SendReportInvocation.Request(
            Reporter.NetCalls.ReceiveInvocationResponse, UCWarfare.I.NetClient!, report, targetPl != null);
        await UCWarfare.ToUpdate();
        if (targetPl != null && targetPl.player != null)
        {
            if (!Data.Languages.TryGetValue(target, out string lang))
                lang = L.DEFAULT;
            ToastMessage.QueueMessage(targetPl, new ToastMessage(T.ReportNotifyViolatorToast.Translate(lang, typename, UCPlayer.FromID(target)), EToastMessageSeverity.SEVERE));
            targetPl.SendChat(T.ReportNotifyViolatorMessage1, typename, message);
            targetPl.SendChat(T.ReportNotifyViolatorMessage2);
        }

        FPlayerName names = await F.GetPlayerOriginalNamesAsync(target);

        if (res.Responded && res.Parameters.Length > 1 && res.Parameters[0] is bool success &&
            success && res.Parameters[1] is string messageUrl)
        {
            //await UCWarfare.ToUpdate();
            //F.SendURL(targetPl, Translation.Translate("report_popup", targetPl, typename), messageUrl);
            L.Log($"Report against {names.PlayerName} ({target}) record: \"{messageUrl}\".", ConsoleColor.Cyan);
            ActionLogger.Add(EActionLogType.CONFIRM_REPORT, report.ToString() + ", Report URL: " + messageUrl, ctx.Caller);
        }
        else
        {
            L.Log($"Report against {names.PlayerName} ({target}) failed to send to UCHB.", ConsoleColor.Cyan);
            ActionLogger.Add(EActionLogType.CONFIRM_REPORT, report.ToString() + ", Report did not reach the discord bot.", ctx.Caller);
        }
        return;
    PlayerNotFound:
        throw ctx.Reply(T.PlayerNotFound);
    DiscordNotLinked:
        throw ctx.Reply(T.ReportDiscordNotLinked, ctx.Caller);
    Help:
        ctx.SendCorrectUsage(SYNTAX + " - " + HELP);
    Types: // not returning here is intentional
        throw ctx.Reply(T.ReportReasons);
    }

    public static readonly KeyValuePair<string, EReportType>[] ReportTypeAliases =
    {
        new KeyValuePair<string, EReportType>("custom",                  EReportType.CUSTOM),
        new KeyValuePair<string, EReportType>("none",                    EReportType.CUSTOM),
        new KeyValuePair<string, EReportType>("chat abuse",              EReportType.CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("racism",                  EReportType.CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("n word",                  EReportType.CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("chat",                    EReportType.CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("chat racism",             EReportType.CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("voice chat abuse",        EReportType.VOICE_CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("voice chat",              EReportType.VOICE_CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("voice chat racism",       EReportType.VOICE_CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("vc abuse",                EReportType.VOICE_CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("vc racism",               EReportType.VOICE_CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("vc",                      EReportType.VOICE_CHAT_ABUSE),
        new KeyValuePair<string, EReportType>("soloing",                 EReportType.SOLOING_VEHICLE),
        new KeyValuePair<string, EReportType>("solo",                    EReportType.SOLOING_VEHICLE),
        new KeyValuePair<string, EReportType>("soloing vehicles",        EReportType.SOLOING_VEHICLE),
        new KeyValuePair<string, EReportType>("asset waste",             EReportType.WASTING_ASSETS),
        new KeyValuePair<string, EReportType>("asset wasteing",          EReportType.WASTING_ASSETS),
        new KeyValuePair<string, EReportType>("wasteing assets",         EReportType.WASTING_ASSETS),
        new KeyValuePair<string, EReportType>("asset wasting",           EReportType.WASTING_ASSETS),
        new KeyValuePair<string, EReportType>("wasting assets",          EReportType.WASTING_ASSETS),
        new KeyValuePair<string, EReportType>("intentional teamkilling", EReportType.INTENTIONAL_TEAMKILL),
        new KeyValuePair<string, EReportType>("teamkilling",             EReportType.INTENTIONAL_TEAMKILL),
        new KeyValuePair<string, EReportType>("teamkill",                EReportType.INTENTIONAL_TEAMKILL),
        new KeyValuePair<string, EReportType>("tk",                      EReportType.INTENTIONAL_TEAMKILL),
        new KeyValuePair<string, EReportType>("tking",                   EReportType.INTENTIONAL_TEAMKILL),
        new KeyValuePair<string, EReportType>("intentional",             EReportType.INTENTIONAL_TEAMKILL),
        new KeyValuePair<string, EReportType>("fob greifing",            EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("structure greifing",      EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("base greifing",           EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("hab greifing",            EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("greifing",                EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("fob griefing",            EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("structure griefing",      EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("base griefing",           EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("hab griefing",            EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("griefing",                EReportType.GREIFING_FOBS),
        new KeyValuePair<string, EReportType>("cheating",                EReportType.CHEATING),
        new KeyValuePair<string, EReportType>("hacking",                 EReportType.CHEATING),
        new KeyValuePair<string, EReportType>("wallhacks",               EReportType.CHEATING),
        new KeyValuePair<string, EReportType>("hacks",                   EReportType.CHEATING),
        new KeyValuePair<string, EReportType>("cheats",                  EReportType.CHEATING),
        new KeyValuePair<string, EReportType>("hacker",                  EReportType.CHEATING),
        new KeyValuePair<string, EReportType>("cheater",                 EReportType.CHEATING)
    };
    public UCPlayer.ENameSearchType GetNameType(EReportType type)
    {
        return type switch
        {
            EReportType.CUSTOM or EReportType.INTENTIONAL_TEAMKILL or EReportType.GREIFING_FOBS or EReportType.SOLOING_VEHICLE or EReportType.VOICE_CHAT_ABUSE or EReportType.WASTING_ASSETS or EReportType.CHEATING => UCPlayer.ENameSearchType.NICK_NAME,
            EReportType.CHAT_ABUSE => UCPlayer.ENameSearchType.CHARACTER_NAME,
            _ => UCPlayer.ENameSearchType.CHARACTER_NAME,
        };
    }
    public string GetName(EReportType type)
    {
        return type switch
        {
            EReportType.CHAT_ABUSE => "Chat Abuse / Racism",
            EReportType.VOICE_CHAT_ABUSE => "Voice Chat Abuse / Racism",
            EReportType.SOLOING_VEHICLE => "Soloing Vehicle",
            EReportType.WASTING_ASSETS => "Wasting Assets / Vehicle Greifing",
            EReportType.INTENTIONAL_TEAMKILL => "Intentional Teamkilling",
            EReportType.GREIFING_FOBS => "FOB / Friendly Structure Greifing",
            EReportType.CHEATING => "Cheating",
            _ => "Custom",
        };
    }
    public EReportType GetReportType(string input)
    {
        for (int i = 0; i < ReportTypeAliases.Length; ++i)
        {
            ref KeyValuePair<string, EReportType> type = ref ReportTypeAliases[i];
            if (type.Key.Equals(input, StringComparison.OrdinalIgnoreCase))
                return type.Value;
        }
        return EReportType.CUSTOM;
    }
    public async Task<bool> CheckLinked(UCPlayer player, CancellationToken token) =>
        (await Data.DatabaseManager.GetDiscordID(player.Steam64, token).ConfigureAwait(false)) != 0;
    public void NotifyAdminsOfReport(FPlayerName violator, FPlayerName reporter, Report report, string typename)
    {
        foreach (LanguageSet set in LanguageSet.OfPermission(EAdminType.MODERATOR))
        {
            string translation = T.ReportNotifyAdmin.Translate(set.Language, reporter, violator, report.Message!, typename);
            while (set.MoveNext())
            {
                ToastMessage.QueueMessage(set.Next, new ToastMessage(translation, EToastMessageSeverity.INFO));
            }
        }
    }
}
