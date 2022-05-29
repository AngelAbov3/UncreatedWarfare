﻿using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Events.Players;
public class PlayerDied : PlayerEvent
{
    public EDeathCause Cause { get; internal set; }
    public ELimb Limb { get; internal set; }
    public UCPlayer? Killer { get; internal set; }
    public CSteamID Intigator { get; internal set; }
    public PlayerDied(UCPlayer player) : base(player)
    {
    }
}
