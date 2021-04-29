﻿using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UncreatedWarfare.Teams
{
    public class Team
    {
        public ulong ID { get; private set; }
        public string Name { get; protected set; }
        public string LocalizedName {
            get
            {
                if (ID == 1)
                    return F.Translate("team_1");
                else if (ID == 2)
                    return F.Translate("team_2");
                else if (ID == 3)
                    return F.Translate("team_3");
                else
                {
                    if (Name == null)
                        return ID.ToString();
                    else return Name;
                }
            }
        }
        public string TeamColorHex
        {
            get
            {
                if (ID == 1)
                    return UCWarfare.I.ColorsHex["team_1_color"];
                else if (ID == 2)
                    return UCWarfare.I.ColorsHex["team_2_color"];
                else return UCWarfare.I.ColorsHex["neutral_color"];
            }
        }
        public Color TeamColor
        {
            get
            {
                if (ID == 1)
                    return UCWarfare.I.Colors["team_1_color"];
                else if (ID == 2)
                    return UCWarfare.I.Colors["team_2_color"];
                else return UCWarfare.I.Colors["neutral_color"];
            }
        }
        public List<ulong> OfflinePlayers { get; private set; }
        public List<SteamPlayer> OnlinePlayers { get; private set; }
        public Vector3 Spawnpoint { get; private set; }
        public GroupInfo GMInfo;
        public Team(TeamData team, bool DummyGroup = false)
        {
            this.ID = team.team_id;
            this.Name = team.name;
            this.OfflinePlayers = new List<ulong>();
            this.OnlinePlayers = new List<SteamPlayer>();
            this.Spawnpoint = new Vector3(team.spawnpoint_x, team.spawnpoint_y, team.spawnpoint_z);
            foreach (ulong player in team.players)
            {
                AddPlayer(player, false);
            }
            if(!DummyGroup)
            {
                CommandWindow.LogWarning("_0");
                CommandWindow.LogWarning(Name);
                CommandWindow.LogWarning(LocalizedName);
                CSteamID cid = new CSteamID(ID);
                //CommandWindow.LogWarning(Name);
                CommandWindow.LogWarning("_0.25");
                CommandWindow.LogWarning(cid.ToString());
                CommandWindow.LogWarning("_0.5");
                try
                {
                    CommandWindow.LogWarning("_1");
                    GMInfo = GroupManager.getGroupInfo(cid);
                    CommandWindow.LogWarning("_2");
                    if (GMInfo != null)
                    {
                        CommandWindow.LogWarning("_3");
                        CommandWindow.LogWarning($"GroupManager group for group {Name} : {LocalizedName} : {Name} ({ID}) found from previous save, loading into memory.");
                        CommandWindow.LogWarning("_4");
                    } else
                    {
                        CommandWindow.LogWarning("_5");
                        GMInfo = GroupManager.addGroup(cid, LocalizedName);
                        CommandWindow.LogWarning("_6");
                        CommandWindow.LogWarning(F.Translate("created_group_console", "Team -> TeamData()", Provider.server.m_SteamID, Name, ID.ToString()));
                        CommandWindow.LogWarning("_7");
                    }
                } 
                catch (NullReferenceException ex)
                {
                    CommandWindow.LogWarning($"Failed to create group.");
                }
            }
        }
        /// <summary>
        /// Add a player to the correct list (OfflinePlayers or OnlinePlayers).
        /// </summary>
        /// <returns><para>true: Player is online</para><para>false: Player is offline</para></returns>
        public bool AddPlayer(ulong PlayerID, bool UpdateJSON = true, bool forceOffline = false)
        {
            SteamPlayer player = PlayerTool.getSteamPlayer(PlayerID);
            if (player == null || forceOffline)
            {
                if (!OfflinePlayers.Contains(PlayerID))
                {
                    OfflinePlayers.Add(PlayerID);
                    if (UpdateJSON) 
                    { 
                        UCWarfare.I.TeamManager.AddPlayerToTeam(ID, PlayerID); 
                    }
                }
                return false;
            } else
            {
                if (!OnlinePlayers.Contains(player))
                {
                    OnlinePlayers.Add(player);
                    if (UpdateJSON)
                    {
                        UCWarfare.I.TeamManager.AddPlayerToTeam(ID, player.playerID.steamID.m_SteamID);
                    }
                }
                return true;
            }
        }
        /// <summary>
        /// Remove a player from the correct list (OfflinePlayers or OnlinePlayers).
        /// </summary>
        /// <returns><para>true: Player is online</para><para>false: Player is offline</para></returns>
        public bool RemovePlayer(ulong PlayerID, bool UpdateJSON = true)
        {
            SteamPlayer player = PlayerTool.getSteamPlayer(PlayerID);
            if (player == null)
            {
                if (OfflinePlayers.Contains(PlayerID))
                {
                    OfflinePlayers.Remove(PlayerID);
                    if (UpdateJSON)
                    {
                        UCWarfare.I.TeamManager.RemovePlayerFromTeam(ID, PlayerID);
                    }
                }
                return false;
            }
            else
            {
                if(OnlinePlayers.Contains(player))
                {
                    OnlinePlayers.Remove(player);
                    if (UpdateJSON)
                    {
                        UCWarfare.I.TeamManager.RemovePlayerFromTeam(ID, player.playerID.steamID.m_SteamID);
                    }
                }
                return true;
            }
        }
        ///<returns>true if the player was swapped to OfflinePlayers, false if the player wasn't in the list already.</returns>
        public bool PlayerGoOffline(SteamPlayer player)
        {
            bool removed = false;
            if (OnlinePlayers.Contains(player))
            {
                OnlinePlayers.Remove(player);
                removed = true;
            }
            if (!OfflinePlayers.Contains(player.playerID.steamID.m_SteamID))
                OfflinePlayers.Add(player.playerID.steamID.m_SteamID);
            return removed;
        }
        ///<returns>true if the player was swapped to OnlinePlayers, false if the player wasn't in the list already.</returns>
        public bool PlayerGoOnline(SteamPlayer player)
        {
            bool removed = false;
            if (OfflinePlayers.Contains(player.playerID.steamID.m_SteamID))
            {
                OfflinePlayers.Remove(player.playerID.steamID.m_SteamID);
                removed = true;
            }
            if (!OnlinePlayers.Contains(player))
                OnlinePlayers.Add(player);
            return removed;
        }
        public void ChangeName(string NewName, bool UpdateInJSON = true)
        {
            this.Name = NewName;
            if (UpdateInJSON)
                JSONMethods.RenameTeam(ID, NewName, out _);
        }
    }
}