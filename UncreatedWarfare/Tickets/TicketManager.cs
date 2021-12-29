﻿using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.XP;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Tickets
{
    public class TicketManager : IDisposable
    {
        public static Config<TicketData> config = new Config<TicketData>(Data.TicketStorage, "config.json");

        public static int Team1Tickets;
        public static int Team2Tickets;
        public static DateTime TimeSinceMatchStart;
        private static ulong _previousWinner;
        internal static int _Team1previousTickets;
        internal static int _Team2previousTickets;
        public TicketManager()
        {
            TimeSinceMatchStart = DateTime.Now;

            _previousWinner = 0;

            Team1Tickets = config.Data.StartingTickets;
            Team2Tickets = config.Data.StartingTickets;
            _Team1previousTickets = config.Data.StartingTickets;
            _Team2previousTickets = config.Data.StartingTickets;

            

            VehicleManager.OnVehicleExploded += OnVehicleExploded;
        }
        public static void OnPlayerDeath(UCWarfare.DeathEventArgs eventArgs)
        {
            if (TeamManager.IsTeam1(eventArgs.dead))
                AddTeam1Tickets(-1);
            else if (TeamManager.IsTeam2(eventArgs.dead))
                AddTeam2Tickets(-1);

        }
        public static void OnPlayerDeathOffline(ulong deadteam)
        {
            if (deadteam == 1)
                AddTeam1Tickets(-1);
            else if (deadteam == 2)
                AddTeam2Tickets(-1);

        }
        public static void OnPlayerSuicide(UCWarfare.SuicideEventArgs eventArgs)
        {
            if (TeamManager.IsTeam1(eventArgs.dead))
                AddTeam1Tickets(-1);
            else if (TeamManager.IsTeam2(eventArgs.dead))
                AddTeam2Tickets(-1);
        }
        public static void OnEnemyKilled(UCWarfare.KillEventArgs parameters)
        {
            if (Data.Is(out Insurgency insurgency))
            {
                if (parameters.dead.quests.groupID.m_SteamID == insurgency.DefendingTeam)
                {
                    insurgency.AddIntelligencePoints(1);
                    if (parameters.killer.TryGetPlaytimeComponent(out PlaytimeComponent c) && c.stats is InsurgencyPlayerStats s)
                        s._intelligencePointsCollected++;
                    insurgency.GameStats.intelligenceGathered++;
                }
            }
            XPManager.AddXP(parameters.killer, UCPlayer.FromPlayer(parameters.killer).NearbyMemberBonus(XPManager.config.Data.EnemyKilledXP, 75),
                Translation.Translate("xp_enemy_killed", parameters.killer.channel.owner.playerID.steamID.m_SteamID, F.GetPlayerOriginalNames(parameters.dead).CharacterName));
            //await OfficerManager.AddOfficerPoints(parameters.killer, parameters.killer.GetTeam(), OfficerManager.config.data.MemberEnemyKilledPoints);
        }
        public static void OnFriendlyKilled(UCWarfare.KillEventArgs parameters)
        {
            XPManager.AddXP(parameters.killer, XPManager.config.Data.FriendlyKilledXP,
                Translation.Translate("xp_friendly_killed", parameters.killer.channel.owner.playerID.steamID.m_SteamID, F.GetPlayerOriginalNames(parameters.dead).CharacterName));
            //await OfficerManager.AddOfficerPoints(parameters.killer, parameters.killer.GetTeam(), OfficerManager.config.data.MemberEnemyKilledPoints);
        }
        private static void OnVehicleExploded(InteractableVehicle vehicle)
        {
            if (VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
            {
                ulong lteam = vehicle.lockedGroup.m_SteamID.GetTeam();

                if (lteam == 1)
                    AddTeam1Tickets(-1 * data.TicketCost);
                else if (lteam == 2)
                    AddTeam2Tickets(-1 * data.TicketCost);

                if (vehicle.transform.gameObject.TryGetComponent(out VehicleComponent vc))
                {
                    if (XPManager.config.Data.VehicleDestroyedXP.ContainsKey(data.Type))
                    {
                        UCPlayer player = UCPlayer.FromCSteamID(vc.owner);
                        ulong dteam = player.GetTeam();
                        bool vehicleWasEnemy = (dteam == 1 && lteam == 2) || (dteam == 2 && lteam == 1);
                        bool vehicleWasFriendly = dteam == lteam;
                        if (!vehicleWasFriendly)
                            Stats.StatsManager.ModifyTeam(dteam, t => t.VehiclesDestroyed++, false);
                        if (!XPManager.config.Data.VehicleDestroyedXP.TryGetValue(data.Type, out int amount))
                            amount = 0;
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

                        UCPlayer owner = UCPlayer.FromCSteamID(vehicle.lockedOwner);
                        if (vehicleWasEnemy)
                        {
                            if (owner is null)
                                Chat.Broadcast("VEHICLE_DESTROYED_UNKNOWN", F.ColorizeName(F.GetPlayerOriginalNames(player).CharacterName, player.GetTeam()), "", vehicle.asset.vehicleName);
                            else
                                Chat.Broadcast("VEHICLE_DESTROYED", F.ColorizeName(F.GetPlayerOriginalNames(player).CharacterName, player.GetTeam()), F.ColorizeName(F.GetPlayerOriginalNames(owner).CharacterName, owner.GetTeam()), vehicle.asset.vehicleName);

                            AwardSquadXP(player, 100f, amount, Mathf.RoundToInt(amount * 0.25f), "xp_" + message, "ofp_vehicle_eliminated", 0.25F);
                            Stats.StatsManager.ModifyStats(player.Steam64, s => s.VehiclesDestroyed++, false);
                            Stats.StatsManager.ModifyVehicle(vehicle.id, v => v.TimesDestroyed++);
                        }
                        else if (vehicleWasFriendly)
                        {
                            Chat.Broadcast("VEHICLE_TEAMKILLED", F.ColorizeName(F.GetPlayerOriginalNames(player).CharacterName, player.GetTeam()), "", vehicle.asset.vehicleName);

                            if (message != string.Empty) message = "xp_friendly_" + message;
                            XPManager.AddXP(player.Player, -amount, Translation.Translate(message, player.Steam64));
                            Invocations.Warfare.LogFriendlyVehicleKill.NetInvoke(player.Steam64, vehicle.id, vehicle.asset.vehicleName ?? vehicle.id.ToString(), DateTime.Now);
                        }
                        if (vehicle.TryGetComponent(out SpawnedVehicleComponent comp))
                            Data.Reporter.OnVehicleDied(vehicle.lockedOwner.m_SteamID, comp.spawn.SpawnPadInstanceID, vc.lastDamager, vehicle.asset.GUID, vc.item, vc.lastDamageOrigin, vehicleWasFriendly);
                        else
                            Data.Reporter.OnVehicleDied(vehicle.lockedOwner.m_SteamID, uint.MaxValue, vc.lastDamager, vehicle.asset.GUID, vc.item, vc.lastDamageOrigin, vehicleWasFriendly);
                    }
                }
            }
        }
        public static void OnRoundWin(ulong team)
        {
            _previousWinner = team;

            float winMultiplier = 0.15f;
            float handicapMultiplier = 0;
            if (team == 1 && _Team2previousTickets > _Team1previousTickets)
            {
                handicapMultiplier = ((float)_Team2previousTickets / _Team1previousTickets) * 0.1F;
            }
            else if (team == 2 && _Team1previousTickets > _Team2previousTickets)
            {
                handicapMultiplier = ((float)_Team1previousTickets / _Team2previousTickets) * 0.1F;
            }

            List<UCPlayer> players = PlayerManager.OnlinePlayers.Where(p => p.GetTeam() == team).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                UCPlayer player = players[i];

                if (player.CSteamID.TryGetPlaytimeComponent(out PlaytimeComponent component) && component.stats is IExperienceStats exp)
                {
                    if (exp.XPGained > 0)
                        XPManager.AddXP(player.Player, Mathf.RoundToInt(exp.XPGained * winMultiplier), Translation.Translate("xp_victory", player.Steam64));

                    if (handicapMultiplier > 0)
                        XPManager.AddXP(player.Player, Mathf.RoundToInt(exp.XPGained * handicapMultiplier), Translation.Translate("xp_handicap", player.Steam64));

                    if (player.IsSquadLeader())
                    {
                        if (exp.OFPGained > 0)
                            OfficerManager.AddOfficerPoints(player.Squad.Leader.Player, Mathf.RoundToInt(exp.OFPGained * winMultiplier), Translation.Translate("ofp_squad_victory", player.Squad.Leader.Steam64));
                    }
                }
            }
        }
        public static void OnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            if (Data.Is<Invasion>(out _))
            {
                if (capturedTeam == 1)
                {
                    Team1Tickets += Gamemode.Config.Invasion.TicketsFlagCaptured;
                    flag.HasBeenCapturedT1 = true;
                }
                else if (capturedTeam == 2)
                {
                    Team2Tickets += Gamemode.Config.Invasion.TicketsFlagCaptured;
                    flag.HasBeenCapturedT2 = true;
                }
            }
            else if (Data.Is<TeamCTF>(out _))
            {
                if (capturedTeam == 1 && !flag.HasBeenCapturedT1)
                {
                    Team1Tickets += Gamemode.Config.TeamCTF.TicketsFlagCaptured;
                    flag.HasBeenCapturedT1 = true;
                }
                else if (capturedTeam == 2 && !flag.HasBeenCapturedT2)
                {
                    Team2Tickets += Gamemode.Config.TeamCTF.TicketsFlagCaptured;
                    flag.HasBeenCapturedT2 = true;
                }

                if (lostTeam == 1)
                    Team1Tickets += Gamemode.Config.TeamCTF.TicketsFlagLost;
                if (lostTeam == 2)
                    Team2Tickets += Gamemode.Config.TeamCTF.TicketsFlagLost;
            }
            else
            {
                if (capturedTeam == 1 && !flag.HasBeenCapturedT1)
                {
                    Team1Tickets += config.Data.TicketsFlagCaptured;
                    flag.HasBeenCapturedT1 = true;
                }
                else if (capturedTeam == 2 && !flag.HasBeenCapturedT2)
                {
                    Team2Tickets += config.Data.TicketsFlagCaptured;
                    flag.HasBeenCapturedT2 = true;
                }

                if (lostTeam == 1)
                    Team1Tickets += config.Data.TicketsFlagLost;
                if (lostTeam == 2)
                    Team2Tickets += config.Data.TicketsFlagLost;
            }
            

            UpdateUITeam1();
            UpdateUITeam2();

            Dictionary<string, int> alreadyUpdated = new Dictionary<string, int>();

            foreach (Player nelsonplayer in flag.PlayersOnFlag.Where(p => TeamManager.IsFriendly(p, capturedTeam)))
            {
                UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);

                int xp = XPManager.config.Data.FlagCapturedXP;

                XPManager.AddXP(player.Player, xp, Translation.Translate("xp_flag_neutralized", player.Steam64));
                XPManager.AddXP(player.Player, player.NearbyMemberBonus(xp, 150) - xp, Translation.Translate("xp_squad_bonus", player.Steam64));

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
                if (alreadyUpdated.TryGetValue(SquadManager.Squads[i].Name, out int amount))
                {
                    OfficerManager.AddOfficerPoints(SquadManager.Squads[i].Leader.Player, amount, Translation.Translate("ofp_squad_flag_captured", SquadManager.Squads[i].Leader.Steam64));
                }
            }
        }
        public static void OnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            Dictionary<string, int> alreadyUpdated = new Dictionary<string, int>();

            foreach (Player nelsonplayer in flag.PlayersOnFlag.Where(p => TeamManager.IsFriendly(p, capturedTeam)))
            {
                UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);

                int xp = XPManager.config.Data.FlagNeutralizedXP;

                XPManager.AddXP(player.Player, xp, Translation.Translate("xp_flag_neutralized", player.Steam64));
                XPManager.AddXP(player.Player, player.NearbyMemberBonus(xp, 150) - xp, Translation.Translate("xp_squad_bonus", player.Steam64));

                if (player.IsNearSquadLeader(150))
                {
                    if (alreadyUpdated.TryGetValue(player.Squad.Name, out int amount))
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
                if (alreadyUpdated.TryGetValue(SquadManager.Squads[i].Name, out int amount))
                {
                    OfficerManager.AddOfficerPoints(SquadManager.Squads[i].Leader.Player, amount, Translation.Translate("ofp_squad_flag_neutralized", SquadManager.Squads[i].Leader.Steam64));
                }
            }
        }
        public static void OnFlagTick()
        {
            if (Data.Is(out IFlagRotation fg))
            {
                for (int i = 0; i < fg.Rotation.Count; i++)
                {
                    Flag flag = fg.Rotation[i];
                    if (flag.LastDeltaPoints > 0 && flag.Owner != 1)
                    {
                        for (int j = 0; j < flag.PlayersOnFlagTeam1.Count; j++)
                            XPManager.AddXP(flag.PlayersOnFlagTeam1[j],
                                XPManager.config.Data.FlagAttackXP,
                                Translation.Translate("xp_flag_attack", flag.PlayersOnFlagTeam1[j]));
                    }
                    else if (flag.LastDeltaPoints < 0 && flag.Owner != 2)
                    {
                        for (int j = 0; j < flag.PlayersOnFlagTeam2.Count; j++)
                            XPManager.AddXP(flag.PlayersOnFlagTeam2[j],
                                XPManager.config.Data.FlagAttackXP,
                                Translation.Translate("xp_flag_attack", flag.PlayersOnFlagTeam2[j]));
                    }
                    else if (flag.Owner == 1 && flag.IsObj(2) && flag.Team2TotalCappers == 0)
                    {
                        for (int j = 0; j < flag.PlayersOnFlagTeam1.Count; j++)
                            XPManager.AddXP(flag.PlayersOnFlagTeam1[j],
                                XPManager.config.Data.FlagDefendXP,
                                Translation.Translate("xp_flag_defend", flag.PlayersOnFlagTeam1[j]));
                    }
                    else if (flag.Owner == 2 && flag.IsObj(1) && flag.Team1TotalCappers == 0)
                    {
                        for (int j = 0; j < flag.PlayersOnFlagTeam2.Count; j++)
                            XPManager.AddXP(flag.PlayersOnFlagTeam2[j],
                                XPManager.config.Data.FlagDefendXP,
                                Translation.Translate("xp_flag_defend", flag.PlayersOnFlagTeam2[j]));
                    }
                }
            }
        }
        public static void OnPlayerJoined(UCPlayer player)
        {
            ulong team = player.GetTeam();
            GetTeamBleed(team, out int bleed, out string message);
            UpdateUI(player.Player.channel.owner.transportConnection, team, bleed, Translation.Translate(message, player));
        }
        public static void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            EffectManager.askEffectClearByID(config.Data.Team1TicketUIID, player.transportConnection);
            EffectManager.askEffectClearByID(config.Data.Team2TicketUIID, player.transportConnection);
            GetTeamBleed(newGroup, out int bleed, out string message);
            UpdateUI(player.transportConnection, newGroup, bleed, Translation.Translate(message, player));
        }
        public static void OnStagingPhaseEnded()
        {
            TimeSinceMatchStart = DateTime.Now;
        }
        public static void OnNewGameStarting()
        {
            if (Data.Is(out Invasion invasion))
            {
                int attack = Gamemode.Config.Invasion.AttackStartingTickets;
                int defence = Gamemode.Config.Invasion.AttackStartingTickets + (invasion.Rotation.Count * Gamemode.Config.Invasion.TicketsFlagCaptured);

                if (invasion.AttackingTeam == 1)
                {
                    Team1Tickets = attack;
                    Team2Tickets = defence;
                }
                else if (invasion.AttackingTeam == 2)
                {
                    Team2Tickets = attack;
                    Team1Tickets = defence;
                }
            }
            else if (Data.Is(out Insurgency insurgency))
            {
                int attack = Gamemode.Config.Insurgency.AttackStartingTickets;
                int defence = insurgency.CachesLeft;

                if (insurgency.AttackingTeam == 1)
                {
                    Team1Tickets = attack;
                    Team2Tickets = defence;
                }
                else if (insurgency.AttackingTeam == 2)
                {
                    Team2Tickets = attack;
                    Team1Tickets = defence;
                }
            }
            else
            {
                Team1Tickets = config.Data.StartingTickets;
                Team2Tickets = config.Data.StartingTickets;
            }

            UpdateUITeam1();
            UpdateUITeam2();
        }

        public static void AddTeam1Tickets(int number)
        {
            if (Data.Is(out Insurgency insurgency) && insurgency.DefendingTeam == 1)
                return;

            Team1Tickets += number;
            if (Team1Tickets <= 0)
            {
                Team1Tickets = 0;
                Data.Gamemode.DeclareWin(2);
            }
            UpdateUITeam1();
        }
        public static void AddTeam2Tickets(int number)
        {
            if (Data.Is(out Insurgency insurgency) && insurgency.DefendingTeam == 2)
                return;

            Team2Tickets += number;
            if (Team2Tickets <= 0)
            {
                Team2Tickets = 0;
                Data.Gamemode.DeclareWin(1);
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

            if (Data.Is(out Insurgency insurgency) && insurgency.DefendingTeam == team)
            {
                EffectManager.sendUIEffect(UIID, (short)UIID, connection, true,
                insurgency.CachesLeft.ToString(Data.Locale) + " Caches", "", "");
            }
            else
            {
                EffectManager.sendUIEffect(UIID, (short)UIID, connection, true,
                tickets.ToString(Data.Locale),
                bleed < 0 ? bleed.ToString(Data.Locale) : string.Empty,
                message
                );
            }
                
        }
        public static void UpdateUITeam1()
        {
            GetTeamBleed(TeamManager.Team1ID, out int bleed, out string message);

            var players = PlayerManager.OnlinePlayers.Where(p => p.IsTeam1()).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                UpdateUI(players[i].Player.channel.owner.transportConnection, TeamManager.Team1ID, bleed, Translation.Translate(message, players[i]));
            }
        }
        public static void UpdateUITeam2()
        {
            GetTeamBleed(TeamManager.Team2ID, out int bleed, out string message);

            var players = PlayerManager.OnlinePlayers.Where(p => p.IsTeam2()).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                UpdateUI(players[i].Player.channel.owner.transportConnection, TeamManager.Team2ID, bleed, Translation.Translate(message, players[i]));
            }
        }
        public static void GetTeamBleed(ulong team, out int bleed, out string message)
        {
            if (Data.Is(out IFlagRotation fg))
            {
                if (Data.Is(out Invasion invasion) && team == invasion.AttackingTeam)
                {
                    int defenderFlags = fg.Rotation.Where(f => f.Owner == invasion.DefendingTeam).Count();

                    if (defenderFlags == fg.Rotation.Count)
                    {
                        bleed = -1;
                        message = bleed.ToString() + "BLEEDING TICKETS";
                    }
                }
            }

            bleed = 0;
            message = "";
        }
        public static void AwardSquadXP(UCPlayer ucplayer, float range, int xp, int ofp, string KeyplayerTranslationKey, string squadTranslationKey, float squadMultiplier)
        {
            string xpstr = Translation.Translate(KeyplayerTranslationKey, ucplayer.Steam64);
            string sqstr = Translation.Translate(squadTranslationKey, ucplayer.Steam64);
            XPManager.AddXP(ucplayer.Player, xp, xpstr);

            if (ucplayer.Squad != null && ucplayer.Squad?.Members.Count > 1)
            {
                if (ucplayer == ucplayer.Squad.Leader)
                    OfficerManager.AddOfficerPoints(ucplayer.Player, ofp, sqstr);

                int squadxp = (int)Math.Round(xp * squadMultiplier);
                int squadofp = (int)Math.Round(ofp * squadMultiplier);

                if (squadxp > 0)
                {
                    for (int i = 0; i < ucplayer.Squad.Members.Count; i++)
                    {
                        UCPlayer member = ucplayer.Squad.Members[i];
                        if (member != ucplayer && ucplayer.IsNearOtherPlayer(member, range))
                        {
                            XPManager.AddXP(member.Player, squadxp, sqstr);
                            if (member.IsSquadLeader())
                                OfficerManager.AddOfficerPoints(ucplayer.Player, squadofp, sqstr);
                        }
                    }
                }
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
        public int TicketHandicapDifference;
        public int FOBCost;
        public int TicketsFlagCaptured;
        public int TicketsFlagLost;
        public ushort Team1TicketUIID;
        public ushort Team2TicketUIID;

        public override void SetDefaults()
        {
            StartingTickets = 200;
            TicketHandicapDifference = 40;
            FOBCost = 15;
            TicketsFlagCaptured = 20;
            TicketsFlagLost = -20;
            Team1TicketUIID = 36035;
            Team2TicketUIID = 36034;
        }
        public TicketData() { }
    }
}
