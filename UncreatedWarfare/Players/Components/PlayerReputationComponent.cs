using SDG.Framework.Utilities;
using System;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.Components;

[PlayerComponent]
public class PlayerReputationComponent : IPlayerComponent, IDisposable
{
    private int _pendingReputation;
    public WarfarePlayer Player { get; private set; }
    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        if (isOnJoin)
            TimeUtility.updated += OnUpdate;
    }

    private void OnUpdate()
    {
        int val = Interlocked.Exchange(ref _pendingReputation, 0);
        if (val == 0)
            return;

        ModifyReputationIntl(val);
    }

    /// <summary>
    /// Adds (or subtracts) a certain reputation value to a player.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public void AddReputation(int reputation)
    {
        if (GameThread.IsCurrent)
        {
            int val = Interlocked.Exchange(ref _pendingReputation, 0);
            val += reputation;
            if (val == 0)
                return;

            ModifyReputationIntl(val);
        }
        else if (reputation != 0)
        {
            Interlocked.Add(ref _pendingReputation, reputation);
        }
    }

    /// <summary>
    /// Sets the reputation value for a player.
    /// </summary>
    /// <remarks>Thread-safe</remarks>
    public void SetReputation(int reputation)
    {
        reputation -= Player.UnturnedPlayer.skills.reputation;
        if (GameThread.IsCurrent)
        {
            _pendingReputation = 0;
            ModifyReputationIntl(reputation);
        }
        else
        {
            _pendingReputation = reputation;
        }
    }

    private void ModifyReputationIntl(int deltaReputation)
    {
        if (deltaReputation == 0)
            return;

        Patches.CancelReputationPatch.IsSettingReputation = true;
        try
        {
            Player.UnturnedPlayer.skills.askRep(deltaReputation);
        }
        finally
        {
            Patches.CancelReputationPatch.IsSettingReputation = false;
        }
    }

    public void Dispose()
    {
        TimeUtility.updated -= OnUpdate;
    }
}