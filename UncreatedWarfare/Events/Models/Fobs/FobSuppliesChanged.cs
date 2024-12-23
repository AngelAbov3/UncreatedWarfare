﻿using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.FOBs.SupplyCrates;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Fobs;

/// <summary>
/// Event listener args which fires after supplies are added or removed from a <see cref="IResourceFob"/>.
/// </summary>
public class FobSuppliesChanged
{
    /// <summary>
    /// The <see cref="IResourceFob"/> where supplies were added or removed.
    /// </summary>
    public required IResourceFob Fob { get; init; }
    /// <summary>
    /// The number of supplies that were added (positive) or removed (negative).
    /// </summary>
    public required float AmountDelta { get; init; }
    /// <summary>
    /// The type of supplies that were added or removed.
    /// </summary>
    public required SupplyType SupplyType { get; init; }
    /// <summary>
    /// The reason the supplies were added or removed.
    /// </summary>
    public required SupplyChangeReason ChangeReason { get; init; }
    /// <summary>
    /// The player who resupplied this fob, if this event was invoked due to resupplying a fob.
    /// </summary>
    public required WarfarePlayer? Resupplier { get; init; }
}
