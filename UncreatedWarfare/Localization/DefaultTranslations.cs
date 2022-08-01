﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uncreated.Framework;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Cache = Uncreated.Warfare.Components.Cache;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare;
internal static class T
{
    /*
     * c$value$ will be replaced by the color "value" on startup
     */

    #region Common Errors
    private const string SECTION_COMMON_ERRORS = "Common Errors";

        [TranslationData(Section = SECTION_COMMON_ERRORS,
        Description = "Sent when a command is not used correctly.", LegacyTranslationId = "correct_usage", FormattingDescriptions = new string[] { "Command usage." })]
    public static readonly Translation<string> CorrectUsage = new Translation<string>(ERROR_COLOR + "Correct usage: {0}.");

        [TranslationData(Section = SECTION_COMMON_ERRORS,
        Description = "A command or feature hasn't been completed or implemented.", LegacyTranslationId = "todo")]
    public static readonly Translation NotImplemented = new Translation(ERROR_COLOR + "This command hasn't been implemented yet.");

        [TranslationData(Section = SECTION_COMMON_ERRORS,
        Description = "A command or feature can only be used by the server console.", LegacyTranslationId = "command_e_no_console")]
    public static readonly Translation ConsoleOnly = new Translation(ERROR_COLOR + "This command can only be called from console.");

        [TranslationData(Section = SECTION_COMMON_ERRORS,
        Description = "A command or feature can only be used by a player (instead of the server console).", LegacyTranslationId = "command_e_no_player")]
    public static readonly Translation PlayersOnly = new Translation(ERROR_COLOR + "This command can not be called from console.");

        [TranslationData(Section = SECTION_COMMON_ERRORS,
        Description = "A player name or ID search turned up no results.", LegacyTranslationId = "command_e_player_not_found")]
    public static readonly Translation PlayerNotFound = new Translation(ERROR_COLOR + "Player not found.");

        [TranslationData(Section = SECTION_COMMON_ERRORS,
        Description = "A command didn't respond to an interaction, or a command chose to throw a vague error response to an uncommon problem.", LegacyTranslationId = "command_e_unknown_error")]
    public static readonly Translation UnknownError = new Translation(ERROR_COLOR + "We ran into an unknown error executing that command.");

        [TranslationData(Section = SECTION_COMMON_ERRORS,
        Description = "A command is disabled in the current gamemode type (ex, /deploy in a gamemode without FOBs).", LegacyTranslationId = "command_e_gamemode")]
    public static readonly Translation GamemodeError = new Translation(ERROR_COLOR + "This command is not enabled in this gamemode.");

        [TranslationData(Section = SECTION_COMMON_ERRORS,
        Description = "The caller of a command is not allowed to use the command.", LegacyTranslationId = "no_permissions")]
    public static readonly Translation NoPermissions = new Translation(ERROR_COLOR + "You do not have permission to use this command.");

        [TranslationData(Section = SECTION_COMMON_ERRORS,
        Description = "A command or feature is turned off in the configuration.", LegacyTranslationId = "not_enabled")]
    public static readonly Translation NotEnabled = new Translation(ERROR_COLOR + "This feature is not currently enabled.");

        [TranslationData(Section = SECTION_COMMON_ERRORS,
        Description = "The caller of a command has permission to use the command but isn't on duty.", LegacyTranslationId = "no_permissions_on_duty")]
    public static readonly Translation NotOnDuty = new Translation(ERROR_COLOR + "You must be on duty to execute that command.");

        [TranslationData(Section = SECTION_COMMON_ERRORS,
        Description = "The value of a parameter was not in a valid time span format..", LegacyTranslationId = "ban_invalid_number", FormattingDescriptions = new string[] { "Inputted text." })]
    public static readonly Translation<string> InvalidTime = new Translation<string>(ERROR_COLOR + "<#d09595>{0}</color> should be in a valid <#cedcde>TIME SPAN</color> format. Example: <#d09595>10d12h</color>, <#d09595>4mo15d12h</color>, <#d09595>2y</color>, <#d09595>permanent</color>.", UCPlayer.CHARACTER_NAME_FORMAT);
    #endregion

    #region Flags
    private const string SECTION_FLAGS = "Flags";
        [TranslationData(Section = SECTION_FLAGS,
        Description = "The caller of a command isn't on team 1 or 2.", LegacyTranslationId = "gamemode_flag_not_on_cap_team")]
    public static readonly Translation NotOnCaptureTeam = new Translation(ERROR_COLOR + "You're not on a team that can capture flags.");

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Sent when the player enters the capture radius of an active flag.", LegacyTranslationId = "entered_cap_radius", FormattingDescriptions = new string[] { "Objective in question" })]
    public static readonly Translation<Flag> EnteredCaptureRadius = new Translation<Flag>(SUCCESS_COLOR + "You have entered the capture radius of {0}.", Flag.COLOR_NAME_FORMAT);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Sent when the player leaves the capture radius of an active flag.", LegacyTranslationId = "left_cap_radius", FormattingDescriptions = new string[] { "Objective in question" })]
    public static readonly Translation<Flag> LeftCaptureRadius = new Translation<Flag>(SUCCESS_COLOR + "You have left the capture radius of {0}.", Flag.COLOR_NAME_FORMAT);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Sent to all players on a flag that's being captured by their team (from neutral).", LegacyTranslationId = "capturing", FormattingDescriptions = new string[] { "Objective in question" })]
    public static readonly Translation<Flag> FlagCapturing = new Translation<Flag>(SUCCESS_COLOR + "Your team is capturing {0}!", Flag.COLOR_NAME_FORMAT);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Sent to all players on a flag that's being captured by the other team.", LegacyTranslationId = "losing", FormattingDescriptions = new string[] { "Objective in question" })]
    public static readonly Translation<Flag> FlagLosing = new Translation<Flag>(ERROR_COLOR + "Your team is losing {0}!", Flag.COLOR_NAME_FORMAT);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Sent to all players on a flag when it begins being contested.", LegacyTranslationId = "contested", FormattingDescriptions = new string[] { "Objective in question" })]
    public static readonly Translation<Flag> FlagContested = new Translation<Flag>("<#c$contested$>{0} is contested, eliminate some enemies to secure it!", Flag.COLOR_NAME_FORMAT);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Sent to all players on a flag that's being cleared by their team (from the other team's ownership).", LegacyTranslationId = "clearing", FormattingDescriptions = new string[] { "Objective in question" })]
    public static readonly Translation<Flag> FlagClearing = new Translation<Flag>(SUCCESS_COLOR + "Your team is clearing {0}!", Flag.COLOR_NAME_FORMAT);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Sent to all players on a flag when it gets secured by their team.", LegacyTranslationId = "secured", FormattingDescriptions = new string[] { "Objective in question" })]
    public static readonly Translation<Flag> FlagSecured = new Translation<Flag>("<#c$secured$>{0} is secure for now, keep up the defense.", Flag.COLOR_NAME_FORMAT);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Sent to a player that walks in the radius of a flag that isn't their team's objective.", LegacyTranslationId = "nocap", FormattingDescriptions = new string[] { "Objective in question" })]
    public static readonly Translation<Flag> FlagNoCap = new Translation<Flag>("<#c$nocap$>{0} is not your objective, check the right of your screen to see which points to attack and defend.", Flag.COLOR_NAME_FORMAT);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Sent to a player that walks in the radius of a flag that is owned by the other team and enough of the other team is on the flag so they can't contest the point.", LegacyTranslationId = "notowned", FormattingDescriptions = new string[] { "Objective in question" })]
    public static readonly Translation<Flag> FlagNotOwned = new Translation<Flag>("<#c$nocap$>{0} is owned by the enemies. Get more players to capture it.", Flag.COLOR_NAME_FORMAT);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Sent to a player that walks in the radius of a flag that is owned by the other team and has been locked from recapture.", LegacyTranslationId = "locked", FormattingDescriptions = new string[] { "Objective in question" })]
    public static readonly Translation<Flag> FlagLocked = new Translation<Flag>("<#c$locked$>{0} has already been captured, try to protect the objective to win.", Flag.COLOR_NAME_FORMAT);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Sent to all players when a flag gets neutralized.", LegacyTranslationId = "flag_neutralized", FormattingDescriptions = new string[] { "Objective in question" })]
    public static readonly Translation<Flag> FlagNeutralized = new Translation<Flag>(SUCCESS_COLOR + "{0} has been neutralized!", Flag.COLOR_NAME_DISCOVER_FORMAT);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Gets broadcasted when a team captures a flag.", LegacyTranslationId = "team_capture")]
    public static readonly Translation<FactionInfo, Flag> TeamCaptured = new Translation<FactionInfo, Flag>("<#a0ad8e>{0} captured {1}.", FactionInfo.COLOR_DISPLAY_NAME_FORMAT, Flag.COLOR_NAME_DISCOVER_FORMAT);

    [TranslationData(Section = SECTION_FLAGS,
        Description = "Backup translation for team 0 name and short name.", LegacyTranslationId = "neutral")]
    public static readonly Translation Neutral = new Translation("Neutral",       TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Shows in place of the objective name for an undiscovered flag or objective.", LegacyTranslationId = "undiscovered_flag")]
    public static readonly Translation UndiscoveredFlag = new Translation("unknown",       TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's team is capturing a flag they're on.", LegacyTranslationId = "ui_capturing")]
    public static readonly Translation UICapturing = new Translation("CAPTURING",     TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's team is losing a flag they're on because there isn't enough of them to contest it.", LegacyTranslationId = "ui_losing")]
    public static readonly Translation UILosing = new Translation("LOSING",        TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's team is clearing a flag they're on.", LegacyTranslationId = "ui_clearing")]
    public static readonly Translation UIClearing = new Translation("CLEARING",      TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's team is contested with the other team on the flag they're on.", LegacyTranslationId = "ui_contested")]
    public static readonly Translation UIContested = new Translation("CONTESTED",     TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's team owns flag they're on.", LegacyTranslationId = "ui_secured")]
    public static readonly Translation UISecured = new Translation("SECURED",       TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's on a flag that isn't their team's objective.", LegacyTranslationId = "ui_nocap")]
    public static readonly Translation UINoCap = new Translation("NOT OBJECTIVE", TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's team has too few people on a flag to contest and the other team owns the flag.", LegacyTranslationId = "ui_notowned")]
    public static readonly Translation UINotOwned = new Translation("TAKEN",         TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the objective they're on is owned by the other team and is locked from recapture.", LegacyTranslationId = "ui_locked")]
    public static readonly Translation UILocked = new Translation("LOCKED",        TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Shows on the Capture UI when the player's in a vehicle on their objective.", LegacyTranslationId = "ui_invehicle")]
    public static readonly Translation UIInVehicle = new Translation("IN VEHICLE",    TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_FLAGS,
        Description = "Shows above the flag list UI.", LegacyTranslationId = "ui_capturing")]
    public static readonly Translation FlagsHeader = new Translation("Flags",         TranslationFlags.UnityUI);
    #endregion

    #region Teams
    private const string SECTION_TEAMS = "Teams";
        [TranslationData(Section = SECTION_TEAMS,
        Description = "Gets sent to the player when they walk or teleport into main base.", LegacyTranslationId = "entered_main")]
    public static readonly Translation<FactionInfo> EnteredMain                 = new Translation<FactionInfo>(SUCCESS_COLOR + "You have entered the safety of {0} headquarters!", FactionInfo.COLOR_DISPLAY_NAME_FORMAT);
        [TranslationData(Section = SECTION_TEAMS,
        Description = "Gets sent to the player when they walk or teleport out of main base.", LegacyTranslationId = "left_main")]
    public static readonly Translation<FactionInfo> LeftMain                    = new Translation<FactionInfo>(SUCCESS_COLOR + "You have left the safety of {0} headquarters!", FactionInfo.COLOR_DISPLAY_NAME_FORMAT);
        [TranslationData(Section = SECTION_TEAMS,
        Description = "Gets sent to the player when they join a team.", LegacyTranslationId = "teams_join_success")]
    public static readonly Translation<FactionInfo> TeamJoinDM                  = new Translation<FactionInfo>("<#a0ad8e>You've joined {0}.", FactionInfo.COLOR_DISPLAY_NAME_FORMAT);
        [TranslationData(Section = SECTION_TEAMS,
        Description = "Gets broadcasted to everyone when someone joins a team.", LegacyTranslationId = "teams_join_announce")]
    public static readonly Translation<FactionInfo, IPlayer> TeamJoinAnnounce   = new Translation<FactionInfo, IPlayer>("<#a0ad8e>{1} joined {0}!", FactionInfo.COLOR_DISPLAY_NAME_FORMAT, UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
        [TranslationData(Section = SECTION_TEAMS,
        Description = "Gets broadcasted when the game is over.", LegacyTranslationId = "team_win")]
    public static readonly Translation<FactionInfo> TeamWin                     = new Translation<FactionInfo>("<#a0ad8e>{0} has won the battle!", FactionInfo.COLOR_DISPLAY_NAME_FORMAT);
    #endregion

    #region Players
    private const string SECTION_PLAYERS = "Players";

        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Gets broadcasted when a player connects.", LegacyTranslationId = "player_connected")]
    public static readonly Translation<IPlayer> PlayerConnected          = new Translation<IPlayer>(SUCCESS_COLOR + "{0} joined the server.");
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Gets broadcasted when a player disconnects.", LegacyTranslationId = "player_disconnected")]
    public static readonly Translation<IPlayer> PlayerDisconnected       = new Translation<IPlayer>(SUCCESS_COLOR + "{0} left the server.");
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Kick message for a player that suffers from a rare bug which will cause GameObject.get_transform() to throw a NullReferenceException (not return null). They are kicked if this happens.", LegacyTranslationId = "null_transform_kick_message")]
    public static readonly Translation<string> NullTransformKickMessage  = new Translation<string>("Your character is bugged, which messes up our zone plugin. Rejoin or contact a Director if this continues. (discord.gg/{0}).", TranslationFlags.NoColor);
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Gets sent to a player when their message gets blocked by the chat filter.", LegacyTranslationId = "text_chat_feedback_chat_filter")]
    public static readonly Translation<string> ChatFilterFeedback        = new Translation<string>(ERROR_COLOR + "Our chat filter flagged <#fdfdfd>{0}</color>, so the message wasn't sent.");
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Gets sent to a player when their message gets blocked by the chat filter.", LegacyTranslationId = "kick_autokick_namefilter", FormattingDescriptions = new string[] { "Required successive alphanumeric character setting." })]
    public static readonly Translation<int> NameFilterKickMessage        = new Translation<int>("Your name does not contain enough alphanumeric characters in succession ({0}), please change your name and rejoin.", TranslationFlags.NoColor);
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Gets sent to a player who is attempting to main camp the other team.", LegacyTranslationId = "amc_reverse_damage")]
    public static readonly Translation AntiMainCampWarning               = new Translation("<#fa9e9e>Stop <b><#ff3300>main-camping</color></b>! Damage is <b>reversed</b> back on you.");
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Gets sent to a player who is attempting to main camp the other team.", LegacyTranslationId = "no_placement_on_vehicle", FormattingDescriptions = new string[] { "Barricade attempting to be placed (plural)." })]
    public static readonly Translation<ItemBarricadeAsset> NoPlacementOnVehicle = new Translation<ItemBarricadeAsset>("<#fa9e9e>You can't place {0} on a vehicle!</color>", RARITY_COLOR_FORMAT + PLURAL);
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Generic message sent when a player is placing something in a place they shouldn't.", LegacyTranslationId = "no_placement_on_vehicle", FormattingDescriptions = new string[] { "Object attempting to be placed (plural)." })]
    public static readonly Translation<ItemAsset> ProhibitedPlacement    = new Translation<ItemAsset>("<#fa9e9e>You're not allowed to place {0} here.", RARITY_COLOR_FORMAT + PLURAL);
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Sent when a player tries to steal a battery.", LegacyTranslationId = "cant_steal_batteries")]
    public static readonly Translation NoStealingBatteries               = new Translation("<#fa9e9e>Stealing batteries is not allowed.</color>");
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Sent when a player tries to manually leave their group.", LegacyTranslationId = "cant_leave_group")]
    public static readonly Translation NoLeavingGroup                    = new Translation("<#fa9e9e>You are not allowed to manually change groups, use <#cedcde>/teams</color> instead.");
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Message sent when a player tries to place a non-whitelisted item in a storage inventory.", LegacyTranslationId = "cant_store_this_item", FormattingDescriptions = new string[] { "Item the player is trying to store (plural)." })]
    public static readonly Translation<ItemAsset> ProhibitedStoring      = new Translation<ItemAsset>("<#fa9e9e>You are not allowed to store {0}.", RARITY_COLOR_FORMAT + PLURAL);
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Sent when a player tries to point or mark while not a squad leader.", LegacyTranslationId = "marker_not_in_squad", FormattingDescriptions = new string[] { "Item the player is trying to store (plural)." })]
    public static readonly Translation MarkerNotInSquad                  = new Translation("<#fa9e9e>Only your squad can see markers, join a squad with <#cedcde>/squad join <name></color> or <color=#cedcde>/squad create <name></color> to use this feature.");
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Sent on a SEVERE toast when the player enters enemy territory.", LegacyTranslationId = "entered_enemy_territory", FormattingDescriptions = new string[] { "Long time string." })]
    public static readonly Translation<string> EnteredEnemyTerritory     = new Translation<string>("Too close to enemy base! You will die in <#cedcde>{0}</color>!", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "WARNING toast sent when someone's about to get mortared by a friendly.", LegacyTranslationId = "friendly_mortar_incoming", FormattingDescriptions = new string[] { "Seconds remaining." })]
    public static readonly Translation<float> MortarStrikeWarning        = new Translation<float>("FRIENDLY MORTAR STRIKE INCOMING: {0} SECONDS OUT", TranslationFlags.UnityUI, "F1");
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Sent 2 times before a player is kicked for inactivity.", LegacyTranslationId = "afk_warning", FormattingDescriptions = new string[] { "Long timestamp remaining" })]
    public static readonly Translation<string> InactivityWarning         = new Translation<string>("<#fa9e9e>You will be AFK-Kicked in <#cedcde>{0}</color> if you don't move.</color>");
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Broadcasted when a player is removed from the game by BattlEye.", LegacyTranslationId = "battleye_kick_broadcast", FormattingDescriptions = new string[] { "Long timestamp remaining" })]
    public static readonly Translation<IPlayer> BattlEyeKickBroadcast    = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was kicked by <#feed00>BattlEye</color>.", UCPlayer.PLAYER_NAME_FORMAT);
        [TranslationData(Section = SECTION_PLAYERS,
        Description = "Sent when an unauthorized player attempts to edit a sign.", LegacyTranslationId = "whitelist_noeditsign")]
    public static readonly Translation ProhibitedSignEditing             = new Translation("<#ff8c69>You are not allowed to edit that sign.");
    #endregion

    #region Leaderboards

    private const string SECTION_LEADERBOARD = "Leaderboard";
    #region Shared
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "lb_next_game")]
    public static readonly Translation StartingSoon                   = new Translation("Starting soon...", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "lb_next_game_shut_down")]
    public static readonly Translation<string> NextGameShutdown       = new Translation<string>("<#94cbff>Shutting Down Because: \"{0}\"</color>", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "lb_next_game_time_format")]
    public static readonly Translation<TimeSpan> NextGameShutdownTime = new Translation<TimeSpan>("{0}", TranslationFlags.UnityUI, "mm:ss");

        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "lb_warstats_header")]
    public static readonly Translation<FactionInfo, FactionInfo> WarstatsHeader = new Translation<FactionInfo, FactionInfo>("{0} vs {1}", TranslationFlags.UnityUI, FactionInfo.COLOR_SHORT_NAME_FORMAT, FactionInfo.COLOR_SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "lb_playerstats_header")]
    public static readonly Translation<IPlayer, float> PlayerstatsHeader       = new Translation<IPlayer, float>("{0} - {1}% presence", TranslationFlags.UnityUI, UCPlayer.COLOR_CHARACTER_NAME_FORMAT, "P0");
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "lb_winner_title")]
    public static readonly Translation<FactionInfo> WinnerTitle                 = new Translation<FactionInfo>("{0} Wins!", TranslationFlags.UnityUI, FactionInfo.COLOR_SHORT_NAME_FORMAT);

        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "lb_longest_shot", FormattingDescriptions = new string[] { "Distance", "Gun Name", "Player" })]
    public static readonly Translation<float, string, IPlayer> LongestShot     = new Translation<float, string, IPlayer>("{0}m - {1}\n{2}", TranslationFlags.UnityUI, "F1", arg2Fmt: UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    #endregion

    #region CTFBase
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_playerstats_0")]
    public static readonly Translation CTFPlayerStats0  = new Translation("Kills: ",            TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_playerstats_1")]
    public static readonly Translation CTFPlayerStats1  = new Translation("Deaths: ",           TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_playerstats_2")]
    public static readonly Translation CTFPlayerStats2  = new Translation("K/D Ratio: ",        TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_playerstats_3")]
    public static readonly Translation CTFPlayerStats3  = new Translation("Kills on Point: ",   TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_playerstats_4")]
    public static readonly Translation CTFPlayerStats4  = new Translation("Time Deployed: ",    TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_playerstats_5")]
    public static readonly Translation CTFPlayerStats5  = new Translation("XP Gained: ",        TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_playerstats_6")]
    public static readonly Translation CTFPlayerStats6  = new Translation("Time on Point: ",    TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_playerstats_7")]
    public static readonly Translation CTFPlayerStats7  = new Translation("Captures: ",         TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_playerstats_8")]
    public static readonly Translation CTFPlayerStats8  = new Translation("Time in Vehicle: ",  TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_playerstats_9")]
    public static readonly Translation CTFPlayerStats9  = new Translation("Teamkills: ",        TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_playerstats_10")]
    public static readonly Translation CTFPlayerStats10 = new Translation("FOBs Destroyed: ",   TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_playerstats_11")]
    public static readonly Translation CTFPlayerStats11 = new Translation("Credits Gained: ",   TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_warstats_0")]
    public static readonly Translation CTFWarStats0 = new Translation("Duration: ", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_warstats_1")]
    public static readonly Translation<FactionInfo> CTFWarStats1 = new Translation<FactionInfo>("{0} Casualties: ",     TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_warstats_2")]
    public static readonly Translation<FactionInfo> CTFWarStats2 = new Translation<FactionInfo>("{0} Casualties: ",     TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_warstats_3")]
    public static readonly Translation CTFWarStats3 = new Translation("Flag Captures: ", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_warstats_4")]
    public static readonly Translation<FactionInfo> CTFWarStats4 = new Translation<FactionInfo>("{0} Average Army: ",   TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_warstats_5")]
    public static readonly Translation<FactionInfo> CTFWarStats5 = new Translation<FactionInfo>("{0} Average Army: ",   TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_warstats_6")]
    public static readonly Translation<FactionInfo> CTFWarStats6 = new Translation<FactionInfo>("{0} FOBs Placed: ",    TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_warstats_7")]
    public static readonly Translation<FactionInfo> CTFWarStats7 = new Translation<FactionInfo>("{0} FOBs Placed: ",    TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_warstats_8")]
    public static readonly Translation<FactionInfo> CTFWarStats8 = new Translation<FactionInfo>("{0} FOBs Destroyed: ", TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_warstats_9")]
    public static readonly Translation<FactionInfo> CTFWarStats9 = new Translation<FactionInfo>("{0} FOBs Destroyed: ", TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_warstats_10")]
    public static readonly Translation CTFWarStats10 = new Translation("Teamkill Casualties: ", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_warstats_11")]
    public static readonly Translation CTFWarStats11 = new Translation("Longest Shot: ",        TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_header_0")]
    public static readonly Translation CTFHeader0 = new Translation("Kills",   TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_header_1")]
    public static readonly Translation CTFHeader1 = new Translation("Deaths",  TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_header_2")]
    public static readonly Translation CTFHeader2 = new Translation("XP",      TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_header_3")]
    public static readonly Translation CTFHeader3 = new Translation("Credits", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_header_4")]
    public static readonly Translation CTFHeader4 = new Translation("Caps",    TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ctf_lb_header_5")]
    public static readonly Translation CTFHeader5 = new Translation("Damage",  TranslationFlags.UnityUI);
    #endregion

    #region Insurgency
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_playerstats_0")]
    public static readonly Translation InsurgencyPlayerStats0  = new Translation("Kills: ",                 TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_playerstats_1")]
    public static readonly Translation InsurgencyPlayerStats1  = new Translation("Deaths: ",                TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_playerstats_2")]
    public static readonly Translation InsurgencyPlayerStats2  = new Translation("Damage Done: ",           TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_playerstats_3")]
    public static readonly Translation InsurgencyPlayerStats3  = new Translation("Objective Kills: ",       TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_playerstats_4")]
    public static readonly Translation InsurgencyPlayerStats4  = new Translation("Time Deployed: ",         TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_playerstats_5")]
    public static readonly Translation InsurgencyPlayerStats5  = new Translation("XP Gained: ",             TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_playerstats_6")]
    public static readonly Translation InsurgencyPlayerStats6  = new Translation("Intelligence Gathered: ", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_playerstats_7")]
    public static readonly Translation InsurgencyPlayerStats7  = new Translation("Caches Discovered: ",     TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_playerstats_8")]
    public static readonly Translation InsurgencyPlayerStats8  = new Translation("Caches Destroyed: ",      TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_playerstats_9")]
    public static readonly Translation InsurgencyPlayerStats9  = new Translation("Teamkills: ",             TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_playerstats_10")]
    public static readonly Translation InsurgencyPlayerStats10 = new Translation("FOBs Destroyed: ",        TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_playerstats_11")]
    public static readonly Translation InsurgencyPlayerStats11 = new Translation("Credits Gained: ",        TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_warstats_0")]
    public static readonly Translation InsurgencyWarStats0 = new Translation("Duration: ", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_warstats_1")]
    public static readonly Translation<FactionInfo> InsurgencyWarStats1 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_warstats_2")]
    public static readonly Translation<FactionInfo> InsurgencyWarStats2 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_warstats_3")]
    public static readonly Translation InsurgencyWarStats3 = new Translation("Intelligence Gathered: ", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_warstats_4")]
    public static readonly Translation<FactionInfo> InsurgencyWarStats4 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_warstats_5")]
    public static readonly Translation<FactionInfo> InsurgencyWarStats5 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_warstats_6")]
    public static readonly Translation<FactionInfo> InsurgencyWarStats6 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_warstats_7")]
    public static readonly Translation<FactionInfo> InsurgencyWarStats7 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_warstats_8")]
    public static readonly Translation<FactionInfo> InsurgencyWarStats8 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_warstats_9")]
    public static readonly Translation<FactionInfo> InsurgencyWarStats9 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_warstats_10")]
    public static readonly Translation InsurgencyWarStats10 = new Translation("Teamkill Casualties: ", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_warstats_11")]
    public static readonly Translation InsurgencyWarStats11 = new Translation("Longest Shot: ",        TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_header_0")]
    public static readonly Translation InsurgencyHeader0 = new Translation("Kills",   TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_header_1")]
    public static readonly Translation InsurgencyHeader1 = new Translation("Deaths",  TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_header_2")]
    public static readonly Translation InsurgencyHeader2 = new Translation("XP",      TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_header_3")]
    public static readonly Translation InsurgencyHeader3 = new Translation("Credits", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_header_4")]
    public static readonly Translation InsurgencyHeader4 = new Translation("KDR",     TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "ins_lb_header_5")]
    public static readonly Translation InsurgencyHeader5 = new Translation("Damage",  TranslationFlags.UnityUI);
    #endregion

    #region Conquest
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_playerstats_0")]
    public static readonly Translation ConquestPlayerStats0  = new Translation("Kills: ",                 TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_playerstats_1")]
    public static readonly Translation ConquestPlayerStats1  = new Translation("Deaths: ",                TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_playerstats_2")]
    public static readonly Translation ConquestPlayerStats2  = new Translation("Damage Done: ",           TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_playerstats_3")]
    public static readonly Translation ConquestPlayerStats3  = new Translation("Objective Kills: ",       TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_playerstats_4")]
    public static readonly Translation ConquestPlayerStats4  = new Translation("Time Deployed: ",         TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_playerstats_5")]
    public static readonly Translation ConquestPlayerStats5  = new Translation("XP Gained: ",             TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_playerstats_6")]
    public static readonly Translation ConquestPlayerStats6  = new Translation("Revives: ",               TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_playerstats_7")]
    public static readonly Translation ConquestPlayerStats7  = new Translation("Flags Captured: ",        TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_playerstats_8")]
    public static readonly Translation ConquestPlayerStats8  = new Translation("Time on Flag: ",          TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_playerstats_9")]
    public static readonly Translation ConquestPlayerStats9  = new Translation("Teamkills: ",             TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_playerstats_10")]
    public static readonly Translation ConquestPlayerStats10 = new Translation("FOBs Destroyed: ",        TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_playerstats_11")]
    public static readonly Translation ConquestPlayerStats11 = new Translation("Credits Gained: ",        TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_warstats_0")]
    public static readonly Translation ConquestWarStats0 = new Translation("Duration: ",                                      TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_warstats_1")]
    public static readonly Translation<FactionInfo> ConquestWarStats1 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_warstats_2")]
    public static readonly Translation<FactionInfo> ConquestWarStats2 = new Translation<FactionInfo>("{0} Casualties: ",      TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_warstats_3")]
    public static readonly Translation ConquestWarStats3 = new Translation("Flag Captures: ",                                 TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_warstats_4")]
    public static readonly Translation<FactionInfo> ConquestWarStats4 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_warstats_5")]
    public static readonly Translation<FactionInfo> ConquestWarStats5 = new Translation<FactionInfo>("{0} Average Army: ",    TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_warstats_6")]
    public static readonly Translation<FactionInfo> ConquestWarStats6 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_warstats_7")]
    public static readonly Translation<FactionInfo> ConquestWarStats7 = new Translation<FactionInfo>("{0} FOBs Placed: ",     TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_warstats_8")]
    public static readonly Translation<FactionInfo> ConquestWarStats8 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_warstats_9")]
    public static readonly Translation<FactionInfo> ConquestWarStats9 = new Translation<FactionInfo>("{0} FOBs Destroyed: ",  TranslationFlags.UnityUI, FactionInfo.SHORT_NAME_FORMAT);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_warstats_10")]
    public static readonly Translation ConquestWarStats10 = new Translation("Teamkill Casualties: ", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_warstats_11")]
    public static readonly Translation ConquestWarStats11 = new Translation("Longest Shot: ",        TranslationFlags.UnityUI);

        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_header_0")]
    public static readonly Translation ConquestHeader0 = new Translation("Kills",   TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_header_1")]
    public static readonly Translation ConquestHeader1 = new Translation("Deaths",  TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_header_2")]
    public static readonly Translation ConquestHeader2 = new Translation("XP",      TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_header_3")]
    public static readonly Translation ConquestHeader3 = new Translation("Credits", TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_header_4")]
    public static readonly Translation ConquestHeader4 = new Translation("KDR",     TranslationFlags.UnityUI);
        [TranslationData(Section = SECTION_LEADERBOARD, LegacyTranslationId = "cqt_lb_header_5")]
    public static readonly Translation ConquestHeader5 = new Translation("Damage",  TranslationFlags.UnityUI);
    #endregion

    #endregion

    #region GroupCommand
    private const string SECTION_GROUP = "Groups";
        [TranslationData(Section = SECTION_GROUP, Description = "Output from /group, tells the player their current group.", LegacyTranslationId = "current_group")]
    public static readonly Translation<ulong, string, Color> CurrentGroup = new Translation<ulong, string, Color>(SUCCESS_COLOR + "Group <#{2}>{0}</color>: <#{2}>{1}</color>");
        [TranslationData(Section = SECTION_GROUP, Description = "Output from /group join <id>.", LegacyTranslationId = "joined_group")]
    public static readonly Translation<ulong, string, Color> JoinedGroup  = new Translation<ulong, string, Color>(SUCCESS_COLOR + "You have joined group <#{2}>{0}</color>: <#{2}>{1}</color>.");
        [TranslationData(Section = SECTION_GROUP, Description = "Output from /group when the player is not in a group.", LegacyTranslationId = "not_in_group")]
    public static readonly Translation NotInGroup           = new Translation(ERROR_COLOR + "You aren't in a group.");
        [TranslationData(Section = SECTION_GROUP, Description = "Output from /group join <id> when the player is already in that group.", LegacyTranslationId = "joined_already_in_group")]
    public static readonly Translation AlreadyInGroup       = new Translation(ERROR_COLOR + "You are already in that group.");
        [TranslationData(Section = SECTION_GROUP, Description = "Output from /group join <id> when the group is not found.", LegacyTranslationId = "joined_group_not_found")]
    public static readonly Translation<ulong> GroupNotFound = new Translation<ulong>(ERROR_COLOR + "Could not find group <#4785ff>{0}</color>.");
    #endregion

    #region LangCommand
    private const string SECTION_LANGUAGES = "Languages";
        [TranslationData(Section = SECTION_LANGUAGES, Description = "Output from /lang, lists all languages.", LegacyTranslationId = "language_list", FormattingDescriptions = new string[] { "Comma separated list of all language codes." })]
    public static readonly Translation<string> LanguageList              = new Translation<string>("<#f53b3b>Languages: <#e6e3d5>{0}</color>.");
        [TranslationData(Section = SECTION_LANGUAGES, Description = "Fallback usage output from /lang, explains /lang reset.", LegacyTranslationId = "reset_language_how")]
    public static readonly Translation ResetLanguageHow                  = new Translation("<#f53b3b>Do <#e6e3d5>/lang reset</color> to reset back to default language.");
        [TranslationData(Section = SECTION_LANGUAGES, Description = "Output from /lang current, tells the player their selected language.", LegacyTranslationId = "language_current", FormattingDescriptions = new string[] { "Current language" })]
    public static readonly Translation<LanguageAliasSet> LanguageCurrent = new Translation<LanguageAliasSet>("<#f53b3b>Current language: <#e6e3d5>{0}</color>.", LanguageAliasSet.DISPLAY_NAME_FORMAT);
        [TranslationData(Section = SECTION_LANGUAGES, Description = "Output from /lang <language>, tells the player their new language.", LegacyTranslationId = "changed_language", FormattingDescriptions = new string[] { "New language" })]
    public static readonly Translation<LanguageAliasSet> ChangedLanguage = new Translation<LanguageAliasSet>("<#f53b3b>Changed your language to <#e6e3d5>{0}</color>.", LanguageAliasSet.DISPLAY_NAME_FORMAT);
        [TranslationData(Section = SECTION_LANGUAGES, Description = "Output from /lang <language> when the player is using already that language.", LegacyTranslationId = "change_language_not_needed", FormattingDescriptions = new string[] { "Current language" })]
    public static readonly Translation<LanguageAliasSet> LangAlreadySet  = new Translation<LanguageAliasSet>(ERROR_COLOR + "You are already set to <#e6e3d5>{0}</color>.", LanguageAliasSet.DISPLAY_NAME_FORMAT);
        [TranslationData(Section = SECTION_LANGUAGES, Description = "Output from /lang reset, tells the player their language changed to the default language.", LegacyTranslationId = "reset_language", FormattingDescriptions = new string[] { "Default language" })]
    public static readonly Translation<LanguageAliasSet> ResetLanguage   = new Translation<LanguageAliasSet>("<#f53b3b>Reset your language to <#e6e3d5>{0}</color>.", LanguageAliasSet.DISPLAY_NAME_FORMAT);
        [TranslationData(Section = SECTION_LANGUAGES, Description = "Output from /lang reset when the player is using already that language.", LegacyTranslationId = "reset_language_not_needed", FormattingDescriptions = new string[] { "Default language" })]
    public static readonly Translation<LanguageAliasSet> ResetCurrent    = new Translation<LanguageAliasSet>(ERROR_COLOR + "You are already on the default language: <#e6e3d5>{0}</color>.", LanguageAliasSet.DISPLAY_NAME_FORMAT);
    [TranslationData(Section = SECTION_LANGUAGES, Description = "Output from /lang <language> when the language isn't found.", LegacyTranslationId = "dont_have_language", FormattingDescriptions = new string[] { "Input language" })]
    public static readonly Translation<string> LanguageNotFound          = new Translation<string>("<#dd1111>We don't have translations for <#e6e3d5>{0}</color> yet. If you are fluent and want to help, feel free to ask us about submitting translations.", LanguageAliasSet.DISPLAY_NAME_FORMAT);
    #endregion

    #region Toasts
    private const string SECTION_TOASTS = "Toasts";
        [TranslationData(Section = SECTION_TOASTS, Description = "Sent when the player joins for the 2nd+ time..", LegacyTranslationId = "welcome_message", FormattingDescriptions = new string[] { "Joining player." })]
    public static readonly Translation<IPlayer> WelcomeBackMessage = new Translation<IPlayer>("Thanks for playing <#c$uncreated$>Uncreated Warfare</color>!\nWelcome back {0}.", TranslationFlags.UnityUI, UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
        [TranslationData(Section = SECTION_TOASTS, Description = "Sent when the player joins for the 1st time..", LegacyTranslationId = "welcome_message_first_time", FormattingDescriptions = new string[] { "Joining player." })]
    public static readonly Translation<IPlayer> WelcomeMessage     = new Translation<IPlayer>("Welcome to <#c$uncreated$>Uncreated Warfare</color> {0}!\nTalk to the NPCs to get started.", TranslationFlags.UnityUI, UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    #endregion

    #region KitCommand
    private const string SECTION_KITS = "Kits";
        [TranslationData(Section = SECTION_KITS, Description = "Sent when the player creates a new kit with /kit create <name>", LegacyTranslationId = "kit_created", FormattingDescriptions = new string[] { "Newly created kit." })]
    public static readonly Translation<Kit> KitCreated          = new Translation<Kit>("<#a0ad8e>Created kit: <#fff>{0}</color>.", Kit.ID_FORMAT);
        [TranslationData(Section = SECTION_KITS, Description = "Sent when the player overwrites the items in a kit with /kit create <name>", LegacyTranslationId = "kit_overwritten", FormattingDescriptions = new string[] { "Overwritten kit." })]
    public static readonly Translation<Kit> KitOverwrote        = new Translation<Kit>("<#a0ad8e>Overwritten items for kit: <#fff>{0}</color>.", Kit.ID_FORMAT);
        [TranslationData(Section = SECTION_KITS, Description = "Sent when the player copies a kit with /kit copyfrom <source> <name>", LegacyTranslationId = "kit_copied", FormattingDescriptions = new string[] { "Source kit.", "Newly created kit." })]
    public static readonly Translation<Kit, Kit> KitCopied      = new Translation<Kit, Kit>("<#a0ad8e>Copied data from <#c7b197>{0}</color> into a new kit: <#fff>{1}</color>.", Kit.ID_FORMAT, Kit.ID_FORMAT);
        [TranslationData(Section = SECTION_KITS, Description = "Sent when the player deletes a kit with /kit delete <name>", LegacyTranslationId = "kit_deleted", FormattingDescriptions = new string[] { "Source kit.", "Newly created kit." })]
    public static readonly Translation<Kit> KitDeleted          = new Translation<Kit>("<#a0ad8e>Deleted kit: <#fff>{0}</color>.", Kit.ID_FORMAT);
    public static readonly Translation<string> KitSearchResults = new Translation<string>("<#a0ad8e>Matches: <i>{0}</i>.");
    public static readonly Translation<Kit> KitAccessGivenDm    = new Translation<Kit>("<#a0ad8e>You were given access to the kit: <#fff>{0}</color>.", Kit.ID_FORMAT);
    public static readonly Translation<Kit> KitAccessRevokedDm  = new Translation<Kit>("<#a0ad8e>Your access to <#fff>{0}</color> was revoked.", Kit.ID_FORMAT);
    public static readonly Translation<string, Kit, string> KitPropertySet    = new Translation<string, Kit, string>("<#a0ad8e>Set <#aaa>{0}</color> on kit <#fff>{1}</color> to <#aaa><uppercase>{2}</uppercase></color>.", arg1Fmt: Kit.ID_FORMAT);
    public static readonly Translation<string> KitNameTaken                   = new Translation<string>(ERROR_COLOR + "A kit named <#fff>{0}</color> already exists.");
    public static readonly Translation<string> KitNotFound                    = new Translation<string>(ERROR_COLOR + "A kit named <#fff>{0}</color> doesn't exists.");
    public static readonly Translation<string> KitPropertyNotFound            = new Translation<string>(ERROR_COLOR + "Kits don't have a <#eee>{0}</color> property.");
    public static readonly Translation<string> KitPropertyProtected           = new Translation<string>(ERROR_COLOR + "<#eee>{0}</color> can not be changed on kits.");
    public static readonly Translation<IPlayer, Kit> KitAlreadyHasAccess      = new Translation<IPlayer, Kit>(ERROR_COLOR + "{0} already has access to <#fff>{1}</color>.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, Kit.ID_FORMAT);
    public static readonly Translation<IPlayer, Kit> KitAlreadyMissingAccess  = new Translation<IPlayer, Kit>(ERROR_COLOR + "{0} doesn't have access to <#fff>{1}</color>.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, Kit.ID_FORMAT);
    public static readonly Translation<Cooldown> KitOnCooldown                = new Translation<Cooldown>(ERROR_COLOR + "You can request this kit again in: <#bafeff>{0}</color>.", Cooldown.SHORT_TIME_FORMAT);
    public static readonly Translation<Cooldown> KitOnGlobalCooldown          = new Translation<Cooldown>(ERROR_COLOR + "You can request another kit again in: <#bafeff>{0}</color>.", Cooldown.SHORT_TIME_FORMAT);
    public static readonly Translation<IPlayer, IPlayer, Kit> KitAccessGiven         = new Translation<IPlayer, IPlayer, Kit>("<#a0ad8e>{0} (<#aaa>{1}</color>) was given access to the kit: <#fff>{2}</color>.", UCPlayer.COLOR_PLAYER_NAME_FORMAT, UCPlayer.STEAM_64_FORMAT, Kit.ID_FORMAT);
    public static readonly Translation<IPlayer, IPlayer, Kit> KitAccessRevoked       = new Translation<IPlayer, IPlayer, Kit>("<#a0ad8e>{0} (<#aaa>{1}</color>)'s access to <#fff>{2}</color> was taken away.", UCPlayer.COLOR_PLAYER_NAME_FORMAT, UCPlayer.STEAM_64_FORMAT, Kit.ID_FORMAT);
    public static readonly Translation<string, Type, string> KitInvalidPropertyValue = new Translation<string, Type, string>(ERROR_COLOR + "<#fff>{2}</color> isn't a valid value for <#eee>{0}</color> (<#aaa>{1}</color>).");
    public static readonly Translation<EClass, IPlayer, IPlayer, Kit> LoadoutCreated = new Translation<EClass, IPlayer, IPlayer, Kit>("<#a0ad8e>Created <#bbc>{0}</color> loadout for {1} (<#aaa>{2}</color>). Kit name: <#fff>{3}</color>.", arg1Fmt: UCPlayer.COLOR_CHARACTER_NAME_FORMAT, arg2Fmt: UCPlayer.STEAM_64_FORMAT, arg3Fmt: Kit.ID_FORMAT);
    public static readonly Translation<ItemAsset> KitProhibitedPickupAmt             = new Translation<ItemAsset>("<#ff8c69>Your kit does not allow you to have any more {0}.", RARITY_COLOR_FORMAT + PLURAL);
    #endregion

    #region RangeCommand
    public static readonly Translation<float> RangeOutput  = new Translation<float>("<#9e9c99>The range to your squad's marker is: <#8aff9f>{0}m</color>.", "N0");
    public static readonly Translation RangeNoMarker       = new Translation("<#9e9c99>You squad has no marker.");
    public static readonly Translation RangeNotSquadleader = new Translation("<#9e9c99>Only <#cedcde>SQUAD LEADERS</color> can place markers.");
    public static readonly Translation RangeNotInSquad     = new Translation("<#9e9c99>You must JOIN A SQUAD in order to do /range.");
    #endregion

    #region Squads
    public static readonly Translation SquadNotOnTeam               = new Translation("<#a89791>You can't join a squad unless you're on a team.");
    public static readonly Translation<Squad> SquadCreated          = new Translation<Squad>("<#a0ad8e>You created {0} squad.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> SquadJoined           = new Translation<Squad>("<#a0ad8e>You joined {0} squad.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> SquadLeft             = new Translation<Squad>("<#a7a8a5>You left {0} squad.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> SquadDisbanded        = new Translation<Squad>("<#a7a8a5>{0} squad was disbanded.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation SquadLockedSquad             = new Translation("<#a7a8a5>You <#6be888>locked</color> your squad.");
    public static readonly Translation SquadUnlockedSquad           = new Translation("<#999e90>You <#6be888>unlocked</color> your squad.");
    public static readonly Translation<Squad> SquadPromoted         = new Translation<Squad>("<#999e90>You're now the <#cedcde>sqauad leader</color> of {0}.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> SquadKicked           = new Translation<Squad>("<#ae8f8f>You were kicked from {0} squad.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<string> SquadNotFound        = new Translation<string>("<#ae8f8f>Failed to find a squad called <#c$neutral$>\"{0}\"</color>. You can also use the first letter of the squad name.");
    public static readonly Translation SquadAlreadyInSquad          = new Translation("<#ae8f8f>You're already in a squad.");
    public static readonly Translation SquadNotInSquad              = new Translation("<#ae8f8f>You're not in a squad yet. Use <#ae8f8f>/squad join <squad></color> to join a squad.");
    public static readonly Translation SquadNotSquadLeader          = new Translation("<#ae8f8f>You're not the leader of your squad.");
    public static readonly Translation<Squad> SquadLocked           = new Translation<Squad>("<#a89791>{0} is locked.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> SquadFull             = new Translation<Squad>("<#a89791>{0} is full.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation SquadTargetNotInSquad        = new Translation("<#a89791>That player isn't in a squad.");
    public static readonly Translation<IPlayer> SquadPlayerJoined   = new Translation<IPlayer>("<#b9bdb3>{0} joined your squad.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer> SquadPlayerLeft     = new Translation<IPlayer>("<#b9bdb3>{0} left your squad.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer> SquadPlayerPromoted = new Translation<IPlayer>("<#b9bdb3>{0} was promoted to <#cedcde>sqauad leader</color>.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer> SquadPlayerKicked   = new Translation<IPlayer>("<#b9bdb3>{0} was kicked from your squad.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation SquadsDisabled               = new Translation("<#a89791>Squads are disabled in this gamemode.");
    public static readonly Translation<int> SquadsTooMany           = new Translation<int>("<#a89791>There can not be more than {0} squads on a team at once.");

    public static readonly Translation<Squad, int, int> SquadsUIHeaderPlayerCount = new Translation<Squad, int, int>("<#bd6b5b>{0}</color {1}/{2}", TranslationFlags.UnityUI, Squad.NAME_FORMAT);
    public static readonly Translation<int, int> SquadsUIPlayerCountSmall         = new Translation<int, int>("{0}/{1}", TranslationFlags.UnityUI);
    public static readonly Translation<int, int> SquadsUIPlayerCountSmallLocked   = new Translation<int, int>("<#969696>{0}/{1}</color>", TranslationFlags.UnityUI);
    public static readonly Translation SquadUIExpanded                            = new Translation("...", TranslationFlags.UnityUI);
    #endregion

    #region Orders
    public static readonly Translation OrderUsageAll              = new Translation("<#9fa1a6>To give orders: <#9dbccf>/order <squad> <type></color>. Type <#d1bd90>/order actions</color> to see a list of actions.");
    public static readonly Translation<Squad> OrderUsageNoAction  = new Translation<Squad>("<#9fa1a6>Try typing: <#9dbccf>/order <lowercase>{0}</lowercase> <action></color>.", Squad.NAME_FORMAT);
    public static readonly Translation<Squad> OrderUsageBadAction = new Translation<Squad>("<#9fa1a6>Try typing: <#9dbccf>/order <lowercase>{0}</lowercase> <b><action></b></color>. Type <#d1bd90>/order actions</color> to see a list of actions.", Squad.NAME_FORMAT);
    public static readonly Translation<string> OrderActions       = new Translation<string>("<#9fa1a6>Order actions: <#9dbccf>{0}</color>.");
    public static readonly Translation<string> OrderSquadNoExist  = new Translation<string>(ERROR_COLOR + "There is no friendly <lowercase><#c$neutral$>{0}</color></lowercase> squad.");
    public static readonly Translation OrderNotSquadleader        = new Translation(ERROR_COLOR + "You must be a <#cedcde>sqauad leader</color> to give orders.");
    public static readonly Translation<string, string> OrderActionInvalid = new Translation<string, string>(ERROR_COLOR + "<#fff>{0}</color> is not a valid action. Try one of these: <#9dbccf>{1}</color>.");
    public static readonly Translation<Squad> OrderAttackMarkerCTF  = new Translation<Squad>(ERROR_COLOR + "Place a map marker on a <#d1bd90>position</color> or <#d1bd90>flag</color> where you want {0} to attack.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> OrderAttackMarkerIns  = new Translation<Squad>(ERROR_COLOR + "Place a map marker on a <#d1bd90>position</color> or <#d1bd90>cache</color> where you want {0} to attack.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> OrderDefenseMarkerCTF = new Translation<Squad>(ERROR_COLOR + "Place a map marker on a <#d1bd90>position</color> or <#d1bd90>flag</color> where you want {0} to defend.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> OrderDefenseMarkerIns = new Translation<Squad>(ERROR_COLOR + "Place a map marker on a <#d1bd90>position</color> or <#d1bd90>cache</color> where you want {0} to defend.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> OrderBuildFOBError    = new Translation<Squad>(ERROR_COLOR + "Place a map marker on a <#d1bd90>position</color> you want {0} to build a <color=#d1bd90>FOB</color>.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation<Squad> OrderMoveError        = new Translation<Squad>(ERROR_COLOR + "Place a map marker on a <#d1bd90>position</color> you want {0} to move to.", Squad.COLORED_NAME_FORMAT);
    public static readonly Translation OrderBuildFOBExists          = new Translation(ERROR_COLOR + "There is already a friendly FOB near that marker.");
    public static readonly Translation OrderBuildFOBTooMany         = new Translation(ERROR_COLOR + "There are already too many FOBs on your team.");
    public static readonly Translation OrderSquadTooClose           = new Translation(ERROR_COLOR + "{0} is already near that marker. Try placing it further away.");
    public static readonly Translation<Squad, Order> OrderSent      = new Translation<Squad, Order>("<#9fa1a6>Order sent to {0}: <#9dbccf>{1}</color>.", Squad.COLORED_NAME_FORMAT, Order.MESSAGE_FORMAT);
    public static readonly Translation<IPlayer, Order> OrderReceived   = new Translation<IPlayer, Order>("<#9fa1a6>{0} has given your squad new orders:" + Environment.NewLine + "<#d4d4d4>{1}</color>.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, Order.MESSAGE_FORMAT);
    public static readonly Translation<IPlayer> OrderUICommander       = new Translation<IPlayer>("Orders from <#a7becf>{0}</color>:", TranslationFlags.UnityUI, UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<Order> OrderUIMessage           = new Translation<Order>("{0}", TranslationFlags.UnityUI, Order.MESSAGE_FORMAT);
    public static readonly Translation<TimeSpan> OrderUITimeLeft       = new Translation<TimeSpan>("- {0}m left", TranslationFlags.UnityUI, "%m");
    public static readonly Translation<int> OrderUIReward              = new Translation<int>("- Reward: {0} XP", TranslationFlags.UnityUI);
    public static readonly Translation<Flag> OrderUIAttackObjective    = new Translation<Flag>("Attack your objective: {0}.", TranslationFlags.UnityUI, Flag.COLOR_SHORT_NAME_FORMAT);
    public static readonly Translation<Flag> OrderUIAttackFlag         = new Translation<Flag>("Attack: {0}.", TranslationFlags.UnityUI, Flag.COLOR_SHORT_NAME_FORMAT);
    public static readonly Translation<Flag> OrderUIDefendObjective    = new Translation<Flag>("Defend your objective: {0}.", TranslationFlags.UnityUI, Flag.COLOR_SHORT_NAME_FORMAT);
    public static readonly Translation<Flag> OrderUIDefendFlag         = new Translation<Flag>("Defend: {0}.", TranslationFlags.UnityUI, Flag.COLOR_SHORT_NAME_FORMAT);
    public static readonly Translation<Cache> OrderUIAttackCache       = new Translation<Cache>("Attack: {0}.", TranslationFlags.UnityUI, FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<Cache> OrderUIDefendCache       = new Translation<Cache>("Defend: {0}.", TranslationFlags.UnityUI, FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<string> OrderUIAttackNearArea   = new Translation<string>("Attack near <#9dbccf>{0}</color>.", TranslationFlags.UnityUI);
    public static readonly Translation<string> OrderUIDefendNearArea   = new Translation<string>("Defend near <#9dbccf>{0}</color>.", TranslationFlags.UnityUI);
    public static readonly Translation<Flag> OrderUIBuildFobFlag       = new Translation<Flag>("Build a FOB on {0}.", TranslationFlags.UnityUI, Flag.COLOR_SHORT_NAME_FORMAT);
    public static readonly Translation<string> OrderUIBuildFobNearArea = new Translation<string>("Build a FOB near <#9dbccf>{0}</color>.", TranslationFlags.UnityUI, Flag.COLOR_SHORT_NAME_FORMAT);
    public static readonly Translation<Cache> OrderUIBuildFobNearCache = new Translation<Cache>("Build a FOB near {0}.", TranslationFlags.UnityUI, FOB.COLORED_NAME_FORMAT);
    #endregion

    #region Rallies
    public static readonly Translation RallySuccess         = new Translation("<#959c8c>You have <#5eff87>rallied</color> with your squad.");
    public static readonly Translation RallyActive          = new Translation("<#959c8c>Your squad has an active <#5eff87>RALLY POINT</color>. Do <#bfbfbf>/rally</color> to rally with your squad.");
    public static readonly Translation<int> RallyWait       = new Translation<int>("<#959c8c>Standby for <#5eff87>RALLY</color> in: <#ffe4b5>{0}s</color>. Do <#a3b4c7>/rally cancel</color> to abort.");
    public static readonly Translation RallyAbort           = new Translation("<#a1a1a1>Cancelled rally deployment.");
    public static readonly Translation RallyObstructed      = new Translation("<#959c8c><#bfbfbf>RALLY</color> is no longer available - there are enemies nearby.");
    public static readonly Translation RallyNoSquadmates    = new Translation("<#99918d>You need more squad members to use a <#bfbfbf>rally point</color>.");
    public static readonly Translation RallyNotSquadleader  = new Translation("<#99918d>You must be a <color=#cedcde>SQUAD LEADER</color> in order to place this.");
    public static readonly Translation RallyAlreadyQueued   = new Translation("<#99918d>You are already waiting on <#5eff87>rally</color> deployment. Do <#a3b4c7>/rally cancel</color> to abort.");
    public static readonly Translation RallyNotQueued       = new Translation("<#959c8c>You aren't waiting on a <#5eff87>rally</color> deployment.");
    public static readonly Translation RallyNotInSquad      = new Translation("<#959c8c>You must be in a squad to use <#5eff87>rallies</color>.");
    public static readonly Translation RallyObstructedPlace = new Translation("<#959c8c>This rally point is obstructed, find a more open place to put it.");
    public static readonly Translation<TimeSpan> RallyUI    = new Translation<TimeSpan>("<#5eff87>RALLY</color> {0}", TranslationFlags.UnityUI, "mm:ss");
    #endregion

    #region Time
    public static readonly Translation TimeSecondSingle = new Translation("second", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeSecondPlural = new Translation("seconds", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeMinuteSingle = new Translation("minute", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeMinutePlural = new Translation("minutes", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeHourSingle   = new Translation("hour", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeHourPlural   = new Translation("hours", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeDaySingle    = new Translation("day", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeDayPlural    = new Translation("days", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeWeekSingle   = new Translation("week", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeWeekPlural   = new Translation("weeks", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeMonthSingle  = new Translation("month", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeMonthsPlural = new Translation("months", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeYearSingle   = new Translation("year", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeYearsPlural  = new Translation("years", TranslationFlags.UnityUINoReplace);
    public static readonly Translation TimeAnd          = new Translation("and", TranslationFlags.UnityUINoReplace);
    #endregion

    #region FOBs and Buildables
    public static readonly Translation BuildNotInRadius        = new Translation("<#ffab87>This can only be placed inside <#cedcde>FOB RADIUS</color>.");
    public static readonly Translation BuildTickNotInRadius    = new Translation("<#ffab87>There's no longer a friendly FOB nearby.");
    public static readonly Translation<float> BuildSmallRadius = new Translation<float>("<#ffab87>This can only be placed within {0}m of this FOB Radio right now. Expand this range by building a <#cedcde>FOB BUNKER</color>.", "N0");
    public static readonly Translation<float> BuildNoRadio     = new Translation<float>("<#ffab87>This can only be placed within {0}m of a friendly <#cedcde>FOB RADIO</color>.", "N0");
    public static readonly Translation<BuildableData> BuildStructureExists     = new Translation<BuildableData>("<#ffab87>This FOB can't have any more {0}.", PLURAL);
    public static readonly Translation<BuildableData> BuildTickStructureExists = new Translation<BuildableData>("<#ffab87>Too many {0} have already been built on this FOB.", PLURAL);
    public static readonly Translation BuildEnemy              = new Translation("<#ffab87>You may not build on an enemy FOB.");
    public static readonly Translation<int, int> BuildMissingSupplies = new Translation<int, int>("<#ffab87>You're missing nearby build! <#d1c597>Building Supplies: <#e0d8b8>{0}/{1}</color></color>.");
    public static readonly Translation BuildMaxFOBsHit         = new Translation("<#ffab87>The max number of FOBs on your team has been reached.");
    public static readonly Translation BuildFOBUnderwater      = new Translation("<#ffab87>You can't build a FOB underwater.");
    public static readonly Translation<float> BuildFOBTooHigh  = new Translation<float>("<#ffab87>You can't build a FOB more than {0}m above the ground.", "F0");
    public static readonly Translation BuildFOBTooCloseToMain  = new Translation("<#ffab87>You can't build a FOB this close to main base.");
    public static readonly Translation BuildNoLogisticsVehicle = new Translation("<#ffab87>You must be near a friendly <#cedcde>LOGISTICS VEHICLE</color> to place a FOB radio.");
    public static readonly Translation<FOB, float, float> BuildFOBTooClose = new Translation<FOB, float, float>("<#ffa238>You are too close to an existing FOB Radio ({0}: {1}m away). You must be at least {2}m away to place a new radio.", FOB.COLORED_NAME_FORMAT, "F0", "F0");
    public static readonly Translation<FOB, GridLocation, string> FOBUI    = new Translation<FOB, GridLocation, string>("{0}  <#d6d2c7>{1}</color>  {2}", TranslationFlags.UnityUI, FOB.NAME_FORMAT);
    public static readonly Translation CacheDestroyedAttack    = new Translation("<#e8d1a7>WEAPONS CACHE HAS BEEN ELIMINATED", TranslationFlags.UnityUI);
    public static readonly Translation CacheDestroyedDefense   = new Translation("<#deadad>WEAPONS CACHE HAS BEEN DESTROYED", TranslationFlags.UnityUI);
    public static readonly Translation<string> CacheDiscoveredAttack = new Translation<string>("<#e8d1a7>NEW WEAPONS CACHE DISCOVERED NEAR <#e3c59a>{0}</color>", TranslationFlags.UnityUI, UPPERCASE);
    public static readonly Translation CacheDiscoveredDefense  = new Translation("<#d9b9a7>WEAPONS CACHE HAS BEEN COMPROMISED, DEFEND IT", TranslationFlags.UnityUI);
    public static readonly Translation CacheSpawnedDefense     = new Translation("<#a8e0a4>NEW WEAPONS CACHE IS NOW ACTIVE", TranslationFlags.UnityUI);
    #endregion

    #region Deploy
    public static readonly Translation<IDeployable> DeploySuccess           = new Translation<IDeployable>("<#fae69c>You have arrived at {0}.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<IDeployable> DeployNotSpawnableTick  = new Translation<IDeployable>("<#ffa238>{0} is no longer active.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<IDeployable> DeployNotSpawnable      = new Translation<IDeployable>("<#ffa238>{0} is not active.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<IDeployable> DeployDestroyed         = new Translation<IDeployable>("<#ffa238>{0} was destroyed.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<IDeployable> DeployNoBunker          = new Translation<IDeployable>("<#ffaa42>{0} doesn't have a <#cedcde>FOB BUNKER</color>. Your team must build one to use the <#cedcde>FOB</color> as a spawnpoint.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<IDeployable> DeployRadioDamaged      = new Translation<IDeployable>("<#ffaa42>The <#cedcde>FOB RADIO</color> at {0} is damaged. Repair it with an <#cedcde>ENTRENCHING TOOL</color>.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation DeployMoved                          = new Translation("<#ffa238>You moved and can no longer deploy.");
    public static readonly Translation<IDeployable> DeployEnemiesNearbyTick = new Translation<IDeployable>("<#ffa238>You no longer deploy to {0} - there are enemies nearby.", FOB.COLORED_NAME_FORMAT);
    public static readonly Translation<IDeployable> DeployEnemiesNearby     = new Translation<IDeployable>("<#ffaa42>You cannot deploy to {0} - there are enemies nearby.");
    public static readonly Translation DeployCancelled                      = new Translation("<#fae69c>Active deployment cancelled.");
    public static readonly Translation<string> DeployableNotFound           = new Translation<string>("<#ffa238>There is no location by the name of <#e3c27f>{0}</color>.", UPPERCASE);
    public static readonly Translation DeployNotNearFOB                     = new Translation("<#ffa238>You must be near a friendly <#cedcde>FOB</color> or in <#cedcde>MAIN BASE</color> in order to deploy.");
    public static readonly Translation DeployNotNearFOBInsurgency           = new Translation("<#ffa238>You must be near a friendly <#cedcde>FOB</color> or <#e8d1a7>CACHE</color>, or in <#cedcde>MAIN BASE</color> in order to deploy.");
    public static readonly Translation<Cooldown> DeployCooldown             = new Translation<Cooldown>("<#ffa238>You can deploy again in: <#e3c27f>{0}</color>", Cooldown.LONG_TIME_FORMAT);
    public static readonly Translation DeployAlreadyActive                  = new Translation("<#b5a591>You're already deploying somewhere.");
    public static readonly Translation<Cooldown> DeployInCombat             = new Translation<Cooldown>("<#ffaa42>You are in combat, soldier! You can deploy in another: <#e3987f>{0}</color>.", Cooldown.LONG_TIME_FORMAT);
    public static readonly Translation DeployInjured                        = new Translation("<#ffaa42>You can not deploy while injured, get a medic to revive you or give up.");
    public static readonly Translation DeployLobbyRemoved                   = new Translation("<#fae69c>The lobby has been removed, use  <#e3c27f>/teams</color> to switch teams instead.");
    #endregion

    #region Ammo
    public static readonly Translation AmmoNoTarget                = new Translation("<#ffab87>Look at an <#cedcde>AMMO CRATE</color>, <#cedcde>AMMO BAG</color> or <#cedcde>VEHICLE</color> in order to resupply.");
    public static readonly Translation<int, int> AmmoResuppliedKit = new Translation<int, int>("<#d1bda7>Resupplied kit. Consumed: <#d97568>{0} AMMO</color> <#948f8a>({1} left)</color>.");
    public static readonly Translation<int> AmmoResuppliedKitMain  = new Translation<int>("<#d1bda7>Resupplied kit. Consumed: <#d97568>{0} AMMO</color>.");
    public static readonly Translation AmmoAutoSupply              = new Translation("<#b3a6a2>This vehicle will <#cedcde>AUTO RESUPPLY</color> when in main. You can also use '<color=#c9bfad>/load <color=#d4c49d>build</color>|<color=#d97568>ammo</color> <amount></color>'.");
    public static readonly Translation AmmoNotNearFOB              = new Translation("<#b3a6a2>This ammo crate is not built on a friendly FOB.");
    public static readonly Translation<int, int> AmmoOutOfStock    = new Translation<int, int>("<#b3a6a2>Insufficient ammo. Required: <#d97568>{0}/{1} AMMO</color>.", VehicleData.COLORED_NAME);
    public static readonly Translation AmmoNoKit                   = new Translation("<#b3a6a2>You don't have a kit yet. Go request one from the armory in your team's headquarters.");
    public static readonly Translation<Cooldown> AmmoCooldown      = new Translation<Cooldown>("<#b7bab1>More <#cedcde>AMMO</color> arriving in: <color=#de95a8>{0}</color>", Cooldown.SHORT_TIME_FORMAT);
    public static readonly Translation AmmoNotRifleman             = new Translation("<#b7bab1>You must be a <#cedcde>RIFLEMAN</color> in order to place this <#cedcde>AMMO BAG</color>.");
    public static readonly Translation<VehicleData, int, int> AmmoResuppliedVehicle = new Translation<VehicleData, int, int>("<#d1bda7>Resupplied {0}. Consumed: <#d97568>{1} AMMO</color> <#948f8a>({2} left)</color>.", VehicleData.COLORED_NAME);
    public static readonly Translation<VehicleData, int> AmmoResuppliedVehicleMain  = new Translation<VehicleData, int>("<#d1bda7>Resupplied {0}. Consumed: <#d97568>{1} AMMO</color>.", VehicleData.COLORED_NAME);
    public static readonly Translation<VehicleData> AmmoVehicleCantRearm            = new Translation<VehicleData>("<#d1bda7>{0} can't be resupplied.", VehicleData.COLORED_NAME + PLURAL);
    public static readonly Translation<VehicleData> AmmoVehicleFullAlready          = new Translation<VehicleData>("<#b3a6a2>Your {0} does not need to be resupplied.", VehicleData.COLORED_NAME);
    public static readonly Translation<VehicleData> AmmoVehicleNotNearRepairStation = new Translation<VehicleData>("<#b3a6a2>Your {0} must be next to a <color=#e3d5ba>REPAIR STATION</color> in order to rearm.", VehicleData.COLORED_NAME);
    #endregion

    #region Load Command
    public static readonly Translation LoadNoTarget = new Translation("<#b3a6a2>Look at a friendly <#cedcde>LOGISTICS VEHICLE</color>.");
    public static readonly Translation LoadUsage = new Translation("<#b3a6a2>Try typing: '<#e6d1b3>/load ammo <amount></color>' or '<#e6d1b3>/load build <amount></color>'.");
    public static readonly Translation<string> LoadInvalidAmount = new Translation<string>("<#b3a6a2>'{0}' is not a valid amount of supplies.", UPPERCASE);
    public static readonly Translation LoadNotInMain = new Translation("<#b3a6a2>You must be in <#cedcde>MAIN</color> to load up this vehicle.");
    public static readonly Translation LoadNotLogisticsVehicle = new Translation("<#b3a6a2>Only <#cedcde>LOGISTICS VEHICLES</color> can be loaded with supplies.");
    public static readonly Translation LoadSpeed = new Translation("<#b3a6a2>You can only load supplies while the vehicle is stopped.");
    public static readonly Translation<int> LoadCompleteBuild = new Translation<int>("<#d1bda7>Loading complete. <#d4c49d>{0} BUILD</color> loaded.");
    public static readonly Translation<int> LoadCompleteAmmo = new Translation<int>("<#d1bda7>Loading complete. <#d97568>{0} AMMO</color> loaded.");
    #endregion

    #region Vehicles
    public static readonly Translation<VehicleData> VehicleStaging  = new Translation<VehicleData>("<#b3a6a2>You can't enter a {0} during the <#cedcde>STAGING PHASE</color>.", VehicleData.COLORED_NAME);
    public static readonly Translation<IPlayer> VehicleWaitForOwner = new Translation<IPlayer>("<#bda897>Only the owner, {0}, can enter the driver's seat right now.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer, Squad> VehicleWaitForOwnerOrSquad = new Translation<IPlayer, Squad>("<#bda897>Only the owner, {0}, or members of {1} Squad can enter the driver's seat right now.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, Squad.COLORED_NAME_FORMAT);
    public static readonly Translation VehicleNoKit = new Translation("<#ff684a>You can not get in a vehicle without a kit.");
    public static readonly Translation VehicleTooHigh = new Translation("<#ff684a>The vehicle is too high off the ground to exit.");
    public static readonly Translation<EClass> VehicleMissingKit = new Translation<EClass>("<#bda897>You need a <#cedcde>{0}</color> kit in order to man this vehicle.");
    public static readonly Translation VehicleDriverNeeded = new Translation("<#bda897>Your vehicle needs a <#cedcde>DRIVER</color> before you can switch to the gunner's seat on the battlefield.");
    public static readonly Translation VehicleAbandoningDriver = new Translation("<#bda897>You cannot abandon the driver's seat on the battlefield.");
    public static readonly Translation VehicleNoPassengerSeats = new Translation("<#bda897>There are no free passenger seats in this vehicle.");
    #endregion

    #region Signs
    private const string SECTION_SIGNS = "Signs";
        [TranslationData(Section = SECTION_SIGNS, SignId = "rules", Description = "Server rules")]
    public static readonly Translation SignRules = new Translation("Rules\nNo suicide vehicles.\netc.", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "kitdelay", Description = "Shown on new seasons when elite kits and loadouts are locked.")]
    public static readonly Translation SignKitDelay = new Translation("<#e6e6e6>All <#3bede1>Elite Kits</color> and <#32a852>Loadouts</color> are locked for the two weeks of the season.\nThey will be available again after <#d8addb>September 1st, 2022</color>.", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_squadleader")]
    public static readonly Translation SignClassDescriptionSquadleader   = new Translation("\n\n<#cecece>Help your squad by supplying them with <#f0a31c>rally points</color> and placing <#f0a31c>FOB radios</color>.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_rifleman")]
    public static readonly Translation SignClassDescriptionRifleman      = new Translation("\n\n<#cecece>Resupply your teammates in the field with an <#f0a31c>Ammo Bag</color>.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_medic")]
    public static readonly Translation SignClassDescriptionMedic         = new Translation("\n\n<#cecece><#f0a31c>Revive</color> your teammates after they've been injured.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_breacher")]
    public static readonly Translation SignClassDescriptionBreacher      = new Translation("\n\n<#cecece>Use <#f0a31c>high-powered explosives</color> to take out <#f01f1c>enemy FOBs</color>.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_autorifleman")]
    public static readonly Translation SignClassDescriptionAutoRifleman  = new Translation("\n\n<#cecece>Equipped with a high-capacity and powerful <#f0a31c>LMG</color> to spray-and-pray your enemies.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_machinegunner")]
    public static readonly Translation SignClassDescriptionMachineGunner = new Translation("\n\n<#cecece>Equipped with a powerful <#f0a31c>Machine Gun</color> to shred the enemy team in combat.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_lat")]
    public static readonly Translation SignClassDescriptionLAT           = new Translation("\n\n<#cecece>A balance between an anti-tank and combat loadout, used to conveniently destroy <#f01f1c>armored enemy vehicles</color>.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_hat")]
    public static readonly Translation SignClassDescriptionHAT           = new Translation("\n\n<#cecece>Equipped with multiple powerful <#f0a31c>anti-tank shells</color> to take out any vehicles.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_grenadier")]
    public static readonly Translation SignClassDescription              = new Translation("\n\n<#cecece>Equipped with a <#f0a31c>grenade launcher</color> to take out enemies behind cover or in light-armored vehicles.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_marksman")]
    public static readonly Translation SignClassDescriptionMarksman      = new Translation("\n\n<#cecece>Equipped with a <#f0a31c>marksman rifle</color> to take out enemies from medium to high distances.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_sniper")]
    public static readonly Translation SignClassDescriptionSniper        = new Translation("\n\n<#cecece>Equipped with a high-powered <#f0a31c>sniper rifle</color> to take out enemies from great distances.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_aprifleman")]
    public static readonly Translation SignClassDescriptionAPRifleman    = new Translation("\n\n<#cecece>Equipped with <#f0a31c>explosive traps</color> to cover entry-points and entrap enemy vehicles.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_engineer")]
    public static readonly Translation SignClassDescriptionEngineer      = new Translation("\n\n<#cecece>Features 200% <#f0a31c>build speed</color> and are equipped with <#f0a31c>fortifications</color> and traps to help defend their team's FOBs.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_crewman")]
    public static readonly Translation SignClassDescriptionCrewman       = new Translation("\n\n<#cecece>The only kits than can man <#f0a31c>armored vehicles</color>.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_pilot")]
    public static readonly Translation SignClassDescriptionPilot         = new Translation("\n\n<#cecece>The only kits that can fly <#f0a31c>aircraft</color>.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "class_desc_specops")]
    public static readonly Translation SignClassDescriptionSpecOps       = new Translation("\n\n<#cecece>Equipped with <#f0a31c>night-vision</color> to help see at night.</color>\n<#f01f1c>\\/</color>", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "bundle_misc")]
    public static readonly Translation SignBundleMisc       = new Translation("<#f0a31c>Misc.", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "bundle_caf")]
    public static readonly Translation SignBundleCanada     = new Translation("<#f0a31c>Canadian Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "bundle_fr")]
    public static readonly Translation SignBundleFrance     = new Translation("<#f0a31c>French Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "bundle_ger")]
    public static readonly Translation SignBundleGermany    = new Translation("<#f0a31c>German Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "bundle_usmc")]
    public static readonly Translation SignBundleUSMC       = new Translation("<#f0a31c>USMC Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "bundle_usa")]
    public static readonly Translation SignBundleUSA        = new Translation("<#f0a31c>USA Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "bundle_pl")]
    public static readonly Translation SignBundlePoland     = new Translation("<#f0a31c>Polish Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "bundle_idf")]
    public static readonly Translation SignBundleIsrael     = new Translation("<#f0a31c>IDF Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "bundle_militia")]
    public static readonly Translation SignBundleMilitia    = new Translation("<#f0a31c>Militia Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "bundle_ru")]
    public static readonly Translation SignBundleRussia     = new Translation("<#f0a31c>Russia Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "bundle_soviet")]
    public static readonly Translation SignBundleSoviet     = new Translation("<#f0a31c>Soviet Bundle", TranslationFlags.TMProSign);
        [TranslationData(Section = SECTION_SIGNS, SignId = "sign_loadout_info", Description = "Information on how to obtain a loadout.")]
    public static readonly Translation SignLoadoutInfo      = new Translation("<#cecece>Loadouts and elite kits can be purchased\nin our <#7483c4>Discord</color> server.</color>\n\n<#7483c4>/discord</color>", TranslationFlags.TMProSign);
    #endregion

    #region Kick Command
    public static readonly Translation NoReasonProvided                       = new Translation("<#9cffb3>You must provide a reason.");
    public static readonly Translation<IPlayer> KickSuccessFeedback           = new Translation<IPlayer>("<#00ffff>You kicked <#d8addb>{0}</color>.", UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer, IPlayer> KickSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#00ffff><#d8addb>{0}</color> was kicked by <#" + TeamManager.AdminColorHex + ">{1}</color>.", UCPlayer.CHARACTER_NAME_FORMAT, UCPlayer.PLAYER_NAME_FORMAT);
    public static readonly Translation<IPlayer> KickSuccessBroadcastOperator  = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was kicked by an operator.", UCPlayer.CHARACTER_NAME_FORMAT);
    #endregion

    #region Ban Command
    public static readonly Translation<IPlayer> BanPermanentSuccessFeedback           = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was <b>permanently</b> banned.", UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer, IPlayer> BanPermanentSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#00ffff><#d8addb>{0}</color> was <b>permanently</b> banned by <#" + TeamManager.AdminColorHex + ">{1}</color>.", UCPlayer.CHARACTER_NAME_FORMAT, UCPlayer.PLAYER_NAME_FORMAT);
    public static readonly Translation<IPlayer> BanPermanentSuccessBroadcastOperator  = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was <b>permanently</b> banned by an operator.", UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer, string> BanSuccessFeedback            = new Translation<IPlayer, string>("<#00ffff><#d8addb>{0}</color> was banned for <#9cffb3>{1}</color>.", UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer, IPlayer, string> BanSuccessBroadcast  = new Translation<IPlayer, IPlayer, string>("<#00ffff><#d8addb>{0}</color> was banned for <#9cffb3>{2}</color> by <#" + TeamManager.AdminColorHex + ">{1}</color>.", UCPlayer.CHARACTER_NAME_FORMAT, UCPlayer.PLAYER_NAME_FORMAT);
    public static readonly Translation<IPlayer, string> BanSuccessBroadcastOperator   = new Translation<IPlayer, string>("<#00ffff><#d8addb>{0}</color> was banned for <#9cffb3>{1}</color> by an operator.", UCPlayer.CHARACTER_NAME_FORMAT);
    #endregion

    #region Unban Command
    public static readonly Translation<IPlayer> UnbanNotBanned = new Translation<IPlayer>("<#9cffb3><#d8addb>{0}</color> is not currently banned.", UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer> UnbanSuccessFeedback = new Translation<IPlayer>("<#00ffff><#d8addb>{0}</color> was unbanned.", UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer, IPlayer> UnbanSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#00ffff><#d8addb>{0}</color> was unbanned by <#" + TeamManager.AdminColorHex + ">{1}</color>.", UCPlayer.CHARACTER_NAME_FORMAT, UCPlayer.PLAYER_NAME_FORMAT);
    public static readonly Translation<IPlayer> UnbanSuccessBroadcastOperator = new Translation<IPlayer>("<#ffff00><#d8addb>{0}</color> was unbanned by an operator.", UCPlayer.CHARACTER_NAME_FORMAT);
    #endregion
    
    #region Warn Command
    public static readonly Translation<IPlayer> WarnSuccessFeedback           = new Translation<IPlayer>("<#ffff00>You warned <#d8addb>{0}</color>.", UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer, IPlayer> WarnSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#ffff00><#d8addb>{0}</color> was warned by <#" + TeamManager.AdminColorHex + ">{1}</color>.", UCPlayer.CHARACTER_NAME_FORMAT, UCPlayer.PLAYER_NAME_FORMAT);
    public static readonly Translation<IPlayer> WarnSuccessBroadcastOperator  = new Translation<IPlayer>("<#ffff00><#d8addb>{0}</color> was warned by an operator.", UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer, string> WarnSuccessDM         = new Translation<IPlayer, string>("<#ffff00><#" + TeamManager.AdminColorHex + ">{0}</color> warned you for <#fff>{1}</color>.", UCPlayer.PLAYER_NAME_FORMAT);
    public static readonly Translation<string> WarnSuccessDMOperator          = new Translation<string>("<#ffff00>An operator warned you for <#fff>{0}</color>.", UCPlayer.PLAYER_NAME_FORMAT);
    #endregion
    
    #region Mute Command
    public static readonly Translation<IPlayer, IPlayer, EMuteType> MutePermanentSuccessFeedback = new Translation<IPlayer, IPlayer, EMuteType>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <b>permanently</b> <#cedcde>{2}</color> muted.", UCPlayer.CHARACTER_NAME_FORMAT, UCPlayer.STEAM_64_FORMAT, LOWERCASE);
    public static readonly Translation<IPlayer, IPlayer, string, EMuteType> MuteSuccessFeedback  = new Translation<IPlayer, IPlayer, string, EMuteType>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <#cedcde>{3}</color> muted for <#9cffb3>{2}</color>.", UCPlayer.CHARACTER_NAME_FORMAT, UCPlayer.STEAM_64_FORMAT, arg3Fmt: LOWERCASE);
    public static readonly Translation<IPlayer, IPlayer, EMuteType> MutePermanentSuccessBroadcastOperator  = new Translation<IPlayer, IPlayer, EMuteType>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <b>permanently</b> <#cedcde>{2}</color> muted by an operator.", UCPlayer.CHARACTER_NAME_FORMAT, UCPlayer.STEAM_64_FORMAT, LOWERCASE);
    public static readonly Translation<IPlayer, IPlayer, EMuteType, IPlayer> MutePermanentSuccessBroadcast = new Translation<IPlayer, IPlayer, EMuteType, IPlayer>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <b>permanently</b> <#cedcde>{2}</color> muted by <#" + TeamManager.AdminColorHex + ">{3}</color>.", UCPlayer.CHARACTER_NAME_FORMAT, UCPlayer.STEAM_64_FORMAT, LOWERCASE, UCPlayer.PLAYER_NAME_FORMAT);
    public static readonly Translation<IPlayer, IPlayer, string, EMuteType> MuteSuccessBroadcastOperator   = new Translation<IPlayer, IPlayer, string, EMuteType>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <#cedcde>{3}</color> muted by an operator for <#9cffb3>{2}</color>.", UCPlayer.CHARACTER_NAME_FORMAT, UCPlayer.STEAM_64_FORMAT, arg3Fmt: LOWERCASE);
    public static readonly Translation<IPlayer, IPlayer, string, EMuteType, IPlayer> MuteSuccessBroadcast  = new Translation<IPlayer, IPlayer, string, EMuteType, IPlayer>("<#00ffff><#d8addb>{0}</color> <#cedcde>({1})</color> was <#cedcde>{3}</color> muted by <#" + TeamManager.AdminColorHex + ">{4}</color> for <#9cffb3>{2}</color>.", UCPlayer.CHARACTER_NAME_FORMAT, UCPlayer.STEAM_64_FORMAT, arg3Fmt: LOWERCASE, arg4Fmt: UCPlayer.PLAYER_NAME_FORMAT);
    public static readonly Translation<IPlayer, string, string, EMuteType> MuteSuccessDM  = new Translation<IPlayer, string, string, EMuteType>("<#ffff00><#" + TeamManager.AdminColorHex + ">{0}</color> <#9cffb3>{3}</color> muted you for <#9cffb3>{2}</color> because: <#9cffb3>{1}</color>.", UCPlayer.PLAYER_NAME_FORMAT, arg3Fmt: LOWERCASE);
    public static readonly Translation<IPlayer, string, EMuteType> MuteSuccessDMPermanent = new Translation<IPlayer, string, EMuteType>("<#ffff00><#" + TeamManager.AdminColorHex + ">{0}</color> permanently <#9cffb3>{2}</color> muted you because: <#9cffb3>{1}</color>.", UCPlayer.PLAYER_NAME_FORMAT, arg2Fmt: LOWERCASE);
    public static readonly Translation<string, string, EMuteType> MuteSuccessDMOperator   = new Translation<string, string, EMuteType>("<#ffff00>An operator <#9cffb3>{2}</color> muted you for <#9cffb3>{1}</color> because: <#9cffb3>{0}</color>.", arg2Fmt: LOWERCASE);
    public static readonly Translation<string, EMuteType> MuteSuccessDMPermanentOperator  = new Translation<string, EMuteType>("<#ffff00>>An operator permanently <#9cffb3>{1}</color> muted you because: <#9cffb3>{0}</color>.", arg1Fmt: LOWERCASE);

    public static readonly Translation<string> MuteTextChatFeedbackPermanent  = new Translation<string>("<#ffff00>You're permanently muted in text chat because: <#9cffb3>{0}</color>.");
    public static readonly Translation<DateTime, string> MuteTextChatFeedback = new Translation<DateTime, string>("<#ffff00>You're muted in text chat until <#cedcde>{0}</color> UTC because <#9cffb3>{1}</color>.", "g");
    #endregion

    #region Unmute Command
    public static readonly Translation<IPlayer> UnmuteNotMuted                  = new Translation<IPlayer>("<#9cffb3><#d8addb>{0}</color> is not currently muted.", UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer> UnmuteSuccessFeedback           = new Translation<IPlayer>("<#ffff00><#d8addb>{0}</color> was unmuted.", UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer, IPlayer> UnmuteSuccessBroadcast = new Translation<IPlayer, IPlayer>("<#ffff00><#d8addb>{0}</color> was unmuted by <#" + TeamManager.AdminColorHex + ">{1}</color>.", UCPlayer.CHARACTER_NAME_FORMAT, UCPlayer.PLAYER_NAME_FORMAT);
    public static readonly Translation<IPlayer> UnmuteSuccessBroadcastOperator  = new Translation<IPlayer>("<#ffff00><#d8addb>{0}</color> was unmuted by an operator.", UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer> UnmuteSuccessDM                 = new Translation<IPlayer>("<#ffff00><#" + TeamManager.AdminColorHex + ">{0}</color> unmuted you.", UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation UnmuteSuccessDMOperator                  = new Translation("<#ffff00>An operator unmuted you.");
    #endregion

    #region Duty Command
    public static readonly Translation DutyOnFeedback            = new Translation("<#c6d4b8>You are now <#95ff4a>on duty</color>.");
    public static readonly Translation DutyOffFeedback           = new Translation("<#c6d4b8>You are now <#ff8c4a>off duty</color>.");
    public static readonly Translation<IPlayer> DutyOnBroadcast  = new Translation<IPlayer>("<#c6d4b8><#d9e882>{0}</color> is now <#95ff4a>on duty</color>.");
    public static readonly Translation<IPlayer> DutyOffBroadcast = new Translation<IPlayer>("<#c6d4b8><#d9e882>{0}</color> is now <#ff8c4a>off duty</color>.");
    #endregion

    #region Request
    public static readonly Translation<Kit> RequestSignSaved = new Translation<Kit>("<#a4baa9>Saved kit: <#ffebbd>{0}</color>.", Kit.ID_FORMAT);
    public static readonly Translation<Kit> RequestSignRemoved = new Translation<Kit>("<#a8918a>Removed kit sign: <#ffebbd>{0}</color>.", Kit.ID_FORMAT);
    public static readonly Translation RequestNoTarget = new Translation("<#a4baa9>You must be looking at a request sign or vehicle.");
    public static readonly Translation RequestSignAlreadySaved = new Translation("<#a4baa9>That sign is already saved.");
    public static readonly Translation RequestSignNotSaved = new Translation("<#a4baa9>That sign is not saved.");
    public static readonly Translation<int> RequestKitBought = new Translation<int>("<#c4a36a>Kit bought for <#c$credits$>C </color><#ffffff>{0}</color>. Request it with '<#b3b0ab>/request</color>'.");
    public static readonly Translation RequestKitNotRegistered = new Translation("<#a8918a>This kit has not been created yet.");
    public static readonly Translation RequestKitAlreadyEquipped = new Translation("<#a8918a>You already have this kit.");
    public static readonly Translation RequestMissingAccess = new Translation("<#a8918a>You already have this kit.");
    public static readonly Translation<int> RequestNotBought = new Translation<int>("<#99918d>Look at this sign and type '<#ffe2ab>/buy</color>' to unlock this kit permanently for <#c$credits$>C </color><#ffffff>{0}</color>.");
    public static readonly Translation<int, int> RequestKitCantAfford = new Translation<int, int>("<#a8918a>You are missing <#c$credits$>C </color><#ffffff>{0}</color> / <#c$credits$>C </color><#ffffff>{1}</color> needed to unlock this kit.");
    public static readonly Translation RequestNotBuyable = new Translation("<#a8918a>This kit cannot be purchased with credits.");
    public static readonly Translation<int> RequestKitLimited = new Translation<int>("<#a8918a>Your team already has a max of <#d9e882>{0}</color> players using this kit. Try again later.");
    public static readonly Translation<RankData> RequestKitLowLevel = new Translation<RankData>("<#b3ab9f>You must be <#ffc29c>{0}</color> to use this kit.", RankData.NAME_FORMAT);
    public static readonly Translation<QuestAsset> RequestKitQuestIncomplete = new Translation<QuestAsset>("<#b3ab9f>You have to complete {0} to request this kit.", BaseQuestData.COLOR_QUEST_ASSET_FORMAT);
    public static readonly Translation RequestKitNotSquadleader = new Translation("<#b3ab9f>You must be a <#cedcde>SQUAD LEADER</color> in order to get this kit.");
    public static readonly Translation RequestLoadoutNotOwned = new Translation("<#a8918a>You do not own this loadout.");
    public static readonly Translation<int, int> RequestVehicleCantAfford = new Translation<int, int>("<#a8918a>You are missing <#c$credits$>C </color><#ffffff>{0}</color> / <#c$credits$>C </color><#ffffff>{1}</color> needed to request this vehicle.");
    public static readonly Translation<Cooldown> RequestVehicleCooldown = new Translation<Cooldown>("<#b3ab9f>This vehicle can't be requested for another: <#ffe2ab>{0}</color>.", Cooldown.SHORT_TIME_FORMAT);
    public static readonly Translation RequestVehicleNotInSquad = new Translation("<#b3ab9f>You must be <#cedcde>IN A SQUAD</color> in order to request this vehicle.");
    public static readonly Translation RequestVehicleNoKit = new Translation("<#a8918a>Get a kit before you request vehicles.");
    public static readonly Translation<FactionInfo> RequestVehicleOtherTeam = new Translation<FactionInfo>("<#a8918a>You must be on {0} to request this vehicle.", FactionInfo.COLOR_DISPLAY_NAME_FORMAT);
    public static readonly Translation<EClass> RequestVehicleWrongClass = new Translation<EClass>("<#b3ab9f>You need a <#cedcde><uppercase>{0}</uppercase></color> kit in order to request this vehicle.");
    public static readonly Translation<RankData> RequestVehicleMissingLevels = new Translation<RankData>("<#b3ab9f>You must be <#ffc29c>{0}</color> to request this vehicle.");
    public static readonly Translation<Ranks.RankData> RequestVehicleRankIncomplete = new Translation<Ranks.RankData>("<#b3ab9f>You must be <#ffc29c>{0}</color> to request this vehicle.", Ranks.RankData.COLOR_NAME_FORMAT);
    public static readonly Translation<QuestAsset> RequestVehicleQuestIncomplete = new Translation<QuestAsset>("<#b3ab9f>You have to complete {0} to request this vehicle.", BaseQuestData.COLOR_QUEST_ASSET_FORMAT);
    public static readonly Translation<IPlayer> RequestVehicleAlreadyRequested = new Translation<IPlayer>("<#a8918a>This vehicle was already requested by {0}.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<VehicleData> RequestVehicleAlreadyOwned = new Translation<VehicleData>("<#a8918a>You already have a nearby {0}.", VehicleData.COLORED_NAME);
    public static readonly Translation<VehicleData> RequestVehicleSuccess = new Translation<VehicleData>("<#b3a591>This {0} is now yours to take into battle.", VehicleData.COLORED_NAME);

    #region Vehicle Request Delays
    public static readonly Translation<string> RequestVehicleTimeDelay = new Translation<string>("<#b3ab9f>This vehicle is delayed for another: <#94cfff>{0}</color>.");
    public static readonly Translation<Cache> RequestVehicleCacheDelayAtk1 = new Translation<Cache>("<#b3ab9f>Destroy <color=#94cfff>{0}</color> to request this vehicle.", FOB.NAME_FORMAT);
    public static readonly Translation<Cache> RequestVehicleCacheDelayDef1 = new Translation<Cache>("<#b3ab9f>You can't request this vehicle until you lose <color=#94cfff>{0}</color>.", FOB.NAME_FORMAT);
    public static readonly Translation RequestVehicleCacheDelayAtkUndiscovered1 = new Translation("<#b3ab9f><color=#94cfff>Discover and Destroy</color> the next cache to request this vehicle.");
    public static readonly Translation RequestVehicleCacheDelayDefUndiscovered1 = new Translation("<#b3ab9f>You can't request this vehicle until you've <color=#94cfff>uncovered and lost</color> your next cache.");
    public static readonly Translation<int> RequestVehicleCacheDelayMultipleAtk = new Translation<int>("<#b3ab9f>Destroy <#94cfff>{0} more caches</color> to request this vehicle.");
    public static readonly Translation<int> RequestVehicleCacheDelayMultipleDef = new Translation<int>("<#b3ab9f>You can't request this vehicle until you've lost <#94cfff>{0} more caches</color>.");
    public static readonly Translation<Flag> RequestVehicleFlagDelay1 = new Translation<Flag>("<#b3ab9f>Capture {0} to request this vehicle.", Flag.COLOR_NAME_DISCOVER_FORMAT);
    public static readonly Translation<Flag> RequestVehicleLoseFlagDelay1 = new Translation<Flag>("<#b3ab9f>You can't request this vehicle until you lose {0}.", Flag.COLOR_NAME_DISCOVER_FORMAT);
    public static readonly Translation<int> RequestVehicleFlagDelayMultiple = new Translation<int>("<#b3ab9f>Capture <#94cfff>{0} more flags</color> to request this vehicle.");
    public static readonly Translation<int> RequestVehicleLoseFlagDelayMultiple = new Translation<int>("<#b3ab9f>You can't request this vehicle until you lose <#94cfff>{0} more flags</color>.");
    public static readonly Translation RequestVehicleStagingDelay = new Translation("<#a6918a>This vehicle can only be requested after the game starts.");
    public static readonly Translation RequestVehicleUnknownDelay = new Translation("<#b3ab9f>This vehicle is delayed because: <#94cfff>{0}</color>.");
    #endregion

    #endregion

    #region Strutures
    public static readonly Translation StructureNoTarget = new Translation("<#ff8c69>You must be looking at a barricade, structure, or vehicle.");
    public static readonly Translation<Structures.Structure> StructureSaved = new Translation<Structures.Structure>("<#e6e3d5>Saved <#c6d4b8>{0}</color>.");
    public static readonly Translation<Structures.Structure> StructureAlreadySaved = new Translation<Structures.Structure>("<#e6e3d5><#c6d4b8>{0}</color> is already saved.");
    public static readonly Translation<Structures.Structure> StructureUnsaved = new Translation<Structures.Structure>("<#e6e3d5>Removed <#c6d4b8>{0}</color> save.");
    public static readonly Translation<Structures.Structure> StructureAlreadyUnsaved = new Translation<Structures.Structure>("<#ff8c69><#c6d4b8>{0}</color> is not saved.");
    public static readonly Translation<ItemAsset> StructureDestroyed = new Translation<ItemAsset>("<#e6e3d5>Destroyed <#c6d4b8>{0}</color>.");
    public static readonly Translation StructureNotDestroyable = new Translation("<#ff8c69>That object can not be destroyed.");
    public static readonly Translation StructureExamineNotExaminable = new Translation("<#ff8c69>That object can not be examined.");
    public static readonly Translation StructureExamineNotLocked = new Translation("<#ff8c69>This vehicle is not locked.");
    public static readonly Translation<Asset, IPlayer, FactionInfo> StructureExamineLastOwnerPrompt = new Translation<Asset, IPlayer, FactionInfo>("Last owner of {0}: {1}, Team: {2}.", arg1Fmt: UCPlayer.PLAYER_NAME_FORMAT, arg2Fmt: FactionInfo.DISPLAY_NAME_FORMAT);
    public static readonly Translation<Asset, IPlayer, IPlayer, FactionInfo> StructureExamineLastOwnerChat = new Translation<Asset, IPlayer, IPlayer, FactionInfo>("<#c6d4b8>Last owner of <#e6e3d5>{0}</color>: {1} <i>({2})</i>, Team: {3}.", RARITY_COLOR_FORMAT, arg1Fmt: UCPlayer.COLOR_PLAYER_NAME_FORMAT, arg2Fmt: UCPlayer.STEAM_64_FORMAT, arg3Fmt: FactionInfo.COLOR_DISPLAY_NAME_FORMAT);
    #endregion

    #region Whitelist
    public static readonly Translation<ItemAsset> WhitelistAdded = new Translation<ItemAsset>("<#a0ad8e>Whitelisted item: {0}.", RARITY_COLOR_FORMAT);
    public static readonly Translation<ItemAsset> WhitelistRemoved = new Translation<ItemAsset>("<#a0ad8e>Removed whitelist for: {0}.", RARITY_COLOR_FORMAT);
    public static readonly Translation<ItemAsset> WhitelistAlreadyAdded = new Translation<ItemAsset>("<#ff8c69>{0} is already whitelisted.", RARITY_COLOR_FORMAT);
    public static readonly Translation<ItemAsset> WhitelistAlreadyRemoved = new Translation<ItemAsset>("<#ff8c69>{0} is not whitelisted.", RARITY_COLOR_FORMAT);
    public static readonly Translation<string> WhitelistItemNotID = new Translation<string>("<#ff8c69><uppercase>{0}</uppercase> couldn't be read as an <#cedcde>ITEM ID</color>.");
    public static readonly Translation<string> WhitelistInvalidAmount = new Translation<string>("<#ff8c69><uppercase>{0}</uppercase> couldn't be read as a <#cedcde>AMOUNT</color> (1-250).");
    public static readonly Translation<ItemAsset> WhitelistProhibitedPickup = new Translation<ItemAsset>("<#ff8c69>{0} can't be picked up.", RARITY_COLOR_FORMAT + PLURAL);
    public static readonly Translation<ItemAsset> WhitelistProhibitedSalvage = new Translation<ItemAsset>("<#ff8c69>{0} can't be salvaged.", RARITY_COLOR_FORMAT + PLURAL);
    public static readonly Translation<ItemAsset> WhitelistProhibitedPickupAmt = new Translation<ItemAsset>("<#ff8c69>You can't carry any more {0}.", RARITY_COLOR_FORMAT + PLURAL);
    public static readonly Translation<ItemAsset> WhitelistProhibitedPlace = new Translation<ItemAsset>("<#ff8c69>You're not allowed to place {0}.", RARITY_COLOR_FORMAT + PLURAL);
    public static readonly Translation<int, ItemAsset> WhitelistProhibitedPlaceAmt = new Translation<int, ItemAsset>("<#ff8c69>You're not allowed to place more than {0} {1}.", RARITY_COLOR_FORMAT + PLURAL + "{0}");
    public static readonly Translation WhitelistNoKit = new Translation("<#ff8c69>Get a kit first before you can pick up items.");
    #endregion

    #region Vehicles
    public static readonly Translation VehicleEnterGameNotStarted = new Translation("<#ff8c69>You may not enter a vehicle right now, the game has not started.");
    public static readonly Translation<VehicleAsset> VehicleBayAdded = new Translation<VehicleAsset>("<#a0ad8e>Added {0} to the vehicle bay.", RARITY_COLOR_FORMAT);
    public static readonly Translation<VehicleAsset> VehicleBayRemoved = new Translation<VehicleAsset>("<#a0ad8e>Removed {0} from the vehicle bay.", RARITY_COLOR_FORMAT);
    public static readonly Translation<string, VehicleAsset, string> VehicleBaySetProperty = new Translation<string, VehicleAsset, string>("<#a0ad8e>Set <#8ce4ff>{0}</color> for vehicle {1} to: <#ffffff>{2}</color>.", arg1Fmt: RARITY_COLOR_FORMAT);
    public static readonly Translation<VehicleAsset, int> VehicleBaySavedMeta = new Translation<VehicleAsset, int>("<#a0ad8e>Successfuly set the rearm list for vehicle {0} from your inventory. It will now drop <#8ce4ff>{1}</color> item(s) with /ammo.", RARITY_COLOR_FORMAT);
    public static readonly Translation<VehicleAsset, int> VehicleBayClearedItems = new Translation<VehicleAsset, int>("<#a0ad8e>Successfuly set the rearm list for vehicle {0} from your inventory. It will now drop <#8ce4ff>{1}</color> item(s) with /ammo.", RARITY_COLOR_FORMAT);
    public static readonly Translation<byte, VehicleAsset> VehicleBaySeatAdded = new Translation<byte, VehicleAsset>("<#a0ad8e>Made seat <#ffffff>#{0}</color> a crewman seat for {1}.", arg1Fmt: RARITY_COLOR_FORMAT);
    public static readonly Translation<byte, VehicleAsset> VehicleBaySeatRemoved = new Translation<byte, VehicleAsset>("<#a0ad8e>Seat <#ffffff>#{0}</color> is no longer a crewman seat for {1}.", arg1Fmt: RARITY_COLOR_FORMAT);
    public static readonly Translation VehicleBayNoTarget = new Translation("<#ff8c69>Look at a vehicle, spawn pad, or sign to use this command.");
    public static readonly Translation<VehicleAsset> VehicleBayAlreadyAdded = new Translation<VehicleAsset>("<#ff8c69>{0} is already added to the vehicle bay.", RARITY_COLOR_FORMAT);
    public static readonly Translation<VehicleAsset> VehicleBayNotAdded = new Translation<VehicleAsset>("<#ff8c69>{0} has not been added to the vehicle bay.", RARITY_COLOR_FORMAT);
    public static readonly Translation<string> VehicleBayInvalidProperty = new Translation<string>("<#ff8c69>{0} isn't a valid a vehicle property. Try putting 'level', 'team', 'rearmcost' etc.");
    public static readonly Translation<string, string> VehicleBayInvalidSetValue = new Translation<string, string>("<#ff8c69><#ddd>{0}</color> isn't a valid value for vehicle property: <#a0ad8e>{1}</color>.");
    public static readonly Translation<string> VehicleBayNotJsonSettable = new Translation<string>("<#ff8c69><#a0ad8e>{0}</color> is not marked as settable.");
    public static readonly Translation<byte, VehicleAsset> VehicleBayCrewSeatAlreadySet = new Translation<byte, VehicleAsset>("<#ff8c69><#ffffff>#{0}</color> is already marked as a crew seat in {1}.", arg1Fmt: RARITY_COLOR_FORMAT + PLURAL);
    public static readonly Translation<byte, VehicleAsset> VehicleBayCrewSeatNotSet = new Translation<byte, VehicleAsset>("<#ff8c69><#ffffff>#{0}</color> isn't marked as a crew seat in {1}.", arg1Fmt: RARITY_COLOR_FORMAT + PLURAL);
    public static readonly Translation<EDelayType, float, string?> VehicleBayAddedDelay = new Translation<EDelayType, float, string?>("<#a0ad8e>Added delay of type <#fff>{0}</color>:<#ddd>{1}</color> during <#ddd>{2}</color> gamemode.", arg1Fmt: "N1");
    public static readonly Translation<int> VehicleBayRemovedDelay = new Translation<int>("<#a0ad8e>Removed {0} matching delays.");
    public static readonly Translation<VehicleAsset> VehicleBaySpawnRegistered = new Translation<VehicleAsset>("<#a0ad8e>Successfully registered spawn. {0} will spawn here.", RARITY_COLOR_FORMAT + PLURAL);
    public static readonly Translation<VehicleAsset> VehicleBaySpawnDeregistered = new Translation<VehicleAsset>("<#a0ad8e>Successfully deregistered {0} spawn.", RARITY_COLOR_FORMAT);
    public static readonly Translation VehicleBayLinkStarted = new Translation("<#a0ad8e>Started linking, do <#ddd>/vb link</color> on the sign now.");
    public static readonly Translation<VehicleAsset> VehicleBayLinkFinished = new Translation<VehicleAsset>("<#a0ad8e>Successfully linked vehicle sign to a {0} vehicle bay.", RARITY_COLOR_FORMAT);
    public static readonly Translation<VehicleAsset> VehicleBayUnlinked = new Translation<VehicleAsset>("<#a0ad8e>Successfully unlinked {0} vehicle sign.", RARITY_COLOR_FORMAT);
    public static readonly Translation VehicleBayLinkNotStarted = new Translation("<#ff8c69>You must do /vb link on a vehicle bay first.");
    public static readonly Translation<VehicleAsset> VehicleBayForceSuccess = new Translation<VehicleAsset>("<#a0ad8e>Skipped timer for that {0} vehicle bay.", RARITY_COLOR_FORMAT);
    public static readonly Translation<string> VehicleBayInvalidInput = new Translation<string>("<#ff8c69><#fff>{0}</color> is not a valid vehicle.");
    public static readonly Translation<ItemAsset> VehicleBayInvalidBayItem = new Translation<ItemAsset>("<#ff8c69>{0} are not valid vehicle bays.", RARITY_COLOR_FORMAT + PLURAL);
    public static readonly Translation<VehicleAsset> VehicleBaySpawnAlreadyRegistered = new Translation<VehicleAsset>("<#ff8c69>This spawn is already registered to a {0}. Unregister it first with <#fff>/vb unreg</color>.", RARITY_COLOR_FORMAT);
    public static readonly Translation VehicleBaySpawnNotRegistered = new Translation("<#ff8c69>This vehicle bay is not registered.");
    public static readonly Translation<uint, VehicleAsset, ushort> VehicleBayCheck = new Translation<uint, VehicleAsset, ushort>("<#a0ad8e>This spawn (<#8ce4ff>{0}</color>) is registered with vehicle: {1} <#fff>({2})</color>.", arg1Fmt: RARITY_COLOR_FORMAT);
    #endregion

    #region Vehicle Deaths
    public static readonly Translation<IPlayer, VehicleAsset, ItemAsset, float> VehicleDestroyed = new Translation<IPlayer, VehicleAsset, ItemAsset, float>("<#c$death_background$>{0} took out a {1} with a {2} from {3}m away.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, RARITY_COLOR_FORMAT, RARITY_COLOR_FORMAT, "F0");
    public static readonly Translation<IPlayer, VehicleAsset> VehicleDestroyedUnknown = new Translation<IPlayer, VehicleAsset>("<#c$death_background$>{0} took out a {1}.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, RARITY_COLOR_FORMAT);
    public static readonly Translation<IPlayer, VehicleAsset> VehicleTeamkilled = new Translation<IPlayer, VehicleAsset>("<#c$death_background_teamkill$>{0} blew up a friendly {1}.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, RARITY_COLOR_FORMAT);
    #endregion

    #region Officers
    public static readonly Translation<RankData, FactionInfo> OfficerPromoted = new Translation<RankData, FactionInfo>("<#9e9788>Congratulations, you have been <#e3b552>PROMOTED</color> to <#e05353>{0}</color> of {1}!", RankData.NAME_FORMAT, FactionInfo.COLOR_DISPLAY_NAME_FORMAT);
    public static readonly Translation<RankData, FactionInfo> OfficerDemoted = new Translation<RankData, FactionInfo>("<#9e9788>You have been <#c47f5c>DEMOTED</color> to <#e05353>{0}</color> of {1}.", RankData.NAME_FORMAT, FactionInfo.COLOR_DISPLAY_NAME_FORMAT);
    public static readonly Translation OfficerDischarged = new Translation("<#9e9788>You have been <color=#ab2e2e>DISCHARGED</color> from the officer ranks for unacceptable behaviour.");
    public static readonly Translation<IPlayer, RankData, FactionInfo> OfficerPromotedBroadcast = new Translation<IPlayer, RankData, FactionInfo>("<#9e9788>{0} has been <#e3b552>PROMOTED</color> to <#e05353>{1}</color> of {2}!", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, RankData.NAME_FORMAT, FactionInfo.COLOR_DISPLAY_NAME_FORMAT);
    public static readonly Translation<IPlayer, RankData, FactionInfo> OfficerDemotedBroadcast = new Translation<IPlayer, RankData, FactionInfo>("<#9e9788>{0} has been <#c47f5c>DEMOTED</color> to <#e05353>{1}</color> of {2}.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, RankData.NAME_FORMAT, FactionInfo.COLOR_DISPLAY_NAME_FORMAT);
    public static readonly Translation<IPlayer, RankData> OfficerDischargedBroadcast = new Translation<IPlayer, RankData>("<#9e9788>{0} has been <#ab2e2e>DISCHARGED</color> from the rank of <#e05353>{1}s</color> for unacceptable behaviour.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, RankData.NAME_FORMAT);
    public static readonly Translation<int, int> OfficerInvalidRank = new Translation<int, int>("<#b08989><#ddd>{0}</color> is not a valid officer level. Try numbers <#ddd>1</color> - <#ddd>{1}</color>.");
    public static readonly Translation<IPlayer, int, int> OfficerChangedRankFeedback = new Translation<IPlayer, int, int>("<#c6d6c1>{0}'s officer rank was successfully changed to <#ddd>{1}</color> of <#ddd>{2}</color>.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer> OfficerDischargedFeedback = new Translation<IPlayer>("<#c6d6c1>{0} was successfully discharged.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    #endregion

    #region Clear
    public static readonly Translation ClearNoPlayerConsole = new Translation("Specify a player name when clearing from console.", TranslationFlags.NoColor);
    public static readonly Translation ClearInventorySelf = new Translation("<#e6e3d5>Cleared your inventory.");
    public static readonly Translation<IPlayer> ClearInventoryOther = new Translation<IPlayer>("<#e6e3d5>Cleared {0}'s inventory.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation ClearItems = new Translation("<#e6e3d5>Cleared all dropped items.");
    public static readonly Translation<IPlayer> ClearItemsOther = new Translation<IPlayer>("<#e6e3d5>Cleared {0}'s dropped items.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation ClearStructures = new Translation("<#e6e3d5>Cleared all placed structures and barricades.");
    public static readonly Translation ClearVehicles = new Translation("<#e6e3d5>Cleared all vehicles.");
    #endregion

    #region Shutdown
    public static readonly Translation<string> ShutdownBroadcastAfterGame = new Translation<string>("<#00ffff>A shutdown has been scheduled after this game because: \"<#6699ff>{0}</color>\".");
    public static readonly Translation ShutdownBroadcastDaily = new Translation("<#00ffff>A daily restart will occur after this game. Down-time estimate: <#6699ff>2 minutes</color>.");
    public static readonly Translation ShutdownBroadcastCancelled = new Translation("<#00ffff>The scheduled shutdown has been canceled.");
    public static readonly Translation<string, string> ShutdownBroadcastTime = new Translation<string, string>("<#00ffff>A shutdown has been scheduled in {0} because: \"<color=#6699ff>{1}</color>\".");
    public static readonly Translation<string> ShutdownBroadcastReminder = new Translation<string>("<#00ffff>A shutdown is scheduled to occur after this game because: \"<#6699ff>{0}</color>\".");
    #endregion

    #region Request Signs
    public static readonly Translation KitExclusive = new Translation("<#aaa>EXCLUSIVE</color>", TranslationFlags.NoColor);
    public static readonly Translation<string> KitName = new Translation<string>("<b>{0}</b>", TranslationFlags.NoColor);
    public static readonly Translation<string> KitWeapons = new Translation<string>("<b>{0}</b>", TranslationFlags.NoColor);
    public static readonly Translation<float> KitPremiumCost = new Translation<float>("$ {0}", TranslationFlags.NoColor, "N2");
    [TranslationData(FormattingDescriptions = new string[] { "Level", "Color depending on player's current level." })]
    public static readonly Translation<float, Color> KitRequiredLevel = new Translation<float, Color>("<#{1}>{0}</color>", TranslationFlags.NoColor);
    [TranslationData(FormattingDescriptions = new string[] { "Rank", "Color depending on player's current rank." })]
    public static readonly Translation<float, Color> KitRequiredRank = new Translation<float, Color>("<#{1}>Rank: {0}</color>", TranslationFlags.NoColor);
    [TranslationData(FormattingDescriptions = new string[] { "Quest", "Color depending on whether the player has completed the quest." })]
    public static readonly Translation<float, Color> KitRequiredQuest = new Translation<float, Color>("<#{1}>Quest: <#fff>{0}</color></color>", TranslationFlags.NoColor);
    [TranslationData(FormattingDescriptions = new string[] { "Number of quests needed.", "Color depending on whether the player has completed the quest(s).", "s if {0} != 1" })]
    public static readonly Translation<int, Color, string> KitRequiredQuestsMultiple = new Translation<int, Color, string>("<#{1}>Finish <#fff>{0}</color> quest{2}.</color>", TranslationFlags.NoColor);
    public static readonly Translation KitRequiredQuestsComplete = new Translation("<#ff974d>Kit Unlocked</color>", TranslationFlags.NoColor);
    public static readonly Translation KitPremiumOwned = new Translation("OWNED", TranslationFlags.NoColor);
    public static readonly Translation<int> KitCreditCost = new Translation<int>("<#c$credits$>C</color> <#fff>{0}</color>", TranslationFlags.NoColor);
    public static readonly Translation KitUnlimited = new Translation("unlimited", TranslationFlags.NoColor);
    public static readonly Translation<int, int> KitPlayerCount = new Translation<int, int>("{0}/{1}", TranslationFlags.NoColor);
    [Obsolete(@"Remember to put a \n after this.")]
    public static readonly Translation<int> LoadoutName = new Translation<int>("LOADOUT {0}", TranslationFlags.NoColor);
    #endregion

    #region Vehicle Bay Signs
    public static readonly Translation<int> VBSTickets = new Translation<int>("<#fff>{0}</color> <#f0f0f0>Tickets</color>", TranslationFlags.NoColor);
    public static readonly Translation VBSStateReady = new Translation("<#33cc33>Ready!</color> <#aaa><b>/request</b></color>", TranslationFlags.NoColor);
    [TranslationData(FormattingDescriptions = new string[] { "Minutes", "Seconds" })]
    public static readonly Translation<int, int> VBSStateDead = new Translation<int, int>("<#ff0000>{0}:{1}</color>", TranslationFlags.NoColor);
    [TranslationData(FormattingDescriptions = new string[] { "Nearest location." })]
    public static readonly Translation<string> VBSStateActive = new Translation<string>("<#ff9933>{0}</color>", TranslationFlags.NoColor);
    [TranslationData(FormattingDescriptions = new string[] { "Minutes", "Seconds" })]
    public static readonly Translation<int, int> VBSStateIdle = new Translation<int, int>("<#ffcc00>Idle: {0}:{1}</color>", TranslationFlags.NoColor);
    public static readonly Translation VBSDelayStaging = new Translation("<#94cfff>Locked Until Start</color>", TranslationFlags.NoColor);
    [TranslationData(FormattingDescriptions = new string[] { "Minutes", "Seconds" })]
    public static readonly Translation<int, int> VBSDelayTime = new Translation<int, int>("<#94cfff>Locked: {0}:{1}</color>", TranslationFlags.NoColor);
    public static readonly Translation<Flag> VBSDelayCaptureFlag = new Translation<Flag>("<#94cfff>Capture {0}</color>", TranslationFlags.NoColor, Flag.SHORT_NAME_DISCOVER_FORMAT);
    public static readonly Translation<Flag> VBSDelayLoseFlag = new Translation<Flag>("<#94cfff>Lose {0}</color>", TranslationFlags.NoColor, Flag.SHORT_NAME_DISCOVER_FORMAT);
    public static readonly Translation<int> VBSDelayLoseFlagMultiple = new Translation<int>("<#94cfff>Lose {0} more flags.</color>", TranslationFlags.NoColor);
    public static readonly Translation<int> VBSDelayCaptureFlagMultiple = new Translation<int>("<#94cfff>Capture {0} more flags.</color>", TranslationFlags.NoColor);
    public static readonly Translation<Cache> VBSDelayAttackCache = new Translation<Cache>("<#94cfff>Destroy {0}</color>", TranslationFlags.NoColor, FOB.CLOSEST_LOCATION_FORMAT);
    public static readonly Translation VBSDelayAttackCacheUnknown = new Translation("<#94cfff>Destroy Next Cache</color>", TranslationFlags.NoColor);
    public static readonly Translation VBSDelayAttackCacheMultiple = new Translation("<#94cfff>Destroy {0} more caches.</color>", TranslationFlags.NoColor);
    public static readonly Translation<Cache> VBSDelayDefendCache = new Translation<Cache>("<#94cfff>Lose {0}</color>", TranslationFlags.NoColor, FOB.CLOSEST_LOCATION_FORMAT);
    public static readonly Translation VBSDelayDefendCacheUnknown = new Translation("<#94cfff>Lose Next Cache</color>", TranslationFlags.NoColor);
    public static readonly Translation VBSDelayDefendCacheMultiple = new Translation("<#94cfff>Lose {0} more caches.</color>", TranslationFlags.NoColor);
    #endregion

    #region Revives
    public static readonly Translation ReviveNotMedic = new Translation("<#bdae9d>Only a <color=#ff758f>MEDIC</color> can heal or revive teammates.");
    public static readonly Translation ReviveHealEnemies = new Translation("<#bdae9d>You cannot aid enemy soldiers.");
    #endregion

    #region Reload Command
    public static readonly Translation ReloadedAll = new Translation("<#e6e3d5>Reloaded all Uncreated Warfare components.");
    public static readonly Translation ReloadedTranslations = new Translation("<#e6e3d5>Reloaded all translation files.");
    public static readonly Translation ReloadedFlags = new Translation("<#e6e3d5>Reloaded flag data.");
    public static readonly Translation ReloadFlagsInvalidGamemode = new Translation("<#ff8c69>You must be on a flag gamemode to use this command!");
    public static readonly Translation ReloadedPermissions = new Translation("<#e6e3d5>Reloaded the permission saver file.");
    public static readonly Translation ReloadedGeneric = new Translation("<#e6e3d5>Reloaded the '{0}' module.");
    public static readonly Translation ReloadedTCP = new Translation("<#e6e3d5>Tried to close any existing TCP connection to UCDiscord and re-open it.");
    public static readonly Translation ReloadedSQL = new Translation("<#e6e3d5>Reopened the MySql Connection.");
    #endregion

    #region Debug Commands
    public static readonly Translation<string> DebugNoMethod = new Translation<string>("<#ff8c69>No method found called <#ff758f>{0}</color>.");
    public static readonly Translation<string, string> DebugErrorExecuting = new Translation<string, string>("<#ff8c69>Ran into an error while executing: <#ff758f>{0} - {1}</color>.");
    public static readonly Translation<string> DebugMultipleMatches = new Translation<string>("<#ff8c69>Multiple methods match <#ff758f>{0}</color>.");
    #endregion

    #region Phases
    public static readonly Translation PhaseBriefing                      = new Translation("BRIEFING PHASE", TranslationFlags.UnityUI);
    public static readonly Translation PhasePreparation                   = new Translation("PREPARATION PHASE", TranslationFlags.UnityUI);
    public static readonly Translation PhaseBreifingInvasionAttack        = new Translation("BRIEFING PHASE", TranslationFlags.UnityUI);
    public static readonly Translation<Flag> PhaseBreifingInvasionDefense = new Translation<Flag>("PREPARATION PHASE\nFORTIFY {0}", TranslationFlags.UnityUI);
    #endregion

    #region XP Toasts
    public static readonly Translation XPToastFromOperator = new Translation("FROM OPERATOR", TranslationFlags.UnityUI);
    public static readonly Translation XPToastFromPlayer = new Translation("FROM ADMIN", TranslationFlags.UnityUI);
    public static readonly Translation XPToastHealedTeammate = new Translation("HEALED TEAMMATE", TranslationFlags.UnityUI);
    public static readonly Translation XPToastEnemyInjured = new Translation("<color=#e3e3e3>DOWNED</color>", TranslationFlags.UnityUI);
    public static readonly Translation XPToastFriendlyInjured = new Translation("<color=#e3e3e3>DOWNED FRIENDLY</color>", TranslationFlags.UnityUI);
    public static readonly Translation XPToastEnemyKilled = new Translation("KILLED ENEMY", TranslationFlags.UnityUI);
    public static readonly Translation XPToastKillAssist = new Translation("ASSIST", TranslationFlags.UnityUI);
    public static readonly Translation XPToastKillVehicleAssist = new Translation("VEHICLE ASSIST", TranslationFlags.UnityUI);
    public static readonly Translation XPToastKillDriverAssist = new Translation("DRIVER ASSIST", TranslationFlags.UnityUI);
    public static readonly Translation XPToastSpotterAssist = new Translation("SPOTTER", TranslationFlags.UnityUI);
    public static readonly Translation XPToastFriendlyKilled = new Translation("TEAMKILLED", TranslationFlags.UnityUI);
    public static readonly Translation XPToastFOBDestroyed = new Translation("FOB DESTROYED", TranslationFlags.UnityUI);
    public static readonly Translation XPToastFriendlyFOBDestroyed = new Translation("FRIENDLY FOB DESTROYED", TranslationFlags.UnityUI);
    public static readonly Translation XPToastFOBUsed = new Translation("FOB IN USE", TranslationFlags.UnityUI);
    public static readonly Translation XPToastSuppliesUnloaded = new Translation("RESUPPLIED FOB", TranslationFlags.UnityUI);
    public static readonly Translation XPToastResuppliedTeammate = new Translation("RESUPPLIED TEAMMATE", TranslationFlags.UnityUI);
    public static readonly Translation XPToastRepairedVehicle = new Translation("REPAIRED VEHICLE", TranslationFlags.UnityUI);
    public static readonly Translation XPToastFOBRepairedVehicle = new Translation("FOB REPAIRED VEHICLE", TranslationFlags.UnityUI);
    public static readonly Translation<EVehicleType> XPToastVehicleDestroyed = new Translation<EVehicleType>("{0} DESTROYED", TranslationFlags.UnityUI, UPPERCASE);
    public static readonly Translation<EVehicleType> XPToastAircraftDestroyed = new Translation<EVehicleType>("{0} SHOT DOWN", TranslationFlags.UnityUI, UPPERCASE);
    public static readonly Translation XPToastTransportingPlayers = new Translation("TRANSPORTING PLAYERS", TranslationFlags.UnityUI);

    public static readonly Translation XPToastFlagCaptured = new Translation("FLAG CAPTURED", TranslationFlags.UnityUI);
    public static readonly Translation XPToastFlagNeutralized = new Translation("FLAG NEUTRALIZED", TranslationFlags.UnityUI);
    public static readonly Translation XPToastFlagAttackTick = new Translation("ATTACK", TranslationFlags.UnityUI);
    public static readonly Translation XPToastFlagDefenseTick = new Translation("DEFENSE", TranslationFlags.UnityUI);
    public static readonly Translation XPToastCacheDestroyed = new Translation("CACHE DESTROYED", TranslationFlags.UnityUI);
    public static readonly Translation XPToastFriendlyCacheDestroyed = new Translation("FRIENDLY CACHE DESTROYED", TranslationFlags.UnityUI);

    public static readonly Translation XPToastSquadBonus = new Translation("SQUAD BONUS", TranslationFlags.UnityUI);
    public static readonly Translation XPToastOnDuty = new Translation("ON DUTY", TranslationFlags.UnityUI);

    public static readonly Translation<int> XPToastGainXP = new Translation<int>("+{0} XP", TranslationFlags.UnityUI);
    public static readonly Translation<int> XPToastLoseXP = new Translation<int>("-{0} XP", TranslationFlags.UnityUI);
    public static readonly Translation<int> XPToastGainCredits = new Translation<int>("+{0} <color=#c$credits$>C</color>", TranslationFlags.UnityUI);
    public static readonly Translation<int> XPToastPurchaseCredits = new Translation<int>("-{0} <color=#c$credits$>C</color>", TranslationFlags.UnityUI);
    public static readonly Translation<int> XPToastLoseCredits = new Translation<int>("-{0} <color=#d69898>C</color>", TranslationFlags.UnityUI);
    public static readonly Translation ToastPromoted = new Translation("YOU HAVE BEEN <color=#ffbd8a>PROMOTED</color> TO", TranslationFlags.UnityUI);
    public static readonly Translation ToastDemoted = new Translation("YOU HAVE BEEN <color=#e86868>DEMOTED</color> TO", TranslationFlags.UnityUI);
    #endregion

    #region Injured UI
    public static readonly Translation InjuredUIHeader = new Translation("You are injured", TranslationFlags.UnityUI);
    public static readonly Translation InjuredUIGiveUp = new Translation("Press <color=#cecece><b><plugin_2/></b></color> to give up.", TranslationFlags.UnityUI);
    public static readonly Translation InjuredUIGiveUpChat = new Translation("<#ff8c69>You were injured, press <color=#cedcde><plugin_2/></color> to give up.");
    #endregion

    #region Insurgency
    public static readonly Translation InsurgencyListHeader = new Translation("Caches", TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyUnknownCacheAttack = new Translation("<color=#696969>Undiscovered</color>", TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyUnknownCacheDefense = new Translation("<color=#696969>Unknown</color>", TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyDestroyedCacheAttack = new Translation("<color=#5a6e5c>Destroyed</color>", TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyDestroyedCacheDefense = new Translation("<color=#6b5858>Lost</color>", TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyCacheAttack = new Translation("<color=#ffca61>{0}</color> <color=#c2c2c2>{1}</color>", TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyCacheDefense = new Translation("<color=#555bcf>{0}</color> <color=#c2c2c2>{1}</color>", TranslationFlags.UnityUI);
    public static readonly Translation InsurgencyCacheDefenseUndiscovered = new Translation("<color=#b780d9>{0}</color> <color=#c2c2c2>{1}</color>", TranslationFlags.UnityUI);
    #endregion

    #region Report Command
    public static readonly Translation ReportReasons = new Translation("<#9cffb3>Report reasons: -none-, \"chat abuse\", \"voice chat abuse\", \"soloing vehicles\", \"wasteing assets\", \"teamkilling\", \"fob greifing\", \"cheating\".");
    public static readonly Translation ReportDiscordNotLinked = new Translation("<#9cffb3>Your account must be linked in our Discord server to use this command. Type <#7483c4>/discord</color> then type <#fff>-link {0}</color> in <#c480d9>#warfare-stats</color>.");
    public static readonly Translation ReportPlayerNotFound = new Translation("<#9cffb3>Unable to find a player with that name, you can use their <color=#ffffff>Steam64 ID</color> instead, as names are only stored until they've been offline for 20 minutes.");
    public static readonly Translation ReportUnknownError = new Translation("<#9cffb3>Unable to generate a report for an unknown reason, check your syntax again with <color=#ffffff>/report help</color>.");
    public static readonly Translation<IPlayer, string, EReportType> ReportSuccessMessage1 = new Translation<IPlayer, string, EReportType>("<#c480d9>Successfully reported {0} for <#fff>{1}</color> as a <#00ffff>{2}</color> report.", UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation ReportSuccessMessage2 = new Translation("<#c480d9>If possible please post evidence in <#ffffff>#player-reports</color> in our <#7483c4>Discord</color> server.");
    public static readonly Translation<IPlayer, IPlayer, string, EReportType> ReportNotifyAdmin = new Translation<IPlayer, IPlayer, string, EReportType>("<#c480d9>{0} reported {1} for <#fff>{2}</color> as a <#00ffff>{3}</color> report.\nCheck <#c480d9>#player-reports</color> for more information.", TranslationFlags.UnityUI, UCPlayer.CHARACTER_NAME_FORMAT, UCPlayer.CHARACTER_NAME_FORMAT);
    public static readonly Translation<string> ReportNotifyViolatorToast = new Translation<string>("<#c480d9>You've been reported for <#00ffff>{0}</color>.\nCheck <#fff>#player-reports</color> in our <#7483c4>Discord</color> (/discord) for more information and to defend yourself.", TranslationFlags.UnityUI);
    public static readonly Translation<EReportType, string> ReportNotifyViolatorMessage1 = new Translation<EReportType, string>("<#c480d9>You've been reported for <#00ffff>{0} - {1}</color>.");
    public static readonly Translation ReportNotifyViolatorMessage2 = new Translation("<#c480d9>Check <#fff>#player-reports</color> in our <#7483c4>Discord</color> (/discord) for more information and to defend yourself.");
    public static readonly Translation<IPlayer> ReportCooldown = new Translation<IPlayer>("<#9cffb3>You've already reported {0} in the past hour.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<ulong, IPlayer> ReportConfirm = new Translation<ulong, IPlayer>("<#c480d9>Did you mean to report {1} <i><#444>{0}</color></i>? Type <#ff8c69>/confirm</color> to continue.", arg1Fmt: UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation ReportNotConnected = new Translation("<#ff8c69>The report system is not available right now, please try again later.");
    #endregion

    #region Abandon
    private const string ABANDON_SECTION = "Abandon";
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player isn't looking at a vehicle when doing /abandon.", LegacyTranslationId = "abandon_no_target")]
    public static readonly Translation AbandonNoTarget = new Translation(ERROR_COLOR + "You must be looking at a vehicle.");
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player is looking at a vehicle they didn't request.", LegacyTranslationId = "abandon_not_owned")]
    public static readonly Translation<InteractableVehicle> AbandonNotOwned = new Translation<InteractableVehicle>(ERROR_COLOR + "You did not request that {0}.");
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player does /abandon while not in main.", LegacyTranslationId = "abandon_not_in_main")]
    public static readonly Translation AbandonNotInMain = new Translation(ERROR_COLOR + "You must be in main to abandon a vehicle.");
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player tries to abandon a damaged vehicle.", LegacyTranslationId = "abandon_damaged")]
    public static readonly Translation<InteractableVehicle> AbandonDamaged = new Translation<InteractableVehicle>(ERROR_COLOR + "Your <#cedcde>{0}</color> is damaged, repair it before returning it to the yard.");
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player tries to abandon a vehicle with low fuel.", LegacyTranslationId = "abandon_needs_fuel")]
    public static readonly Translation<InteractableVehicle> AbandonNeedsFuel = new Translation<InteractableVehicle>(ERROR_COLOR + "Your <#cedcde>{0}</color> is not fully fueled, .");
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player tries to abandon a vehicle and all the bays for that vehicle are already full, theoretically should never happen.", LegacyTranslationId = "abandon_no_space")]
    public static readonly Translation<InteractableVehicle> AbandonNoSpace = new Translation<InteractableVehicle>(ERROR_COLOR + "There's no space for <#cedcde>{0}</color> in the yard.", PLURAL);
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player tries to abandon a vehicle that isn't allowed to be abandoned.", LegacyTranslationId = "abandon_not_allowed")]
    public static readonly Translation<InteractableVehicle> AbandonNotAllowed = new Translation<InteractableVehicle>(ERROR_COLOR + "<#cedcde>{0}</color> can not be abandoned.", PLURAL);
    [TranslationData(Section = ABANDON_SECTION, Description = "Sent when a player abandons a vehicle.", LegacyTranslationId = "abandon_success")]
    public static readonly Translation<InteractableVehicle> AbandonSuccess = new Translation<InteractableVehicle>("<#a0ad8e>Your <#cedcde>{0}</color> was returned to the yard.", PLURAL);
    [TranslationData(Section = ABANDON_SECTION, Description = "Credits toast for returning a vehicle soon after requesting it.", LegacyTranslationId = "abandon_compensation_toast")]
    public static readonly Translation AbandonCompensationToast = new Translation("RETURNED VEHICLE", TranslationFlags.UnityUI);
    #endregion
    
    #region DailyQuests
    private const string DAILY_QUEST_SECTION = "Daily Quests";
    [TranslationData(Section = DAILY_QUEST_SECTION, Description = "Sent when new daily quests are put into action.")]
    public static readonly Translation<DateTime> DailyQuestsNewIndex = new Translation<DateTime>("<#66ccff>New daily quests have been generated! They will be active until <#cedcde>{0}</color> UTC.", "G");
    [TranslationData(Section = DAILY_QUEST_SECTION, Description = "Sent 1 hour before new daily quests are put into action.")]
    public static readonly Translation DailyQuestsOneHourRemaining = new Translation("<#66ccff>You have one hour until new daily quests will be generated!");
    #endregion

    #region Tips
    public static readonly Translation TipPlaceRadio = new Translation("Place a <#ababab>FOB RADIO</color>.", TranslationFlags.UnityUI);
    public static readonly Translation TipPlaceBunker = new Translation("Build a <#a5c3d9>FOB BUNKER</color> so that your team can spawn.", TranslationFlags.UnityUI);
    public static readonly Translation TipUnloadSupplies = new Translation("<#d9c69a>DROP SUPPLIES</color> onto the FOB.", TranslationFlags.UnityUI);
    public static readonly Translation<IPlayer> TipHelpBuild = new Translation<IPlayer>("<#d9c69a>{0} needs help building!", TranslationFlags.UnityUI, UCPlayer.COLOR_NICK_NAME_FORMAT);
    public static readonly Translation<EVehicleType> TipLogisticsVehicleResupplied = new Translation<EVehicleType>("Your <#009933>{0}</color> has been auto resupplied.", TranslationFlags.UnityUI, UPPERCASE);
    #endregion

    #region Zone Command
    public static readonly Translation ZoneNoResultsLocation = new Translation("<#ff8c69>You aren't in any existing zone.");
    public static readonly Translation ZoneNoResultsName = new Translation("<#ff8c69>Couldn't find a zone by that name.");
    public static readonly Translation ZoneNoResults = new Translation("<#ff8c69>You must be in a zone or specify a valid zone name to use this command.");
    public static readonly Translation<Zone> ZoneGoSuccess = new Translation<Zone>("<#e6e3d5>Teleported to <#5a6e5c>{0}</color>.", Flag.NAME_FORMAT);
    public static readonly Translation<int, Zone> ZoneVisualizeSuccess = new Translation<int, Zone>("<#e6e3d5>Spawned {0} particles around <color=#cedcde>{1}</color>.", arg1Fmt: Flag.NAME_FORMAT);

    // Zone > Delete
    public static readonly Translation ZoneDeleteZoneNotInZone = new Translation("<#ff8c69>You must be standing in 1 zone (not 0 or multiple). Alternatively, provide a zone name as another argument.");
    public static readonly Translation<string> ZoneDeleteZoneNotFound = new Translation<string>("<#ff8c69>Failed to find a zone named \"{0}\".");
    public static readonly Translation<Zone> ZoneDeleteZoneConfirm = new Translation<Zone>("Did you mean to delete <#666>{0}</color>? Type <#ff8c69>/confirm</color> to continue.", Flag.NAME_FORMAT);
    public static readonly Translation<Zone> ZoneDeleteZoneSuccess = new Translation<Zone>("<#e6e3d5>Deleted <#666>{0}</color>.", Flag.NAME_FORMAT);
    public static readonly Translation ZoneDeleteEditingZoneDeleted = new Translation("<#ff8c69>Someone deleted the zone you're working on, saving this will create a new one.");

    // Zone > Create
    public static readonly Translation<Zone, EZoneType> ZoneCreated = new Translation<Zone, EZoneType>("<#e6e3d5>Started zone builder for {0}, a {1} zone.", Flag.NAME_FORMAT);
    public static readonly Translation<string> ZoneCreateNameTaken = new Translation<string>("<#ff8c69>The name \"{0}\" is already in use by another zone.");
    public static readonly Translation<string, IPlayer> ZoneCreateNameTakenEditing = new Translation<string, IPlayer>("<#ff8c69>The name \"{0}\" is already in use by another zone being created by {1}.");
    
    // Zone > Edit
    public static readonly Translation<int> ZoneEditPointNotDefined = new Translation<int>("<#ff8c69>Point <#ff9999>#{0}</color> is not defined.");
    public static readonly Translation<int> ZoneEditPointNotNearby = new Translation<int>("<#ff8c69>There is no point near <#ff9999>{0}</color>.");

    // Zone > Edit > Existing
    public static readonly Translation ZoneEditExistingInvalid = new Translation("<#ff8c69>Edit existing zone requires the zone name as a parameter. Alternatively stand in the zone (without overlapping another).");
    public static readonly Translation ZoneEditExistingInProgress = new Translation("<#ff8c69>Cancel or finalize the zone you're currently editing first.");
    public static readonly Translation<Zone, EZoneType> ZoneEditExistingSuccess = new Translation<Zone, EZoneType>("<#e6e3d5>Started editing zone <#fff>{0}</color>, a <#ff9999>{1}</color> zone.");

    // Zone > Edit > Finalize
    public static readonly Translation ZoneEditNotStarted = new Translation("<#ff8c69>Start creating a zone with <#fff>/zone create <polygon|rectangle|circle> <name></color>.");
    public static readonly Translation ZoneEditFinalizeExists = new Translation("<#ff8c69>There's already a zone saved with that id.");
    public static readonly Translation<string> ZoneEditFinalizeSuccess = new Translation<string>("<#e6e3d5>Successfully finalized and saved {0}.");
    public static readonly Translation<string> ZoneEditFinalizeFailure = new Translation<string>("<#ff8c69>The provided zone data was invalid because: <#fff>{0}</color>.");
    public static readonly Translation ZoneEditFinalizeUseCaseUnset = new Translation("<#ff8c69>Before saving you must set a use case with /zone edit use case <type>: \"flag\", \"lobby\", \"t1_main\", \"t2_main\", \"t1_amc\", or \"t2_amc\".");
    public static readonly Translation<Zone> ZoneEditFinalizeOverwrote = new Translation<Zone>("<#e6e3d5>Successfully overwrote <#fff>{0}</color>.", Flag.NAME_FORMAT);

    // Zone > Edit > Cancel
    public static readonly Translation<string> ZoneEditCancelled = new Translation<string>("<#e6e3d5>Successfully cancelled making <#fff>{0}</color>.");

    // Zone > Edit > Type
    public static readonly Translation ZoneEditTypeInvlaid = new Translation("<#ff8c69>Type must be rectangle, circle, or polygon.");
    public static readonly Translation<EZoneType> ZoneEditTypeAlreadySet = new Translation<EZoneType>("<#ff8c69>This zone is already a <#ff9999>{0}</color>.");
    public static readonly Translation<EZoneType> ZoneEditTypeSuccess = new Translation<EZoneType>("<#ff8c69>Set type to <#ff9999>{0}</color>.");

    // Zone > Edit > Max-Height
    public static readonly Translation ZoneEditMaxHeightInvalid = new Translation("<#ff8c69>Maximum Height must be a decimal or whole number, or leave it blank to use the player's current height.");
    public static readonly Translation<float> ZoneEditMaxHeightSuccess = new Translation<float>("<#e6e3d5>Set maximum height to <#ff9999>{0}</color>.", "0.##");

    // Zone > Edit > Min-Height
    public static readonly Translation ZoneEditMinHeightInvalid = new Translation("<#ff8c69>Minimum Height must be a decimal or whole number, or leave it blank to use the player's current height.");
    public static readonly Translation<float> ZoneEditMinHeightSuccess = new Translation<float>("<#e6e3d5>Set minimum height to <#ff9999>{0}</color>.", "0.##");

    // Zone > Edit > Add-Point
    public static readonly Translation ZoneEditAddPointInvalid = new Translation("<#ff8c69>Adding a point requires either: blank (appends, current pos), <index> (current pos), <x> <z> (appends), or <index> <x> <z> parameters.");
    public static readonly Translation<int, Vector2> ZoneEditAddPointSuccess = new Translation<int, Vector2>("<#e6e3d5>Added point <#ff9999>#{0}</color> at <#ff9999>{1}</color>.", arg1Fmt: "0.##");

    // Zone > Edit > Delete-Point
    public static readonly Translation ZoneEditDeletePointInvalid = new Translation("<#ff8c69>Deleting a point requires either: nearby X and Z parameters, a point number, or leave them blank to use the player's current position");
    public static readonly Translation<int, Vector2> ZoneEditDeletePointSuccess = new Translation<int, Vector2>("<#e6e3d5>Removed point <#ff9999>#{0}</color> at <#ff9999>{1}</color>.", arg1Fmt: "0.##");

    // Zone > Edit > Set-Point
    public static readonly Translation ZoneEditSetPointInvalid = new Translation("<#ff8c69>Moving a point requires either: blank (move nearby closer), <nearby src x> <nearby src z> <dest x> <dest z>, <pt num> (destination is player position), <pt num> <dest x> <dest z>, or <nearby src x> <nearby src z> (destination is nearby player).");
    public static readonly Translation<int, Vector2> ZoneEditSetPointSuccess = new Translation<int, Vector2>("<#e6e3d5>Removed point <#ff9999>#{0}</color> at <#ff9999>{1}</color>.", arg1Fmt: "0.##");

    // Zone > Edit > Order-Point
    public static readonly Translation ZoneEditOrderPointInvalid = new Translation("<#ff8c69>Ordering a point requires either: <from-index> <to-index>, <to-index> (from is nearby player), or <src x> <src z> <to-index>.");
    public static readonly Translation<int, int> ZoneEditOrderPointSuccess = new Translation<int, int>("<#e6e3d5>Moved point <#ff9999>#{0}</color> to index <#ff9999>#{1}</color>.");

    // Zone > Edit > Clear-Points
    [TranslationData(FormattingDescriptions = new string[] { "Amount of points restored.", "\"s\" unless {0} == 1." })]
    public static readonly Translation<int, string> ZoneEditUnclearedSuccess = new Translation<int, string>("<#e6e3d5>Restored {0} point{1}.");
    public static readonly Translation ZoneEditClearSuccess = new Translation("<#e6e3d5>Cleared all polygon points.");

    // Zone > Edit > Radius
    public static readonly Translation ZoneEditRadiusInvalid = new Translation("<#ff8c69>Radius must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.");
    public static readonly Translation<float> ZoneEditRadiusSuccess = new Translation<float>("<#e6e3d5>Set radius to <#ff9999>{0}</color>.", "0.##");

    // Zone > Edit > Size-X
    public static readonly Translation ZoneEditSizeXInvalid = new Translation("<#ff8c69>Size X must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.");
    public static readonly Translation<float> ZoneEditSizeXSuccess = new Translation<float>("<#e6e3d5>Set size x to <#ff9999>{0}</color>.", "0.##");

    // Zone > Edit > Size-Z
    public static readonly Translation ZoneEditSizeZInvalid = new Translation("<#ff8c69>Size Z must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.");
    public static readonly Translation<float> ZoneEditSizeZSuccess = new Translation<float>("<#e6e3d5>Set size z to <#ff9999>{0}</color>.", "0.##");

    // Zone > Edit > Center
    public static readonly Translation ZoneEditCenterInvalid = new Translation("<#ff8c69>To set center you must provide two decimal or whole numbers, or leave them blank to use the player's current position.");
    public static readonly Translation<Vector2> ZoneEditCenterSuccess = new Translation<Vector2>("<#e6e3d5>Set center position to <#ff9999>{0}</color>.", "0.##");

    // Zone > Edit > Name
    public static readonly Translation ZoneEditNameInvalid = new Translation("<#ff8c69>Name requires one string argument. Quotation marks aren't required.");
    public static readonly Translation<string> ZoneEditNameSuccess = new Translation<string>("<#e6e3d5>Set name to \"<#ff9999>{0}</color>\".");

    // Zone > Edit > Short-Name
    public static readonly Translation ZoneEditShortNameInvalid = new Translation("<#ff8c69>Short name requires one string argument. Quotation marks aren't required.");
    public static readonly Translation<string> ZoneEditShortNameSuccess = new Translation<string>("<#e6e3d5>Set short name to \"<#ff9999>{0}</color>\".");
    public static readonly Translation ZoneEditShortNameRemoved = new Translation("<#e6e3d5>Removed short name.");

    // Zone > Edit > Use-Case
    public static readonly Translation ZoneEditUseCaseInvalid = new Translation("<#ff8c69>Use case requires one string argument: \"flag\", \"lobby\", \"t1_main\", \"t2_main\", \"t1_amc\", or \"t2_amc\".");
    public static readonly Translation<EZoneUseCase> ZoneEditUseCaseSuccess = new Translation<EZoneUseCase>("<#e6e3d5>Set use case to \"<#ff9999>{0}</color>\".");

    // Zone > Edit > Transactions
    public static readonly Translation ZoneEditUndoEmpty = new Translation("<#ff8c69>There is nothing to undo.");
    public static readonly Translation ZoneEditRedoEmpty = new Translation("<#ff8c69>There is nothing to redo.");

    // Zone > Edit > UI
    public static readonly Translation ZoneEditUIYLimits = new Translation("Y: {0} - {1}", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditUIYLimitsInfinity = new Translation("∞", TranslationFlags.UnityUI);

    // Zone > Edit > UI > Suggestions
    public static readonly Translation ZoneEditSuggestedCommandsHeader = new Translation("Suggested Commands", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand1  = new Translation("/ze maxheight [value]", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand2  = new Translation("/ze minheight [value]", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand3  = new Translation("/ze finalize", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand4  = new Translation("/ze cancel", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand5  = new Translation("/ze addpt [x z]", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand6  = new Translation("/ze delpt [number | x z]", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand7  = new Translation("/ze setpt <number | src: x z | number dest: x z | src: x z dest: x z>", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand8  = new Translation("/ze orderpt <from-index to-index | to-index | src: x z to-index>", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand9  = new Translation("/ze radius [value]", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand10 = new Translation("/ze sizex [value]", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand11 = new Translation("/ze sizez [value]", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand12 = new Translation("/zone util location", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand13 = new Translation("/ze type <rectangle | circle | polygon>", TranslationFlags.UnityUI);
    public static readonly Translation ZoneEditSuggestedCommand14 = new Translation("/ze clearpoints", TranslationFlags.UnityUI);

    // Zone > Util > Location
    [TranslationData(FormattingDescriptions = new string[] { "X m", "Y m", "Z m", "Yaw °" })]
    public static readonly Translation<float, float, float, float> ZoneUtilLocation = new Translation<float, float, float, float>("<#e6e3d5>Location: {0}, {1}, {2} | Yaw: {3}°.", "0.##", "0.##", "0.##", "0.##");
    #endregion

    #region Teams
    public static readonly Translation<Cooldown> TeamsCooldown = new Translation<Cooldown>("<#ff8c69>You can't use /teams for another {0}.", Cooldown.LONG_TIME_FORMAT);
    public static readonly Translation TeamsUIHeader = new Translation("Choose a Team", TranslationFlags.UnityUI);
    public static readonly Translation TeamsUIClickToJoin = new Translation("CLICK TO JOIN", TranslationFlags.UnityUI);
    public static readonly Translation TeamsUIClickToJoinDonor = new Translation("<#e3b552>CLICK TO JOIN", TranslationFlags.UnityUI);
    public static readonly Translation TeamsUIJoined = new Translation("JOINED", TranslationFlags.UnityUI);
    public static readonly Translation TeamsUIJoinedDonor = new Translation("<#e3b552>JOINED", TranslationFlags.UnityUI);
    public static readonly Translation TeamsUIFull = new Translation("<#bf6363>FULL", TranslationFlags.UnityUI);
    public static readonly Translation TeamsUIConfirm = new Translation("<#888888>CONFIRM", TranslationFlags.UnityUI);
    public static readonly Translation TeamsUIJoining = new Translation("<#999999>JOINING...", TranslationFlags.UnityUI);
    #endregion

    #region Spotting
    public static readonly Translation SpottedToast = new Translation("<#b9ffaa>SPOTTED", TranslationFlags.UnityUI);
    #endregion

    #region Teleport
    public static readonly Translation<IPlayer> TeleportTargetDead = new Translation<IPlayer>("<#8f9494>{0} is not alive.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer, InteractableVehicle> TeleportSelfSuccessVehicle = new Translation<IPlayer, InteractableVehicle>("<#bfb9ac>You were put in {0}'s {1}.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, RARITY_COLOR_FORMAT);
    public static readonly Translation<IPlayer> TeleportSelfSuccessPlayer = new Translation<IPlayer>("<#bfb9ac>You were teleported to {0}.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer> TeleportSelfPlayerObstructed = new Translation<IPlayer>("<#8f9494>Failed to teleport you to {0}, their position is obstructed.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<string> TeleportLocationNotFound = new Translation<string>("<#8f9494>Failed to find a location similar to <#ddd>{0}</color>.");
    public static readonly Translation<string> TeleportSelfLocationSuccess = new Translation<string>("<#bfb9ac>You were teleported to <#ddd>{0}</color>.");
    public static readonly Translation<string> TeleportSelfLocationObstructed = new Translation<string>("<#8f9494>Failed to teleport you to <#ddd>{0}</color>, it's position is obstructed.");
    public static readonly Translation<IPlayer, IPlayer, InteractableVehicle> TeleportOtherSuccessVehicle = new Translation<IPlayer, IPlayer, InteractableVehicle>("<#bfb9ac>{0} was put in {1}'s {2}.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, UCPlayer.COLOR_CHARACTER_NAME_FORMAT, RARITY_COLOR_FORMAT);
    public static readonly Translation<IPlayer, IPlayer> TeleportOtherSuccessPlayer = new Translation<IPlayer, IPlayer>("<#bfb9ac>{0} was teleported to {1}.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer, IPlayer> TeleportOtherObstructedPlayer = new Translation<IPlayer, IPlayer>("<#8f9494>Failed to teleport {0} to {1}, their position is obstructed.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT, UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer, string> TeleportOtherSuccessLocation = new Translation<IPlayer, string>("<#bfb9ac>{0} was teleported to <#ddd>{1}</color>.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<IPlayer, string> TeleportOtherObstructedLocation = new Translation<IPlayer, string>("<#8f9494>Failed to teleport {0} to <#ddd>{1}</color>, it's position is obstructed.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation<string> TeleportTargetNotFound = new Translation<string>("<#8f9494>Failed to find a player from <#ddd>{0}</color>.");
    public static readonly Translation TeleportInvalidCoordinates = new Translation("<#8f9494>Use of coordinates should look like: <#eee>/tp [player] <x y z></color>.");
    #endregion

    #region Heal Command
    public static readonly Translation<IPlayer> HealPlayer = new Translation<IPlayer>("<#ff9966>You healed {0}.", UCPlayer.COLOR_CHARACTER_NAME_FORMAT);
    public static readonly Translation HealSelf = new Translation("<#ff9966>You we're healed.");
    #endregion

    #region God Command
    public static readonly Translation GodModeEnabled = new Translation("<#bfb9ac>God mode <#99ff66>enabled</color>.");
    public static readonly Translation GodModeDisabled = new Translation("<#ff9966>God mode <#ff9999>disabled</color>.");
    #endregion

    #region Vanish Command
    public static readonly Translation VanishModeEnabled = new Translation("<#bfb9ac>Vanish mode <#99ff66>enabled</color>.");
    public static readonly Translation VanishModeDisabled = new Translation("<#ff9966>Vanish mode <#ff9999>disabled</color>.");
    #endregion

    #region Permission Command
    public static readonly Translation<string> PermissionsCurrent = new Translation<string>("<#bfb9ac>Current permisions: <color=#ffdf91>{0}</color>.");
    public static readonly Translation<EAdminType, IPlayer, ulong> PermissionGrantSuccess = new Translation<EAdminType, IPlayer, ulong>("<#bfb9ac><#7f8182>{1}</color> <#ddd>({2})</color> is now a <#ffdf91>{0}</color>.");
    public static readonly Translation<EAdminType, IPlayer, ulong> PermissionGrantAlready = new Translation<EAdminType, IPlayer, ulong>("<#bfb9ac><#7f8182>{1}</color> <#ddd>({2})</color> is already at the <#ffdf91>{0}</color> level.");
    public static readonly Translation<IPlayer, ulong> PermissionRevokeSuccess = new Translation<IPlayer, ulong>("<#bfb9ac><#7f8182>{0}</color> <#ddd>({1})</color> is now a <#ffdf91>member</color>.");
    public static readonly Translation<IPlayer, ulong> PermissionRevokeAlready = new Translation<IPlayer, ulong>("<#bfb9ac><#7f8182>{0}</color> <#ddd>({1})</color> is already a <#ffdf91>member</color>.");
    #endregion

    #region Win UI
    public static readonly Translation<int> WinUIValueTickets = new Translation<int>("{0} Tickets", TranslationFlags.UnityUI);
    public static readonly Translation<int> WinUIValueCaches = new Translation<int>("{0} Caches Left", TranslationFlags.UnityUI);
    public static readonly Translation<int> WinUIHeaderWinner = new Translation<int>("{0}\r\nhas won the battle!", TranslationFlags.UnityUI);
    #endregion
    
    private const string ERROR_COLOR = "<#ff8c69>";
    private const string SUCCESS_COLOR = "<#e6e3d5>";
    internal const string PLURAL = "$plural$";
    internal const string UPPERCASE = "upper";
    internal const string LOWERCASE = "lower";
    internal const string PROPERCASE = "proper";
    internal const string RARITY_COLOR_FORMAT = "rarity";
    public static readonly Translation[] Translations;
    public static readonly Dictionary<string, Translation> Signs;
    static T()
    {
        FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(x => typeof(Translation).IsAssignableFrom(x.FieldType)).ToArray();
        Translations = new Translation[fields.Length];
        int i2 = -1;
        int signCt = 0;
        for (int i = 0; i < fields.Length; ++i)
        {
            FieldInfo field = fields[i];
            if (typeof(Translation).IsAssignableFrom(field.FieldType))
            {
                if (field.GetValue(null) is not Translation tr)
                    L.LogError("Failed to convert " + field.Name + " to a translation!");
                else if (i2 + 1 < Translations.Length)
                {
                    tr.Key = field.Name;
                    tr.Id = i2;
                    tr.AttributeData = Attribute.GetCustomAttribute(field, typeof(TranslationDataAttribute)) as TranslationDataAttribute;
                    tr.Init();
                    if (tr.AttributeData is not null && !string.IsNullOrEmpty(tr.AttributeData.SignId))
                        ++signCt;
                    Translations[++i2] = tr;
                }
                else
                    L.LogError("Ran out of space in translation array for " + field.Name + " at " + (i2 + 1), method: "TRANSLATIONS");
            }
        }

        if (Translations.Length != i2 + 1)
        {
            L.LogWarning("Translations had to resize for some reason from " + Translations.Length + " to " + (i2 + 1) + ". Check to make sure there's only one field that isn't a translation.",
                method: "TRANSLATIONS");
            Array.Resize(ref Translations, i2 + 1);
        }
        Signs = new Dictionary<string, Translation>(signCt);
        for (int i = 0; i < Translations.Length; ++i)
        {
            Translation tr = Translations[i];
            if (tr.AttributeData is not null && !string.IsNullOrEmpty(tr.AttributeData.SignId))
            {
                if (Signs.ContainsKey(tr.AttributeData.SignId!))
                    L.LogWarning("Duplicate Sign ID: \"" + tr.AttributeData.SignId + "\" in translation \"" + tr.Key + "\".", method: "TRANSLATIONS");
                else
                    Signs.Add(tr.AttributeData.SignId!, tr);
            }
        }
    }
}
