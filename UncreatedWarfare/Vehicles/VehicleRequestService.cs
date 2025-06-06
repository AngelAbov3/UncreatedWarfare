using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Requests;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Vehicles;

[Priority(-3 /* run after vehicle storage services (specifically VehicleSpawnerStore and VehicleInfoStore) */)]
public class VehicleRequestService : 
    IRequestHandler<VehicleBaySignInstanceProvider, VehicleSpawner>,
    IRequestHandler<VehicleSpawner, VehicleSpawner>,
    IRequestHandler<WarfareVehicleComponent, VehicleSpawner>
{
    private static readonly IAssetLink<EffectAsset> UnlockSound = AssetLink.Create<EffectAsset>("4bfd3e5fcb3e4d109d3ec5ecca87d603");//AssetLink.Create<EffectAsset>("bc41e0feaebe4e788a3612811b8722d3");
    private readonly RequestVehicleTranslations _reqTranslations;
    private readonly VehicleInfoStore _vehicleInfoStore;
    private readonly VehicleSpawnerService _spawnerService;
    private readonly VehicleService _vehicleService;
    private readonly ILogger<VehicleRequestService> _logger;
    private readonly ZoneStore _globalZoneStore;
    private readonly PointsService _pointsService;
    private readonly DatabaseInterface _moderationSql;

    private const float MaxVehicleAbandonmentDistance = 300;

    public VehicleRequestService(IServiceProvider serviceProvider, ILogger<VehicleRequestService> logger)
    {
        _logger = logger;
        _vehicleInfoStore = serviceProvider.GetRequiredService<VehicleInfoStore>();
        _spawnerService = serviceProvider.GetRequiredService<VehicleSpawnerService>();
        _vehicleService = serviceProvider.GetRequiredService<VehicleService>();
        _globalZoneStore = serviceProvider.GetRequiredService<ZoneStore>();
        _pointsService = serviceProvider.GetRequiredService<PointsService>();
        _moderationSql = serviceProvider.GetRequiredService<DatabaseInterface>();
        _reqTranslations = serviceProvider.GetRequiredService<TranslationInjection<RequestVehicleTranslations>>().Value;
    }

    Task<bool> IRequestHandler<VehicleBaySignInstanceProvider, VehicleSpawner>.RequestAsync(WarfarePlayer player, VehicleBaySignInstanceProvider? sign, IRequestResultHandler resultHandler, CancellationToken token)
    {
        if (sign == null || !_spawnerService.TryGetSpawner(sign.BarricadeInstanceId, out VehicleSpawner? spawner))
        {
            resultHandler.NotFoundOrRegistered(player);
            return Task.FromResult(false);
        }

        return RequestAsync(player, spawner, resultHandler, token);
    }

    Task<bool> IRequestHandler<WarfareVehicleComponent, VehicleSpawner>.RequestAsync(WarfarePlayer player, WarfareVehicleComponent? vehicleComponent, IRequestResultHandler resultHandler, CancellationToken token)
    {
        if (vehicleComponent?.WarfareVehicle.Spawn == null)
        {
            resultHandler.NotFoundOrRegistered(player);
            return Task.FromResult(false);
        }

        return RequestAsync(player, vehicleComponent.WarfareVehicle.Spawn, resultHandler, token);
    }

    /// <summary>
    /// Request unlocking a vehicle from a spawn.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public async Task<bool> RequestAsync(WarfarePlayer player, VehicleSpawner? spawn, IRequestResultHandler resultHandler, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (!player.IsOnline)
        {
            return false;
        }

        if (spawn?.Buildable == null || !spawn.Buildable.Alive || !spawn.VehicleInfo.VehicleAsset.TryGetAsset(out VehicleAsset? vehicleAsset))
        {
            resultHandler.NotFoundOrRegistered(player);
            return false;
        }

        WarfareVehicleInfo? vehicleInfo = _vehicleInfoStore.Vehicles.FirstOrDefault(x => x.VehicleAsset.MatchAsset(vehicleAsset));
        if (vehicleInfo == null)
        {
            resultHandler.NotFoundOrRegistered(player);
            return false;
        }

        AssetBan? existingAssetBan = await _moderationSql.GetActiveAssetBan(player.Steam64, vehicleInfo.Type, token: token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        if (!player.IsOnline)
        {
            return false;
        }

        // asset ban
        if (existingAssetBan != null && existingAssetBan.IsAssetBanned(vehicleInfo.Type, true, true))
        {
            if (existingAssetBan.VehicleTypeFilter.Length == 0)
            {
                resultHandler.MissingRequirement(player, spawn,
                    existingAssetBan.IsPermanent
                        ? _reqTranslations.AssetBannedGlobalPermanent.Translate(player)
                        : _reqTranslations.AssetBannedGlobal.Translate(existingAssetBan.GetTimeUntilExpiry(false), player)
                );
                return false;
            }

            string commaList = existingAssetBan.GetCommaList(false, player.Locale.LanguageInfo);
            resultHandler.MissingRequirement(player, spawn,
                existingAssetBan.IsPermanent
                    ? _reqTranslations.AssetBannedPermanent.Translate(commaList, player)
                    : _reqTranslations.AssetBanned.Translate(commaList, existingAssetBan.GetTimeUntilExpiry(false), player)
            );
            return false;
        }

        InteractableVehicle? vehicle = spawn.LinkedVehicle;
        if (vehicle == null || vehicle.isDead || vehicle.isExploded || vehicle.isDrowned || !vehicle.asset.canBeLocked)
        {
            resultHandler.MissingRequirement(player, spawn, _reqTranslations.NotAvailable.Translate(player));
            return false;
        }

        if (vehicle.lockedGroup.m_SteamID != 0 || vehicle.lockedOwner.m_SteamID != 0)
        {
            resultHandler.MissingRequirement(player, spawn, _reqTranslations.AlreadyRequested.Translate(player));
            return false;
        }

        Team team = player.Team;
        Zone? mainZone = _globalZoneStore.EnumerateInsideZones(spawn.Buildable.Position, ZoneType.MainBase).FirstOrDefault();
        if (mainZone?.Faction != null && !mainZone.Faction.Equals(team.Faction.FactionId, StringComparison.OrdinalIgnoreCase))
        {
            resultHandler.MissingRequirement(player, spawn, _reqTranslations.IncorrectTeam.Translate(player));
            return false;
        }

        KitPlayerComponent comp = player.Component<KitPlayerComponent>();

        if (vehicleInfo.Class > Class.Unarmed && comp.ActiveClass != vehicleInfo.Class || vehicleInfo.Class == Class.Squadleader && !player.IsSquadLeader())
        {
            resultHandler.MissingRequirement(player, spawn, _reqTranslations.IncorrectKitClass.Translate(vehicleInfo.Class, player));
            return false;
        }

        Vector3 pos = spawn.Buildable.Position;

        foreach (VehicleSpawner otherSpawn in _spawnerService.Spawners)
        {
            if (otherSpawn == spawn
                || !otherSpawn.Team.IsFriendly(player.Team)
                || otherSpawn.LinkedVehicle == null
                || otherSpawn.LinkedVehicle.isDead
                || otherSpawn.LinkedVehicle.isExploded
                || otherSpawn.LinkedVehicle.isDrowned)
            {
                continue;
            }

            InteractableVehicle v = otherSpawn.LinkedVehicle;
            if (v.lockedOwner.m_SteamID != player.Steam64.m_SteamID || !MathUtility.WithinRange(v.transform.position, in pos, MaxVehicleAbandonmentDistance))
            {
                continue;
            }

            resultHandler.MissingRequirement(player, spawn, _reqTranslations.AnotherVehicleAlreadyOwned.Translate(v.asset, player));
            return false;
        }

        if (vehicleInfo.UnlockRequirements != null)
        {
            foreach (UnlockRequirement requirement in vehicleInfo.UnlockRequirements)
            {
                bool canAccess = await requirement.CanAccessAsync(player, token);
                await UniTask.SwitchToMainThread(token);
                if (!player.IsOnline)
                    return false;

                if (canAccess)
                    continue;

                resultHandler.MissingUnlockRequirement(player, spawn, requirement);
                return false;
            }
        }

        // check enough credits
        if (player.CachedPoints.Credits < vehicleInfo.CreditCost)
        {
            resultHandler.MissingCreditsOwnership(player, spawn, vehicleInfo.CreditCost);
            return false;
        }

        if (spawn.IsDelayed(out TimeSpan timeLeft))
        {
            resultHandler.VehicleDelayed(player, spawn, timeLeft);
            return false;
        }

        await UniTask.SwitchToMainThread(token);

        if (spawn.LinkedVehicle == null || spawn.LinkedVehicle.isDead || spawn.LinkedVehicle.isDrowned || spawn.LinkedVehicle.isExploded)
        {
            spawn.UnlinkVehicle();
            await _vehicleService.SpawnVehicleAsync(spawn, token);
            await UniTask.SwitchToMainThread(token);
        }
        

        vehicle = spawn.LinkedVehicle;

        if (vehicle == null)
        {
            resultHandler.NotFoundOrRegistered(player);
            return false;
        }

        VehicleManager.ServerSetVehicleLock(vehicle, player.Steam64, player.Team.GroupId, true);

        WarfareVehicle warfareVehicle = _vehicleService.GetVehicle(vehicle);
        warfareVehicle.OriginalOwner = player.Steam64;

        spawn.NotifyRequsted();

        DropStartingItems(vehicleInfo, player);
            
        EffectUtility.TriggerEffect(UnlockSound, EffectManager.SMALL, player.Position /* todo: vehicle!.transform.position */, true);
        
        // purchase the vehicle
        if (vehicleInfo.CreditCost > 0)
        {
            await _pointsService.ApplyEvent(player, _pointsService.GetPurchaseEvent(player, vehicleInfo.CreditCost), token);
        }

        resultHandler.Success(player, spawn);
        return true;
    }

    private void DropStartingItems(WarfareVehicleInfo vehicleInfo, WarfarePlayer player)
    {
        foreach (IAssetLink<ItemAsset> item in vehicleInfo.StartingItems)
        {
            if (item.TryGetAsset(out ItemAsset? asset))
                ItemManager.dropItem(new Item(asset, EItemOrigin.WORLD), player.Position, true, true, true);
            else
                _logger.LogWarning($"Vehicle starting item not found: {item}");
        }
    }
}