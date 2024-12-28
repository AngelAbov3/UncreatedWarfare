using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Components;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles.Damage;

public class AdvancedVehicleDamageApplier
{
    private readonly ILogger _logger;
    private Queue<AdvancedDamagePending> _damageQueue;

    public AdvancedVehicleDamageApplier()
    {
        _logger = WarfareModule.Singleton.GlobalLogger;
        _damageQueue = new Queue<AdvancedDamagePending>();
    }

    public void RegisterPendingDamageForNextEvent(float damageMultiplier)
    {
        _damageQueue.Enqueue(new AdvancedDamagePending
        {
            Multiplier = damageMultiplier,
            Timestamp = DateTime.Now
        });
        _logger.LogDebug($"Registered pending advanced vehicle damage multiplier of {damageMultiplier} for vehicle. Multipliers queued for this vehicle: {_damageQueue.Count}");
    }

    public float ApplyLatestRelevantDamageMultiplier()
    {
        while (_damageQueue.Count > 0)
        {
            AdvancedDamagePending pendingDamage = _damageQueue.Dequeue();
            TimeSpan timeElapsedSinceDamageRegistered = DateTime.Now - pendingDamage.Timestamp;
            if (timeElapsedSinceDamageRegistered.TotalSeconds > 0.1f) // do not apply pending that's too old (older than a fraction of a second)
                continue;
            
            _logger.LogDebug($"Applying advanced vehicle damage multiplier of {pendingDamage.Multiplier}.");
            return pendingDamage.Multiplier;
        }
        
        return 1;
    }
    public static float GetComponentDamageMultiplier(InputInfo hitInfo)
    {
        if (hitInfo.colliderTransform == null)
            return 1;
        
        return GetComponentDamageMultiplier(hitInfo.colliderTransform);
    }
    
    public static float GetComponentDamageMultiplier(Transform colliderTransform)
    {
        if (!colliderTransform.name.StartsWith("damage_"))
            return 1;

        if (!float.TryParse(colliderTransform.name[7..], NumberStyles.Any,
                CultureInfo.InvariantCulture, out float multiplier))
            return 1;

        return multiplier;
    }

    public struct AdvancedDamagePending
    {
        public required float Multiplier { get; init; }
        public required DateTime Timestamp { get; init; }
    }
}