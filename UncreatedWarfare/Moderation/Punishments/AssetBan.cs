﻿using SDG.Framework.Utilities;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.SQL;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Moderation.Punishments;

[ModerationEntry(ModerationEntryType.AssetBan)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class AssetBan : DurationPunishment
{
    private static char[]? _split;

    [JsonPropertyName("vehicle_type_filter")]
    [JsonConverter(typeof(ArrayConverter<VehicleType, JsonStringEnumConverter>))]
    public VehicleType[] VehicleTypeFilter { get; set; }

    internal string FillFromText(string? text)
    {
        ThreadUtil.assertIsGameThread();

        if (string.IsNullOrWhiteSpace(text)
            || text!.Equals("*", StringComparison.InvariantCultureIgnoreCase)
            || text.Equals("all", StringComparison.InvariantCultureIgnoreCase))
        {
            VehicleTypeFilter = Array.Empty<VehicleType>();
            return string.Empty;
        }

        _split ??= new char[] { ',' };
        string[] splits = text!.Split(_split, StringSplitOptions.RemoveEmptyEntries);
        List<VehicleType>? vehicleTypes = null;

        StringBuilder response = new StringBuilder();
        
        for (int i = 0; i < splits.Length; ++i)
        {
            string val = splits[i];
            ParseValue(val, out VehicleType single, out VehicleType[]? types);

            if (types is { Length: > 0 })
            {
                (vehicleTypes ??= new List<VehicleType>(2)).AddRange(types);
                for (int j = 0; j < types.Length; ++j)
                {
                    if (response.Length > 0)
                        response.Append(", ");
                    response.Append(types[j].ToString());
                }
            }
            else if (single != VehicleType.None)
            {
                (vehicleTypes ??= new List<VehicleType>(2)).Add(single);
                if (response.Length > 0)
                    response.Append(", ");
                response.Append(single.ToString());
            }
        }

        VehicleTypeFilter = vehicleTypes == null ? Array.Empty<VehicleType>() : vehicleTypes.ToArray();

        return response.ToString();
    }

    private static void ParseValue(string input, out VehicleType single, out VehicleType[]? vehicleTypes)
    {
        single = default;
        vehicleTypes = null;
        if (!input.Equals("emplacement", StringComparison.InvariantCultureIgnoreCase) && Enum.TryParse(input, true, out VehicleType type))
        {
            single = type;
            return;
        }

        if (input.StartsWith("transport", StringComparison.InvariantCultureIgnoreCase))
        {
            vehicleTypes = new VehicleType[] { VehicleType.TransportAir, VehicleType.TransportGround };
        }
        else if (input.StartsWith("air", StringComparison.InvariantCultureIgnoreCase))
        {
            vehicleTypes = new VehicleType[] { VehicleType.TransportAir, VehicleType.Jet, VehicleType.AttackHeli };
        }
        else if (input.StartsWith("armor", StringComparison.InvariantCultureIgnoreCase))
        {
            vehicleTypes = new VehicleType[] { VehicleType.APC, VehicleType.IFV, VehicleType.MBT, VehicleType.ScoutCar };
        }
        else if (input.StartsWith("logi", StringComparison.InvariantCultureIgnoreCase))
        {
            vehicleTypes = new VehicleType[] { VehicleType.LogisticsGround, VehicleType.TransportAir };
        }
        else if(input.StartsWith("assault air", StringComparison.InvariantCultureIgnoreCase)
               || input.StartsWith("assaultair", StringComparison.InvariantCultureIgnoreCase)
               || input.StartsWith("airassault", StringComparison.InvariantCultureIgnoreCase)
               || input.StartsWith("air assault", StringComparison.InvariantCultureIgnoreCase))
        {
            vehicleTypes = new VehicleType[] { VehicleType.AttackHeli, VehicleType.Jet };
        }
        else if (input.StartsWith("empl", StringComparison.InvariantCultureIgnoreCase))
        {
            vehicleTypes = new VehicleType[] { VehicleType.HMG, VehicleType.ATGM, VehicleType.AA, VehicleType.Mortar };
        }
    }
    public bool IsAssetBanned(VehicleType type, bool considerForgiven, bool checkStillActive = true)
    {
        if (checkStillActive && !IsApplied(considerForgiven))
            return false;

        if (!checkStillActive && considerForgiven && (Forgiven || Removed))
            return true;
        
        if (VehicleTypeFilter.Length == 0) return true;
        if (type == VehicleType.None) return false;
        for (int i = 0; i < VehicleTypeFilter.Length; ++i)
        {
            if (VehicleTypeFilter[i] == type)
                return true;
        }

        return false;
    }
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);
        
        VehicleTypeFilter = new VehicleType[reader.ReadInt32()];
        for (int i = 0; i < VehicleTypeFilter.Length; ++i)
            VehicleTypeFilter[i] = (VehicleType)reader.ReadUInt16();
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);
        
        writer.Write(VehicleTypeFilter.Length);
        for (int i = 0; i < VehicleTypeFilter.Length; ++i)
            writer.Write((ushort)VehicleTypeFilter[i]);
    }

    public override string GetDisplayName() => "Asset Ban";
    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("vehicle_type_filter", StringComparison.InvariantCultureIgnoreCase))
        {
            if (reader.TokenType == JsonTokenType.Null)
                VehicleTypeFilter = Array.Empty<VehicleType>();
            else if (reader.TokenType == JsonTokenType.StartArray)
            {
                List<VehicleType> list;
                bool pooled = false;
                if (UCWarfare.IsLoaded && UCWarfare.IsMainThread)
                {
                    pooled = true;
                    list = ListPool<VehicleType>.claim();
                }
                else list = new List<VehicleType>(16);

                try
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                            break;
                        switch (reader.TokenType)
                        {
                            case JsonTokenType.Null:
                                list.Add(VehicleType.None);
                                break;
                            case JsonTokenType.String:
                                list.Add((VehicleType)Enum.Parse(typeof(VehicleType), reader.GetString()!, true));
                                break;
                            case JsonTokenType.Number:
                                list.Add((VehicleType)reader.GetInt32());
                                break;
                            default:
                                throw new JsonException($"Invalid token type: {reader.TokenType} for VehicleType[] element.");
                        }
                    }

                    VehicleTypeFilter = list.Count == 0 ? Array.Empty<VehicleType>() : list.ToArray();
                }
                finally
                {
                    if (pooled)
                        ListPool<VehicleType>.release(list);
                }
            }
            else
                throw new JsonException($"Invalid token type: {reader.TokenType} for VehicleType[].");
        }
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);
        writer.WritePropertyName("vehicle_type_filter");
        writer.WriteStartArray();
        for (int i = 0; i < VehicleTypeFilter.Length; ++i)
            writer.WriteStringValue(VehicleTypeFilter[i].ToString());
        writer.WriteEndArray();
    }

    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        await base.AddExtraInfo(db, workingList, formatter, token);
        if (VehicleTypeFilter.Length > 0)
        {
            StringBuilder sb = new StringBuilder();
            List<VehicleType> types = new List<VehicleType>(VehicleTypeFilter);
            if (Array.IndexOf(VehicleTypeFilter, VehicleType.TransportAir) != -1 && Array.IndexOf(VehicleTypeFilter, VehicleType.TransportGround) != -1)
            {
                types.Remove(VehicleType.TransportAir);
                types.Remove(VehicleType.TransportGround);
                sb.Append("Transports");
            }
            if (Array.IndexOf(VehicleTypeFilter, VehicleType.TransportAir) != -1 && Array.IndexOf(VehicleTypeFilter, VehicleType.Jet) != -1
                && Array.IndexOf(VehicleTypeFilter, VehicleType.AttackHeli) != -1)
            {
                types.Remove(VehicleType.TransportAir);
                types.Remove(VehicleType.Jet);
                types.Remove(VehicleType.AttackHeli);
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append("Aircraft");
            }
            else if (Array.IndexOf(VehicleTypeFilter, VehicleType.Jet) != -1 && Array.IndexOf(VehicleTypeFilter, VehicleType.AttackHeli) != -1)
            {
                types.Remove(VehicleType.Jet);
                types.Remove(VehicleType.AttackHeli);
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append("Assault Aircraft");
            }
            if (Array.IndexOf(VehicleTypeFilter, VehicleType.APC) != -1 && Array.IndexOf(VehicleTypeFilter, VehicleType.IFV) != -1
                                                                        && Array.IndexOf(VehicleTypeFilter, VehicleType.MBT) != -1
                                                                        && Array.IndexOf(VehicleTypeFilter, VehicleType.ScoutCar) != -1)
            {
                types.Remove(VehicleType.APC);
                types.Remove(VehicleType.IFV);
                types.Remove(VehicleType.MBT);
                types.Remove(VehicleType.ScoutCar);
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append("Armors");
            }
            if (Array.IndexOf(VehicleTypeFilter, VehicleType.LogisticsGround) != -1 && Array.IndexOf(VehicleTypeFilter, VehicleType.TransportAir) != -1)
            {
                types.Remove(VehicleType.LogisticsGround);
                types.Remove(VehicleType.TransportAir);
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append("Loigistics");
            }
            if (Array.IndexOf(VehicleTypeFilter, VehicleType.HMG) != -1 && Array.IndexOf(VehicleTypeFilter, VehicleType.ATGM) != -1
                                                                        && Array.IndexOf(VehicleTypeFilter, VehicleType.AA) != -1
                                                                        && Array.IndexOf(VehicleTypeFilter, VehicleType.Mortar) != -1)
            {
                types.Remove(VehicleType.HMG);
                types.Remove(VehicleType.ATGM);
                types.Remove(VehicleType.AA);
                types.Remove(VehicleType.Mortar);
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append("Emplacements");
            }
            for (int i = 0; i < types.Count; ++i)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(Localization.TranslateEnum(types));
            }
            
            workingList.Add("Type Filter:");
            workingList.Add(sb.ToString());
        }
        else
        {
            workingList.Add("Asset banned from all assets");
        }
    }

    internal override int EstimateParameterCount() => base.EstimateParameterCount() + VehicleTypeFilter.Length;
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);
        
        builder.Append($"DELETE FROM `{DatabaseInterface.TableAssetBanTypeFilters}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (VehicleTypeFilter.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableAssetBanTypeFilters}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnAssetBanFiltersType)}) VALUES ");

            for (int i = 0; i < VehicleTypeFilter.Length; ++i)
            {
                F.AppendPropertyList(builder, args.Count, 1, i, 1);
                args.Add(VehicleTypeFilter[i].ToString());
            }

            builder.Append(';');
        }

        return hasEvidenceCalls;
    }
}
