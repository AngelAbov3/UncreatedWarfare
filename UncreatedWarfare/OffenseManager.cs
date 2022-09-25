﻿using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Networking.Async;
using Uncreated.Players;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Commands.Permissions;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using UnityEngine;
using SteamGameServerNetworkingUtils = SDG.Unturned.SteamGameServerNetworkingUtils;

namespace Uncreated.Warfare;

public static class OffenseManager
{
    // calls the type initializer
    internal static void Init()
    {
        EventDispatcher.OnPlayerDied += OnPlayerDied;
        EventDispatcher.OnPlayerJoined += OnPlayerJoined;
        EventDispatcher.OnPlayerPending += OnPlayerPending;
        Load<Ban>(0);
        Load<Unban>(1);
        Load<Kick>(2);
        Load<Warn>(3);
        Load<Mute>(4);
        Load<BattlEyeKick>(5);
        Load<Teamkill>(6);
        Load<VehicleTeamkill>(7);
        Load<Unmute>(8);
    }
    internal static void Deinit()
    {
        EventDispatcher.OnPlayerPending -= OnPlayerPending;
        EventDispatcher.OnPlayerJoined -= OnPlayerJoined;
        EventDispatcher.OnPlayerDied -= OnPlayerDied;
        for (int i = 0; i < _pendings.Length; ++i)
        {
            IList l = _pendings[i];
            lock (l)
                l.Clear();
        }
    }
    private static void OnPlayerPending(PlayerPending e)
    {
        List<uint> packs = new List<uint>(4);
        Data.DatabaseManager.Query("SELECT `Packed` FROM `ip_addresses` WHERE `Steam64` = @0;", new object[] { e.Steam64 }, R =>
        {
            packs.Add(R.GetUInt32(0));
        });
        for (int i = 0; i < SteamBlacklist.list.Count; ++i)
        {
            uint ip = SteamBlacklist.list[i].ip;
            for (int j = 0; j < packs.Count; ++j)
            {
                if (ip != 0 && ip == packs[j])
                {
                    e.Reject("Your IP is banned for: " + SteamBlacklist.list[i].reason);
                    return;
                }
            }
        }
    }
    private static void OnPlayerJoined(PlayerJoined e)
    {
        byte[][] bytes = (e.Player.SteamPlayer.playerID.GetHwids() as byte[][])!;
        int[] update = new int[bytes.Length];
        int updates = 0;
        ulong s64 = e.Steam64;
        uint packed = e.Player.SteamPlayer.transportConnection.TryGetIPv4Address(out uint ip) ? ip : 0u;
        Task.Run(async () =>
        {
            try
            {
                await Data.DatabaseManager.QueryAsync("SELECT `Id`, `Index`, `HWID` FROM `hwids` WHERE `Steam64` = @0 ORDER BY `LastLogin` DESC;",
                    new object[] { s64 },
                    R =>
                    {
                        int id = R.GetInt32(0);
                        byte[] buffer = new byte[20];
                        R.GetBytes(2, 0, buffer, 0, 20);
                        for (int i = 0; i < update.Length; ++i)
                        {
                            if (update[i] > 0) continue;
                            byte[] b = bytes[i];
                            if (b is null) continue;
                            for (int x = 0; x < 20; ++x)
                                if (b[x] != buffer[x])
                                    goto cont;
                            update[i] = id;
                            ++updates;
                            break;
                        cont:;
                        }
                    });

                uint? id = null;
                await Data.DatabaseManager.QueryAsync("SELECT `Id` FROM `ip_addresses` WHERE `Steam64` = @0 AND `Packed` = @1 LIMIT 1;", new object[] { s64, packed },
                    R => id = R.GetUInt32(0));
                StringBuilder sbq = new StringBuilder(64);
                StringBuilder sbq2 = new StringBuilder(64);
                int c = 1;
                for (int i = 0; i < update.Length; ++i)
                {
                    if (update[i] < 1 && bytes[i] is not null && bytes[i].Length == 20 && i < 8)
                        c += 2;
                }
                object[] objs = new object[c];
                objs[0] = s64;
                c = 0;
                int d = 0;
                object[] o2 = new object[update.Length];
                for (int i = 0; i < update.Length; ++i)
                {
                    if (update[i] > 0)
                    {
                        sbq2.Append("UPDATE `hwids` SET `LoginCount` = `LoginCount` + 1, `LastLogin` = NOW() WHERE `Id` = @" + i + ";");
                        ++d;
                        o2[i] = update[i];
                        continue;
                    }
                    else if (bytes[i] is not null && bytes[i].Length == 20 && i < 8)
                    {
                        if (c != 0)
                            sbq.Append(',');
                        sbq.Append('(').Append("@0, @").Append(c * 2 + 1).Append(", @").Append(c * 2 + 2).Append(')');
                        objs[c * 2 + 1] = i;
                        objs[c * 2 + 2] = bytes[i];
                        ++c;
                    }

                    o2[i] = 0;
                }
                if (c > 0)
                {
                    sbq.Insert(0, "INSERT INTO `hwids` (`Steam64`, `Index`, `HWID`) VALUES ");
                    sbq.Append(';');
                    string query = sbq.ToString();

                    await Data.DatabaseManager.NonQueryAsync(query, objs);
                }
                if (d > 0)
                {
                    await Data.DatabaseManager.NonQueryAsync(sbq2.ToString(), o2);
                }
                if (id.HasValue)
                    await Data.DatabaseManager.NonQueryAsync(
                        "UPDATE `ip_addresses` SET `LoginCount` = `LoginCount` + 1, `LastLogin` = NOW() WHERE `Id` = @0 LIMIT 1;",
                        new object[] { id.Value });
                else
                    await Data.DatabaseManager.NonQueryAsync(
                        "INSERT INTO `ip_addresses` (`Steam64`, `Packed`, `Unpacked`) VALUES (@0, @1, @2);",
                        new object[] { s64, packed, Parser.getIPFromUInt32(packed) });
            }
            catch (Exception ex)
            {
                L.LogError("Error handling player connect: ");
                L.LogError(ex);
                await UCWarfare.ToUpdate();
                Provider.kick(e.Player.CSteamID, "There was a fatal error connecting you to the server.");
            }
        }).ConfigureAwait(false);
    }
    public static async Task<List<byte[]>> GetAllHWIDs(ulong s64)
    {
        List<byte[]> bytes = new List<byte[]>(8);
        await Data.DatabaseManager.QueryAsync("SELECT `Id`, `Index`, `HWID` FROM `hwids` WHERE `Steam64` = @0 ORDER BY `Index` ASC, `LastLogin` DESC;",
            new object[] { s64 },
            R =>
            {
                int id = R.GetInt32(0);
                byte[] buffer = new byte[20];
                R.GetBytes(2, 0, buffer, 0, 20);
                bytes.Add(buffer);
            });
        return bytes;
    }
    public static async Task ApplyMuteSettings(UCPlayer joining)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (joining == null) return;
        string? reason = null;
        int duration = -2;
        DateTime timestamp = DateTime.MinValue;
        EMuteType type = EMuteType.NONE;
        await Data.DatabaseManager.QueryAsync(
            "SELECT `Reason`, `Duration`, `Timestamp`, `Type` FROM `muted` WHERE `Steam64` = @0 AND `Deactivated` = 0 AND " +
            "(`Duration` = -1 OR TIME_TO_SEC(TIMEDIFF(`Timestamp`, NOW())) * -1 < `Duration`) ORDER BY (TIME_TO_SEC(TIMEDIFF(`Timestamp`, NOW())) * -1) - `Duration` LIMIT 1;",
            new object[1] { joining.Steam64 },
            R =>
            {
                int dir = R.GetInt32(1);
                DateTime ts = R.GetDateTime(2);
                if (dir == -1 || dir > duration)
                {
                    duration = dir;
                    timestamp = ts;
                    reason = R.GetString(0);
                }
                else if (reason is null)
                    reason = R.GetString(0);
                type |= (EMuteType)R.GetByte(3);
            }
        );
        if (type == EMuteType.NONE) return;
        DateTime now = DateTime.Now;
        DateTime unmutedTime = duration == -1 ? DateTime.MaxValue : timestamp + TimeSpan.FromSeconds(duration);
        joining.TimeUnmuted = unmutedTime;
        joining.MuteReason = reason;
        joining.MuteType = type;
    }

    public static bool IsValidSteam64ID(ulong id)
    {
        return id / 100000000000000ul == 765;
    }
    public static bool IsValidSteam64ID(CSteamID id)
    {
        return id.m_SteamID / 100000000000000ul == 765;
    }

    private static readonly List<Ban> _pendingBans = new List<Ban>(8);
    private static readonly List<Unban> _pendingUnbans = new List<Unban>(8);
    private static readonly List<Kick> _pendingKicks = new List<Kick>(8);
    private static readonly List<Warn> _pendingWarnings = new List<Warn>(8);
    private static readonly List<Mute> _pendingMutes = new List<Mute>(8);
    private static readonly List<BattlEyeKick> _pendingBattlEyeKicks = new List<BattlEyeKick>(8);
    private static readonly List<Teamkill> _pendingTeamkills = new List<Teamkill>(8);
    private static readonly List<VehicleTeamkill> _pendingVehicleTeamkills = new List<VehicleTeamkill>(8);
    private static readonly List<Unmute> _pendingUnmutes = new List<Unmute>(9);
    private static string GetSavePath(int index) => Path.Combine(Data.Paths.PendingOffenses, index switch
    {
        0 => "bans",
        1 => "unbans",
        2 => "kicks",
        3 => "warnings",
        4 => "mutes",
        5 => "battleye_kicks",
        6 => "teamkills",
        7 => "vehicle_teamkills",
        8 => "unmute",
        _ => throw new ArgumentOutOfRangeException("#" + index.ToString(Data.Locale) + " doesn't match a pending type.")
    } + ".json");

    private static readonly IList[] _pendings = new IList[]
    {
        _pendingBans,
        _pendingUnbans,
        _pendingKicks,
        _pendingWarnings,
        _pendingMutes,
        _pendingBattlEyeKicks,
        _pendingTeamkills,
        _pendingVehicleTeamkills,
        _pendingUnmutes
    };
    private static void Save<T>(int index)
    {
        if (_pendings[index] is List<T> col)
        {
            F.CheckDir(Data.Paths.PendingOffenses, out bool success);
            if (success)
            {
                lock (col)
                {
                    string path = GetSavePath(index);
                    using (FileStream str = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        JsonSerializer.Serialize(str, col, JsonEx.serializerSettings);
                    }
                }
            }
        }
    }
    private static void Load<T>(int index)
    {
        if (_pendings[index] is List<T> col)
        {
            lock (col)
            {
                col.Clear();
                string path = GetSavePath(index);
                if (File.Exists(path))
                {
                    T[]? t;
                    using (FileStream str = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        t = JsonSerializer.Deserialize<T[]>(str, JsonEx.serializerSettings);
                    }
                    if (t is null) return;
                    col.AddRange(t);
                }
            }
        }
    }

    internal static async Task OnConnected()
    {
        ITimestampOffense[] stamps = _pendingBans
            .Cast<ITimestampOffense>()
            .Concat(_pendingUnbans
                .Cast<ITimestampOffense>())
            .Concat(_pendingKicks
                .Cast<ITimestampOffense>())
            .Concat(_pendingWarnings
                .Cast<ITimestampOffense>())
            .Concat(_pendingMutes
                .Cast<ITimestampOffense>())
            .Concat(_pendingBattlEyeKicks
                .Cast<ITimestampOffense>())
            .Concat(_pendingTeamkills
                .Cast<ITimestampOffense>())
            .Concat(_pendingVehicleTeamkills
                .Cast<ITimestampOffense>())
            .Concat(_pendingUnmutes
                .Cast<ITimestampOffense>())
            .OrderBy(x => x.Timestamp)
            .ToArray();

        if (stamps.Length == 0)
        {
            L.Log("No queued past offenses.", ConsoleColor.Magenta);
            return;
        }

        L.Log("Sending past offenses, " + stamps.Length.ToString(Data.Locale) + " queued.", ConsoleColor.Magenta);
        int ct = Math.Max(1, stamps.Length / 10);
        int num = 0;
        bool c = false;
        try
        {
            _pendingBans.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            for (int i = _pendingBans.Count - 1; i >= 0; --i)
            {
                Ban ban = _pendingBans[i];
                await UCWarfare.ToUpdate();
                if (UCWarfare.CanUseNetCall && (await NetCalls.SendPlayerBanned.Request(NetCalls.AckPlayerBanned,
                        UCWarfare.I.NetClient!, ban.Violator, ban.Admin, ban.Reason, ban.Duration, ban.Timestamp,
                        10000)).Responded)
                    _pendingBans.RemoveAt(i);
                else
                    L.LogWarning("  Failed to send ban #" + i.ToString(Data.Locale) + "!");
                c = true;
                ++num;
                if (num % ct == 0)
                    L.Log("  Sending past offenses: " + num.ToString(Data.Locale) + "/" +
                          stamps.Length.ToString(Data.Locale));
            }
        }
        catch (Exception ex)
        {
            L.LogError(ex);
        }

        if (c)
        {
            Save<Ban>(0);
            c = false;
        }


        try
        {
            _pendingUnbans.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            for (int i = _pendingUnbans.Count - 1; i >= 0; --i)
            {
                Unban unban = _pendingUnbans[i];
                await UCWarfare.ToUpdate();
                if (UCWarfare.CanUseNetCall && (await NetCalls.SendPlayerUnbanned.Request(NetCalls.AckPlayerUnbanned,
                        UCWarfare.I.NetClient!, unban.Violator, unban.Admin, unban.Timestamp, 10000)).Responded)
                    _pendingUnbans.RemoveAt(i);
                else
                    L.LogWarning("  Failed to send unban #" + i.ToString(Data.Locale) + "!");
                c = true;
                ++num;
                if (num % ct == 0)
                    L.Log("  Sending past offenses: " + num.ToString(Data.Locale) + "/" +
                          stamps.Length.ToString(Data.Locale));
            }
        }
        catch (Exception ex)
        {
            L.LogError(ex);
        }

        if (c)
        {
            Save<Unban>(1);
            c = false;
        }

        try
        {
            _pendingKicks.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            for (int i = _pendingKicks.Count - 1; i >= 0; --i)
            {
                Kick kick = _pendingKicks[i];
                await UCWarfare.ToUpdate();
                if (UCWarfare.CanUseNetCall && (await NetCalls.SendPlayerKicked.Request(NetCalls.AckPlayerKicked,
                        UCWarfare.I.NetClient!, kick.Violator, kick.Admin, kick.Reason, kick.Timestamp, 10000))
                    .Responded)
                    _pendingKicks.RemoveAt(i);
                else
                    L.LogWarning("  Failed to send kick #" + i.ToString(Data.Locale) + "!");
                c = true;
                ++num;
                if (num % ct == 0)
                    L.Log("  Sending past offenses: " + num.ToString(Data.Locale) + "/" +
                          stamps.Length.ToString(Data.Locale));
            }
        }
        catch (Exception ex)
        {
            L.LogError(ex);
        }

        if (c)
        {
            Save<Kick>(2);
            c = false;
        }

        try
        {
            _pendingWarnings.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            for (int i = _pendingWarnings.Count - 1; i >= 0; --i)
            {
                Warn warn = _pendingWarnings[i];
                await UCWarfare.ToUpdate();
                if (UCWarfare.CanUseNetCall && (await NetCalls.SendPlayerWarned.Request(NetCalls.AckPlayerWarned,
                        UCWarfare.I.NetClient!, warn.Violator, warn.Admin, warn.Reason, warn.Timestamp, 10000))
                    .Responded)
                    _pendingWarnings.RemoveAt(i);
                else
                    L.LogWarning("  Failed to send warn #" + i.ToString(Data.Locale) + "!");
                c = true;
                ++num;
                if (num % ct == 0)
                    L.Log("  Sending past offenses: " + num.ToString(Data.Locale) + "/" +
                          stamps.Length.ToString(Data.Locale));
            }
        }
        catch (Exception ex)
        {
            L.LogError(ex);
        }

        if (c)
        {
            Save<Warn>(3);
            c = false;
        }

        try
        {
            _pendingMutes.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            for (int i = _pendingMutes.Count - 1; i >= 0; --i)
            {
                Mute mute = _pendingMutes[i];
                await UCWarfare.ToUpdate();
                if (UCWarfare.CanUseNetCall && (await NetCalls.SendPlayerMuted.Request(NetCalls.AckPlayerMuted,
                        UCWarfare.I.NetClient!, mute.Violator, mute.Admin, mute.MuteType, mute.Duration, mute.Reason,
                        mute.Timestamp, 10000)).Responded)
                    _pendingMutes.RemoveAt(i);
                else
                    L.LogWarning("  Failed to send mute #" + i.ToString(Data.Locale) + "!");
                c = true;
                ++num;
                if (num % ct == 0)
                    L.Log("  Sending past offenses: " + num.ToString(Data.Locale) + "/" +
                          stamps.Length.ToString(Data.Locale));
            }
        }
        catch (Exception ex)
        {
            L.LogError(ex);
        }

        if (c)
        {
            Save<Mute>(4);
            c = false;
        }

        try
        {
            _pendingBattlEyeKicks.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            for (int i = _pendingBattlEyeKicks.Count - 1; i >= 0; --i)
            {
                BattlEyeKick battlEyeKick = _pendingBattlEyeKicks[i];
                await UCWarfare.ToUpdate();
                if (UCWarfare.CanUseNetCall &&
                    (await NetCalls.SendPlayerBattleyeKicked.Request(NetCalls.AckPlayerBattleyeKicked,
                        UCWarfare.I.NetClient!, battlEyeKick.Violator, battlEyeKick.Reason, battlEyeKick.Timestamp,
                        10000)).Responded)
                    _pendingBattlEyeKicks.RemoveAt(i);
                else
                    L.LogWarning("  Failed to send battleye kick #" + i.ToString(Data.Locale) + "!");
                c = true;
                ++num;
                if (num % ct == 0)
                    L.Log("  Sending past offenses: " + num.ToString(Data.Locale) + "/" +
                          stamps.Length.ToString(Data.Locale));
            }
        }
        catch (Exception ex)
        {
            L.LogError(ex);
        }

        if (c)
        {
            Save<BattlEyeKick>(5);
            c = false;
        }

        try
        {
            _pendingTeamkills.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            for (int i = _pendingTeamkills.Count - 1; i >= 0; --i)
            {
                Teamkill teamkill = _pendingTeamkills[i];
                await UCWarfare.ToUpdate();
                if (UCWarfare.CanUseNetCall && (await NetCalls.SendTeamkill.Request(NetCalls.AckTeamkill,
                        UCWarfare.I.NetClient!, teamkill.Violator, teamkill.Dead, teamkill.DeathCause,
                        teamkill.ItemName, teamkill.Timestamp, 10000)).Responded)
                    _pendingTeamkills.RemoveAt(i);
                else
                    L.LogWarning("  Failed to send teamkill #" + i.ToString(Data.Locale) + "!");
                c = true;
                ++num;
                if (num % ct == 0)
                    L.Log("  Sending past offenses: " + num.ToString(Data.Locale) + "/" +
                          stamps.Length.ToString(Data.Locale));
            }
        }
        catch (Exception ex)
        {
            L.LogError(ex);
        }

        if (c)
        {
            Save<Teamkill>(6);
            c = false;
        }

        try
        {
            _pendingUnmutes.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            for (int i = _pendingUnmutes.Count - 1; i >= 0; --i)
            {
                Unmute unmute = _pendingUnmutes[i];
                await UCWarfare.ToUpdate();
                if (UCWarfare.CanUseNetCall && (await NetCalls.SendUnmuteRequest.Request(NetCalls.AckPlayerUnmuted,
                        UCWarfare.I.NetClient!, unmute.Violator, unmute.Admin, unmute.Timestamp, 10000)).Responded)
                    _pendingUnmutes.RemoveAt(i);
                else
                    L.LogWarning("  Failed to send unmute #" + i.ToString(Data.Locale) + "!");
                c = true;
                ++num;
                if (num % ct == 0)
                    L.Log("  Sending past offenses: " + num.ToString(Data.Locale) + "/" +
                          stamps.Length.ToString(Data.Locale));
            }
        }
        catch (Exception ex)
        {
            L.LogError(ex);
        }

        if (c)
        {
            Save<Unmute>(8);
            c = false;
        }

        try
        {
            _pendingVehicleTeamkills.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            for (int i = _pendingVehicleTeamkills.Count - 1; i >= 0; --i)
            {
                VehicleTeamkill vehicleTeamkill = _pendingVehicleTeamkills[i];
                await UCWarfare.ToUpdate();
                if (UCWarfare.CanUseNetCall && (await NetCalls.SendVehicleTeamkilled.Request(
                        NetCalls.AckVehicleTeamkill, UCWarfare.I.NetClient!, vehicleTeamkill.Violator,
                        vehicleTeamkill.VehicleID, vehicleTeamkill.VehicleName, vehicleTeamkill.Timestamp, 10000))
                    .Responded)
                    _pendingVehicleTeamkills.RemoveAt(i);
                else
                    L.LogWarning("  Failed to send vehicle teamkill #" + i.ToString(Data.Locale) + "!");
                c = true;
                ++num;
                if (num % ct == 0)
                    L.Log("  Sending past offenses: " + num.ToString(Data.Locale) + "/" +
                          stamps.Length.ToString(Data.Locale));
            }
        }
        catch (Exception ex)
        {
            L.LogError(ex);
        }

        if (c)
        {
            Save<VehicleTeamkill>(7);
            c = false;
        }
    }
    public static void LogBanPlayer(ulong violator, ulong caller, string reason, int duration, DateTime timestamp)
    {
        Task.Run(async () =>
        {
            await Data.DatabaseManager.AddBan(violator, caller, duration, reason!);
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                await UCWarfare.ToUpdate();
                RequestResponse response = await NetCalls.SendPlayerBanned.Request(NetCalls.AckPlayerBanned, UCWarfare.I.NetClient!, violator, caller, reason, duration, timestamp, 10000);
                if (response.Responded)
                    return;
            }
            lock (_pendingBans)
                _pendingBans.Add(new Ban(violator, caller, reason, duration, timestamp));
            Save<Ban>(0);
        });
    }
    public static void LogUnbanPlayer(ulong violator, ulong caller, DateTime timestamp)
    {
        Task.Run(async () =>
        {
            await Data.DatabaseManager.AddUnban(violator, caller);
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                await UCWarfare.ToUpdate();
                RequestResponse response = await NetCalls.SendPlayerUnbanned.Request(NetCalls.AckPlayerUnbanned, UCWarfare.I.NetClient!, violator, caller, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (_pendingUnbans)
                _pendingUnbans.Add(new Unban(violator, caller, timestamp));
            Save<Unban>(1);
        });
    }
    public static void LogKickPlayer(ulong violator, ulong caller, string reason, DateTime timestamp)
    {
        Task.Run(async () =>
        {
            await Data.DatabaseManager.AddKick(violator, caller, reason);
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                await UCWarfare.ToUpdate();
                RequestResponse response = await NetCalls.SendPlayerKicked.Request(NetCalls.AckPlayerKicked, UCWarfare.I.NetClient!, violator, caller, reason, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (_pendingKicks)
                _pendingKicks.Add(new Kick(violator, caller, reason, timestamp));
            Save<Kick>(2);
        });
    }
    public static void LogWarnPlayer(ulong violator, ulong caller, string reason, DateTime timestamp)
    {
        Task.Run(async () =>
        {
            await Data.DatabaseManager.AddWarning(violator, caller, reason);
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendPlayerWarned.Request(NetCalls.AckPlayerWarned, UCWarfare.I.NetClient!, violator, caller, reason, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (_pendingWarnings)
                _pendingWarnings.Add(new Warn(violator, caller, reason, timestamp));
            Save<Warn>(3);
        });
    }
    public static void LogMutePlayer(ulong violator, ulong caller, EMuteType type, int duration, string reason, DateTime timestamp)
    {
        Task.Run(async () =>
        {
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendPlayerMuted.Request(NetCalls.AckPlayerMuted, UCWarfare.I.NetClient!, violator, caller, type, duration, reason, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (_pendingMutes)
                _pendingMutes.Add(new Mute(violator, caller, type, duration, reason, timestamp));
            Save<Mute>(4);
        });
    }
    public static void LogBattlEyeKicksPlayer(ulong violator, string reason, DateTime timestamp)
    {
        Task.Run(async () =>
        {
            await Data.DatabaseManager.AddBattleyeKick(violator, reason);
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendPlayerBattleyeKicked.Request(NetCalls.AckPlayerBattleyeKicked, UCWarfare.I.NetClient!, violator, reason, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (_pendingBattlEyeKicks)
                _pendingBattlEyeKicks.Add(new BattlEyeKick(violator, reason, timestamp));
            Save<BattlEyeKick>(5);
        });
    }
    public static void LogTeamkill(ulong violator, ulong teamkilled, string deathCause, string itemName, ushort itemId, float distance, DateTime timestamp)
    {
        Task.Run(async () =>
        {
            await Data.DatabaseManager.AddTeamkill(violator, teamkilled, deathCause, itemName, itemId, distance);
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendTeamkill.Request(NetCalls.AckTeamkill, UCWarfare.I.NetClient!, violator, teamkilled, deathCause, itemName, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (_pendingTeamkills)
                _pendingTeamkills.Add(new Teamkill(violator, teamkilled, deathCause, itemName, timestamp));
            Save<Teamkill>(6);
        });
    }
    public static void LogVehicleTeamkill(ulong violator, ushort vehicleId, string vehicleName, DateTime timestamp)
    {
        Task.Run(async () =>
        {
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendVehicleTeamkilled.Request(NetCalls.AckVehicleTeamkill, UCWarfare.I.NetClient!, violator, vehicleId, vehicleName, timestamp, 10000);
                if (response.Responded)
                    return;
            }

            lock (_pendingVehicleTeamkills)
                _pendingVehicleTeamkills.Add(new VehicleTeamkill(violator, vehicleId, vehicleName, timestamp));
            Save<VehicleTeamkill>(7);
        });
    }
    public static void LogUnmutePlayer(ulong violator, ulong callerId, DateTime now)
    {
        Task.Run(async () =>
        {
            await UCWarfare.ToUpdate();
            if (UCWarfare.CanUseNetCall)
            {
                RequestResponse response = await NetCalls.SendPlayerUnmuted.Request(NetCalls.AckPlayerUnmuted, UCWarfare.I.NetClient!, violator, callerId, now, 10000);
                if (response.Responded)
                    return;
            }

            lock (_pendingUnmutes)
                _pendingUnmutes.Add(new Unmute(violator, callerId, now));
            Save<Unmute>(8);
        });
    }
    public static void BanPlayer(ulong targetId, ulong callerId, string reason, int duration)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? target = UCPlayer.FromID(targetId);
        UCPlayer? caller = UCPlayer.FromID(callerId);
        FPlayerName name;
        FPlayerName callerName;
        uint ipv4;
        Task.Run(async () =>
        {
            List<byte[]> hwids = await GetAllHWIDs(targetId);
            await UCWarfare.ToUpdate();
            if (target is not null) // player is online
            {
                CSteamID id = target.Player.channel.owner.playerID.steamID;
                ipv4 = SteamGameServerNetworkingUtils.getIPv4AddressOrZero(id);
                name = F.GetPlayerOriginalNames(target);
                Provider.requestBanPlayer(Provider.server, id, ipv4, hwids, reason, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration));
            }
            else
            {
                ipv4 = Data.DatabaseManager.GetPackedIP(targetId);
                name = F.GetPlayerOriginalNames(targetId);
                F.OfflineBan(targetId, ipv4, caller is null ? CSteamID.Nil : caller.Player.channel.owner.playerID.steamID,
                    reason!, duration == -1 ? SteamBlacklist.PERMANENT : checked((uint)duration), hwids.ToArray());
            }
            if (callerId != 0)
                callerName = F.GetPlayerOriginalNames(callerId);
            else
                callerName = FPlayerName.Console;
            ActionLogger.Add(EActionLogType.BAN_PLAYER, $"BANNED {targetId.ToString(Data.Locale)} FOR \"{reason}\" DURATION: " +
                (duration == -1 ? "PERMANENT" : (duration.ToString(Data.Locale) + " SECONDS")), callerId);

            LogBanPlayer(targetId, callerId, reason, duration, DateTime.Now);

            if (duration == -1)
            {
                if (callerId == 0)
                {
                    L.Log($"{name.PlayerName} ({targetId}) was permanently banned by an operator because {reason!}.", ConsoleColor.Cyan);
                    Chat.Broadcast(T.BanPermanentSuccessBroadcastOperator, target as IPlayer ?? name);
                }
                else
                {
                    L.Log($"{name.PlayerName} ({targetId}) was permanenly banned by {callerName.PlayerName} ({callerId}) because {reason!}.", ConsoleColor.Cyan);
                    Chat.Broadcast(LanguageSet.AllBut(callerId), T.BanPermanentSuccessBroadcast, target as IPlayer ?? name, caller as IPlayer ?? callerName);
                    caller?.SendChat(T.BanPermanentSuccessFeedback, target as IPlayer ?? name);
                }
            }
            else
            {
                string time = Localization.GetTimeFromSeconds(duration, L.DEFAULT);
                if (callerId == 0)
                {
                    L.Log($"{name.PlayerName} ({targetId}) was banned for {time} by an operator because {reason!}.", ConsoleColor.Cyan);
                    bool f = false;
                    foreach (LanguageSet set in LanguageSet.All())
                    {
                        if (f || !set.Language.Equals(L.DEFAULT, StringComparison.Ordinal))
                        {
                            time = Localization.GetTimeFromSeconds(duration, set.Language);
                            f = true;
                        }
                        Chat.Broadcast(set, T.BanSuccessBroadcastOperator, target as IPlayer ?? name, time);
                    }
                }
                else
                {
                    L.Log($"{name.PlayerName} ({targetId}) was banned for {time} by {callerName.PlayerName} ({callerId}) because {reason!}.", ConsoleColor.Cyan);
                    bool f = false;
                    foreach (LanguageSet set in LanguageSet.AllBut(callerId))
                    {
                        if (f || !set.Language.Equals(L.DEFAULT, StringComparison.Ordinal))
                        {
                            time = Localization.GetTimeFromSeconds(duration, set.Language);
                            f = true;
                        }
                        Chat.Broadcast(set, T.BanSuccessBroadcast, target as IPlayer ?? name, caller as IPlayer ?? callerName, time);
                    }
                    if (f)
                        time = Localization.GetTimeFromSeconds(duration, callerId);
                    else if (Data.Languages.TryGetValue(callerId, out string lang) && !lang.Equals(L.DEFAULT, StringComparison.Ordinal))
                        time = Localization.GetTimeFromSeconds(duration, lang);
                    caller?.SendChat(T.BanSuccessFeedback, target as IPlayer ?? name, time);
                }
            }
        });
    }
    public static void KickPlayer(ulong target, ulong caller, string reason)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? targetPlayer = UCPlayer.FromID(target);
        if (targetPlayer is null)
            return;
        FPlayerName names = F.GetPlayerOriginalNames(target);
        Provider.kick(targetPlayer.Player.channel.owner.playerID.steamID, reason);

        LogKickPlayer(target, caller, reason, DateTime.Now);

        ActionLogger.Add(EActionLogType.KICK_PLAYER, $"KICKED {target.ToString(Data.Locale)} FOR \"{reason}\"", caller);
        if (caller == 0)
        {
            L.Log($"{names.PlayerName} ({target}) was kicked by an operator because {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(T.KickSuccessBroadcastOperator, targetPlayer as IPlayer ?? names);
        }
        else
        {
            FPlayerName callerNames = F.GetPlayerOriginalNames(caller);
            L.Log($"{names.PlayerName} ({target}) was kicked by {callerNames.PlayerName} ({caller}) because {reason}.", ConsoleColor.Cyan);
            UCPlayer? callerPlayer = UCPlayer.FromID(caller);
            Chat.Broadcast(LanguageSet.AllBut(caller), T.KickSuccessBroadcast, targetPlayer as IPlayer ?? names, callerPlayer as IPlayer ?? callerNames);
            callerPlayer?.SendChat(T.KickSuccessFeedback, targetPlayer as IPlayer ?? names);
        }
    }
    public static void UnbanPlayer(ulong targetId, ulong callerId)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FPlayerName targetNames = F.GetPlayerOriginalNames(targetId);
        if (!Provider.requestUnbanPlayer(callerId == 0 ? CSteamID.Nil : new CSteamID(callerId), new CSteamID(targetId)))
        {
            L.Log(callerId + " not banned.", ConsoleColor.Cyan);
            return;
        }

        LogUnbanPlayer(targetId, callerId, DateTime.Now);

        string tid = targetId.ToString(Data.Locale);
        ActionLogger.Add(EActionLogType.UNBAN_PLAYER, $"UNBANNED {tid}", callerId);
        if (callerId == 0)
        {
            L.Log($"{targetNames.PlayerName} ({tid}) was unbanned by an operator.", ConsoleColor.Cyan);
            Chat.Broadcast(T.UnbanSuccessBroadcastOperator, targetNames);
        }
        else
        {
            FPlayerName callerNames = F.GetPlayerOriginalNames(callerId);
            UCPlayer? caller = UCPlayer.FromID(callerId);
            L.Log($"{targetNames.PlayerName} ({tid}) was unbanned by {callerNames.PlayerName} ({callerId}).", ConsoleColor.Cyan);
            caller?.SendChat(T.UnbanSuccessFeedback, targetNames);
            Chat.Broadcast(LanguageSet.AllBut(callerId), T.UnbanSuccessBroadcast, targetNames, caller as IPlayer ?? callerNames);
        }
    }
    public static void WarnPlayer(ulong targetId, ulong callerId, string reason)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? target = UCPlayer.FromID(targetId);
        if (target is null)
            return;
        UCPlayer? caller = UCPlayer.FromID(callerId);
        FPlayerName targetNames = F.GetPlayerOriginalNames(target);

        LogWarnPlayer(targetId, callerId, reason, DateTime.Now);

        string tid = targetId.ToString(Data.Locale);
        ActionLogger.Add(EActionLogType.WARN_PLAYER, $"WARNED {tid} FOR \"{reason}\"", callerId);
        if (callerId == 0)
        {
            L.Log($"{targetNames.PlayerName} ({targetId}) was warned by an operator because {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(LanguageSet.AllBut(targetId), T.WarnSuccessBroadcastOperator, target);

            string lang = Localization.GetLang(target.Steam64);
            ToastMessage.QueueMessage(target, new ToastMessage(T.WarnSuccessDMOperator.Translate(T.WarnSuccessDMOperator.Translate(lang),
                lang, reason, target, target.GetTeam(), T.WarnSuccessDMOperator.Flags | TranslationFlags.UnityUI), EToastMessageSeverity.WARNING));

            target.SendChat(T.WarnSuccessDMOperator, reason!);
        }
        else
        {
            FPlayerName callerNames = F.GetPlayerOriginalNames(callerId);
            L.Log($"{targetNames.PlayerName} ({targetId}) was warned by {callerNames.PlayerName} ({caller}) because {reason}.", ConsoleColor.Cyan);
            IPlayer caller2 = caller as IPlayer ?? callerNames;
            Chat.Broadcast(LanguageSet.AllBut(callerId, targetId), T.WarnSuccessBroadcast, target, caller2);
            caller?.SendChat(T.WarnSuccessFeedback, target);

            string lang = Localization.GetLang(target.Steam64);
            ToastMessage.QueueMessage(target, new ToastMessage(T.WarnSuccessDM.Translate(T.WarnSuccessDM.Translate(lang),
                lang, caller2, reason, target, target.GetTeam(), T.WarnSuccessDM.Flags | TranslationFlags.UnityUI), EToastMessageSeverity.WARNING));

            target.SendChat(T.WarnSuccessDM, caller2, reason!);
        }
    }
    public static void MutePlayer(ulong target, ulong admin, EMuteType type, int duration, string reason)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer? muted = UCPlayer.FromID(target);
        UCPlayer? muter = UCPlayer.FromID(admin);
        Task.Run(async () =>
        {
            DateTime now = DateTime.Now;
            await Data.DatabaseManager.NonQueryAsync(
                "INSERT INTO `muted` (`Steam64`, `Admin`, `Reason`, `Duration`, `Timestamp`, `Type`) VALUES (@0, @1, @2, @3, @4, @5);",
                new object[] { target, admin, reason, duration, now, (byte)type });
            FPlayerName names = await F.GetPlayerOriginalNamesAsync(target);
            FPlayerName names2 = await F.GetPlayerOriginalNamesAsync(admin);
            await UCWarfare.ToUpdate();
            DateTime unmutedTime = duration == -1 ? DateTime.MaxValue : now + TimeSpan.FromSeconds(duration);
            if (muted is not null && muted.TimeUnmuted < unmutedTime)
            {
                muted.TimeUnmuted = unmutedTime;
                muted.MuteReason = reason;
                muted.MuteType = type;
            }

            LogMutePlayer(target, admin, type, duration, reason, DateTime.Now);

            string dur = duration == -1 ? "PERMANENT" : duration.GetTimeFromSeconds(0);
            ActionLogger.Add(EActionLogType.MUTE_PLAYER, $"MUTED {target} FOR \"{reason}\" DURATION: " + dur, admin);

            if (admin == 0)
            {
                if (duration == -1)
                {
                    foreach (LanguageSet set in LanguageSet.AllBut(target))
                        Chat.Broadcast(set, T.MutePermanentSuccessBroadcastOperator, names, names, type);

                    L.Log($"{names.PlayerName} ({target}) was permanently {type} muted for {reason} by an operator.", ConsoleColor.Cyan);
                }
                else
                {
                    foreach (LanguageSet set in LanguageSet.AllBut(target))
                        Chat.Broadcast(set, T.MuteSuccessBroadcastOperator, names, names, dur, type);

                    L.Log($"{names.PlayerName} ({target}) was {type} muted for {reason} by an operator. Duration: {dur}.", ConsoleColor.Cyan);
                }
            }
            else
            {
                if (duration == -1)
                {
                    foreach (LanguageSet set in LanguageSet.AllBut(target, admin))
                        Chat.Broadcast(set, T.MutePermanentSuccessBroadcast, names, names, type, names2);

                    L.Log($"{names.PlayerName} ({target}) was permanently {type} muted for {reason} by {names2.PlayerName} ({admin}).", ConsoleColor.Cyan);
                }
                else
                {
                    foreach (LanguageSet set in LanguageSet.AllBut(target, admin))
                        Chat.Broadcast(set, T.MuteSuccessBroadcast, names, names, dur, type, names2);

                    L.Log($"{names.PlayerName} ({target}) was {type} muted for {reason} by {names2.PlayerName} ({admin}). Duration: {dur}.", ConsoleColor.Cyan);
                }
            }
            if (muted != null)
            {
                if (admin == 0)
                {
                    if (duration == -1)
                        muted.SendChat(T.MuteSuccessDMPermanentOperator, reason, type);
                    else
                        muted.SendChat(T.MuteSuccessDMOperator, reason, dur, type);
                }
                else
                {
                    if (duration == -1)
                        muted.SendChat(T.MuteSuccessDMPermanent, names2, reason, type);
                    else
                        muted.SendChat(T.MuteSuccessDM, names2, reason, dur, type);
                }
            }
        }).ConfigureAwait(false);
    }
    public static void UnmutePlayer(ulong targetId, ulong callerId)
    {
        UCPlayer? caller = UCPlayer.FromID(callerId);
        UCPlayer? onlinePlayer = UCPlayer.FromID(targetId);
        Task.Run(async () =>
        {
            FPlayerName names = await Data.DatabaseManager.GetUsernamesAsync(targetId);
            if (names.WasFound)
            {
                int rows = await Data.DatabaseManager.NonQueryAsync("UPDATE `muted` SET `Deactivated` = 1 WHERE `Steam64` = @0 AND " +
                                                                    "`Deactivated` = 0 AND (`Duration` = -1 OR TIME_TO_SEC(TIMEDIFF(`Timestamp`, NOW())) / -60 < `Duration`)", new object[] { targetId });

                await UCWarfare.ToUpdate();
                if (rows == 0)
                {
                    if (caller is not null)
                        caller.SendChat(T.UnmuteNotMuted, names);
                    else if (callerId == 0)
                        L.Log(F.RemoveRichText(T.UnmuteNotMuted.Translate(L.DEFAULT, names, out Color color)), F.GetClosestConsoleColor(color));
                }
                else
                {
                    onlinePlayer ??= UCPlayer.FromID(targetId);
                    if (onlinePlayer is not null)
                    {
                        onlinePlayer.MuteReason = null;
                        onlinePlayer.MuteType = EMuteType.NONE;
                        onlinePlayer.TimeUnmuted = DateTime.MinValue;
                    }
                    LogUnmutePlayer(targetId, callerId, DateTime.Now);
                    FPlayerName n2 = await F.GetPlayerOriginalNamesAsync(callerId);
                    await UCWarfare.ToUpdate();
                    if (callerId == 0)
                    {
                        Chat.Broadcast(LanguageSet.AllBut(targetId), T.UnmuteSuccessBroadcastOperator, names);
                        onlinePlayer?.SendChat(T.UnmuteSuccessDMOperator);
                    }
                    else
                    {
                        Chat.Broadcast(LanguageSet.AllBut(targetId, callerId), T.UnmuteSuccessBroadcast, names, n2);
                        onlinePlayer?.SendChat(T.UnmuteSuccessDM, n2);
                        caller?.SendChat(T.UnmuteSuccessFeedback, names);
                    }
                    ActionLogger.Add(EActionLogType.UNMUTE_PLAYER, targetId.ToString() + " unmuted.", callerId);
                    L.Log($"{names.PlayerName} ({targetId}) was unmuted by {(callerId == 0 ? "an operator" : (n2.PlayerName + "(" + callerId + ")"))}.");
                }
            }
            else
            {
                await UCWarfare.ToUpdate();
                if (caller is not null)
                    caller.SendChat(T.PlayerNotFound);
                else if (callerId == 0)
                    L.Log(F.RemoveRichText(T.PlayerNotFound.Translate(L.DEFAULT, out Color color)), F.GetClosestConsoleColor(color));
            }
        });
    }
    private static void OnPlayerDied(PlayerDied e)
    {
        if (e.WasTeamkill && e.Killer is not null)
        {
            Asset a = Assets.find(e.PrimaryAsset);
            string itemName = a?.FriendlyName ?? e.PrimaryAsset.ToString("N");
            LogTeamkill(e.Killer.Steam64, e.Player.Steam64, e.Cause.ToString(), itemName, a == null ? (ushort)0 : a.id, e.KillDistance, DateTime.Now);
        }
    }
    private interface ITimestampOffense
    {
        public DateTime Timestamp { get; set; }
        public bool Sent { get; set; }
        public bool Received { get; set; }
    }

    private struct Ban : ITimestampOffense
    {
        public ulong Violator;
        public ulong Admin;
        public string Reason;
        public int Duration;
        public DateTime Timestamp { get; set; }
        public bool Sent { get; set; }
        public bool Received { get; set; }
        [JsonConstructor]
        public Ban(ulong violator, ulong admin, string reason, int duration, DateTime timestamp)
        {
            Violator = violator;
            Admin = admin;
            Reason = reason;
            Duration = duration;
            Timestamp = timestamp;
        }
    }
    private struct Unban : ITimestampOffense
    {
        public ulong Violator;
        public ulong Admin;
        public DateTime Timestamp { get; set; }
        public bool Sent { get; set; }
        public bool Received { get; set; }
        [JsonConstructor]
        public Unban(ulong violator, ulong admin, DateTime timestamp)
        {
            Violator = violator;
            Admin = admin;
            Timestamp = timestamp;
        }
    }
    private struct Kick : ITimestampOffense
    {
        public ulong Violator;
        public ulong Admin;
        public string Reason;
        public DateTime Timestamp { get; set; }
        public bool Sent { get; set; }
        public bool Received { get; set; }
        [JsonConstructor]
        public Kick(ulong violator, ulong admin, string reason, DateTime timestamp)
        {
            Violator = violator;
            Admin = admin;
            Reason = reason;
            Timestamp = timestamp;
        }
    }
    private struct Warn : ITimestampOffense
    {
        public ulong Violator;
        public ulong Admin;
        public string Reason;
        public DateTime Timestamp { get; set; }
        public bool Sent { get; set; }
        public bool Received { get; set; }
        [JsonConstructor]
        public Warn(ulong violator, ulong admin, string reason, DateTime timestamp)
        {
            Violator = violator;
            Admin = admin;
            Reason = reason;
            Timestamp = timestamp;
        }
    }
    private struct Mute : ITimestampOffense
    {
        public ulong Violator;
        public ulong Admin;
        public EMuteType MuteType;
        public int Duration;
        public string Reason;
        public DateTime Timestamp { get; set; }
        public bool Sent { get; set; }
        public bool Received { get; set; }
        [JsonConstructor]
        public Mute(ulong violator, ulong admin, EMuteType muteType, int duration, string reason, DateTime timestamp)
        {
            Violator = violator;
            Admin = admin;
            MuteType = muteType;
            Duration = duration;
            Reason = reason;
            Timestamp = timestamp;
        }
    }
    private struct BattlEyeKick : ITimestampOffense
    {
        public ulong Violator;
        public string Reason;
        public DateTime Timestamp { get; set; }
        public bool Sent { get; set; }
        public bool Received { get; set; }
        [JsonConstructor]
        public BattlEyeKick(ulong violator, string reason, DateTime timestamp)
        {
            Violator = violator;
            Reason = reason;
            Timestamp = timestamp;
        }
    }
    private struct Teamkill : ITimestampOffense
    {
        public ulong Violator;
        public ulong Dead;
        public string DeathCause;
        public string ItemName;
        public DateTime Timestamp { get; set; }
        public bool Sent { get; set; }
        public bool Received { get; set; }
        [JsonConstructor]
        public Teamkill(ulong violator, ulong dead, string deathCause, string itemName, DateTime timestamp)
        {
            Violator = violator;
            Dead = dead;
            DeathCause = deathCause;
            ItemName = itemName;
            Timestamp = timestamp;
        }
    }
    private struct VehicleTeamkill : ITimestampOffense
    {
        public ulong Violator;
        public ushort VehicleID;
        public string VehicleName;
        public DateTime Timestamp { get; set; }
        public bool Sent { get; set; }
        public bool Received { get; set; }
        [JsonConstructor]
        public VehicleTeamkill(ulong violator, ushort vehicleId, string vehicleName, DateTime timestamp)
        {
            Violator = violator;
            VehicleID = vehicleId;
            VehicleName = vehicleName;
            Timestamp = timestamp;
        }
    }
    private struct Unmute : ITimestampOffense
    {
        public ulong Violator;
        public ulong Admin;
        public DateTime Timestamp { get; set; }
        public bool Sent { get; set; }
        public bool Received { get; set; }
        [JsonConstructor]
        public Unmute(ulong violator, ulong admin, DateTime timestamp)
        {
            Violator = violator;
            Admin = admin;
            Timestamp = timestamp;
        }
    }
    public static class NetCalls
    {
        public static readonly NetCall<ulong, ulong, string, int, DateTime> SendBanRequest = new NetCall<ulong, ulong, string, int, DateTime>(ReceiveBanRequest);
        public static readonly NetCall<ulong, ulong, DateTime> SendUnbanRequest = new NetCall<ulong, ulong, DateTime>(ReceiveUnbanRequest);
        public static readonly NetCall<ulong, ulong, string, DateTime> SendKickRequest = new NetCall<ulong, ulong, string, DateTime>(ReceieveKickRequest);
        public static readonly NetCall<ulong, ulong, string, DateTime> SendWarnRequest = new NetCall<ulong, ulong, string, DateTime>(ReceiveWarnRequest);
        public static readonly NetCall<ulong, ulong, EMuteType, int, string, DateTime> SendMuteRequest = new NetCall<ulong, ulong, EMuteType, int, string, DateTime>(ReceieveMuteRequest);
        public static readonly NetCall<ulong, ulong, DateTime> SendUnmuteRequest = new NetCall<ulong, ulong, DateTime>(ReceieveUnmuteRequest);
        public static readonly NetCall<ulong> GrantAdminRequest = new NetCall<ulong>(ReceiveGrantAdmin);
        public static readonly NetCall<ulong> RevokeAdminRequest = new NetCall<ulong>(ReceiveRevokeAdmin);
        public static readonly NetCall<ulong> GrantInternRequest = new NetCall<ulong>(ReceiveGrantIntern);
        public static readonly NetCall<ulong> RevokeInternRequest = new NetCall<ulong>(ReceiveRevokeIntern);
        public static readonly NetCall<ulong> GrantHelperRequest = new NetCall<ulong>(ReceiveGrantHelper);
        public static readonly NetCall<ulong> RevokeHelperRequest = new NetCall<ulong>(ReceiveRevokeHelper);

        public static readonly NetCall<ulong, ulong, string, int, DateTime> SendPlayerBanned = new NetCall<ulong, ulong, string, int, DateTime>(1001);
        public static readonly NetCall<ulong, ulong, DateTime> SendPlayerUnbanned = new NetCall<ulong, ulong, DateTime>(1002);
        public static readonly NetCall<ulong, ulong, string, DateTime> SendPlayerKicked = new NetCall<ulong, ulong, string, DateTime>(1003);
        public static readonly NetCall<ulong, ulong, string, DateTime> SendPlayerWarned = new NetCall<ulong, ulong, string, DateTime>(1004);
        public static readonly NetCall<ulong, string, DateTime> SendPlayerBattleyeKicked = new NetCall<ulong, string, DateTime>(1005);
        public static readonly NetCall<ulong, ulong, string, string, DateTime> SendTeamkill = new NetCall<ulong, ulong, string, string, DateTime>(1006);
        public static readonly NetCall<ulong, ulong, EMuteType, int, string, DateTime> SendPlayerMuted = new NetCall<ulong, ulong, EMuteType, int, string, DateTime>(1027);
        public static readonly NetCall<ulong, ushort, string, DateTime> SendVehicleTeamkilled = new NetCall<ulong, ushort, string, DateTime>(1112);
        public static readonly NetCall<ulong, ulong, DateTime> SendPlayerUnmuted = new NetCall<ulong, ulong, DateTime>(1020);

        public static readonly NetCall AckPlayerBanned = new NetCall(1138);
        public static readonly NetCall AckPlayerUnbanned = new NetCall(1139);
        public static readonly NetCall AckPlayerKicked = new NetCall(1140);
        public static readonly NetCall AckPlayerWarned = new NetCall(1141);
        public static readonly NetCall AckPlayerBattleyeKicked = new NetCall(1142);
        public static readonly NetCall AckTeamkill = new NetCall(1143);
        public static readonly NetCall AckPlayerMuted = new NetCall(1144);
        public static readonly NetCall AckVehicleTeamkill = new NetCall(1145);
        public static readonly NetCall AckPlayerUnmuted = new NetCall(1146);

        [NetCall(ENetCall.FROM_SERVER, 1007)]
        internal static async Task ReceiveBanRequest(MessageContext context, ulong target, ulong admin, string reason, int duration, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            BanPlayer(target, admin, reason, duration);
        }

        [NetCall(ENetCall.FROM_SERVER, 1009)]
        internal static async Task ReceiveUnbanRequest(MessageContext context, ulong target, ulong admin, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            UnbanPlayer(target, admin);
        }

        [NetCall(ENetCall.FROM_SERVER, 1010)]
        internal static async Task ReceieveKickRequest(MessageContext context, ulong target, ulong admin, string reason, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            KickPlayer(target, admin, reason);
        }

        [NetCall(ENetCall.FROM_SERVER, 1028)]
        internal static async Task ReceieveMuteRequest(MessageContext context, ulong target, ulong admin, EMuteType type, int duration, string reason, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            MutePlayer(target, admin, type, duration, reason);
        }

        [NetCall(ENetCall.FROM_SERVER, 1021)]
        internal static async Task ReceieveUnmuteRequest(MessageContext context, ulong target, ulong admin, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            UnmutePlayer(target, admin);
        }

        [NetCall(ENetCall.FROM_SERVER, 1011)]
        internal static async Task ReceiveWarnRequest(MessageContext context, ulong target, ulong admin, string reason, DateTime timestamp)
        {
            await UCWarfare.ToUpdate();
            WarnPlayer(target, admin, reason);
        }

        [NetCall(ENetCall.FROM_SERVER, 1103)]
        internal static void ReceiveGrantAdmin(MessageContext context, ulong player)
        {
            PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.ADMIN);
        }

        [NetCall(ENetCall.FROM_SERVER, 1104)]
        internal static void ReceiveRevokeAdmin(MessageContext context, ulong player)
        {
            RevokeAll(player);
        }

        [NetCall(ENetCall.FROM_SERVER, 1105)]
        internal static void ReceiveGrantIntern(MessageContext context, ulong player)
        {
            PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.TRIAL_ADMIN);
        }

        [NetCall(ENetCall.FROM_SERVER, 1106)]
        internal static void ReceiveRevokeIntern(MessageContext context, ulong player)
        {
            RevokeAll(player);
        }

        [NetCall(ENetCall.FROM_SERVER, 1107)]
        internal static void ReceiveGrantHelper(MessageContext context, ulong player)
        {
            PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.HELPER);
        }

        [NetCall(ENetCall.FROM_SERVER, 1108)]
        internal static void ReceiveRevokeHelper(MessageContext context, ulong player)
        {
            RevokeAll(player);
        }
        private static void RevokeAll(ulong player)
        {
            PermissionSaver.Instance.SetPlayerPermissionLevel(player, EAdminType.MEMBER);
        }
    }
}