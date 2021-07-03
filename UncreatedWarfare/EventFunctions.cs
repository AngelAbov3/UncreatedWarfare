﻿using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.XP;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare
{
    public static class EventFunctions
    {
        public delegate Task GroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup);
        public static event GroupChanged OnGroupChanged;
        internal static async Task OnGroupChangedInvoke(SteamPlayer player, ulong oldGroup, ulong newGroup) => await OnGroupChanged?.Invoke(player, oldGroup, newGroup);
        internal static async Task GroupChangedAction(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            ulong oldteam = oldGroup.GetTeam();
            ulong newteam = newGroup.GetTeam();

            PlayerManager.VerifyTeam(player.player);
            await Data.Gamemode?.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);


            SquadManager.ClearUIsquad(player.player);
            SquadManager.UpdateUIMemberCount(newGroup);
            TicketManager.OnGroupChanged(player, oldGroup, newGroup);
            FOBManager.UpdateUI(UCPlayer.FromSteamPlayer(player));

            await rtn;
            await XPManager.OnGroupChanged(player, oldGroup, newGroup);
            await OfficerManager.OnGroupChanged(player, oldGroup, newGroup);
        }
        internal static void OnStructureDestroyed(StructureRegion region, StructureData data, StructureDrop drop, uint instanceID)
        {
            Data.VehicleSpawner.OnStructureDestroyed(region, data, drop, instanceID);
        }
        internal static void OnBarricadeDestroyed(BarricadeRegion region, BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant, ushort index)
        {
            if (Data.OwnerComponents != null)
            {
                int c = Data.OwnerComponents.FindIndex(x => x != null && x.transform != default && data != default && x.transform.position == data.point);
                if (c != -1)
                {
                    UnityEngine.Object.Destroy(Data.OwnerComponents[c]);
                    Data.OwnerComponents.RemoveAt(c);
                }
            }
            FOBManager.OnBarricadeDestroyed(region, data, drop, instanceID, plant, index);
            RallyManager.OnBarricadeDestroyed(region, data, drop, instanceID, plant, index);
            RepairManager.OnBarricadeDestroyed(region, data, drop, instanceID, plant, index);
            Data.VehicleSpawner.OnBarricadeDestroyed(region, data, drop, instanceID, plant, index);
            Data.VehicleSigns.OnBarricadeDestroyed(region, data, drop, instanceID, plant, index);
        }
        internal static void StopCosmeticsToggleEvent(ref EVisualToggleType type, SteamPlayer player, ref bool allow)
        {
            if (!UCWarfare.Config.AllowCosmetics) allow = UnturnedPlayer.FromSteamPlayer(player).OnDuty();
        }
        internal static void StopCosmeticsSetStateEvent(ref EVisualToggleType type, SteamPlayer player, ref bool state, ref bool allow)
        {
            if (!UCWarfare.Config.AllowCosmetics && UnturnedPlayer.FromSteamPlayer(player).OffDuty()) state = false;
        }
        internal static void OnBarricadePlaced(BarricadeRegion region, BarricadeData data, ref Transform location)
        {
            F.Log("Placed barricade: " + data.barricade.asset.itemName + ", " + location.position.ToString());
            BarricadeOwnerDataComponent c = location.gameObject.AddComponent<BarricadeOwnerDataComponent>();
            c.SetData(data, region, location);
            Data.OwnerComponents.Add(c);
            RallyManager.OnBarricadePlaced(region, data, ref location);
            RepairManager.OnBarricadePlaced(region, data, ref location);
        }
        internal static async void OnLandmineExploded(InteractableTrap trap, Collider collider, BarricadeOwnerDataComponent owner)
        {
            if (owner == default || owner.owner == default)
            {
                if (owner == default || owner.ownerID == 0) return;
                FPlayerName usernames = await Data.DatabaseManager.GetUsernames(owner.ownerID);
                F.Log(usernames.PlayerName + "'s landmine exploded");
                return;
            }
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            if (F.TryGetPlaytimeComponent(owner.owner.player, out PlaytimeComponent c))
                c.LastLandmineExploded = new LandmineDataForPostAccess(trap, owner);
            F.Log(F.GetPlayerOriginalNames(owner.owner).PlayerName + "'s landmine exploded");
            await rtn;
        }
        internal static void ThrowableSpawned(UseableThrowable useable, GameObject throwable)
        {
            ThrowableOwnerDataComponent t = throwable.AddComponent<ThrowableOwnerDataComponent>();
            PlaytimeComponent c = F.GetPlaytimeComponent(useable.player, out bool success);
            t.Set(useable, throwable, c);
            if (success)
                c.thrown.Add(t);
        }
        internal static void ProjectileSpawned(UseableGun gun, GameObject projectile)
        {
            PlaytimeComponent c = F.GetPlaytimeComponent(gun.player, out bool success);
            if (success)
            {
                c.lastProjected = gun.equippedGunAsset.id;
            }
        }
        internal static void BulletSpawned(UseableGun gun, BulletInfo bullet)
        {
            PlaytimeComponent c = F.GetPlaytimeComponent(gun.player, out bool success);
            if (success)
            {
                c.lastShot = gun.equippedGunAsset.id;
            }
        }
        internal static async Task ReloadCommand_onTranslationsReloaded()
        {
            foreach (SteamPlayer player in Provider.clients)
                await UCWarfare.I.UpdateLangs(player);
        }
        internal static void OnBarricadeTryPlaced(Barricade barricade, ItemBarricadeAsset asset, Transform hit, ref Vector3 point, ref float angle_x,
            ref float angle_y, ref float angle_z, ref ulong owner, ref ulong group, ref bool shouldAllow)
        {
            if (hit != null && hit.transform.CompareTag("Vehicle"))
            {
                if (!UCWarfare.Config.AdminLoggerSettings.AllowedBarricadesOnVehicles.Contains(asset.id))
                {
                    UnturnedPlayer player = UnturnedPlayer.FromCSteamID(new CSteamID(owner));
                    if (player != null && player.OffDuty())
                    {
                        shouldAllow = false;
                        player.SendChat("no_placement_on_vehicle", UCWarfare.GetColor("defaulterror"), asset.itemName, asset.itemName.An());
                    }
                }
            }
            if (shouldAllow)
                RallyManager.OnBarricadePlaceRequested(barricade, asset, hit, ref point, ref angle_x, ref angle_y, ref angle_z, ref owner, ref group, ref shouldAllow);
        }
        internal static void OnPostHealedPlayer(Player instigator, Player target)
        {
            Data.ReviveManager.OnPlayerHealed(instigator, target);
        }
        internal static async void OnPostPlayerConnected(UnturnedPlayer player)
        {
            FPlayerName names = F.GetPlayerOriginalNames(player);
            PlayerManager.InvokePlayerConnected(player); // must always be first
            UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);
            await OfficerManager.OnPlayerJoined(ucplayer);
            await XPManager.OnPlayerJoined(ucplayer);
            await Client.SendPlayerJoined(names);
            await Data.DatabaseManager.CheckUpdateUsernames(names);
            bool FIRST_TIME = !await Data.DatabaseManager.HasPlayerJoined(player.Player.channel.owner.playerID.steamID.m_SteamID);
            await Data.DatabaseManager.RegisterLogin(player.Player);
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            F.Broadcast("player_connected", UCWarfare.GetColor("join_message_background"), player.Player.channel.owner.playerID.playerName, UCWarfare.GetColorHex("join_message_name"));
            if (Data.PlaytimeComponents.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
            {
                UnityEngine.Object.DestroyImmediate(Data.PlaytimeComponents[player.Player.channel.owner.playerID.steamID.m_SteamID]);
                Data.PlaytimeComponents.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            }
            PlaytimeComponent pt = player.Player.transform.gameObject.AddComponent<PlaytimeComponent>();
            pt.StartTracking(player.Player);
            Data.PlaytimeComponents.Add(player.Player.channel.owner.playerID.steamID.m_SteamID, pt);
            pt.UCPlayerStats?.LogIn(player.Player.channel.owner, names);
            ToastMessage.QueueMessage(player, F.Translate(FIRST_TIME ? "welcome_message_first_time" : "welcome_message", player, 
                UCWarfare.GetColorHex("uncreated"), names.CharacterName, TeamManager.GetTeamHexColor(player.GetTeam()) ), ToastMessageSeverity.INFO);
            if (!UCWarfare.Config.AllowCosmetics)
            {
                player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.COSMETIC, false);
                player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.MYTHIC, false);
                player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.SKIN, false);
            }
            if (UCWarfare.Config.ModifySkillLevels)
            {
                player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.SHARPSHOOTER, 7);
                player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.PARKOUR, 3);
            }
            Data.ReviveManager.OnPlayerConnected(player);

            TicketManager.OnPlayerJoined(ucplayer);

            await Data.Gamemode.OnPlayerJoined(player.Player.channel.owner);
            await rtn;
        }
        internal static void OnTryStoreItem(Player player, byte page, ItemJar jar, ref bool allow)
        {
            if (!player.inventory.isStoring) return;
            UnturnedPlayer utplayer = UnturnedPlayer.FromPlayer(player);
            if (utplayer.OnDuty())
                return;
            if (!Whitelister.IsWhitelisted(jar.item.id, out _))
            {
                allow = false;
                player.SendChat("cant_store_this_item", UCWarfare.GetColor("cant_store_this_item"),
                    !(Assets.find(EAssetType.ITEM, jar.item.id) is ItemAsset asset) || asset.itemName == null ? jar.item.id.ToString(Data.Locale) : asset.itemName, UCWarfare.GetColorHex("cant_store_this_item_item"));
            }
        }
        internal static void StructureMovedInWorkzone(CSteamID instigator, byte x, byte y, uint instanceID, ref Vector3 point, ref byte angle_x, ref byte angle_y, ref byte angle_z, ref bool shouldAllow)
        {
            if (Structures.StructureSaver.StructureExists(instanceID, Structures.EStructType.STRUCTURE, out Structures.Structure found))
            {
                found.transform = new SerializableTransform(new SerializableVector3(point), new SerializableVector3(angle_x * 2f, angle_y * 2f, angle_z * 2f));
                Structures.StructureSaver.Save();
                if (Vehicles.VehicleSpawner.IsRegistered(instanceID, out Vehicles.VehicleSpawn spawn, Structures.EStructType.STRUCTURE))
                {
                    List<Vehicles.VehicleSign> linked = Vehicles.VehicleSigns.GetLinkedSigns(spawn);
                    if (linked.Count > 0)
                    {
                        for (int i = 0; i < linked.Count; i++)
                        {
                            linked[i].bay_transform = found.transform;
                        }
                        Vehicles.VehicleSigns.Save();
                    }
                }
            }
        }
        internal static void BarricadeMovedInWorkzone(CSteamID instigator, byte x, byte y, ushort plant, uint instanceID, ref Vector3 point, ref byte angle_x, ref byte angle_y, ref byte angle_z, ref bool shouldAllow)
        {
            if (Structures.StructureSaver.StructureExists(instanceID, Structures.EStructType.BARRICADE, out Structures.Structure found))
            {
                found.transform = new SerializableTransform(new SerializableVector3(point), new SerializableVector3(angle_x * 2f, angle_y * 2f, angle_z * 2f));
                Structures.StructureSaver.Save();
                if (Vehicles.VehicleSpawner.IsRegistered(instanceID, out Vehicles.VehicleSpawn spawn, Structures.EStructType.BARRICADE))
                {
                    List<Vehicles.VehicleSign> linked = Vehicles.VehicleSigns.GetLinkedSigns(spawn);
                    if (linked.Count > 0)
                    {
                        for (int i = 0; i < linked.Count; i++)
                        {
                            linked[i].bay_transform = found.transform;
                        }
                        Vehicles.VehicleSigns.Save();
                    }
                }
            }
            F.GetBarricadeFromInstID(instanceID, out BarricadeDrop drop);
            if (drop != default)
            {
                if (drop.model.TryGetComponent(out InteractableSign sign))
                {
                    if (Vehicles.VehicleSigns.SignExists(sign, out Vehicles.VehicleSign vbsign))
                    {
                        vbsign.sign_transform = new SerializableTransform(new SerializableVector3(point), new SerializableVector3(angle_x * 2f, angle_y * 2f, angle_z * 2f));
                        Vehicles.VehicleSigns.Save();
                    }
                }
            }
        }
        internal static void OnPlayerLeavesVehicle(Player player, InteractableVehicle vehicle, ref bool shouldAllow, ref Vector3 pendingLocation, ref float pendingYaw)
        {
            if (shouldAllow)
                Vehicles.VehicleSpawner.OnPlayerLeaveVehicle(player, vehicle);
        }
        internal static void BatteryStolen(SteamPlayer theif, ref bool allow)
        {
            if (!UCWarfare.Config.AllowBatteryStealing)
            {
                allow = false;
                theif.SendChat("cant_steal_batteries", UCWarfare.GetColor("cant_steal_batteries"));
            }
        }
        internal static void OnCalculateSpawnDuringRevive(PlayerLife sender, bool wantsToSpawnAtHome, ref Vector3 position, ref float yaw)
        {
            ulong team = sender.player.GetTeam();
            position = team.GetBaseSpawnFromTeam();
            yaw = team.GetBaseAngle();
        }
        internal static async void OnPlayerDisconnected(UnturnedPlayer player)
        {
            UCPlayer ucplayer = UCPlayer.FromUnturnedPlayer(player);

            if (Data.OriginalNames.TryGetValue(player.Player.channel.owner.playerID.steamID.m_SteamID, out FPlayerName names))
            {
                await Client.SendPlayerLeft(names);
                if (player.OnDuty())
                {
                    if (player.IsAdmin())
                        Commands.DutyCommand.AdminOnToOff(player, names);
                    else if (player.IsIntern())
                        Commands.DutyCommand.InternOnToOff(player, names);
                }
                Data.OriginalNames.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            }
            await XPManager.OnPlayerLeft(ucplayer);
            await OfficerManager.OnPlayerLeft(ucplayer);
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            if (Data.OriginalNames.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                F.Broadcast("player_disconnected", UCWarfare.GetColor("leave_message_background"), player.Player.channel.owner.playerID.playerName, UCWarfare.GetColorHex("leave_message_name"));
            if (UCWarfare.Config.RemoveLandminesOnDisconnect)
            {
                IEnumerable<BarricadeOwnerDataComponent> ownedTraps = Data.OwnerComponents.Where(x => x != null && x.ownerID == player.CSteamID.m_SteamID
               && x.barricade?.asset?.type == EItemType.TRAP);
                foreach (BarricadeOwnerDataComponent comp in ownedTraps.ToList())
                {
                    if (comp == null) continue;
                    if (BarricadeManager.tryGetInfo(comp.barricadeTransform, out byte x, out byte y, out ushort plant, out ushort index, out BarricadeRegion region))
                    {
                        BarricadeManager.destroyBarricade(region, x, y, plant, index);
                        F.Log($"Removed {player.DisplayName}'s {comp.barricade.asset.itemName} at {x}, {y}", ConsoleColor.Green);
                    }
                    UnityEngine.Object.Destroy(comp);
                    Data.OwnerComponents.Remove(comp);
                }
            }
            if (F.TryGetPlaytimeComponent(player.Player, out PlaytimeComponent c))
            {
                UnityEngine.Object.Destroy(c);
                Data.PlaytimeComponents.Remove(player.CSteamID.m_SteamID);
            }
            Data.ReviveManager.OnPlayerDisconnected(player);
            TicketManager.OnPlayerLeft(ucplayer);
            PlayerManager.InvokePlayerDisconnected(player);

            await Data.Gamemode?.OnPlayerLeft(player.Player.channel.owner);
            await rtn;
        }
        internal static async Task LangCommand_OnPlayerChangedLanguage(UnturnedPlayer player, LanguageAliasSet oldSet, LanguageAliasSet newSet)
            => await UCWarfare.I.UpdateLangs(player.Player.channel.owner);

        internal static void OnPrePlayerConnect(ValidateAuthTicketResponse_t ticket, ref bool isValid, ref string explanation)
        {
            SteamPending player = Provider.pending.FirstOrDefault(x => x.playerID.steamID.m_SteamID == ticket.m_SteamID.m_SteamID);
            if (player == default(SteamPending)) return;
            F.Log(player.playerID.playerName);
            if (Data.OriginalNames.ContainsKey(player.playerID.steamID.m_SteamID))
                Data.OriginalNames[player.playerID.steamID.m_SteamID] = new FPlayerName(player.playerID);
            else
                Data.OriginalNames.Add(player.playerID.steamID.m_SteamID, new FPlayerName(player.playerID));
            ulong team = 0;
            if (PlayerManager.HasSave(player.playerID.steamID.m_SteamID, out var save))
            {
                team = save.Team;
            }
            F.Log("PLAYER TEAM: " + team);

            string globalPrefix = "";
            string teamPrefix = "";

            // add team tags to global prefix
            if (TeamManager.IsTeam1(team)) globalPrefix += $"{TeamManager.Team1Code.ToUpper()}-";
            else if (TeamManager.IsTeam2(team)) globalPrefix += $"{TeamManager.Team2Code.ToUpper()}-";

            int xp = XPManager.GetXP(player.playerID.steamID.m_SteamID, team, true).GetAwaiter().GetResult();
            int stars = 0;

            Rank rank = null;

            if (OfficerManager.IsOfficer(player.playerID.steamID, out var officer))
            {
                rank = OfficerManager.GetOfficerRank(officer.officerLevel);
                var officerPoints = OfficerManager.GetOfficerPoints(player.playerID.steamID.m_SteamID, team).GetAwaiter().GetResult();
                stars = OfficerManager.GetStars(officerPoints);
            }
            else
            {
                rank = XPManager.GetRank(xp, out _, out _);
            }

            if (TeamManager.IsTeam1(team) || TeamManager.IsTeam2(team))
            {
                globalPrefix += rank.abbreviation;
                teamPrefix += rank.abbreviation;

                //if (stars >= 3)
                //{
                //    globalPrefix.Replace('.', ' ');
                //    globalPrefix += stars.ToString() + ".";
                //    teamPrefix.Replace('.', ' ');
                //    teamPrefix += stars.ToString() + ".";
                //}

                globalPrefix += " ";
                teamPrefix += " ";

                player.playerID.characterName = globalPrefix + player.playerID.characterName;
                player.playerID.nickName = teamPrefix + player.playerID.nickName;
            }
        }
    }
}
