﻿using MySqlConnector;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Kits;

public class Kit : IListItem, ITranslationArgument, IReadWrite, ICloneable
{
    public PrimaryKey PrimaryKey { get; set; }
    public PrimaryKey FactionKey { get; set; }
    public string Id { get; set; }
    [CommandSettable]
    public Class Class { get; set; }
    [CommandSettable]
    public Branch Branch { get; set; }
    [CommandSettable]
    public KitType Type { get; set; }
    [CommandSettable("IsDisabled")]
    public bool Disabled { get; set; }
    [CommandSettable("NitroBooster")]
    public bool RequiresNitro { get; set; }
    [CommandSettable("MapWhitelist")]
    public bool MapFilterIsWhitelist { get; set; }
    [CommandSettable("FactionWhitelist")]
    public bool FactionFilterIsWhitelist { get; set; }
    [CommandSettable]
    public int Season { get; set; }
    [CommandSettable]
    public float RequestCooldown { get; set; }
    [CommandSettable]
    public float TeamLimit { get; set; }
    [CommandSettable]
    public int CreditCost { get; set; }
    [CommandSettable]
    public decimal PremiumCost { get; set; }
    [CommandSettable]
    public SquadLevel SquadLevel { get; set; }
    public TranslationList SignText { get; set; }
    public IKitItem[] Items { get; set; }
    public UnlockRequirement[] UnlockRequirements { get; set; }
    public Skillset[] Skillsets { get; set; }
    public PrimaryKey[] FactionFilter { get; set; }
    public PrimaryKey[] MapFilter { get; set; }
    public PrimaryKey[] RequestSigns { get; set; }
    [CommandSettable("Weapons")]
    public string? WeaponText { get; set; }
    public DateTimeOffset CreatedTimestamp { get; set; }
    public ulong Creator { get; set; }
    public DateTimeOffset LastEditedTimestamp { get; set; }
    public ulong LastEditor { get; set; }
    [JsonIgnore]
    internal List<KeyValuePair<KeyValuePair<ItemAsset?, RedirectType>, int>>? ItemListCache { get; set; }

    [JsonIgnore]
    internal string? ClothingSetCache { get; set; }
    public FactionInfo? Faction
    {
        get => FactionKey.IsValid ? TeamManager.GetFactionInfo(FactionKey) : null;
        set
        {
            if (value is null)
                FactionKey = PrimaryKey.NotAssigned;
            else if (!value.PrimaryKey.IsValid)
                throw new ArgumentException("Invalid faction provided, no key set.", nameof(value));
            else FactionKey = value.PrimaryKey;
        }
    }
    /// <summary>Checks that the kit is publicly available and has a vaild class (not None or Unarmed).</summary>
    public bool IsPublicKit => Type == KitType.Public && Class > Class.Unarmed;

    /// <summary>Elite kit or loadout.</summary>
    public bool IsPaid => Type is KitType.Elite or KitType.Loadout;
    /// <summary>Checks disabled status, season, map blacklist, faction blacklist. Checks both active teams, use <see cref="IsRequestable(ulong)"/> to check for a certain team.</summary>
    public bool Requestable => !Disabled && (Season >= UCWarfare.Season || Season < 1) &&
                             IsCurrentMapAllowed() &&
                             (IsFactionAllowed(TeamManager.Team1Faction) || IsFactionAllowed(TeamManager.Team2Faction));
    /// <summary>Checks disabled status, season, map blacklist, faction blacklist.</summary>
    public bool IsRequestable(ulong team) => team is not 1ul and not 2ul ? Requestable : (!Disabled && (Season >= UCWarfare.Season || Season < 1) &&
                             IsCurrentMapAllowed() &&
                             IsFactionAllowed(TeamManager.GetFaction(team)));
    /// <summary>Checks disabled status, season, map blacklist, faction blacklist.</summary>
    public bool IsRequestable(FactionInfo? faction) => faction is null ? Requestable : (!Disabled && (Season >= UCWarfare.Season || Season < 1) &&
                                                                               IsCurrentMapAllowed() &&
                                                                               IsFactionAllowed(faction));
    public Kit(string id, Class @class, Branch branch, KitType type, SquadLevel squadLevel, FactionInfo? faction)
    {
        Faction = faction;
        Id = id;
        Class = @class;
        Branch = branch;
        Type = type;
        SquadLevel = squadLevel;
        SignText = new TranslationList(id);
        Items = Array.Empty<IKitItem>();
        UnlockRequirements = Array.Empty<UnlockRequirement>();
        Skillsets = Array.Empty<Skillset>();
        FactionFilter = Array.Empty<PrimaryKey>();
        MapFilter = Array.Empty<PrimaryKey>();
        RequestSigns = Array.Empty<PrimaryKey>();
        Season = UCWarfare.Season;
        TeamLimit = KitManager.GetDefaultTeamLimit(@class);
        RequestCooldown = KitManager.GetDefaultRequestCooldown(@class);
        CreatedTimestamp = LastEditedTimestamp = DateTime.UtcNow;
        /* DEFAULTS *
        FactionFilterIsWhitelist = false;
        MapFilterIsWhitelist = false;
        Disabled = false;
        CreditCost = 0;
        PremiumCost = 0m;
        WeaponText = null;
        */
    }
    public Kit(string id, Kit copy)
    {
        FactionKey = copy.FactionKey;
        Id = id;
        Class = copy.Class;
        Branch = copy.Branch;
        Type = copy.Type;
        SquadLevel = copy.SquadLevel;
        SignText = (TranslationList)copy.SignText.Clone();
        Items = F.CloneArray(copy.Items);
        UnlockRequirements = F.CloneArray(copy.UnlockRequirements);
        Skillsets = F.CloneStructArray(copy.Skillsets);
        FactionFilter = F.CloneStructArray(copy.FactionFilter);
        MapFilter = F.CloneStructArray(copy.MapFilter);
        RequestSigns = Array.Empty<PrimaryKey>();
        Season = copy.Season;
        TeamLimit = copy.TeamLimit;
        RequestCooldown = copy.RequestCooldown;
        FactionFilterIsWhitelist = copy.FactionFilterIsWhitelist;
        MapFilterIsWhitelist = copy.MapFilterIsWhitelist;
        Disabled = copy.Disabled;
        CreditCost = copy.CreditCost;
        PremiumCost = copy.PremiumCost;
        WeaponText = copy.WeaponText;
        CreatedTimestamp = DateTime.UtcNow;
        LastEditedTimestamp = copy.LastEditedTimestamp;
        LastEditor = copy.LastEditor;
        RequiresNitro = copy.RequiresNitro;
    }
    public Kit(ulong loadoutOwner, char loadout, Class @class, string? displayName, FactionInfo? faction)
    {
        Faction = faction;
        Id = loadoutOwner.ToString(Data.AdminLocale) + "_" + new string(loadout, 1);
        Class = @class;
        Branch = KitManager.GetDefaultBranch(@class);
        Type = KitType.Loadout;
        SquadLevel = SquadLevel.Member;
        SignText = displayName == null ? new TranslationList() : new TranslationList(displayName);
        Items = Array.Empty<IKitItem>();
        UnlockRequirements = Array.Empty<UnlockRequirement>();
        Skillsets = Array.Empty<Skillset>();
        FactionFilter = Array.Empty<PrimaryKey>();
        MapFilter = Array.Empty<PrimaryKey>();
        RequestSigns = Array.Empty<PrimaryKey>();
        Season = UCWarfare.Season;
        TeamLimit = KitManager.GetDefaultTeamLimit(@class);
        RequestCooldown = KitManager.GetDefaultRequestCooldown(@class);
        PremiumCost = UCWarfare.Config.LoadoutCost;
        CreatedTimestamp = LastEditedTimestamp = DateTime.UtcNow;
        /* DEFAULTS *
        FactionFilterIsWhitelist = false;
        MapFilterIsWhitelist = false;
        Disabled = false;
        CreditCost = 0;
        WeaponText = null;
        RequiresNitro = false;
        */
    }
    public Kit()
    {
        Items = Array.Empty<IKitItem>();
        UnlockRequirements = Array.Empty<UnlockRequirement>();
        Skillsets = Array.Empty<Skillset>();
        FactionFilter = Array.Empty<PrimaryKey>();
        MapFilter = Array.Empty<PrimaryKey>();
        RequestSigns = Array.Empty<PrimaryKey>();
    }
    public bool IsFactionAllowed(FactionInfo? faction)
    {
        if (faction == TeamManager.Team1Faction && Faction == TeamManager.Team2Faction ||
            faction == TeamManager.Team2Faction && Faction == TeamManager.Team1Faction)
            return false;
        if (FactionFilter.NullOrEmpty() || faction is null || !faction.PrimaryKey.IsValid)
            return true;
        int pk = faction.PrimaryKey.Key;
        for (int i = 0; i < FactionFilter.Length; ++i)
            if (FactionFilter[i].Key == pk)
                return FactionFilterIsWhitelist;

        return !FactionFilterIsWhitelist;
    }
    public bool IsCurrentMapAllowed()
    {
        if (MapFilter.NullOrEmpty())
            return true;
        PrimaryKey map = MapScheduler.Current;
        for (int i = 0; i < MapFilter.Length; ++i)
        {
            if (MapFilter[i].Key == map.Key)
                return MapFilterIsWhitelist;
        }

        return !MapFilterIsWhitelist;
    }
    public bool MeetsUnlockRequirements(UCPlayer player)
    {
        if (UnlockRequirements is not { Length: > 0 }) return false;
        for (int i = 0; i < UnlockRequirements.Length; ++i)
        {
            if (!UnlockRequirements[i].CanAccess(player))
                return false;
        }

        return true;
    }
    public string GetDisplayName(string language = L.Default)
    {
        if (SignText is null) return Id;
        if (SignText.TryGetValue(language, out string val))
            return val ?? Id;
        if (SignText.Count > 0)
            return SignText.FirstOrDefault().Value ?? Id;
        return Id;
    }
    public void Write(ByteWriter writer)
    {
        throw new NotImplementedException(); // todo
    }
    public void Read(ByteReader reader)
    {
        throw new NotImplementedException(); // todo
    }

    [FormatDisplay("Kit Id")]
    public const string IdFormat = "i";
    [FormatDisplay("Display Name")]
    public const string DisplayNameFormat = "d";
    [FormatDisplay("Class (" + nameof(Kits.Class) + ")")]
    public const string ClassFormat = "c";
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(IdFormat, StringComparison.Ordinal))
                return Id;
            if (format.Equals(ClassFormat, StringComparison.Ordinal))
                return Localization.TranslateEnum(Class, language);
        }

        return GetDisplayName(language);
    }

    public object Clone() => new Kit(Id, this);
}
[JsonConverter(typeof(SkillsetConverter))]
public readonly struct Skillset : IEquatable<Skillset>, ITranslationArgument
{
    public static readonly Skillset[] DefaultSkillsets =
    {
        new Skillset(EPlayerOffense.SHARPSHOOTER, 7),
        new Skillset(EPlayerOffense.PARKOUR, 2),
        new Skillset(EPlayerOffense.EXERCISE, 1),
        new Skillset(EPlayerOffense.CARDIO, 5),
        new Skillset(EPlayerDefense.VITALITY, 5),
    };

    public readonly EPlayerSpeciality Speciality;
    public readonly byte Level;
    public readonly byte SkillIndex;
    public EPlayerOffense Offense => (EPlayerOffense)SkillIndex;
    public EPlayerDefense Defense => (EPlayerDefense)SkillIndex;
    public EPlayerSupport Support => (EPlayerSupport)SkillIndex;
    public byte SpecialityIndex => (byte)Speciality;
    public Skillset(EPlayerOffense skill, byte level)
    {
        Speciality = EPlayerSpeciality.OFFENSE;
        SkillIndex = (byte)skill;
        Level = level;
    }
    public Skillset(EPlayerDefense skill, byte level)
    {
        Speciality = EPlayerSpeciality.DEFENSE;
        SkillIndex = (byte)skill;
        Level = level;
    }
    public Skillset(EPlayerSupport skill, byte level)
    {
        Speciality = EPlayerSpeciality.SUPPORT;
        SkillIndex = (byte)skill;
        Level = level;
    }
    internal Skillset(EPlayerSpeciality specialty, byte skill, byte level)
    {
        Speciality = specialty;
        SkillIndex = skill;
        Level = level;
    }
    public static Skillset Read(ByteReader reader)
    {
        EPlayerSpeciality speciality = (EPlayerSpeciality)reader.ReadUInt8();
        byte val = reader.ReadUInt8();
        byte level = reader.ReadUInt8();
        return speciality switch
        {
            EPlayerSpeciality.SUPPORT or EPlayerSpeciality.DEFENSE or EPlayerSpeciality.OFFENSE => new Skillset(speciality, val, level),
            _ => throw new Exception("Invalid value of specialty while reading skillset.")
        };
    }
    public static void Write(ByteWriter writer, Skillset skillset)
    {
        writer.Write((byte)skillset.Speciality);
        writer.Write(skillset.SkillIndex);
        writer.Write(skillset.Level);
    }
    public void ServerSet(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        player.Player.skills.ServerSetSkillLevel(SpecialityIndex, SkillIndex, Level);
    }
    /// <exception cref="FormatException"/>
    public static Skillset Read(MySqlDataReader reader, int colOffset = 0)
    {
        string type = reader.GetString(colOffset + 1);
        byte level = reader.GetByte(colOffset + 2);
        if (Enum.TryParse(type, true, out EPlayerOffense offense))
            return new Skillset(offense, level);
        if (Enum.TryParse(type, true, out EPlayerDefense defense))
            return new Skillset(defense, level);
        if (Enum.TryParse(type, true, out EPlayerSupport support))
            return new Skillset(support, level);
        throw new FormatException("Unable to find valid skill for skillset: \"" + type + "\" at level " + level + ".");
    }
    public static Skillset Read(ref Utf8JsonReader reader)
    {
        bool valFound = false;
        bool lvlFound = false;
        EPlayerSpeciality spec = default;
        EPlayerOffense offense = default;
        EPlayerDefense defense = default;
        EPlayerSupport support = default;
        byte level = 255;
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            string? property = reader.GetString();
            if (reader.Read() && property != null)
            {
                switch (property)
                {
                    case "offense":
                        spec = EPlayerSpeciality.OFFENSE;
                        string? value2 = reader.GetString();
                        if (value2 != null)
                        {
                            Enum.TryParse(value2, true, out offense);
                            valFound = true;
                        }
                        break;
                    case "defense":
                        spec = EPlayerSpeciality.DEFENSE;
                        string? value3 = reader.GetString();
                        if (value3 != null)
                        {
                            Enum.TryParse(value3, true, out defense);
                            valFound = true;
                        }
                        break;
                    case "support":
                        spec = EPlayerSpeciality.SUPPORT;
                        string? value4 = reader.GetString();
                        if (value4 != null)
                        {
                            Enum.TryParse(value4, true, out support);
                            valFound = true;
                        }
                        break;
                    case "level":
                        if (reader.TryGetByte(out level))
                        {
                            lvlFound = true;
                        }
                        break;
                }
            }
        }
        if (valFound && lvlFound)
        {
            switch (spec)
            {
                case EPlayerSpeciality.OFFENSE:
                    return new Skillset(offense, level);
                case EPlayerSpeciality.DEFENSE:
                    return new Skillset(defense, level);
                case EPlayerSpeciality.SUPPORT:
                    return new Skillset(support, level);
            }
        }
        L.Log("Error parsing skillset.");
        return default;
    }
    public static void Write(Utf8JsonWriter writer, in Skillset skillset)
    {
        switch (skillset.Speciality)
        {
            case EPlayerSpeciality.OFFENSE:
                writer.WriteString("offense", skillset.Offense.ToString());
                break;
            case EPlayerSpeciality.DEFENSE:
                writer.WriteString("defense", skillset.Defense.ToString());
                break;
            case EPlayerSpeciality.SUPPORT:
                writer.WriteString("support", skillset.Support.ToString());
                break;
        }
        writer.WriteNumber("level", skillset.Level);
    }
    public override bool Equals(object? obj) => obj is Skillset skillset && EqualsHelper(in skillset, true);
    private bool EqualsHelper(in Skillset skillset, bool compareLevel)
    {
        if (compareLevel && skillset.Level != Level) return false;
        return skillset.Speciality == Speciality && skillset.SkillIndex == SkillIndex;
    }
    public override string ToString()
    {
        return Speciality switch
        {
            EPlayerSpeciality.OFFENSE => "Offense: " + Offense,
            EPlayerSpeciality.DEFENSE => "Defense: " + Defense,
            EPlayerSpeciality.SUPPORT => "Support: " + Support,
            _ => "Invalid speciality #" + SkillIndex.ToString(Data.AdminLocale)
        } + " at level " + Level.ToString(Data.AdminLocale) + ".";
    }
    public override int GetHashCode()
    {
        int hashCode = 1232939970;
        hashCode *= -1521134295 + Speciality.GetHashCode();
        hashCode *= -1521134295 + Level.GetHashCode();
        hashCode *= -1521134295 + SkillIndex;
        return hashCode;
    }

    [FormatDisplay("No Level")]
    public const string FormatNoLevel = "nl";
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, CultureInfo? culture, ref TranslationFlags flags)
    {
        string b = Speciality switch
        {
            EPlayerSpeciality.DEFENSE => Localization.TranslateEnum(Defense, language),
            EPlayerSpeciality.OFFENSE => Localization.TranslateEnum(Offense, language),
            EPlayerSpeciality.SUPPORT => Localization.TranslateEnum(Support, language),
            _ => SpecialityIndex.ToString(culture) + "." + SkillIndex.ToString(culture)
        };
        if (format != null && format.Equals(FormatNoLevel, StringComparison.Ordinal))
            return b;
        return b + " Level " + Level.ToString(culture);
    }

    public bool Equals(Skillset other) => EqualsHelper(in other, true);
    public bool TypeEquals(in Skillset skillset) => EqualsHelper(in skillset, false);
    public static void SetDefaultSkills(UCPlayer player)
    {
        player.EnsureSkillsets(Array.Empty<Skillset>());
    }
    /// <returns>-1 if parse failure.</returns>
    public static int GetSkillsetFromEnglishName(string name, out EPlayerSpeciality speciality)
    {
        if (Enum.TryParse(name, true, out EPlayerOffense offense))
        {
            speciality = EPlayerSpeciality.OFFENSE;
            return (int)offense;
        }
        if (Enum.TryParse(name, true, out EPlayerDefense defense))
        {
            speciality = EPlayerSpeciality.DEFENSE;
            return (int)defense;
        }
        if (Enum.TryParse(name, true, out EPlayerSupport support))
        {
            speciality = EPlayerSpeciality.SUPPORT;
            return (int)support;
        }
        speciality = (EPlayerSpeciality)(-1);
        return -1;
    }
    public static bool operator ==(Skillset a, Skillset b) => a.EqualsHelper(in b, true);
    public static bool operator !=(Skillset a, Skillset b) => !a.EqualsHelper(in b, true);
    public const string COLUMN_PK = "pk";
    public const string COLUMN_SKILL = "Skill";
    public const string COLUMN_LEVEL = "Level";

    private static readonly string SkillEnumName = "enum('" + string.Join("','",
        typeof(EPlayerOffense).GetEnumNames().Concat(typeof(EPlayerDefense).GetEnumNames())
            .Concat(typeof(EPlayerSupport).GetEnumNames())) + "')";
    public static Schema GetDefaultSchema(string tableName, string fkColumn, string mainTable, string mainPkColumn, bool oneToOne = false, bool hasPk = false)
    {
        if (!oneToOne && fkColumn.Equals(COLUMN_PK, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Foreign key column may not be the same as \"" + COLUMN_PK + "\".", nameof(fkColumn));
        int ct = 3;
        if (!oneToOne && hasPk)
            ++ct;
        Schema.Column[] columns = new Schema.Column[ct];
        int index = 0;
        if (!oneToOne && hasPk)
        {
            columns[0] = new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            };
        }
        else index = -1;
        columns[++index] = new Schema.Column(fkColumn, SqlTypes.INCREMENT_KEY)
        {
            PrimaryKey = oneToOne,
            AutoIncrement = oneToOne,
            ForeignKey = true,
            ForeignKeyColumn = mainPkColumn,
            ForeignKeyTable = mainTable
        };
        columns[++index] = new Schema.Column(COLUMN_SKILL, SkillEnumName);
        columns[++index] = new Schema.Column(COLUMN_LEVEL, SqlTypes.BYTE);
        return new Schema(tableName, columns, false, typeof(Skillset));
    }
}

[JsonConverter(typeof(UnlockRequirementConverter))]
public abstract class UnlockRequirement : ICloneable
{
    private static readonly Dictionary<int, KeyValuePair<Type, string[]>> Types = new Dictionary<int, KeyValuePair<Type, string[]>>(4);
    private static bool _hasReflected;
    private static void Reflect()
    {
        Types.Clear();
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(typeof(UnlockRequirement).IsAssignableFrom))
        {
            if (Attribute.GetCustomAttribute(type, typeof(UnlockRequirementAttribute)) is UnlockRequirementAttribute att && !Types.ContainsKey(att.Type))
            {
                Types.Add(att.Type, new KeyValuePair<Type, string[]>(type, att.Properties));
            }
        }
        _hasReflected = true;
    }
    public abstract bool CanAccess(UCPlayer player);
    public static UnlockRequirement? Read(ref Utf8JsonReader reader)
    {
        if (!_hasReflected) Reflect();
        UnlockRequirement? t = null;
        while (reader.TokenType == JsonTokenType.PropertyName || (reader.Read() && reader.TokenType == JsonTokenType.PropertyName))
        {
            string? property = reader.GetString();
            if (reader.Read() && property != null)
            {
                if (t == null)
                {
                    foreach (KeyValuePair<int, KeyValuePair<Type, string[]>> propertyList in Types)
                    {
                        for (int i = 0; i < propertyList.Value.Value.Length; i++)
                        {
                            if (property.Equals(propertyList.Value.Value[i], StringComparison.OrdinalIgnoreCase))
                            {
                                t = Activator.CreateInstance(propertyList.Value.Key) as UnlockRequirement;
                                goto done;
                            }
                        }
                    }
                }
                else
                {
                    t.ReadProperty(ref reader, property);
                }
                continue;
            done:
                if (t != null)
                    t.ReadProperty(ref reader, property);
                else
                {
                    L.LogWarning("Failed to find property \"" + property + "\" when parsing unlock requirements.");
                }
            }
        }
        return t;
    }
    public static UnlockRequirement? Read(MySqlDataReader reader, int colOffset = 0)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(reader.GetString(colOffset + 1));
        Utf8JsonReader reader2 = new Utf8JsonReader(bytes, JsonEx.readerOptions);
        return Read(ref reader2);
    }
    public static void Write(Utf8JsonWriter writer, UnlockRequirement requirement)
    {
        requirement.WriteProperties(writer);
    }
    protected abstract void ReadProperty(ref Utf8JsonReader reader, string property);
    protected abstract void WriteProperties(Utf8JsonWriter writer);
    public abstract string GetSignText(UCPlayer player);
    public abstract object Clone();
    protected abstract void Read(ByteReader reader);
    protected abstract void Write(ByteWriter writer);
    public const string COLUMN_PK = "pk";
    public const string COLUMN_JSON = "JSON";
    public static Schema GetDefaultSchema(string tableName, string fkColumn, string mainTable, string mainPkColumn, bool oneToOne = false, bool hasPk = false)
    {
        if (!oneToOne && fkColumn.Equals(COLUMN_PK, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Foreign key column may not be the same as \"" + COLUMN_PK + "\".", nameof(fkColumn));
        int ct = 2;
        if (!oneToOne && hasPk)
            ++ct;
        Schema.Column[] columns = new Schema.Column[ct];
        int index = 0;
        if (!oneToOne && hasPk)
        {
            columns[0] = new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            };
        }
        else index = -1;
        columns[++index] = new Schema.Column(fkColumn, SqlTypes.INCREMENT_KEY)
        {
            PrimaryKey = oneToOne,
            AutoIncrement = oneToOne,
            ForeignKey = true,
            ForeignKeyColumn = mainPkColumn,
            ForeignKeyTable = mainTable
        };
        columns[++index] = new Schema.Column(COLUMN_JSON, SqlTypes.STRING_255);
        return new Schema(tableName, columns, false, typeof(UnlockRequirement));
    }
    public virtual Exception RequestKitFailureToMeet(CommandInteraction ctx, Kit kit)
    {
        L.LogWarning("Unhandled kit requirement type: " + GetType().Name);
        return ctx.SendUnknownError();
    }
    public virtual Exception RequestVehicleFailureToMeet(CommandInteraction ctx, VehicleData data)
    {
        L.LogWarning("Unhandled vehicle requirement type: " + GetType().Name);
        return ctx.SendUnknownError();
    }
    public virtual Exception RequestTraitFailureToMeet(CommandInteraction ctx, TraitData trait)
    {
        L.LogWarning("Unhandled trait requirement type: " + GetType().Name);
        return ctx.SendUnknownError();
    }
}
public class UnlockRequirementConverter : JsonConverter<UnlockRequirement>
{
    public override UnlockRequirement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => UnlockRequirement.Read(ref reader);
    public override void Write(Utf8JsonWriter writer, UnlockRequirement value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        UnlockRequirement.Write(writer, value);
        writer.WriteEndObject();
    }
}
[UnlockRequirement(1, "unlock_level")]
public class LevelUnlockRequirement : UnlockRequirement
{
    public int UnlockLevel = -1;
    public override bool CanAccess(UCPlayer player)
    {
        return player.Level.Level >= UnlockLevel;
    }
    public override string GetSignText(UCPlayer player)
    {
        if (UnlockLevel == 0)
            return string.Empty;

        int lvl = Points.GetLevel(player.CachedXP);
        return T.KitRequiredLevel.Translate(player, LevelData.GetRankAbbreviation(UnlockLevel), lvl >= UnlockLevel ? UCWarfare.GetColor("kit_level_available") : UCWarfare.GetColor("kit_level_unavailable"));
    }
    protected override void ReadProperty(ref Utf8JsonReader reader, string property)
    {
        if (property.Equals("unlock_level", StringComparison.OrdinalIgnoreCase))
        {
            reader.TryGetInt32(out UnlockLevel);
        }
    }
    protected override void WriteProperties(Utf8JsonWriter writer)
    {
        writer.WriteNumber("unlock_level", UnlockLevel);
    }
    public override object Clone() => new LevelUnlockRequirement { UnlockLevel = UnlockLevel };
    protected override void Read(ByteReader reader)
    {
        UnlockLevel = reader.ReadInt32();
    }
    protected override void Write(ByteWriter writer)
    {
        writer.Write(UnlockLevel);
    }

    public override Exception RequestKitFailureToMeet(CommandInteraction ctx, Kit kit)
    {
        LevelData data = new LevelData(Points.GetLevelXP(UnlockLevel));
        return ctx.Reply(T.RequestKitLowLevel, data);
    }
    public override Exception RequestVehicleFailureToMeet(CommandInteraction ctx, VehicleData data)
    {
        LevelData data2 = new LevelData(Points.GetLevelXP(UnlockLevel));
        return ctx.Reply(T.RequestVehicleMissingLevels, data2);
    }
    public override Exception RequestTraitFailureToMeet(CommandInteraction ctx, TraitData trait)
    {
        LevelData data = new LevelData(Points.GetLevelXP(UnlockLevel));
        return ctx.Reply(T.RequestTraitLowLevel, trait, data);
    }
}
[UnlockRequirement(2, "unlock_rank")]
public class RankUnlockRequirement : UnlockRequirement
{
    public int UnlockRank = -1;
    public override bool CanAccess(UCPlayer player)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(player, out bool success);
        return success && data.Order >= UnlockRank;
    }
    public override string GetSignText(UCPlayer player)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(player, out bool success);
        ref Ranks.RankData reqData = ref Ranks.RankManager.GetRank(UnlockRank, out _);
        return T.KitRequiredRank.Translate(player, reqData, success && data.Order >= reqData.Order ? UCWarfare.GetColor("kit_level_available") : UCWarfare.GetColor("kit_level_unavailable"));
    }
    protected override void ReadProperty(ref Utf8JsonReader reader, string property)
    {
        if (property.Equals("unlock_rank", StringComparison.OrdinalIgnoreCase))
        {
            reader.TryGetInt32(out UnlockRank);
        }
    }
    protected override void WriteProperties(Utf8JsonWriter writer)
    {
        writer.WriteNumber("unlock_rank", UnlockRank);
    }
    public override object Clone() => new RankUnlockRequirement { UnlockRank = UnlockRank };
    protected override void Read(ByteReader reader)
    {
        UnlockRank = reader.ReadInt32();
    }
    protected override void Write(ByteWriter writer)
    {
        writer.Write(UnlockRank);
    }

    public override Exception RequestKitFailureToMeet(CommandInteraction ctx, Kit kit)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(UnlockRank, out bool success);
        if (!success)
            L.LogWarning("Invalid rank order in kit requirement: " + (kit?.Id ?? string.Empty) + " :: " + UnlockRank + ".");
        return ctx.Reply(T.RequestKitLowRank, data);
    }
    public override Exception RequestVehicleFailureToMeet(CommandInteraction ctx, VehicleData data)
    {
        ref Ranks.RankData rankData = ref Ranks.RankManager.GetRank(UnlockRank, out bool success);
        if (!success)
            L.LogWarning("Invalid rank order in vehicle requirement: " + data.VehicleID + " :: " + UnlockRank + ".");
        return ctx.Reply(T.RequestVehicleRankIncomplete, rankData);
    }
    public override Exception RequestTraitFailureToMeet(CommandInteraction ctx, TraitData trait)
    {
        ref Ranks.RankData data = ref Ranks.RankManager.GetRank(UnlockRank, out bool success);
        if (!success)
            L.LogWarning("Invalid rank order in trait requirement: " + trait.TypeName + " :: " + UnlockRank + ".");
        return ctx.Reply(T.RequestTraitLowRank, trait, data);
    }
}
[UnlockRequirement(3, "unlock_presets", "quest_id")]
public class QuestUnlockRequirement : UnlockRequirement
{
    public Guid QuestID;
    public Guid[] UnlockPresets = Array.Empty<Guid>();
    public override bool CanAccess(UCPlayer player)
    {
        for (int i = 0; i < UnlockPresets.Length; i++)
        {
            if (!player.QuestComplete(UnlockPresets[i]))
                return false;
        }
        return true;
    }
    public override string GetSignText(UCPlayer player)
    {
        bool access = CanAccess(player);
        if (access)
            return T.KitRequiredQuestsComplete.Translate(player);
        if (Assets.find(QuestID) is QuestAsset quest)
            return T.KitRequiredQuest.Translate(player, quest, UCWarfare.GetColor("kit_level_unavailable"));

        return T.KitRequiredQuestsMultiple.Translate(player, UnlockPresets.Length, UCWarfare.GetColor("kit_level_unavailable"), UnlockPresets.Length.S());
    }
    protected override void ReadProperty(ref Utf8JsonReader reader, string property)
    {
        if (property.Equals("unlock_presets", StringComparison.OrdinalIgnoreCase))
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                List<Guid> ids = new List<Guid>(4);
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TryGetGuid(out Guid guid) && !ids.Contains(guid))
                        ids.Add(guid);
                }
                UnlockPresets = ids.ToArray();
            }
        }
        else if (property.Equals("quest_id", StringComparison.OrdinalIgnoreCase))
        {
            if (!reader.TryGetGuid(out QuestID))
                L.LogWarning("Failed to convert " + property + " with value \"" + (reader.GetString() ?? "null") + "\" to a GUID.");
        }
    }
    protected override void WriteProperties(Utf8JsonWriter writer)
    {
        writer.WritePropertyName("unlock_presets");
        writer.WriteStartArray();
        for (int i = 0; i < UnlockPresets.Length; i++)
        {
            writer.WriteStringValue(UnlockPresets[i]);
        }
        writer.WriteEndArray();

        writer.WriteString("quest_id", QuestID);
    }
    public override object Clone()
    {
        QuestUnlockRequirement req = new QuestUnlockRequirement
        {
            QuestID = QuestID,
            UnlockPresets = new Guid[UnlockPresets.Length]
        };
        Array.Copy(UnlockPresets, req.UnlockPresets, UnlockPresets.Length);
        return req;
    }
    protected override void Read(ByteReader reader)
    {
        QuestID = reader.ReadGuid();
        UnlockPresets = reader.ReadGuidArray();
    }
    protected override void Write(ByteWriter writer)
    {
        writer.Write(QuestID);
        writer.Write(UnlockPresets);
    }

    public override Exception RequestKitFailureToMeet(CommandInteraction ctx, Kit kit)
    {
        if (Assets.find(QuestID) is QuestAsset asset)
        {
            ctx.Caller.Player.quests.ServerAddQuest(asset);
            return ctx.Reply(T.RequestKitQuestIncomplete, asset);
        }
        return ctx.Reply(T.RequestKitQuestIncomplete, null!);
    }
    public override Exception RequestVehicleFailureToMeet(CommandInteraction ctx, VehicleData data)
    {
        if (Assets.find(QuestID) is QuestAsset asset)
        {
            ctx.Caller.Player.quests.ServerAddQuest(asset);
            return ctx.Reply(T.RequestVehicleQuestIncomplete, asset);
        }
        return ctx.Reply(T.RequestVehicleQuestIncomplete, null!);
    }
    public override Exception RequestTraitFailureToMeet(CommandInteraction ctx, TraitData trait)
    {
        if (Assets.find(QuestID) is QuestAsset asset)
        {
            ctx.Caller.Player.quests.ServerAddQuest(asset);
            return ctx.Reply(T.RequestTraitQuestIncomplete, trait, asset);
        }
        return ctx.Reply(T.RequestTraitQuestIncomplete, trait, null!);
    }
}
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class UnlockRequirementAttribute : Attribute
{
    public string[] Properties => _properties;
    public int Type => _type;
    /// <param name="properties">Must be unique among other unlock requirements.</param>
    public UnlockRequirementAttribute(int type, params string[] properties)
    {
        _properties = properties;
        _type = type;
    }
    private readonly string[] _properties;
    private readonly int _type;
}
public interface IClothingJar
{
    ClothingType Type { get; set; }
}
public interface IKitItem : ICloneable, IComparable
{
    public ItemAsset? GetItem(Kit kit, FactionInfo? targetTeam, out byte amount, out byte[] state);
}
public interface IItemJar
{
    byte X { get; set; }
    byte Y { get; set; }
    byte Rotation { get; set; }
    Page Page { get; set; }
}
public interface IAssetRedirect
{
    RedirectType RedirectType { get; set; }
}
public interface IBaseItem
{
    Guid Item { get; set; }
    [JsonConverter(typeof(Base64Converter))]
    byte[] State { get; set; }
}
public interface IItem : IBaseItem
{
    byte Amount { get; set; }
}
public class AssetRedirectItem : IItemJar, IAssetRedirect, IKitItem
{
    public RedirectType RedirectType { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public byte Rotation { get; set; }
    public Page Page { get; set; }
    public AssetRedirectItem() { }
    public AssetRedirectItem(RedirectType redirectType, byte x, byte y, byte rotation, Page page)
    {
        RedirectType = redirectType;
        X = x;
        Y = y;
        Rotation = rotation;
        Page = page;
    }
    public AssetRedirectItem(AssetRedirectItem copy)
    {
        RedirectType = copy.RedirectType;
        X = copy.X;
        Y = copy.Y;
        Rotation = copy.Rotation;
        Page = copy.Page;
    }
    public object Clone() => new AssetRedirectItem(this);
    public ItemAsset? GetItem(Kit kit, FactionInfo? targetTeam, out byte amount, out byte[] state) =>
        TeamManager.GetRedirectInfo(RedirectType, kit.Faction, targetTeam, out state, out amount);

    public int CompareTo(object obj)
    {
        if (obj is IKitItem kitItem)
        {
            if (kitItem is IItemJar jar)
            {
                if (jar is not IAssetRedirect r)
                    return -1;
                return Page != jar.Page ? Page.CompareTo(jar.Page) : RedirectType.CompareTo(r.RedirectType);
            }
            if (kitItem is IClothingJar)
            {
                return Page is Page.Primary or Page.Secondary ? -1 : 1;
            }
        }

        return -1;
    }
}
public class AssetRedirectClothing : IClothingJar, IAssetRedirect, IKitItem
{
    public RedirectType RedirectType { get; set; }
    public ClothingType Type { get; set; }
    public AssetRedirectClothing() { }
    public AssetRedirectClothing(RedirectType redirectType, ClothingType type)
    {
        RedirectType = redirectType;
        Type = type;
    }
    public AssetRedirectClothing(AssetRedirectClothing copy)
    {
        RedirectType = copy.RedirectType;
        Type = copy.Type;
    }
    public object Clone() => new AssetRedirectClothing(this);
    public ItemAsset? GetItem(Kit kit, FactionInfo? targetTeam, out byte amount, out byte[] state) =>
        TeamManager.GetRedirectInfo(RedirectType, kit.Faction, targetTeam, out state, out amount);
    public int CompareTo(object obj)
    {
        if (obj is IKitItem kitItem)
        {
            if (kitItem is IClothingJar cjar)
            {
                if (Type != cjar.Type)
                    return Type.CompareTo(cjar.Type);
            }
            if (kitItem is IItemJar jar)
            {
                return jar.Page is Page.Primary or Page.Secondary ? 1 : -1;
            }
        }

        return -1;
    }
}
public class PageItem : IItemJar, IItem, IKitItem
{
    private Guid _item;
#if DEBUG
    private bool _isLegacyRedirect;
    private RedirectType _legacyRedirect;
    public RedirectType? LegacyRedirect => _isLegacyRedirect ? _legacyRedirect : null;
#endif

    [JsonPropertyName("id")]
    public Guid Item
    {
        get => _item;
        set
        {
            _item = value;
#if DEBUG
#pragma warning disable CS0612
            TeamManager.GetLegacyRedirect(value, out _legacyRedirect);
#pragma warning restore CS0612
            if (_legacyRedirect == RedirectType.None)
                _legacyRedirect = TeamManager.GetRedirectInfo(value, out _, false);
            _isLegacyRedirect = _legacyRedirect != RedirectType.None;
#endif
        }
    }

    [JsonPropertyName("x")]
    public byte X { get; set; }

    [JsonPropertyName("y")]
    public byte Y { get; set; }

    [JsonPropertyName("rotation")]
    public byte Rotation { get; set; }

    [JsonPropertyName("page")]
    public Page Page { get; set; }

    [JsonPropertyName("amount")]
    public byte Amount { get; set; }

    [JsonPropertyName("metadata")]
    [JsonConverter(typeof(Base64Converter))]
    public byte[] State { get; set; }
    
    public PageItem(Guid item, byte x, byte y, byte rotation, byte[] state, byte amount, Page page)
    {
        this.Item = item;
        this.X = x;
        this.Y = y;
        this.Rotation = rotation;
        this.Page = page;
        this.Amount = amount;
        this.State = state;
    }
    public PageItem(PageItem copy)
    {
        Item = copy.Item;
        X = copy.X;
        Y = copy.Y;
        Rotation = copy.Rotation;
        Page = copy.Page;
        Amount = copy.Amount;
        State = copy.State;
    }
    public PageItem() { }
    public object Clone() => new PageItem(this);
    public int CompareTo(object obj)
    {
        if (obj is IKitItem kitItem)
        {
            if (kitItem is IItemJar jar)
            {
                if (jar is not IItem)
                    return 1;
                return Page != jar.Page ? Page.CompareTo(jar.Page) : jar.Y == Y ? X.CompareTo(jar.X) : Y.CompareTo(jar.Y);
            }
            if (kitItem is IClothingJar)
            {
                return Page is Page.Primary or Page.Secondary ? -1 : 1;
            }
        }

        return -1;
    }
    public const string COLUMN_PK = "pk";
    public const string COLUMN_GUID = "Item";
    public const string COLUMN_X = "X";
    public const string COLUMN_Y = "Y";
    public const string COLUMN_ROTATION = "Rotation";
    public const string COLUMN_PAGE = "Page";
    public const string COLUMN_AMOUNT = "Amount";
    public const string COLUMN_METADATA = "Metadata";
    public static Schema GetDefaultSchema(string tableName, string fkColumn, string mainTable, string mainPkColumn, bool guidString, bool includePage = true, bool oneToOne = false, bool hasPk = false)
    {
        if (!oneToOne && fkColumn.Equals(COLUMN_PK, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Foreign key column may not be the same as \"" + COLUMN_PK + "\".", nameof(fkColumn));
        int ct = 7;
        if (!oneToOne && hasPk)
            ++ct;
        if (includePage)
            ++ct;
        Schema.Column[] columns = new Schema.Column[ct];
        int index = 0;
        if (!oneToOne && hasPk)
        {
            columns[0] = new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            };
        }
        else index = -1;
        columns[++index] = new Schema.Column(fkColumn, SqlTypes.INCREMENT_KEY)
        {
            PrimaryKey = oneToOne,
            AutoIncrement = oneToOne,
            ForeignKey = true,
            ForeignKeyColumn = mainPkColumn,
            ForeignKeyTable = mainTable
        };
        columns[++index] = new Schema.Column(COLUMN_GUID, guidString ? SqlTypes.GUID_STRING : SqlTypes.GUID);
        columns[++index] = new Schema.Column(COLUMN_X, SqlTypes.BYTE);
        columns[++index] = new Schema.Column(COLUMN_Y, SqlTypes.BYTE);
        columns[++index] = new Schema.Column(COLUMN_ROTATION, SqlTypes.BYTE);
        if (includePage)
            columns[++index] = new Schema.Column(COLUMN_PAGE, SqlTypes.BYTE);
        columns[++index] = new Schema.Column(COLUMN_AMOUNT, SqlTypes.BYTE);
        columns[++index] = new Schema.Column(COLUMN_METADATA, SqlTypes.BYTES_255);
        return new Schema(tableName, columns, false, typeof(PageItem));
    }

    public ItemAsset? GetItem(Kit kit, FactionInfo? targetTeam, out byte amount, out byte[] state)
    {
#if DEBUG
        if (_isLegacyRedirect)
            return TeamManager.GetRedirectInfo(_legacyRedirect, kit.Faction, targetTeam, out state, out amount);
#endif
        if (Assets.find(Item) is ItemAsset item)
        {
            amount = Amount < 1 ? item.amount : Amount;
            state = State is null ? item.getState(EItemOrigin.ADMIN) : Util.CloneBytes(State);
            return item;
        }

        state = Array.Empty<byte>();
        amount = default;
        return null;
    }
}
public class ClothingItem : IClothingJar, IBaseItem, IKitItem
{
    private Guid _item;
#if DEBUG
    private bool _isLegacyRedirect;
    private RedirectType _legacyRedirect;
    public RedirectType? LegacyRedirect => _isLegacyRedirect ? _legacyRedirect : null;
#endif

    [JsonPropertyName("id")]
    public Guid Item
    {
        get => _item;
        set
        {
            _item = value;
#if DEBUG
#pragma warning disable CS0612
            TeamManager.GetLegacyRedirect(value, out _legacyRedirect);
#pragma warning restore CS0612
            if (_legacyRedirect == RedirectType.None)
                _legacyRedirect = TeamManager.GetRedirectInfo(value, out _, true);
            _isLegacyRedirect = _legacyRedirect != RedirectType.None;
#endif
        }
    }

    [JsonPropertyName("type")]
    public ClothingType Type { get; set; }

    [JsonPropertyName("metadata")]
    [JsonConverter(typeof(Base64Converter))]
    public byte[] State { get; set; }
    
    public ClothingItem(Guid id, ClothingType type, byte[] state)
    {
        this.Item = id;
        this.Type = type;
        this.State = state ?? Array.Empty<byte>();
    }

    public ClothingItem(ClothingItem copy)
    {
        Item = copy.Item;
        Type = copy.Type;
        State = copy.State;
    }

    public ClothingItem() { }

    public object Clone() => new ClothingItem(this);
    public ItemAsset? GetItem(Kit kit, FactionInfo? targetTeam, out byte amount, out byte[] state)
    {
        amount = 1;
#if DEBUG
        if (_isLegacyRedirect)
            return TeamManager.GetRedirectInfo(_legacyRedirect, kit.Faction, targetTeam, out state, out amount);
#endif
        if (Assets.find(Item) is ItemAsset item)
        {
            state = State.NullOrEmpty() ? item.getState(EItemOrigin.ADMIN) : Util.CloneBytes(State);
            return item;
        }

        state = Array.Empty<byte>();
        return null;
    }
    public int CompareTo(object obj)
    {
        if (obj is IKitItem kitItem)
        {
            if (kitItem is IClothingJar cjar)
            {
                if (Type != cjar.Type)
                    return Type.CompareTo(cjar.Type);
                return 0;
            }
            if (kitItem is IItemJar jar)
            {
                return jar.Page is Page.Primary or Page.Secondary ? 1 : -1;
            }
        }

        return -1;
    }
}

/// <summary>Max field character limit: <see cref="KitEx.SquadLevelMaxCharLimit"/>.</summary>
[Translatable("Squad Level")]
public enum SquadLevel : byte
{
    [Translatable("Member")]
    Member = 0,
    [Translatable("Commander")]
    Commander = 4
}

/// <summary>Max field character limit: <see cref="KitEx.BranchMaxCharLimit"/>.</summary>
[Translatable("Branch")]
public enum Branch : byte
{
    Default,
    Infantry,
    Armor,
    [Translatable("Air Force")]
    Airforce,
    [Translatable("Special Ops")]
    SpecOps,
    Navy
}

/// <summary>Max field character limit: <see cref="KitEx.ClothingMaxCharLimit"/>.</summary>
public enum ClothingType : byte
{
    Shirt,
    Pants,
    Vest,
    Hat,
    Mask,
    Backpack,
    Glasses
}

/// <summary>Max field character limit: <see cref="KitEx.TypeMaxCharLimit"/>.</summary>
[Translatable("Kit Type")]
public enum KitType : byte
{
    Public,
    Elite,
    Special,
    Loadout
}

[Translatable]
/// <summary>Max field character limit: <see cref="KitEx.RedirectTypeCharLimit"/>.</summary>
public enum RedirectType : byte
{
    None = 255,
    Shirt = 0,
    Pants,
    Vest,
    Hat,
    Mask,
    Backpack,
    Glasses,
    [Translatable("Ammo Supplies")]
    AmmoSupply,
    [Translatable("Building Supplies")]
    BuildSupply,
    [Translatable("Rally Point")]
    RallyPoint,
    [Translatable("FOB Radio")]
    Radio,
    ZoneBlocker,
    [Translatable("Ammo Bag")]
    AmmoBag,
    [Translatable("Ammo Crate")]
    AmmoCrate,
    [Translatable("Repair Station")]
    RepairStation,
    [Translatable("FOB Bunker")]
    Bunker,
    VehicleBay,
    [Translatable("Entrenching Tool")]
    EntrenchingTool,
    UAV,
    [Translatable("Built Repair Station")]
    RepairStationBuilt,
    [Translatable("Built Ammo Crate")]
    AmmoCrateBuilt,
    [Translatable("Built FOB Bunker")]
    BunkerBuilt,
    Cache,
    RadioDamaged,
    [Translatable("Laser Designator")]
    LaserDesignator,
    StandardAmmoIcon,
    StandardMeleeIcon,
    StandardGrenadeIcon,
    StandardSmokeGrenadeIcon,
}
public enum Page : byte
{
    Primary = 0,
    Secondary = 1,
    Hands = 2,
    Backpack = 3,
    Vest = 4,
    Shirt = 5,
    Pants = 6,
    Storage = 7,
    Area = 8
}
/// <summary>Max field character limit: <see cref="KitEx.ClassMaxCharLimit"/>.</summary>
[JsonConverter(typeof(ClassConverter))]
[Translatable("Kit Class")]
public enum Class : byte
{
    None = 0,
    [Translatable(LanguageAliasSet.RUSSIAN, "Безоружный")]
    [Translatable(LanguageAliasSet.SPANISH, "Desarmado")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Neinarmat")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Desarmado")]
    [Translatable(LanguageAliasSet.POLISH, "Nieuzbrojony")]
    Unarmed = 1,
    [Translatable("Squad Leader")]
    [Translatable(LanguageAliasSet.RUSSIAN, "Лидер отряда")]
    [Translatable(LanguageAliasSet.SPANISH, "Líder De Escuadrón")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Lider de Echipa")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Líder de Esquadrão")]
    [Translatable(LanguageAliasSet.POLISH, "Dowódca Oddziału")]
    Squadleader = 2,
    [Translatable(LanguageAliasSet.RUSSIAN, "Стрелок")]
    [Translatable(LanguageAliasSet.SPANISH, "Fusilero")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Puscas")]
    [Translatable(LanguageAliasSet.POLISH, "Strzelec")]
    Rifleman = 3,
    [Translatable(LanguageAliasSet.RUSSIAN, "Медик")]
    [Translatable(LanguageAliasSet.SPANISH, "Médico")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Medic")]
    [Translatable(LanguageAliasSet.POLISH, "Medyk")]
    Medic = 4,
    [Translatable(LanguageAliasSet.RUSSIAN, "Нарушитель")]
    [Translatable(LanguageAliasSet.SPANISH, "Brechador")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Breacher")]
    [Translatable(LanguageAliasSet.POLISH, "Wyłamywacz")]
    Breacher = 5,
    [Translatable(LanguageAliasSet.RUSSIAN, "Солдат с автоматом")]
    [Translatable(LanguageAliasSet.SPANISH, "Fusilero Automático")]
    [Translatable(LanguageAliasSet.SPANISH, "Puscas Automat")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Fuzileiro Automobilístico")]
    [Translatable(LanguageAliasSet.POLISH, "Strzelec Automatyczny")]
    [Translatable(LanguageAliasSet.ENGLISH, "Automatic Rifleman")]
    AutomaticRifleman = 6,
    [Translatable(LanguageAliasSet.RUSSIAN, "Гренадёр")]
    [Translatable(LanguageAliasSet.SPANISH, "Granadero")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Grenadier")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Granadeiro")]
    [Translatable(LanguageAliasSet.POLISH, "Grenadier")]
    Grenadier = 7,
    [Translatable(LanguageAliasSet.ROMANIAN, "Mitralior")]
    [Translatable(LanguageAliasSet.ENGLISH, "Machine Gunner")]
    MachineGunner = 8,
    [Translatable("LAT")]
    [Translatable(LanguageAliasSet.RUSSIAN, "Лёгкий противотанк")]
    [Translatable(LanguageAliasSet.SPANISH, "Anti-Tanque Ligero")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Anti-Tanc Usor")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Anti-Tanque Leve")]
    [Translatable(LanguageAliasSet.POLISH, "Lekka Piechota Przeciwpancerna")]
    LAT = 9,
    [Translatable("HAT")]
    HAT = 10,
    [Translatable(LanguageAliasSet.RUSSIAN, "Марксман")]
    [Translatable(LanguageAliasSet.SPANISH, "Tirador Designado")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Lunetist-Usor")]
    [Translatable(LanguageAliasSet.POLISH, "Zwiadowca")]
    Marksman = 11,
    [Translatable(LanguageAliasSet.RUSSIAN, "Снайпер")]
    [Translatable(LanguageAliasSet.SPANISH, "Francotirador")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Lunetist")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Franco-Atirador")]
    [Translatable(LanguageAliasSet.POLISH, "Snajper")]
    Sniper = 12,
    [Translatable("Anti-personnel Rifleman")]
    [Translatable(LanguageAliasSet.RUSSIAN, "Противопехотный")]
    [Translatable(LanguageAliasSet.SPANISH, "Fusilero Anti-Personal")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Puscas Anti-Personal")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Antipessoal")]
    [Translatable(LanguageAliasSet.POLISH, "Strzelec Przeciw-Piechotny")]
    APRifleman = 13,
    [Translatable(LanguageAliasSet.RUSSIAN, "Инженер")]
    [Translatable(LanguageAliasSet.SPANISH, "Ingeniero")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Inginer")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Engenheiro")]
    [Translatable(LanguageAliasSet.POLISH, "Inżynier")]
    [Translatable(LanguageAliasSet.ENGLISH, "Combat Engineer")]
    CombatEngineer = 14,
    [Translatable(LanguageAliasSet.RUSSIAN, "Механик-водитель")]
    [Translatable(LanguageAliasSet.SPANISH, "Tripulante")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Echipaj")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Tripulante")]
    [Translatable(LanguageAliasSet.POLISH, "Załogant")]
    Crewman = 15,
    [Translatable(LanguageAliasSet.RUSSIAN, "Пилот")]
    [Translatable(LanguageAliasSet.SPANISH, "Piloto")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Pilot")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Piloto")]
    [Translatable(LanguageAliasSet.POLISH, "Pilot")]
    Pilot = 16,
    [Translatable("Special Ops")]
    [Translatable(LanguageAliasSet.SPANISH, "Op. Esp.")]
    [Translatable(LanguageAliasSet.ROMANIAN, "Trupe Speciale")]
    [Translatable(LanguageAliasSet.PORTUGUESE, "Op. Esp.")]
    [Translatable(LanguageAliasSet.POLISH, "Specjalista")]
    SpecOps = 17,
    // increment ClassConverter.MaxClass if adding another field!
}
public sealed class ClassConverter : JsonConverter<Class>
{
    internal const Class MaxClass = Class.SpecOps;
    public override Class Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return Class.None;
            case JsonTokenType.Number:
                if (reader.TryGetByte(out byte b))
                    return (Class)b;
                throw new JsonException("Invalid Class value.");
            case JsonTokenType.String:
                string val = reader.GetString()!;
                if (KitEx.TryParseClass(val, out Class @class))
                    return @class;
                throw new JsonException("Invalid Class value.");
            default:
                throw new JsonException("Invalid token for Class parameter.");
        }
    }
    public override void Write(Utf8JsonWriter writer, Class value, JsonSerializerOptions options)
    {
        if (value >= Class.None && value <= MaxClass)
            writer.WriteStringValue(value.ToString());
        else
            writer.WriteNumberValue((byte)value);
    }
}
public sealed class SkillsetConverter : JsonConverter<Skillset>
{
    public override Skillset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Skillset.Read(ref reader);
    public override void Write(Utf8JsonWriter writer, Skillset value, JsonSerializerOptions options) => Skillset.Write(writer, in value);
}