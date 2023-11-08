﻿using SDG.Unturned;
using System;
using System.Globalization;
using Uncreated.Warfare.API.Assets;

namespace Uncreated.Warfare.Models.Assets;
public readonly struct UnturnedAssetReference
{
    public Guid Guid { get; }
    public ushort Id { get; }
    public UnturnedAssetReference(Guid guid)
    {
        Guid = guid;
        Id = 0;
    }
    public UnturnedAssetReference(ushort id)
    {
        Id = id;
        Guid = Guid.Empty;
    }
    public UnturnedAssetReference()
    {
        Id = 0;
        Guid = Guid.Empty;
    }

    public bool TryGetAsset<TAsset>(out TAsset asset) where TAsset : Asset
    {
        asset = GetAsset<TAsset>()!;
        return asset != null;
    }
    public TAsset? GetAsset<TAsset>() where TAsset : Asset
    {
        if (Id != 0)
        {
            EAssetType type = AssetHelper.GetAssetCategory<TAsset>();
            if (type == EAssetType.NONE)
                return null;

            return SDG.Unturned.Assets.find(type, Id) as TAsset;
        }

        return SDG.Unturned.Assets.find<TAsset>(Guid);
    }

    public static bool operator !=(UnturnedAssetReference left, UnturnedAssetReference right) => !(left == right);
    public static bool operator ==(UnturnedAssetReference left, UnturnedAssetReference right)
    {
        return left.Guid == right.Guid && left.Id == right.Id;
    }

    public bool Equals(UnturnedAssetReference other) => other.Guid == Guid && other.Id == Id;
    public bool Equals(Guid guid) => Guid != Guid.Empty && guid == Guid;
    public bool Equals(ushort id) => Id != 0 && id == Id;
    public override bool Equals(object obj) => obj switch
    {
        Guid g => g == Guid,
        ushort i => i == Id,
        UnturnedAssetReference r => r.Guid == Guid && r.Id == Id,
        _ => false
    };

    public override int GetHashCode() => Id != 0 ? Id.GetHashCode() : Guid.GetHashCode();

    public static UnturnedAssetReference Parse(string text)
    {
        if (TryParse(text, out UnturnedAssetReference result))
            return result;

        throw new FormatException("Failed to parse UnturnedAssetReference. Expected a UInt16 ID or Guid.");
    }
    public static bool TryParse(string text, out UnturnedAssetReference result)
    {
        int index = text.IndexOf('\0');
        if (index != -1)
            text = text.Substring(0, index);
        if (ushort.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out ushort id))
        {
            result = new UnturnedAssetReference(id);
            return true;
        }

        if (Guid.TryParse(text, out Guid guid))
        {
            result = new UnturnedAssetReference(guid);
            return true;
        }

        result = default;
        return false;
    }
    public override string ToString()
    {
        return Id != 0 ? Id.ToString(CultureInfo.InvariantCulture) : Guid.ToString("N", CultureInfo.InvariantCulture);
    }
}
