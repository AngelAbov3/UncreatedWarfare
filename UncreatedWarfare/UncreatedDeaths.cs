﻿using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UncreatedWarfare.Components;
using UncreatedWarfare.Stats;
using UncreatedWarfare.Teams;
using UnityEngine;

namespace UncreatedWarfare
{
    partial class UCWarfare
    {
        public event Rocket.Unturned.Events.UnturnedPlayerEvents.PlayerDeath OnPlayerDeathPostMessages;
        public class TeamkillEventArgs : EventArgs
        {
            public Player killer;
            public Player dead;
            public Player LandmineLinkedNonOffender;
            public EDeathCause cause;
            public ushort item;
            public string itemName;
            public string key;
            public ELimb limb;
            public float distance;
            public override string ToString()
            {
                string msg;
                if(cause == EDeathCause.LANDMINE)
                {
                    if (LandmineLinkedNonOffender == null)
                    {
                        F.BroadcastLandmineDeath(key, F.GetPlayerOriginalNames(dead), dead.GetTeam(), F.GetPlayerOriginalNames(killer), killer.GetTeam(),
                            new FPlayerName() { CharacterName = "Unknown", NickName = "Unknown", PlayerName = "Unknown", Steam64 = 0 }, 0, limb, itemName, out msg, false);
                    }
                    else
                    {
                        F.BroadcastLandmineDeath(key, F.GetPlayerOriginalNames(dead), dead.GetTeam(), F.GetPlayerOriginalNames(killer), killer.GetTeam(),
                            F.GetPlayerOriginalNames(LandmineLinkedNonOffender), LandmineLinkedNonOffender.GetTeam(), limb, itemName, out msg, false);
                    }
                } else
                {
                    F.BroadcastDeath(key, cause, F.GetPlayerOriginalNames(dead), dead.GetTeam(), F.GetPlayerOriginalNames(killer), false, killer.GetTeam(), limb, itemName, distance, out msg, false);
                }
                return msg;
            }
        }
        public event EventHandler<TeamkillEventArgs> OnTeamkill;
        private void Teamkill(TeamkillEventArgs parameters)
        {
            F.Log(" __TEAMKILL__  -  " + parameters.ToString(), ConsoleColor.Blue);
            OnTeamkill?.Invoke(this, parameters);
        }
        public class KillEventArgs : EventArgs
        {
            public Player killer;
            public Player dead;
            public Player LandmineLinkedAssistant;
            public EDeathCause cause;
            public ushort item;
            public string itemName;
            public string key;
            public ELimb limb;
            public float distance;
            public override string ToString()
            {
                string msg;
                if (cause == EDeathCause.LANDMINE)
                {
                    if(LandmineLinkedAssistant == null)
                    {
                        F.BroadcastLandmineDeath(key, F.GetPlayerOriginalNames(dead), dead.GetTeam(), F.GetPlayerOriginalNames(killer), killer.GetTeam(),
                            new FPlayerName() { CharacterName = "Unknown", NickName = "Unknown", PlayerName = "Unknown", Steam64 = 0 }, 0, limb, itemName, out msg, false);
                    } else
                    {
                        F.BroadcastLandmineDeath(key, F.GetPlayerOriginalNames(dead), dead.GetTeam(), F.GetPlayerOriginalNames(killer), killer.GetTeam(),
                            F.GetPlayerOriginalNames(LandmineLinkedAssistant), LandmineLinkedAssistant.GetTeam(), limb, itemName, out msg, false);
                    }
                }
                else
                {
                    F.BroadcastDeath(key, cause, F.GetPlayerOriginalNames(dead), dead.GetTeam(), F.GetPlayerOriginalNames(killer), false, killer.GetTeam(), limb, itemName, distance, out msg, false);
                }
                return msg;
            }
        }
        public event EventHandler<KillEventArgs> OnKill;
        private void Kill(KillEventArgs parameters)
        {
            F.Log(" __KILL__  -  " + parameters.ToString(), ConsoleColor.Blue);
            OnKill?.Invoke(this, parameters);
        }
        public class SuicideEventArgs : EventArgs
        {

        }
        public event EventHandler<SuicideEventArgs> OnSuicide;
        public void Suicide(SuicideEventArgs parameters)
        {
            F.Log(" __SUICIDE__  -  " + parameters.ToString(), ConsoleColor.Blue);
            OnSuicide?.Invoke(this, parameters);
        }
        private void OnPlayerDeath(UnturnedPlayer dead, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            F.Log($"Received death: {dead.DisplayName}, {cause}, {limb}, {murderer}", ConsoleColor.Blue);
            if (cause == EDeathCause.LANDMINE)
            {
                SteamPlayer placer = PlayerTool.getSteamPlayer(murderer.m_SteamID);
                Player triggerer;
                FPlayerName placerName;
                FPlayerName triggererName;
                bool foundPlacer;
                bool foundTriggerer;
                ulong deadTeam = F.GetTeam(dead);
                ulong placerTeam;
                ulong triggererTeam;
                ushort landmineID;
                LandmineDataForPostAccess landmine;
                string landmineName;
                if (placer == null)
                {
                    placer = dead.Player.channel.owner;
                    placerName = new FPlayerName() { CharacterName = "Unknown", PlayerName = "Unknown", NickName = "Unknown", Steam64 = 0 };
                    foundPlacer = false;
                    landmineID = 0;
                    landmineName = "Unknown";
                    landmine = default;
                    placerTeam = 0;
                    triggererTeam = 0;
                    triggererName = new FPlayerName() { CharacterName = "Unknown", PlayerName = "Unknown", NickName = "Unknown", Steam64 = 0 };
                    triggerer = null;
                    foundTriggerer = false;
                }
                else
                {
                    placerName = F.GetPlayerOriginalNames(placer);
                    placerTeam = F.GetTeam(placer);
                    foundPlacer = true;
                    if (F.TryGetPlaytimeComponent(placer.player, out PlaytimeComponent c))
                    {
                        if (c.LastLandmineExploded.Equals(default)
                            || c.LastLandmineExploded.Equals(default) || c.LastLandmineExploded.owner == null)
                        {
                            landmine = default;
                            landmineID = 0;
                        }
                        else
                        {
                            landmine = c.LastLandmineExploded;
                            landmineID = c.LastLandmineExploded.barricadeID;
                        }
                    } else
                    {
                        landmineID = 0;
                        landmine = default;
                    }
                    if (landmineID != 0)
                    {
                        ItemAsset asset = (ItemAsset)Assets.find(EAssetType.ITEM, landmineID);
                        if (asset != null) landmineName = asset.itemName;
                        else landmineName = landmineID.ToString();
                    }
                    else landmineName = "Unknown";
                    if(!landmine.Equals(default))
                    {
                        KeyValuePair<ulong, PlaytimeComponent> pt = Data.PlaytimeComponents.FirstOrDefault(
                            x => 
                            x.Value != default && 
                            !x.Value.LastLandmineTriggered.Equals(default) && 
                            x.Value.LastLandmineTriggered.owner != default && 
                            landmine.barricadeInstId == x.Value.LastLandmineTriggered.barricadeInstId);
                        if (!pt.Equals(default))
                        {
                            triggerer = pt.Value.player;
                            triggererTeam = F.GetTeam(triggerer);
                            triggererName = F.GetPlayerOriginalNames(triggerer);
                            foundTriggerer = true;
                        } else
                        {
                            triggerer = null;
                            triggererTeam = 0;
                            triggererName = new FPlayerName() { CharacterName = "Unknown", PlayerName = "Unknown", NickName = "Unknown", Steam64 = 0 };
                            foundTriggerer = false;
                        }
                    } else
                    {
                        triggerer = null;
                        triggererTeam = 0;
                        triggererName = new FPlayerName() { CharacterName = "Unknown", PlayerName = "Unknown", NickName = "Unknown", Steam64 = 0 };
                        foundTriggerer = false;
                    }
                }
                string key = "LANDMINE";
                string itemkey = landmineID.ToString();
                if (foundPlacer && placer.playerID.steamID.m_SteamID == dead.CSteamID.m_SteamID)
                {
                    key += "_SUICIDE";
                }
                if(landmineID == 0)
                {
                    key += "_UNKNOWN";
                }
                if(foundTriggerer && triggerer.channel.owner.playerID.steamID.m_SteamID != dead.CSteamID.m_SteamID && triggerer.channel.owner.playerID.steamID.m_SteamID != placer.playerID.steamID.m_SteamID)
                {
                    key += "_TRIGGERED";
                }
                if(!foundPlacer)
                {
                    key += "_UNKNOWNKILLER";
                }
                LogLandmineMessage(key, dead.Player, placerName, placerTeam, limb, landmineName, triggererName, triggererTeam);
                if(foundPlacer && foundTriggerer)
                {
                    if (triggerer.channel.owner.playerID.steamID.m_SteamID == dead.CSteamID.m_SteamID && triggerer.channel.owner.playerID.steamID.m_SteamID == placer.playerID.steamID.m_SteamID)
                    {
                        Suicide(new SuicideEventArgs() { });
                    } else if (placerTeam == triggererTeam)
                    {
                        if(deadTeam == placerTeam)
                        {
                            Teamkill(new TeamkillEventArgs()
                            {
                                dead = dead.Player,
                                killer = triggerer,
                                cause = cause,
                                item = landmineID,
                                itemName = landmineName,
                                key = key,
                                limb = limb,
                                LandmineLinkedNonOffender = placer.player,
                                distance = 0
                            });
                        } else
                        {
                            Kill(new KillEventArgs()
                            {
                                dead = dead.Player,
                                killer = placer.player,
                                cause = cause,
                                item = landmineID,
                                itemName = landmineName,
                                key = key,
                                limb = limb,
                                LandmineLinkedAssistant = triggerer,
                                distance = 0
                            });
                        }
                    } 
                    else 
                    {
                        if(deadTeam == placerTeam) // and placer team != triggerer team
                        {
                            Kill(new KillEventArgs()
                            {
                                dead = dead.Player,
                                killer = triggerer,
                                cause = cause,
                                item = landmineID,
                                itemName = landmineName,
                                key = key,
                                limb = limb,
                                LandmineLinkedAssistant = placer.player,
                                distance = 0
                            });
                        } else // dead team == triggerer team
                        {
                            Kill(new KillEventArgs()
                            {
                                dead = dead.Player,
                                killer = placer.player,
                                cause = cause,
                                item = landmineID,
                                itemName = landmineName,
                                key = key,
                                limb = limb,
                                LandmineLinkedAssistant = triggerer,
                                distance = 0
                            });
                        }
                    }
                } else if (foundPlacer)
                {
                    if (dead.Player.channel.owner.playerID.steamID.m_SteamID == placer.playerID.steamID.m_SteamID)
                    {
                        Suicide(new SuicideEventArgs() { });
                    } else if (deadTeam == placerTeam)
                    {
                        Teamkill(new TeamkillEventArgs()
                        {
                            dead = dead.Player,
                            killer = placer.player,
                            cause = cause,
                            item = landmineID,
                            itemName = landmineName,
                            key = key,
                            limb = limb,
                            LandmineLinkedNonOffender = null,
                            distance = 0
                        });
                    }
                    else
                    {
                        Kill(new KillEventArgs()
                        {
                            dead = dead.Player,
                            killer = placer.player,
                            cause = cause,
                            item = landmineID,
                            itemName = landmineName,
                            key = key,
                            limb = limb,
                            LandmineLinkedAssistant = null,
                            distance = 0
                        });
                    }
                } else if (foundTriggerer)
                {
                    if (triggerer.channel.owner.playerID.steamID.m_SteamID == dead.CSteamID.m_SteamID)
                    {
                        Suicide(new SuicideEventArgs() { });
                    } else if (deadTeam == triggererTeam)
                    {
                        Teamkill(new TeamkillEventArgs()
                        {
                            dead = dead.Player,
                            killer = triggerer,
                            cause = cause,
                            item = landmineID,
                            itemName = landmineName,
                            key = key,
                            limb = limb,
                            LandmineLinkedNonOffender = null,
                            distance = 0
                        });
                    }
                    else
                    {
                        Kill(new KillEventArgs()
                        {
                            dead = dead.Player,
                            killer = triggerer,
                            cause = cause,
                            item = landmineID,
                            itemName = landmineName,
                            key = key,
                            limb = limb,
                            LandmineLinkedAssistant = null,
                            distance = 0
                        });
                    }
                }
            } else
            {
                SteamPlayer killer = PlayerTool.getSteamPlayer(murderer.m_SteamID);
                FPlayerName killerName;
                bool foundKiller;
                ushort item;
                string itemName;
                float distance = 0f;
                bool translateName = false;
                ulong killerTeam;
                bool itemIsVehicle = cause == EDeathCause.VEHICLE || cause == EDeathCause.ROADKILL;
                if (killer == null)
                {
                    killer = dead.Player.channel.owner;
                    if(cause == EDeathCause.ZOMBIE)
                    {
                        killerName = new FPlayerName() { CharacterName = "zombie", PlayerName = "zombie", NickName = "zombie", Steam64 = 0 };
                        killerTeam = TeamManager.ZombieTeamID;
                        translateName = true;
                    }
                    else
                    {
                        killerName = new FPlayerName() { CharacterName = "Unknown", PlayerName = "Unknown", NickName = "Unknown", Steam64 = 0 };
                        killerTeam = 0;
                    }
                    foundKiller = false;
                    item = 0;
                    itemName = "Unknown";
                }
                else
                {
                    killerName = F.GetPlayerOriginalNames(killer);
                    killerTeam = F.GetTeam(killer);
                    foundKiller = true;
                    try
                    {
                        if (Data.ReviveManager.DistancesFromInitialShot.ContainsKey(dead.CSteamID.m_SteamID))
                            distance = Data.ReviveManager.DistancesFromInitialShot[dead.CSteamID.m_SteamID];
                        else
                            distance = Vector3.Distance(killer.player.transform.position, dead.Position);
                    }
                    catch { }
                    PlaytimeComponent c = F.GetPlaytimeComponent(killer.player, out bool success);
                    if (success)
                    {
                        if (cause == EDeathCause.GUN && c.lastShot != default)
                            item = c.lastShot;
                        else if (cause == EDeathCause.GRENADE && c.thrown != default && c.thrown.Count > 0)
                            item = c.thrown.Last().asset.id;
                        else if (cause == EDeathCause.MISSILE && c.lastProjected != default)
                            item = c.lastProjected;
                        else if (cause == EDeathCause.VEHICLE && c.lastExplodedVehicle != default)
                            item = c.lastExplodedVehicle;
                        else if (cause == EDeathCause.ROADKILL && c.lastRoadkilledBy != default)
                            item = c.lastRoadkilledBy;
                        else item = killer.player.equipment.itemID;
                    }
                    else item = dead.Player.equipment.itemID;
                    if (item != 0)
                    {
                        if(itemIsVehicle)
                        {
                            VehicleAsset asset = (VehicleAsset)Assets.find(EAssetType.VEHICLE, item);
                            if (asset != null) itemName = asset.vehicleName;
                            else itemName = item.ToString();
                        } else
                        {
                            ItemAsset asset = (ItemAsset)Assets.find(EAssetType.ITEM, item);
                            if (asset != null) itemName = asset.itemName;
                            else itemName = item.ToString();
                        }
                    }
                    else itemName = "Unknown";
                }
                string key = cause.ToString();
                if (dead.CSteamID.m_SteamID == murderer.m_SteamID && cause != EDeathCause.SUICIDE) key += "_SUICIDE";
                if (cause == EDeathCause.ARENA && Data.DeathLocalization[JSONMethods.DefaultLanguage].ContainsKey("MAINCAMP")) key = "MAINCAMP";
                if ((cause == EDeathCause.GUN || cause == EDeathCause.MELEE || cause == EDeathCause.MISSILE || cause == EDeathCause.SPLASH || cause == EDeathCause.VEHICLE || cause == EDeathCause.ROADKILL) && foundKiller)
                {
                    if (item != 0)
                    {
                        string k1 = (itemIsVehicle ? "v" : "") + item.ToString();
                        string k2 = k1 + "_SUICIDE";
                        if (Data.DeathLocalization[JSONMethods.DefaultLanguage].ContainsKey(k1))
                        {
                            key = k1;
                        }
                        if (dead.CSteamID.m_SteamID == killer.playerID.steamID.m_SteamID && cause != EDeathCause.SUICIDE && Data.DeathLocalization[JSONMethods.DefaultLanguage].ContainsKey(k2))
                        {
                            key = k2;
                        }
                    }
                    else
                    {
                        key += "_UNKNOWN";
                    }
                    if (cause == EDeathCause.BLEEDING)
                    {
                        if (murderer == Provider.server)
                            key += "_SUICIDE";
                        else if (!murderer.m_SteamID.ToString().StartsWith("765"))
                            killerName = new FPlayerName() { CharacterName = "zombie", NickName = "zombie", PlayerName = "zombie", Steam64 = murderer == null || murderer == CSteamID.Nil ? 0 : murderer.m_SteamID };
                    }
                }
                LogDeathMessage(key, cause, dead.Player, killerName, translateName, killerTeam, limb, itemName, distance);
            }
            OnPlayerDeathPostMessages?.Invoke(dead, cause, limb, murderer);
        }
        private void LogDeathMessage(string key, EDeathCause backupcause, Player dead, FPlayerName killerName, bool translateName, ulong killerGroup, ELimb limb, string itemName, float distance)
        {
            F.Log(key, ConsoleColor.Blue);
            F.BroadcastDeath(key, backupcause, F.GetPlayerOriginalNames(dead), dead.GetTeam(), killerName, translateName, killerGroup, limb, itemName, distance, out string message, true);
            F.Log(message, ConsoleColor.Cyan);
        }
        private void LogLandmineMessage(string key, Player dead, FPlayerName killerName, ulong killerGroup, ELimb limb, string landmineName, FPlayerName triggererName, ulong triggererTeam)
        {
            F.Log(key, ConsoleColor.Blue);
            F.BroadcastLandmineDeath(key, F.GetPlayerOriginalNames(dead), dead.GetTeam(), killerName, killerGroup, triggererName, triggererTeam, limb, landmineName, out string message, true);
            F.Log(message, ConsoleColor.Cyan);
        }
    }
}
