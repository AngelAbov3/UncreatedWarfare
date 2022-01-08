﻿using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static class Translation
    {
        public static string ObjectTranslate(string key, string language, params object[] formatting)
        {
            if (language == null || !Data.Localization.TryGetValue(language, out Dictionary<string, TranslationData> data))
            {
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        data = Data.Localization.First().Value;
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
            }
            if (data.TryGetValue(key, out TranslationData translation))
            {
                try
                {
                    return string.Format(translation.Original, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                    return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else if (language != JSONMethods.DefaultLanguage)
            {
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        data = Data.Localization.First().Value;
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                if (data.TryGetValue(key, out translation))
                {
                    try
                    {
                        return string.Format(translation.Original, formatting);
                    }
                    catch (FormatException ex)
                    {
                        L.LogError(ex);
                        return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else
            {
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        public static string ObjectTranslate(string key, ulong player, params object[] formatting)
        {
            if (key == null)
            {
                string args = formatting.Length == 0 ? string.Empty : string.Join(", ", formatting);
                L.LogError($"Message to be sent to {player} was null{(formatting.Length == 0 ? "" : ": ")}{args}");
                return args;
            }
            if (key.Length == 0)
            {
                return formatting.Length > 0 ? string.Join(", ", formatting) : "";
            }
            if (player == 0)
            {
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<string, TranslationData> data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                        {
                            try
                            {
                                return string.Format(translation.Original, formatting);
                            }
                            catch (FormatException ex)
                            {
                                L.LogError(ex);
                                return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                            }
                        }
                        else
                        {
                            return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    if (data.TryGetValue(key, out TranslationData translation))
                    {
                        try
                        {
                            return string.Format(translation.Original, formatting);
                        }
                        catch (FormatException ex)
                        {
                            L.LogError(ex);
                            return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
            }
            else
            {
                if (Data.Languages.TryGetValue(player, out string lang))
                {
                    if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data2) || !data2.ContainsKey(key))
                        lang = JSONMethods.DefaultLanguage;
                }
                else lang = JSONMethods.DefaultLanguage;
                if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                        {
                            try
                            {
                                return string.Format(translation.Original, formatting);
                            }
                            catch (FormatException ex)
                            {
                                L.LogError(ex);
                                return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                            }
                        }
                        else
                        {
                            return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else if (data.TryGetValue(key, out TranslationData translation))
                {
                    try
                    {
                        return string.Format(translation.Original, formatting);
                    }
                    catch (FormatException ex)
                    {
                        L.LogError(ex);
                        return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
        }
        public static string Translate(string key, UCPlayer player, params string[] formatting) =>
            Translate(key, player.Steam64, formatting);
        public static string Translate(string key, UCPlayer player, out Color color, params string[] formatting) =>
            Translate(key, player.Steam64, out color, formatting);
        public static string Translate(string key, SteamPlayer player, params string[] formatting) =>
            Translate(key, player.playerID.steamID.m_SteamID, formatting);
        public static string Translate(string key, SteamPlayer player, out Color color, params string[] formatting) =>
            Translate(key, player.playerID.steamID.m_SteamID, out color, formatting);
        public static string Translate(string key, Player player, params string[] formatting) =>
            Translate(key, player.channel.owner.playerID.steamID.m_SteamID, formatting);
        public static string Translate(string key, Player player, out Color color, params string[] formatting) =>
            Translate(key, player.channel.owner.playerID.steamID.m_SteamID, out color, formatting);
        public static string Translate(string key, UnturnedPlayer player, params string[] formatting) =>
            Translate(key, player.Player.channel.owner.playerID.steamID.m_SteamID, formatting);
        public static string Translate(string key, UnturnedPlayer player, out Color color, params string[] formatting) =>
            Translate(key, player.Player.channel.owner.playerID.steamID.m_SteamID, out color, formatting);
        /// <summary>
        /// Tramslate an unlocalized string to a localized translation structure using the translations file.
        /// </summary>
        /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="player">The player to check language on, pass 0 to use the <see cref="JSONMethods.DefaultLanguage"/>.</param>
        /// <returns>A translation structure.</returns>
        public static TranslationData GetTranslation(string key, ulong player)
        {
            if (key == null)
            {
                L.LogError($"Message to be sent to {player} was null.");
                return TranslationData.Nil;
            }
            if (key.Length == 0)
            {
                return TranslationData.Nil;
            }
            if (player == 0)
            {
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<string, TranslationData> data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                        {
                            return translation;
                        }
                        else
                        {
                            return TranslationData.Nil;
                        }
                    }
                    else
                    {
                        return TranslationData.Nil;
                    }
                }
                else
                {
                    if (data.TryGetValue(key, out TranslationData translation))
                    {
                        return translation;
                    }
                    else
                    {
                        return TranslationData.Nil;
                    }
                }
            }
            else
            {
                if (Data.Languages.TryGetValue(player, out string lang))
                {
                    if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data2) || !data2.ContainsKey(key))
                        lang = JSONMethods.DefaultLanguage;
                }
                else lang = JSONMethods.DefaultLanguage;
                if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                        {
                            return translation;
                        }
                        else
                        {
                            return TranslationData.Nil;
                        }
                    }
                    else
                    {
                        return TranslationData.Nil;
                    }
                }
                else if (data.TryGetValue(key, out TranslationData translation))
                {
                    return translation;
                }
                else
                {
                    return TranslationData.Nil;
                }
            }
        }
        /// <summary>
        /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the Original message (non-color removed)
        /// </summary>
        /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="player">The player to check language on, pass 0 to use the <see cref="JSONMethods.DefaultLanguage">Default Language</see>.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        /// <returns>A localized string based on the player's language.</returns>
        public static string Translate(string key, ulong player, params string[] formatting)
        {
            if (key == null)
            {
                string args = formatting.Length == 0 ? string.Empty : string.Join(", ", formatting);
                L.LogError($"Message to be sent to {player} was null{(formatting.Length == 0 ? "" : ": ")}{args}");
                return args;
            }
            if (key.Length == 0)
            {
                return formatting.Length > 0 ? string.Join(", ", formatting) : "";
            }
            if (player == 0)
            {
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<string, TranslationData> data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                        {
                            try
                            {
                                return string.Format(translation.Original, formatting);
                            }
                            catch (FormatException ex)
                            {
                                L.LogError(ex);
                                return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                            }
                        }
                        else
                        {
                            return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    if (data.TryGetValue(key, out TranslationData translation))
                    {
                        try
                        {
                            return string.Format(translation.Original, formatting);
                        }
                        catch (FormatException ex)
                        {
                            L.LogError(ex);
                            return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
            }
            else
            {
                if (Data.Languages.TryGetValue(player, out string lang))
                {
                    if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data2) || !data2.ContainsKey(key))
                        lang = JSONMethods.DefaultLanguage;
                }
                else lang = JSONMethods.DefaultLanguage;
                if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                        {
                            try
                            {
                                return string.Format(translation.Original, formatting);
                            }
                            catch (FormatException ex)
                            {
                                L.LogError(ex);
                                return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                            }
                        }
                        else
                        {
                            return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else if (data.TryGetValue(key, out TranslationData translation))
                {
                    try
                    {
                        return string.Format(translation.Original, formatting);
                    }
                    catch (FormatException ex)
                    {
                        L.LogError(ex);
                        return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
        }
        /// <summary>
        /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the color-removed message along with the color.
        /// </summary>
        /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="player">The player to check language on, pass 0 to use the <see cref="JSONMethods.DefaultLanguage">Default Language</see>.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        /// <param name="color">Color of the message.</param>
        /// <returns>A localized string based on the player's language.</returns>
        public static string Translate(string key, ulong player, out Color color, params string[] formatting)
        {
            if (key == null)
            {
                string args = formatting.Length == 0 ? string.Empty : string.Join(", ", formatting);
                L.LogError($"Message to be sent to {player} was null{(formatting.Length == 0 ? "" : ": ")}{args}");
                color = UCWarfare.GetColor("default");
                return args;
            }
            if (key.Length == 0)
            {
                color = UCWarfare.GetColor("default");
                return formatting.Length > 0 ? string.Join(", ", formatting) : "";
            }
            if (player == 0)
            {
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<string, TranslationData> data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                        {
                            color = translation.Color;
                            try
                            {
                                return string.Format(translation.Message, formatting);
                            }
                            catch (FormatException ex)
                            {
                                L.LogError(ex);
                                return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                            }
                        }
                        else
                        {
                            color = UCWarfare.GetColor("default");
                            return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        color = UCWarfare.GetColor("default");
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    if (data.TryGetValue(key, out TranslationData translation))
                    {
                        color = translation.Color;
                        try
                        {
                            return string.Format(translation.Message, formatting);
                        }
                        catch (FormatException ex)
                        {
                            L.LogError(ex);
                            return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        color = UCWarfare.GetColor("default");
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
            }
            else
            {
                if (Data.Languages.TryGetValue(player, out string lang))
                {
                    if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data2) || !data2.ContainsKey(key))
                        lang = JSONMethods.DefaultLanguage;
                }
                else lang = JSONMethods.DefaultLanguage;
                if (!Data.Localization.TryGetValue(lang, out Dictionary<string, TranslationData> data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        if (Data.Localization.ElementAt(0).Value.TryGetValue(key, out TranslationData translation))
                        {
                            color = translation.Color;
                            try
                            {
                                return string.Format(translation.Message, formatting);
                            }
                            catch (FormatException ex)
                            {
                                L.LogError(ex);
                                return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                            }
                        }
                        else
                        {
                            color = UCWarfare.GetColor("default");
                            return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                        }
                    }
                    else
                    {
                        color = UCWarfare.GetColor("default");
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else if (data.TryGetValue(key, out TranslationData translation))
                {
                    color = translation.Color;
                    try
                    {
                        return string.Format(translation.Message, formatting);
                    }
                    catch (FormatException ex)
                    {
                        L.LogError(ex);
                        return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    color = UCWarfare.GetColor("default");
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
        }

        /// <summary>
        /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the color-removed message along with the color.
        /// </summary>
        /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="language">The first language to translate with, pass null to use <see cref="JSONMethods.DefaultLanguage">Default Language</see>.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        /// <param name="color">Color of the message.</param>
        /// <returns>A localized string based on <paramref name="language"/>.</returns>
        public static string Translate(string key, string language, out Color color, params string[] formatting)
        {
            if (language == null || !Data.Localization.TryGetValue(language, out Dictionary<string, TranslationData> data))
            {
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        data = Data.Localization.First().Value;
                    }
                    else
                    {
                        color = UCWarfare.GetColor("default");
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
            }
            if (data.TryGetValue(key, out TranslationData translation))
            {
                color = translation.Color;
                try
                {
                    return string.Format(translation.Message, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                    return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else if (language != JSONMethods.DefaultLanguage)
            {
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        data = Data.Localization.First().Value;
                    }
                    else
                    {
                        color = UCWarfare.GetColor("default");
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                if (data.TryGetValue(key, out translation))
                {
                    color = translation.Color;
                    try
                    {
                        return string.Format(translation.Message, formatting);
                    }
                    catch (FormatException ex)
                    {
                        L.LogError(ex);
                        return translation.Message + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    color = UCWarfare.GetColor("default");
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else
            {
                color = UCWarfare.GetColor("default");
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }
        /// <summary>
        /// Tramslate an unlocalized string to a localized string using the Rocket translations file, provides the message with color still in it.
        /// </summary>
        /// <param name="key">The unlocalized string to match with the translation dictionary.</param>
        /// <param name="language">The first language to translate with, pass null to use <see cref="JSONMethods.DefaultLanguage">Default Language</see>.</param>
        /// <param name="formatting">list of strings to replace the {n}s in the translations.</param>
        /// <returns>A localized string based on <paramref name="language"/>.</returns>
        public static string Translate(string key, string language, params string[] formatting)
        {
            if (language == null || !Data.Localization.TryGetValue(language, out Dictionary<string, TranslationData> data))
            {
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        data = Data.Localization.First().Value;
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
            }
            if (data.TryGetValue(key, out TranslationData translation))
            {
                try
                {
                    return string.Format(translation.Original, formatting);
                }
                catch (FormatException ex)
                {
                    L.LogError(ex);
                    return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else if (language != JSONMethods.DefaultLanguage)
            {
                if (!Data.Localization.TryGetValue(JSONMethods.DefaultLanguage, out data))
                {
                    if (Data.Localization.Count > 0)
                    {
                        data = Data.Localization.First().Value;
                    }
                    else
                    {
                        return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                if (data.TryGetValue(key, out translation))
                {
                    try
                    {
                        return string.Format(translation.Original, formatting);
                    }
                    catch (FormatException ex)
                    {
                        L.LogError(ex);
                        return translation.Original + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                    }
                }
                else
                {
                    return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
                }
            }
            else
            {
                return key + (formatting.Length > 0 ? (" - " + string.Join(", ", formatting)) : "");
            }
        }

        public static string GetTimeFromSeconds(this uint seconds, ulong player)
        {
            if (seconds < 60) // < 1 minute
            {
                return (seconds + 1).ToString(Data.Locale) + ' ' + Translate("time_second" + seconds.S(), player);
            }
            else if (seconds < 3600) // < 1 hour
            {
                int minutes = F.DivideRemainder(seconds, 60, out int secondOverflow);
                return $"{minutes} {Translate("time_minute" + minutes.S(), player)}{(secondOverflow == 0 ? "" : $" {Translate("time_and", player)} {secondOverflow} {Translate("time_second" + secondOverflow.S(), player)}")}";
            }
            else if (seconds < 86400) // < 1 day 
            {
                int hours = F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out int minutesOverflow);
                return $"{hours} {Translate("time_hour" + hours.S(), player)}{(minutesOverflow == 0 ? "" : $" {Translate("time_and", player)} {minutesOverflow} {Translate("time_minute" + minutesOverflow.S(), player)}")}";
            }
            else if (seconds < 2628000) // < 1 month (30.416 days) (365/12)
            {
                uint days = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out uint hoursOverflow);
                return $"{days} {Translate("time_day" + days.S(), player)}{(hoursOverflow == 0 ? "" : $" {Translate("time_and", player)} {hoursOverflow} {Translate("time_hour" + hoursOverflow.S(), player)}")}";
            }
            else if (seconds < 31536000) // < 1 year
            {
                uint months = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out uint daysOverflow);
                return $"{months} {Translate("time_month" + months.S(), player)}{(daysOverflow == 0 ? "" : $" {Translate("time_and", player)} {daysOverflow} {Translate("time_day" + daysOverflow.S(), player)}")}";
            }
            else // > 1 year
            {
                uint years = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out _), 12, out uint monthOverflow);
                return $"{years} {Translate("time_year" + years.S(), player)}{years.S()}{(monthOverflow == 0 ? "" : $" {Translate("time_and", player)} {monthOverflow} {Translate("time_month" + monthOverflow.S(), player)}")}";
            }
        }
        public static string GetTimeFromSeconds(this uint seconds, string language)
        {
            if (seconds < 60) // < 1 minute
            {
                return (seconds + 1).ToString(Data.Locale) + ' ' + Translate("time_second" + seconds.S(), language);
            }
            else if (seconds < 3600) // < 1 hour
            {
                int minutes = F.DivideRemainder(seconds, 60, out int secondOverflow);
                return $"{minutes} {Translate("time_minute" + minutes.S(), language)}{(secondOverflow == 0 ? "" : $" {Translate("time_and", language)} {secondOverflow} {Translate("time_second" + secondOverflow.S(), language)}")}";
            }
            else if (seconds < 86400) // < 1 day 
            {
                int hours = F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out int minutesOverflow);
                return $"{hours} {Translate("time_hour" + hours.S(), language)}{(minutesOverflow == 0 ? "" : $" {Translate("time_and", language)} {minutesOverflow} {Translate("time_minute" + minutesOverflow.S(), language)}")}";
            }
            else if (seconds < 2628000) // < 1 month (30.416 days) (365/12)
            {
                uint days = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out uint hoursOverflow);
                return $"{days} {Translate("time_day" + days.S(), language)}{(hoursOverflow == 0 ? "" : $" {Translate("time_and", language)} {hoursOverflow} {Translate("time_hour" + hoursOverflow.S(), language)}")}";
            }
            else if (seconds < 31536000) // < 1 year
            {
                uint months = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out uint daysOverflow);
                return $"{months} {Translate("time_month" + months.S(), language)}{(daysOverflow == 0 ? "" : $" {Translate("time_and", language)} {daysOverflow} {Translate("time_day" + daysOverflow.S(), language)}")}";
            }
            else // > 1 year
            {
                uint years = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(seconds, 60, out _), 60, out _), 24, out _), 30.416m, out _), 12, out uint monthOverflow);
                return $"{years} {Translate("time_year" + years.S(), language)}{years.S()}{(monthOverflow == 0 ? "" : $" {Translate("time_and", language)} {monthOverflow} {Translate("time_month" + monthOverflow.S(), language)}")}";
            }
        }
        public static string GetTimeFromMinutes(this uint minutes, ulong player)
        {
            if (minutes < 60) // < 1 hour
            {
                return minutes.ToString(Data.Locale) + ' ' + Translate("time_minute" + minutes.S(), player);
            }
            else if (minutes < 1440) // < 1 day 
            {
                uint hours = F.DivideRemainder(minutes, 60, out uint minutesOverflow);
                return $"{hours} {Translate("time_hour" + hours.S(), player)}{(minutesOverflow == 0 ? "" : $" {Translate("time_and", player)} {minutesOverflow} {Translate("time_minute" + minutesOverflow.S(), player)}")}";
            }
            else if (minutes < 43800) // < 1 month (30.416 days)
            {
                uint days = F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out uint hoursOverflow);
                return $"{days} {Translate("time_day" + days.S(), player)}{(hoursOverflow == 0 ? "" : $" {Translate("time_and", player)} {hoursOverflow} {Translate("time_hour" + hoursOverflow.S(), player)}")}";
            }
            else if (minutes < 525600) // < 1 year
            {
                uint months = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out uint daysOverflow);
                return $"{months} {Translate("time_month" + months.S(), player)}{(daysOverflow == 0 ? "" : $" {Translate("time_and", player)} {daysOverflow} {Translate("time_day" + daysOverflow.S(), player)}")}";
            }
            else // > 1 year
            {
                uint years = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out _), 12, out uint monthOverflow);
                return $"{years} {Translate("time_year" + years.S(), player)}{(monthOverflow == 0 ? "" : $" {Translate("time_and", player)} {monthOverflow} {Translate("time_month" + monthOverflow.S(), player)}")}";
            }
        }
        public static string GetTimeFromMinutes(this uint minutes, string language)
        {
            if (minutes < 60) // < 1 hour
            {
                return minutes.ToString(Data.Locale) + ' ' + Translate("time_minute" + minutes.S(), language);
            }
            else if (minutes < 1440) // < 1 day 
            {
                uint hours = F.DivideRemainder(minutes, 60, out uint minutesOverflow);
                return $"{hours} {Translate("time_hour" + hours.S(), language)}{(minutesOverflow == 0 ? "" : $" {Translate("time_and", language)} {minutesOverflow} {Translate("time_minute" + minutesOverflow.S(), language)}")}";
            }
            else if (minutes < 43800) // < 1 month (30.416 days)
            {
                uint days = F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out uint hoursOverflow);
                return $"{days} {Translate("time_day" + days.S(), language)}{(hoursOverflow == 0 ? "" : $" {Translate("time_and", language)} {hoursOverflow} {Translate("time_hour" + hoursOverflow.S(), language)}")}";
            }
            else if (minutes < 525600) // < 1 year
            {
                uint months = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out uint daysOverflow);
                return $"{months} {Translate("time_month" + months.S(), language)}{(daysOverflow == 0 ? "" : $" {Translate("time_and", language)} {daysOverflow} {Translate("time_day" + daysOverflow.S(), language)}")}";
            }
            else // > 1 year
            {
                uint years = F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(F.DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out _), 12, out uint monthOverflow);
                return $"{years} {Translate("time_year" + years.S(), language)}{(monthOverflow == 0 ? "" : $" {Translate("time_and", language)} {monthOverflow} {Translate("time_month" + monthOverflow.S(), language)}")}";
            }
        }
        public static string TranslateSign(string key, string language, UCPlayer ucplayer, bool important = false)
        {
            try
            {
                if (key == null) return string.Empty;
                if (!key.StartsWith("sign_")) return Translate(key, language);
                string key2 = key.Substring(5);
                if (key2.StartsWith("loadout_") && key2.Length > 8 && ushort.TryParse(key2.Substring(8), System.Globalization.NumberStyles.Any, Data.Locale, out ushort loadoutid))
                {
                    if (ucplayer != null)
                    {
                        ulong team = ucplayer.GetTeam();
                        List<Kit> loadouts = KitManager.GetKitsWhere(k => k.IsLoadout && k.Team == team && k.AllowedUsers.Contains(ucplayer.Steam64)).ToList();
                        loadouts.Sort((k1, k2) => k1.Name.CompareTo(k2.Name));

                        if (loadouts.Count > 0)
                        {
                            if (loadoutid > 0 && loadoutid <= loadouts.Count)
                            {
                                Kit kit = loadouts[loadoutid - 1];

                                if (!kit.SignTexts.TryGetValue(language, out string name))
                                    if (!kit.SignTexts.TryGetValue(JSONMethods.DefaultLanguage, out name))
                                        if (kit.SignTexts.Count > 0)
                                            name = kit.SignTexts.First().Value;
                                        else
                                            name = kit.DisplayName ?? kit.Name;
                                bool keepline = false;
                                foreach (char @char in name)
                                {
                                    if (@char == '\n')
                                    {
                                        keepline = true;
                                        break;
                                    }
                                }
                                string cost = Translate("loadout_name_owned", language, loadoutid.ToString()).Colorize(UCWarfare.GetColorHex("kit_level_dollars"));
                                if (!keepline) cost = "\n" + cost;

                                string playercount = string.Empty;

                                if (kit.TeamLimit >= 1f || kit.TeamLimit <= 0f)
                                {
                                    playercount = Translate("kit_unlimited", language).Colorize(UCWarfare.GetColorHex("kit_unlimited_players"));
                                }
                                else if (kit.IsClassLimited(out int total, out int allowed, kit.Team > 0 && kit.Team < 3 ? kit.Team : team, true))
                                {
                                    playercount = Translate("kit_player_count", language, total.ToString(Data.Locale), allowed.ToString(Data.Locale))
                                        .Colorize(UCWarfare.GetColorHex("kit_player_counts_unavailable"));
                                }
                                else
                                {
                                    playercount = Translate("kit_player_count", language, total.ToString(Data.Locale), allowed.ToString(Data.Locale))
                                        .Colorize(UCWarfare.GetColorHex("kit_player_counts_available"));
                                }

                                return Translate("sign_kit_request", language,
                                    name.ToUpper().Colorize(UCWarfare.GetColorHex("kit_public_header")),
                                    cost,
                                    kit.Weapons == "" ? " " : Translate("kit_weapons", language, kit.Weapons.ToUpper().Colorize(UCWarfare.GetColorHex("kit_weapon_list"))),
                                    playercount
                                    );
                            }
                        }
                    }

                    return Translate("sign_kit_request", language,
                                Translate("loadout_name", language, loadoutid.ToString()).Colorize(UCWarfare.GetColorHex("kit_public_header")),
                                string.Empty,
                                ObjectTranslate("kit_price_dollars", language, UCWarfare.Config.LoadoutCost).Colorize(UCWarfare.GetColorHex("kit_level_dollars")),
                                string.Empty
                                );
                }
                else if (KitManager.KitExists(key2, out Kit kit))
                {
                    ulong playerteam = 0;
                    RankData playerrank = null;
                    if (ucplayer != null)
                    {
                        playerteam = ucplayer.GetTeam();
                        playerrank = ucplayer.Ranks[kit.UnlockBranch];
                    }

                    if (!kit.SignTexts.TryGetValue(language, out string name))
                        if (!kit.SignTexts.TryGetValue(JSONMethods.DefaultLanguage, out name))
                            if (kit.SignTexts.Count > 0)
                                name = kit.SignTexts.First().Value;
                            else
                                name = kit.DisplayName ?? kit.Name;

                    bool keepline = false;
                    foreach (char @char in name)
                    {
                        if (@char == '\n')
                        {
                            keepline = true;
                            break;
                        }
                    }
                    name = Translate("kit_name", language, name.ToUpper().Colorize(UCWarfare.GetColorHex("kit_public_header")));
                    string weapons = kit.Weapons ?? string.Empty;
                    if (weapons != string.Empty)
                        weapons = Translate("kit_weapons", language, weapons.ToUpper().Colorize(UCWarfare.GetColorHex("kit_weapon_list")));
                    string cost = "";
                    string playercount;
                    if (kit.IsPremium && (kit.PremiumCost > 0 || kit.PremiumCost == -1))
                    {
                        if (kit.AllowedUsers.Contains(ucplayer.Steam64))
                            cost = ObjectTranslate("kit_owned", language).Colorize(UCWarfare.GetColorHex("kit_level_dollars_owned"));
                        else if (kit.PremiumCost == -1)
                            cost = Translate("kit_price_exclusive", language).Colorize(UCWarfare.GetColorHex("kit_level_dollars_exclusive"));
                        else
                            cost = ObjectTranslate("kit_price_dollars", language, kit.PremiumCost).Colorize(UCWarfare.GetColorHex("kit_level_dollars"));
                    }
                    else if (kit.UnlockLevel > 0)
                    {
                        if (playerrank.Level < kit.UnlockLevel)
                        {
                            cost = Translate("kit_required_level", language, kit.UnlockLevel.ToString(Data.Locale), UCWarfare.GetColorHex("kit_level_unavailable"),
                                RankData.GetRankAbbreviation(RankData.GetRankTier(kit.UnlockLevel)), UCWarfare.GetColorHex("kit_level_unavailable_abbr"));
                        }
                        else
                        {
                            cost = Translate("kit_required_level", language, kit.UnlockLevel.ToString(Data.Locale), (UCWarfare.GetColorHex("kit_level_available")),
                                RankData.GetRankAbbreviation(RankData.GetRankTier(kit.UnlockLevel)), UCWarfare.GetColorHex("kit_level_available_abbr"));
                        }
                    }
                    else
                    {
                        cost = string.Empty;
                    }
                    if (!keepline) cost = "\n" + cost;
                    if (kit.TeamLimit >= 1f || kit.TeamLimit <= 0f)
                    {
                        playercount = Translate("kit_unlimited", language).Colorize(UCWarfare.GetColorHex("kit_unlimited_players"));
                    }
                    else if (kit.IsLimited(out int total, out int allowed, kit.Team > 0 && kit.Team < 3 ? kit.Team : playerteam, true))
                    {
                        playercount = Translate("kit_player_count", language, total.ToString(Data.Locale), allowed.ToString(Data.Locale))
                            .Colorize(UCWarfare.GetColorHex("kit_player_counts_unavailable"));
                    }
                    else
                    {
                        playercount = Translate("kit_player_count", language, total.ToString(Data.Locale), allowed.ToString(Data.Locale))
                            .Colorize(UCWarfare.GetColorHex("kit_player_counts_available"));
                    }
                    return Translate("sign_kit_request", language, name, cost, weapons, playercount);
                }
                else return key;
            }
            catch (Exception ex)
            {
                L.LogError("Error translating sign: ");
                L.LogError(ex);
                return ex.GetType().Name;
            }
        }
        public static string TranslateSign(string key, UCPlayer player, bool important = true)
        {
            if (!Data.Languages.TryGetValue(player.Steam64, out string lang))
                lang = JSONMethods.DefaultLanguage;
            return TranslateSign(key, lang, player, important);
        }
        public static string DecideLanguage<TVal>(ulong player, Dictionary<string, TVal> searcher)
        {
            if (player == 0)
            {
                if (!searcher.ContainsKey(JSONMethods.DefaultLanguage))
                {
                    if (searcher.Count > 0)
                    {
                        return searcher.ElementAt(0).Key;
                    }
                    else return JSONMethods.DefaultLanguage;
                }
                else return JSONMethods.DefaultLanguage;
            }
            else
            {
                if (!Data.Languages.TryGetValue(player, out string lang) || !searcher.ContainsKey(lang))
                {
                    if (searcher.Count > 0)
                    {
                        return searcher.ElementAt(0).Key;
                    }
                    else return JSONMethods.DefaultLanguage;
                }
                return lang;
            }
        }
        public static string TranslateLimb(string language, ELimb limb)
        {
            if ((!Data.LimbLocalization.TryGetValue(language, out Dictionary<ELimb, string> loc) || !loc.ContainsKey(limb)) && !Data.LimbLocalization.TryGetValue(JSONMethods.DefaultLanguage, out loc))
            {
                return limb.ToString().ToLower().Replace('_', ' ');
            }
            if (loc.TryGetValue(limb, out string lang))
                return lang;
            return limb.ToString().ToLower().Replace('_', ' ');
        }
        public static string TranslateLimb(ulong player, ELimb limb)
        {
            if (player == 0)
            {
                if (!Data.LimbLocalization.TryGetValue(JSONMethods.DefaultLanguage, out Dictionary<ELimb, string> loc))
                {
                    if (Data.LimbLocalization.Count > 0)
                    {
                        loc = Data.LimbLocalization.ElementAt(0).Value;
                        if (loc.TryGetValue(limb, out string v))
                        {
                            return v;
                        }
                        else return limb.ToString();
                    }
                    else return limb.ToString();
                }
                else
                {
                    if (loc.TryGetValue(limb, out string v))
                    {
                        return v;
                    }
                    else return limb.ToString();
                }
            }
            else
            {
                if (!Data.Languages.TryGetValue(player, out string lang) || !Data.LimbLocalization.TryGetValue(lang, out Dictionary<ELimb, string> loc) || !loc.ContainsKey(limb))
                {
                    lang = JSONMethods.DefaultLanguage;
                }
                if (!Data.LimbLocalization.TryGetValue(lang, out loc))
                {
                    if (Data.LimbLocalization.Count > 0)
                    {
                        loc = Data.LimbLocalization.ElementAt(0).Value;
                        if (loc.TryGetValue(limb, out string v))
                        {
                            return v;
                        }
                        else return limb.ToString();
                    }
                    else return limb.ToString();
                }
                else if (loc.TryGetValue(limb, out string v))
                {
                    return v;
                }
                else return limb.ToString();
            }
        }
        /// <param name="backupcause">Used in case the key can not be found.</param>
        public static string TranslateDeath(string language, string key, EDeathCause backupcause, FPlayerName dead, ulong deadTeam, FPlayerName killerName, ulong killerTeam, ELimb limb, string itemName, float distance, bool usePlayerName = false, bool translateKillerName = false, bool colorize = true)
        {
            string deadname = usePlayerName ? dead.PlayerName : dead.CharacterName;
            if (colorize) deadname = F.ColorizeName(deadname, deadTeam);
            string murderername = translateKillerName ? Translate(killerName.PlayerName, language) : (usePlayerName ? killerName.PlayerName : killerName.CharacterName);
            if (colorize) murderername = F.ColorizeName(murderername, killerTeam);
            string dis = Mathf.RoundToInt(distance).ToString(Data.Locale) + 'm';

            if ((!Data.DeathLocalization.TryGetValue(language, out Dictionary<string, string> loc) || !loc.TryGetValue(key, out string v) || !loc.TryGetValue(backupcause.ToString(), out v)) && (language == JSONMethods.DefaultLanguage || !Data.DeathLocalization.TryGetValue(JSONMethods.DefaultLanguage, out loc) || !loc.TryGetValue(key, out v) || !loc.TryGetValue(backupcause.ToString(), out v)))
            {
                return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Mathf.RoundToInt(distance).ToString(Data.Locale) + "m"}";
            }
            try
            {
                return string.Format(v, deadname, murderername, TranslateLimb(language, limb), itemName, dis);
            }
            catch (FormatException ex)
            {
                L.LogError(ex);
                return key + $" ({deadname}, {murderername}, {limb}, {itemName}, {Mathf.RoundToInt(distance).ToString(Data.Locale) + "m"}";
            }
        }
        public static string TranslateLandmineDeath(string language, string key, FPlayerName dead, ulong deadTeam, FPlayerName killerName, ulong killerTeam, FPlayerName triggererName, ulong triggererTeam, ELimb limb, string landmineName, bool usePlayerName = false, bool colorize = true)
        {
            string deadname = usePlayerName ? dead.PlayerName : dead.CharacterName;
            if (colorize) deadname = F.ColorizeName(deadname, deadTeam);
            string murderername = usePlayerName ? killerName.PlayerName : killerName.CharacterName;
            if (colorize) murderername = F.ColorizeName(murderername, killerTeam);
            string triggerername = usePlayerName ? triggererName.PlayerName : triggererName.CharacterName;
            if (colorize) triggerername = F.ColorizeName(triggerername, triggererTeam);


            if ((!Data.DeathLocalization.TryGetValue(language, out Dictionary<string, string> loc) || !loc.TryGetValue(key, out string v)) && (language == JSONMethods.DefaultLanguage || !Data.DeathLocalization.TryGetValue(JSONMethods.DefaultLanguage, out loc) || !loc.TryGetValue(key, out v)))
            {
                return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
            }
            try
            {
                return string.Format(v, deadname, murderername, TranslateLimb(language, limb), landmineName, "0", triggerername);
            }
            catch (FormatException ex)
            {
                L.LogError(ex);
                return key + $" ({deadname}, {murderername}, {limb}, {landmineName}, 0m, {triggerername}";
            }
        }
        public static string TranslateBranch(EBranch branch, UCPlayer player)
        {
            string branchName = "team";
            ulong team = player.GetTeam();
            if (team == 1)
                branchName += "1_";
            else if (team == 2)
                branchName += "2_";

            return Translate(branchName + branch.ToString().ToLower(), player.Steam64, out _);
        }
        public static string TranslateBranch(EBranch branch, ulong player)
        {
            string branchName = "team";
            ulong team = player.GetTeamFromPlayerSteam64ID();
            if (team == 1)
                branchName += "1_";
            else if (team == 2)
                branchName += "2_";
            return Translate(branchName + branch.ToString().ToLower(), player, out _);
        }
        public static string TranslateVBS(Vehicles.VehicleSpawn spawn, VehicleData data, ulong player)
        {
            if (player == 0)
            {
                return TranslateVBS(spawn, data, JSONMethods.DefaultLanguage);
            }
            else
            {
                if (!Data.Languages.TryGetValue(player, out string lang))
                    lang = JSONMethods.DefaultLanguage;
                return TranslateVBS(spawn, data, lang);
            }
        }
        public static string TranslateVBS(Vehicles.VehicleSpawn spawn, VehicleData data, string language)
        {
            VehicleSpawnComponent comp;
            if (spawn.type == Structures.EStructType.STRUCTURE)
                if (spawn.StructureDrop != null)
                    comp = spawn.StructureDrop.model.gameObject.GetComponent<VehicleSpawnComponent>();
                else
                    return spawn.VehicleID.ToString("N");
            else if (spawn.BarricadeDrop != null)
                comp = spawn.BarricadeDrop.model.gameObject.GetComponent<VehicleSpawnComponent>();
            else return spawn.VehicleID.ToString("N");
            if (comp == null) return spawn.VehicleID.ToString("N");


            string finalformat =
                $"<color=#{UCWarfare.GetColorHex("vbs_name")}>{(Assets.find(spawn.VehicleID) is VehicleAsset asset ? asset.vehicleName : spawn.VehicleID.ToString("N"))}</color>\n" +
                $"<color=#{UCWarfare.GetColorHex("vbs_branch")}>{Translate("vbs_branch_" + data.Branch.ToString().ToLower(), language)}</color>\n" +
                (data.TicketCost > 0 ? $"<color=#{UCWarfare.GetColorHex("vbs_ticket_number")}>{data.TicketCost.ToString(Data.Locale)}</color><color=#{UCWarfare.GetColorHex("vbs_ticket_label")}> {Translate("vbs_tickets_postfix", language)}</color>" : string.Empty) +
                $"\n<color=#{{0}}>{(data.UnlockLevel <= 0 ? string.Empty : Translate("vbs_level_prefix", language) + " " + data.UnlockLevel.ToString(Data.Locale))}</color>\n";
            if (!spawn.HasLinkedVehicle(out InteractableVehicle vehicle) || !vehicle.TryGetComponent(out SpawnedVehicleComponent vehcomp)) // vehicle is dead
            {
                return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_dead")}>{Translate("vbs_state_dead", language, Mathf.FloorToInt(comp.respawnTimeRemaining / 60f).ToString(), (Mathf.RoundToInt(comp.respawnTimeRemaining) % 60).ToString("D2"))}</color>";
            }
            else if (vehcomp.hasBeenRequested)
            {
                if (vehcomp.isIdle)
                {
                    return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_idle")}>{Translate("vbs_state_idle", language, Mathf.FloorToInt(vehcomp.idleSecondsRemaining / 60f).ToString(), (Mathf.RoundToInt(vehcomp.idleSecondsRemaining) % 60).ToString("D2"))}</color>";
                }
                return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_active")}>{Translate("vbs_state_active", language, F.GetClosestLocation(vehicle.transform.position))}</color>";
            }
            else
            {
                return finalformat + $"<color=#{UCWarfare.GetColorHex("vbs_ready")}>{Translate("vbs_state_ready", language)}</color>";
            }
        }
        private static readonly List<LanguageSet> languages = new List<LanguageSet>(Data.Localization == null ? 3 : Data.Localization.Count);
        public static IEnumerable<LanguageSet> EnumerateLanguageSets()
        {
            lock (languages)
            {
                if (languages.Count > 0)
                    languages.Clear();
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                {
                    UCPlayer pl = PlayerManager.OnlinePlayers[i];
                    if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                        lang = JSONMethods.DefaultLanguage;
                    bool found = false;
                    for (int i2 = 0; i2 < languages.Count; i2++)
                    {
                        if (languages[i2].Language == lang)
                        {
                            languages[i2].Add(pl);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        languages.Add(new LanguageSet(lang, pl));
                }
                for (int i = 0; i < languages.Count; i++)
                {
                    yield return languages[i];
                }
                languages.Clear();
            }
        }
        public static IEnumerable<LanguageSet> EnumerateLanguageSets(IEnumerator<SteamPlayer> players)
        {
            lock (languages)
            {
                if (languages.Count > 0)
                    languages.Clear();
                while (players.MoveNext())
                {
                    UCPlayer pl = UCPlayer.FromSteamPlayer(players.Current);
                    if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                        lang = JSONMethods.DefaultLanguage;
                    bool found = false;
                    for (int i2 = 0; i2 < languages.Count; i2++)
                    {
                        if (languages[i2].Language == lang)
                        {
                            languages[i2].Add(pl);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        languages.Add(new LanguageSet(lang, pl));
                }
                players.Dispose();
                for (int i = 0; i < languages.Count; i++)
                {
                    yield return languages[i];
                }
                languages.Clear();
            }
        }
        public static IEnumerable<LanguageSet> EnumerateLanguageSets(IEnumerator<Player> players)
        {
            lock (languages)
            {
                if (languages.Count > 0)
                    languages.Clear();
                while (players.MoveNext())
                {
                    UCPlayer pl = UCPlayer.FromPlayer(players.Current);
                    if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                        lang = JSONMethods.DefaultLanguage;
                    bool found = false;
                    for (int i2 = 0; i2 < languages.Count; i2++)
                    {
                        if (languages[i2].Language == lang)
                        {
                            languages[i2].Add(pl);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        languages.Add(new LanguageSet(lang, pl));
                }
                players.Dispose();
                for (int i = 0; i < languages.Count; i++)
                {
                    yield return languages[i];
                }
                languages.Clear();
            }
        }
        public static IEnumerable<LanguageSet> EnumeratePermissions(EAdminType type = EAdminType.MODERATE_PERMS)
        {
            lock (languages)
            {
                if (languages.Count > 0)
                    languages.Clear();
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                {
                    UCPlayer pl = PlayerManager.OnlinePlayers[i];
                    if ((type & pl.GetPermissions()) != type) continue;
                    if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                        lang = JSONMethods.DefaultLanguage;
                    bool found = false;
                    for (int i2 = 0; i2 < languages.Count; i2++)
                    {
                        if (languages[i2].Language == lang)
                        {
                            languages[i2].Add(pl);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        languages.Add(new LanguageSet(lang, pl));
                }
                for (int i = 0; i < languages.Count; i++)
                {
                    yield return languages[i];
                }
                languages.Clear();
            }
        }
        public static IEnumerable<LanguageSet> EnumerateLanguageSets(IEnumerator<UCPlayer> players)
        {
            lock (languages)
            {
                if (languages.Count > 0)
                    languages.Clear();
                while (players.MoveNext())
                {
                    UCPlayer pl = players.Current;
                    if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                        lang = JSONMethods.DefaultLanguage;
                    bool found = false;
                    for (int i2 = 0; i2 < languages.Count; i2++)
                    {
                        if (languages[i2].Language == lang)
                        {
                            languages[i2].Add(pl);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        languages.Add(new LanguageSet(lang, pl));
                }
                players.Dispose();
                for (int i = 0; i < languages.Count; i++)
                {
                    yield return languages[i];
                }
                languages.Clear();
            }
        }
        public static IEnumerable<LanguageSet> EnumerateLanguageSets(ulong team)
        {
            lock (languages)
            {
                if (languages.Count > 0)
                    languages.Clear();
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                {
                    UCPlayer pl = PlayerManager.OnlinePlayers[i];
                    if (pl.GetTeam() != team) continue;
                    if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                        lang = JSONMethods.DefaultLanguage;
                    bool found = false;
                    for (int i2 = 0; i2 < languages.Count; i2++)
                    {
                        if (languages[i2].Language == lang)
                        {
                            languages[i2].Add(pl);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        languages.Add(new LanguageSet(lang, pl));
                }
                for (int i = 0; i < languages.Count; i++)
                {
                    yield return languages[i];
                }
                languages.Clear();
            }
        }
        public static IEnumerable<LanguageSet> EnumerateLanguageSets(Squads.Squad squad)
        {
            lock (languages)
            {
                if (languages.Count > 0)
                    languages.Clear();
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                {
                    UCPlayer pl = PlayerManager.OnlinePlayers[i];
                    if (pl.Squad != squad) continue;
                    if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                        lang = JSONMethods.DefaultLanguage;
                    bool found = false;
                    for (int i2 = 0; i2 < languages.Count; i2++)
                    {
                        if (languages[i2].Language == lang)
                        {
                            languages[i2].Add(pl);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        languages.Add(new LanguageSet(lang, pl));
                }
                for (int i = 0; i < languages.Count; i++)
                {
                    yield return languages[i];
                }
                languages.Clear();
            }
        }
        public static IEnumerable<LanguageSet> EnumerateLanguageSets(Predicate<UCPlayer> selector)
        {
            lock (languages)
            {
                if (languages.Count > 0)
                    languages.Clear();
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                {
                    UCPlayer pl = PlayerManager.OnlinePlayers[i];
                    if (!selector(pl)) continue;
                    if (!Data.Languages.TryGetValue(pl.Steam64, out string lang))
                        lang = JSONMethods.DefaultLanguage;
                    bool found = false;
                    for (int i2 = 0; i2 < languages.Count; i2++)
                    {
                        if (languages[i2].Language == lang)
                        {
                            languages[i2].Add(pl);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        languages.Add(new LanguageSet(lang, pl));
                }
                for (int i = 0; i < languages.Count; i++)
                {
                    yield return languages[i];
                }
                languages.Clear();
            }
        }
    }
    /// <summary>Disposing does nothing.</summary>
    public struct LanguageSet : IEnumerator<UCPlayer>
    {
        public string Language;
        public List<UCPlayer> Players;
        private int nextIndex;
        /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
        public UCPlayer Next;

        UCPlayer IEnumerator<UCPlayer>.Current => Next;

        object IEnumerator.Current => Next;

        public LanguageSet(string lang)
        {
            this.Language = lang;
            this.Players = new List<UCPlayer>(Provider.clients.Count);
            this.nextIndex = 0;
            this.Next = null;
        }
        public LanguageSet(string lang, UCPlayer first)
        {
            this.Language = lang;
            this.Players = new List<UCPlayer>(lang == JSONMethods.DefaultLanguage ? Provider.clients.Count : 4) { first };
            this.nextIndex = 0;
            this.Next = null;
        }
        public void Add(UCPlayer pl) => this.Players.Add(pl);
        /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
        public bool MoveNext()
        {
            if (nextIndex < this.Players.Count)
            {
                Next = this.Players[nextIndex];
                nextIndex++;
                return true;
            }
            else
                return false;
        }
        /// <summary>Use <see cref="MoveNext"/> to enumerate through the players and <seealso cref="Reset"/> to reset it.</summary>
        public void Reset()
        {
            Next = null;
            nextIndex = 0;
        }

        public void Dispose() { }
    }
}
