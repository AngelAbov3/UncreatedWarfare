﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Networking;
using ItemData = Uncreated.Framework.Assets.ItemData;

namespace Uncreated.Warfare;

public static class UCAssetManager
{
    public static VehicleAsset FindVehicleAsset(ushort vehicleID) => Assets.find(EAssetType.VEHICLE).Cast<VehicleAsset>().Where(k => k?.id == vehicleID).FirstOrDefault();
    public static VehicleAsset FindVehicleAsset(string vehicleName)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<VehicleAsset> assets = Assets.find(EAssetType.VEHICLE).Cast<VehicleAsset>()
            .Where(k => k?.name != null && k.vehicleName != null).OrderBy(k => k.vehicleName.Length).ToList();

        VehicleAsset asset = assets.FirstOrDefault(k =>
            vehicleName.Equals(k.id.ToString(Data.Locale), StringComparison.OrdinalIgnoreCase) ||
            vehicleName.Split(' ').All(l => k.vehicleName.ToLower().Contains(l)) ||
            vehicleName.Split(' ').All(l => k.name.ToLower().Contains(l))
            );

        return asset;
    }
    public static ItemAsset? FindItemAsset(string itemName, out int numberOfSimilarNames, bool additionalCheckWithoutNonAlphanumericCharacters = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        itemName = itemName.ToLower();

        numberOfSimilarNames = 0;

        List<ItemAsset> assets = Assets.find(EAssetType.ITEM).Cast<ItemAsset>()
            .Where(k => k?.name != null && k.itemName != null).OrderBy(k => k.itemName.Length).ToList();

        IEnumerable<ItemAsset> selection = assets.Where(k =>
            itemName.Equals(k.id.ToString(Data.Locale), StringComparison.OrdinalIgnoreCase) ||
            itemName.Split(' ').All(l => k.itemName.ToLower().Contains(l)) ||
            itemName.Split(' ').All(l => k.name.ToLower().Contains(l))
            );

        numberOfSimilarNames = selection.Count();

        ItemAsset? asset = selection.FirstOrDefault();

        if (asset == null && additionalCheckWithoutNonAlphanumericCharacters)
        {
            itemName = itemName.RemoveMany(false, '.', ',', '&', '-', '_');

            selection = assets.Where(k =>
            itemName.Equals(k.id.ToString(Data.Locale), StringComparison.OrdinalIgnoreCase) ||
            itemName.Split(' ').All(l => k.itemName.ToLower().RemoveMany(false, '.', ',', '&', '-', '_').Contains(l)) ||
            itemName.Split(' ').All(l => k.name.ToLower().RemoveMany(false, '.', ',', '&', '-', '_').Contains(l))
            );

            numberOfSimilarNames = selection.Count();
            asset = selection.FirstOrDefault();
        }

        numberOfSimilarNames--;

        return asset;
    }
    public static class NetCalls
    {
        public static readonly NetCall<ushort, EAssetType> RequestAssetName = new NetCall<ushort, EAssetType>(ReceiveRequestAssetName);
        public static readonly NetCall<Guid> RequestItemInfo = new NetCall<Guid>(ReceiveItemInfoRequest);
        public static readonly NetCall<Guid[]> RequestItemInfos = new NetCall<Guid[]>(ReceiveItemInfosRequest);
        public static readonly NetCall RequestAllItemInfos = new NetCall(ReceiveAllItemInfosRequest);

        public static readonly NetCall<ushort, EAssetType, string> SendAssetName = new NetCall<ushort, EAssetType, string>(1026);
        public static readonly NetCallRaw<ItemData?> SendItemInfo = new NetCallRaw<ItemData?>(1120, ItemData.Read, ItemData.Write);
        public static readonly NetCallRaw<ItemData?[]> SendItemInfos = new NetCallRaw<ItemData?[]>(1122, ItemData.ReadMany, ItemData.WriteMany);

        [NetCall(ENetCall.FROM_SERVER, 1025)]
        internal static void ReceiveRequestAssetName(MessageContext context, ushort id, EAssetType type)
        {
            Asset a = Assets.find(type, id);
            if (a is null)
            {
                context.Reply(SendAssetName, id, type, string.Empty);
                return;
            }
            context.Reply(SendAssetName, id, type, a.FriendlyName);
        }
        [NetCall(ENetCall.FROM_SERVER, 1119)]
        internal static void ReceiveItemInfoRequest(MessageContext context, Guid item)
        {
            if (Assets.find(item) is ItemAsset asset)
                context.Reply(SendItemInfo, ItemData.FromAsset(asset));
            else
                context.Reply(SendItemInfo, null);
        }
        [NetCall(ENetCall.FROM_SERVER, 1121)]
        internal static void ReceiveItemInfosRequest(MessageContext context, Guid[] items)
        {
            ItemData[] rtn = new ItemData[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                if (Assets.find(items[i]) is ItemAsset asset)
                    rtn[i] = ItemData.FromAsset(asset);
            }
            context.Reply(SendItemInfos, rtn);
        }
        [NetCall(ENetCall.FROM_SERVER, 1123)]
        internal static void ReceiveAllItemInfosRequest(MessageContext context)
        {
            Asset[] assets = Assets.find(EAssetType.ITEM);
            ItemData[] rtn = new ItemData[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                try
                {
                    if (assets[i] is ItemAsset asset) rtn[i] = ItemData.FromAsset(asset);
                }
                catch (Exception ex)
                {
                    L.LogError($"Error converting asset of type {assets[i].GetType().FullName} to ItemData ({assets[i].name}).");
                    L.LogError(ex);
                    rtn[i] = null!;
                }
            }
            context.Reply(SendItemInfos, rtn);
        }
    }
}
