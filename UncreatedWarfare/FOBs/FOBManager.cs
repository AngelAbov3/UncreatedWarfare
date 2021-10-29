﻿using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.FOBs
{
    public delegate void PlayerEnteredFOBRadiusHandler(FOB fob, UCPlayer player);
    public delegate void PlayerLeftFOBRadiusHandler(FOB fob, UCPlayer player);

    public class FOBManager
    {
        public static Config<FOBConfig> config;
        public static readonly List<FOB> Team1FOBs = new List<FOB>();
        public static readonly List<FOB> Team2FOBs = new List<FOB>();
        public static readonly List<SpecialFOB> SpecialFOBs = new List<SpecialFOB>();


        public static event PlayerEnteredFOBRadiusHandler OnPlayerEnteredFOBRadius;
        public static event PlayerLeftFOBRadiusHandler OnPlayerLeftFOBRadius;
        public static event PlayerEnteredFOBRadiusHandler OnEnemyEnteredFOBRadius;
        public static event PlayerLeftFOBRadiusHandler OnEnemyLeftFOBRadius;

        public FOBManager()
        {
            config = new Config<FOBConfig>(Data.FOBStorage, "config.json");

            OnPlayerEnteredFOBRadius += OnEnteredFOBRadius;
            OnPlayerLeftFOBRadius += OnLeftFOBRadius;
            OnEnemyEnteredFOBRadius += OnEnemyEnteredFOB;
            OnEnemyLeftFOBRadius += OnEnemyLeftFOB;

        }

        public static void Dispose()
        {
            Team1FOBs.Clear();
            Team2FOBs.Clear();
            SpecialFOBs.Clear();
            UpdateUIAll();
            OnPlayerEnteredFOBRadius -= OnEnteredFOBRadius;
            OnPlayerLeftFOBRadius -= OnLeftFOBRadius;
            OnEnemyEnteredFOBRadius -= OnEnemyEnteredFOB;
            OnEnemyLeftFOBRadius -= OnEnemyLeftFOB;
        }
        public static void OnItemDropped(Item item, Vector3 point)
        {
            if (item.id == config.Data.Team1BuildID || item.id == config.Data.Team2BuildID)
            {
                IEnumerable<BarricadeDrop> TotalFOBs = UCBarricadeManager.GetAllFobs();
                IEnumerable<BarricadeDrop> NearbyFOBs = UCBarricadeManager.GetNearbyBarricades(TotalFOBs, config.Data.FOBBuildPickupRadius, point, true);

                if (NearbyFOBs.Count() != 0)
                {
                    UpdateBuildUIForFOB(NearbyFOBs.FirstOrDefault());
                }
                else
                {
                    IEnumerable<BarricadeDrop> NearbyFOBBases = UCBarricadeManager.GetNearbyBarricades(config.Data.FOBBaseID, 30, point, true);

                    if (NearbyFOBBases.Count() != 0)
                    {
                        UpdateBuildUIForFOB(NearbyFOBBases.FirstOrDefault());
                    }
                }
            }
        }
        public static void OnItemRemoved(SDG.Unturned.ItemData itemData)
        {
            if (itemData.item.id == config.Data.Team1BuildID || itemData.item.id == config.Data.Team2BuildID)
            {
                IEnumerable<BarricadeDrop> TotalFOBs = UCBarricadeManager.GetAllFobs();
                IEnumerable<BarricadeDrop> NearbyFOBs = UCBarricadeManager.GetNearbyBarricades(TotalFOBs, config.Data.FOBBuildPickupRadius, itemData.point, true);

                if (NearbyFOBs.Count() != 0)
                {
                    UpdateBuildUIForFOB(NearbyFOBs.FirstOrDefault());
                }
                else
                {
                    IEnumerable<BarricadeDrop> NearbyFOBBases = UCBarricadeManager.GetNearbyBarricades(config.Data.FOBBaseID, 30, itemData.point, true);

                    if (NearbyFOBBases.Count() != 0)
                    {
                        UpdateBuildUIForFOB(NearbyFOBBases.FirstOrDefault());
                    }
                }
            }
        }
        public static void RefillMainStorages()
        {
            var repairStations = UCBarricadeManager.GetBarricadesByID(config.Data.RepairStationID).ToList();
            var ammoCrates = UCBarricadeManager.GetBarricadesByID(config.Data.AmmoCrateID).ToList();

            for (int i = 0; i < repairStations.Count; i++)
            {
                if (F.IsInMain(repairStations[i].model.transform.position))
                {
                    ushort BuildID = 0;
                    if (repairStations[i].GetServersideData().group == 1)
                        BuildID = config.Data.Team1BuildID;
                    else if (repairStations[i].GetServersideData().group == 2)
                        BuildID = config.Data.Team2BuildID;

                    UCBarricadeManager.TryAddItemToStorage(repairStations[i], BuildID);
                }
            }
            for (int i = 0; i < ammoCrates.Count; i++)
            {
                if (F.IsInMain(ammoCrates[i].model.transform.position))
                {
                    ushort AmmoID = 0;
                    if (ammoCrates[i].GetServersideData().group == 1)
                        AmmoID = config.Data.Team1AmmoID;
                    else if (ammoCrates[i].GetServersideData().group == 2)
                        AmmoID = config.Data.Team2AmmoID;

                    UCBarricadeManager.TryAddItemToStorage(ammoCrates[i], AmmoID);
                }
            }
        }
        public static void UpdateBuildUIForFOB(BarricadeDrop fob)
        {
            var data = fob.GetServersideData();

            ushort BuildID = 0;
            if (data.group == 1)
                BuildID = config.Data.Team1BuildID;
            else if (data.group == 2)
                BuildID = config.Data.Team2BuildID;
            else
                return;

            List<SDG.Unturned.ItemData> NearbyBuild = UCBarricadeManager.GetNearbyItems(BuildID, config.Data.FOBBuildPickupRadius, fob.model.position);

            List<UCPlayer> nearbyPlayers = PlayerManager.OnlinePlayers.Where(p => p.GetTeam() == data.group && !p.Player.life.isDead && (p.Position - fob.model.position).sqrMagnitude < Math.Pow(config.Data.FOBBuildPickupRadius, 2)).ToList();

            for (int i = 0; i < nearbyPlayers.Count; i++)
            {
                EffectManager.sendUIEffectText((short)unchecked(config.Data.BuildResourceUI), nearbyPlayers[i].Player.channel.owner.transportConnection, true,
                    "Build",
                    NearbyBuild.Count.ToString()
                    );
            }
        }
        public static void OnAmmoCrateUpdated(InteractableStorage storage, BarricadeDrop ammoCrate)
        {
            IEnumerable<BarricadeDrop> TotalFOBs = UCBarricadeManager.GetAllFobs().Where(f => f.GetServersideData().group == ammoCrate.GetServersideData().group);
            IEnumerable<BarricadeDrop> NearbyFOBs = UCBarricadeManager.GetNearbyBarricades(TotalFOBs, config.Data.FOBBuildPickupRadius, ammoCrate.model.position, true);

            if (NearbyFOBs.Count() != 0)
            {
                UpdateAmmoUIForFOB(storage, ammoCrate, NearbyFOBs.FirstOrDefault());
            }
        }
        public static void UpdateAmmoUIForFOB(InteractableStorage storage, BarricadeDrop ammoCrate, BarricadeDrop fob)
        {
            var data = ammoCrate.GetServersideData();

            int ammoCount = 0;

            for (int i = 0; i < storage.items.items.Count; i++)
            {
                var jar = storage.items.items[i];

                if ((TeamManager.IsTeam1(data.group) && jar.item.id == config.Data.Team1AmmoID) || (TeamManager.IsTeam2(data.group) && jar.item.id == config.Data.Team2AmmoID))
                {
                    ammoCount++;
                }
            }

            List<UCPlayer> nearbyPlayers = PlayerManager.OnlinePlayers.Where(p => p.GetTeam() == data.group && !p.Player.life.isDead && (p.Position - fob.model.position).sqrMagnitude < Math.Pow(config.Data.FOBBuildPickupRadius, 2)).ToList();

            for (int i = 0; i < nearbyPlayers.Count; i++)
            {
                EffectManager.sendUIEffectText((short)unchecked(config.Data.BuildResourceUI), nearbyPlayers[i].Player.channel.owner.transportConnection, true,
                    "Ammo",
                    ammoCount.ToString()
                    );
            }
        }
        public static void OnGameTick(uint counter)
        {
            for (int i = 0; i < Team1FOBs.Count; i++)
            {
                Tick(Team1FOBs[i], (int)counter);
            }
            for (int i = 0; i < Team2FOBs.Count; i++)
            {
                Tick(Team2FOBs[i], (int)counter);
            }
            for (int i = 0; i < SpecialFOBs.Count; i++)
            {
                Tick(SpecialFOBs[i], (int)counter);
            }

            if (counter % 60 == 0)
                RefillMainStorages();
        }
        public static void Tick(FOB fob, int counter = -1)
        {
            for (int j = 0; j < PlayerManager.OnlinePlayers.Count; j++)
            {
                if (PlayerManager.OnlinePlayers[j].GetTeam() == fob.Structure.GetServersideData().group)
                {
                    if ((fob.Structure.model.position - PlayerManager.OnlinePlayers[j].Position).sqrMagnitude < Math.Pow(config.Data.FOBBuildPickupRadius, 2))
                    {
                        if (!fob.nearbyPlayers.Contains(PlayerManager.OnlinePlayers[j]))
                        {
                            fob.nearbyPlayers.Add(PlayerManager.OnlinePlayers[j]);
                            OnPlayerEnteredFOBRadius?.Invoke(fob, PlayerManager.OnlinePlayers[j]);
                        }
                        else
                        {
                            if (counter % 5 == 0)
                            {
                                UpdateBuildUIForFOB(fob.Structure);
                            }
                        }
                    }
                    else
                    {
                        if (fob.nearbyPlayers.Contains(PlayerManager.OnlinePlayers[j]))
                        {
                            fob.nearbyPlayers.Remove(PlayerManager.OnlinePlayers[j]);
                            OnPlayerLeftFOBRadius?.Invoke(fob, PlayerManager.OnlinePlayers[j]);
                        }
                    }
                }
                else
                {
                    if (!PlayerManager.OnlinePlayers[j].Player.life.isDead && (fob.Structure.model.position - PlayerManager.OnlinePlayers[j].Position).sqrMagnitude < Math.Pow(9, 2))
                    {
                        if (!fob.nearbyEnemies.Contains(PlayerManager.OnlinePlayers[j]))
                        {
                            fob.nearbyEnemies.Add(PlayerManager.OnlinePlayers[j]);
                            OnEnemyEnteredFOBRadius?.Invoke(fob, PlayerManager.OnlinePlayers[j]);
                        }
                    }
                    else
                    {
                        if (fob.nearbyEnemies.Contains(PlayerManager.OnlinePlayers[j]))
                        {
                            fob.nearbyEnemies.Remove(PlayerManager.OnlinePlayers[j]);
                            OnEnemyLeftFOBRadius?.Invoke(fob, PlayerManager.OnlinePlayers[j]);
                        }
                    }
                }
            }
        }
        public static void Tick(SpecialFOB special, int counter = -1)
        {
            if (special.DisappearAroundEnemies && counter % 5 == 0)
            {
                if (Provider.clients.Where(p => p.GetTeam() != special.Team && (p.player.transform.position - special.Point).sqrMagnitude < Math.Pow(20, 2)).Count() > 0)
                {
                    DeleteSpecialFOB(special.Name, special.Team);
                }
            }
        }

        public static void OnEnteredFOBRadius(FOB fob, UCPlayer player)
        {
            EffectManager.sendUIEffect(config.Data.BuildResourceUI, (short)unchecked(config.Data.BuildResourceUI), player.Player.channel.owner.transportConnection, true);

            UpdateBuildUIForFOB(fob.Structure);

            BarricadeDrop NearestAmmoCrate = UCBarricadeManager.GetNearbyBarricades(config.Data.AmmoCrateID, config.Data.FOBBuildPickupRadius, fob.Structure.model.position, true).FirstOrDefault();

            if (NearestAmmoCrate != null)
            {
                if (NearestAmmoCrate.interactable is InteractableStorage storage)
                {
                    UpdateAmmoUIForFOB(storage, NearestAmmoCrate, fob.Structure);
                }
            }
        }
        public static void OnLeftFOBRadius(FOB fob, UCPlayer player)
        {
            EffectManager.askEffectClearByID(config.Data.BuildResourceUI, player.Player.channel.owner.transportConnection);
        }

        public static void OnEnemyEnteredFOB(FOB fob, UCPlayer enemy)
        {
            UpdateUIForTeam(fob.Structure.GetServersideData().group);
        }
        public static void OnEnemyLeftFOB(FOB fob, UCPlayer enemy)
        {
            UpdateUIForTeam(fob.Structure.GetServersideData().group);
        }

        public static void OnBarricadeDestroyed(SDG.Unturned.BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
        {
            if (data.barricade.id == config.Data.FOBID)
            {
                DeleteFOB(instanceID, data.group.GetTeam(), drop.model.TryGetComponent(out BarricadeComponent o) ? o.LastDamager : 0);
            }
            else if (data.barricade.id == config.Data.AmmoCrateID)
            {
                IEnumerable<BarricadeDrop> TotalFOBs = UCBarricadeManager.GetAllFobs().Where(f => f.GetServersideData().group == data.group);
                IEnumerable<BarricadeDrop> NearbyFOBs = UCBarricadeManager.GetNearbyBarricades(TotalFOBs, config.Data.FOBBuildPickupRadius, drop.model.position, true);

                if (NearbyFOBs.Count() != 0)
                {
                    List<UCPlayer> nearbyPlayers = PlayerManager.OnlinePlayers.Where(p => p.GetTeam() == data.group && !p.Player.life.isDead && (p.Position - NearbyFOBs.FirstOrDefault().model.position).sqrMagnitude < Math.Pow(config.Data.FOBBuildPickupRadius, 2)).ToList();

                    for (int i = 0; i < nearbyPlayers.Count; i++)
                    {
                        EffectManager.sendUIEffectText((short)unchecked(config.Data.BuildResourceUI), nearbyPlayers[i].Player.channel.owner.transportConnection, true,
                            "Ammo",
                            "0"
                            );
                    }
                }
            }
            else if (data.barricade.id == config.Data.FOBBaseID)
            {
                if (drop.model.TryGetComponent<FOBBaseComponent>(out var component))
                {
                    component.OnDestroyed();
                }
            }
        }

        public static void LoadFobsFromMap()
        {
            GetRegionBarricadeLists(
                out List<BarricadeDrop> Team1FOBBarricades,
                out List<BarricadeDrop> Team2FOBBarricades
                );

            Team1FOBs.Clear();
            Team2FOBs.Clear();
            SpecialFOBs.Clear();

            for (int i = 0; i < Team1FOBs.Count; i++)
            {
                Team1FOBs.Add(new FOB("FOB" + (i + 1).ToString(Data.Locale), i + 1, Team1FOBBarricades[i]));
            }
            for (int i = 0; i < Team2FOBs.Count; i++)
            {
                Team2FOBs.Add(new FOB("FOB" + (i + 1).ToString(Data.Locale), i + 1, Team2FOBBarricades[i]));
            }
            UpdateUIAll();
        }
        public static void RegisterNewFOB(BarricadeDrop Structure)
        {
            ulong team = Structure.GetServersideData().group.GetTeam();
            if (Data.Gamemode is Gamemodes.Flags.TeamCTF.TeamCTF ctf && ctf.GameStats != null)
            {
                if (F.TryGetPlaytimeComponent(Structure.GetServersideData().owner, out PlaytimeComponent c) && c.stats is IFOBStats f)
                    f.AddFOBPlaced();
                if (team == 1)
                {
                    ctf.GameStats.fobsPlacedT1++;
                }
                else if (team == 2)
                {
                    ctf.GameStats.fobsPlacedT2++;
                }
            }
            if (team == 1)
            {
                for (int i = 0; i < Team1FOBs.Count; i++)
                {
                    if (Team1FOBs[i].Number != i + 1)
                    {
                        Team1FOBs.Insert(i, new FOB("FOB" + (i + 1).ToString(Data.Locale), i + 1, Structure));
                        return;
                    }
                }

                Team1FOBs.Add(new FOB("FOB" + (Team1FOBs.Count + 1).ToString(Data.Locale), Team1FOBs.Count + 1, Structure));
            }
            else if (team == 2)
            {
                for (int i = 0; i < Team2FOBs.Count; i++)
                {
                    if (Team2FOBs[i].Number != i + 1)
                    {
                        Team2FOBs.Insert(i, new FOB("FOB" + (i + 1).ToString(Data.Locale), i + 1, Structure));
                        return;
                    }
                }

                Team2FOBs.Add(new FOB("FOB" + (Team2FOBs.Count + 1).ToString(Data.Locale), Team2FOBs.Count + 1, Structure));
            }

            UpdateUIForTeam(team);
        }
        public static void RegisterNewSpecialFOB(string name, Vector3 point, ulong team, string UIcolor, bool disappearAroundEnemies)
        {
            SpecialFOBs.Add(new SpecialFOB(name, point, team, UIcolor, disappearAroundEnemies));

            UpdateUIForTeam(team);
        }

        public static void DeleteFOB(uint instanceID, ulong team, ulong player)
        {
            FOB removed;
            if (team == 1)
            {
                removed = Team1FOBs.FirstOrDefault(x => x.Structure.instanceID == instanceID);
                Team1FOBs.RemoveAll(f => f.Structure.instanceID == instanceID);
            }
            else if (team == 2)
            {
                removed = Team2FOBs.FirstOrDefault(x => x.Structure.instanceID == instanceID);
                Team2FOBs.RemoveAll(f => f.Structure.instanceID == instanceID);
            }
            else removed = null;

            if (removed != null)
            {
                for (int i = 0; i < removed.nearbyPlayers.Count; i++)
                    EffectManager.askEffectClearByID(config.Data.BuildResourceUI, removed.nearbyPlayers[i].Player.channel.owner.transportConnection);

                IEnumerator<PlaytimeComponent> pts = Data.PlaytimeComponents.Values.GetEnumerator();
                while (pts.MoveNext())
                {
                    if (pts.Current.PendingFOB is FOB fob && fob.Number == removed.Number)
                    {
                        pts.Current.CancelTeleport();
                    }
                }
            }
            if (Data.Is(out IWarstatsGamemode w) && w.GameStats != null && w.State == Gamemodes.EState.ACTIVE)
            // doesnt count destroying fobs after game ends
            {
                if (F.TryGetPlaytimeComponent(player, out PlaytimeComponent c) && c.stats is IFOBStats f)
                    f.AddFOBDestroyed();
                if (team == 1)
                {
                    w.GameStats.fobsDestroyedT2++;
                }
                else if (team == 2)
                {
                    w.GameStats.fobsDestroyedT1++;
                }
            }
            UCPlayer ucplayer = UCPlayer.FromID(player);
            if (ucplayer != null)
            {
                if (ucplayer.GetTeam() == team)
                {
                    XP.XPManager.AddXP(ucplayer.Player, XP.XPManager.config.Data.FOBTeamkilledXP, F.Translate("xp_fob_teamkilled", player));
                }
                else
                {
                    XP.XPManager.AddXP(ucplayer.Player, XP.XPManager.config.Data.FOBKilledXP, F.Translate("xp_fob_killed", player));
                    Stats.StatsManager.ModifyStats(player, x => x.FobsDestroyed++, false);
                    Stats.StatsManager.ModifyTeam(team, t => t.FobsDestroyed++, false);
                }
            }
            UpdateUIForTeam(team);
        }
        public static void DeleteSpecialFOB(string name, ulong team)
        {
            SpecialFOB removed = SpecialFOBs.FirstOrDefault(x => x.Name == name && x.Team == team);
            SpecialFOBs.Remove(removed);

            if (removed != null)
            {
                IEnumerator<PlaytimeComponent> pts = Data.PlaytimeComponents.Values.GetEnumerator();
                while (pts.MoveNext())
                {
                    if (pts.Current.PendingFOB is SpecialFOB special)
                    {
                        pts.Current.CancelTeleport();
                    }
                }
            }

            UpdateUIForTeam(team);
        }

        public static void GetRegionBarricadeLists(
                out List<BarricadeDrop> Team1Barricades,
                out List<BarricadeDrop> Team2Barricades
                )
        {
            IEnumerable<BarricadeRegion> barricadeRegions = BarricadeManager.regions.Cast<BarricadeRegion>();

            List<BarricadeDrop> barricadeDrops = barricadeRegions.SelectMany(brd => brd.drops).ToList();

            Team1Barricades = barricadeDrops.Where(b =>
                b.GetServersideData().barricade.id == config.Data.FOBID &&   // All barricades that are FOB Structures
                TeamManager.IsTeam1(b.GetServersideData().group)        // All barricades that are friendly
                ).ToList();
            Team2Barricades = barricadeDrops.Where(b =>
                b.GetServersideData().barricade.id == config.Data.FOBID &&   // All barricades that are FOB Structures
                TeamManager.IsTeam2(b.GetServersideData().group)        // All barricades that are friendly
                ).ToList();
        }

        public static bool FindFOBByName(string name, ulong team, out object fob)
        {
            fob = SpecialFOBs.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && f.Team == team);
            if (fob != null)
                return true;

            if (team == 1)
            {
                fob = Team1FOBs.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                return fob != null;
            }
            else if (team == 2)
            {
                fob = Team2FOBs.Find(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                return fob != null;
            }
            fob = null;
            return false;
        }

        public static void UpdateUI(UCPlayer player)
        {
            List<FOB> FOBList;
            ulong team = player.GetTeam();
            if (team == 1)
            {
                FOBList = Team1FOBs;
            }
            else if (team == 2)
            {
                FOBList = Team2FOBs;
            }
            else return;


            for (int i = 0; i < Math.Min(SpecialFOBs.Count, config.Data.FobLimit); i++)
            {
                EffectManager.askEffectClearByID(unchecked((ushort)(config.Data.FirstFOBUiId + i)), player.Player.channel.owner.transportConnection);
            }

            int start = 0;
            for (int i = start; i < Math.Min(SpecialFOBs.Count, config.Data.FobLimit); i++)
            {
                if (SpecialFOBs[i].IsActive && SpecialFOBs[i].Team == team)
                {
                    string name = $"<color={SpecialFOBs[i].UIColor}>{SpecialFOBs[i].Name}</color>";
                    EffectManager.sendUIEffect(unchecked((ushort)(config.Data.FirstFOBUiId + i)), unchecked((short)(config.Data.FirstFOBUiId + i)),
                    player.Player.channel.owner.transportConnection, true, F.Translate("fob_ui", player.Steam64, name, SpecialFOBs[i].ClosestLocation));
                    start++;
                }
            }
            for (int i = start; i < Math.Min(FOBList.Count, config.Data.FobLimit); i++)
            {
                string name = FOBList[i].nearbyEnemies.Count == 0 ? $"<color=#54e3ff>{FOBList[i].Name}</color>" : $"<color=#ff8754>{FOBList[i].Name}</color>";

                EffectManager.sendUIEffect(unchecked((ushort)(config.Data.FirstFOBUiId + i)), unchecked((short)(config.Data.FirstFOBUiId + i)),
                player.Player.channel.owner.transportConnection, true, F.Translate("fob_ui", player.Steam64, name, FOBList[i].ClosestLocation));
            }
        }
        public static void UpdateUIAll()
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                UpdateUI(player);
            }
        }
        public static void UpdateUIForTeam(ulong team)
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers.Where(p => p.GetTeam() == team))
            {
                UpdateUI(player);
            }
        }
    }

    public class FOB
    {
        public string Name;
        public int Number;
        public BarricadeDrop Structure;
        public string ClosestLocation;
        public List<UCPlayer> nearbyPlayers;
        public List<UCPlayer> nearbyEnemies;
        public FOB(string Name, int number, BarricadeDrop Structure)
        {
            this.Name = Name;
            Number = number;
            this.Structure = Structure;
            ClosestLocation =
                (LevelNodes.nodes
                .Where(n => n.type == ENodeType.LOCATION)
                .Aggregate((n1, n2) =>
                    (n1.point - Structure.model.position).sqrMagnitude <= (n2.point - Structure.model.position).sqrMagnitude ? n1 : n2) as LocationNode)
                .name;
            nearbyPlayers = new List<UCPlayer>();
            nearbyEnemies = new List<UCPlayer>();
        }
    }

    public class SpecialFOB
    {
        public string Name;
        public Vector3 Point;
        public string ClosestLocation;
        public ulong Team;
        public string UIColor;
        public bool IsActive;
        public bool DisappearAroundEnemies;

        public SpecialFOB(string name, Vector3 point, ulong team, string color, bool disappearAroundEnemies)
        {
            Name = name;
            ClosestLocation =
                (LevelNodes.nodes
                .Where(n => n.type == ENodeType.LOCATION)
                .Aggregate((n1, n2) =>
                    (n1.point - point).sqrMagnitude <= (n2.point - point).sqrMagnitude ? n1 : n2) as LocationNode)
                .name;
            Team = team;
            Point = point;
            UIColor = color;
            IsActive = true;
            DisappearAroundEnemies = disappearAroundEnemies;
        }
    }

    public class FOBConfig : ConfigData
    {
        public ushort Team1BuildID;
        public ushort Team2BuildID;
        public ushort Team1AmmoID;
        public ushort Team2AmmoID;
        public ushort FOBBaseID;
        public float FOBMaxHeightAboveTerrain;
        public bool RestrictFOBPlacement;
        public ushort FOBID;
        public ushort FOBRequiredBuild;
        public int FOBBuildPickupRadius;
        public byte FobLimit;

        public float AmmoCommandCooldown;
        public ushort AmmoCrateBaseID;
        public ushort AmmoCrateID;
        public ushort AmmoCrateRequiredBuild;
        public ushort RepairStationBaseID;
        public ushort RepairStationID;
        public ushort RepairStationRequiredBuild;
        public ushort MortarID;
        public ushort MortarBaseID;
        public ushort MortarRequiredBuild;
        public ushort MortarShellID;

        public List<Emplacement> Emplacements;
        public List<Fortification> Fortifications;
        public List<ushort> LogiTruckIDs;
        public List<ushort> AmmoBagIDs;
        public int AmmoBagMaxUses;

        public float DeloyMainDelay;
        public float DeloyFOBDelay;

        public bool EnableCombatLogger;
        public uint CombatCooldown;

        public bool EnableDeployCooldown;
        public uint DeployCooldown;
        public bool DeployCancelOnMove;
        public bool DeployCancelOnDamage;

        public bool ShouldRespawnAtMain;
        public bool ShouldWipeAllFOBsOnRoundedEnded;
        public bool ShouldSendPlayersBackToMainOnRoundEnded;
        public bool ShouldKillMaincampers;

        public ushort FirstFOBUiId;
        public ushort BuildResourceUI;

        public override void SetDefaults()
        {
            Team1BuildID = 38312;
            Team2BuildID = 38313;
            Team1AmmoID = 38314;
            Team2AmmoID = 38315;
            FOBBaseID = 38310;
            FOBMaxHeightAboveTerrain = 25f;
            RestrictFOBPlacement = true;
            FOBID = 38311;
            FOBRequiredBuild = 15;
            FOBBuildPickupRadius = 80;
            FobLimit = 10;

            AmmoCrateBaseID = 38316;
            AmmoCrateID = 38317;
            AmmoCrateRequiredBuild = 2;
            AmmoCommandCooldown = 0f;

            RepairStationBaseID = 38318;
            RepairStationID = 38319;
            RepairStationRequiredBuild = 6;

            LogiTruckIDs = new List<ushort>() { 38305, 38306, 38311, 38312 };
            AmmoBagIDs = new List<ushort>() { 38398 };
            AmmoBagMaxUses = 3;

            Fortifications = new List<Fortification>() {
                new Fortification
                {
                    base_id = 38350,
                    barricade_id = 38351,
                    required_build = 1
                },
                new Fortification
                {
                    base_id = 38352,
                    barricade_id = 38353,
                    required_build = 1
                },
                new Fortification
                {
                    base_id = 38354,
                    barricade_id = 38355,
                    required_build = 1
                },
                new Fortification
                {
                    base_id = 38356,
                    barricade_id = 38357,
                    required_build = 2
                },
                new Fortification
                {
                    base_id = 38358,
                    barricade_id = 38359,
                    required_build = 1
                },
                new Fortification
                {
                    base_id = 38360,
                    barricade_id = 38361,
                    required_build = 3
                },
                new Fortification
                {
                    base_id = 38362,
                    barricade_id = 38363,
                    required_build = 3
                }
            };

            Emplacements = new List<Emplacement>() {
                new Emplacement
                {
                    baseID = 38345,
                    vehicleID = 38316,
                    ammoID = 38302,
                    ammoAmount = 2,
                    requiredBuild = 4
                },
                new Emplacement
                {
                    baseID = 38346,
                    vehicleID = 38317,
                    ammoID = 38305,
                    ammoAmount = 2,
                    requiredBuild = 4
                },
                new Emplacement
                {
                    baseID = 38342,
                    vehicleID = 38315,
                    ammoID = 38341,
                    ammoAmount = 1,
                    requiredBuild = 8
                },
                new Emplacement
                {
                    baseID = 38339,
                    vehicleID = 38314,
                    ammoID = 38338,
                    ammoAmount = 1,
                    requiredBuild = 8
                },
                new Emplacement
                {
                    baseID = 38336,
                    vehicleID = 38313,
                    ammoID = 38330,
                    ammoAmount = 3,
                    requiredBuild = 6
                },
            };

            DeloyMainDelay = 3;
            DeloyFOBDelay = 10;

            DeployCancelOnMove = true;
            DeployCancelOnDamage = true;

            ShouldRespawnAtMain = true;
            ShouldSendPlayersBackToMainOnRoundEnded = true;
            ShouldWipeAllFOBsOnRoundedEnded = true;
            ShouldKillMaincampers = true;

            FirstFOBUiId = 36020;
            BuildResourceUI = 36090;
        }

        public FOBConfig() { }
    }

    public class Emplacement
    {
        public ushort vehicleID;
        public ushort baseID;
        public ushort ammoID;
        public ushort ammoAmount;
        public ushort requiredBuild;
    }

    public class Fortification
    {
        public ushort barricade_id;
        public ushort base_id;
        public ushort required_build;
    }
}
