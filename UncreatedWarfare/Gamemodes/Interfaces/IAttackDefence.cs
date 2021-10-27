﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Gamemodes.Interfaces
{
    public interface IAttackDefence : IGamemode
    {
        ulong AttackingTeam { get; }
        ulong DefendingTeam { get; }
    }
}
