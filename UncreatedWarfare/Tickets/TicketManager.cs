﻿using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.XP;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Tickets
{
    public class TicketManager
    {
        public static Config<TicketData> config;

        public static int Team1Tickets;
        public static int Team2Tickets;

        private static Coroutine ticketloop;

        public TicketManager()
        {
            config = new Config<TicketData>(Data.TicketStorage, "config.json");

            Team1Tickets = config.Data.StartingTickets;
            Team2Tickets = config.Data.StartingTickets;

            VehicleManager.OnVehicleExploded += OnVehicleExploded;
        }
        public static async Task OnPlayerDeath(UCWarfare.DeathEventArgs eventArgs)
        {
            if (TeamManager.IsTeam1(eventArgs.dead))
            {
                await AddTeam1Tickets(-1);
            }
            else if (TeamManager.IsTeam2(eventArgs.dead))
            {
                await AddTeam2Tickets(-1);
            }
        }
        public static async Task OnPlayerSuicide(UCWarfare.SuicideEventArgs eventArgs)
        {
            if (TeamManager.IsTeam1(eventArgs.dead))
            {
                await AddTeam1Tickets(-1);
            }
            else if (TeamManager.IsTeam2(eventArgs.dead))
            {
                await AddTeam2Tickets(-1);
            }
        }
        public static async Task OnEnemyKilled(UCWarfare.KillEventArgs parameters)
        {
            await XPManager.AddXP(parameters.killer, parameters.killer.GetTeam(), UCPlayer.FromPlayer(parameters.killer).NearbyMemberBonus(XPManager.config.Data.EnemyKilledXP, 75), 
                F.Translate("xp_enemy_killed", parameters.killer.channel.owner.playerID.steamID.m_SteamID, F.GetPlayerOriginalNames(parameters.dead).CharacterName));
            //await OfficerManager.AddOfficerPoints(parameters.killer, parameters.killer.GetTeam(), OfficerManager.config.data.MemberEnemyKilledPoints);
        }
        public static async Task OnFriendlyKilled(UCWarfare.KillEventArgs parameters)
        {
            await XPManager.AddXP(parameters.killer, parameters.killer.GetTeam(), XPManager.config.Data.FriendlyKilledXP,
                F.Translate("xp_friendly_killed", parameters.killer.channel.owner.playerID.steamID.m_SteamID, F.GetPlayerOriginalNames(parameters.dead).CharacterName));
            //await OfficerManager.AddOfficerPoints(parameters.killer, parameters.killer.GetTeam(), OfficerManager.config.data.MemberEnemyKilledPoints);
        }
        private static async void OnVehicleExploded(InteractableVehicle vehicle)
        {
            if (VehicleBay.VehicleExists(vehicle.id, out var data))
            {
                if (TeamManager.IsTeam1(vehicle.lockedGroup))
                {
                    await AddTeam1Tickets(-1 * data.TicketCost);
                }
                if (TeamManager.IsTeam2(vehicle.lockedGroup))
                {
                    await AddTeam2Tickets(-1 * data.TicketCost);
                }
                if (vehicle.transform.gameObject.TryGetComponent(out VehicleDamageOwnerComponent vc))
                {
                    if (XPManager.config.Data.VehicleDestroyedXP.ContainsKey(data.Type))
                    {
                        UCPlayer player = UCPlayer.FromCSteamID(vc.owner);

                        bool vehicleWasEnemy = (player.IsTeam1() && TeamManager.IsTeam2(vehicle.lockedGroup)) || (player.IsTeam2() && TeamManager.IsTeam1(vehicle.lockedGroup));
                        bool vehicleWasFriendly = (player.IsTeam1() && TeamManager.IsTeam1(vehicle.lockedGroup)) || (player.IsTeam2() && TeamManager.IsTeam2(vehicle.lockedGroup));

                        int amount = XPManager.config.Data.VehicleDestroyedXP[data.Type];
                        string message = string.Empty;

                        switch (data.Type)
                        {
                            case EVehicleType.HUMVEE:
                                message = "humvee_destroyed";
                                break;
                            case EVehicleType.TRANSPORT:
                                message = "transport_destroyed";
                                break;
                            case EVehicleType.LOGISTICS:
                                message = "logistics_destroyed";
                                break;
                            case EVehicleType.APC:
                                message = "apc_destroyed";
                                break;
                            case EVehicleType.IFV:
                                message = "ifv_destroyed";
                                break;
                            case EVehicleType.MBT:
                                message = "tank_destroyed";
                                break;
                            case EVehicleType.HELI_TRANSPORT:
                                message = "helicopter_destroyed";
                                break;
                            case EVehicleType.EMPLACEMENT:
                                message = "emplacement_destroyed";
                                break;
                        }

                        if (vehicleWasEnemy)
                        {
                            await XPManager.AddXP(player.Player, player.GetTeam(), player.NearbyMemberBonus(amount, 75), F.Translate("xp_" + message, player.Steam64));
                            if (player.IsNearSquadLeader(100))
                            {
                                await OfficerManager.AddOfficerPoints(player.Squad.Leader.Player, player.GetTeam(), amount, F.Translate("ofp_vehicle_eliminated", player.Squad.Leader.Steam64));
                            }
                        }
                        else if (vehicleWasFriendly)
                        {
                            if (message != string.Empty) message = "xp_friendly_" + message;
                            await XPManager.AddXP(player.Player, player.GetTeam(), -amount, F.Translate(message, player.Steam64));
                        }
                    }
                    
                }
            }
        }
        public static async Task OnRoundWin(ulong team)
        {
            var players = PlayerManager.OnlinePlayers.Where(p => p.GetTeam() == team).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];

                if (F.TryGetPlaytimeComponent(player.CSteamID, out var component))
                {
                    await XPManager.AddXP(player.Player, team, Mathf.RoundToInt(component.stats.xpgained * 0.2f), F.Translate("xp_victory", player.Steam64));

                    if (player.IsSquadLeader())
                    {
                        await OfficerManager.AddOfficerPoints(player.Squad.Leader.Player, team, Mathf.RoundToInt(component.stats.xpgained * 0.2f), F.Translate("ofp_squad_victory", player.Squad.Leader.Steam64));
                    }
                }
            }
        }
        public static async Task OnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            if (capturedTeam == 1 && !flag.HasBeenCapturedT1)
            {
                Team1Tickets += config.Data.TicketsFlagCaptured;
                flag.HasBeenCapturedT1 = true;
            }
            else if (capturedTeam == 2 && !flag.HasBeenCapturedT2)
            {
                Team1Tickets += config.Data.TicketsFlagCaptured;
                flag.HasBeenCapturedT2 = true;
            }

            if (TeamManager.IsTeam1(lostTeam))
                Team1Tickets += config.Data.TicketsFlagLost;
            if (TeamManager.IsTeam2(lostTeam))
                Team2Tickets += config.Data.TicketsFlagLost;

            Dictionary<string, int> alreadyUpdated = new Dictionary<string, int>();

            foreach (Player nelsonplayer in flag.PlayersOnFlagTeam1.Where(p => TeamManager.IsFriendly(p, capturedTeam)))
            {
                var player = UCPlayer.FromPlayer(nelsonplayer);

                await XPManager.AddXP(player.Player, capturedTeam, player.NearbyMemberBonus(XPManager.config.Data.FlagCapturedXP, 100), F.Translate("xp_flag_captured", player.Steam64));

                if (player.IsNearSquadLeader(100))
                {
                    if (alreadyUpdated.TryGetValue(player.Squad.Name, out var amount))
                    {
                        amount += OfficerManager.config.Data.MemberFlagCapturePoints;
                    }
                    else
                    {
                        alreadyUpdated.Add(player.Squad.Name, OfficerManager.config.Data.MemberFlagCapturePoints);
                    }
                }
            }

            for (int i = 0; i < SquadManager.Squads.Count; i++)
            {
                if (alreadyUpdated.TryGetValue(SquadManager.Squads[i].Name, out var amount))
                {
                    await OfficerManager.AddOfficerPoints(SquadManager.Squads[i].Leader.Player, capturedTeam, amount, F.Translate("ofp_squad_flag_captured", SquadManager.Squads[i].Leader.Steam64));
                }
            }
        }
        public static async Task OnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            Dictionary<string, int> alreadyUpdated = new Dictionary<string, int>();

            foreach (Player nelsonplayer in flag.PlayersOnFlagTeam1.Where(p => TeamManager.IsFriendly(p, capturedTeam)))
            {
                var player = UCPlayer.FromPlayer(nelsonplayer);

                await XPManager.AddXP(player.Player, capturedTeam, player.NearbyMemberBonus(XPManager.config.Data.FlagNeutralizedXP, 100), F.Translate("xp_flag_neutralized", player.Steam64));

                if (player.IsNearSquadLeader(100))
                {
                    if (alreadyUpdated.TryGetValue(player.Squad.Name, out var amount))
                    {
                        amount += OfficerManager.config.Data.MemberFlagCapturePoints;
                    }
                    else
                    {
                        alreadyUpdated.Add(player.Squad.Name, OfficerManager.config.Data.MemberFlagNeutralizedPoints);
                    }
                }
            }

            for (int i = 0; i < SquadManager.Squads.Count; i++)
            {
                if (alreadyUpdated.TryGetValue(SquadManager.Squads[i].Name, out var amount))
                {
                    await OfficerManager.AddOfficerPoints(SquadManager.Squads[i].Leader.Player, capturedTeam, amount, F.Translate("ofp_squad_flag_neutralized", SquadManager.Squads[i].Leader.Steam64));
                }
            }
        }
        public static async Task OnFlagTick(Player nelsonplayer)
        {
            var player = UCPlayer.FromPlayer(nelsonplayer);

            await XPManager.AddXP(player.Player, nelsonplayer.GetTeam(), XPManager.config.Data.FlagNeutralizedXP);

            if (player.Squad?.Members.Count > 1 && player.Steam64 != player.Squad.Leader.Steam64)
            {
                if (player.IsNearSquadLeader(100))
                {
                    await OfficerManager.AddOfficerPoints(player.Player, player.GetTeam(), OfficerManager.config.Data.MemberFlagTickPoints);
                }
            }
        }

        public static void OnPlayerJoined(UCPlayer player)
        {
            ulong team = player.GetTeam();
            GetTeamBleed(team, out int bleed, out var message);
            UpdateUI(player.Player.channel.owner.transportConnection, team, bleed, F.Translate(message, player));
        }
        public static void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            EffectManager.askEffectClearByID(config.Data.Team1TicketUIID, player.transportConnection);
            EffectManager.askEffectClearByID(config.Data.Team2TicketUIID, player.transportConnection);
            GetTeamBleed(newGroup, out int bleed, out var message);
            UpdateUI(player.transportConnection, newGroup, bleed, F.Translate(message, player));
        }

        public static void OnNewGameStarting()
        {
            Team1Tickets = config.Data.StartingTickets;
            Team2Tickets = config.Data.StartingTickets;
            UpdateUITeam1();
            UpdateUITeam2();

            if (ticketloop != null)
            {
                try
                {
                    UCWarfare.I.StopCoroutine(ticketloop);
                }
                catch { }
            }
            ticketloop = UCWarfare.I.StartCoroutine(TicketLoop());
        }

        public static async Task AddTeam1Tickets(int number)
        {
            Team1Tickets += number;
            if (Team1Tickets <= 0)
            {
                Team1Tickets = 0;
                await Data.Gamemode.DeclareWin(2);
            }
            UpdateUITeam1();
        }
        public static async Task AddTeam2Tickets(int number)
        {
            Team2Tickets += number;
            if (Team2Tickets <= 0)
            {
                Team2Tickets = 0;
                await Data.Gamemode.DeclareWin(1);
            }
            UpdateUITeam2();
        }

        public static void UpdateUI(ITransportConnection connection, ulong team, int bleed, string message)
        {
            ushort UIID = 0;
            int tickets = 0;
            if (TeamManager.IsTeam1(team))
            {
                tickets = Team1Tickets;
                UIID = config.Data.Team1TicketUIID;
            }

            else if (TeamManager.IsTeam2(team))
            {
                tickets = Team2Tickets;
                UIID = config.Data.Team2TicketUIID;
            }
                
            EffectManager.sendUIEffect(UIID, (short)UIID, connection, true,
                tickets.ToString(Data.Locale),
                bleed < 0 ? bleed.ToString(Data.Locale) : string.Empty,
                message
                );
        }
        public static void UpdateUITeam1()
        {
            GetTeamBleed(TeamManager.Team1ID, out int bleed, out string message);

            var players = PlayerManager.OnlinePlayers.Where(p => p.IsTeam1()).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                UpdateUI(players[i].Player.channel.owner.transportConnection, TeamManager.Team1ID, bleed, F.Translate(message, players[i]));
            }
        }
        public static void UpdateUITeam2()
        {
            GetTeamBleed(TeamManager.Team2ID, out int bleed, out string message);

            var players = PlayerManager.OnlinePlayers.Where(p => p.IsTeam2()).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                UpdateUI(players[i].Player.channel.owner.transportConnection, TeamManager.Team2ID, bleed, F.Translate(message, players[i]));
            }
        }
        public static void GetTeamBleed(ulong team, out int bleed, out string message)
        {
            if (Data.Gamemode is FlagGamemode fg)
            {
                int friendlyCount = fg.Rotation.Where(f => f.Owner == team).Count();
                int enemyCount = fg.Rotation.Where(f => f.Owner != team && !f.IsNeutral()).Count();

                float friendlyRatio = (float)friendlyCount * fg.Rotation.Count();
                float enemyRatio = (float)enemyCount / fg.Rotation.Count();

                if (enemyRatio <= 0.6F && friendlyRatio <= 0.6F)
                {
                    bleed = 0;
                    message = "";
                }
                else if (0.6F < enemyRatio && enemyRatio <= 0.8F)
                {
                    message = "enemy_controlling";
                    bleed = -1;
                }
                else if (0.8F < enemyRatio && enemyRatio < 1F)
                {
                    message = "enemy_dominating";
                    bleed = -2;
                }
                else if (enemyRatio == 1)
                {
                    message = "defeated";
                    bleed = -3;
                }
                else if (0.6F < friendlyRatio && friendlyRatio <= 0.8F)
                {
                    message = "controlling";
                    bleed = 1;
                }
                else if (0.8F < friendlyRatio && friendlyRatio < 1F)
                {
                    message = "dominating";
                    bleed = 2;
                }
                else if (friendlyRatio == 1)
                {
                    message = "victorious";
                    bleed = 3;
                }
                else
                {
                    bleed = 0;
                    message = "";
                }
            }
            else
            {
                bleed = 0;
                message = "";
            }
        }

        private static IEnumerator<WaitForSeconds> TicketLoop()
        {
            int count = 0;

            while (UCWarfare.I.State == Rocket.API.PluginState.Loaded)
            {
                GetTeamBleed(TeamManager.Team1ID, out int Team1Bleed, out _);
                GetTeamBleed(TeamManager.Team2ID, out int Team2Bleed, out _);

                if (count % 12 == 0) // every 1 minute
                {
                    if (Team1Bleed == -1)
                        Team1Tickets--;
                    if (Team2Bleed == -1)
                        Team2Tickets--;
                }
                if (count % 6 == 0) // every 30 seconds
                {
                    if (Team1Bleed == -2)
                        Team1Tickets--;
                    if (Team2Bleed == -2)
                        Team2Tickets--;
                }
                if (count % 2 == 0) // every 10 seconds
                {
                    if (Team1Bleed == -3)
                        Team1Tickets--;
                    if (Team2Bleed == -3)
                        Team2Tickets--;
                }

                count++;
                if (count >= 12)
                    count = 0;
                yield return new WaitForSeconds(5);
            }
        }

        public void Dispose()
        {
            VehicleManager.OnVehicleExploded -= OnVehicleExploded;
        }
    }
    public class TicketData : ConfigData
    {
        public int StartingTickets;
        public int FOBCost;
        public int TicketsFlagCaptured;
        public int TicketsFlagLost;
        public ushort Team1TicketUIID;
        public ushort Team2TicketUIID;

        public override void SetDefaults()
        {
            StartingTickets = 350;
            FOBCost = 20;
            TicketsFlagCaptured = 20;
            TicketsFlagLost = -20;
            Team1TicketUIID = 36035;
            Team2TicketUIID = 36034;
        }
        public TicketData() { }
    }
}
