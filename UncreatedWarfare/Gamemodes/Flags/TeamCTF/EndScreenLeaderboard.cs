﻿using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF
{
    public class EndScreenLeaderboard : MonoBehaviour
    {
        public const float SecondsEndGameLength = 30f;
        public const short UiIdentifier = 10000;
        public event Networking.EmptyTaskDelegate OnLeaderboardExpired;
        private float secondsLeft;
        public bool ShuttingDown = false;
        public string ShuttingDownMessage = string.Empty;
        public ulong ShuttingDownPlayer = 0;
        const float updateTimeFrequency = 1f;
        public WarStatsTracker warstats;
        public ulong winner;
        public CancellationTokenSource CancelToken = new CancellationTokenSource();
        List<KeyValuePair<Player, char>> topsquadplayers;
        List<PlayerCurrentGameStats> statsT1 = new List<PlayerCurrentGameStats>();
        List<PlayerCurrentGameStats> statsT2 = new List<PlayerCurrentGameStats>();
        Players.FPlayerName longestShotTaker;
        ulong longestShotTakerTeam;
        float longestShot;
        bool longestShotTaken;
        string squadname;
        ulong squadteam;
        public async Task EndGame(string progresschars)
        {
            await GetValues();
            SendEndScreen(winner, progresschars);
            secondsLeft = SecondsEndGameLength;
            _ = StartUpdatingTimer(CancelToken.Token, progresschars).ConfigureAwait(false);
            await Task.Yield();
        }
        private async Task StartUpdatingTimer(CancellationToken token, string progresschars)
        {
            while (secondsLeft > 0 && !token.IsCancellationRequested)
            {
                secondsLeft -= updateTimeFrequency;
                await Task.Delay(Mathf.RoundToInt(updateTimeFrequency * 1000));
                UpdateLeaderboard(secondsLeft, progresschars);
            }
            EffectManager.ClearEffectByID_AllPlayers(UCWarfare.Config.EndScreenUI);
            foreach (SteamPlayer player in Provider.clients)
            {
                player.player.setAllPluginWidgetFlags(EPluginWidgetFlags.Default);
                player.player.movement.sendPluginSpeedMultiplier(1f);
                player.player.movement.sendPluginJumpMultiplier(1f);
            }
            if (ShuttingDown)
            {
                await Networking.Client.SendShuttingDown(ShuttingDownPlayer, ShuttingDownMessage);
                Provider.shutdown(0, ShuttingDownMessage);
            }
            else if (OnLeaderboardExpired != null)
            {
                await OnLeaderboardExpired.Invoke();
            }
        }
        public async Task GetValues()
        {
            // topsquadplayers = warstats.GetTopSquad(out squadname, out squadteam, winner);
            warstats.GetTopStats(14, out statsT1, out statsT2);

            longestShotTaken = !warstats.LongestShot.Equals(default(KeyValuePair<ulong, float>));
            if (longestShotTaken)
            {
                SteamPlayer longestshottaker = PlayerTool.getSteamPlayer(warstats.LongestShot.Key);
                if (longestshottaker == null)
                {
                    longestShotTaker = await Data.DatabaseManager.GetUsernames(warstats.LongestShot.Key);
                    if (PlayerManager.HasSave(warstats.LongestShot.Key, out PlayerSave save))
                        longestShotTakerTeam = save.Team;
                    else longestShotTakerTeam = 0;
                }
                else
                {
                    longestShotTaker = F.GetPlayerOriginalNames(longestshottaker);
                    longestShotTakerTeam = longestshottaker.GetTeam();
                }
                longestShot = warstats.LongestShot.Value;
            } else
            {
                longestShot = 0;
                longestShotTaker = Players.FPlayerName.Nil;
                longestShotTakerTeam = 0;
            }
        }
        public void SendScreenToPlayer(SteamPlayer player, string progresschars) => SendScreenToPlayer(winner, player, TeamManager.GetTeamHexColor(winner), progresschars);
        public void SendScreenToPlayer(ulong winner, SteamPlayer player, string teamcolor, string progresschars)
        {
            try
            {
                UCPlayer ucplayer = UCPlayer.FromSteamPlayer(player);

                CTFUI.ClearListUI(player.transportConnection, (Data.Gamemode as TeamCTF).Config.FlagUICount);
                player.player.movement.sendPluginSpeedMultiplier(0f);
                player.player.life.serverModifyHealth(100);
                player.player.life.serverModifyFood(100);
                player.player.life.serverModifyWater(100);
                player.player.life.serverModifyVirus(100);
                player.player.life.serverModifyStamina(100);
                player.player.movement.sendPluginJumpMultiplier(0f);
                Data.ReviveManager.RevivePlayer(player.player);

                if (!player.player.life.isDead)
                    player.player.teleportToLocation(F.GetBaseSpawn(player, out ulong playerteam), F.GetBaseAngle(playerteam));
                else
                    player.player.life.ReceiveRespawnRequest(false);

                // resupply the kit.
                if (ucplayer != null && ucplayer.KitName != null && ucplayer.KitName != "")
                {
                    if (KitManager.KitExists(ucplayer.KitName, out Kit kit))
                        KitManager.ResupplyKit(ucplayer, kit);
                }
                player.player.setAllPluginWidgetFlags(EPluginWidgetFlags.None);
                KeyValuePair<ulong, PlayerCurrentGameStats> statsvalue = warstats.playerstats.FirstOrDefault(x => x.Key == player.playerID.steamID.m_SteamID);
                PlayerCurrentGameStats stats;
                if (statsvalue.Equals(default(KeyValuePair<ulong, PlayerCurrentGameStats>)))
                    stats = new PlayerCurrentGameStats(player.player);
                else stats = statsvalue.Value;
                Players.FPlayerName originalNames = F.GetPlayerOriginalNames(player);
                ITransportConnection channel = player.transportConnection;
                EffectManager.sendUIEffect(UCWarfare.Config.EndScreenUI, UiIdentifier, channel, true);
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TitleWinner", F.Translate("winner", player, TeamManager.TranslateName(winner, player), teamcolor));
                if (ShuttingDown)
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "NextGameStartsIn", F.Translate("next_game_start_label_shutting_down", player, ShuttingDownMessage));
                else
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "NextGameStartsIn", F.Translate("next_game_start_label", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "NextGameSeconds", F.ObjectTranslate("next_game_starting_format", player.playerID.steamID.m_SteamID, TimeSpan.FromSeconds(SecondsEndGameLength)));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "NextGameCircleForeground", progresschars[CTFUI.FromMax(0, Mathf.RoundToInt(SecondsEndGameLength), progresschars)].ToString());
                
                
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "PlayerGameStatsHeader", F.ObjectTranslate("player_name_header", player.playerID.steamID.m_SteamID, originalNames.CharacterName, F.GetTeamColorHex(player), (float)stats.onlineCount / warstats.gamepercentagecounter * 100f));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "WarHeader", F.Translate("war_name_header", player,
                    TeamManager.TranslateName(1, player.playerID.steamID.m_SteamID), TeamManager.Team1ColorHex,
                    TeamManager.TranslateName(2, player.playerID.steamID.m_SteamID), TeamManager.Team2ColorHex));
                
                // LEADERBOARD

                for (int i = 0; i < Math.Min(15, statsT1.Count); i++)
                {
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "1N" + i, i == 0 ? TeamManager.TranslateName(1, player, true).ToUpper() : player.playerID.nickName);
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "1K" + i, statsT1[i].kills.ToString());
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "1D" + i, statsT1[i].deaths.ToString());
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "1X" + i, statsT1[i].xpgained.ToString());
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "1F" + i, statsT1[i].officerpointsgained.ToString());
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "1C" + i, statsT1[i].captures.ToString());
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "1T" + i, statsT1[i].damagedone.ToString());
                }

                for (int i = 0; i < Math.Min(15, statsT2.Count); i++)
                {
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "2N" + i, i == 0 ? TeamManager.TranslateName(2, player, true).ToUpper() : player.playerID.nickName);
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "2K" + i, statsT2[i].kills.ToString());
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "2D" + i, statsT2[i].deaths.ToString());
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "2X" + i, statsT2[i].xpgained.ToString());
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "2F" + i, statsT2[i].officerpointsgained.ToString());
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "2C" + i, statsT2[i].captures.ToString());
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, "2T" + i, statsT2[i].damagedone.ToString());
                }

                //UCPlayer topOfficer = PlayerManager.OnlinePlayers.OrderByDescending(x => x.cachedOfp).FirstOrDefault();
                //if (topOfficer.cachedOfp == 0) topOfficer = default;
                /*
                 *  STATS
                 */
                // titles
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblKills", F.Translate("lblKills", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblDeaths", F.Translate("lblDeaths", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblKDR", F.Translate("lblKDR", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblKillsOnPoint", F.Translate("lblKillsOnPoint", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblTimeDeployed", F.Translate("lblTimeDeployed", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblXpGained", F.Translate("lblXpGained", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblTimeOnPoint", F.Translate("lblTimeOnPoint", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblCaptures", F.Translate("lblCaptures", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblTimeInVehicle", F.Translate("lblTimeInVehicle", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblTeamkills", F.Translate("lblTeamkills", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblEnemyFOBsDestroyed", F.Translate("lblFOBsDestroyed", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblOfficerPointsGainedValue", F.Translate("lblOfficerPointsGained", player));

                string defaultColor = UCWarfare.GetColorHex("default");
                // values
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "KillsValue", F.ObjectTranslate("stats_player_value", player.playerID.steamID.m_SteamID, stats.kills, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "DeathsValue", F.ObjectTranslate("stats_player_value", player.playerID.steamID.m_SteamID, stats.deaths, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "KDRValue", F.ObjectTranslate("stats_player_float_value", player.playerID.steamID.m_SteamID, stats.KDR, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "KillsOnPointValue", F.ObjectTranslate("stats_player_value", player.playerID.steamID.m_SteamID, stats.killsonpoint, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TimeDeployedValue", F.ObjectTranslate("stats_player_time_value", player.playerID.steamID.m_SteamID, stats.TimeDeployed, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "XPGainedValue", F.ObjectTranslate("stats_player_value", player.playerID.steamID.m_SteamID, stats.xpgained, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TimeOnPointValue", F.ObjectTranslate("stats_player_time_value", player.playerID.steamID.m_SteamID, stats.TimeOnPoint, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "CapturesValue", F.ObjectTranslate("stats_player_value", player.playerID.steamID.m_SteamID, stats.captures, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TimeInVehicleValue", F.ObjectTranslate("stats_player_value", player.playerID.steamID.m_SteamID, stats.damagedone, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TeamkillsValue", F.ObjectTranslate("stats_player_value", player.playerID.steamID.m_SteamID, stats.teamkills, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "EnemyFOBsDestroyedValue", F.ObjectTranslate("stats_player_value", player.playerID.steamID.m_SteamID, stats.fobsdestroyed, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "OfficerPointsGainedValue", F.ObjectTranslate("stats_player_value", player.playerID.steamID.m_SteamID, stats.officerpointsgained, defaultColor));

                /*
                 *  WAR
                 */
                // titles
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblDuration", F.Translate("lblDuration", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblCasualtiesT1", F.Translate("lblDeathsT1", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblCasualtiesT2", F.Translate("lblDeathsT2", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblOwnerChangedCount", F.Translate("lblOwnerChangeCount", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblAveragePlayerCountT1", F.Translate("lblAveragePlayerCountT1", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblAveragePlayerCountT2", F.Translate("lblAveragePlayerCountT2", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblFOBsPlacedT1", F.Translate("lblFOBsPlacedT1", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblFOBsPlacedT2", F.Translate("lblFOBsPlacedT2", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblFOBsDestroyedT1", F.Translate("lblFOBsDestroyedT1", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblFOBsDestroyedT2", F.Translate("lblFOBsDestroyedT2", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblTeamkillingCasualties", F.Translate("lblTeamkillingCasualties", player));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblTopRankingOfficer", F.Translate("lblTopRankingOfficer", player));

                // values
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "DurationValue", F.ObjectTranslate("stats_war_time_value", player.playerID.steamID.m_SteamID, warstats.Duration, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "CasualtiesValueT1", F.ObjectTranslate("stats_war_value", player.playerID.steamID.m_SteamID, warstats.casualtiesT1, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "CasualtiesValueT2", F.ObjectTranslate("stats_war_value", player.playerID.steamID.m_SteamID, warstats.casualtiesT2, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "FlagCapturesValue", F.ObjectTranslate("stats_war_value", player.playerID.steamID.m_SteamID, warstats.totalFlagOwnerChanges, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "AveragePlayerCountsT1Value", F.ObjectTranslate("stats_war_float_value", player.playerID.steamID.m_SteamID, warstats.averageArmySizeT1, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "AveragePlayerCountsT2Value", F.ObjectTranslate("stats_war_float_value", player.playerID.steamID.m_SteamID, warstats.averageArmySizeT2, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "FOBsPlacedT1Value", F.ObjectTranslate("stats_war_value", player.playerID.steamID.m_SteamID, warstats.fobsPlacedT1, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "FOBsPlacedT2Value", F.ObjectTranslate("stats_war_value", player.playerID.steamID.m_SteamID, warstats.fobsPlacedT2, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "FOBsDestroyedT1Value", F.ObjectTranslate("stats_war_value", player.playerID.steamID.m_SteamID, warstats.fobsDestroyedT1, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "FOBsDestroyedT2Value", F.ObjectTranslate("stats_war_value", player.playerID.steamID.m_SteamID, warstats.fobsDestroyedT2, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TeamkillingCasualtiesValue", F.ObjectTranslate("stats_war_value", player.playerID.steamID.m_SteamID, warstats.teamkills, defaultColor));
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TopRankingOfficerValue", longestShotTaken ? 
                    F.Translate("longest_shot_format", player.playerID.steamID.m_SteamID, longestShot.ToString("N1"), 
                    F.ColorizeName(longestShotTaker.CharacterName, longestShotTakerTeam)) : WarStatsTracker.NO_PLAYER_NAME_PLACEHOLDER);
            }
            catch (Exception ex)
            {
                F.LogError($"Error sending end screen to {F.GetPlayerOriginalNames(player).PlayerName} ( {player.playerID.steamID.m_SteamID} ).");
                F.LogError(ex);
            }
        }
        public void SendEndScreen(ulong winner, string progresschars)
        {
            string teamcolor = TeamManager.GetTeamHexColor(winner);
            for (int players = 0; players < Provider.clients.Count; players++)
            {
                SendScreenToPlayer(winner, Provider.clients[players], teamcolor, progresschars);
            }
        }
        public void UpdateLeaderboard(float newTime, string progresschars)
        {
            foreach(SteamPlayer player in Provider.clients)
            {
                EffectManager.sendUIEffectText(UiIdentifier, player.transportConnection, true, "NextGameSeconds", F.ObjectTranslate("next_game_starting_format", 
                    player.playerID.steamID.m_SteamID, TimeSpan.FromSeconds(newTime)));
                int time = Mathf.RoundToInt(SecondsEndGameLength);
                EffectManager.sendUIEffectText(UiIdentifier, player.transportConnection, true, "NextGameCircleForeground",
                    progresschars[CTFUI.FromMax(Mathf.RoundToInt(time - newTime), time, progresschars)].ToString());
            }
        }
    }
    public class PlayerCurrentGameStats
    {
        public Player player;
        public readonly ulong id;
        public int kills;
        public int deaths;
        public float KDR { get => deaths == 0 ? kills : (float)kills / deaths; }
        public int killsonpoint;
        public int xpgained;
        public int officerpointsgained;
        public TimeSpan TimeDeployed { get => TimeSpan.FromSeconds(timeDeployedCounter); }
        private float timeDeployedCounter;
        public TimeSpan TimeOnPoint { get => TimeSpan.FromSeconds(timeOnPointCounter); }
        private float timeOnPointCounter;
        public int captures;
        public int teamkills;
        public int fobsdestroyed;
        public int fobsplaced;
        //public TimeSpan TimeDriving { get => TimeSpan.FromSeconds(timeDrivingCounter); }
        //private float timeDrivingCounter;
        public int damagedone;
        public int onlineCount;
        public int revives;
        public PlayerCurrentGameStats(Player player)
        {
            this.player = player;
            this.id = player.channel.owner.playerID.steamID.m_SteamID;
            Reset();
        }
        public PlayerCurrentGameStats()
        {
            Reset();
        }
        public void Reset()
        {
            this.kills = 0;
            this.deaths = 0;
            this.killsonpoint = 0;
            this.timeDeployedCounter = 0;
            this.timeOnPointCounter = 0;
            this.captures = 0;
            this.teamkills = 0;
            this.fobsdestroyed = 0;
            this.fobsplaced = 0;
            this.damagedone = 0;
            this.xpgained = 0;
            this.officerpointsgained = 0;
            this.onlineCount = 0;
            this.revives = 0;
        }
        public void AddKill()
        {
            kills++;
            if (player != default && Data.Gamemode is FlagGamemode fg && fg.Rotation.Exists(x => x.ZoneData.IsInside(player.transform.position))) killsonpoint++;
        }
        public void AddDeath() => deaths++;
        public void AddTeamkill() => teamkills++;
        public void AddXP(int amount) => xpgained += amount;
        public void AddOfficerPoints(int amount) => officerpointsgained += amount;
        public void AddToTimeDeployed(float amount) => timeDeployedCounter += amount;
        public void AddToTimeOnPoint(float amount) => timeOnPointCounter += amount;
        //public void AddToTimeDriving(float amount) => timeDrivingCounter += amount;
        public void CheckGame()
        {
            if (player != null)
                onlineCount++;
        }
        public override string ToString()
            =>
            $"Player: {id} ({(player == null ? "offline" : player.channel.owner.playerID.playerName)})\n" +
            $"Kills: {kills}\nDeaths: {deaths}\nKills on point: {killsonpoint}\nTime Deployed: {TimeDeployed:g}\n" +
            $"Time On Point: {TimeOnPoint:g}\nCaptures: {captures}\nTeamkills: {teamkills}\nFobs Destroyed: {fobsdestroyed}\n" +
            $"Fobs Placed: {fobsplaced}\nDamage Done: {damagedone}\nXP Gained: {xpgained}\nOfficer Pts Gained: {officerpointsgained}\n" +
            $"OnlineTime:{(float)onlineCount / ((TeamCTF)Data.Gamemode).GameStats.gamepercentagecounter * 100}%.";
    }
    public class WarStatsTracker : MonoBehaviour
    {
        public TimeSpan Duration { get => TimeSpan.FromSeconds(durationCounter); }
        public Dictionary<ulong, PlayerCurrentGameStats> playerstats;
        private float durationCounter = 0; // works
        public int casualtiesT1; // works
        public int casualtiesT2; // works
        public int totalFlagOwnerChanges; // works
        public float averageArmySizeT1; // works
        public float averageArmySizeT2; // works
        public int fobsPlacedT1; // works
        public int fobsPlacedT2; // works
        public int fobsDestroyedT1; // works
        public int fobsDestroyedT2; // works
        public int teamkills; // works
        internal int gamepercentagecounter;
        public Coroutine update;
        public KeyValuePair<ulong, float> LongestShot;
        public void Update()
        {
            durationCounter += Time.deltaTime;
        }
        public void AddPlayer(Player player)
        {
            if (!playerstats.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out PlayerCurrentGameStats s))
            {
                s = new PlayerCurrentGameStats(player);
                playerstats.Add(player.channel.owner.playerID.steamID.m_SteamID, s);
                if (F.TryGetPlaytimeComponent(player, out Components.PlaytimeComponent c))
                    c.stats = s;
            }
            else
            {
                s.player = player;
                if (F.TryGetPlaytimeComponent(player, out Components.PlaytimeComponent c))
                    c.stats = s;
            }
            F.Log(player.name + " added to playerstats, " + playerstats.Count + " trackers");
        }
        public void Start() => Reset(true);
        public void Reset(bool start = false)
        {
            if (!start)
                StopCounting();
            if (playerstats == null)
                playerstats = new Dictionary<ulong, PlayerCurrentGameStats>();
            for (int i = 0; i < Provider.clients.Count; i++)
            {
                if (playerstats.TryGetValue(Provider.clients[i].playerID.steamID.m_SteamID, out PlayerCurrentGameStats p))
                {
                    p.player = Provider.clients[i].player;
                    p.Reset();
                }
                else
                {
                    PlayerCurrentGameStats s = new PlayerCurrentGameStats(Provider.clients[i].player);
                    playerstats.Add(Provider.clients[i].playerID.steamID.m_SteamID, s);
                    if (Provider.clients[i].player.TryGetPlaytimeComponent(out Components.PlaytimeComponent pt))
                        pt.stats = s;
                }
            }
            foreach (KeyValuePair<ulong, PlayerCurrentGameStats> p in playerstats.ToList())
            {
                SteamPlayer player = PlayerTool.getSteamPlayer(p.Key);
                if (player == null) playerstats.Remove(p.Key);
            }
            durationCounter = 0;
            casualtiesT1 = 0;
            casualtiesT2 = 0;
            totalFlagOwnerChanges = 0;
            averageArmySizeT1 = 0;
            averageArmySizeT2 = 0;
            gamepercentagecounter = 0;
            fobsPlacedT1 = 0;
            fobsPlacedT2 = 0;
            fobsDestroyedT1 = 0;
            fobsDestroyedT2 = 0;
            teamkills = 0;
            update = StartCoroutine(CompileAverages());
            LongestShot = default;
        }
        public void StopCounting()
        {
            if (update == null) return;
            StopCoroutine(update);
        }
        private void CompileArmyAverageT1(int newcount)
        {
            float oldArmySize = averageArmySizeT1 * gamepercentagecounter;
            averageArmySizeT1 = gamepercentagecounter == 0 ? (oldArmySize + newcount) : ((oldArmySize + newcount) / gamepercentagecounter);
        }
        private void CompileArmyAverageT2(int newcount)
        {
            float oldArmySize = averageArmySizeT2 * gamepercentagecounter;
            averageArmySizeT2 = gamepercentagecounter == 0 ? (oldArmySize + newcount) : ((oldArmySize + newcount) / gamepercentagecounter);
        }
        private IEnumerator<WaitForSeconds> CompileAverages()
        {
            while (true)
            {
                // checks for how many players are outside of main
                DateTime dt = DateTime.Now;
                CompileArmyAverageT1(Provider.clients.Count(x => x.GetTeam() == 1 && x.player.transform != null && !TeamManager.Team1Main.IsInside(x.player.transform.position)));
                CompileArmyAverageT2(Provider.clients.Count(x => x.GetTeam() == 2 && x.player.transform != null && !TeamManager.Team2Main.IsInside(x.player.transform.position)));
                foreach (PlayerCurrentGameStats s in playerstats.Values)
                    s.CheckGame();
                gamepercentagecounter++;
                yield return new WaitForSeconds(10f);
            }
        }
        public bool TryGetPlayer(ulong id, out PlayerCurrentGameStats stats)
        {
            if (!(playerstats == null) && playerstats.ContainsKey(id))
            {
                stats = playerstats[id];
                return true;
            }
            else
            {
                stats = null;
                return false;
            }
        }
        public const string NO_PLAYER_NAME_PLACEHOLDER = "---";
        public const string NO_PLAYER_VALUE_PLACEHOLDER = "--";
        public void GetTopStats(int count, out List<PlayerCurrentGameStats> statsT1, out List<PlayerCurrentGameStats> statsT2)
        {
            List<PlayerCurrentGameStats> stats = playerstats.Values.ToList();

            stats.RemoveAll(p =>
            {
                if (p == null) return true;
                if (p.player == null)
                {
                    SteamPlayer player = PlayerTool.getSteamPlayer(p.id);
                    if (player == default || player.player == default) return true;
                    else p.player = player.player;
                    return false;
                }
                else return false;
            });

            var totalT1 = new PlayerCurrentGameStats();
            var totalT2 = new PlayerCurrentGameStats();
            for (int i = 0; i < playerstats.Values.Count; i++)
            {
                var stat = playerstats.Values.ElementAt(i);

                if (stat.id.GetTeamFromPlayerSteam64ID() == 1)
                {
                    totalT1.kills += stat.kills;
                    totalT1.deaths += stat.deaths;
                    totalT1.xpgained += stat.xpgained;
                    totalT1.officerpointsgained += stat.officerpointsgained;
                    totalT1.captures += stat.captures;
                    totalT1.damagedone += stat.damagedone;
                }
                else if (stat.id.GetTeamFromPlayerSteam64ID() == 1)
                {
                    totalT2.kills += stat.kills;
                    totalT2.deaths += stat.deaths;
                    totalT2.xpgained += stat.xpgained;
                    totalT2.officerpointsgained += stat.officerpointsgained;
                    totalT2.captures += stat.captures;
                    totalT2.damagedone += stat.damagedone;
                }
            }

            stats.Sort((PlayerCurrentGameStats a, PlayerCurrentGameStats b) => b.xpgained.CompareTo(a.xpgained));

            statsT1 = stats.Where(p => p.player.GetTeam() == 1).ToList();
            statsT2 = stats.Where(p => p.player.GetTeam() == 2).ToList();
            statsT1.Take(count);
            statsT2.Take(count);
            statsT1.Insert(0, totalT1);
            statsT2.Insert(0, totalT2);
        }
        public List<KeyValuePair<Player, char>> GetTopSquad(out string squadname, out ulong squadteam, ulong winner)
        {
            List<Squad> squads = SquadManager.Squads.Where(x => x.Team == winner).ToList();
            if (squads.Count == 0)
            {
                squadname = NO_PLAYER_NAME_PLACEHOLDER;
                squadteam = 0;
                return new List<KeyValuePair<Player, char>>();
            }
            squads.Sort((a, b) =>
            {
                int totalxpgaina = 0;
                for (int i = 0; i < a.Members.Count; i++)
                {
                    if (a.Members[i].Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                        totalxpgaina += c.stats.xpgained;
                }
                int totalxpgainb = 0;
                for (int i = 0; i < b.Members.Count; i++)
                {
                    if (b.Members[i].Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                        totalxpgainb += c.stats.xpgained;
                }
                if (totalxpgaina == totalxpgainb)
                {
                    int totalopgaina = 0;
                    for (int i = 0; i < a.Members.Count; i++)
                    {
                        if (a.Members[i].Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                            totalopgaina += c.stats.officerpointsgained;
                    }
                    int totalopgainb = 0;
                    for (int i = 0; i < b.Members.Count; i++)
                    {
                        if (b.Members[i].Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                            totalopgainb += c.stats.officerpointsgained;
                    }
                    if (totalxpgaina == totalxpgainb)
                    {
                        int totalkillsa = 0;
                        for (int i = 0; i < a.Members.Count; i++)
                        {
                            if (a.Members[i].Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                                totalkillsa += c.stats.kills;
                        }
                        int totalkillsb = 0;
                        for (int i = 0; i < b.Members.Count; i++)
                        {
                            if (b.Members[i].Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                                totalkillsb += c.stats.kills;
                        }
                        return totalkillsa.CompareTo(totalkillsb);
                    }
                    return totalopgaina.CompareTo(totalopgainb);
                }
                return totalxpgaina.CompareTo(totalxpgainb);
            });
            Squad topsquad = squads[0];
            squadname = topsquad.Name;
            squadteam = topsquad.Team;
            List<UCPlayer> players = topsquad.Members.ToList();
            players.Sort((a, b) =>
            {
                if (topsquad.Leader.Steam64 == a.Steam64) return 1;
                else
                {
                    int axp = 0, bxp = 0;
                    if (a.Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent ca))
                        axp = ca.stats.xpgained;
                    if (b.Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent cb))
                        bxp = cb.stats.xpgained;
                    return axp.CompareTo(bxp);
                }
            });
            List<KeyValuePair<Player, char>> rtn = new List<KeyValuePair<Player, char>>(players.Count > 6 ? 6 : players.Count);
            for (int i = 0; i < (players.Count > 6 ? 6 : players.Count); i++)
            {
                rtn.Add(new KeyValuePair<Player, char>(players[i].Player, players[i].Icon));
            }
            return rtn;
        }
    }
}
