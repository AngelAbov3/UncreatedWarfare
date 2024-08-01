﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Zones;
using Random = UnityEngine.Random;
using XPReward = Uncreated.Warfare.Levels.XPReward;

namespace Uncreated.Warfare.Components;

public class VehicleComponent : MonoBehaviour
{
    private const int StartingFlaresAttackHeli = 30;
    private const int StartingFlaresTransportHeli = 50;
    private const int StartingFlaresJet = 30;
    public const int FlareBurstCount = 10;
    public const int FlareCooldown = 11;

    public static readonly VehicleHUD VehicleHUD = new VehicleHUD();

    private PlayerService _playerService = null!;

    private Zone? _noDropZone;
    private Zone? _noPickZone;
    private Zone? _safezoneZone;
    private Vector3 _lastPosInterval;
    private float _totalDistance;
    private float _lastCheck;
    private float _lastZoneCheck;
    private float _timeLastFlare;
    private float _timeLastFlareDrop;
    private int _flareBurst;
    public ulong LastDriver;
    public float LastDriverTime;
    public float LastDriverDistance;
    public Guid LastItem;
    public bool LastItemIsVehicle;
    public InteractableVehicle Vehicle;
    public InteractableVehicle? LastDamagedFromVehicle;
    public EDamageOrigin LastDamageOrigin;
    public ulong LastInstigator;
    public CSteamID LastLocker;
    private bool _isResupplied;
    private Coroutine? _quotaLoop;
    private Coroutine? _autoSupplyLoop;
    private PlayerDictionary<DateTime> _timeEnteredTable;
    private PlayerDictionary<DateTime> _timeRewardedTable;
    public Dictionary<ulong, KeyValuePair<ushort, DateTime>> DamageTable;
    public ulong PreviousOwner;
    public float TotalDistanceTravelled => _totalDistance;
    public float TimeRequested { get; set; }
    public Stack<ulong> OwnerHistory { get; } = new Stack<ulong>(2);
    public ulong Team => Vehicle.lockedGroup.m_SteamID.GetTeam();
    public PlayerDictionary<Vector3> TransportTable { get; private set; }
    public PlayerDictionary<double> UsageTable { get; private set; }
    public float Quota { get; set; }
    public float RequiredQuota { get; set; }
    public Coroutine? ForceSupplyLoop { get; private set; }
    public WarfareVehicleInfo? VehicleData { get; private set; }

    /// <summary>
    /// The spawn the vehicle was created at, if any.
    /// </summary>
    // todo fill this value
    public VehicleSpawnInfo? Spawn { get; private set; }
    public Zone? SafezoneZone
    {
        get
        {
            if (Data.Gamemode is null)
                return null;
            if (Time.time - _lastZoneCheck < Data.Gamemode.EventLoopSpeed)
                CheckZones();
            return _safezoneZone;
        }
    }
    public Zone? NoDropZone
    {
        get
        {
            if (Data.Gamemode is null)
                return null;
            if (Time.time - _lastZoneCheck < Data.Gamemode.EventLoopSpeed)
                CheckZones();
            return _noDropZone;
        }
    }
    public Zone? NoPickZone
    {
        get
        {
            if (Data.Gamemode is null)
                return null;
            if (Time.time - _lastZoneCheck < Data.Gamemode.EventLoopSpeed)
                CheckZones();
            return _noPickZone;
        }
    }

    public bool IsGroundVehicle => VehicleData != null && VehicleData.Type.IsGroundVehicle();
    public bool IsArmor => VehicleData != null && VehicleData.Type.IsArmor();
    public bool IsLogistics => VehicleData != null && VehicleData.Type.IsLogistics();
    public bool IsAircraft => VehicleData != null && VehicleData.Type.IsAircraft();
    public bool IsAssaultAircraft => VehicleData != null && VehicleData.Type.IsAssaultAircraft();
    public bool IsEmplacement => VehicleData != null && VehicleData.Type.IsEmplacement();
    public bool IsInVehiclebay => VehicleData != null;
    public bool CanTransport => VehicleData != null && VehicleData.CanTransport(Vehicle);

    public void Initialize(InteractableVehicle vehicle, IServiceProvider serviceProvider)
    {
        Vehicle = vehicle;
        TransportTable = new PlayerDictionary<Vector3>(8);
        UsageTable = new PlayerDictionary<double>(8);
        _timeEnteredTable = new PlayerDictionary<DateTime>();
        _timeRewardedTable = new PlayerDictionary<DateTime>();
        DamageTable = new Dictionary<ulong, KeyValuePair<ushort, DateTime>>();
        _isResupplied = true;
        if (vehicle.lockedOwner.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
            OwnerHistory.Push(vehicle.lockedOwner.m_SteamID);

        Quota = 0;
        RequiredQuota = -1;

        _playerService = serviceProvider.GetRequiredService<PlayerService>();

        VehicleInfoStore? vehicleInfoStore = serviceProvider.GetService<VehicleInfoStore>();
        if (vehicleInfoStore != null)
        {
            VehicleData = vehicleInfoStore.GetVehicleInfo(Vehicle.asset);
            if (VehicleData != null)
            {
                vehicle.transform.gameObject.AddComponent<SpottedComponent>().Initialize(VehicleData.Type, vehicle);
            }
        }
        _lastPosInterval = transform.position;

        foreach (var passenger in Vehicle.turrets)
        {
            if (VehicleBay.Config.GroundAAWeapons.ContainsId(passenger.turret.itemID))
                passenger.turretAim.gameObject.AddComponent<HeatSeekingController>().Initialize(700, 1500, Gamemode.Config.EffectLockOn1, 0.7f, 14.6f);
            if (VehicleBay.Config.AirAAWeapons.ContainsId(passenger.turret.itemID))
                passenger.turretAim.gameObject.AddComponent<HeatSeekingController>().Initialize(600, Gamemode.Config.EffectLockOn2, 1, 11);
        }

        ReloadCountermeasures();
    }
    public static void TryAddOwnerToHistory(InteractableVehicle vehicle, ulong steam64)
    {
        if (vehicle.TryGetComponent(out VehicleComponent comp))
        {
            if (comp.OwnerHistory.Count > 0)
                comp.PreviousOwner = comp.OwnerHistory.Peek();
            else
                comp.TimeRequested = Time.realtimeSinceStartup;
            comp.OwnerHistory.Push(steam64);
        }
    }
    public bool IsType(VehicleType type) => VehicleData != null && VehicleData.Type == type;
    private void CheckZones()
    {
        // todo not sure if we're keeping this
        _lastZoneCheck = Time.time;
        if (Data.Singletons.TryGetSingleton(out ZoneList zoneList))
        {
            zoneList.WriteWait();
            try
            {
                _safezoneZone = null;
                _noDropZone = null;
                _noPickZone = null;
                for (int i = 0; i < zoneList.Items.Count; ++i)
                {
                    Zone? zone = zoneList.Items[i]?.Item;
                    if (zone is null || zone.Data.Flags == ZoneFlags.None || !zone.IsInside(transform.position))
                        continue;
                    if ((zone.Data.Flags & ZoneFlags.Safezone) != 0)
                    {
                        if (_safezoneZone is null || _safezoneZone.BoundsArea > zone.BoundsArea)
                        {
                            _safezoneZone = zone;
                            L.LogDebug($"Vehicle {Vehicle.asset.name} in sz {_safezoneZone.Name}.");
                        }
                    }
                    if ((zone.Data.Flags & ZoneFlags.NoDropItems) != 0)
                    {
                        if (_noDropZone is null || _noDropZone.BoundsArea > zone.BoundsArea)
                        {
                            _noDropZone = zone;
                            L.LogDebug($"Vehicle {Vehicle.asset.name} in ndz {_noDropZone.Name}.");
                        }
                    }
                    if ((zone.Data.Flags & ZoneFlags.NoPickItems) != 0)
                    {
                        if (_noPickZone is null || _noPickZone.BoundsArea > zone.BoundsArea)
                        {
                            _noPickZone = zone;
                            L.LogDebug($"Vehicle {Vehicle.asset.name} in npz {_noPickZone.Name}.");
                        }
                    }
                }
            }
            finally
            {
                zoneList.WriteRelease();
            }
        }
    }

    private void ShowHUD(WarfarePlayer player, byte seat)
    {
        if (VehicleData == null)
            return;

        if (!IsAircraft)
            return;

        if (!VehicleData.IsCrewSeat(seat))
            return;

        VehicleHUD.SendToPlayer(player.Connection);

        VehicleHUD.MissileWarning.SetVisibility(player.Connection, false);
        VehicleHUD.MissileWarningDriver.SetVisibility(player.Connection, false);
        VehicleHUD.FlareCount.SetVisibility(player.Connection, seat == 0);

        if (seat == 0)
            VehicleHUD.FlareCount.SetText(player.Connection, "FLARES: " + _totalFlaresLeft);
    }
    private void UpdateHUDFlares()
    {
        if (!IsAircraft)
            return;

        var driver = Vehicle.passengers[0].player;
        if (driver != null)
            VehicleHUD.FlareCount.SetText(driver.transportConnection, "FLARES: " + _totalFlaresLeft);
    }
    private void HideHUD(WarfarePlayer player)
    {
        VehicleHUD.ClearFromPlayer(player.Connection);
    }
    internal void OnPlayerEnteredVehicle(EnterVehicle e)
    {
        WarfarePlayer player = e.Player;

        byte toSeat = e.PassengerIndex;
        if (toSeat == 0)
        {
            LastDriver = player.Steam64.m_SteamID;
            LastDriverTime = Time.realtimeSinceStartup;
            _totalDistance = 0;
        }

        if (VehicleData.IsCrewSeat(toSeat))
        {
            TransportTable[player] = player.Position;
        }

        _timeEnteredTable[player] = DateTime.UtcNow;

        if (_quotaLoop is null)
        {
            RequiredQuota = VehicleData.TicketCost * 0.5f;
            _quotaLoop = StartCoroutine(QuotaLoop());
        }

        ShowHUD(player, toSeat);
    }
    public void OnPlayerExitedVehicle(ExitVehicle e)
    {
        if (IsInVehiclebay)
            EvaluateUsage(e.Player.UnturnedPlayer.channel.owner);

        HideHUD(e.Player);
        
        if (e.Player.KitClass == Class.Squadleader &&
            (VehicleData != null && VehicleData.Type.IsLogistics()) &&
            !F.IsInMain(e.Player.Position) &&
            Data.Singletons.GetSingleton<FOBManager>()?.FindNearestFOB<FOB>(e.Player.Position, e.Player.GetTeam()) == null
            )
        {
            Tips.TryGiveTip(e.Player, 300, T.TipPlaceRadio);
        }

        if (e.Vehicle.passengers.Length > 0 && e.Vehicle.passengers[0] == null || e.Vehicle.passengers[0].player == null ||
            e.Vehicle.passengers[0].player.player.channel.owner.playerID.steamID.m_SteamID == e.Player.Steam64.m_SteamID)
        {
            if (LastDriver == e.Player.Steam64.m_SteamID)
                LastDriverDistance = _totalDistance;
            return;
        }
        if (TransportTable.TryGetValue(e.Player, out Vector3 original))
        {
            float distance = (e.Player.Position - original).magnitude;
            if (distance >= 200 && e.Player.KitClass is not Class.Crewman and not Class.Pilot)
            {
                if (!(_timeRewardedTable.TryGetValue(e.Player, out DateTime time) && (DateTime.UtcNow - time).TotalSeconds < 60))
                {
                    int amount = (int)(Math.Floor(distance / 100) * 2) + 5;

                    Player player = e.Vehicle.passengers[0].player.player;
                    if (UCPlayer.FromPlayer(player) is { } pl)
                        Points.AwardXP(pl, XPReward.TransportingPlayer, T.XPToastTransportingPlayers, amount);

                    Quota += 0.5F;

                    _timeRewardedTable[e.Player] = DateTime.UtcNow;
                }
            }

            TransportTable.Remove(e.Player);
        }
    }
    public void OnPlayerSwapSeatRequested(VehicleSwapSeat e)
    {
        if (e.NewSeat == 0)
        {
            // new driver
            LastDriver = e.Player.Steam64.m_SteamID;
            LastDriverTime = Time.realtimeSinceStartup;
            _totalDistance = 0;
        }

        if (IsInVehiclebay)
        {
            EvaluateUsage(e.Player.SteamPlayer);

            if (VehicleData.IsCrewSeat(e.NewSeat))
                TransportTable.TryAdd(e.Player, e.Player.Position);
            else
                TransportTable.Remove(e.Player);
        }

        ShowHUD(e.Player, e.NewSeat);
    }
    public void EvaluateUsage(SteamPlayer player)
    {
        byte currentSeat = player.player.movement.getSeat();
        bool isCrewSeat = VehicleData.IsCrewSeat(currentSeat);

        if (currentSeat != 0 && !isCrewSeat)
            return;

        CSteamID s64 = player.playerID.steamID;

        if (!_timeEnteredTable.TryGetValue(s64, out DateTime start))
            return;

        double time = (DateTime.UtcNow - start).TotalSeconds;
        if (!UsageTable.ContainsPlayer(s64))
            UsageTable.Add(s64, time);
        else
            UsageTable[s64] += time;
    }
    private int _totalFlaresLeft;
    public void ReloadCountermeasures()
    {
        if (VehicleData != null)
        {
            _totalFlaresLeft = VehicleData.Type switch
            {
                VehicleType.AttackHeli => StartingFlaresAttackHeli,
                VehicleType.TransportAir => StartingFlaresTransportHeli,
                VehicleType.Jet => StartingFlaresJet,
                _ => _totalFlaresLeft
            };
        }

        UpdateHUDFlares();
    }
    public void ReceiveMissileWarning()
    {
        if (_warningRoutine is not null)
            StopCoroutine(_warningRoutine);

        _warningRoutine = StartCoroutine(WarningRoutine());
    }
    private Coroutine _warningRoutine;
    private IEnumerator<WaitForSeconds> WarningRoutine()
    {
        ToggleMissileWarning(true);
        yield return new WaitForSeconds(1);
        ToggleMissileWarning(false);
    }
    private void ToggleMissileWarning(bool enabled)
    {
        if (VehicleData is null)
            return;

        for (byte i = 0; i < Vehicle.passengers.Length; i++)
        {
            Passenger passenger = Vehicle.passengers[i];
            if (passenger?.player == null || !VehicleData.IsCrewSeat(i))
                continue;

            VehicleHUD.MissileWarning.SetVisibility(passenger.player.transportConnection, enabled);
            if (i == 0)
                VehicleHUD.MissileWarningDriver.SetVisibility(passenger.player.transportConnection, enabled);
        }
    }
    public void StartForceLoadSupplies(UCPlayer caller, SupplyType type, int amount)
    {
        ForceSupplyLoop = StartCoroutine(ForceSupplyLoopCoroutine(caller, type, amount));
    }
    public bool TryStartAutoLoadSupplies()
    {
        if (_isResupplied || _autoSupplyLoop != null || VehicleData?.Trunk == null || Vehicle.trunkItems == null)
            return false;

        _autoSupplyLoop = StartCoroutine(AutoSupplyLoop());
        return true;
    }
    private IEnumerator<WaitForSeconds> ForceSupplyLoopCoroutine(UCPlayer caller, SupplyType type, int amount)
    {
        ItemAsset? supplyAsset;

        if (type is SupplyType.Build)
            TeamManager.GetFaction(Team).Build.TryGetAsset(out supplyAsset);
        else if (type is SupplyType.Ammo)
            TeamManager.GetFaction(Team).Ammo.TryGetAsset(out supplyAsset);
        else
        {
            caller.SendChat(T.UnknownError);
            yield break;
        }
        if (supplyAsset == null)
        {
            caller.SendChat(T.UnknownError);
            yield break;
        }
        int addedNewCount = 0;
        int loaderBreak = 0;
        for (int i = 0; i < amount; i++)
        {
            if (!TeamManager.IsInAnyMain(Vehicle.transform.position))
                break;
            if (!Vehicle.trunkItems.tryAddItem(new Item(supplyAsset.id, true)))
                break;

            addedNewCount++;
            loaderBreak++;
            if (loaderBreak >= 3)
            {
                loaderBreak = 0;
                F.TryTriggerSupplyEffect(type, Vehicle.transform.position);
                yield return new WaitForSeconds(1);

                while (!(Vehicle.ReplicatedSpeed >= -1 && Vehicle.ReplicatedSpeed <= 1))
                    yield return new WaitForSeconds(1);
            }
        }

        caller.SendChat(type is SupplyType.Build ? T.LoadCompleteBuild : T.LoadCompleteAmmo, addedNewCount);

        ForceSupplyLoop = null;
    }
    private IEnumerator<WaitForSeconds> AutoSupplyLoop()
    {
        TeamManager.GetFaction(Team).Build.TryGetAsset(out ItemAsset? build);
        TeamManager.GetFaction(Team).Ammo.TryGetAsset(out ItemAsset? ammo);

        WarfarePlayer? driver = _playerService.GetOnlinePlayerOrNull(LastDriver);

        int loaderCount = 0;

        bool shouldMessagePlayer = false;

        if (VehicleData == null)
            yield break;

        if (VehicleData.Trunk != null)
        {
            IReadOnlyList<WarfareVehicleInfo.TrunkItem> trunk = VehicleData.Trunk;
            for (int i = 0; i < trunk.Count; i++)
            {
                ItemAsset? asset;
                WarfareVehicleInfo.TrunkItem trunkItem = trunk[i];
                if (build != null && trunkItem.Item.Guid == build.GUID)
                {
                    asset = build;
                }
                else if (ammo != null && trunkItem.Item.Guid == ammo.GUID)
                {
                    asset = ammo;
                }
                else
                {
                    asset = trunkItem.Item.GetAsset();
                }

                if (asset == null
                    || !Vehicle.trunkItems.checkSpaceEmpty(trunkItem.X, trunkItem.Y, asset.size_x, asset.size_y, trunkItem.Rotation)
                    || !TeamManager.IsInMain(Vehicle.lockedGroup.m_SteamID.GetTeam(), Vehicle.transform.position))
                {
                    continue;
                }

                byte[] state;
                if (trunkItem.State is { Length: > 0 })
                {
                    state = new byte[trunkItem.State.Length];
                    Buffer.BlockCopy(trunkItem.State, 0, state, 0, state.Length);
                }
                else
                {
                    state = Array.Empty<byte>();
                }

                Item item = new Item(asset.id, asset.amount, 100, state);
                Vehicle.trunkItems.addItem(trunkItem.X, trunkItem.Y, trunkItem.Rotation, item);
                loaderCount++;

                if (loaderCount < 3)
                    continue;

                loaderCount = 0;
                F.TryTriggerSupplyEffect(asset == build ? SupplyType.Build : SupplyType.Ammo, Vehicle.transform.position);
                shouldMessagePlayer = true;

                yield return new WaitForSeconds(1);
                while (Vehicle.ReplicatedSpeed is < -1 or > 1)
                    yield return new WaitForSeconds(1);
            }
        }

        _isResupplied = true;
        _autoSupplyLoop = null;

        if (shouldMessagePlayer && driver is { IsOnline: true } && F.IsInMain(driver.Position) && VehicleData != null)
        {
            Tips.TryGiveTip(driver, 120, T.TipLogisticsVehicleResupplied, VehicleData.Type);
        }
    }
    private IEnumerator<WaitForSeconds> QuotaLoop()
    {
        int tick = 0;

        while (true)
        {
            yield return new WaitForSeconds(3);
            if (F.IsInMain(Vehicle.transform.position))
            {
                //var ammoCrate = BarricadeUtility.CountBarricadesInRange(30, Vehicle.transform.position, Gamemode.Config.Barricades.AmmoCrateGUID, true).FirstOrDefault();
                if (Vehicle.ReplicatedSpeed is >= -1 and <= 1)
                {
                    TryStartAutoLoadSupplies();
                }
            }
            else if (_isResupplied)
            {
                _isResupplied = false;
            }

            tick++;
            if (tick >= 20)
            {
                Quota += 0.5F;
                tick = 0;
            }
        }
    }

    [UsedImplicitly]
    private void Update()
    {
        float time = Time.time;
        if (time - _lastCheck > 3f)
        {
            _lastCheck = time;
            if (Vehicle.passengers[0]?.player == null)
            {
                return;
            }

            Vector3 pos = transform.position;
            if (pos == _lastPosInterval)
            {
                return;
            }

            float old = _totalDistance;
            _totalDistance += (_lastPosInterval - pos).magnitude;
            QuestManager.OnDistanceUpdated(LastDriver, _totalDistance, _totalDistance - old, this);
            _lastPosInterval = pos;
        }
    }

    public void TryDropFlares()
    {
        if (Time.time - _timeLastFlareDrop < FlareCooldown || _totalFlaresLeft < 0)
            return;

        _flareBurst = FlareBurstCount;
        _timeLastFlareDrop = Time.time;

        if (!VehicleBay.Config.CountermeasureEffectID.TryGetAsset(out EffectAsset? countermeasureEffect))
            return;

        for (byte seat = 0; seat < Vehicle.passengers.Length; seat++)
        {
            if (Vehicle.passengers[seat].player != null && VehicleData.IsCrewSeat(seat))
                EffectManager.sendUIEffect(countermeasureEffect.id, -1, Vehicle.passengers[seat].player.transportConnection, true);
        }
    }

    [UsedImplicitly]
    private void FixedUpdate()
    {
        if (_totalFlaresLeft <= 0 || _flareBurst <= 0 || !(Time.time - _timeLastFlare > 0.2f))
            return;

        _timeLastFlare = Time.time;

        if (!VehicleBay.Config.CountermeasureGUID.TryGetAsset(out VehicleAsset? countermeasureAsset))
            return;

        InteractableVehicle? countermeasureVehicle = VehicleManager.spawnVehicleV2(countermeasureAsset, Vehicle.transform.TransformPoint(0, -4, 0), Vehicle.transform.rotation);

        float sideforce = Random.Range(20f, 30f);

        if (_flareBurst % 2 == 0) sideforce = -sideforce;

        Rigidbody? rigidbody = countermeasureVehicle.transform.GetComponent<Rigidbody>();
        Vector3 velocity = Vehicle.transform.forward * Vehicle.ReplicatedSpeed * 0.9f - Vehicle.transform.up * 15 + Vehicle.transform.right * sideforce;
        rigidbody.velocity = velocity;

        var countermeasure = countermeasureVehicle.gameObject.AddComponent<Countermeasure>();

        Countermeasure.ActiveCountermeasures.Add(countermeasure);

        _totalFlaresLeft--;
        _flareBurst--;
        UpdateHUDFlares();
    }
}

public enum SupplyType
{
    Build,
    Ammo
}