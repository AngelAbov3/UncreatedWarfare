using Microsoft.Extensions.Configuration;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Objects;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Exceptions;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Lobby;

/// <summary>
/// Handles visual effects in the lobby.
/// </summary>
public class LobbyZoneManager :
    IHostedService,
    ILevelHostedService,
    IEventListener<QuestObjectInteracted>,
    ILayoutStartingListener,
    IEventListener<PlayerTeamChanged>,
    IEventListener<PlayerLeft>
{
    private const short FlagJoining = -1;
    private const short FlagFull = 0;
    private const short FlagOpen = 1;

    private readonly ZoneStore _zoneStore;
    private readonly OptionsUI _optionsUi;
    private readonly LobbyConfiguration _lobbyConfig;
    private readonly IFactionDataStore _factionDataStore;
    private readonly ILogger<LobbyZoneManager> _logger;
    private readonly WarfareModule _module;
    private readonly LayoutFactory _layoutFactory;
    private ITrackingProximity<WarfarePlayer>? _zoneCollider;
    private Zone? _lobbyZone;
    private Guid _settingsFlagGuid;

    /// <summary>
    /// If the lobby is disabled.
    /// </summary>
    /// <remarks>Added this so older maps can be loaded in the mean time before their lobby is upgraded.</remarks>
    public bool Disabled { get; private set; }

    private readonly ITeamSelectorBehavior _behavior;

    internal FlagInfo[]? TeamFlags;
    private readonly IPlayerService _playerService;

    /// <summary>
    /// Number of seconds to wait until joining the team.
    /// </summary>
    public TimeSpan JoinDelay { get; private set; }

    public LobbyZoneManager(
        ZoneStore zoneStore,
        LobbyConfiguration lobbyConfig,
        IFactionDataStore factionDataStore,
        ILogger<LobbyZoneManager> logger,
        WarfareModule module,
        ITeamSelectorBehavior behavior,
        IPlayerService playerService,
        OptionsUI optionsUi,
        LayoutFactory layoutFactory)
    {
        _optionsUi = optionsUi;
        _layoutFactory = layoutFactory;
        _zoneStore = zoneStore;
        _lobbyConfig = lobbyConfig;
        _factionDataStore = factionDataStore;
        _logger = logger;
        _module = module;
        _behavior = behavior;
        _playerService = playerService;
        _layoutFactory.LoadingStateUpdated += LoadingStateUpdated;
    }

    private void LoadingStateUpdated(bool isLoading)
    {
        if (_zoneCollider == null)
            return;

        if (isLoading)
        {
            foreach (WarfarePlayer player in _zoneCollider.ActiveObjects)
            {
                if (player.UnturnedPlayer.movement.pluginSpeedMultiplier != 0f)
                    player.UnturnedPlayer.movement.sendPluginSpeedMultiplier(0f);
            }
        }
        else
        {
            foreach (WarfarePlayer player in _zoneCollider.ActiveObjects)
            {
                if (player.UnturnedPlayer.movement.pluginSpeedMultiplier < 1f)
                    player.UnturnedPlayer.movement.sendPluginSpeedMultiplier(1f);
            }
        }
    }

    UniTask ILevelHostedService.LoadLevelAsync(CancellationToken token)
    {
        // find team flag objects
        List<FlagInfo> flags = new List<FlagInfo>(2);

        if (_lobbyConfig.GetValue("Disabled", false))
        {
            Disabled = true;
            _logger.LogInformation("Lobby disabled by Lobby config.");
            return UniTask.CompletedTask;
        }

        JoinDelay = _lobbyConfig.GetValue("JoinDelay", defaultValue: TimeSpan.FromSeconds(3d));

        _settingsFlagGuid = _lobbyConfig.GetValue("Settings:Object", defaultValue: Guid.Empty);

        foreach (IConfigurationSection flagInfo in _lobbyConfig.GetSection("Flags").GetChildren())
        {
            string? teamStr = flagInfo["Team"];
            IAssetLink<ObjectAsset> asset = flagInfo.GetAssetLink<ObjectAsset>("Object");
            ushort flagId = flagInfo.GetValue<ushort>("Flag");

            FactionInfo? faction = _factionDataStore.FindFaction(teamStr);
            if (faction == null)
                throw new GameConfigurationException("Invalid faction \"" + teamStr + "\"", _lobbyConfig.FilePath);

            ObjectInfo foundObject = default;
            foreach (ObjectInfo obj in LevelObjectUtility.EnumerateObjects())
            {
                if (!asset.MatchAsset(obj.Object.asset))
                    continue;

                if (foundObject.HasValue)
                {
                    throw new GameConfigurationException($"Multiple {asset} objects in the map, unable to choose one for the lobby flag", _lobbyConfig.FilePath);
                }

                foundObject = obj;
            }

            if (!foundObject.HasValue)
            {
                throw new GameConfigurationException($"No {asset} objects in the map, unable to find a lobby flag", _lobbyConfig.FilePath);
            }

            flags.Add(new FlagInfo(flags.Count, foundObject, flagId, faction));
        }

        TeamFlags = flags.ToArrayFast();

        TeamInfo[] behaviorTeamInfo = new TeamInfo[TeamFlags.Length];
        for (int i = 0; i < behaviorTeamInfo.Length; ++i)
        {
            behaviorTeamInfo[i].Team = TeamFlags[i].Team;
        }

        _behavior.Teams = behaviorTeamInfo;

        // find zone
        string? lobbyZoneName = _lobbyConfig["Zone"];
        if (lobbyZoneName == null)
            throw new GameConfigurationException("Lobby zone not specified ('Zone')", _lobbyConfig.FilePath);

        _lobbyZone = _zoneStore.Zones.FirstOrDefault(x => x.Name.Equals(lobbyZoneName, StringComparison.Ordinal))
                     ?? throw new GameConfigurationException("Lobby zone not found: \"" + lobbyZoneName + "\"", _lobbyConfig.FilePath);

        _zoneCollider = _zoneStore.CreateColliderForZone(_lobbyZone);

        _zoneCollider.OnObjectEntered += OnObjectEnteredLobby;
        _zoneCollider.OnObjectExited += OnObjectExitedLobby;

        TimeUtility.physicsUpdated += OnFixedUpdate;

        return UniTask.CompletedTask;
    }

    /// <summary>
    /// Gets a reference to the zone being used as the lobby zone.
    /// </summary>
    public Zone? GetLobbyZone()
    {
        return _lobbyZone;
    }
    
    /// <summary>
    /// Teleport a player to the lobby and leave their group.
    /// </summary>
    public async UniTask JoinLobbyAsync(WarfarePlayer player, CancellationToken token = default)
    {
        await _module.GetActiveLayout().TeamManager.JoinTeamAsync(player, Team.NoTeam, wasByAdminCommand: false, token);

        if (_lobbyZone == null)
            return;

        await UniTask.SwitchToMainThread(CancellationToken.None);
        player.UnturnedPlayer.teleportToLocationUnsafe(_lobbyZone.Spawn, _lobbyZone.SpawnYaw);
        if (_layoutFactory.IsLoading)
        {
            if (player.UnturnedPlayer.movement.pluginSpeedMultiplier != 0f)
                player.UnturnedPlayer.movement.sendPluginSpeedMultiplier(0f);
        }
        else
        {
            if (player.UnturnedPlayer.movement.pluginSpeedMultiplier < 1f)
                player.UnturnedPlayer.movement.sendPluginSpeedMultiplier(1f);
        }
    }

    /// <summary>
    /// Gets the total cached player count on a single team.
    /// </summary>
    /// <remarks>Out of bounds indices just return 0.</remarks>
    public int GetTeamPlayerCount(int teamIndex)
    {
        if (TeamFlags == null)
            return 0;
        return !Disabled ? teamIndex >= 0 && teamIndex < TeamFlags.Length ? _behavior.Teams![teamIndex].PlayerCount : 0 : 0;
    }

    /// <summary>
    /// Gets the total of all players on a team.
    /// </summary>
    public int GetActivePlayerCount()
    {
        if (Disabled || TeamFlags == null)
            return 0;

        int ct = 0;
        for (int i = 0; i < TeamFlags.Length; ++i)
        {
            ct += _behavior.Teams![i].PlayerCount;
        }

        return ct;
    }

    public void StartJoiningTeam(WarfarePlayer player, int teamIndex)
    {
        if (Disabled || TeamFlags == null)
            throw new InvalidOperationException("Lobby is disabled.");

        PlayerLobbyComponent component = player.Component<PlayerLobbyComponent>();
        if (teamIndex < 0)
        {
            if (!component.IsJoining)
                return;

            // cancel joining team
            component.StartJoiningTeam(-1);
            UpdateAllFlags(player);
            return;
        }

        if (component.IsJoining || !_behavior.CanJoinTeam(teamIndex, -1))
        {
            _logger.LogWarning("{0} tried to join a team ({1}) they can't.", player, teamIndex);
            UpdateAllFlags(player);
            return;
        }

        ref FlagInfo flag = ref TeamFlags[teamIndex];
        component.StartJoiningTeam(teamIndex);

        // this should be done automatically using a reward but just in case its configured wrong we double check
        PlayerQuests quests = player.UnturnedPlayer.quests;
        if (!quests.getFlag(flag.FlagId, out short value) || value != FlagJoining)
        {
            quests.sendSetFlag(flag.FlagId, FlagJoining);
        }

        // set all other teams as full just to prevent joining another team
        for (int i = 0; i < TeamFlags.Length; ++i)
        {
            if (i == teamIndex)
                continue;

            ref FlagInfo flag2 = ref TeamFlags[i];

            if (!quests.getFlag(flag2.FlagId, out value) || value != FlagFull)
            {
                quests.sendSetFlag(flag2.FlagId, FlagFull);
            }
        }
    }

    private void UpdateAllFlags(WarfarePlayer player)
    {
        if (TeamFlags == null)
            return;

        PlayerQuests quests = player.UnturnedPlayer.quests;
        PlayerLobbyComponent component = player.Component<PlayerLobbyComponent>();
        if (component.IsJoining)
        {
            for (int i = 0; i < TeamFlags.Length; ++i)
            {
                ref FlagInfo flag = ref TeamFlags[i];
                short flagValue = component.JoiningTeam.Index == i ? FlagJoining : FlagFull;
                if (!quests.getFlag(flag.FlagId, out short value) || value != flagValue)
                    quests.sendSetFlag(flag.FlagId, flagValue);
            }
        }
        else
        {
            for (int i = 0; i < TeamFlags.Length; ++i)
            {
                ref FlagInfo flag = ref TeamFlags[i];
                short flagValue = _behavior.CanJoinTeam(i, -1) ? FlagOpen : FlagFull;
                if (!quests.getFlag(flag.FlagId, out short value) || value != flagValue)
                    quests.sendSetFlag(flag.FlagId, flagValue);
                // todo put barricade in front of player for a frame to refresh the highlight color
            }
        }
    }

    private void UpdateAllFlags(int teamIndex)
    {
        if (TeamFlags == null || _zoneCollider == null)
            return;

        ref FlagInfo flag = ref TeamFlags[teamIndex];
        bool canJoinTeam = _behavior.CanJoinTeam(teamIndex, -1);

        foreach (WarfarePlayer player in _zoneCollider.ActiveObjects)
        {
            PlayerLobbyComponent component = player.Component<PlayerLobbyComponent>();
            
            short flagValue;
            if (component.IsJoining)
            {
                flagValue = component.JoiningTeam.Index == teamIndex ? FlagJoining : FlagFull;
            }
            else
            {
                flagValue = canJoinTeam ? FlagOpen : FlagFull;
            }

            PlayerQuests quests = player.UnturnedPlayer.quests;
            if (!quests.getFlag(flag.FlagId, out short value) || value != flagValue)
                quests.sendSetFlag(flag.FlagId, flagValue);
        }
    }

    internal void UpdateTeamCount(int teamIndex, int change)
    {
        if (Disabled || teamIndex < 0 || TeamFlags == null)
            return;

        _behavior.Teams![teamIndex].PlayerCount += change;
        UpdateAllFlags(teamIndex);
    }

    private void OnFixedUpdate()
    {
        if (_zoneCollider == null)
            return;

        foreach (WarfarePlayer player in _zoneCollider.ActiveObjects)
        {
            UpdatePlayerPositionalData(player);
        }
    }

    private void UpdatePlayerPositionalData(WarfarePlayer player)
    {
        if (!player.IsOnline)
            return;

        PlayerLobbyComponent component = player.Component<PlayerLobbyComponent>();
        if (component.IsJoining)
        {
            component.UpdatePositionalData(component.JoiningTeam.Index, component.JoiningTeam.Index);
            return;
        }

        int closestLookIndex = -1;
        float closestLookDot = 0;
        int closestPosIndex = -1;
        float closestDistSqr = 0;
        Transform pos = player.UnturnedPlayer.look.aim;
        Vector3 playerPos = pos.position;
        Vector3 playerLookVector = pos.forward;

        if (TeamFlags == null)
        {
            player.Component<PlayerLobbyComponent>().UpdatePositionalData(closestLookIndex, closestPosIndex);
            return;
        }

        // find closest sign and closest sign to being looked at
        for (int i = 0; i < TeamFlags.Length; ++i)
        {
            ref FlagInfo flag = ref TeamFlags[i];

            Vector3 lookVector = flag.Position - playerPos;

            float dot = Vector3.Dot(lookVector.normalized, playerLookVector);
            if (closestLookIndex == -1 || closestLookDot < dot)
            {
                closestLookDot = dot;
                closestLookIndex = i;
            }

            float distSqr = lookVector.sqrMagnitude;
            if (closestPosIndex == -1 || closestDistSqr > distSqr)
            {
                closestDistSqr = distSqr;
                closestPosIndex = i;
            }
        }

        // within 35 degrees of looking at sign
        float angle = Mathf.Acos(closestLookDot);
        if (angle > 35f * Mathf.Deg2Rad)
            closestLookIndex = -1;

        player.Component<PlayerLobbyComponent>().UpdatePositionalData(closestLookIndex, closestPosIndex);
    }

    private void OnObjectEnteredLobby(WarfarePlayer player)
    {
        UpdatePlayerPositionalData(player);
        player.Component<PlayerLobbyComponent>().EnterLobby();
        UpdateAllFlags(player);
    }

    private void OnObjectExitedLobby(WarfarePlayer player)
    {
        player.Component<PlayerLobbyComponent>().ExitLobby();
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        UpdateTeamCounts();
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerTeamChanged>.HandleEvent(PlayerTeamChanged e, IServiceProvider serviceProvider)
    {
        // update lobby counts
        if (Disabled)
            return;

        UpdateTeamCounts();
    }

    void IEventListener<QuestObjectInteracted>.HandleEvent(QuestObjectInteracted e, IServiceProvider serviceProvider)
    {
        if (Disabled || _layoutFactory.IsLoading || !_module.IsLayoutActive() || _module.GetActiveLayout().ActivePhase is LeaderboardPhase)
            return;

        if (e.Object.GUID == _settingsFlagGuid)
        {
            _optionsUi.Open(e.Player);
            return;
        }

        if (TeamFlags == null)
        {
            return;
        }

        for (int i = 0; i < TeamFlags.Length; ++i)
        {
            ref FlagInfo flag = ref TeamFlags[i];
            if (flag.Object.Index != e.ObjectIndex || !flag.Object.Coord.Equals(e.RegionPosition))
                continue;

            StartJoiningTeam(e.Player, i);
            break;
        }
    }

    private void UpdateTeamCounts()
    {
        _behavior.UpdateTeams();

        if (_zoneCollider == null)
            return;
        
        // add joining players to the player counts
        foreach (WarfarePlayer player in _zoneCollider.ActiveObjects)
        {
            PlayerLobbyComponent comp = player.Component<PlayerLobbyComponent>();
            if (!comp.IsJoining)
                continue;

            ++_behavior.Teams![comp.JoiningTeam.Index].PlayerCount;
        }

        foreach (WarfarePlayer player in _zoneCollider.ActiveObjects)
            UpdateAllFlags(player);
    }

    async UniTask ILayoutStartingListener.HandleLayoutStartingAsync(Layout layout, CancellationToken token)
    {
        if (Disabled)
            return;

        // update 'Team' objects for the new layout, since they may have changed between layouts
        ITeamManager<Team> teamManager = _module.GetActiveLayout().TeamManager;
        if (TeamFlags != null)
        {
            for (int i = 0; i < TeamFlags.Length; ++i)
            {
                ref FlagInfo flag = ref TeamFlags[i];
                FactionInfo faction = flag.Faction;

                Team? team = teamManager.AllTeams.FirstOrDefault(x => x.Faction.Equals(faction));

                if (team == null)
                {
                    throw new GameConfigurationException($"No team registered with the faction {faction.Name}", _lobbyConfig.FilePath);
                }

                flag.Team = team;

                ref TeamInfo behaviorTeam = ref _behavior.Teams![i];
                behaviorTeam.Team = team;
            }
        }

        if (_zoneCollider != null)
        {
            foreach (WarfarePlayer player in _zoneCollider.ActiveObjects)
            {
                PlayerLobbyComponent comp = player.Component<PlayerLobbyComponent>();

                comp.StartJoiningTeam(-1);
                UpdateAllFlags(player);
            }
        }

        UpdateTeamCounts();

        UniTask[] tasks = new UniTask[_playerService.OnlinePlayers.Count];
        int index = -1;

        // teleport all players on a team to lobby
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            if (!player.Team.IsValid)
                continue;

            tasks[++index] = JoinLobbyAsync(player, CancellationToken.None);
        }

        await UniTask.WhenAll(tasks);
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        TimeUtility.physicsUpdated -= OnFixedUpdate;

        if (_zoneCollider != null)
        {
            _zoneCollider.OnObjectEntered -= OnObjectEnteredLobby;
            _zoneCollider.OnObjectExited -= OnObjectExitedLobby;

            if (_zoneCollider is IDisposable d)
                d.Dispose();

            _zoneCollider = null!;
        }

        return UniTask.CompletedTask;
    }

    public struct FlagInfo
    {
        public readonly ObjectInfo Object;
        public readonly ushort FlagId;
        public readonly Vector3 Position;
        public readonly int Index;
        public readonly FactionInfo Faction;
        public Team? Team;
        public FlagInfo(int index, ObjectInfo obj, ushort flagId, FactionInfo faction)
        {
            Index = index;
            Object = obj;
            FlagId = flagId;
            Position = obj.Object.transform.position;
            Faction = faction;
        }
    }
}