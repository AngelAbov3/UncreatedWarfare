﻿using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Teams;
using UnityEngine;

namespace UncreatedWarfare.Flags
{
    public class PlayerEventArgs : EventArgs { public Player player; }
    public class CaptureChangeEventArgs : EventArgs { public int NewPoints; public int OldPoints; }
    public class OwnerChangeEventArgs : EventArgs { public ulong OldOwner; public ulong NewOwner; }
    public class Flag : IDisposable
    {
        public const int MaxPoints = 64;
        public Zone ZoneData { get; private set; }
        public FlagManager Manager { get; private set; }
        public int Level { get => _level; }
        private readonly int _level;
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                _x = _position.x;
                _y = _position.y;
                _z = _position.z;
                _position2d = new Vector2(_x, _z);
            }
        }
        private Vector3 _position;
        public Vector2 Position2D
        {
            get => _position2d;
            set
            {
                _position2d = value;
                _x = _position2d.x;
                _z = _position2d.y;
                _position = new Vector3(_x, _y, _z);
            }
        }
        private Vector2 _position2d;
        public int ID { get => _id; }
        private readonly int _id;
        public string Name { get => _name; }
        private readonly string _name;
        public float X
        {
            get => _x;
            set
            {
                _x = value;
                _position = new Vector3(_x, _y, _z);
                _position2d = new Vector2(_x, _z);
            }
        }
        public float Y
        {
            get => _y;
            set
            {
                _y = value;
                _position = new Vector3(_x, _y, _z);
            }
        }
        public float Z
        {
            get => _z;
            set
            {
                _z = value;
                _position = new Vector3(_x, _y, _z);
                _position2d = new Vector2(_x, _z);
            }
        }
        private float _x;
        private float _y;
        private float _z;
        public string ColorString { get => _color; set => _color = value; }
        private string _color;
        public Color Color { get => _color.Hex(); }
        public Color TeamSpecificColor
        {
            get
            {
                if (_owner == 1)
                    return UCWarfare.GetColor("team_1_color");
                else if (_owner == 2)
                    return UCWarfare.GetColor("team_2_color");
                else return UCWarfare.GetColor("neutral_color");
            }
        }
        public string TeamSpecificHexColor
        {
            get
            {
                if (_owner == 1)
                    return UCWarfare.GetColorHex("team_1_color");
                else if (_owner == 2)
                    return UCWarfare.GetColorHex("team_2_color");
                else return UCWarfare.GetColorHex("neutral_color");
            }
        }
        public void ResetFlag()
        {
            this.FullOwner = 0;
            this._owner = 0;
            OnReset?.Invoke(this, EventArgs.Empty);
        }
        public void Dispose()
        {
            OnDisposed?.Invoke(this, EventArgs.Empty);
            GC.SuppressFinalize(this);
        }
        private ulong _owner;
        public ulong Owner { get => _owner; set => _owner = value; }
        public float SizeX { get => _sizeX; set => _sizeX = value; }
        public float SizeZ { get => _sizeZ; set => _sizeZ = value; }
        private float _sizeX;
        private float _sizeZ;
        public List<Player> PlayersOnFlagTeam1;
        public int Team1TotalPlayers;
        public List<Player> PlayersOnFlagTeam2;
        public int Team2TotalPlayers;
        public void RecalcCappers(bool RecalcOnFlag = false) => RecalcCappers(Provider.clients, RecalcOnFlag);
        public void RecalcCappers(List<SteamPlayer> OnlinePlayers, bool RecalcOnFlag = true)
        {
            if (RecalcOnFlag)
            {
                PlayersOnFlag.Clear();
                foreach (SteamPlayer player in OnlinePlayers.Where(p => PlayerInRange(p)))
                    PlayersOnFlag.Add(player.player);
            }
            PlayersOnFlagTeam1 = PlayersOnFlag.Where(player => player.quests.groupID.m_SteamID == TeamManager.Team1ID).ToList();
            Team1TotalPlayers = PlayersOnFlagTeam1.Count;
            PlayersOnFlagTeam2 = PlayersOnFlag.Where(player => player.quests.groupID.m_SteamID == TeamManager.Team2ID).ToList();
            Team2TotalPlayers = PlayersOnFlagTeam2.Count;
        }
        /// <param name="NewPlayers">Players that have entered the flag since last check.</param>
        /// <returns>Players that have left the flag since last check.</returns>
        public List<Player> GetUpdatedPlayers(List<SteamPlayer> OnlinePlayers, out List<Player> NewPlayers)
        {
            List<Player> OldPlayers = PlayersOnFlag.ToList();
            RecalcCappers(OnlinePlayers, true);
            NewPlayers = PlayersOnFlag.Where(p => !OldPlayers.Exists(p2 => p.channel.owner.playerID.steamID.m_SteamID == p2.channel.owner.playerID.steamID.m_SteamID)).ToList();
            return OldPlayers.Where(p => !PlayersOnFlag.Exists(p2 => p.channel.owner.playerID.steamID.m_SteamID == p2.channel.owner.playerID.steamID.m_SteamID)).ToList();
        }
        public ulong FullOwner { 
            get
            {
                if (_points >= MaxPoints)
                    return 1;
                else if (_points <= MaxPoints * -1)
                    return 2;
                else return 0;
            } 
            set
            {
                if (1 == value)
                    Points = MaxPoints;
                else if (2 == value)
                    Points = MaxPoints * -1;
                else if (0 == value)
                    Points = 0;
                else F.LogError($"Tried to set owner of flag {_id} to an invalid team: {value}.");
            }
        }
        private int _points;
        public int Points
        {
            get => _points;
            set
            {
                ulong OldOwner;
                int OldPoints = _points;
                if (_points >= MaxPoints)
                    OldOwner = 1;
                else if (_points <= MaxPoints * -1)
                    OldOwner = 2;
                else OldOwner = 0;
                if (value > MaxPoints) _points = MaxPoints;
                else if (value < MaxPoints * -1) _points = MaxPoints * -1;
                else _points = value;
                if (OldPoints != _points)
                {
                    OnPointsChanged?.Invoke(this, new CaptureChangeEventArgs { NewPoints = _points, OldPoints = OldPoints });
                    ulong NewOwner;
                    if (_points >= MaxPoints)
                        NewOwner = 1;
                    else if (_points <= -MaxPoints)
                        NewOwner = 2;
                    else NewOwner = 0;
                    if (OldOwner != NewOwner) OnOwnerChanged?.Invoke(this, new OwnerChangeEventArgs { OldOwner = OldOwner, NewOwner = NewOwner });
                }
            }
        }
        public event EventHandler<PlayerEventArgs> OnPlayerEntered;
        public event EventHandler<PlayerEventArgs> OnPlayerLeft;
        public event EventHandler<CaptureChangeEventArgs> OnPointsChanged;
        public event EventHandler<OwnerChangeEventArgs> OnOwnerChanged;
        public event EventHandler OnDisposed;
        public event EventHandler OnReset;
        public List<Player> PlayersOnFlag { get; private set; }
        public Flag(FlagData data, FlagManager manager)
        {
            this.Manager = manager;
            this._id = data.id;
            this._x = data.x;
            this._y = data.y;
            this._position2d = data.Position2D;
            this._level = data.level;
            this._name = data.name;
            this._color = data.color;
            this._owner = 0;
            PlayersOnFlag = new List<Player>();
            this.ZoneData = ComplexifyZone(data);
        }
        public static Zone ComplexifyZone(FlagData data)
        {
            switch (data.zone.type)
            {
                case "rectangle":
                    return new RectZone(data.Position2D, data.zone, data.use_map_size_multiplier, data.name);
                case "circle":
                    return new CircleZone(data.Position2D, data.zone, data.use_map_size_multiplier, data.name);
                case "polygon":
                    return new PolygonZone(data.Position2D, data.zone, data.use_map_size_multiplier, data.name);
                default:
                    F.LogError("Invalid zone type \"" + data.zone.type + "\" at flag ID: " + data.id.ToString() + ", name: " + data.name);
                    return new RectZone(data.Position2D, new ZoneData("circle", "50"), data.use_map_size_multiplier, data.name);
            }
        }
        public bool IsFriendly(UnturnedPlayer player) => IsFriendly(player.Player.quests.groupID.m_SteamID);
        public bool IsFriendly(SteamPlayer player) => IsFriendly(player.player.quests.groupID.m_SteamID);
        public bool IsFriendly(Player player) => IsFriendly(player.quests.groupID.m_SteamID);
        public bool IsFriendly(CSteamID groupID) => IsFriendly(groupID.m_SteamID);
        public bool IsFriendly(ulong groupID) => groupID == _owner;
        public bool PlayerInRange(Vector3 PlayerPosition) => ZoneData.IsInside(PlayerPosition);
        public bool PlayerInRange(Vector2 PlayerPosition) => ZoneData.IsInside(PlayerPosition);
        public bool PlayerInRange(UnturnedPlayer player) => PlayerInRange(player.Position);
        public bool PlayerInRange(SteamPlayer player) => PlayerInRange(player.player.transform.position);
        public bool PlayerInRange(Player player) => PlayerInRange(player.transform.position);
        public void EnterPlayer(Player player)
        {
            OnPlayerEntered?.Invoke(this, new PlayerEventArgs { player = player });
            if (!PlayersOnFlag.Exists(p => p.channel.owner.playerID.steamID.m_SteamID == player.channel.owner.playerID.steamID.m_SteamID)) PlayersOnFlag.Add(player);
        }
        public void ExitPlayer(Player player)
        {
            OnPlayerLeft?.Invoke(this, new PlayerEventArgs { player = player });
            PlayersOnFlag.Remove(player);
        }
        public bool IsNeutral() => FullOwner == 0;
        public void CapT1(int amount = 1)
        {
            Points += amount;
        }
        public void CapT2(int amount = 1)
        {
            Points -= amount;
        }
        public bool T1Obj { get => ID == Data.FlagManager.ObjectiveTeam1.ID; }
        public bool T2Obj { get => ID == Data.FlagManager.ObjectiveTeam2.ID; }
        public void EvaluatePoints(bool overrideInactiveCheck = false)
        {
            if (Manager.State == EState.ACTIVE || overrideInactiveCheck)
            {
                if (T1Obj)
                {
                    if (Team1TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= Team2TotalPlayers || (Team1TotalPlayers > 0 && Team2TotalPlayers == 0))
                        CapT1();
                    else if (
                      (Team2TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= Team1TotalPlayers ||
                      (Team2TotalPlayers > 0 && Team1TotalPlayers == 0)) &&
                      Owner == 2 && _points > -1 * MaxPoints)
                        CapT2();
                }
                else if (T2Obj)
                {
                    if (Team2TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= Team2TotalPlayers || (Team2TotalPlayers > 0 && Team1TotalPlayers == 0))
                        CapT2();
                    else if (
                      (Team1TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= Team2TotalPlayers ||
                      (Team1TotalPlayers > 0 && Team2TotalPlayers == 0)) &&
                      Owner == 1 && _points < MaxPoints)
                        CapT1();
                }
            }
        }
    }
}
