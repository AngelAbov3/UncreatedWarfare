﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Proximity;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Zones;

/// <summary>
/// Represents a zone or cluster of zones linked with their <see cref="IProximity"/> instances.
/// </summary>
public class ActiveZoneCluster : IDisposable
{
    private readonly ZoneProximity[] _zones;
    private bool _disposed;
    private readonly TrackingList<WarfarePlayer> _players = new TrackingList<WarfarePlayer>(8);

    /// <summary>
    /// List of all players currently inside the zone.
    /// </summary>
    public ReadOnlyTrackingList<WarfarePlayer> Players { get; }

    /// <summary>
    /// List of all zones in this cluster.
    /// </summary>
    public IReadOnlyList<ZoneProximity> Zones { get; }

    /// <summary>
    /// The shared name of the cluster of zones.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The shared short name of the cluster of zones.
    /// </summary>
    public string? ShortName { get; }

    /// <summary>
    /// The zone marked as primary of this group.
    /// </summary>
    [JsonIgnore]
    public ref readonly ZoneProximity Primary => ref _zones[PrimaryIndex];

    /// <summary>
    /// The index of the primary zone in <see cref="Zones"/>.
    /// </summary>
    public int PrimaryIndex { get; }

    /// <summary>
    /// Total number of zones in this cluster.
    /// </summary>
    public int Count => _zones.Length;

    internal ActiveZoneCluster(ZoneProximity[] zones, IServiceProvider serviceProvider)
    {
        if (zones.Length == 0)
            throw new ArgumentException("A zone group must consist of at least one zone.", nameof(zones));

        Players = new ReadOnlyTrackingList<WarfarePlayer>(_players);

        // move primary to the front of the array
        for (int i = 1; i < zones.Length; ++i)
        {
            if (!zones[i].Zone.IsPrimary)
                continue;

            (zones[0], zones[i]) = (zones[i], zones[0]);
            break;
        }

        _zones = zones;
        Zones = new ReadOnlyCollection<ZoneProximity>(_zones);

        int primaryIndex = -1;
        for (int i = 0; i < zones.Length; ++i)
        {
            if (!zones[i].Zone.IsPrimary)
                continue;

            if (primaryIndex != -1)
                throw new ArgumentException("A zone group must consist of exactly one primary zone.", nameof(zones));

            primaryIndex = i;
        }

        if (primaryIndex == -1)
        {
            throw new ArgumentException("A zone group must consist of exactly one primary zone.", nameof(zones));
        }

        PrimaryIndex = primaryIndex;

        Zone primary = Primary.Zone;

        Name = primary.Name;
        ShortName = primary.ShortName;

        for (int i = 0; i < zones.Length; ++i)
        {
            if (zones[i].Proximity is not ITrackingProximity<WarfarePlayer> proximity)
                continue;

            proximity.OnObjectEntered += OnObjectEnteredAnyZone;
            proximity.OnObjectExited += OnObjectExitedAnyZone;
        }
    }

    private void OnObjectExitedAnyZone(WarfarePlayer player)
    {
        // check to make sure they're not already in another part of the cluster
        if (_zones.Length > 0)
        {
            bool isInAnotherZone = false;
            for (int i = 0; i < _zones.Length; ++i)
            {
                if (_zones[i].Proximity is not ITrackingProximity<WarfarePlayer> proximity || !proximity.Contains(player))
                    continue;

                isInAnotherZone = true;
                break;
            }

            if (isInAnotherZone)
            {
                _players.AddIfNotExists(player);
                return;
            }
        }

        _players.Remove(player);
    }

    private void OnObjectEnteredAnyZone(WarfarePlayer player)
    {
        _players.AddIfNotExists(player);
    }

    /// <summary>
    /// Check if a position is within the proximity.
    /// </summary>
    public bool TestPoint(Vector3 position)
    {
        for (int i = 0; i < _zones.Length; ++i)
        {
            ref ZoneProximity proximity = ref _zones[i];
            if (proximity.Proximity.TestPoint(position))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a position is within the proximity while ignoring Y position.
    /// </summary>
    public bool TestPoint(Vector2 position)
    {
        for (int i = 0; i < _zones.Length; ++i)
        {
            ref ZoneProximity proximity = ref _zones[i];
            if (proximity.Proximity.TestPoint(position))
            {
                return true;
            }
        }

        return false;
    }

    // clean-up colliders and GameObjects
    public void Dispose()
    {
        if (GameThread.IsCurrent)
        {
            DisposeIntl();
        }
        else
        {
            if (_disposed)
                return;

            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                DisposeIntl();
            });
        }
    }

    private void DisposeIntl()
    {
        if (_disposed)
            return;

        _disposed = true;
        for (int i = 0; i < _zones.Length; ++i)
        {
            if (_zones[i].Proximity is IEventBasedProximity<WarfarePlayer> proximity)
            {
                proximity.OnObjectEntered -= OnObjectEnteredAnyZone;
                proximity.OnObjectExited -= OnObjectExitedAnyZone;
            }

            switch (_zones[i].Proximity)
            {
                case IDisposable disposable:
                    disposable.Dispose();
                    break;

                case Object component when component != null:
                    Object.Destroy(component);
                    break;
            }
        }
    }
}

/// <summary>
/// Links zone info with it's <see cref="IProximity"/> instance.
/// </summary>
public readonly struct ZoneProximity(IProximity proximity, Zone zone)
{
    public IProximity Proximity { get; } = proximity;
    public Zone Zone { get; } = zone;
}