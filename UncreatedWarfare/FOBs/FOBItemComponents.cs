﻿using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Structures;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using UnityEngine;
using XPReward = Uncreated.Warfare.Levels.XPReward;

namespace Uncreated.Warfare.FOBs;

public class RadioComponent : MonoBehaviour, IManualOnDestroy, IFOBItem, IShovelable, ISalvageListener
{
    private bool _destroyed;
#nullable disable
    public FOB FOB { get; set; }
#nullable restore
    public RadioState State { get; private set; }
    public BuildableType Type => BuildableType.Radio;
    public BarricadeDrop Barricade { get; private set; }
    public BuildableData? Buildable => null;
    public ulong Owner { get; private set; }
    public JsonAssetReference<EffectAsset>? Icon { get; private set; }
    public ulong Team { get; private set; }
    public bool IsSalvaged { get; set; }
    public ulong Salvager { get; set; }
    public bool Destroyed => _destroyed;
    public TickResponsibilityCollection Builders { get; } = new TickResponsibilityCollection();
    public Vector3 Position => transform.position;

    [UsedImplicitly]
    private void Awake()
    {
        Barricade = BarricadeManager.FindBarricadeByRootTransform(transform);
        if (Barricade == null)
        {
            L.LogDebug($"[FOBS] RadioComponent added to unknown barricade: {name}.");
            goto destroy;
        }
        
        if (!Gamemode.Config.FOBRadios.Value.HasGuid(Barricade.asset.GUID))
        {
            if (Gamemode.Config.BarricadeFOBRadioDamaged.MatchGuid(Barricade.asset))
            {
                State = RadioState.Bleeding;
                Icon = Gamemode.Config.EffectMarkerRadioDamaged;
            }
            else
            {
                L.LogDebug($"[FOBS] RadioComponent unable to find a valid buildable: {(Buildable?.Foundation?.Value?.Asset?.itemName)}.");
                goto destroy;
            }
        }
        else
        {
            State = RadioState.Alive;
            Icon = Gamemode.Config.EffectMarkerRadio;
        }

        Owner = Barricade.GetServersideData().owner;
        Team = Barricade.GetServersideData().group.GetTeam();
        Builders.Set(Owner, FOBManager.Config.BaseFOBRepairHits);

        if (State == RadioState.Alive)
        {
            if (Gamemode.Config.EffectMarkerRadio.ValidReference(out Guid guid))
                IconManager.AttachIcon(guid, transform, Team, 3.5f);
        }
        else if (State == RadioState.Bleeding)
        {
            if (Gamemode.Config.EffectMarkerRadioDamaged.ValidReference(out Guid guid))
                IconManager.AttachIcon(guid, transform, Team, 3.5f);
        }

        if (Barricade.interactable is InteractableStorage storage)
            storage.despawnWhenDestroyed = true;

        L.LogDebug("[FOBS] Radio Initialized: " + Barricade.asset.itemName + ". (State: " + State + ").");
        return;
        destroy:
        State = RadioState.Destroyed;
        Destroy(this);
    }
    void ISalvageListener.OnSalvageRequested(SalvageRequested e)
    {
        if (!e.Player.OnDuty())
        {
            L.Log($"[FOBS] [{FOB?.Name ?? "FLOATING"}] {e.Player} tried to salvage the radio.");
            e.Break();
            e.Player.SendChat(T.WhitelistProhibitedSalvage, Barricade.asset);
        }
    }
    [UsedImplicitly]
    private void Start()
    {
        FOB?.Restock();
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        if (!_destroyed && Barricade != null && Barricade.model != null &&
            BarricadeManager.tryGetRegion(Barricade.model, out byte x, out byte y, out ushort plant, out _))
        {
            BarricadeManager.destroyBarricade(Barricade, x, y, plant);
            _destroyed = true;
            Barricade = null!;
        }

        FOBManager.EnsureDisposed(this);

        State = RadioState.Destroyed;
    }

    void IManualOnDestroy.ManualOnDestroy()
    {
        _destroyed = true;
        Destroy(this);
    }

    public enum RadioState
    {
        Alive,
        Bleeding,
        Destroyed
    }

    public bool Shovel(UCPlayer shoveler)
    {
        if (State == RadioState.Bleeding && shoveler.GetTeam() == Team)
        {
            ushort maxHealth = Barricade.asset.health;
            float amt = maxHealth / FOBManager.Config.BaseFOBRepairHits * FOBManager.GetBuildIncrementMultiplier(shoveler);

            BarricadeManager.repair(Barricade.model, amt, 1, shoveler.CSteamID);
            FOBManager.TriggerBuildEffect(transform.position);
            Builders.Increment(shoveler.Steam64, amt);
            UpdateHitsUI();

            if (Barricade.GetServersideData().barricade.health >= maxHealth)
            {
                FOB.UpdateRadioState(RadioState.Alive);
            }

            return true;
        }

        return false;
    }
    public void QuickShovel(UCPlayer shoveler)
    {
        if (State == RadioState.Bleeding)
        {
            ushort maxHealth = Barricade.asset.health;
            float amt = maxHealth - Barricade.GetServersideData().barricade.health;
            BarricadeManager.repair(Barricade.model, amt, 1, shoveler.CSteamID);
            FOBManager.TriggerBuildEffect(transform.position);
            Builders.Increment(shoveler.Steam64, amt);
            UpdateHitsUI();

            FOB.UpdateRadioState(RadioState.Alive);
        }
    }
    private void UpdateHitsUI()
    {
        Builders.RetrieveLock();
        try
        {
            float time = Time.realtimeSinceStartup;
            ToastMessage msg = new ToastMessage(
                Points.GetProgressBar(Barricade.GetServersideData().barricade.health, Barricade.asset.health, 25).Colorize("ff9966"),
                ToastMessageSeverity.Progress);
            foreach (TickResponsibility responsibility in Builders)
            {
                if (time - responsibility.LastUpdated < 5f)
                {
                    if (UCPlayer.FromID(responsibility.Steam64) is { } pl && pl.Player.TryGetPlayerData(out UCPlayerData component))
                        component.QueueMessage(msg, true);
                }
            }
        }
        finally
        {
            Builders.ReturnLock();
        }
    }
}

public class ShovelableComponent : MonoBehaviour, IManualOnDestroy, IFOBItem, IShovelable, ISalvageListener
{
    private bool _destroyed;
    private bool _subbedToStructureEvent;
    private int _buildRemoved;
    public FOB? FOB { get; set; }
    public BuildableType Type { get; private set; }
    public BuildableState State { get; private set; }
    public BuildableData Buildable { get; private set; }
    public IBuildable? ActiveStructure { get; private set; }
    public InteractableVehicle? ActiveVehicle { get; private set; }
    public IBuildable? Base { get; private set; }
    public Vector3 Position { get; private set; }
    public ulong Team { get; private set; }
    public ulong Owner { get; private set; }
    public float Progress { get; private set; }
    public float Total { get; private set; }
    public bool IsSalvaged { get; set; }
    public ulong Salvager { get; set; }
    public bool IsFloating { get; private set; }
    public JsonAssetReference<EffectAsset>? Icon { get; protected set; }
    public TickResponsibilityCollection Builders { get; } = new TickResponsibilityCollection();
    public Asset Asset { get; protected set; }

    [UsedImplicitly]
    private void Awake()
    {
        BarricadeDrop barricade = BarricadeManager.FindBarricadeByRootTransform(transform);
        if (barricade == null)
        {
            StructureDrop structure = StructureManager.FindStructureByRootTransform(transform);
            if (structure == null)
            {
                InteractableVehicle vehicle = DamageTool.getVehicle(transform);
                if (vehicle == null)
                {
                    L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] ShovelableComponent not added to barricade, structure, or vehicle: {name}.");
                    goto destroy;
                }

                ActiveVehicle = vehicle;
                ActiveStructure = null;
                Asset = vehicle.asset;
                Position = vehicle.transform.position;
                Team = vehicle.lockedGroup.m_SteamID.GetTeam();
                Owner = vehicle.lockedOwner.m_SteamID;
            }
            else
            {
                ActiveStructure = new UCStructure(structure);
                _subbedToStructureEvent = true;
                EventDispatcher.StructureDestroyed += OnStructureDestroyed;
                Asset = structure.asset;
                Position = structure.model.position;
                Team = structure.GetServersideData().group.GetTeam();
                Owner = structure.GetServersideData().owner;
            }
        }
        else
        {
            ActiveStructure = new UCBarricade(barricade);
            Asset = barricade.asset;
            Position = barricade.model.position;
            Team = barricade.GetServersideData().group.GetTeam();
            Owner = barricade.GetServersideData().owner;
        }

        Progress = 0f;

        Buildable = FOBManager.FindBuildable(Asset)!;
        Type = Buildable.Type;
        if (Buildable is not { Type: not BuildableType.Radio })
        {
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] ShovelableComponent unable to find a valid buildable: " +
                         $"{Buildable?.Foundation?.Value?.Asset?.itemName} ({Asset.FriendlyName}).");
            goto destroy;
        }

        if (ActiveStructure != null && Buildable.Foundation.MatchGuid(ActiveStructure.Asset.GUID))
        {
            Total = Buildable.RequiredHits;
            State = BuildableState.Foundation;
            if (Gamemode.Config.EffectMarkerBuildable.ValidReference(out Guid guid) && Buildable.RequiredHits < 15)
                IconManager.AttachIcon(guid, transform, Team, 2f);
        }
        else
            State = BuildableState.Full;

        InitAwake();
        L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] {Asset.FriendlyName} Initialized: {Buildable} in state: {State}.");
        return;
        destroy:
        Destroy(this);
    }

    [UsedImplicitly]
    private void Start()
    {
        IsFloating = FOB == null;
        InitStart();
    }

    [UsedImplicitly]
    private void OnDestroy()
    {
        if (_subbedToStructureEvent)
            EventDispatcher.StructureDestroyed -= OnStructureDestroyed;
        Destroy();
        if (!_destroyed)
        {
            if (ActiveStructure != null)
            {
                if (ActiveStructure.Destroy())
                {
                    _destroyed = true;
                    ActiveStructure = null!;
                }
            }
            else if (ActiveVehicle != null)
            {
                for (int i = 0; i < ActiveVehicle.turrets.Length; ++i)
                {
                    byte[] state = ActiveVehicle.turrets[i].state;
                    if (state.Length != 18)
                        continue;
                    Attachments.parseFromItemState(state, out _, out _, out _, out _, out ushort mag);
                    byte amt = state[10];
                    if (mag != 0 && Assets.find(EAssetType.ITEM, mag) is ItemMagazineAsset asset)
                        ItemManager.dropItem(new Item(asset.id, amt, 100), ActiveVehicle.transform.position, true, false, true);
                }
                VehicleBarricadeRegion region = BarricadeManager.findRegionFromVehicle(ActiveVehicle);
                if (region != null)
                {
                    for (int i = 0; i < region.drops.Count; ++i)
                    {
                        if (region.drops[i].interactable is InteractableStorage st)
                            st.despawnWhenDestroyed = true;
                    }
                }
                if (!ActiveVehicle.isExploded)
                    VehicleManager.sendVehicleExploded(ActiveVehicle);
                ActiveVehicle = null;
                _destroyed = true;
            }
        }
        if (Base != null && Base.Destroy())
        {
            _destroyed = true;
            ActiveStructure = null!;
        }


        FOBManager.EnsureDisposed(this);
        L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Destroyed: {Buildable} ({Asset.FriendlyName}).");
    }
    void IManualOnDestroy.ManualOnDestroy()
    {
        _destroyed = true;
        Destroy(this);
    }
    void ISalvageListener.OnSalvageRequested(SalvageRequested e)
    {
        if (State != BuildableState.Foundation || _buildRemoved > 0 || !e.Player.OnDuty())
        {
            L.Log($"[FOBS] [{FOB?.Name ?? "FLOATING"}] {e.Player} tried to salvage {Buildable}.");
            e.Break();
            e.Player.SendChat(T.WhitelistProhibitedSalvage, ActiveStructure?.Asset ?? Buildable.Foundation.GetAsset()!);
        }
    }
    private void OnStructureDestroyed(StructureDestroyed e)
    {
        if (ActiveStructure != null && ActiveStructure.Type == StructType.Structure && ActiveStructure.InstanceId == e.InstanceID)
        {
            _destroyed = true;
            Destroy(this);
        }
    }
    protected virtual void InitAwake() { }
    protected virtual void InitStart() { }
    protected virtual void Destroy() { }

    public bool Shovel(UCPlayer shoveler)
    {
        if (State == BuildableState.Foundation && shoveler.GetTeam() == Team)
        {
            if (FOB == null && !IsFloating)
            {
                shoveler.SendChat(T.BuildTickNotInRadius);
                return true;
            }
            if (!IsFloating && Buildable.Type == BuildableType.Bunker && FOB!.Bunker != null)
            {
                shoveler.SendChat(T.BuildTickStructureExists, Buildable);
                return true;
            }
            if (!IsFloating && !FOB!.ValidatePlacement(Buildable, shoveler, this) ||
                IsFloating && Data.Is(out IFOBs fobs) && !fobs.FOBManager.ValidateFloatingPlacement(Buildable, shoveler, transform.position, this))
            {
                return false;
            }

            float amount = FOBManager.GetBuildIncrementMultiplier(shoveler);

            L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Incrementing build: {shoveler} ({Progress} + {amount} = {Progress + amount} / {Total}).");
            Progress += amount;
            
            FOBManager.TriggerBuildEffect(transform.position);
            
            Builders.Increment(shoveler.Steam64, amount);

            int build = Mathf.FloorToInt(Buildable.RequiredHits / Buildable.RequiredBuild * amount);
            _buildRemoved += build;
            FOB?.ModifyBuild(-build);

            UpdateHitsUI();

            if (Progress >= Total)
                Build();

            return true;
        }

        return false;
    }
    public void QuickShovel(UCPlayer shoveler)
    {
        if (State == BuildableState.Foundation)
        {
            float amount = Total - Progress;
            L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Incrementing build: {shoveler} ({Progress} + {amount} = {Progress + amount} / {Total}).");
            Progress += amount;

            FOBManager.TriggerBuildEffect(transform.position);

            Builders.Increment(shoveler.Steam64, amount);
            UpdateHitsUI();

            if (Progress >= Total)
                Build();
        }
    }

    private void UpdateHitsUI()
    {
        Builders.RetrieveLock();
        try
        {
            float time = Time.realtimeSinceStartup;
            ToastMessage msg = new ToastMessage(Points.GetProgressBar(Progress, Total, 25), ToastMessageSeverity.Progress);
            foreach (TickResponsibility responsibility in Builders)
            {
                if (time - responsibility.LastUpdated < 5f)
                {
                    if (UCPlayer.FromID(responsibility.Steam64) is { } pl && pl.Player.TryGetPlayerData(out UCPlayerData component))
                        component.QueueMessage(msg, true);
                }
            }
        }
        finally
        {
            Builders.ReturnLock();
        }
    }

    public bool Build()
    {
        if (State != BuildableState.Foundation)
            return false;
        IBuildable? newBase = null;
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        ulong group = TeamManager.GetGroupID(Team);
        // base
        if (Buildable.Emplacement != null && Buildable.Emplacement.BaseBarricade.ValidReference(out ItemAsset @base))
        {
            if (@base is ItemBarricadeAsset bAsset)
            {
                FOBManager.IgnorePlacingBarricade = true;
                try
                {
                    Barricade b = new Barricade(bAsset, bAsset.health, bAsset.getState());
                    Transform? t = BarricadeManager.dropNonPlantedBarricade(b, position, rotation, Owner, group);
                    BarricadeDrop? drop = t == null ? null : BarricadeManager.FindBarricadeByRootTransform(t);
                    if (drop != null)
                        newBase = new UCBarricade(drop);
                }
                finally
                {
                    FOBManager.IgnorePlacingBarricade = false;
                }
            }
            else if (@base is ItemStructureAsset sAsset)
            {
                FOBManager.IgnorePlacingStructure = true;
                try
                {
                    Structure s = new Structure(sAsset, sAsset.health);
                    bool success = StructureManager.dropReplicatedStructure(s, position, rotation, Owner, group);
                    if (success)
                    {
                        if (Regions.tryGetCoordinate(position, out byte x, out byte y) && StructureManager.tryGetRegion(x, y, out StructureRegion region))
                            newBase = new UCStructure(region.drops.GetTail());
                    }
                }
                finally
                {
                    FOBManager.IgnorePlacingStructure = false;
                }
            }


            if (newBase == null)
                L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Unable to place base: {@base.itemName}.");
            else
                L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Placed base: {@base.itemName}.");
        }

        Transform? newTransform = null;

        // emplacement
        if (Buildable.Emplacement != null && Buildable.Emplacement.EmplacementVehicle.ValidReference(out VehicleAsset vehicle))
        {
            InteractableVehicle veh = FOBManager.SpawnEmplacement(vehicle, position, rotation, Owner, group);

            if (veh == null)
                L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Unable to spawn vehicle: {vehicle.vehicleName}.");
            else
            {
                newTransform = veh.transform;
                L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Spawned vehicle: {vehicle.vehicleName}.");
            }
        }

        // fortification
        if (newTransform == null && Buildable.FullBuildable.ValidReference(out ItemAsset buildable))
        {
            if (buildable is ItemBarricadeAsset bAsset)
            {
                FOBManager.IgnorePlacingBarricade = true;
                try
                {
                    Barricade b = new Barricade(bAsset, bAsset.health, bAsset.getState());
                    Transform? t = BarricadeManager.dropNonPlantedBarricade(b, position, rotation, Owner, group);
                    BarricadeDrop? drop = t == null ? null : BarricadeManager.FindBarricadeByRootTransform(t);
                    if (drop != null)
                        newTransform = drop.model;
                }
                finally
                {
                    FOBManager.IgnorePlacingBarricade = false;
                }
            }
            else if (buildable is ItemStructureAsset sAsset)
            {
                FOBManager.IgnorePlacingStructure = true;
                try
                {
                    Structure s = new Structure(sAsset, sAsset.health);
                    bool success = StructureManager.dropReplicatedStructure(s, position, rotation, Owner, group);
                    if (success)
                    {
                        if (Regions.tryGetCoordinate(position, out byte x, out byte y) && StructureManager.tryGetRegion(x, y, out StructureRegion region))
                            newTransform = region.drops.GetTail().model;
                    }
                }
                finally
                {
                    FOBManager.IgnorePlacingStructure = false;
                }
            }

            if (newTransform == null)
                L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Unable to place buildable: {buildable.itemName}.");
            else
                L.LogDebug($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Placed buildable: {buildable.itemName}.");
        }

        if (newTransform == null)
        {
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Parent for buildable upgrade not spawned: {Buildable}.");
            if (newBase != null)
                newBase.Destroy();
            return false;
        }

        int buildRemaining = Buildable.RequiredBuild - _buildRemoved;
        FOB?.ModifyBuild(-buildRemaining);

        IFOBItem? @new = null;
        if (FOB != null)
            @new = FOB.UpgradeItem(this, newTransform);
        else if (Data.Is(out IFOBs fobs))
            @new = fobs.FOBManager.UpgradeFloatingItem(this, newTransform);
        
        if (@new == null)
        {
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] Unable to upgrade buildable: {Buildable}.");
            newBase?.Destroy();
            return false;
        }

        if (@new is ShovelableComponent sh)
            sh.Base = newBase;

        return true;
    }

    public enum BuildableState
    {
        Full,
        Foundation,
        Destroyed
    }
}

public class BunkerComponent : ShovelableComponent
{
    public Vector3 SpawnPosition => transform.position;
    public float SpawnYaw => transform.rotation.eulerAngles.y;
}

public class RepairStationComponent : ShovelableComponent
{
    private static readonly List<InteractableVehicle> WorkingVehicles = new List<InteractableVehicle>(12);
    public readonly Dictionary<uint, int> VehiclesRepairing = new Dictionary<uint, int>(3);
    private int _counter;
    protected override void InitAwake()
    {
        if (Buildable.Type != BuildableType.RepairStation)
        {
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] RepairStationComponent not added to a repair station: {Buildable}.");
            goto destroy;
        }
        if (ActiveStructure == null)
        {
            L.LogWarning($"[FOBS] [{FOB?.Name ?? "FLOATING"}] RepairStationComponent not added to a barricade or structure: {Buildable}.");
            goto destroy;
        }
        
        return;
        destroy:
        Destroy(this);
    }

    protected override void InitStart()
    {
        StartCoroutine(RepairStationLoop());
    }
    private IEnumerator<WaitForSeconds> RepairStationLoop()
    {
        const int tickCountPerBuild = 9;
        const float tickSpeed = 1.5f;

        while (true)
        {
            if (Data.Gamemode is { State: Gamemodes.State.Staging or Gamemodes.State.Active })
            {
#if DEBUG
                IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                VehicleManager.getVehiclesInRadius(Position, 25f * 25f, WorkingVehicles);
                try
                {
                    for (int i = 0; i < WorkingVehicles.Count; i++)
                    {
                        InteractableVehicle vehicle = WorkingVehicles[i];
                        if (vehicle.lockedGroup.m_SteamID.GetTeam() != Team)
                            continue;

                        if (vehicle.asset.engine is not EEngine.PLANE and not EEngine.HELICOPTER &&
                            (Position - vehicle.transform.position).sqrMagnitude > 12f * 12f)
                            continue;

                        if (vehicle.health >= vehicle.asset.health && vehicle.fuel >= vehicle.asset.fuel)
                        {
                            if (VehiclesRepairing.ContainsKey(vehicle.instanceID))
                                VehiclesRepairing.Remove(vehicle.instanceID);
                        }
                        else
                        {
                            if (VehiclesRepairing.TryGetValue(vehicle.instanceID, out int ticks))
                            {
                                if (ticks > 0)
                                {
                                    if (vehicle.health < vehicle.asset.health)
                                    {
                                        TickRepair(vehicle);
                                        --ticks;
                                    }
                                    else if (_counter % 3 == 0 && !vehicle.isEngineOn)
                                    {
                                        TickRefuel(vehicle);
                                        --ticks;
                                    }
                                }
                                if (ticks <= 0)
                                    VehiclesRepairing.Remove(vehicle.instanceID);
                                else
                                    VehiclesRepairing[vehicle.instanceID] = ticks;
                            }
                            else
                            {
                                bool inMain = TeamManager.IsInMain(Team, Position);
                                FOB? owningFob = inMain ? null : FOB;
                                if (inMain || (owningFob != null && owningFob.BuildSupply > 0))
                                {
                                    VehiclesRepairing.Add(vehicle.instanceID, tickCountPerBuild);
                                    TickRepair(vehicle);

                                    if (owningFob != null)
                                    {
                                        owningFob.ModifyBuild(-1);

                                        UCPlayer? stationPlacer = UCPlayer.FromID(Owner);
                                        if (stationPlacer != null)
                                        {
                                            if (stationPlacer.CSteamID != vehicle.lockedOwner)
                                                Points.AwardXP(stationPlacer, XPReward.RepairVehicle);

                                            if (stationPlacer.Steam64 != owningFob.Owner)
                                                Points.TryAwardFOBCreatorXP(owningFob, XPReward.RepairVehicle, 0.5f);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    _counter++;
                }
                finally
                {
                    WorkingVehicles.Clear();
                }
#if DEBUG
                profiler.Dispose();
#endif
            }
            yield return new WaitForSeconds(tickSpeed);
        }
    }
    public void TickRepair(InteractableVehicle vehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (vehicle.health >= vehicle.asset.health)
            return;

        const ushort amount = 25;

        ushort newHealth = (ushort)Math.Min(vehicle.health + amount, ushort.MaxValue);
        if (vehicle.health + amount >= vehicle.asset.health)
        {
            newHealth = vehicle.asset.health;
            if (vehicle.transform.TryGetComponent(out VehicleComponent c))
            {
                c.DamageTable.Clear();
            }
        }

        VehicleManager.sendVehicleHealth(vehicle, newHealth);
        if (Gamemode.Config.EffectRepair.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicle.transform.position);
        vehicle.updateVehicle();
    }
    public void TickRefuel(InteractableVehicle vehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (vehicle.fuel >= vehicle.asset.fuel)
            return;

        const ushort amount = 180;

        vehicle.askFillFuel(amount);

        if (Gamemode.Config.EffectRefuel.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.SMALL, vehicle.transform.position);
        vehicle.updateVehicle();
    }
}

public interface IShovelable
{
    TickResponsibilityCollection Builders { get; }
    bool Shovel(UCPlayer shoveler);
    void QuickShovel(UCPlayer shoveler);
}

public interface IFOBItem
{
    FOB? FOB { get; set; }
    BuildableType Type { get; }
    BuildableData? Buildable { get; }
    ulong Team { get; }
    ulong Owner { get; }
    JsonAssetReference<EffectAsset>? Icon { get; }
    Vector3 Position { get; }
}

public enum FobRadius : byte
{
    Short,
    Full,
    FullBunkerDependant,
    FobPlacement,
    EnemyBunkerClaim
}
