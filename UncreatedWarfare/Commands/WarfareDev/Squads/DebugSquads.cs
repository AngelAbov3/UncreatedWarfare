﻿using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("squads", "sq"), SubCommandOf(typeof(WarfareDevCommand))]
internal sealed class DebugSquads : ICommand;
