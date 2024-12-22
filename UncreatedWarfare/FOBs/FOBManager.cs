﻿using Microsoft.Extensions.DependencyInjection;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.Deployment;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Fobs;

public partial class FobManager : ILayoutHostedService
{
    private readonly FobTranslations _translations;
    private readonly AssetConfiguration _assetConfiguration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FobManager> _logger;
    private readonly TrackingList<IFobEntity> _entities;
    private readonly TrackingList<IFob> _fobs;
    private readonly ChatService _chatService;

    public readonly FobConfiguration Configuration;

    /// <summary>
    /// Items placed by players that aren't linked to a specific FOB.
    /// </summary>
    public IReadOnlyList<IFobEntity> Entities { get; }

    /// <summary>
    /// List of all FOBs in the world.
    /// </summary>
    public IReadOnlyList<IFob> Fobs { get; }

    public FobManager(IServiceProvider serviceProvider, ILogger<FobManager> logger)
    {
        Configuration = serviceProvider.GetRequiredService<FobConfiguration>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<FobTranslations>>().Value;
        _assetConfiguration = serviceProvider.GetRequiredService<AssetConfiguration>();
        _chatService = serviceProvider.GetRequiredService<ChatService>();
        _serviceProvider = serviceProvider;
        _logger = logger;
        _fobs = new TrackingList<IFob>(24);
        _entities = new TrackingList<IFobEntity>(32);

        Fobs = new ReadOnlyTrackingList<IFob>(_fobs);
        Entities = new ReadOnlyTrackingList<IFobEntity>(_entities);
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    public BuildableFob RegisterFob(IBuildable fobBuildable)
    {
        GridLocation griddy = new GridLocation(fobBuildable.Position);
        string fobName = $"{NATOPhoneticAlphabetHelper.GetProperCase(griddy.LetterX)}-{griddy.Y + 1}";

        BuildableFob fob = new BuildableFob(_serviceProvider, fobName, fobBuildable);
        _fobs.Add(fob);
        _logger.LogDebug("Registered new FOB: " + fob);
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobRegistered { Fob = fob });
        return fob;
    }
    public bool DeregisterFob(IFob fob)
    {
        IFob? existing = _fobs.FindAndRemove(f => f == fob);
        if (existing == null)
            return false;
        _logger.LogDebug("Deregistered FOB: " + fob);
        _ = WarfareModule.EventDispatcher.DispatchEventAsync(new FobDeregistered { Fob = fob });
        fob.DestroyAsync();
        return true;
    }
    public void RegisterFobEntity(IFobEntity entity)
    {
        _entities.Add(entity);
        _logger.LogDebug("Registered new FOB Entity: " + entity);

    }
    public bool DeregisterFobEntity(IFobEntity entity)
    {
        IFobEntity? existing = _entities.FindAndRemove(f => f == entity);
        if (existing == null)
            return false;
        _logger.LogDebug("Deregistered FOB Entity: " + entity);
        return true;
    }
    public FobType? FindFob<FobType>(IBuildable matchingBuildable) where FobType : BasePlayableFob
    {
        return _fobs.OfType<FobType>().FirstOrDefault(f => f.Buildable.Equals(matchingBuildable));
    }
    public BuildableFob? FindNearestBuildableFob(Team team, Vector3 position, bool includeUnbuilt = true)
    {
        return _fobs.OfType<BuildableFob>().FirstOrDefault(f =>
            f.Team == team &&
            MathUtility.WithinRange(position, f.Position, f.EffectiveRadius) &&
            (includeUnbuilt ? true : f.IsBuilt)
        );
    }
    public BuildableFob? FindNearestBuildableFob(CSteamID teamGroup, Vector3 position, bool includeUnbuilt = true)
    {
        return _fobs.OfType<BuildableFob>().FirstOrDefault(f =>
            f.Team.GroupId == teamGroup &&
            MathUtility.WithinRange(position, f.Position, f.EffectiveRadius) &&
            (includeUnbuilt ? true : f.IsBuilt)
        );
    }
    public IEnumerable<BuildableFob> FriendlyBuildableFobs(Team team, bool includeUnbuilt = true)
    {
        return _fobs.OfType<BuildableFob>().Where(f =>
            f.Team == team &&
            (includeUnbuilt ? true : f.IsBuilt)
        );
    }
    public TEntity? GetBuildableFobEntity<TEntity>(IBuildable buildable) where TEntity : IBuildableFobEntity
    {
        return _entities.OfType<TEntity>().FirstOrDefault(f =>
            f.Buildable.Equals(buildable)
        );
    }
    public EmplacementEntity? GetEmplacementFobEntity(InteractableVehicle emplacementVehicle)
    {
        return _entities.OfType<EmplacementEntity>().FirstOrDefault(f =>
            f.Vehicle.Vehicle.instanceID == emplacementVehicle.instanceID
        );
    }
}