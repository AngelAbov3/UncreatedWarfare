﻿using Rocket.API;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Teams
{
    class Command_teams : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "teams";
        public string Help => "Pull up the Teams UI";
        public string Syntax => "/teams";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.teams" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);
            if (player == null) return;

            if (!Data.Is(out ITeams teamgm) && teamgm.UseJoinUI)
            {
                player.SendChat("command_e_gamemode");
                return;
            }
            if (!player.OnDuty() && CooldownManager.HasCooldown(player, ECooldownType.CHANGE_TEAMS, out Cooldown cooldown))
            {
                player.SendChat("teams_e_cooldown", cooldown.ToString());
                return;
            }
            ulong team = player.GetTeam();
            if ((team == 1ul || team == 2ul) && !player.Player.IsInMain())
            {
                player.SendChat("teams_e_notinmain");
                return;
            }
            teamgm.JoinManager.JoinLobby(player, true);
            
        }
    }
}
