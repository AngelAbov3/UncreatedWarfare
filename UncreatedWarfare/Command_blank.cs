﻿using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare.Kits
{
    public class Command_blank : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "com";
        public string Help => "does something";
        public string Syntax => "/com";
        public List<string> Aliases => new List<string>() { "c" };
        public List<string> Permissions => new List<string>() { "com" };
        public void Execute(IRocketPlayer caller, string[] arguments)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            // TODO
        }
    }
}