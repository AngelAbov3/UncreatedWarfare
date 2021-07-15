﻿using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Commands
{
    public delegate Task PlayerChangedLanguageDelegate(UnturnedPlayer player, LanguageAliasSet oldLanguage, LanguageAliasSet newLanguage);
    public class LangCommand : IRocketCommand
    {
        public static event PlayerChangedLanguageDelegate OnPlayerChangedLanguage;
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "lang";
        public string Help => "Switch your language to some of our supported languages.";
        public string Syntax => "/lang";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.lang" };
        public async void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            string op = command.Length > 0 ? command[0].ToLower() : string.Empty;
            if (command.Length == 0)
            {
                StringBuilder sb = new StringBuilder();
                for(int i = 0; i < Data.LanguageAliases.Keys.Count; i++)
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
            } else if (command.Length == 1)
            {
                if(op == "current")
                {
                    string OldLanguage = JSONMethods.DefaultLanguage;
                    if (Data.Languages.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                        OldLanguage = Data.Languages[player.Player.channel.owner.playerID.steamID.m_SteamID];
                    LanguageAliasSet oldSet;
                    if (Data.LanguageAliases.ContainsKey(OldLanguage))
                        oldSet = Data.LanguageAliases[OldLanguage];
                    else
                        oldSet = new LanguageAliasSet(OldLanguage, OldLanguage, new List<string>());

                    player.SendChat("language_current", $"{oldSet.display_name} : {oldSet.key}");
                } else if(op == "reset")
                {
                    string fullname = JSONMethods.DefaultLanguage;
                    LanguageAliasSet alias;
                    if (Data.LanguageAliases.ContainsKey(JSONMethods.DefaultLanguage))
                    {
                        alias = Data.LanguageAliases[JSONMethods.DefaultLanguage];
                        fullname = alias.display_name;
                    } else
                        alias = new LanguageAliasSet(fullname, fullname, new List<string>());
                    if (Data.Languages.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                    {
                        string OldLanguage = Data.Languages[player.Player.channel.owner.playerID.steamID.m_SteamID];
                        LanguageAliasSet oldSet;
                        if (Data.LanguageAliases.ContainsKey(OldLanguage))
                            oldSet = Data.LanguageAliases[OldLanguage];
                        else
                            oldSet = new LanguageAliasSet(OldLanguage, OldLanguage, new List<string>());
                        if (OldLanguage == JSONMethods.DefaultLanguage)
                            player.SendChat("reset_language_not_needed", fullname);
                        else
                        {
                            JSONMethods.SetLanguage(player.Player.channel.owner.playerID.steamID.m_SteamID, JSONMethods.DefaultLanguage);
                            if(OnPlayerChangedLanguage != null)
                                await OnPlayerChangedLanguage.Invoke(player, oldSet, alias);
                            player.SendChat("reset_language", fullname);
                        }
                    } else
                        player.SendChat("reset_language_not_needed", fullname);
                } else
                {
                    string OldLanguage = JSONMethods.DefaultLanguage;
                    if (Data.Languages.ContainsKey(player.Player.channel.owner.playerID.steamID.m_SteamID))
                        OldLanguage = Data.Languages[player.Player.channel.owner.playerID.steamID.m_SteamID];
                    LanguageAliasSet oldSet;
                    if (Data.LanguageAliases.ContainsKey(OldLanguage))
                        oldSet = Data.LanguageAliases[OldLanguage];
                    else
                        oldSet = new LanguageAliasSet(OldLanguage, OldLanguage, new List<string>());
                    string langInput = op.Trim();
                    LanguageAliasSet aliases;
                    if (Data.LanguageAliases.ContainsKey(langInput))
                        aliases = Data.LanguageAliases[langInput];
                    else
                        aliases = Data.LanguageAliases.Values.FirstOrDefault(x => x.values.Contains(langInput));
                    if (!aliases.Equals(default))
                    {
                        if (OldLanguage == aliases.key)
                            player.SendChat("change_language_not_needed", aliases.display_name);
                        else
                        {
                            JSONMethods.SetLanguage(player.Player.channel.owner.playerID.steamID.m_SteamID, aliases.key);
                            if (OnPlayerChangedLanguage != null)
                                await OnPlayerChangedLanguage.Invoke(player, oldSet, aliases);
                            player.SendChat("changed_language", aliases.display_name);
                        }
                    }
                    else
                    {
                        player.SendChat("dont_have_language", langInput);
                    }
                }
            } else
            {
                player.SendChat("reset_language_how");
            }
        }
    }
}