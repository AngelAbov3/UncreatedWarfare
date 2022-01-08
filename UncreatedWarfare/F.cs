﻿using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Networking.Encoding;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Color = UnityEngine.Color;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare
{
    public static class F
    {
        public const float SPAWN_HEIGHT_ABOVE_GROUND = 0.5f;
        public static readonly List<char> vowels = new List<char> { 'a', 'e', 'i', 'o', 'u' };
        /// <summary>Convert an HTMLColor string to a actual color.</summary>
        /// <param name="htmlColorCode">A hexadecimal/HTML color key.</param>
        public static Color Hex(this string htmlColorCode)
        {
            string code = "#";
            if (htmlColorCode.Length > 0 && htmlColorCode[0] != '#')
                code += htmlColorCode;
            else
                code = htmlColorCode;
            if (ColorUtility.TryParseHtmlString(code, out Color color))
                return color;
            else if (ColorUtility.TryParseHtmlString(htmlColorCode, out color))
                return color;
            else return Color.white;
        }
        public static string MakeRemainder(this string[] array, int startIndex = 0, int length = -1, string deliminator = " ")
        {
            StringBuilder builder = new StringBuilder();
            for (int i = startIndex; i < (length == -1 ? array.Length : length); i++)
            {
                if (i > startIndex) builder.Append(deliminator);
                builder.Append(array[i]);
            }
            return builder.ToString();
        }
        public static string[] ReadStringArray(ByteReader R)
        {
            int length = R.ReadInt32();
            string[] rtn = new string[length];
            for (int i = 0; i < length; i++)
                rtn[i] = R.ReadString();
            return rtn;
        }
        public static void WriteStringArray(ByteWriter W, string[] A)
        {
            W.Write(A.Length);
            for (int i = 0; i < A.Length; i++)
                W.Write(A[i]);
        }
        public static int DivideRemainder(float divisor, float dividend, out int remainder)
        {
            float answer = divisor / dividend;
            remainder = (int)Mathf.Round((answer - Mathf.Floor(answer)) * dividend);
            return (int)Mathf.Floor(answer);
        }
        public static uint DivideRemainder(uint divisor, uint dividend, out uint remainder)
        {
            decimal answer = (decimal)divisor / dividend;
            remainder = (uint)Math.Round((answer - Math.Floor(answer)) * dividend);
            return (uint)Math.Floor(answer);
        }
        public static uint DivideRemainder(uint divisor, decimal dividend, out uint remainder)
        {
            decimal answer = divisor / dividend;
            remainder = (uint)Math.Round((answer - Math.Floor(answer)) * dividend);
            return (uint)Math.Floor(answer);
        }

        public static bool PermissionCheck(this IRocketPlayer player, EAdminType type)
        {
            List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(player, false);
            for (int i = 0; i < groups.Count; i++)
            {
                RocketPermissionsGroup grp = groups[i];
                if (grp.Id == "default") continue;
                if (grp.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup)
                {
                    if ((type & EAdminType.ADMIN_OFF_DUTY) == EAdminType.ADMIN_OFF_DUTY) return true;
                    continue;
                }
                if (grp.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup)
                {
                    if ((type & EAdminType.ADMIN_ON_DUTY) == EAdminType.ADMIN_ON_DUTY) return true;
                    continue;
                }
                if (grp.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup)
                {
                    if ((type & EAdminType.TRIAL_ADMIN_OFF_DUTY) == EAdminType.TRIAL_ADMIN_OFF_DUTY) return true;
                    continue;
                }
                if (grp.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup)
                {
                    if ((type & EAdminType.TRIAL_ADMIN_ON_DUTY) == EAdminType.TRIAL_ADMIN_ON_DUTY) return true;
                    continue;
                }
                if (grp.Id == UCWarfare.Config.AdminLoggerSettings.HelperGroup)
                {
                    if ((type & EAdminType.HELPER) == EAdminType.HELPER) return true;
                    continue;
                }
            }
            return false;
        }
        public static EAdminType GetPermissions(this IRocketPlayer player)
        {
            List<RocketPermissionsGroup> groups = R.Permissions.GetGroups(player, false);
            EAdminType perms = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                RocketPermissionsGroup grp = groups[i];
                if (grp.Id == "default") continue;
                if (grp.Id == UCWarfare.Config.AdminLoggerSettings.AdminOffDutyGroup || grp.Id == UCWarfare.Config.AdminLoggerSettings.AdminOnDutyGroup)
                {
                    perms |= EAdminType.ADMIN;
                }
                else if (grp.Id == UCWarfare.Config.AdminLoggerSettings.InternOffDutyGroup || grp.Id == UCWarfare.Config.AdminLoggerSettings.InternOnDutyGroup)
                {
                    perms |= EAdminType.TRIAL_ADMIN;
                }
                else if (grp.Id == UCWarfare.Config.AdminLoggerSettings.HelperGroup)
                {
                    perms |= EAdminType.HELPER;
                }
            }
            return perms;
        }
        public static bool OnDutyOrAdmin(this IRocketPlayer player) => (player is UnturnedPlayer pl && pl.Player.channel.owner.isAdmin) || (player is UCPlayer upl && upl.Player.channel.owner.isAdmin) || player.PermissionCheck(EAdminType.MODERATE_PERMS_ON_DUTY);
        public static bool OnDuty(this IRocketPlayer player) => player.PermissionCheck(EAdminType.MODERATE_PERMS_ON_DUTY);
        public static bool OffDuty(this IRocketPlayer player) => !OnDuty(player);
        public static bool IsIntern(this IRocketPlayer player) => player.PermissionCheck(EAdminType.TRIAL_ADMIN);
        public static bool IsAdmin(this IRocketPlayer player) => player.PermissionCheck(EAdminType.ADMIN);
        public static bool IsHelper(this IRocketPlayer player) => player.PermissionCheck(EAdminType.HELPER);
        /// <summary>Ban someone for <paramref name="duration"/> seconds.</summary>
        /// <param name="duration">Duration of ban IN SECONDS</param>
        public static void OfflineBan(ulong BannedID, uint IPAddress, CSteamID BannerID, string reason, uint duration)
        {
            CSteamID banned = new CSteamID(BannedID);
            Provider.ban(banned, reason, duration);
            for (int index = 0; index < SteamBlacklist.list.Count; ++index)
            {
                if (SteamBlacklist.list[index].playerID.m_SteamID == BannedID)
                {
                    SteamBlacklist.list[index].judgeID = BannerID;
                    SteamBlacklist.list[index].reason = reason;
                    SteamBlacklist.list[index].duration = duration;
                    SteamBlacklist.list[index].banned = Provider.time;
                    return;
                }
            }
            SteamBlacklist.list.Add(new SteamBlacklistID(banned, IPAddress, BannerID, reason, duration, Provider.time));
        }
        public static string An(this string word) => (word.Length > 0 && vowels.Contains(word[0].ToString().ToLower()[0])) ? "n" : "";
        public static string An(this char letter) => vowels.Contains(letter.ToString().ToLower()[0]) ? "n" : "";
        public static string S(this int number) => number == 1 ? "" : "s";
        public static string S(this float number) => number == 1 ? "" : "s";
        public static string S(this uint number) => number == 1 ? "" : "s";
        public static ulong GetTeamFromPlayerSteam64ID(this ulong s64)
        {
            if (!(Data.Gamemode is TeamGamemode))
            {
                SteamPlayer pl2 = PlayerTool.getSteamPlayer(s64);
                if (pl2 == null) return 0;
                else return pl2.player.quests.groupID.m_SteamID;
            }
            SteamPlayer pl = PlayerTool.getSteamPlayer(s64);
            if (pl == default)
            {
                if (PlayerManager.HasSave(s64, out PlayerSave save))
                    return save.Team;
                else return 0;
            }
            else return pl.GetTeam();
        }
        public static ulong GetTeam(this UCPlayer player) => GetTeam(player.Player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this SteamPlayer player) => GetTeam(player.player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this Player player) => GetTeam(player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this UnturnedPlayer player) => GetTeam(player.Player.quests.groupID.m_SteamID);
        public static ulong GetTeam(this ulong groupID)
        {
            if (!(Data.Gamemode is TeamGamemode)) return groupID;
            if (groupID == TeamManager.Team1ID) return 1;
            else if (groupID == TeamManager.Team2ID) return 2;
            else if (groupID == TeamManager.AdminID) return 3;
            else return 0;
        }
        public static byte GetTeamByte(this SteamPlayer player) => GetTeamByte(player.player.quests.groupID.m_SteamID);
        public static byte GetTeamByte(this Player player) => GetTeamByte(player.quests.groupID.m_SteamID);
        public static byte GetTeamByte(this UnturnedPlayer player) => GetTeamByte(player.Player.quests.groupID.m_SteamID);
        public static byte GetTeamByte(this ulong groupID)
        {
            if (!(Data.Gamemode is TeamGamemode)) return groupID > byte.MaxValue ? byte.MaxValue : (byte)groupID;
            if (groupID == TeamManager.Team1ID) return 1;
            else if (groupID == TeamManager.Team2ID) return 2;
            else if (groupID == TeamManager.AdminID) return 3;
            else return 0;
        }
        public static Vector3 GetBaseSpawn(this SteamPlayer player, out ulong team) => player.player.GetBaseSpawn(out team);
        public static Vector3 GetBaseSpawn(this Player player)
        {
            if (!(Data.Gamemode is ITeams)) return TeamManager.LobbySpawn;
            ulong team = player.GetTeam();
            if (team == 1)
            {
                return TeamManager.Team1Main.Center3D;
            }
            else if (team == 2)
            {
                return TeamManager.Team2Main.Center3D;
            }
            else return TeamManager.LobbySpawn;
        }
        public static Vector3 GetBaseSpawn(this Player player, out ulong team)
        {
            if (!(Data.Gamemode is ITeams))
            {
                team = player.quests.groupID.m_SteamID;
                return TeamManager.LobbySpawn;
            }
            team = player.GetTeam();
            if (team == 1)
            {
                return TeamManager.Team1Main.Center3D;
            }
            else if (team == 2)
            {
                return TeamManager.Team2Main.Center3D;
            }
            else return TeamManager.LobbySpawn;
        }
        public static Vector3 GetBaseSpawn(this ulong playerID, out ulong team)
        {
            team = playerID.GetTeamFromPlayerSteam64ID();
            if (!(Data.Gamemode is ITeams))
            {
                return TeamManager.LobbySpawn;
            }
            return team.GetBaseSpawnFromTeam();
        }
        public static Vector3 GetBaseSpawnFromTeam(this ulong team)
        {
            if (!(Data.Gamemode is ITeams))
            {
                return TeamManager.LobbySpawn;
            }
            if (team == 1) return TeamManager.Team1Main.Center3D;
            else if (team == 2) return TeamManager.Team2Main.Center3D;
            else return TeamManager.LobbySpawn;
        }
        public static float GetBaseAngle(this ulong team)
        {
            if (!(Data.Gamemode is ITeams))
            {
                return TeamManager.LobbySpawnAngle;
            }
            if (team == 1) return TeamManager.Team1SpawnAngle;
            else if (team == 2) return TeamManager.Team2SpawnAngle;
            else return TeamManager.LobbySpawnAngle;
        }
        public static void InvokeSignUpdateFor(SteamPlayer client, InteractableSign sign, string text)
        {
            string newtext = text;
            if (text.StartsWith("sign_"))
                newtext = Translation.TranslateSign(text, UCPlayer.FromSteamPlayer(client), false);
            Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Reliable, client.transportConnection, newtext);
        }
        /// <summary>Runs one player at a time instead of one language at a time. Used for kit signs.</summary>
        public static void InvokeSignUpdateForAll(InteractableSign sign, byte x, byte y, string text)
        {
            if (text == null) return;
            IEnumerator<SteamPlayer> connections = EnumerateClients_Remote(x, y, BarricadeManager.BARRICADE_REGIONS).GetEnumerator();
            while (connections.MoveNext())
            {
                string newtext = text;
                if (text.StartsWith("sign_"))
                    newtext = Translation.TranslateSign(text, UCPlayer.FromSteamPlayer(connections.Current), false);
                Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Reliable, connections.Current.transportConnection, newtext);
            }
            connections.Dispose();
        }
        public static IEnumerable<SteamPlayer> EnumerateClients_Remote(byte x, byte y, byte distance)
        {
            for (int i = 0; i < Provider.clients.Count; i++)
            {
                SteamPlayer client = Provider.clients[i];
                if (client.player != null && Regions.checkArea(x, y, client.player.movement.region_x, client.player.movement.region_y, distance))
                    yield return client;
            }
        }
        public static void InvokeSignUpdateFor(SteamPlayer client, InteractableSign sign, bool changeText = false, string text = "")
        {
            if (text == default || client == default) return;
            string newtext;
            if (!changeText)
                newtext = sign.text;
            else newtext = text;
            if (newtext.StartsWith("sign_"))
                newtext = Translation.TranslateSign(newtext, UCPlayer.FromSteamPlayer(client), false);
            Data.SendChangeText.Invoke(sign.GetNetId(), ENetReliability.Reliable, client.transportConnection, newtext);
        }
        public static float GetTerrainHeightAt2DPoint(Vector2 position, float above = 0) => GetTerrainHeightAt2DPoint(position.x, position.y, above: above);
        public static float GetTerrainHeightAt2DPoint(float x, float z, float defaultY = 0, float above = 0)
        {
            return LevelGround.getHeight(new Vector3(x, 0, z));
            /*
            if (Physics.Raycast(new Vector3(x, Level.HEIGHT, z), new Vector3(0f, -1, 0f), out RaycastHit h, Level.HEIGHT, RayMasks.GROUND | RayMasks.GROUND2))
                return h.point.y + above;
            else return defaultY; */
        }
        public static float GetHeightAt2DPoint(float x, float z, float defaultY = 0, float above = 0)
        {
            if (Physics.Raycast(new Vector3(x, Level.HEIGHT, z), new Vector3(0f, -1, 0f), out RaycastHit h, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
                return h.point.y + above;
            else return defaultY;
        }
        public static string ReplaceCaseInsensitive(this string source, string replaceIf, string replaceWith = "")
        {
            if (source == null) return null;
            if (replaceIf == null || replaceWith == null || source.Length == 0 || replaceIf.Length == 0) return source;
            char[] chars = source.ToCharArray();
            char[] lowerchars = source.ToLower().ToCharArray();
            char[] replaceIfChars = replaceIf.ToLower().ToCharArray();
            StringBuilder buffer = new StringBuilder();
            int replaceIfLength = replaceIfChars.Length;
            StringBuilder newString = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            {
                if (buffer.Length < replaceIfLength)
                {
                    if (lowerchars[i] == replaceIfChars[buffer.Length]) buffer.Append(chars[i]);
                    else
                    {
                        if (buffer.Length != 0)
                            newString.Append(buffer.ToString());
                        buffer.Clear();
                        newString.Append(chars[i]);
                    }
                }
                else
                {
                    if (replaceWith.Length != 0) newString.Append(replaceWith);
                    newString.Append(chars[i]);
                }
            }
            return newString.ToString();
        }
        public static string RemoveMany(this string source, bool caseSensitive, params char[] replacables)
        {
            if (source == null) return null;
            if (replacables.Length == 0) return source;
            char[] chars = source.ToCharArray();
            char[] lowerchars = caseSensitive ? chars : source.ToLower().ToCharArray();
            char[] lowerrepls;
            if (!caseSensitive)
            {
                lowerrepls = new char[replacables.Length];
                for (int i = 0; i < replacables.Length; i++)
                {
                    lowerrepls[i] = char.ToLower(replacables[i]);
                }
            }
            else lowerrepls = replacables;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            {
                bool found = false;
                for (int c = 0; c < lowerrepls.Length; c++)
                {
                    if (lowerrepls[c] == lowerchars[i])
                    {
                        found = true;
                    }
                }
                if (!found) sb.Append(chars[i]);
            }
            return sb.ToString();
        }
        public static void TriggerEffectReliable(ushort ID, CSteamID player, Vector3 Position)
        {
            TriggerEffectParameters p = new TriggerEffectParameters(ID)
            {
                position = Position,
                reliable = true,
                relevantPlayerID = player
            };
            EffectManager.triggerEffect(p);
        }
        public static bool SavePhotoToDisk(string path, Texture2D texture)
        {
            byte[] data = texture.EncodeToPNG();
            try
            {
                FileStream stream = File.Create(path);
                stream.Write(data, 0, data.Length);
                stream.Close();
                stream.Dispose();
                return true;
            }
            catch { return false; }
        }
        public static bool TryGetPlaytimeComponent(this Player player, out PlaytimeComponent component)
        {
            component = GetPlaytimeComponent(player, out bool success);
            return success;
        }
        public static bool TryGetPlaytimeComponent(this CSteamID player, out PlaytimeComponent component)
        {
            component = GetPlaytimeComponent(player, out bool success);
            return success;
        }
        public static bool TryGetPlaytimeComponent(this ulong player, out PlaytimeComponent component)
        {
            component = GetPlaytimeComponent(player, out bool success);
            return success;
        }
        public static PlaytimeComponent GetPlaytimeComponent(this Player player, out bool success)
        {
            if (Data.PlaytimeComponents.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID))
            {
                success = Data.PlaytimeComponents[player.channel.owner.playerID.steamID.m_SteamID] != null;
                return Data.PlaytimeComponents[player.channel.owner.playerID.steamID.m_SteamID];
            }
            else if (player == null || player.transform == null)
            {
                success = false;
                return null;
            }
            else if (player.transform.TryGetComponent(out PlaytimeComponent playtimeObj))
            {
                success = true;
                return playtimeObj;
            }
            else
            {
                success = false;
                return null;
            }
        }
        public static PlaytimeComponent GetPlaytimeComponent(this CSteamID player, out bool success)
        {
            if (Data.PlaytimeComponents.ContainsKey(player.m_SteamID))
            {
                success = Data.PlaytimeComponents[player.m_SteamID] != null;
                return Data.PlaytimeComponents[player.m_SteamID];
            }
            else if (player == default || player == CSteamID.Nil)
            {
                success = false;
                return null;
            }
            else
            {
                Player p = PlayerTool.getPlayer(player);
                if (p == null)
                {
                    success = false;
                    return null;
                }
                if (p.transform.TryGetComponent(out PlaytimeComponent playtimeObj))
                {
                    success = true;
                    return playtimeObj;
                }
                else
                {
                    success = false;
                    return null;
                }
            }
        }
        public static PlaytimeComponent GetPlaytimeComponent(this ulong player, out bool success)
        {
            if (player == 0)
            {
                success = false;
                return default;
            }
            if (Data.PlaytimeComponents.ContainsKey(player))
            {
                success = Data.PlaytimeComponents[player] != null;
                return Data.PlaytimeComponents[player];
            }
            else
            {
                SteamPlayer p = PlayerTool.getSteamPlayer(player);
                if (p == default || p.player == default)
                {
                    success = false;
                    return null;
                }
                if (p.player.transform.TryGetComponent(out PlaytimeComponent playtimeObj))
                {
                    success = true;
                    return playtimeObj;
                }
                else
                {
                    success = false;
                    return null;
                }
            }
        }
        public static float GetCurrentPlaytime(this Player player)
        {
            if (player.TryGetPlaytimeComponent(out PlaytimeComponent playtimeObj))
                return playtimeObj.CurrentTimeSeconds;
            else return 0f;
        }
        public static FPlayerName GetPlayerOriginalNames(UCPlayer player) => GetPlayerOriginalNames(player.Player);
        public static FPlayerName GetPlayerOriginalNames(SteamPlayer player) => GetPlayerOriginalNames(player.player);
        public static FPlayerName GetPlayerOriginalNames(UnturnedPlayer player) => GetPlayerOriginalNames(player.Player);
        public static FPlayerName GetPlayerOriginalNames(Player player)
        {
            if (Data.OriginalNames.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID))
                return Data.OriginalNames[player.channel.owner.playerID.steamID.m_SteamID];
            else return new FPlayerName(player);
        }
        public static FPlayerName GetPlayerOriginalNames(ulong player)
        {
            if (Data.OriginalNames.TryGetValue(player, out FPlayerName names))
                return names;
            else
            {
                SteamPlayer pl = PlayerTool.getSteamPlayer(player);
                if (pl == default)
                    return Data.DatabaseManager.GetUsernames(player);
                else return new FPlayerName()
                {
                    CharacterName = pl.playerID.characterName,
                    NickName = pl.playerID.nickName,
                    PlayerName = pl.playerID.playerName,
                    Steam64 = player
                };
            }
        }
        public static async Task<FPlayerName> GetPlayerOriginalNamesAsync(ulong player)
        {
            if (Data.OriginalNames.TryGetValue(player, out FPlayerName names))
                return names;
            else
            {
                SteamPlayer pl = PlayerTool.getSteamPlayer(player);
                if (pl == default)
                    return await Data.DatabaseManager.GetUsernamesAsync(player);
                else return new FPlayerName()
                {
                    CharacterName = pl.playerID.characterName,
                    NickName = pl.playerID.nickName,
                    PlayerName = pl.playerID.playerName,
                    Steam64 = player
                };
            }
        }
        public static bool IsInMain(this Player player)
        {
            if (!(Data.Gamemode is TeamGamemode)) return false;
            ulong team = player.GetTeam();
            if (team == 1) return TeamManager.Team1Main.IsInside(player.transform.position);
            else if (team == 2) return TeamManager.Team2Main.IsInside(player.transform.position);
            else return false;
        }
        public static bool IsInMain(Vector3 point)
        {
            if (!(Data.Gamemode is TeamGamemode)) return false;
            return TeamManager.Team1Main.IsInside(point) || TeamManager.Team2Main.IsInside(point);
        }
        public static bool IsOnFlag(this Player player) => player != null && Data.Is(out IFlagRotation fg) && fg.OnFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID);
        public static bool IsOnFlag(this Player player, out Flag flag)
        {
            if (player != null && Data.Is(out IFlagRotation fg))
            {
                if (fg.OnFlag == null)
                {
                    L.LogError("onflag null");
                    if (fg.Rotation == null) L.LogError("rot null");
                    flag = null;
                    return false;
                }
                else if (fg.Rotation == null)
                {
                    L.LogError("rot null");
                    if (fg.OnFlag == null) L.LogError("onflag null");
                    flag = null;
                    return false;
                }
                if (fg.OnFlag.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out int id))
                {
                    flag = fg.Rotation.Find(x => x.ID == id);
                    return flag != null;
                }
            }
            flag = null;
            return false;
        }
        public static string Colorize(this string inner, string colorhex) => $"<color=#{colorhex}>{inner}</color>";
        public static string ColorizeName(string innerText, ulong team)
        {
            if (!(Data.Gamemode is TeamGamemode)) return innerText;
            if (team == TeamManager.ZOMBIE_TEAM_ID) return $"<color=#{UCWarfare.GetColorHex("death_zombie_name_color")}>{innerText}</color>";
            else if (team == TeamManager.Team1ID) return $"<color=#{TeamManager.Team1ColorHex}>{innerText}</color>";
            else if (team == TeamManager.Team2ID) return $"<color=#{TeamManager.Team2ColorHex}>{innerText}</color>";
            else if (team == TeamManager.AdminID) return $"<color=#{TeamManager.AdminColorHex}>{innerText}</color>";
            else return $"<color=#{TeamManager.NeutralColorHex}>{innerText}</color>";
        }
        public static void CheckDir(string path, out bool success, bool unloadIfFail = false)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                    success = true;
                    L.Log("Created directory: \"" + path + "\".", ConsoleColor.Magenta);
                }
                catch (Exception ex)
                {
                    L.LogError("Unable to create data directory " + path + ". Check permissions: " + ex.Message);
                    success = false;
                    if (unloadIfFail)
                        UCWarfare.I?.UnloadPlugin();
                }
            }
            else success = true;
        }
        public static void SendSteamURL(this SteamPlayer player, string message, ulong SteamID) => player.SendURL(message, $"https://steamcommunity.com/profiles/{SteamID}/");
        public static void SendURL(this SteamPlayer player, string message, string url)
        {
            if (player == default || url == default) return;
            player.player.sendBrowserRequest(message, url);
        }
        public static string GetLayer(Vector3 direction, Vector3 origin, int Raymask)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, 8192f, Raymask))
            {
                if (hit.transform != null)
                    return hit.transform.gameObject.layer.ToString();
                else return "nullHitNoTransform";
            }
            else return "nullNoHit";
        }
        public static bool CanStandAtLocation(Vector3 source)
        {
            return Physics.OverlapCapsuleNonAlloc(source + new Vector3(0.0f, PlayerStance.RADIUS + 0.01f, 0.0f), source +
                new Vector3(0.0f, PlayerMovement.HEIGHT_STAND + 0.5f - PlayerStance.RADIUS, 0.0f), PlayerStance.RADIUS, PlayerStance.checkColliders,
                RayMasks.BLOCK_STANCE, QueryTriggerInteraction.Ignore) == 0;
        }
        public static string GetClosestLocation(Vector3 point)
        {
            string closest = null;
            float smallest = -1f;
            for (int i = 0; i < LevelNodes.nodes.Count; i++)
            {
                if (LevelNodes.nodes[i] is LocationNode node)
                {
                    float amt = (point - node.point).sqrMagnitude;
                    if (smallest == -1 || amt < smallest)
                    {
                        closest = node.name;
                        smallest = amt;
                    }
                }
            }
            return closest;
        }
        public static void NetInvoke(this NetCall call) =>
            call.Invoke(Data.NetClient.connection);
        public static void NetInvoke<T>(this NetCallRaw<T> call, T arg) =>
            call.Invoke(Data.NetClient.connection, arg);
        public static void NetInvoke<T1, T2>(this NetCallRaw<T1, T2> call, T1 arg1, T2 arg2) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2);
        public static void NetInvoke<T1, T2, T3>(this NetCallRaw<T1, T2, T3> call, T1 arg1, T2 arg2, T3 arg3) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3);
        public static void NetInvoke<T1, T2, T3, T4>(this NetCallRaw<T1, T2, T3, T4> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4);
        public static void NetInvoke<T1>(this NetCall<T1> call, T1 arg1) =>
            call.Invoke(Data.NetClient.connection, arg1);
        public static void NetInvoke<T1, T2>(this NetCall<T1, T2> call, T1 arg1, T2 arg2) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2);
        public static void NetInvoke<T1, T2, T3>(this NetCall<T1, T2, T3> call, T1 arg1, T2 arg2, T3 arg3) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3);
        public static void NetInvoke<T1, T2, T3, T4>(this NetCall<T1, T2, T3, T4> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4);
        public static void NetInvoke<T1, T2, T3, T4, T5>(this NetCall<T1, T2, T3, T4, T5> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4, arg5);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6>(this NetCall<T1, T2, T3, T4, T5, T6> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4, arg5, arg6);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7>(this NetCall<T1, T2, T3, T4, T5, T6, T7> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8, T9> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        public static void NetInvoke<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this NetCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> call, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10) =>
            call.Invoke(Data.NetClient.connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        public static bool FilterName(string original, out string final)
        {
            if (UCWarfare.Config.DisableNameFilter || UCWarfare.Config.MinAlphanumericStringLength <= 0)
            {
                final = original;
                return false;
            }
            IEnumerator<char> charenum = original.GetEnumerator();
            int counter = 0;
            int alphanumcount = 0;
            while (charenum.MoveNext())
            {
                counter++;
                char ch = charenum.Current;
                int c = ch;
                if (c > 31 && c < 127)
                {
                    if (alphanumcount - 1 >= UCWarfare.Config.MinAlphanumericStringLength)
                    {
                        final = original;
                        charenum.Dispose();
                        return false;
                    }
                    else
                    {
                        alphanumcount++;
                    }
                }
                else
                {
                    alphanumcount = 0;
                }
            }
            charenum.Dispose();
            final = original;
            return alphanumcount != original.Length;
        }
        public static DateTime FromUnityTime(this float realtimeSinceStartup) => 
            DateTime.Now - TimeSpan.FromSeconds(Time.realtimeSinceStartup) + TimeSpan.FromSeconds(realtimeSinceStartup);

        /// <summary>
        /// Finds the 2D distance between two Vector3's x and z components.
        /// </summary>
        public static float SqrDistance2D(Vector3 a, Vector3 b) => Mathf.Pow(b.x - a.x, 2) + Mathf.Pow(b.z - a.z, 2);
    }
}