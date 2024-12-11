using DanielWillett.ReflectionTools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Vehicles.Info;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Vehicles.Vehicle;

namespace Uncreated.Warfare.Vehicles
{
    [Priority(-3 /* run after vehicle storage services (specifically VehicleSpawnerStore and VehicleInfoStore) */)]
    public class VehicleService : 
        ILayoutHostedService,
        IEventListener<VehicleSpawned>,
        IEventListener<VehicleDespawned>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VehicleService> _logger;
        private readonly VehicleInfoStore _vehicleInfoStore;
        private readonly VehicleSpawnerStore _vehicleSpawnerStore;

        private const float VehicleSpawnOffset = 5f;
        public const ushort MaxBatteryCharge = 10000;

        public TrackingList<WarfareVehicle> Vehicles { get; }

        public VehicleService(IServiceProvider serviceProvider, ILogger<VehicleService> logger)
        {
            Vehicles = new TrackingList<WarfareVehicle>();
            _logger = logger;
            _serviceProvider = serviceProvider;
            _vehicleInfoStore = serviceProvider.GetRequiredService<VehicleInfoStore>();
            _vehicleSpawnerStore = serviceProvider.GetRequiredService<VehicleSpawnerStore>();
        }

        public async UniTask StartAsync(CancellationToken token)
        {
            await DeleteAllVehiclesAsync(token);
        }

        public UniTask StopAsync(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }
        public WarfareVehicle? RegisterWarfareVehicle(InteractableVehicle vehicle)
        {
            WarfareVehicleInfo? info = _vehicleInfoStore.GetVehicleInfo(vehicle.asset);

            if (info == null)
                info = new WarfareVehicleInfo(); // todo: make a default WarfareVehicleInfo to avoid nullptr exceptions

            WarfareVehicle warfareVehicle = new WarfareVehicle(vehicle, info);
            Vehicles.AddIfNotExists(warfareVehicle);
            return warfareVehicle;
        }
        public WarfareVehicle? DeregisterWarfareVehicle(InteractableVehicle vehicle)
        {
            WarfareVehicle? existing = Vehicles.FindAndRemove(f => f.Vehicle == vehicle);
            existing?.Dispose();
            return existing;
        }
        /// <summary>
        /// Spawn a vehicle at a given vehicle spawner.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="AssetNotFoundException">Unable to find the vehicle asset.</exception>
        /// <exception cref="NotSupportedException">There's already a linked vehicle to the spawner.</exception>
        /// <exception cref="RecordsNotFoundException">Unable to find any <see cref="WarfareVehicleInfo"/> for the vehicle.</exception>
        /// <exception cref="InvalidOperationException">The spawner buildable doesn't exist. -OR- Failed to unlink or link the vehicle to it's spawn.</exception>
        /// <exception cref="Exception">Game failed to spawn the vehicle.</exception>
        public async UniTask<InteractableVehicle> SpawnVehicleAsync(VehicleSpawner spawner, CancellationToken token = default)
        {
            await UniTask.SwitchToMainThread(token);

            if (spawner.LinkedVehicle != null && spawner.LinkedVehicle.isDead && !spawner.LinkedVehicle.isExploded)
            {
                throw new NotSupportedException($"There can only be one vehicle per spawn, and this spawn already has a vehicle: {spawner.ToDisplayString()}.");
            }

            spawner.UnlinkVehicle();

            if (spawner.Buildable == null || spawner.Buildable.IsDead)
            {
                throw new InvalidOperationException("Spawner buildable no longer exists.");
            }

            Quaternion spawnRotation = spawner.Buildable.Rotation * BarricadeUtility.InverseDefaultBarricadeRotation;

            Vector3 spawnPosition = spawner.Buildable.Position + Vector3.up * VehicleSpawnOffset;

            InteractableVehicle vehicle = await SpawnVehicleAsync(spawner.VehicleInfo.VehicleAsset, spawnPosition, spawnRotation, paintColor: spawner.VehicleInfo.PaintColor, token: token);
            await UniTask.SwitchToMainThread(token);

            spawner.LinkVehicle(vehicle);

            _logger.LogDebug("Spawned new {0} at {1}.", spawner.VehicleInfo.VehicleAsset.ToDisplayString(), spawnPosition);
            return vehicle;
        }

        /// <summary>
        /// Spawn a vehicle with the given information.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="AssetNotFoundException">Unable to find the vehicle asset.</exception>
        /// <exception cref="Exception">Game failed to spawn the vehicle.</exception>
        public async UniTask<InteractableVehicle> SpawnVehicleAsync(
            IAssetLink<VehicleAsset> vehicle,
            Vector3 position,
            Quaternion rotation,
            CSteamID owner = default,
            CSteamID group = default,
            Color32 paintColor = default,
            bool locked = true,
            CancellationToken token = default)
        {
            await UniTask.SwitchToMainThread(token);

            VehicleAsset asset = vehicle.GetAssetOrFail(nameof(vehicle));

            byte[][] turrets = new byte[asset.turrets.Length][];

            for (int i = 0; i < asset.turrets.Length; ++i)
            {
                // todo april fools should add dootpressor but i couldn't get it to work on s3
                if (Assets.find(EAssetType.ITEM, asset.turrets[i].itemID) is ItemGunAsset turret)
                {
                    turrets[i] = turret.getState(EItemOrigin.ADMIN);
                }
            }

            InteractableVehicle? veh = VehicleManager.SpawnVehicleV3(asset, 0, 0, 0f, position, rotation, false, false, false, false,
                                                                     asset.fuel, asset.health, MaxBatteryCharge, owner, group, locked, turrets,
                                                                     byte.MaxValue, paintColor: paintColor);
            if (veh == null)
                throw new Exception($"Failed to spawn vehicle {vehicle.ToDisplayString()} due to vanilla code, possible a misconfigured vehicle.");

            WarfareVehicleInfo? vehicleInfo = _vehicleInfoStore.GetVehicleInfo(vehicle);

            if (vehicleInfo is not { Trunk.Count: > 0 })
                return veh;

            // add items to trunk
            foreach (WarfareVehicleInfo.TrunkItem item in vehicleInfo.Trunk)
            {
                if (!item.Item.TryGetAsset(out ItemAsset? itemAsset))
                {
                    _logger.LogWarning("Failed to find item asset for the trunk of {0}: {1}.", vehicle.ToDisplayString(), item.Item.ToDisplayString());
                    continue;
                }

                Item info = new Item(itemAsset.id, itemAsset.amount, 100, item.State ?? itemAsset.getState(EItemOrigin.ADMIN));
                if (veh.trunkItems.checkSpaceEmpty(item.X, item.Y, itemAsset.size_x, itemAsset.size_y, item.Rotation))
                {
                    veh.trunkItems.addItem(item.X, item.Y, item.Rotation, info);
                }
                else if (!veh.trunkItems.tryAddItem(info))
                {
                    ItemManager.dropItem(info, position, false, true, true);
                }
            }

            return veh;
        }
        /// <summary>
        /// Remove one vehicle and clean up spawn information and items.
        /// </summary>
        public async UniTask DeleteVehicleAsync(InteractableVehicle vehicle, CancellationToken token = default)
        {
            await UniTask.SwitchToMainThread(token);

            PrepareToDeleteVehicle(vehicle);
            VehicleManager.askVehicleDestroy(vehicle);
        }
        /// <summary>
        /// Remove all vehicles and clean up spawn information and items.
        /// </summary>
        public async UniTask<int> DeleteAllVehiclesAsync(CancellationToken token = default)
        {
            await UniTask.SwitchToMainThread(token);

            int count = VehicleManager.vehicles.Count;
            for (int i = 0; i < count; i++)
            {
                PrepareToDeleteVehicle(VehicleManager.vehicles[i]);
            }

            VehicleManager.askVehicleDestroyAll();

            return count;
        }

        private void PrepareToDeleteVehicle(InteractableVehicle vehicle)
        {
            // keep storage items from dropping on destroy
            BarricadeRegion region = BarricadeManager.getRegionFromVehicle(vehicle);
            if (region != null)
            {
                for (int b = 0; b < region.drops.Count; b++)
                {
                    if (region.drops[b].interactable is InteractableStorage storage)
                    {
                        storage.despawnWhenDestroyed = true;
                    }
                }
            }

            // empty trunk so items don't drop
            if (vehicle.trunkItems != null)
            {
                int ct = vehicle.trunkItems.getItemCount();
                for (int i = ct - 1; i >= 0; --i)
                    vehicle.trunkItems.removeItem((byte)i);
            }

            // remove all players and teleport them to the ground so they don't take fall damage
            for (int i = 0; i < vehicle.passengers.Length; ++i)
            {
                SteamPlayer? pl = vehicle.passengers[i].player;
                if (pl == null)
                    continue;

                VehicleManager.forceRemovePlayer(vehicle, pl.playerID.steamID);

                if (vehicle.asset.engine is not EEngine.BLIMP and not EEngine.HELICOPTER and not EEngine.PLANE)
                    continue;

                Vector3 p = pl.player.transform.position;
                float y = TerrainUtility.GetHighestPoint(in p, 1f);
                if (Mathf.Abs(p.y - y) > 5f)
                {
                    p.y = y;
                    pl.player.teleportToLocationUnsafe(p, pl.player.look.aim.transform.rotation.eulerAngles.y);
                }
            }

            if (!vehicle.TryGetComponent(out VehicleComponent component) || component.Spawn == null)
                return;

            // unlink vehicle from it's spawner if it had one
            try
            {
                component.Spawn.UnlinkVehicle();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to unlink vehicle spawn for vehicle {0} ({1}).", vehicle.asset.FriendlyName, vehicle.asset.GUID);
            }
        }
        public void HandleEvent(VehicleSpawned e, IServiceProvider serviceProvider)
        {
            RegisterWarfareVehicle(e.Vehicle);
        }
        public void HandleEvent(VehicleDespawned e, IServiceProvider serviceProvider)
        {
            DeregisterWarfareVehicle(e.Vehicle);
        }
    }
}
