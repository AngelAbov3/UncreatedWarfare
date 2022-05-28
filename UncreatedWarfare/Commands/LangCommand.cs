﻿using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uncreated.Warfare.Commands;

public delegate void PlayerChangedLanguageDelegate(UnturnedPlayer player, LanguageAliasSet oldLanguage, LanguageAliasSet newLanguage);
public class LangCommand : IRocketCommand
{
    private readonly List<string> _permissions = new List<string>(1) { "uc.lang" };
    private readonly List<string> _aliases = new List<string>(0);
    public AllowedCaller AllowedCaller => AllowedCaller.Player;
    public string Name => "lang";
    public string Help => "Switch your language to some of our supported languages.";
    public string Syntax => "/lang";
    public List<string> Aliases => _aliases;
	public List<string> Permissions => _permissions;

    public static event PlayerChangedLanguageDelegate OnPlayerChangedLanguage;
    public void Execute(IRocketPlayer caller, string[] command)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UnturnedPlayer player = (UnturnedPlayer)caller;
        string op = command.Length > 0 ? command[0].ToLower() : string.Empty;
        if (command.Length == 0)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Data.LanguageAliases.Keys.Count; i++)
            {
                string langInput = Data.LanguageAliases.Keys.ElementAt(i);
                if (!Data.Localization.ContainsKey(langInput)) continue; // only show languages with translations
                if (i != 0) sb.Append(", ");
                sb.Append(langInput);
                LanguageAliasSet aliases;
                if (Data.LanguageAliases.ContainsKey(langInput))
                    aliases = Data.LanguageAliases[langInput];
                else
                    aliases = Data.LanguageAliases.Values.FirstOrDefault(x => x.values.Contains(langInput));
                if (!aliases.Equals(default(LanguageAliasSet))) sb.Append(" : ").Append(aliases.display_name);
            }
            player.SendChat("language_list", sb.ToString());
        }
        else if (command.Length == 1)
        {
            if (op == "current")
            {
                string OldLanguage = JSONMethods.DEFAULT_LANGUAGE;
                if (Data.Languages.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                    OldLanguage = Data.Languages[player.Player.channel.owner.playerID.steamID.m_SteamID];
                LanguageAliasSet oldSet;
                if (Data.LanguageAliases.ContainsKey(OldLanguage))
                    oldSet = Data.LanguageAliases[OldLanguage];
                else
                    oldSet = new LanguageAliasSet(OldLanguage, OldLanguage, new string[0]);

                player.SendChat("language_current", $"{oldSet.display_name} : {oldSet.key}");
            }
            else if (op == "reset")
            {
                string fullname = JSONMethods.DEFAULT_LANGUAGE;
                LanguageAliasSet alias;
                if (Data.LanguageAliases.ContainsKey(JSONMethods.DEFAULT_LANGUAGE))
                {
                    alias = Data.LanguageAliases[JSONMethods.DEFAULT_LANGUAGE];
                    fullname = alias.display_name;
                }
                else
                    alias = new LanguageAliasSet(fullname, fullname, new string[0]);
                if (Data.Languages.TryGetValue(player.Player.channel.owner.playerID.steamID.m_SteamID, out string oldLang))
                {
                    if (!Data.LanguageAliases.TryGetValue(oldLang, out LanguageAliasSet oldSet))
                        oldSet = new LanguageAliasSet(oldLang, oldLang, new string[0]);
                    if (oldLang == JSONMethods.DEFAULT_LANGUAGE)
                        player.SendChat("reset_language_not_needed", fullname);
                    else
                    {
                        JSONMethods.SetLanguage(player.Player.channel.owner.playerID.steamID.m_SteamID, JSONMethods.DEFAULT_LANGUAGE);
                        ActionLog.Add(EActionLogType.CHANGE_LANGUAGE, oldLang + " >> " + JSONMethods.DEFAULT_LANGUAGE, player.CSteamID.m_SteamID);
                        if (OnPlayerChangedLanguage != null)
                            OnPlayerChangedLanguage.Invoke(player, oldSet, alias);
                        player.SendChat("reset_language", fullname);
                    }
                }
                else
                    player.SendChat("reset_language_not_needed", fullname);
            }
            else
            {
                if (!Data.Languages.TryGetValue(player.Player.channel.owner.playerID.steamID.m_SteamID, out string oldLang))
                    oldLang = JSONMethods.DEFAULT_LANGUAGE;
                if (!Data.LanguageAliases.TryGetValue(oldLang, out LanguageAliasSet oldSet))
                    oldSet = new LanguageAliasSet(oldLang, oldLang, new string[0]);
                string langInput = op.Trim();
                bool found = false;
                if (!Data.LanguageAliases.TryGetValue(langInput, out LanguageAliasSet aliases))
                {
                    IEnumerator<LanguageAliasSet> sets = Data.LanguageAliases.Values.GetEnumerator();
                    found = sets.MoveNext();
                    while (found)
                    {
                        if (sets.Current.key == langInput || sets.Current.values.Contains(langInput))
                        {
                            aliases = sets.Current;
                            break;
                        }
                        found = sets.MoveNext();
                    }
                    sets.Dispose();
                }
                else found = true;

                if (found && aliases.key != null && aliases.values != null && aliases.display_name != null)
                {
                    if (oldLang == aliases.key)
                        player.SendChat("change_language_not_needed", aliases.display_name);
                    else
                    {
                        JSONMethods.SetLanguage(player.Player.channel.owner.playerID.steamID.m_SteamID, aliases.key);
                        ActionLog.Add(EActionLogType.CHANGE_LANGUAGE, oldLang + " >> " + aliases.key, player.CSteamID.m_SteamID);
                        if (OnPlayerChangedLanguage != null)
                            OnPlayerChangedLanguage.Invoke(player, oldSet, aliases);
                        player.SendChat("changed_language", aliases.display_name);
                    }
                }
                else
                {
                    player.SendChat("dont_have_language", langInput);
                }
            }
        }
        else
        {
            player.SendChat("reset_language_how");
        }
    }
}