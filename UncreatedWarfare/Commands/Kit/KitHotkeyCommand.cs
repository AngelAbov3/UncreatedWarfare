﻿using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("hotkey", "keybind", "bind"), SubCommandOf(typeof(KitCommand)), RedirectCommandTo(typeof(KitHotkeyAddCommand))]
internal sealed class KitHotkeyCommand : ICommand;
