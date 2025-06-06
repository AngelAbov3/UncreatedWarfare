using DanielWillett.ReflectionTools;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Events.Patches;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Players.Management;

/// <summary>
/// Tracks which players dropped specific items.
/// </summary>
public class DroppedItemTracker : IHostedService, IEventListener<PlayerLeft>
{
    private static bool _ignoreSpawningItemEvent;

    private readonly IPlayerService _playerService;
    private readonly EventDispatcher _eventDispatcher;
    private readonly WarfareModule _module;
    private readonly Dictionary<uint, ulong> _itemDroppers = new Dictionary<uint, ulong>(128);
    private readonly PlayerDictionary<List<uint>> _droppedItems = new PlayerDictionary<List<uint>>(Provider.maxPlayers);
    private readonly Dictionary<Item, ulong> _itemsPendingDrop = new Dictionary<Item, ulong>(4);
    private readonly StaticGetter<uint>? _getNextInstanceId = Accessor.GenerateStaticGetter<ItemManager, uint>("instanceCount");
    private readonly StaticSetter<uint>? _setNextInstanceId = Accessor.GenerateStaticSetter<ItemManager, uint>("instanceCount");
    private readonly ClientStaticMethod<byte, byte, ushort, byte, byte, byte[], Vector3, uint, bool>? SendItem
        = ReflectionUtility.FindRpc<ItemManager, ClientStaticMethod<byte, byte, ushort, byte, byte, byte[], Vector3, uint, bool>>("SendItem");

    public DroppedItemTracker(IPlayerService playerService, EventDispatcher eventDispatcher, WarfareModule module)
    {
        _playerService = playerService;
        _eventDispatcher = eventDispatcher;
        _module = module;
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        ItemManager.onServerSpawningItemDrop += OnServerSpawningItemDrop;
        ItemUtility.OnItemDestroyed += OnItemDestroyed;

        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        ItemManager.onServerSpawningItemDrop -= OnServerSpawningItemDrop;
        ItemUtility.OnItemDestroyed -= OnItemDestroyed;

        return UniTask.CompletedTask;
    }

    /// <summary>
    /// Destroy all items that were dropped by the given player.
    /// </summary>
    /// <returns>Number of items destroyed.</returns>
    public async UniTask<int> DestroyItemsDroppedByPlayerAsync(CSteamID player, bool despawned, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (!_droppedItems.TryGetValue(player, out List<uint>? instanceIds))
        {
            return 0;
        }

        uint[] underlying = instanceIds.GetUnderlyingArrayOrCopy();
        int ct = instanceIds.Count;

        int foundItems = 0;
        foreach (ItemInfo item in ItemUtility.EnumerateDroppedItems())
        {
            uint instanceId = item.Item.instanceID;

            bool found = false;
            for (int i = 0; i < ct; ++i)
            {
                if (underlying[i] != instanceId)
                    continue;

                found = true;
                break;
            }

            if (!found)
                continue;

            RegionCoord region = item.Coord;
            ItemUtility.RemoveDroppedItemUnsafe(region.x, region.y, item.Index, despawned, CSteamID.Nil, false, 0, 0, 0, 0);
            ++foundItems;
        }

        return foundItems;
    }

    [EventListener(MustRunInstantly = true)]
    void IEventListener<PlayerLeft>.HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        _droppedItems.Remove(e.Player);
    }

    /// <summary>
    /// Get the Steam64 ID of the owner of the given <paramref name="item"/>.
    /// </summary>
    [Pure]
    public CSteamID GetOwner(ItemData item)
    {
        return GetOwner(item.instanceID);
    }

    /// <summary>
    /// Get the Steam64 ID of the owner of the given item instance ID.
    /// </summary>
    [Pure]
    public CSteamID GetOwner(uint itemInstanceId)
    {
        return _itemDroppers.TryGetValue(itemInstanceId, out ulong player) ? Unsafe.As<ulong, CSteamID>(ref player) : CSteamID.Nil;
    }

    /// <summary>
    /// Enumerate all <see cref="ItemData"/> that a player dropped.
    /// </summary>
    /// <remarks>Note that this is a bit slower than <see cref="EnumerateDroppedItemInstanceIds"/> so that should be used when possible.</remarks>
    [Pure]
    public IEnumerable<ItemData> EnumerateDroppedItems(CSteamID player)
    {
        return _droppedItems.TryGetValue(player, out List<uint>? items)
            ? items
                .Select(x => ItemUtility.FindItem(x).Item)
                .Where(x => x != null)
            : Enumerable.Empty<ItemData>();
    }

    /// <summary>
    /// Enumerate the instance IDs of all items that a player dropped.
    /// </summary>
    [Pure]
    public IEnumerable<uint> EnumerateDroppedItemInstanceIds(CSteamID player)
    {
        return _droppedItems.TryGetValue(player, out List<uint>? items) ? items : Enumerable.Empty<uint>();
    }

    private void OnItemDestroyed(in ItemInfo itemInfo, bool despawned, bool pickedUp, CSteamID pickUpPlayer, Page pickupPage, byte pickupX, byte pickupY, byte pickupRot)
    {
        _itemsPendingDrop.Remove(itemInfo.Item.item);
        if (!_itemDroppers.Remove(itemInfo.Item.instanceID, out ulong dropper64))
            return;

        if (_droppedItems.TryGetValue(dropper64, out List<uint>? items))
            items.Remove(itemInfo.Item.item.id);

        ItemDestroyed args = new ItemDestroyed
        {
            DroppedItem = itemInfo.Item,
            Item = itemInfo.Item.item,
            Despawned = despawned,
            PickedUp = pickedUp,
            DropPlayer = _playerService.GetOnlinePlayerOrNull(dropper64),
            DropPlayerId = Unsafe.As<ulong, CSteamID>(ref dropper64),
            PickUpPlayer = pickedUp ? _playerService.GetOnlinePlayerOrNull(pickUpPlayer) : null,
            PickUpPlayerId = pickedUp ? pickUpPlayer : CSteamID.Nil,
            PickUpPage = pickedUp ? pickupPage : 0,
            PickUpX = pickedUp ? pickupX : (byte)0,
            PickUpY = pickedUp ? pickupY : (byte)0,
            PickUpRotation = pickedUp ? pickupRot : (byte)0
        };

        _ = _eventDispatcher.DispatchEventAsync(args, CancellationToken.None);
    }

    private void OnServerSpawningItemDrop(Item item, ref Vector3 location, ref bool shouldallow)
    {
        if (_ignoreSpawningItemEvent)
            return;

        if (!shouldallow)
        {
            _itemsPendingDrop.Remove(item);
            return;
        }

        uint instanceId = _getNextInstanceId == null ? uint.MaxValue : _getNextInstanceId() + 1;
        _itemsPendingDrop.Remove(item, out ulong steam64Num);

        CSteamID steam64 = Unsafe.As<ulong, CSteamID>(ref steam64Num);

        ItemSpawning args = new ItemSpawning
        {
            InstanceId = instanceId,
            Position = location,
            Item = item,
            PlayerDroppedId = steam64,
            PlayerDropped = _playerService.GetOnlinePlayerOrNull(steam64),
            IsDroppedByPlayer = PlayerInventoryReceiveDropItem.LastIsDropped,
            IsWideSpread = PlayerInventoryReceiveDropItem.LastWideSpread,
            PlayDropEffect = PlayerInventoryReceiveDropItem.LastPlayEffect
        };

        CombinedTokenSources sources = default;
        CancellationToken token = _module.UnloadToken;
        if (args.PlayerDropped != null)
        {
            sources = token.CombineTokensIfNeeded(args.PlayerDropped.DisconnectToken);
        }

        try
        {
            EventContinuations.Dispatch(args, _eventDispatcher, token, out shouldallow, continuation: args =>
            {
                // ReSharper disable once AccessToDisposedClosure
                sources.Dispose();

                if (args.PlayerDropped is { IsOnline: false } || _getNextInstanceId == null || ItemManager.regions == null || !Regions.checkSafe(args.Position) || item.GetAsset() is not { isPro: false })
                    return;

                uint instanceId = _getNextInstanceId() + 1;
                SavePlayerInstigator(args.PlayerDroppedId, instanceId);

                _ignoreSpawningItemEvent = true;
                try
                {
                    ItemManager.dropItem(args.Item, args.Position, args.PlayDropEffect, args.IsDroppedByPlayer, args.IsWideSpread);
                }
                finally
                {
                    _ignoreSpawningItemEvent = false;
                }
            }, args => args.PlayerDroppedId.IsIndividual());
        }
        finally
        {
            if (shouldallow || args.IsActionCancelled)
            {
                sources.Dispose();
            }
        }

        if (!shouldallow)
            return;

        SavePlayerInstigator(steam64, instanceId);
    }

    private void SavePlayerInstigator(CSteamID steam64, uint instanceId)
    {
        if (steam64.GetEAccountType() != EAccountType.k_EAccountTypeIndividual || instanceId == uint.MaxValue)
            return;

        if (_droppedItems.TryGetValue(steam64, out List<uint>? instanceids))
        {
            instanceids.Add(instanceId);
        }
        else
        {
            _droppedItems.Add(steam64, [instanceId]);
        }

        _itemDroppers[instanceId] = steam64.m_SteamID;
    }

    /// <summary>
    /// Adds an instigator to an item that's about to be dropped.
    /// </summary>
    internal void SetNextDroppedItemInstigator(Item item, ulong steam64)
    {
        _itemsPendingDrop[item] = steam64;
    }

    // invoked by PlayerEventDispatcher
    internal void InvokeDropItemRequested(PlayerInventory inv, Item item, ref bool shouldAllow)
    {
        WarfarePlayer player = _playerService.GetOnlinePlayer(inv);

        if (item.GetAsset() is not { isPro: false } asset)
        {
            shouldAllow = false;
            return;
        }

        Vector3 point = inv.transform.position + inv.transform.forward * 0.5f;

        ItemJar? foundJar = null;
        Page foundPage = (Page)byte.MaxValue;
        byte foundIndex = byte.MaxValue;
        for (int page = 0; page < PlayerInventory.AREA; ++page)
        {
            Items pg = inv.items[page];
            int ct = pg.getItemCount();
            for (int i = 0; i < ct; ++i)
            {
                ItemJar jar = pg.getItem((byte)i);
                if (!ReferenceEquals(jar.item, item))
                    continue;

                foundJar = jar;
                foundPage = (Page)page;
                foundIndex = (byte)i;
                break;
            }

            if (foundJar != null)
                break;
        }

        DropItemRequested args = new DropItemRequested
        {
            Player = player,
            Item = item,
            Asset = asset,
            Page = foundPage,
            Index = foundIndex,
            X = foundJar?.x ?? 0,
            Y = foundJar?.y ?? 0,
            Rotation = foundJar?.rot ?? 0,
            Position = point
        };

        EventContinuations.Dispatch(args, _eventDispatcher, player.DisconnectToken, out shouldAllow, continuation: async (args, token) =>
        {
            if (!args.Player.IsOnline)
                return;

            Vector3 point = args.Position;

            bool isCustomDropping = (args.Exact || !args.Grounded) && _getNextInstanceId != null && _setNextInstanceId != null && SendItem != null;

            if (isCustomDropping && !args.Exact)
            {
                if (args.WideSpread)
                {
                    point.x += UnityEngine.Random.Range(-0.75f, 0.75f);
                    point.z += UnityEngine.Random.Range(-0.75f, 0.75f);
                }
                else
                {
                    point.x += UnityEngine.Random.Range(-0.125f, 0.125f);
                    point.z += UnityEngine.Random.Range(-0.125f, 0.125f);
                }
            }

            Vector3 serversidePoint = point;
            if (isCustomDropping)
            {
                if (Physics.SphereCast(new Ray(point + Vector3.up, Vector3.down), 0.1f, out RaycastHit hitInfo, 2048f, RayMasks.BLOCK_ITEM))
                {
                    point.y = hitInfo.point.y;
                }

                if (args.Grounded)
                    serversidePoint = point;
            }

            byte x = 0, y = 0;
            if (isCustomDropping && !Regions.tryGetCoordinate(point, out x, out y))
            {
                return;
            }

            ItemJar? itemJar = args.Player.UnturnedPlayer.inventory.GetItemAt(args.Page, args.X, args.Y, out byte index);
            if (itemJar?.item != args.Item)
                return;


            ItemInfo droppedItem;
            if (!isCustomDropping)
            {
                _itemsPendingDrop[args.Item] = args.Player.Steam64.m_SteamID;

                ItemManager.dropItem(args.Item, point, true, true, args.WideSpread);
                droppedItem = ItemUtility.FindItem(args.Item, args.Position);
            }
            else
            {
                _ignoreSpawningItemEvent = true;
                try
                {
                    bool shouldAllow = true;
                    ItemManager.onServerSpawningItemDrop?.Invoke(args.Item, ref point, ref shouldAllow);
                    if (!shouldAllow)
                        return;
                }
                finally
                {
                    _ignoreSpawningItemEvent = false;
                }

                uint instId = _getNextInstanceId!();
                ++instId;
                _setNextInstanceId!(instId);
                ItemSpawning spawningArgs = new ItemSpawning
                {
                    Position = point,
                    InstanceId = instId,
                    IsDroppedByPlayer = true,
                    PlayerDropped = args.Player,
                    PlayerDroppedId = args.Player.Steam64,
                    IsWideSpread = args is { WideSpread: true, Exact: false },
                    PlayDropEffect = true,
                    Item = args.Item
                };

                if (!await _eventDispatcher.DispatchEventAsync(spawningArgs, token))
                {
                    return;
                }

                if (!args.Grounded && serversidePoint.y - point.y > 17.5f) // max range is 20m, use a lower number to be safe
                {
                    serversidePoint.y = point.y + 17.5f;
                }

                ItemData itemData = new ItemData(args.Item, instId, serversidePoint, true);

                SavePlayerInstigator(args.Player.Steam64, instId);
                ItemRegion region = ItemManager.regions[x, y];
                int droppedItemIndex = region.items.Count;
                region.items.Add(itemData);
                droppedItem = new ItemInfo(itemData, droppedItemIndex, new RegionCoord(x, y));

                Item item = args.Item;
                SendItem!.Invoke(
                    ENetReliability.Reliable,
                    Regions.GatherClientConnections(x, y, ItemManager.ITEM_REGIONS),
                    x,
                    y,
                    item.id,
                    item.amount,
                    item.quality,
                    item.state,
                    serversidePoint,
                    instId,
                    true
                );
            }

            args.Player.UnturnedPlayer.inventory.removeItem((byte)args.Page, index);
            if ((int)args.Page < PlayerInventory.SLOTS)
            {
                args.Player.UnturnedPlayer.equipment.sendSlot((byte)args.Page);
            }

            ItemDropped dropArgs = new ItemDropped
            {
                Player = args.Player,
                Region = droppedItem.HasValue ? droppedItem.GetRegion() : null,
                RegionPosition = droppedItem.Coord,
                Index = (ushort)droppedItem.Index,
                Item = args.Item,
                Asset = args.Item?.GetAsset(),
                DroppedItem = droppedItem.Item,
                OldPage = args.Page,
                OldX = args.X,
                OldY = args.Y,
                OldRotation = args.Rotation,
                LandingPoint = point,
                DropPoint = args.Position
            };

            _ = _eventDispatcher.DispatchEventAsync(dropArgs, CancellationToken.None);

        }, _ => true);
    }
}