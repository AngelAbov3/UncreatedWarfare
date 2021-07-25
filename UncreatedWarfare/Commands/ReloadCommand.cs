﻿using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.XP;
using Uncreated;
using System.Reflection;

namespace Uncreated.Warfare.Commands
{
    public class ReloadCommand : IRocketCommand
    {
        public static event Networking.EmptyTaskDelegate OnTranslationsReloaded;
        public static event Networking.EmptyTaskDelegate OnFlagsReloaded;
        public AllowedCaller AllowedCaller => AllowedCaller.Both;
        public string Name => "reload";
        public string Help => "Reload certain parts of UCWarfare.";
        public string Syntax => "/reload [module]";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.reload" };
        public async void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = caller as UnturnedPlayer;
            bool isConsole = caller.DisplayName == "Console";
            string cmd = command.Length == 0 ? string.Empty : command[0].ToLower();
            if (command.Length == 0 || (command.Length == 1 && cmd == "all"))
            {
                if (isConsole || player.HasPermission("uc.reload.all"))
                {
                    await ReloadTranslations();
                    ReloadAllConfigFiles();
                    ReloadConfig();
                    await ReloadKits();
                    await ReloadFlags();
                    await ReloadTCPServer(isConsole ? 0 : player.CSteamID.m_SteamID, "Reload Command");

                    SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();

                    player?.SendChat("reload_reloaded_all");
                    await rtn;
                }
                else
                    player?.Player.SendChat("no_permissions");
            }
            else
            {
                if (cmd == "config")
                {
                    if (isConsole || player.HasPermission("uc.reload.config") || player.HasPermission("uc.reload.all"))
                    {
                        if (isConsole) F.Log(F.Translate("reload_reloaded_config", 0, out _));
                        else player.SendChat("reload_reloaded_config");
                        ReloadConfig();
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
                else if (cmd == "translations" || cmd == "lang")
                {
                    if (isConsole || player.HasPermission("uc.reload.translations") || player.HasPermission("uc.reload.all"))
                    {
                        await ReloadTranslations();
                        if (isConsole) F.Log(F.Translate("reload_reloaded_lang", 0, out _));
                        else player.SendChat("reload_reloaded_lang");
                    }
                    else
                        player.Player.SendChat("no_permissions");
                } else if (cmd == "flags")
                {
                    if (isConsole || player.HasPermission("uc.reload.flags") || player.HasPermission("uc.reload.all"))
                    {
                        await ReloadFlags();
                        if (isConsole) F.Log(F.Translate("reload_reloaded_flags", 0, out _));
                        else player.SendChat("reload_reloaded_flags");
                    }
                    else
                        player.Player.SendChat("no_permissions");
                } else if (cmd == "tcp")
                {
                    if (isConsole || player.HasPermission("uc.reload.tcp") || player.HasPermission("uc.reload.all"))
                    {
                        await ReloadTCPServer(isConsole ? 0 : player.CSteamID.m_SteamID, "Reload command.");
                        if (isConsole) F.Log(F.Translate("reload_reloaded_tcp", 0, out _));
                        else player.SendChat("reload_reloaded_tcp");
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
                else if (cmd == "kits")
                {
                    if (isConsole || player.HasPermission("uc.reload.kits") || player.HasPermission("uc.reload.all"))
                    {
                        await ReloadKits();
                        if (isConsole) F.Log(F.Translate("reload_reloaded_kits", 0, out _));
                        else player.SendChat("reload_reloaded_kits");
                    }
                    else
                        player.Player.SendChat("no_permissions");
                }
            }
        }
        internal static void ReloadConfig()
        {
            try
            {
                SquadManager.config.Reload();
                TicketManager.config.Reload();
                XPManager.config.Reload();
                OfficerManager.config.Reload();
                FOBManager.config.Reload();

                UCWarfare.Instance.Configuration.Load();
            }
            catch (Exception ex)
            {
                F.LogError("Execption when reloading config.");
                F.LogError(ex);
            }
        }
        internal static async Task ReloadTranslations()
        {
            try
            {
                Data.LanguageAliases = JSONMethods.LoadLangAliases();
                Data.Languages = JSONMethods.LoadLanguagePreferences();
                Data.Localization = JSONMethods.LoadTranslations(out Data.DeathLocalization, out Data.LimbLocalization);
                Data.Colors = JSONMethods.LoadColors(out Data.ColorsHex);
                if(OnTranslationsReloaded != null)
                    await OnTranslationsReloaded.Invoke();
            }
            catch (Exception ex)
            { 
                F.LogError("Execption when reloading translations.");
                F.LogError(ex);
            }
        }
        internal static async Task ReloadFlags()
        {
            try
            {
                if (Data.Gamemode is Gamemodes.Flags.FlagGamemode flaggm)
                {
                    if (Data.Gamemode is Gamemodes.Flags.TeamCTF.TeamCTF tctf)
                        tctf.ReloadConfig();
                    flaggm.LoadAllFlags();
                    await flaggm.StartNextGame();
                }
                Data.ExtraZones = JSONMethods.LoadExtraZones();
                Data.ExtraPoints = JSONMethods.LoadExtraPoints();
                if(OnFlagsReloaded != null)
                    await OnFlagsReloaded.Invoke();
            }
            catch (Exception ex)
            {
                F.LogError("Execption when reloading flags.");
                F.LogError(ex);
            }
        }
        internal static async Task ReloadTCPServer(ulong admin, string reason)
        {
            try
            {
                if (Networking.TCPClient.I != null && Networking.TCPClient.I.connection != null)
                {
                    await Networking.Client.SendReloading(admin, reason);
                    Networking.TCPClient.I?.Shutdown();
                }
                Data.CancelTcp.Cancel();
                Data.CancelTcp.Token.WaitHandle.WaitOne();
                Data.CancelTcp = new CancellationTokenSource();
                Networking.TCPClient.I = new Networking.TCPClient(UCWarfare.Config.PlayerStatsSettings.TCPServerIP,
                    UCWarfare.Config.PlayerStatsSettings.TCPServerPort, UCWarfare.Config.PlayerStatsSettings.TCPServerIdentity);
                _ = Networking.TCPClient.I.Connect(Data.CancelTcp).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                F.LogError("Execption when reloading TCP client.");
                F.LogError(ex);
            }
        }
        internal static async Task ReloadKits()
        {
            Kits.KitManager.Reload();
            foreach (Kits.RequestSign sign in Kits.RequestSigns.ActiveObjects)
            {
                await sign.InvokeUpdate();
            }
        }
        internal static void ReloadAllConfigFiles()
        {
            try
            {
                IEnumerable<FieldInfo> objects = typeof(Data).GetFields(BindingFlags.Static | BindingFlags.Public).Where(x => x.FieldType.IsClass);
                foreach (FieldInfo obj in objects)
                {
                    try
                    {
                        object o = obj.GetValue(null);
                        IEnumerable<FieldInfo> configfields = o.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).
                            Where(x => x.FieldType.GetInterfaces().Contains(typeof(IConfiguration)));
                        foreach (FieldInfo config in configfields)
                        {
                            IConfiguration c;
                            if (config.IsStatic)
                            {
                                c = (IConfiguration)config.GetValue(null);
                            }
                            else
                            {
                                c = (IConfiguration)config.GetValue(o);
                            }
                            c.Reload();
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception ex)
            {
                F.LogError("Failed to find all objects in type " + typeof(Data).Name);
                F.LogError(ex);
            }
        }
    }
}