﻿using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles;

[SingletonDependency(typeof(VehicleBay))]
[SingletonDependency(typeof(StructureSaver))]
public class VehicleSpawner : ListSingleton<VehicleSpawn>, ILevelStartListener, IGameStartListener
{
    private static VehicleSpawner Singleton;
    public static bool Loaded => Singleton.IsLoaded<VehicleSpawner, VehicleSpawn>();
    public const float VEHICLE_HEIGHT_OFFSET = 5f;
    public VehicleSpawner() : base("vehiclespawns", Data.VehicleStorage + "vehiclespawns.json") { }
    protected override string LoadDefaults() => EMPTY_LIST;
    public static IEnumerable<VehicleSpawn> Spawners => Loaded ? Singleton : null!;
    public override void Load()
    {
        TeamManager.OnPlayerEnteredMainBase += OnPlayerEnterMain;
        TeamManager.OnPlayerLeftMainBase += OnPlayerLeftMain;
        Singleton = this;
    }
    public override void Unload()
    {
        Singleton = null!;
        TeamManager.OnPlayerLeftMainBase -= OnPlayerLeftMain;
        TeamManager.OnPlayerEnteredMainBase -= OnPlayerEnterMain;
    }
    void ILevelStartListener.OnLevelReady()
    {
        LoadSpawns();
        RespawnAllVehicles();
    }
    void IGameStartListener.OnGameStarting(bool isOnLoad)
    {
        UpdateSigns();
    }
    private void LoadSpawns()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (VehicleSpawn spawn in this)
        {
            spawn.Initialize();
        }
    }
    internal void OnBarricadeDestroyed(BarricadeData data, BarricadeDrop drop, uint instanceID, ushort plant)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (data.barricade.asset.GUID == Gamemode.Config.Barricades.VehicleBayGUID)
        {
            if (IsRegistered(data.instanceID, out _, EStructType.BARRICADE))
            {
                L.LogDebug("Vehicle spawn was deregistered because the barricade was salvaged or destroyed.");
                DeleteSpawn(data.instanceID, EStructType.BARRICADE);
            }
        }
    }
    internal void OnStructureDestroyed(StructureData data, StructureDrop drop, uint instanceID)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (data.structure.asset.GUID == Gamemode.Config.Barricades.VehicleBayGUID)
        {
            if (IsRegistered(data.instanceID, out _, EStructType.STRUCTURE))
            {
                L.LogDebug("Vehicle spawn was deregistered because the structure was salvaged or destroyed.");
                DeleteSpawn(data.instanceID, EStructType.STRUCTURE);
            }
        }
    }
    public static void SaveSingleton()
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
        Singleton.Save();
    }
    public static void RespawnAllVehicles()
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
        L.Log("Respawning vehicles...", ConsoleColor.Magenta);
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = VehicleManager.vehicles.Count - 1; i >= 0; i--)
        {
            InteractableVehicle v = VehicleManager.vehicles[i];
            VehicleBay.DeleteVehicle(v);
        }
        foreach (VehicleSpawn spawn in Singleton)
        {
            spawn.SpawnVehicle();
        }
    }
    public static void CreateSpawn(BarricadeDrop drop, SDG.Unturned.BarricadeData data, Guid vehicleID)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleSpawn spawn = new VehicleSpawn(data.instanceID, vehicleID, EStructType.BARRICADE, new SerializableTransform(drop.model));
        spawn.Initialize();
        Singleton.AddObjectToSave(spawn);
        StructureSaver.AddStructure(drop, data, out _);
        spawn.SpawnVehicle();
    }
    public static void CreateSpawn(StructureDrop drop, SDG.Unturned.StructureData data, Guid vehicleID)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleSpawn spawn = new VehicleSpawn(data.instanceID, vehicleID, EStructType.STRUCTURE, new SerializableTransform(drop.model));
        spawn.Initialize();
        Singleton.AddObjectToSave(spawn);
        StructureSaver.AddStructure(drop, data, out _);
        spawn.SpawnVehicle();
    }
    public static void DeleteSpawn(uint barricadeInstanceID, EStructType type)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleSpawn spawn = Singleton.GetObject(s => s.SpawnPadInstanceID == barricadeInstanceID && s.type == type);
        if (spawn != null)
        {
            spawn.IsActive = false;
            spawn.initialized = false;
            if (spawn.type == EStructType.STRUCTURE && spawn.StructureDrop != null && spawn.StructureDrop.model.TryGetComponent(out VehicleBayComponent vbc))
            {
                UnityEngine.Object.Destroy(vbc);
            }
            if (VehicleSigns.Loaded)
            {
                foreach (VehicleSign sign in VehicleSigns.GetLinkedSigns(spawn))
                {
                    if (sign.SignInteractable != null)
                        VehicleSigns.UnlinkSign(sign.SignInteractable);
                }
            }
        }
        Singleton.RemoveWhere(s => s.SpawnPadInstanceID == barricadeInstanceID && s.type == type);
        if (StructureSaver.StructureExists(barricadeInstanceID, type, out Structures.Structure structure))
            StructureSaver.RemoveStructure(structure);
    }
    public static bool IsRegistered(uint barricadeInstanceID, out VehicleSpawn spawn, EStructType type)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (type == EStructType.BARRICADE)
            return Singleton.ObjectExists(s => barricadeInstanceID == s.SpawnPadInstanceID, out spawn);
        else if (type == EStructType.STRUCTURE)
            return Singleton.ObjectExists(s => barricadeInstanceID == s.SpawnPadInstanceID, out spawn);
        else
        {
            spawn = null!;
            return false;
        }
    }
    public static bool IsRegistered(SerializableTransform transform, out VehicleSpawn spawn, EStructType type)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (type == EStructType.BARRICADE)
            return Singleton.ObjectExists(s => s.type == EStructType.BARRICADE && s.BarricadeDrop != null && transform == s.BarricadeDrop.model.transform, out spawn);
        else if (type == EStructType.STRUCTURE)
            return Singleton.ObjectExists(s => s.type == EStructType.STRUCTURE && s.StructureDrop != null && transform == s.StructureDrop.model.transform, out spawn);
        else
        {
            spawn = null!;
            return false;
        }
    }
    public static bool UnusedSpawnExists(Guid vehicleID, out VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
        return Singleton.ObjectExists(s =>
        {
            if (s.VehicleID == vehicleID && s.VehicleInstanceID != 0)
            {
                var vehicle = VehicleManager.getVehicle(s.VehicleInstanceID);
                return vehicle != null && !vehicle.isDead && !vehicle.isDrowned;
            }
            return false;
        }, out spawn);
    }

    public static bool SpawnExists(uint bayInstanceID, EStructType type, out VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
        return Singleton.ObjectExists(s => s.SpawnPadInstanceID == bayInstanceID && s.type == type, out spawn);
    }

    public static bool HasLinkedSpawn(uint vehicleInstanceID, out VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
        return Singleton.ObjectExists(s => s.VehicleInstanceID == vehicleInstanceID, out spawn);
    }
    private void OnPlayerEnterMain(SteamPlayer player, ulong team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        player.SendChat("entered_main", TeamManager.TranslateName(team, player, true));
        for (int i = 0; i < Count; i++)
        {
            VehicleSpawn spawn = this[i];
            if (spawn.LinkedSign != null && spawn.LinkedSign.SignDrop != null && (
                team == 1 && TeamManager.Team1Main.IsInside(spawn.LinkedSign.SignDrop.model.transform.position) || 
                team == 2 && TeamManager.Team2Main.IsInside(spawn.LinkedSign.SignDrop.model.transform.position)))
                spawn.UpdateSign(player);
        }
    }
    private void OnPlayerLeftMain(SteamPlayer player, ulong team)
    {
        player.SendChat("left_main", TeamManager.TranslateName(team, player, true));
    }
    public static void UpdateSigns(Guid vehicle)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Singleton.Count; i++)
        {
            VehicleSpawn spawn = Singleton[i];
            if (spawn.VehicleID == vehicle)
                spawn.UpdateSign();
        }
    }
    public static void UpdateSigns(UCPlayer player)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Singleton.Count; i++)
        {
            Singleton[i].UpdateSign(player.Player.channel.owner);
        }
    }
    public static void UpdateSigns()
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Singleton.Count; i++)
            Singleton[i].UpdateSign();
    }
    public static void UpdateSigns(uint vehicle)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Singleton.Count; i++)
        {
            VehicleSpawn spawn = Singleton[i];
            if (spawn.VehicleInstanceID == vehicle)
                spawn.UpdateSign();
        }
    }
    public static void UpdateSignsWhere(Predicate<VehicleSpawn> selector)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Singleton.Count; i++)
        {
            VehicleSpawn spawn = Singleton[i];
            if (selector(spawn))
                spawn.UpdateSign();
        }
    }

    internal static bool TryGetSpawnFromSign(InteractableSign sign, out VehicleSpawn spawn)
    {
        Singleton.AssertLoaded<VehicleSpawner, VehicleSpawn>();
        for (int i = 0; i < Singleton.Count; i++)
        {
            spawn = Singleton[i];
            if (spawn.LinkedSign is not null && spawn.LinkedSign.SignInteractable == sign)
                return true;
        }
        spawn = null!;
        return false;
    }
}
[JsonSerializable(typeof(VehicleSpawn))]
public class VehicleSpawn
{
    public uint SpawnPadInstanceID;
    public Guid VehicleID;
    public EStructType type;
    public SerializableTransform SpawnpadLocation;
    [JsonIgnore]
    public uint VehicleInstanceID { get; internal set; }
    [JsonIgnore]
    public BarricadeDrop? BarricadeDrop { get; internal set; }
    [JsonIgnore]
    public BarricadeData? BarricadeData { get; internal set; }
    [JsonIgnore]
    public StructureDrop? StructureDrop { get; internal set; }
    [JsonIgnore]
    public StructureData? StructureData { get; internal set; }
    [JsonIgnore]
    public bool IsActive;
    [JsonIgnore]
    public bool initialized = false;
    [JsonIgnore]
    public VehicleSign? LinkedSign;

    [JsonIgnore]
    public VehicleBayComponent? Component
    {
        get
        {
            if (type == EStructType.STRUCTURE)
            {
                if (StructureDrop != null && StructureDrop.model.TryGetComponent(out VehicleBayComponent comp))
                    return comp;
                else return null;
            }
            else if (type == EStructType.BARRICADE)
            {
                if (BarricadeDrop != null && BarricadeDrop.model.TryGetComponent(out VehicleBayComponent comp))
                    return comp;
                else return null;
            }
            else return null;
        }
    }
    public override string ToString() => $"Instance id: {SpawnPadInstanceID}, guid: {VehicleID}, type: {type}";
    public VehicleSpawn(uint spawnPadInstanceId, Guid vehicleID, EStructType type, SerializableTransform loc)
    {
        SpawnPadInstanceID = spawnPadInstanceId;
        VehicleID = vehicleID;
        VehicleInstanceID = 0;
        IsActive = true;
        this.type = type;
        initialized = false;
        SpawnpadLocation = loc;
    }
    public VehicleSpawn()
    {
        SpawnPadInstanceID = 0;
        VehicleID = Guid.Empty;
        VehicleInstanceID = 0;
        IsActive = true;
        type = EStructType.BARRICADE;
        initialized = false;
        SpawnpadLocation = default;
    }
    public void Initialize()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            if (type == EStructType.BARRICADE)
            {
                BarricadeData = UCBarricadeManager.GetBarricadeFromInstID(SpawnPadInstanceID, out BarricadeDrop? drop);
                BarricadeDrop = drop;
                initialized = BarricadeData != null;
                if (!initialized)
                {
                    L.LogWarning("VEHICLE SPAWNER ERROR: corresponding BarricadeDrop could not be found, attempting to replace the barricade.");
                    if (!StructureSaver.StructureExists(SpawnPadInstanceID, EStructType.BARRICADE, out Structures.Structure? structure))
                    {
                        if (SpawnpadLocation != default(SerializableTransform))
                            structure = StructureSaver.FindStructure(x => x.transform == SpawnpadLocation && x.type == EStructType.BARRICADE);
                        if (structure == null)
                        {
                            L.LogError("VEHICLE SPAWNER ERROR: barricade save not found.");
                            initialized = false;
                            IsActive = false;
                            return;
                        }
                    }
                    if (Assets.find(structure.id) is not ItemBarricadeAsset asset)
                    {
                        L.LogError("VEHICLE SPAWNER ERROR: barricade asset not found.");
                        initialized = false;
                        IsActive = false;
                        return;
                    }
                    Transform newBarricade = BarricadeManager.dropNonPlantedBarricade(
                        new Barricade(asset, asset.health, structure.Metadata),
                        structure.transform.position.Vector3, structure.transform.Rotation, structure.owner, structure.group);
                    if (newBarricade == null)
                    {
                        L.LogError("VEHICLE SPAWNER ERROR: barricade could not be spawned.");
                        initialized = false;
                        IsActive = false;
                        return;
                    }
                    BarricadeDrop newdrop = BarricadeManager.FindBarricadeByRootTransform(newBarricade);
                    if (newdrop != null)
                    {
                        BarricadeDrop = newdrop;
                        BarricadeData = newdrop.GetServersideData();
                        structure.instance_id = newdrop.instanceID;
                        SpawnPadInstanceID = newdrop.instanceID;
                        SpawnpadLocation = new SerializableTransform(newdrop.model);
                        VehicleSpawner.SaveSingleton();
                        StructureSaver.SaveSingleton();
                        initialized = true;
                    }
                    else
                    {
                        L.LogError("VEHICLE SPAWNER ERROR: spawned barricade could not be found.");
                        initialized = false;
                    }
                }
                else if (SpawnpadLocation != drop!.model)
                {
                    SpawnpadLocation = new SerializableTransform(drop.model);
                    VehicleSpawner.SaveSingleton();
                }
                if (BarricadeDrop != null)
                {
                    if (!VehicleBay.VehicleExists(this.VehicleID, out VehicleData data))
                        L.LogError("VEHICLE SPAWNER ERROR: Failed to find vehicle data for " + VehicleID.ToString("N"));
                    else if (!BarricadeDrop.model.TryGetComponent<VehicleBayComponent>(out _))
                        BarricadeDrop.model.transform.gameObject.AddComponent<VehicleBayComponent>().Init(this, data);
                }
                else
                {
                    L.LogError("VEHICLE SPAWNER ERROR: Failed to find or create the barricade drop.");
                }
            }
            else if (type == EStructType.STRUCTURE)
            {
                StructureDrop = UCBarricadeManager.GetStructureFromInstID(SpawnPadInstanceID);
                initialized = StructureDrop != null;
                if (!initialized)
                {
                    L.LogWarning("VEHICLE SPAWNER ERROR: corresponding StructureDrop could not be found, attempting to replace the structure.");
                    if (!StructureSaver.StructureExists(SpawnPadInstanceID, EStructType.STRUCTURE, out Structures.Structure? structure))
                    {
                        if (SpawnpadLocation != default(SerializableTransform))
                            structure = StructureSaver.FindStructure(x => x.transform == SpawnpadLocation && x.type == EStructType.STRUCTURE);
                        if (structure == null)
                        {
                            L.LogError("VEHICLE SPAWNER ERROR: structure save not found.");
                            initialized = false;
                            IsActive = false;
                            return;
                        }
                    }
                    if (!(Assets.find(structure.id) is ItemStructureAsset asset))
                    {
                        L.LogError("VEHICLE SPAWNER ERROR: structure asset not found.");
                        initialized = false;
                        IsActive = false;
                        return;
                    }
                    if (!StructureManager.dropStructure(
                        new SDG.Unturned.Structure(asset, ushort.MaxValue),
                        structure.transform.position.Vector3, structure.transform.euler_angles.x, structure.transform.euler_angles.y,
                        structure.transform.euler_angles.z, structure.owner, structure.group))
                    {
                        L.LogWarning("VEHICLE SPAWNER ERROR: Structure could not be replaced");
                        initialized = false;
                    }
                    else if (Regions.tryGetCoordinate(structure.transform.position.Vector3, out byte x, out byte y))
                    {
                        StructureDrop newdrop = StructureManager.regions[x, y].drops.LastOrDefault();
                        if (newdrop == null)
                        {
                            L.LogWarning("VEHICLE SPAWNER ERROR: Spawned structure could not be found");
                            initialized = false;
                        }
                        else
                        {
                            StructureData = newdrop.GetServersideData();
                            StructureDrop = newdrop;
                            structure.instance_id = newdrop.instanceID;
                            SpawnPadInstanceID = newdrop.instanceID;
                            VehicleSpawner.SaveSingleton();
                            StructureSaver.SaveSingleton();
                            initialized = true;
                        }
                    }
                    else
                    {
                        L.LogWarning("VEHICLE SPAWNER ERROR: Unable to get region coordinates.");
                        initialized = false;
                    }
                }
                else
                    StructureData = StructureDrop!.GetServersideData();
                if (StructureDrop != null)
                {
                    if (!VehicleBay.VehicleExists(this.VehicleID, out VehicleData data))
                        L.LogError("VEHICLE SPAWNER ERROR: Failed to find vehicle data for " + VehicleID.ToString("N"));
                    else if (!StructureDrop.model.TryGetComponent<VehicleBayComponent>(out _))
                        StructureDrop.model.transform.gameObject.AddComponent<VehicleBayComponent>().Init(this, data);
                }
                else
                {
                    L.LogError("VEHICLE SPAWNER ERROR: Failed to find or create the structure drop.");
                }
            }
            IsActive = initialized;
        }
        catch (Exception ex)
        {
            initialized = false;
            L.LogError("Error initializing vehicle spawn: ");
            L.LogError(ex);
        }
    }
    public void SpawnVehicle()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (HasLinkedVehicle(out _))
        {
            L.LogDebug("Could not spawn vehicle because another is already linked");
            return;
        }
        try
        {
            if (!initialized)
            {
                L.LogError($"VEHICLE SPAWNER ERROR: Tried to spawn vehicle without Initializing. {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} spawn.");
                return;
            }
            if (type == EStructType.BARRICADE)
            {
                if (BarricadeData == default)
                {
                    L.LogError($"VEHICLE SPAWNER ERROR: {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} at spawn {SpawnpadLocation} was unable to find BarricadeData.");
                    return;
                }
                Quaternion rotation = new Quaternion
                { eulerAngles = new Vector3((BarricadeData.angle_x * 2) + 90, BarricadeData.angle_y * 2, BarricadeData.angle_z * 2) };
                InteractableVehicle? veh = VehicleBay.SpawnLockedVehicle(VehicleID, new Vector3(BarricadeData.point.x, BarricadeData.point.y + VehicleSpawner.VEHICLE_HEIGHT_OFFSET, BarricadeData.point.z), rotation, out _);
                if (veh == null)
                {
                    L.LogWarning("VEHICLE SPAWNER ERROR: " + (Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N")) + " returned null.");
                    return;
                }
                LinkNewVehicle(veh);
                UpdateSign();
                if (UCWarfare.Config.Debug)
                    L.Log($"VEHICLE SPAWNER: spawned {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} at spawn {BarricadeData.point}", ConsoleColor.DarkGray);
            }
            else if (type == EStructType.STRUCTURE)
            {
                if (StructureData == default)
                {
                    L.LogError($"VEHICLE SPAWNER ERROR: {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} at spawn {SpawnpadLocation} was unable to find StructureData.");
                    return;
                }
                    
                Quaternion rotation = new Quaternion
                { eulerAngles = new Vector3(StructureData.angle_x * 2 + 90, StructureData.angle_y * 2, StructureData.angle_z * 2) };
                InteractableVehicle? veh = VehicleBay.SpawnLockedVehicle(VehicleID, new Vector3(StructureData.point.x, StructureData.point.y + VehicleSpawner.VEHICLE_HEIGHT_OFFSET, StructureData.point.z), rotation, out _);
                if (veh == null)
                {
                    L.LogWarning("VEHICLE SPAWNER ERROR: " + (Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N")) + " returned null.");
                    return;
                }
                LinkNewVehicle(veh);
                UpdateSign();
                if (UCWarfare.Config.Debug)
                    L.Log($"VEHICLE SPAWNER: spawned {(Assets.find(VehicleID) is VehicleAsset va ? (va.vehicleName + " - " + VehicleID.ToString("N")) : VehicleID.ToString("N"))} at spawn {StructureData.point}", ConsoleColor.DarkGray);
            }
        }
        catch (Exception ex)
        {
            L.LogError($"Error spawning vehicle {this.VehicleID} on vehicle bay: ");
            L.LogError(ex);
        }
    }
    public bool HasLinkedVehicle(out InteractableVehicle vehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (VehicleInstanceID == 0)
        {
            vehicle = null!;
            return false;
        }
        vehicle = VehicleManager.getVehicle(VehicleInstanceID);
        return vehicle != null && !vehicle.isDead && !vehicle.isDrowned;
    }
    public void LinkNewVehicle(InteractableVehicle vehicle)
    {
        VehicleInstanceID = vehicle.instanceID;
        if (type == EStructType.STRUCTURE)
        {
            if (StructureDrop != null && StructureDrop.model.TryGetComponent(out VehicleBayComponent comp))
                comp.OnSpawn(vehicle);
        }
        else if (type == EStructType.BARRICADE && BarricadeDrop != null && BarricadeDrop.model.TryGetComponent(out VehicleBayComponent comp))
            comp.OnSpawn(vehicle);
    }
    public void Unlink()
    {
        VehicleInstanceID = 0;
    }

    public void UpdateSign(SteamPlayer player)
    {
        if (this.LinkedSign == null || this.LinkedSign.SignInteractable == null || this.LinkedSign.SignDrop == null) return;
        UpdateSignInternal(player, this);
    }
    private static IEnumerator<SteamPlayer> BasesToPlayer(IEnumerator<KeyValuePair<ulong, byte>> b, byte team)
    {
        while (b.MoveNext())
        {
            if (b.Current.Value == team)
            {
                for (int i = 0; i < Provider.clients.Count; i++)
                {
                    if (Provider.clients[i].playerID.steamID.m_SteamID == b.Current.Key)
                        yield return Provider.clients[i];
                }
            }
        }
        b.Dispose();
    }
    public void UpdateSign()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (this.LinkedSign == null || this.LinkedSign.SignInteractable == null || this.LinkedSign.SignDrop == null) return;
        if (TeamManager.Team1Main.IsInside(LinkedSign.SignDrop.model.transform.position))
        {
            IEnumerator<SteamPlayer> t1Main = BasesToPlayer(TeamManager.PlayerBaseStatus.GetEnumerator(), 1);
            UpdateSignInternal(t1Main, this);
        }
        else if (TeamManager.Team2Main.IsInside(LinkedSign.SignDrop.model.transform.position))
        {
            IEnumerator<SteamPlayer> t2Main = BasesToPlayer(TeamManager.PlayerBaseStatus.GetEnumerator(), 2);
            UpdateSignInternal(t2Main, this);
        }
        else if (Regions.tryGetCoordinate(LinkedSign.SignDrop.model.transform.position, out byte x, out byte y))
        {
            IEnumerator<SteamPlayer> everyoneElse = F.EnumerateClients_Remote(x, y, BarricadeManager.BARRICADE_REGIONS).GetEnumerator();
            UpdateSignInternal(everyoneElse, this);
        }
        else
        {
            L.LogWarning($"Vehicle sign not in main bases or any region!");
        }
    }
    private static void UpdateSignInternal(IEnumerator<SteamPlayer> players, VehicleSpawn spawn)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (spawn.LinkedSign != null && spawn.LinkedSign.SignInteractable != null)
        {
            if (!VehicleBay.VehicleExists(spawn.VehicleID, out VehicleData data))
                return;
            foreach (LanguageSet set in Translation.EnumerateLanguageSets(players))
            {
                string val = Translation.TranslateVBS(spawn, data, set.Language);
                NetId id = spawn.LinkedSign.SignInteractable.GetNetId();
                while (set.MoveNext())
                {
                    try
                    {
                        string val2 = string.Format(val, data.GetCostLine(set.Next));
                        Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, set.Next.Player.channel.owner.transportConnection, val2);
                    }
                    catch (FormatException)
                    {
                        Data.SendChangeText.Invoke(id, ENetReliability.Unreliable, set.Next.Player.channel.owner.transportConnection, val);
                        L.LogError("Formatting error in send vbs!");
                    }
                }
            }
        }
        players.Dispose();
    }
    private void UpdateSignInternal(SteamPlayer player, VehicleSpawn spawn)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (spawn.LinkedSign != null && spawn.LinkedSign.SignInteractable != null)
        {
            if (!VehicleBay.VehicleExists(spawn.VehicleID, out VehicleData data))
                return;
            if (!Data.Languages.TryGetValue(player.playerID.steamID.m_SteamID, out string lang))
                lang = JSONMethods.DEFAULT_LANGUAGE;
            string val = Translation.TranslateVBS(spawn, data, lang);
            try
            {
                UCPlayer? pl = UCPlayer.FromSteamPlayer(player);
                if (pl == null) return;
                string val2 = string.Format(val, data.GetCostLine(pl));
                Data.SendChangeText.Invoke(spawn.LinkedSign.SignInteractable.GetNetId(), ENetReliability.Unreliable,
                    player.transportConnection, val2);
            }
            catch (FormatException)
            {
                Data.SendChangeText.Invoke(spawn.LinkedSign.SignInteractable.GetNetId(), ENetReliability.Unreliable,
                    player.transportConnection, val);
                L.LogError("Formatting error in send vbs!");
            }
        }
    }
}
public enum EVehicleBayState : byte
{
    UNKNOWN = 0,
    READY = 1,
    IDLE = 2,
    DEAD = 3,
    TIME_DELAYED = 4,
    IN_USE = 5,
    DELAYED = 6,
    NOT_INITIALIZED = 255,
}

public sealed class VehicleBayComponent : MonoBehaviour
{
    private VehicleSpawn spawnData;
    private VehicleData vehicleData;
    public VehicleSpawn Spawn => spawnData;
    private EVehicleBayState state = EVehicleBayState.NOT_INITIALIZED;
    private InteractableVehicle? vehicle;
    public EVehicleBayState State => state;
    public Vector3 lastLoc = Vector3.zero;

    public void Init(VehicleSpawn spawn, VehicleData data)
    {
        spawnData = spawn;
        vehicleData = data;
        state = EVehicleBayState.UNKNOWN;
    }
    private int lastLocIndex = -1;
    private float idleStartTime = -1f;
    public string CurrentLocation = string.Empty;
    public float IdleTime;
    public float DeadTime;
    private float deadStartTime = -1f;
    private float lastIdleCheck = 0f;
    private float lastSignUpdate = 0f;
    private float lastLocCheck = 0f;
    private float lastDelayCheck = 0f;
    public void OnSpawn(InteractableVehicle vehicle)
    {
        this.vehicle = vehicle;
        IdleTime = 0;
        DeadTime = 0;
        if (vehicleData.IsDelayed(out Delay delay))
            this.state = delay.type == EDelayType.TIME ? EVehicleBayState.TIME_DELAYED : EVehicleBayState.DELAYED;
        else this.state = EVehicleBayState.READY;
    }
    public void OnRequest()
    {
        state = EVehicleBayState.IN_USE;
    }
    private bool checkTime = false;
    public void UpdateTimeDelay()
    {
        checkTime = true;
    }
    void Update()
    {
        if (state == EVehicleBayState.NOT_INITIALIZED) return;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float time = Time.realtimeSinceStartup;
        if (checkTime || (time - lastDelayCheck > 1f && (state == EVehicleBayState.UNKNOWN || state == EVehicleBayState.DELAYED || state == EVehicleBayState.TIME_DELAYED)))
        {
            lastDelayCheck = time;
            checkTime = false;
            if (vehicleData.IsDelayed(out Delay delay))
            {
                if (delay.type == EDelayType.TIME)
                {
                    state = EVehicleBayState.TIME_DELAYED;
                    lastSignUpdate = time;
                    UpdateSign();
                    return;
                }
                else if (state != EVehicleBayState.DELAYED)
                {
                    state = EVehicleBayState.DELAYED;
                    UpdateSign();
                }
            }
            else if (vehicle != null && spawnData.HasLinkedVehicle(out InteractableVehicle veh) && !veh.lockedOwner.IsValid())
            {
                state = EVehicleBayState.READY;
                UpdateSign();
            }
            else
            {
                state = EVehicleBayState.IN_USE;
                UpdateSign();
            }
        }
        if ((state == EVehicleBayState.IDLE || state == EVehicleBayState.IN_USE) && time - lastIdleCheck >= 4f)
        {
            lastIdleCheck = time;
            if (vehicle != null && (vehicle.anySeatsOccupied || PlayerManager.IsPlayerNearby(vehicle.lockedOwner.m_SteamID, 150f, vehicle.transform.position)))
            {
                if (state != EVehicleBayState.IN_USE)
                {
                    state = EVehicleBayState.IN_USE;
                    UpdateSign();
                }
            }
            else if (state != EVehicleBayState.IDLE)
            {
                idleStartTime = time;
                IdleTime = 0f;
                state = EVehicleBayState.IDLE;
                UpdateSign();
            }
        }
        if (state != EVehicleBayState.DEAD && state >= EVehicleBayState.READY && state <= EVehicleBayState.IN_USE)
        {
            if (vehicle == null || vehicle.isDead || vehicle.isExploded)
            {
                // carry over idle time to dead timer
                if (state == EVehicleBayState.IDLE)
                {
                    deadStartTime = idleStartTime;
                    DeadTime = deadStartTime == -1 ? 0 : time - deadStartTime;
                }
                else
                {
                    deadStartTime = time;
                    DeadTime = 0f;
                }
                state = EVehicleBayState.DEAD;
                UpdateSign();
            }
        }
        if (state == EVehicleBayState.IN_USE && time - lastLocCheck > 4f && vehicle != null && !vehicle.isDead)
        {
            lastLocCheck = time;
            if (lastLoc != vehicle.transform.position)
            {
                lastLoc = vehicle.transform.position;
                int ind = F.GetClosestLocationIndex(lastLoc);
                if (ind != lastLocIndex)
                {
                    lastLocIndex = ind;
                    CurrentLocation = ((LocationNode)LevelNodes.nodes[ind]).name;
                    UpdateSign();
                }
            }
        }
        if (state == EVehicleBayState.IDLE)
            IdleTime = idleStartTime == -1 ? 0 : time - idleStartTime;
        else if (state == EVehicleBayState.DEAD)
            DeadTime = deadStartTime == -1 ? 0 : time - deadStartTime;
        if ((state == EVehicleBayState.IDLE && IdleTime > vehicleData.RespawnTime) || (state == EVehicleBayState.DEAD && DeadTime > vehicleData.RespawnTime))
        {
            if (vehicle != null)
            {
                VehicleBay.DeleteVehicle(vehicle);
            }
            vehicle = null;
            spawnData.SpawnVehicle();
        }
        if ((state == EVehicleBayState.IDLE || state == EVehicleBayState.DEAD || state == EVehicleBayState.TIME_DELAYED) && time - lastSignUpdate >= 1f)
        {
            lastSignUpdate = time;
            UpdateSign();
        }
    }
    void OnDestroy()
    {
        state = EVehicleBayState.UNKNOWN;
        UpdateSign();
    }
    private void UpdateSign() => spawnData.UpdateSign();
}