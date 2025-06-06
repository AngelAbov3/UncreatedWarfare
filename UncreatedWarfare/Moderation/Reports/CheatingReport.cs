using DanielWillett.SpeedBytes;
using DanielWillett.SpeedBytes.Unity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration.JsonConverters;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Moderation.Reports;

[ModerationEntry(ModerationEntryType.CheatingReport)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class CheatingReport : Report
{
    [JsonPropertyName("hits")]
    public ShotRecord[] Shots { get; set; } = Array.Empty<ShotRecord>();
    public double GetAccuracy(ERaycastInfoType? type = null)
    {
        int hits = 0;

        for (int i = 0; i < Shots.Length; ++i)
        {
            ref ShotRecord shot = ref Shots[i];
            if (shot.HitType == ERaycastInfoType.NONE || type.HasValue && shot.HitType != type.Value)
                continue;

            ++hits;
        }

        return hits / (double)Shots.Length;
    }

    public double GetAccuracy(out AccuracyMap map, ERaycastInfoType? type = null)
    {
        int hits = 0, head = 0, spine = 0, lfoot = 0, rfoot = 0, lleg = 0, rleg = 0, lhand = 0, rhand = 0, larm = 0, rarm = 0, lback = 0, rback = 0, lfront = 0, rfront = 0;

        for (int i = 0; i < Shots.Length; ++i)
        {
            ref ShotRecord shot = ref Shots[i];
            if (shot.HitType == ERaycastInfoType.NONE || type.HasValue && shot.HitType != type.Value)
                continue;

            ++hits;

            if (!shot.Limb.HasValue)
                continue;

            switch (shot.Limb.Value)
            {
                case ELimb.LEFT_FOOT:
                    ++lfoot;
                    break;
                case ELimb.LEFT_LEG:
                    ++lleg;
                    break;
                case ELimb.RIGHT_FOOT:
                    ++rfoot;
                    break;
                case ELimb.RIGHT_LEG:
                    ++rleg;
                    break;
                case ELimb.LEFT_HAND:
                    ++lhand;
                    break;
                case ELimb.LEFT_ARM:
                    ++larm;
                    break;
                case ELimb.RIGHT_HAND:
                    ++rhand;
                    break;
                case ELimb.RIGHT_ARM:
                    ++rarm;
                    break;
                case ELimb.LEFT_BACK:
                    ++lback;
                    break;
                case ELimb.RIGHT_BACK:
                    ++rback;
                    break;
                case ELimb.LEFT_FRONT:
                    ++lfront;
                    break;
                case ELimb.RIGHT_FRONT:
                    ++rfront;
                    break;
                case ELimb.SPINE:
                    ++spine;
                    break;
                case ELimb.SKULL:
                    ++head;
                    break;
            }
        }

        double max = Shots.Length;

        map = new AccuracyMap(hits / max, head / max, spine / max, lfoot / max, rfoot / max, lleg / max, rleg / max,
            lhand / max, rhand / max, larm / max, rarm / max, lback / max, rback / max, lfront / max, rfront / max);

        return hits / max;
    }
    public override string GetDisplayName() => "Cheating Report";
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        Shots = new ShotRecord[reader.ReadInt32()];
        for (int i = 0; i < Shots.Length; ++i)
            Shots[i] = new ShotRecord(reader);
    }
    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);

        writer.Write(Shots.Length);
        for (int i = 0; i < Shots.Length; ++i)
            Shots[i].Write(writer);
    }
    public override bool ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("hits", StringComparison.InvariantCultureIgnoreCase))
            Shots = JsonSerializer.Deserialize<ShotRecord[]>(ref reader, options) ?? Array.Empty<ShotRecord>();
        else
            return base.ReadProperty(ref reader, propertyName, options);

        return true;
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);

        writer.WritePropertyName("hits");
        JsonSerializer.Serialize(writer, Shots, options);
    }
    internal override int EstimateParameterCount() => base.EstimateParameterCount() + Shots.Length * 22;
    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        await base.AddExtraInfo(db, workingList, formatter, token);
        
        GetAccuracy(out AccuracyMap map, ERaycastInfoType.PLAYER);
        StringBuilder limbMask = new StringBuilder(16);

        if (map.HeadAccuracy > 0)
            limbMask.Append("|SK");
        if (map.SpineAccuracy > 0)
            limbMask.Append("|SP");
        if (map.LeftArmAccuracy > 0)
            limbMask.Append("|LA");
        if (map.RightArmAccuracy > 0)
            limbMask.Append("|RA");
        if (map.LeftHandAccuracy > 0)
            limbMask.Append("|LH");
        if (map.RightHandAccuracy > 0)
            limbMask.Append("|RH");
        if (map.LeftLegAccuracy > 0)
            limbMask.Append("|LL");
        if (map.RightLegAccuracy > 0)
            limbMask.Append("|RL");
        if (map.LeftFootAccuracy > 0)
            limbMask.Append("|LFT");
        if (map.RightFootAccuracy > 0)
            limbMask.Append("|RFT");
        if (map.LeftFrontAccuracy > 0)
            limbMask.Append("|LFR");
        if (map.RightFrontAccuracy > 0)
            limbMask.Append("|RFR");
        if (map.LeftBackAccuracy > 0)
            limbMask.Append("|LB");
        if (map.RightBackAccuracy > 0)
            limbMask.Append("|RB");

        workingList.Add($"Recorded Shots: {Shots.Length.ToString(formatter)}");
        if (map.OverallAccuracy > 0)
        {
            workingList.Add($"Overall Player Accuracy: {map.OverallAccuracy.ToString("P1", formatter)}");
            workingList.Add($"Skull Accuracy: {map.HeadAccuracy.ToString("P1", formatter)}");
            workingList.Add($"Spine Accuracy: {map.SpineAccuracy.ToString("P1", formatter)}");
            if (limbMask.Length > 0)
                workingList.Add($"Limb mask: {limbMask.ToString(1, limbMask.Length - 1)}");
        }
        else
        {
            workingList.Add("No player hits.");
        }
    }
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($"DELETE FROM `{DatabaseInterface.TableReportShotRecords}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (Shots.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableReportShotRecords}` ({MySqlSnippets.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnReportsShotRecordAmmo, DatabaseInterface.ColumnReportsShotRecordAmmoName,
                DatabaseInterface.ColumnReportsShotRecordItem, DatabaseInterface.ColumnReportsShotRecordItemName,
                DatabaseInterface.ColumnReportsShotRecordDamageDone, DatabaseInterface.ColumnReportsShotRecordLimb,
                DatabaseInterface.ColumnReportsShotRecordIsProjectile, DatabaseInterface.ColumnReportsShotRecordDistance,
                DatabaseInterface.ColumnReportsShotRecordHitPointX, DatabaseInterface.ColumnReportsShotRecordHitPointY, DatabaseInterface.ColumnReportsShotRecordHitPointZ,
                DatabaseInterface.ColumnReportsShotRecordShootFromPointX, DatabaseInterface.ColumnReportsShotRecordShootFromPointY, DatabaseInterface.ColumnReportsShotRecordShootFromPointZ,
                DatabaseInterface.ColumnReportsShotRecordShootFromRotationX, DatabaseInterface.ColumnReportsShotRecordShootFromRotationY, DatabaseInterface.ColumnReportsShotRecordShootFromRotationZ,
                DatabaseInterface.ColumnReportsShotRecordHitType, DatabaseInterface.ColumnReportsShotRecordHitActor,
                DatabaseInterface.ColumnReportsShotRecordHitAsset, DatabaseInterface.ColumnReportsShotRecordHitAssetName,
                DatabaseInterface.ColumnReportsShotRecordTimestamp)}) VALUES ");

            for (int i = 0; i < Shots.Length; ++i)
            {
                ref ShotRecord record = ref Shots[i];
                MySqlSnippets.AppendPropertyList(builder, args.Count, 22, i, 1);
                args.Add(record.Ammo.ToString("N"));
                args.Add(record.AmmoName.Truncate(48) ?? string.Empty);
                args.Add(record.Item.ToString("N"));
                args.Add(record.ItemName.Truncate(48) ?? string.Empty);
                args.Add(record.DamageDone);
                args.Add(record.Limb.HasValue ? record.Limb.Value.ToString() : DBNull.Value);
                args.Add(record.IsProjectile);
                args.Add(record.Distance);
                if (record.HitPoint.HasValue)
                {
                    args.Add(record.HitPoint.Value.x);
                    args.Add(record.HitPoint.Value.y);
                    args.Add(record.HitPoint.Value.z);
                }
                else
                {
                    args.Add(DBNull.Value);
                    args.Add(DBNull.Value);
                    args.Add(DBNull.Value);
                }
                args.Add(record.ShootFromPoint.x);
                args.Add(record.ShootFromPoint.y);
                args.Add(record.ShootFromPoint.z);
                args.Add(record.ShootFromRotation.x);
                args.Add(record.ShootFromRotation.y);
                args.Add(record.ShootFromRotation.z);
                args.Add(record.HitType.ToString());
                args.Add(record.HitActor == null ? DBNull.Value : record.HitActor.Id);
                args.Add(record.HitAsset.HasValue ? record.HitAsset.Value.ToString("N") : DBNull.Value);
                args.Add(record is { HitAsset: not null, HitAssetName: not null } ? record.HitAssetName : DBNull.Value);
                args.Add(record.Timestamp.UtcDateTime);
            }

            builder.Append(';');
        }

        return hasEvidenceCalls;
    }
}

public struct AccuracyMap
{
    [JsonPropertyName("overall_accuracy")]
    public double OverallAccuracy { get; set; }

    [JsonPropertyName("head_accuracy")]
    public double HeadAccuracy { get; set; }

    [JsonPropertyName("spine_accuracy")]
    public double SpineAccuracy { get; set; }

    [JsonPropertyName("left_foot_accuracy")]
    public double LeftFootAccuracy { get; set; }

    [JsonPropertyName("right_foot_accuracy")]
    public double RightFootAccuracy { get; set; }

    [JsonPropertyName("left_leg_accuracy")]
    public double LeftLegAccuracy { get; set; }

    [JsonPropertyName("right_leg_accuracy")]
    public double RightLegAccuracy { get; set; }

    [JsonPropertyName("left_hand_accuracy")]
    public double LeftHandAccuracy { get; set; }

    [JsonPropertyName("right_hand_accuracy")]
    public double RightHandAccuracy { get; set; }

    [JsonPropertyName("left_arm_accuracy")]
    public double LeftArmAccuracy { get; set; }

    [JsonPropertyName("right_arm_accuracy")]
    public double RightArmAccuracy { get; set; }

    [JsonPropertyName("left_back_accuracy")]
    public double LeftBackAccuracy { get; set; }

    [JsonPropertyName("right_back_accuracy")]
    public double RightBackAccuracy { get; set; }

    [JsonPropertyName("left_front_accuracy")]
    public double LeftFrontAccuracy { get; set; }

    [JsonPropertyName("right_front_accuracy")]
    public double RightFrontAccuracy { get; set; }

    public AccuracyMap() { }
    public AccuracyMap(double overallAccuracy, double headAccuracy, double spineAccuracy, double leftFootAccuracy, double rightFootAccuracy, double leftLegAccuracy, double rightLegAccuracy, double leftHandAccuracy, double rightHandAccuracy, double leftArmAccuracy, double rightArmAccuracy, double leftBackAccuracy, double rightBackAccuracy, double leftFrontAccuracy, double rightFrontAccuracy)
    {
        OverallAccuracy = overallAccuracy;
        HeadAccuracy = headAccuracy;
        SpineAccuracy = spineAccuracy;
        LeftFootAccuracy = leftFootAccuracy;
        RightFootAccuracy = rightFootAccuracy;
        LeftLegAccuracy = leftLegAccuracy;
        RightLegAccuracy = rightLegAccuracy;
        LeftHandAccuracy = leftHandAccuracy;
        RightHandAccuracy = rightHandAccuracy;
        LeftArmAccuracy = leftArmAccuracy;
        RightArmAccuracy = rightArmAccuracy;
        LeftBackAccuracy = leftBackAccuracy;
        RightBackAccuracy = rightBackAccuracy;
        LeftFrontAccuracy = leftFrontAccuracy;
        RightFrontAccuracy = rightFrontAccuracy;
    }
}

public readonly struct ShotRecord
{
    [JsonPropertyName("item")]
    public Guid Item { get; }

    [JsonPropertyName("ammo")]
    public Guid Ammo { get; }

    [JsonPropertyName("item_name")]
    public string? ItemName { get; }

    [JsonPropertyName("ammo_name")]
    public string? AmmoName { get; }

    [JsonPropertyName("hit_type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ERaycastInfoType HitType { get; }

    [JsonPropertyName("hit_actor")]
    [JsonConverter(typeof(ActorConverter))]
    public IModerationActor? HitActor { get; }

    [JsonPropertyName("hit_asset")]
    public Guid? HitAsset { get; }

    [JsonPropertyName("hit_asset_name")]
    public string? HitAssetName { get; }

    [JsonPropertyName("limb")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ELimb? Limb { get; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; }

    [JsonPropertyName("aim_from_pos")]
    [JsonConverter(typeof(Vector3JsonConverter))]
    public Vector3 ShootFromPoint { get; }

    [JsonPropertyName("aim_from_rot")]
    [JsonConverter(typeof(Vector3JsonConverter))]
    public Vector3 ShootFromRotation { get; }

    [JsonPropertyName("aim_hit")]
    [JsonConverter(typeof(Vector3JsonConverter))]
    public Vector3? HitPoint { get; }

    [JsonPropertyName("is_projectile")]
    public bool IsProjectile { get; }

    [JsonPropertyName("damage")]
    public int DamageDone { get; }

    [JsonPropertyName("distance")]
    public double Distance { get; }

    public ShotRecord() { }
    public ShotRecord(Guid item, Guid ammo, string? itemName, string? ammoName, ERaycastInfoType hitType, IModerationActor? hitActor, Guid? hitAsset, string? hitAssetName, ELimb? limb, DateTimeOffset timestamp, Vector3 shootFromPoint, Vector3 shootFromRotation, Vector3? hitPoint, bool isProjectile, int damageDone, double distance)
    {
        Item = item;
        Ammo = ammo;
        ItemName = itemName;
        AmmoName = ammoName;
        HitType = hitType;
        HitActor = hitActor;
        HitAsset = hitAsset;
        HitAssetName = hitAssetName;
        Limb = limb;
        Timestamp = timestamp;
        ShootFromPoint = shootFromPoint;
        ShootFromRotation = shootFromRotation;
        HitPoint = hitPoint;
        IsProjectile = isProjectile;
        DamageDone = damageDone;
        Distance = distance;
    }

    public ShotRecord(ByteReader reader)
    {
        Item = reader.ReadGuid();
        ItemName = reader.ReadNullableString();
        Ammo = reader.ReadGuid();
        AmmoName = reader.ReadNullableString();
        HitType = (ERaycastInfoType)reader.ReadUInt8();
        byte flag = reader.ReadUInt8();
        if (HitType != ERaycastInfoType.NONE)
        {
            HitActor = (flag & 1) != 0 ? Actors.GetActor(reader.ReadUInt64()) : null;
            HitAsset = (flag & 2) != 0 ? reader.ReadGuid() : null;
            HitAssetName = (flag & 2) != 0 ? reader.ReadNullableString() : null;
            Limb = (flag & 4) != 0 ? (ELimb)reader.ReadUInt8() : null;
        }
        else
        {
            HitActor = null;
            HitAsset = null;
            HitAssetName = null;
            Limb = null;
        }
        Timestamp = reader.ReadDateTimeOffset();
        ShootFromPoint = reader.ReadVector3();
        ShootFromRotation = reader.ReadVector3();
        IsProjectile = (flag & 8) != 0;
        HitPoint = (flag & 16) != 0 ? reader.ReadVector3() : null;
        DamageDone = reader.ReadInt32();
        Distance = reader.ReadDouble();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Item);
        writer.WriteNullable(ItemName);
        writer.Write(Ammo);
        writer.WriteNullable(AmmoName);
        writer.Write((byte)HitType);
        byte flag = (byte)((HitActor != null ? 1 : 0) | (HitAsset.HasValue ? 2 : 0) | (Limb.HasValue ? 4 : 0) | (IsProjectile ? 8 : 0) | (HitPoint.HasValue ? 16 : 0));
        writer.Write(flag);
        if (HitType != ERaycastInfoType.NONE)
        {
            if (HitActor != null)
                writer.Write(HitActor.Id);
            if (HitAsset.HasValue)
            {
                writer.Write(HitAsset.Value);
                writer.WriteNullable(HitAssetName);
            }

            if (Limb.HasValue)
                writer.Write((byte)Limb.Value);
        }
        writer.Write(Timestamp);
        writer.Write(ShootFromPoint);
        writer.Write(ShootFromRotation);
        if (HitPoint.HasValue)
            writer.Write(HitPoint.Value);
        writer.Write(DamageDone);
        writer.Write(Distance);
    }
}