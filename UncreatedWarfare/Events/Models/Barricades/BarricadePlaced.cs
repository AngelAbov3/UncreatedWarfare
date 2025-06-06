using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Events.Models.Buildables;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Barricades;

/// <summary>
/// Event listener args which handles <see cref="BarricadeManager.onBarricadeSpawned"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class BarricadePlaced : IBuildablePlacedEvent
{
    /// <summary>
    /// The owner of the barricade, if they're online.
    /// </summary>
    public required WarfarePlayer? Owner { get; init; }

    /// <summary>
    /// The barricade's object and model data.
    /// </summary>
    public required BarricadeDrop Barricade { get; init; }

    /// <summary>
    /// The barricade's server-side data.
    /// </summary>
    public required BarricadeData ServersideData { get; init; }

    /// <summary>
    /// The index of the vehicle region in <see cref="BarricadeManager.vehicleRegions"/>. <see cref="ushort.MaxValue"/> if the barricade is not planted.
    /// </summary>
    /// <remarks>Also known as 'plant'.</remarks>
    public required ushort VehicleRegionIndex { get; init; }

    /// <summary>
    /// Coordinate of the barricade region in <see cref="BarricadeManager.regions"/>.
    /// </summary>
    public required RegionCoord RegionPosition { get; init; }

    /// <summary>
    /// The region the barricade was placed in. This could be of type <see cref="VehicleBarricadeRegion"/>.
    /// </summary>
    public required BarricadeRegion Region { get; init; }

    /// <summary>
    /// The Steam ID of the player that placed the barricade.
    /// </summary>
    public CSteamID OwnerId => new CSteamID(ServersideData.owner);

    /// <summary>
    /// The Steam ID of the group that placed the barricade.
    /// </summary>
    public CSteamID GroupId => new CSteamID(ServersideData.group);

    /// <summary>
    /// The Unity model of the barricade.
    /// </summary>
    public Transform Transform => Barricade.model;

    /// <summary>
    /// Abstracted <see cref="IBuildable"/> of the barricade.
    /// </summary>
    public required IBuildable Buildable { get; init; }

    /// <summary>
    /// If this barricade is being placed on a vehicle.
    /// </summary>
    public bool IsOnVehicle => VehicleRegionIndex != ushort.MaxValue;

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        return new ActionLogEntry(ActionLogTypes.BuildableDestroyed,
            $"Barricade {AssetLink.ToDisplayString(Barricade.asset)} owned by {ServersideData.owner} ({ServersideData.group}) # {Barricade.instanceID} (on vehicle: {IsOnVehicle}) " +
            $"@ {ServersideData.point:F2}, {ServersideData.rotation:F2}",
            Owner?.Steam64.m_SteamID ?? 0
        );
    }
}