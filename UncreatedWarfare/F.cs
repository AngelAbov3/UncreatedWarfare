﻿using DanielWillett.ReflectionTools;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Players.Management.Legacy;
using Uncreated.Warfare.Steam.Models;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Zones;
using Types = SDG.Unturned.Types;

namespace Uncreated.Warfare;

public static class F
{
    public static bool HasPlayer(this List<UCPlayer> list, UCPlayer player)
    {
        IEqualityComparer<UCPlayer> c = UCPlayer.Comparer;
        for (int i = 0; i < list.Count; ++i)
        {
            if (c.Equals(list[i], player))
                return true;
        }
        return false;
    }
    public static string FilterRarityToHex(string color)
    {
        if (color == null)
            return UCWarfare.GetColorHex("default");
        string f1 = "color=" + color;
        string f2 = ItemTool.filterRarityRichText(f1);
        string rtn;
        if (f2.Equals(f1) || f2.Length <= 7)
            rtn = color;
        else
            rtn = f2.Substring(7); // 7 is "color=#" length
        return !int.TryParse(rtn, NumberStyles.HexNumber, Data.AdminLocale, out _) ? UCWarfare.GetColorHex("default") : rtn;
    }

    public static string RemoveMany(this string source, bool caseSensitive, params char[] replacables)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (replacables.Length == 0) return source;
        char[] chars = source.ToCharArray();
        char[] lowerchars = caseSensitive ? chars : source.ToLower().ToCharArray();
        char[] lowerrepls;
        if (!caseSensitive)
        {
            lowerrepls = new char[replacables.Length];
            for (int i = 0; i < replacables.Length; i++)
            {
                lowerrepls[i] = char.ToLower(replacables[i]);
            }
        }
        else lowerrepls = replacables;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < chars.Length; i++)
        {
            bool found = false;
            for (int c = 0; c < lowerrepls.Length; c++)
            {
                if (lowerrepls[c] == lowerchars[i])
                {
                    found = true;
                }
            }
            if (!found) sb.Append(chars[i]);
        }
        return sb.ToString();
    }
    public static bool ArrayContains(this byte[] array, byte value)
    {
        for (int i = 0; i < array.Length; ++i)
        {
            if (array[i] == value)
                return true;
        }

        return false;
    }

    public static void TryTriggerSupplyEffect(SupplyType type, Vector3 position)
    {
        if ((type is SupplyType.Build ? Gamemode.Config.EffectUnloadBuild : Gamemode.Config.EffectUnloadAmmo).TryGetAsset(out EffectAsset? effect))
            TriggerEffectReliable(effect, EffectManager.MEDIUM, position);
    }
    public static void TriggerEffectReliable(EffectAsset asset, ITransportConnection connection, Vector3 position)
    {
        GameThread.AssertCurrent();
        TriggerEffectParameters p = new TriggerEffectParameters(asset)
        {
            position = position,
            reliable = true
        };
        p.SetRelevantPlayer(connection);
        EffectManager.triggerEffect(p);
    }
    public static void TriggerEffectReliable(EffectAsset asset, float range, Vector3 position)
        => TriggerEffectReliable(asset, Provider.GatherRemoteClientConnectionsWithinSphere(position, range), position);
    public static void TriggerEffectReliable(EffectAsset asset, PooledTransportConnectionList connection, Vector3 position)
    {
        GameThread.AssertCurrent();
        TriggerEffectParameters p = new TriggerEffectParameters(asset)
        {
            position = position,
            reliable = true
        };
        p.SetRelevantTransportConnections(connection);
        EffectManager.triggerEffect(p);
    }
    public static void TriggerEffectUnreliable(EffectAsset asset, ITransportConnection connection, Vector3 position)
    {
        GameThread.AssertCurrent();
        TriggerEffectParameters p = new TriggerEffectParameters(asset)
        {
            position = position,
            reliable = false
        };
        p.SetRelevantPlayer(connection);
        EffectManager.triggerEffect(p);
    }
    public static void TriggerEffectUnreliable(EffectAsset asset, float range, Vector3 position)
        => TriggerEffectUnreliable(asset, Provider.GatherRemoteClientConnectionsWithinSphere(position, range), position);
    public static void TriggerEffectUnreliable(EffectAsset asset, PooledTransportConnectionList connection, Vector3 position)
    {
        GameThread.AssertCurrent();
        TriggerEffectParameters p = new TriggerEffectParameters(asset)
        {
            position = position,
            reliable = false
        };
        p.SetRelevantTransportConnections(connection);
        EffectManager.triggerEffect(p);
    }

    public static bool TryGetPlayerData(this Player player, out UCPlayerData component)
    {
        component = GetPlayerData(player, out bool success)!;
        return success;
    }
    public static bool TryGetPlayerData(this CSteamID player, out UCPlayerData component)
    {
        component = GetPlayerData(player, out bool success)!;
        return success;
    }

    public static UCPlayerData? GetPlayerData(this Player player, out bool success)
    {
        if (Data.PlaytimeComponents.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out UCPlayerData pt))
        {
            success = pt != null;
            return pt;
        }
        if (player == null || player.transform == null)
        {
            success = false;
            return null;
        }
        if (player.transform.TryGetComponent(out UCPlayerData playtimeObj))
        {
            success = true;
            return playtimeObj;
        }
        success = false;
        return null;
    }
    public static UCPlayerData? GetPlayerData(this CSteamID player, out bool success)
    {
        if (Data.PlaytimeComponents.TryGetValue(player.m_SteamID, out UCPlayerData pt))
        {
            success = pt != null;
            return pt;
        }
        else if (player == default || player == CSteamID.Nil)
        {
            success = false;
            return null;
        }
        else
        {
            Player p = PlayerTool.getPlayer(player);
            if (p == null)
            {
                success = false;
                return null;
            }
            if (p.transform.TryGetComponent(out UCPlayerData playtimeObj))
            {
                success = true;
                return playtimeObj;
            }
            else
            {
                success = false;
                return null;
            }
        }
    }

    public static ValueTask<PlayerNames> GetPlayerOriginalNamesAsync(ulong player, CancellationToken token = default)
    {
        UCPlayer? pl = UCPlayer.FromID(player);
        if (pl != null)
        {
            Data.ModerationSql.UpdateUsernames(player, pl.Name);
            return new ValueTask<PlayerNames>(pl.Name);
        }

        return new CSteamID(player).GetEAccountType() == EAccountType.k_EAccountTypeIndividual
            ? new ValueTask<PlayerNames>(Data.DatabaseManager.GetUsernamesAsync(player, token))
            : new ValueTask<PlayerNames>(PlayerNames.Nil);
    }
    public static bool IsInMain(this Player player)
    {
        if (!Data.Is<ITeams>()) return false;
        ulong team = player.GetTeam();
        return team switch
        {
            1 => TeamManager.Team1Main.IsInside(player.transform.position),
            2 => TeamManager.Team2Main.IsInside(player.transform.position),
            _ => false
        };
    }
    public static bool IsInMain(Vector3 point)
    {
        if (!Data.Is<ITeams>()) return false;
        return TeamManager.Team1Main.IsInside(point) || TeamManager.Team2Main.IsInside(point);
    }
    /// <remarks>Write-locks <see cref="ZoneList"/> if <paramref name="zones"/> is <see langword="true"/>.</remarks>
    public static string GetClosestLocationName(Vector3 point, bool zones = false, bool shortNames = false)
    {
        if (zones)
        {
            ZoneList? list = Data.Singletons.GetSingleton<ZoneList>();
            if (list != null)
            {
                list.WriteWait();
                try
                {
                    string? val = null;
                    float smallest = -1f;
                    Vector2 point2d = new Vector2(point.x, point.z);
                    for (int i = 0; i < list.Items.Count; ++i)
                    {
                        SqlItem<Zone> proxy = list.Items[i];
                        Zone? z = proxy.Item;
                        if (z != null)
                        {
                            float amt = (point2d - z.Center).sqrMagnitude;
                            if (smallest < 0f || amt < smallest)
                            {
                                val = shortNames ? z.ShortName ?? z.Name : z.Name;
                                smallest = amt;
                            }
                        }
                    }

                    if (val != null)
                        return val;
                }
                finally
                {
                    list.WriteRelease();
                }
            }
        }
        LocationDevkitNode? node = GetClosestLocation(point);
        return node == null ? new GridLocation(in point).ToString() : node.locationName;
    }
    public static LocationDevkitNode? GetClosestLocation(Vector3 point)
    {
        IReadOnlyList<LocationDevkitNode> list = LocationDevkitNodeSystem.Get().GetAllNodes();
        int index = -1;
        float smallest = -1f;
        for (int i = 0; i < list.Count; ++i)
        {
            float amt = (point - list[i].transform.position).sqrMagnitude;
            if (smallest < 0f || amt < smallest)
            {
                index = i;
                smallest = amt;
            }
        }

        if (index == -1)
            return null;
        return list[index];
    }
    public static bool FilterName(string original, out string final)
    {
        if (UCWarfare.Config.DisableNameFilter || UCWarfare.Config.MinAlphanumericStringLength <= 0)
        {
            final = original;
            return false;
        }
        IEnumerator<char> charenum = original.GetEnumerator();
        int alphanumcount = 0;
        while (charenum.MoveNext())
        {
            char ch = charenum.Current;
            int c = ch;
            if (c is > 31 and < 127)
            {
                if (alphanumcount - 1 >= UCWarfare.Config.MinAlphanumericStringLength)
                {
                    final = original;
                    charenum.Dispose();
                    return false;
                }
                alphanumcount++;
            }
            else
            {
                alphanumcount = 0;
            }
        }
        charenum.Dispose();
        final = original;
        return alphanumcount != original.Length;
    }
    public static ItemJarData[] GetItemsFromStorageState(ItemStorageAsset storage, byte[] state, out ItemDisplayData? displayData, PrimaryKey parent, bool clientState = false)
    {
        if (!Level.isLoaded)
            throw new Exception("Level not loaded.");
        GameThread.AssertCurrent();
        if (state.Length < 17)
        {
            displayData = null;
            return Array.Empty<ItemJarData>();
        }
        Block block = new Block(state);
        block.step += sizeof(ulong) * 2;
        ItemJarData[] rtn;
        if (!clientState)
        {
            int ct = block.readByte();
            rtn = new ItemJarData[ct];
            for (int i = 0; i < ct; ++i)
            {
                if (BarricadeManager.version > 7)
                {
                    object[] objArray = block.read(Types.BYTE_TYPE, Types.BYTE_TYPE, Types.BYTE_TYPE, Types.UINT16_TYPE, Types.BYTE_TYPE, Types.BYTE_TYPE, Types.BYTE_ARRAY_TYPE);
                    Guid guid = Assets.find(EAssetType.ITEM, (ushort)objArray[3]) is ItemAsset asset ? asset.GUID : new Guid((ushort)objArray[3], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                    rtn[i] = new ItemJarData(PrimaryKey.NotAssigned, parent, guid,
                        (byte)objArray[0], (byte)objArray[1], (byte)objArray[2], (byte)objArray[4], (byte)objArray[5],
                        (byte[])objArray[6]);
                }
                else
                {
                    object[] objArray = block.read(Types.BYTE_TYPE, Types.BYTE_TYPE, Types.UINT16_TYPE, Types.BYTE_TYPE, Types.BYTE_TYPE, Types.BYTE_ARRAY_TYPE);
                    Guid guid = Assets.find(EAssetType.ITEM, (ushort)objArray[2]) is ItemAsset asset ? asset.GUID : new Guid((ushort)objArray[2], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                    rtn[i] = new ItemJarData(PrimaryKey.NotAssigned, parent, guid,
                        (byte)objArray[0], (byte)objArray[1], 0, (byte)objArray[3], (byte)objArray[4],
                        (byte[])objArray[5]);
                }
            }
        }
        else rtn = Array.Empty<ItemJarData>();

        if (storage.isDisplay)
        {
            if (clientState)
            {
                object[] objArray = block.read(Types.UINT16_TYPE, Types.BYTE_TYPE, Types.BYTE_ARRAY_TYPE, Types.UINT16_TYPE, Types.UINT16_TYPE, Types.STRING_TYPE, Types.STRING_TYPE, Types.BYTE_TYPE);
                displayData = new ItemDisplayData(parent, (ushort)objArray[3], (ushort)objArray[4], (byte)objArray[7], (string)objArray[5], (string)objArray[6]);
            }
            else
            {
                ushort skin = block.readUInt16();
                ushort mythic = block.readUInt16();
                string? tags;
                string? dynProps;
                if (BarricadeManager.version > 12)
                {
                    tags = block.readString();
                    if (tags.Length == 0)
                        tags = null;
                    dynProps = block.readString();
                    if (dynProps.Length == 0)
                        dynProps = null;
                }
                else
                {
                    tags = null;
                    dynProps = null;
                }
                byte rot = BarricadeManager.version > 8 ? block.readByte() : (byte)0;
                displayData = new ItemDisplayData(parent, skin, mythic, rot, tags, dynProps);
            }
        }
        else displayData = null;

        return rtn;
    }
    

    public static bool RoughlyEquals(string? a, string? b) => string.Compare(a, b, CultureInfo.InvariantCulture,
        CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols) == 0;

    public static string ActionLogDisplay(this Asset asset) =>
        $"{asset.FriendlyName} / {asset.id.ToString(Data.AdminLocale)} / {asset.GUID:N}";
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="type"/> is not a valid value.</exception>
    public static EItemType GetItemType(this ClothingType type) => type switch
    {
        ClothingType.Shirt => EItemType.SHIRT,
        ClothingType.Pants => EItemType.PANTS,
        ClothingType.Vest => EItemType.VEST,
        ClothingType.Hat => EItemType.HAT,
        ClothingType.Mask => EItemType.MASK,
        ClothingType.Backpack => EItemType.BACKPACK,
        ClothingType.Glasses => EItemType.GLASSES,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
    public static bool ServerTrackQuest(this UCPlayer player, QuestAsset quest)
    {
        GameThread.AssertCurrent();
        if (player is not { IsOnline: true })
            return false;
        QuestAsset? current = player.Player.quests.GetTrackedQuest();
        if (current != null && quest != null)
        {
            if (current.GUID == quest.GUID && !player.Save.TrackQuests)
                player.Player.quests.ServerAddQuest(quest);

            return false;
        }
        if (player.Save.TrackQuests)
            player.Player.quests.ServerAddQuest(quest);
        return true;
    }
    public static bool ServerUntrackQuest(this UCPlayer player, QuestAsset quest)
    {
        GameThread.AssertCurrent();
        if (player is not { IsOnline: true })
            return false;
        QuestAsset current = player.Player.quests.GetTrackedQuest();
        if (current == quest)
            return false;

        if (player.Player.quests.GetTrackedQuest() == quest)
            player.Player.quests.TrackQuest(null);
        return true;
    }
    public static CombinedTokenSources CombineTokensIfNeeded(this ref CancellationToken token, CancellationToken other)
    {
        if (token.CanBeCanceled)
        {
            if (!other.CanBeCanceled)
                return new CombinedTokenSources(token, null);

            if (token == other)
                return new CombinedTokenSources(token, null);

            CancellationTokenSource src = CancellationTokenSource.CreateLinkedTokenSource(token, other);
            token = src.Token;
            return new CombinedTokenSources(token, src);
        }

        if (!other.CanBeCanceled)
            return default;
        
        token = other;
        return new CombinedTokenSources(other, null);
    }
    public static CombinedTokenSources CombineTokensIfNeeded(this ref CancellationToken token, CancellationToken other1, CancellationToken other2)
    {
        CancellationTokenSource src;
        if (token.CanBeCanceled)
        {
            if (other1.CanBeCanceled)
            {
                if (other2.CanBeCanceled)
                {
                    src = CancellationTokenSource.CreateLinkedTokenSource(token, other1, other2);
                    token = src.Token;
                    return new CombinedTokenSources(token, src);
                }

                src = CancellationTokenSource.CreateLinkedTokenSource(token, other1);
                token = src.Token;
                return new CombinedTokenSources(token, src);
            }

            if (!other2.CanBeCanceled)
                return new CombinedTokenSources(token, null);
            
            src = CancellationTokenSource.CreateLinkedTokenSource(token, other2);
            token = src.Token;
            return new CombinedTokenSources(token, src);
        }
        
        if (other1.CanBeCanceled)
        {
            if (other2.CanBeCanceled)
            {
                src = CancellationTokenSource.CreateLinkedTokenSource(other1, other2);
                token = src.Token;
                return new CombinedTokenSources(token, src);
            }

            token = other1;
            return new CombinedTokenSources(other1, null);
        }

        if (!other2.CanBeCanceled)
            return default;

        token = other2;
        return new CombinedTokenSources(token, null);

    }

    /// <summary>INSERT INTO `<paramref name="table"/>` (<paramref name="columns"/>[,`<paramref name="columnPk"/>`]) VALUES (parameters[,LAST_INSERT_ID(@pk)]) ON DUPLICATE KEY UPDATE (<paramref name="columns"/>,`<paramref name="columnPk"/>`=LAST_INSERT_ID(`<paramref name="columnPk"/>`);<br/>SET @pk := (SELECT LAST_INSERT_ID() as `pk`);<br/>SELECT @pk</summary>
    public static string BuildInitialInsertQuery(string table, string columnPk, bool hasPk, string? extPk, string[]? deleteTables, params string[] columns)
    {
        return "INSERT INTO `" + table + "` (" + SqlTypes.ColumnList(columns) +
            (hasPk ? $",`{columnPk}`" : string.Empty) +
            ") VALUES (" + SqlTypes.ParameterList(0, columns.Length) +
            (hasPk ? ",LAST_INSERT_ID(@" + columns.Length.ToString(Data.AdminLocale) + ")" : string.Empty) +
            ") ON DUPLICATE KEY UPDATE " +
            SqlTypes.ColumnUpdateList(0, columns) +
            $",`{columnPk}`=LAST_INSERT_ID(`{columnPk}`);" +
            "SET @pk := (SELECT LAST_INSERT_ID() as `pk`);" + (hasPk && extPk != null && deleteTables != null ? GetDeleteText(deleteTables, extPk, columns.Length) : string.Empty) +
            " SELECT @pk;";
    }
    private static string GetDeleteText(string[] deleteTables, string columnPk, int pk)
    {
        StringBuilder sb = new StringBuilder(deleteTables.Length * 15);
        for (int i = 0; i < deleteTables.Length; ++i)
            sb.Append("DELETE FROM `").Append(deleteTables[i]).Append("` WHERE `").Append(columnPk).Append("`=@").Append(pk.ToString(Data.AdminLocale)).Append(';');
        return sb.ToString();
    }

    /// <summary>INSERT INTO `<paramref name="table"/>` (<paramref name="columns"/>) VALUES </summary>
    public static string StartBuildOtherInsertQueryNoUpdate(string table, params string[] columns)
    {
        return $"INSERT INTO `{table}` (" + SqlTypes.ColumnList(columns) + ") VALUES ";
    }

    /// <summary> ON DUPLICATE KEY UPDATE (<paramref name="columns"/> without first column);</summary>
    /// <remarks>Assumes pk is first column.</remarks>
    public static string EndBuildOtherInsertQueryUpdate(params string[] columns)
    {
        return " ON DUPLICATE KEY UPDATE " + SqlTypes.ColumnUpdateList(1, 1, columns) + ";";
    }
    /// <summary>SELECT <paramref name="columns"/> FROM `<paramref name="table"/>` WHERE `<paramref name="checkColumnEquals"/>`=@<paramref name="parameter"/>;</summary>
    public static string BuildSelectWhere(string table, string checkColumnEquals, int parameter, params string[] columns)
    {
        return "SELECT " + SqlTypes.ColumnList(columns) +
               $" FROM `{table}` WHERE `{checkColumnEquals}`=@" + parameter.ToString(Data.AdminLocale) + ";";
    }

    public static bool IsDefault(this string str) => str.Equals(L.Default, StringComparison.OrdinalIgnoreCase);
    
    public static T[] AsArrayFast<T>(this IEnumerable<T> enumerable, bool copy = false)
    {
        if (enumerable == null)
            return Array.Empty<T>();
        if (enumerable is T[] arr1)
        {
            if (arr1.Length == 0)
                return Array.Empty<T>();
            if (copy)
            {
                T[] arr2 = new T[arr1.Length];
                Array.Copy(arr1, 0, arr2, 0, arr1.Length);
                return arr2;
            }

            return arr1;
        }
        if (enumerable is List<T> list1)
            return list1.Count == 0 ? Array.Empty<T>() : list1.ToArray();
        if (enumerable is ICollection<T> col1)
        {
            if (col1.Count == 0)
                return Array.Empty<T>();
            T[] arr2 = new T[col1.Count];
            col1.CopyTo(arr2, 0);
            return arr2;
        }

        return enumerable.ToArray();
    }

    public static T[] ToArrayFast<T>(this IEnumerable<T> enumerable, bool copy = false)
    {
        if (!copy && enumerable is T[] array)
            return array;

        if (enumerable is List<T> list && list.Count == list.Capacity && Accessor.TryGetUnderlyingArray(list, out T[] underlying))
        {
            return underlying;
        }

        return enumerable.ToArray();
    }
    /// <summary>
    /// Takes a list of primary key pairs and calls <paramref name="action"/> for each array of values per id.
    /// </summary>
    /// <remarks>The values should be sorted by ID, if not set <paramref name="sort"/> to <see langword="true"/>.</remarks>
    public static void ApplyQueriedList<T>(List<PrimaryKeyPair<T>> list, Action<int, T[]> action, bool sort = true)
    {
        if (list.Count == 0) return;

        if (sort && list.Count != 1)
            list.Sort((a, b) => a.Key.CompareTo(b.Key));

        T[] arr;
        int key;
        int last = -1;
        for (int i = 0; i < list.Count; i++)
        {
            PrimaryKeyPair<T> val = list[i];
            if (i <= 0 || list[i - 1].Key == val.Key)
                continue;

            arr = new T[i - 1 - last];
            for (int j = 0; j < arr.Length; ++j)
                arr[j] = list[last + j + 1].Value;
            last = i - 1;

            key = list[i - 1].Key;
            action(key, arr);
        }

        arr = new T[list.Count - 1 - last];
        for (int j = 0; j < arr.Length; ++j)
            arr[j] = list[last + j + 1].Value;

        key = list[list.Count - 1].Key;
        action(key, arr);
    }

    public static T? FindIndexed<T>(this T[] array, Func<T, int, bool> predicate)
    {
        for (int i = 0; i < array.Length; ++i)
        {
            if (predicate(array[i], i))
                return array[i];
        }

        return default;
    }

    public static async UniTask<string?> GetProfilePictureURL(ulong steam64, AvatarSize size, CancellationToken token = default)
    {
        if (!UCWarfare.IsLoaded)
            throw new SingletonUnloadedException(typeof(UCWarfare));
        if (UCPlayer.FromID(steam64) is { } player)
        {
            return await player.GetProfilePictureURL(size, token);
        }

        if (Data.ModerationSql.TryGetAvatar(steam64, size, out string url))
            return url;

        PlayerSummary summary = await GetPlayerSummary(steam64, token: token);

        return size switch
        {
            AvatarSize.Full => summary.AvatarUrlFull,
            AvatarSize.Medium => summary.AvatarUrlMedium,
            _ => summary.AvatarUrlSmall
        };
    }
    public static async UniTask<PlayerSummary> GetPlayerSummary(ulong steam64, bool allowCache = true, CancellationToken token = default)
    {
        if (UCWarfare.IsLoaded && UCPlayer.FromID(steam64) is { } player)
        {
            return await player.GetPlayerSummary(allowCache, token);
        }

        PlayerSummary? playerSummary = await Steam.SteamAPIService.GetPlayerSummary(steam64, token);
        await UniTask.SwitchToMainThread(token);
#if DEBUG
        GameThread.AssertCurrent();
#endif

        if (playerSummary != null && UCWarfare.IsLoaded)
        {
            if (!string.IsNullOrEmpty(playerSummary.AvatarUrlSmall))
                Data.ModerationSql.UpdateAvatar(steam64, AvatarSize.Small, playerSummary.AvatarUrlSmall);
            if (!string.IsNullOrEmpty(playerSummary.AvatarUrlMedium))
                Data.ModerationSql.UpdateAvatar(steam64, AvatarSize.Medium, playerSummary.AvatarUrlMedium);
            if (!string.IsNullOrEmpty(playerSummary.AvatarUrlFull))
                Data.ModerationSql.UpdateAvatar(steam64, AvatarSize.Full, playerSummary.AvatarUrlFull);
        }

        return playerSummary ?? new PlayerSummary
        {
            Steam64 = steam64,
            PlayerName = steam64.ToString()
        };
    }
}
public readonly struct PrimaryKeyPair<T>
{
    public uint Key { get; }
    public T Value { get; }
    public PrimaryKeyPair(uint key, T value)
    {
        Key = key;
        Value = value;
    }

    public override string ToString() => $"({{{Key}}}, {(Value is null ? "NULL" : Value.ToString())})";
}
public readonly struct CombinedTokenSources : IDisposable
{
    private readonly CancellationTokenSource? _tknSrc;
    public readonly CancellationToken Token;
    internal CombinedTokenSources(CancellationToken token, CancellationTokenSource? tknSrc)
    {
        Token = token;
        _tknSrc = tknSrc;
    }
    public void Dispose()
    {
        _tknSrc?.Dispose();
    }
}