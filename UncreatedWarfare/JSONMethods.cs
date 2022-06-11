﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using UnityEngine;

namespace Uncreated.Warfare;

public readonly struct TranslationData
{
    public static readonly TranslationData Nil = new TranslationData("default", "<color=#ffffff>default</color>", true, Color.white);
    public readonly string Message;
    public readonly string Original;
    public readonly Color Color;
    public readonly bool UseColor;
    public TranslationData(string message, string original, bool useColor, Color color)
    {
        this.Message = message;
        this.Original = original;
        this.UseColor = useColor;
        this.Color = color;
    }
    public TranslationData(string Original)
    {
        this.Original = Original;
        this.Color = GetColorFromMessage(Original, out Message, out UseColor);
    }
    public static TranslationData GetPlaceholder(string key) => new TranslationData(key, key, false, Color.white);
    public static Color GetColorFromMessage(string Original, out string InnerText, out bool found)
    {
        if (Original.Length < 23)
        {
            InnerText = Original;
            found = false;
            return UCWarfare.GetColor("default");
        }
        if (Original.StartsWith("<color=#") && Original[8] != '{' && Original.EndsWith("</color>"))
        {
            IEnumerator<char> characters = Original.Skip(8).GetEnumerator();
            int start = 8;
            int length = 0;
            while (characters.MoveNext())
            {
                if (characters.Current == '>') break; // keep moving until the ending > is found.
                length++;
            }
            characters.Dispose();
            int msgStart = start + length + 1;
            InnerText = Original.Substring(msgStart, Original.Length - msgStart - 8);
            found = true;
            return Original.Substring(start, length).Hex();
        }
        else
        {
            InnerText = Original;
            found = false;
            return UCWarfare.GetColor("default");
        }
    }
    public override readonly string ToString() =>
        $"Original: {Original}, Inner text: {Message}, {(UseColor ? $"Color: {Color} ({ColorUtility.ToHtmlStringRGBA(Color)}." : "Unable to find color.")}";
}
public struct Point3D
{
    public string name;
    public float x;
    public float y;
    public float z;
    [JsonIgnore]
    public readonly Vector3 Vector3 { get => new Vector3(x, y, z); }
    [JsonConstructor]
    public Point3D(string name, float x, float y, float z)
    {
        this.name = name;
        this.x = x;
        this.y = y;
        this.z = z;
    }
}
public struct SerializableVector3 : IJsonReadWrite
{
    public static readonly SerializableVector3 Zero = new SerializableVector3(0, 0, 0);
    public float x;
    public float y;
    public float z;
    [JsonIgnore]
    public Vector3 Vector3
    {
        readonly get => new Vector3(x, y, z);
        set
        {
            x = value.x; y = value.y; z = value.z;
        }
    }
    public static bool operator ==(SerializableVector3 a, SerializableVector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
    public static bool operator ==(SerializableVector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
    public static bool operator !=(SerializableVector3 a, SerializableVector3 b) => a.x != b.x || a.y != b.y || a.z != b.z;
    public static bool operator !=(SerializableVector3 a, Vector3 b) => a.x != b.x || a.y != b.y || a.z != b.z;
    public override readonly bool Equals(object obj)
    {
        if (obj == default) return false;
        if (obj is SerializableVector3 v3)
            return x == v3.x && y == v3.y && z == v3.z;
        else if (obj is Vector3 uv3)
            return x == uv3.x && y == uv3.y && z == uv3.z;
        else return false;
    }
    public override readonly int GetHashCode()
    {
        int hashCode = 373119288;
        hashCode = hashCode * -1521134295 + x.GetHashCode();
        hashCode = hashCode * -1521134295 + y.GetHashCode();
        hashCode = hashCode * -1521134295 + z.GetHashCode();
        return hashCode;
    }
    public override readonly string ToString() => $"({Mathf.RoundToInt(x).ToString(Data.Locale)}, {Mathf.RoundToInt(y).ToString(Data.Locale)}, {Mathf.RoundToInt(z).ToString(Data.Locale)})";
    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }
    [JsonConstructor]
    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public readonly void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteProperty(nameof(x), this.x);
        writer.WriteProperty(nameof(y), this.y);
        writer.WriteProperty(nameof(z), this.z);
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string val = reader.GetString()!;
                if (val != null && reader.Read())
                {
                    switch (val)
                    {
                        case nameof(x):
                            x = (float)reader.GetDouble();
                            break;
                        case nameof(y):
                            y = (float)reader.GetDouble();
                            break;
                        case nameof(z):
                            z = (float)reader.GetDouble();
                            break;
                    }
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject) return;
        }
    }
}
public struct SerializableTransform : IJsonReadWrite
{
    public static readonly SerializableTransform Zero = new SerializableTransform(SerializableVector3.Zero, SerializableVector3.Zero);
    public SerializableVector3 position;
    public SerializableVector3 euler_angles;
    [JsonIgnore]
    public readonly Quaternion Rotation { get => Quaternion.Euler(euler_angles.Vector3); }
    [JsonIgnore]
    public readonly Vector3 Position { get => position.Vector3; }
    public static bool operator ==(SerializableTransform a, SerializableTransform b) => a.position == b.position && a.euler_angles == b.euler_angles;
    public static bool operator !=(SerializableTransform a, SerializableTransform b) => a.position != b.position || a.euler_angles != b.euler_angles;
    public static bool operator ==(SerializableTransform a, Transform b) => a.position == b.position && a.euler_angles == b.rotation.eulerAngles;
    public static bool operator !=(SerializableTransform a, Transform b) => a.position != b.position || a.euler_angles != b.rotation.eulerAngles;
    public override readonly bool Equals(object obj)
    {
        if (obj == default) return false;
        if (obj is SerializableTransform t)
            return position == t.position && euler_angles == t.euler_angles;
        else if (obj is Transform ut)
            return position == ut.position && euler_angles == ut.eulerAngles;
        else return false;
    }
    public override readonly string ToString() => position.ToString();
    public override readonly int GetHashCode()
    {
        int hashCode = -1079335343;
        hashCode = hashCode * -1521134295 + position.GetHashCode();
        hashCode = hashCode * -1521134295 + euler_angles.GetHashCode();
        return hashCode;
    }
    [JsonConstructor]
    public SerializableTransform(SerializableVector3 position, SerializableVector3 euler_angles)
    {
        this.position = position;
        this.euler_angles = euler_angles;
    }
    public SerializableTransform(Transform transform)
    {
        this.position = new SerializableVector3(transform.position);
        this.euler_angles = new SerializableVector3(transform.rotation.eulerAngles);
    }
    public SerializableTransform(Vector3 position, Vector3 eulerAngles)
    {
        this.position = new SerializableVector3(position);
        this.euler_angles = new SerializableVector3(eulerAngles);
    }
    public SerializableTransform(float posx, float posy, float posz, float rotx, float roty, float rotz)
    {
        this.position = new SerializableVector3(posx, posy, posz);
        this.euler_angles = new SerializableVector3(rotx, roty, rotz);
    }
    public SerializableTransform(Vector3 position, Quaternion rotation)
    {
        this.position = new SerializableVector3(position);
        this.euler_angles = new SerializableVector3(rotation.eulerAngles);
    }
    public readonly void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteProperty(nameof(position), position);
        writer.WriteProperty(nameof(euler_angles), euler_angles);
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string val = reader.GetString()!;
                if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                {
                    switch (val)
                    {
                        case nameof(position):
                            position = new SerializableVector3();
                            position.ReadJson(ref reader);
                            break;
                        case nameof(euler_angles):
                            euler_angles = new SerializableVector3();
                            euler_angles.ReadJson(ref reader);
                            break;
                    }
                }
                else if (reader.TokenType == JsonTokenType.EndObject) return;
            }
            else if (reader.TokenType == JsonTokenType.EndObject) return;
        }
    }
}
public struct LanguageAliasSet : IJsonReadWrite
{
    public string key;
    public string display_name;
    public string[] values;
    [JsonConstructor]
    public LanguageAliasSet(string key, string display_name, string[] values)
    {
        this.key = key;
        this.display_name = display_name;
        this.values = values;
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;
            else if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString()!;
                if (!reader.Read()) continue;
                if (prop == nameof(key))
                    this.key = reader.GetString()!;
                else if (prop == nameof(display_name))
                    this.display_name = reader.GetString()!;
                else if (prop == nameof(values) && reader.TokenType == JsonTokenType.StartArray)
                {
                    List<string> tlist = new List<string>(24);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            tlist.Add(reader.GetString()!);
                        }
                    }
                    this.values = tlist.ToArray();
                }
            }
        }
    }
    public readonly void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteProperty(nameof(key), key);
        writer.WriteProperty(nameof(display_name), display_name);
        writer.WritePropertyName(nameof(values));
        writer.WriteStartArray();
        for (int i = 0; i < values.Length; i++)
        {
            writer.WriteStringValue(values[i]);
        }
        writer.WriteEndArray();
    }
}
public static partial class JSONMethods
{
    public const string DEFAULT_LANGUAGE = "en-us";

    public static Dictionary<string, Color> LoadColors(out Dictionary<string, string> HexValues)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        F.CheckDir(Data.DATA_DIRECTORY, out bool fileExists);
        string chatColors = Path.Combine(Data.DATA_DIRECTORY, "chat_colors.json");
        if (fileExists)
        {
            if (!File.Exists(chatColors))
            {
                Dictionary<string, Color> defaultColors2 = new Dictionary<string, Color>(DefaultColors.Count);
                using (FileStream stream = new FileStream(chatColors, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                    writer.WriteStartObject();
                    foreach (KeyValuePair<string, string> color in DefaultColors)
                    {
                        writer.WritePropertyName(color.Key);
                        writer.WriteStringValue(color.Value);
                        defaultColors2.Add(color.Key, color.Value.Hex());
                    }
                    writer.WriteEndObject();
                    writer.Dispose();
                    stream.Close();
                    stream.Dispose();
                }
                HexValues = DefaultColors;
                return defaultColors2;
            }
            using (FileStream stream = new FileStream(chatColors, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long len = stream.Length;
                if (len > int.MaxValue)
                {
                    L.LogError("chat_colors.json is too long to read.");
                    goto def;
                }
                else
                {
                    Dictionary<string, Color> converted = new Dictionary<string, Color>(DefaultColors.Count);
                    Dictionary<string, string> read = new Dictionary<string, string>(DefaultColors.Count);
                    byte[] bytes = new byte[len];
                    stream.Read(bytes, 0, (int)len);
                    try
                    {
                        Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.StartObject) continue;
                            else if (reader.TokenType == JsonTokenType.EndObject) break;
                            else if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                string key = reader.GetString()!;
                                if (reader.Read() && reader.TokenType == JsonTokenType.String)
                                {
                                    string color = reader.GetString()!;
                                    string value = reader.GetString()!;
                                    if (read.ContainsKey(key))
                                        L.LogWarning("Duplicate color key \"" + key + "\" in chat_colors.json");
                                    else
                                    {
                                        read.Add(key, color);
                                        converted.Add(key, color.Hex());
                                    }
                                }
                            }
                        }
                        HexValues = read;
                        return converted;
                    }
                    catch (Exception e)
                    {
                        L.LogError("Failed to read chat_colors.json");
                        L.LogError(e);
                        goto def;
                    }
                }
            }
        }
        else
        {
            L.LogError("Failed to create chat_colors.json, read above.");
            goto def;
        }

        def:
        Dictionary<string, Color> NewDefaults = new Dictionary<string, Color>(DefaultColors.Count);
        foreach (KeyValuePair<string, string> color in DefaultColors)
        {
            NewDefaults.Add(color.Key, color.Value.Hex());
        }
        HexValues = DefaultColors;
        return NewDefaults;
    }
    public static Dictionary<string, Dictionary<string, TranslationData>> LoadTranslations()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string[] langDirs = Directory.GetDirectories(Data.LangStorage, "*", SearchOption.TopDirectoryOnly);
        Dictionary<string, Dictionary<string, TranslationData>> languages = new Dictionary<string, Dictionary<string, TranslationData>>();
        string defLang = Path.Combine(Data.LangStorage, DEFAULT_LANGUAGE);
        F.CheckDir(defLang, out bool folderIsThere);
        if (folderIsThere)
        {
            string loc = Path.Combine(defLang, "localization.json");
            if (!File.Exists(loc))
            {
                Dictionary<string, TranslationData> defaultLocal = new Dictionary<string, TranslationData>(DefaultTranslations.Count);
                using (FileStream stream = new FileStream(loc, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                    writer.WriteStartObject();
                    foreach (KeyValuePair<string, string> translation in DefaultTranslations)
                    {
                        writer.WritePropertyName(translation.Key);
                        writer.WriteStringValue(translation.Value);
                        defaultLocal.Add(translation.Key, new TranslationData(translation.Value));
                    }
                    writer.WriteEndObject();
                    writer.Dispose();
                    stream.Close();
                    stream.Dispose();
                }

                languages.Add(DEFAULT_LANGUAGE, defaultLocal);
            }
            foreach (string folder in langDirs)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(folder);
                string lang = directoryInfo.Name;
                FileInfo[] langFiles = directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly);
                foreach (FileInfo info in langFiles)
                {
                    if (info.Name == "localization.json")
                    {
                        if (languages.ContainsKey(lang)) continue;
                        using (FileStream stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            long len = stream.Length;
                            if (len > int.MaxValue)
                            {
                                L.LogError(info.FullName + " is too long to read.");
                                if (lang == DEFAULT_LANGUAGE && !languages.ContainsKey(DEFAULT_LANGUAGE))
                                {
                                    Dictionary<string, TranslationData> defaultLocal = new Dictionary<string, TranslationData>(DefaultTranslations.Count);
                                    foreach (KeyValuePair<string, string> translation in DefaultTranslations)
                                    {
                                        defaultLocal.Add(translation.Key, new TranslationData(translation.Value));
                                    }
                                    languages.Add(DEFAULT_LANGUAGE, defaultLocal);
                                }
                            }
                            else
                            {
                                Dictionary<string, TranslationData> local = new Dictionary<string, TranslationData>(DefaultTranslations.Count);
                                byte[] bytes = new byte[len];
                                stream.Read(bytes, 0, (int)len);
                                try
                                {
                                    Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                                    while (reader.Read())
                                    {
                                        if (reader.TokenType == JsonTokenType.StartObject) continue;
                                        else if (reader.TokenType == JsonTokenType.EndObject) break;
                                        else if (reader.TokenType == JsonTokenType.PropertyName)
                                        {
                                            string key = reader.GetString()!;
                                            if (reader.Read() && reader.TokenType == JsonTokenType.String)
                                            {
                                                string value = reader.GetString()!;
                                                if (local.ContainsKey(key))
                                                    L.LogWarning("Duplicate key \"" + key + "\" in localization file for " + lang);
                                                else
                                                    local.Add(key, new TranslationData(value));
                                            }
                                        }
                                    }
                                    languages.Add(lang, local);
                                }
                                catch (Exception e)
                                {
                                    L.LogError("Failed to read " + lang + " translations.");
                                    L.LogError(e);
                                }
                            }
                        }
                    }
                }
            }
            L.Log($"Loaded {languages.Count} languages, " + DEFAULT_LANGUAGE + $" having {(languages.TryGetValue(DEFAULT_LANGUAGE, out Dictionary<string, TranslationData> d) ? d.Count.ToString(Data.Locale) : "0")} translations.");
        }
        else
        {
            L.LogError("Failed to load translations, see above.");
            Dictionary<string, TranslationData> rtn = new Dictionary<string, TranslationData>(DefaultTranslations.Count);
            foreach (KeyValuePair<string, string> kvp in DefaultTranslations)
                rtn.Add(kvp.Key, new TranslationData(kvp.Value));
            if (!languages.ContainsKey(DEFAULT_LANGUAGE))
                languages.Add(DEFAULT_LANGUAGE, rtn);
            return languages;
        }
        return languages;
    }
    public static Dictionary<string, Vector3> LoadExtraPoints()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        F.CheckDir(Data.FlagStorage, out bool dirExists);
        if (dirExists)
        {
            string xtraPts = Path.Combine(Data.FlagStorage, "extra_points.json");
            if (!File.Exists(xtraPts))
            {
                Dictionary<string, Vector3> defaultXtraPoints2 = new Dictionary<string, Vector3>(DefaultExtraPoints.Count);
                using (FileStream stream = new FileStream(xtraPts, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                    writer.WriteStartObject();
                    for (int i = 0; i < DefaultExtraPoints.Count; i++)
                    {
                        Point3D point = DefaultExtraPoints[i];
                        writer.WritePropertyName(point.name);
                        writer.WriteStartObject();
                        writer.WriteProperty("x", point.x);
                        writer.WriteProperty("y", point.y);
                        writer.WriteProperty("z", point.z);
                        writer.WriteEndObject();
                        defaultXtraPoints2.Add(point.name, point.Vector3);
                    }
                    writer.WriteEndObject();
                    writer.Dispose();
                    stream.Close();
                    stream.Dispose();
                }
                return defaultXtraPoints2;
            }
            using (FileStream stream = new FileStream(xtraPts, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long len = stream.Length;
                if (len > int.MaxValue)
                {
                    L.LogError("extra_points.json is too long to read.");
                    goto def;
                }
                else
                {
                    Dictionary<string, Vector3> xtraPoints = new Dictionary<string, Vector3>(DefaultExtraPoints.Count);
                    byte[] bytes = new byte[len];
                    stream.Read(bytes, 0, (int)len);
                    try
                    {
                        Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.StartObject) continue;
                            else if (reader.TokenType == JsonTokenType.EndObject) break;
                            else if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                string key = reader.GetString()!;
                                if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                                {
                                    float x = 0f;
                                    float y = 0f;
                                    float z = 0f;
                                    while (reader.Read())
                                    {
                                        if (reader.TokenType == JsonTokenType.EndObject) break;
                                        else if (reader.TokenType == JsonTokenType.PropertyName)
                                        {
                                            string prop = reader.GetString()!;
                                            if (reader.Read())
                                            {
                                                switch (prop)
                                                {
                                                    case "x":
                                                        x = (float)reader.GetDouble();
                                                        break;
                                                    case "y":
                                                        y = (float)reader.GetDouble();
                                                        break;
                                                    case "z":
                                                        z = (float)reader.GetDouble();
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                    xtraPoints.Add(key, new Vector3(x, y, z));
                                }
                            }
                        }

                        return xtraPoints;
                    }
                    catch (Exception e)
                    {
                        L.LogError("Failed to read " + xtraPts);
                        L.LogError(e);
                        goto def;
                    }
                }
            }
        }
        else
        {
            L.LogError("Failed to load extra points, see above. Loading default points.");
            goto def;
        }


    def:
        Dictionary<string, Vector3> defaultXtraPoints = new Dictionary<string, Vector3>(DefaultExtraPoints.Count);
        for (int i = 0; i < DefaultExtraPoints.Count; i++)
        {
            Point3D point = DefaultExtraPoints[i];
            defaultXtraPoints.Add(point.name, point.Vector3);
        }
        return defaultXtraPoints;
    }
    public static Dictionary<ulong, string> LoadLanguagePreferences()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        F.CheckDir(Data.LangStorage, out bool dirExists);
        string langPrefs = Path.Combine(Data.LangStorage, "preferences.json");
        if (dirExists)
        {
            if (!File.Exists(langPrefs))
            {
                using (FileStream stream = new FileStream(langPrefs, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    byte[] utf8 = System.Text.Encoding.UTF8.GetBytes("[]");
                    stream.Write(utf8, 0, utf8.Length);
                    stream.Close();
                    stream.Dispose();
                }
                return new Dictionary<ulong, string>();
            }
            using (FileStream stream = new FileStream(langPrefs, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long len = stream.Length;
                if (len > int.MaxValue)
                {
                    L.LogError("Language preferences at preferences.json is too long to read.");
                    return new Dictionary<ulong, string>();
                }
                else
                {
                    Dictionary<ulong, string> prefs = new Dictionary<ulong, string>(48);
                    byte[] bytes = new byte[len];
                    stream.Read(bytes, 0, (int)len);
                    try
                    {
                        Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.StartObject) continue;
                            else if (reader.TokenType == JsonTokenType.EndObject) break;
                            else if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                string input = reader.GetString()!;
                                if (!ulong.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out ulong steam64))
                                {
                                    L.LogWarning("Invalid Steam64 ID: \"" + input + "\" in Lang\\preferences.json");
                                }
                                else if (reader.Read() && reader.TokenType == JsonTokenType.String)
                                {
                                    string language = reader.GetString()!;
                                    prefs.Add(steam64, language);
                                }
                            }
                        }

                        return prefs;
                    }
                    catch (Exception ex)
                    {
                        L.LogError("Failed to read language preferences at " + langPrefs);
                        L.LogError(ex);
                        return new Dictionary<ulong, string>();
                    }
                }
            }
        }
        else
        {
            L.LogError("Failed to load language preferences, see above.");
            return new Dictionary<ulong, string>();
        }
    }
    public static void SaveLangs(Dictionary<ulong, string> languages)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (languages == null) return;
        F.CheckDir(Data.LangStorage, out bool dirExists);
        if (dirExists)
        {
            using (FileStream stream = new FileStream(Path.Combine(Data.LangStorage, "preferences.json"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                writer.WriteStartObject();
                foreach (KeyValuePair<ulong, string> languagePref in languages)
                {
                    writer.WritePropertyName(languagePref.Key.ToString(Data.Locale));
                    writer.WriteStringValue(languagePref.Value);
                }
                writer.WriteEndObject();
                writer.Dispose();
                stream.Close();
                stream.Dispose();
            }
        }
    }
    public static void SetLanguage(ulong player, string language)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Languages.ContainsKey(player))
        {
            Data.Languages[player] = language;
            SaveLangs(Data.Languages);
        }
        else
        {
            Data.Languages.Add(player, language);
            SaveLangs(Data.Languages);
        }
    }
    public static Dictionary<string, LanguageAliasSet> LoadLangAliases()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        F.CheckDir(Data.LangStorage, out bool dirExists);
        string langAliases = Path.Combine(Data.LangStorage, "aliases.json");
        if (dirExists)
        {
            if (!File.Exists(langAliases))
            {
                Dictionary<string, LanguageAliasSet> defaultLanguageAliasSets2 = new Dictionary<string, LanguageAliasSet>(DefaultLanguageAliasSets.Count);
                using (FileStream stream = new FileStream(langAliases, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                    writer.WriteStartArray();
                    for (int i = 0; i < DefaultLanguageAliasSets.Count; i++)
                    {
                        LanguageAliasSet set = DefaultLanguageAliasSets[i];
                        writer.WriteStartObject();
                        set.WriteJson(writer);
                        writer.WriteEndObject();
                        defaultLanguageAliasSets2.Add(set.key, set);
                    }
                    writer.WriteEndArray();
                    writer.Dispose();
                    stream.Close();
                    stream.Dispose();
                }
                return defaultLanguageAliasSets2;
            }
            using (FileStream stream = new FileStream(langAliases, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long len = stream.Length;
                if (len > int.MaxValue)
                {
                    L.LogError("Language alias sets at aliases.json is too long to read.");
                    goto def;
                }
                else
                {
                    Dictionary<string, LanguageAliasSet> languageAliasSets = new Dictionary<string, LanguageAliasSet>(DefaultLanguageAliasSets.Count);
                    byte[] bytes = new byte[len];
                    stream.Read(bytes, 0, (int)len);
                    try
                    {
                        Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.StartArray) continue;
                            else if (reader.TokenType == JsonTokenType.EndArray) break;
                            else if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                LanguageAliasSet set = new LanguageAliasSet();
                                set.ReadJson(ref reader);
                                if (set.key != null)
                                    languageAliasSets.Add(set.key, set);
                            }
                        }

                        return languageAliasSets;
                    }
                    catch (Exception e)
                    {
                        L.LogError("Failed to read language aliases at aliases.json.");
                        L.LogError(e);
                        goto def;
                    }
                }
            }
        }
        else
        {
            L.LogError("Failed to load language aliases, see above. Loading default language aliases.");
            goto def;
        }
        def:
        Dictionary<string, LanguageAliasSet> defaultLanguageAliasSets = new Dictionary<string, LanguageAliasSet>(DefaultLanguageAliasSets.Count);
        for (int i = 0; i < DefaultLanguageAliasSets.Count; i++)
        {
            LanguageAliasSet set = DefaultLanguageAliasSets[i];
            defaultLanguageAliasSets.Add(set.key, set);
        }
        return defaultLanguageAliasSets;
    }
}
