using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Reflection;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class VehicleExplodeAddInstigatorPatch : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(InteractableVehicle).GetMethod("explode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for saving vehicle explode instigator.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("explode")
                .DeclaredIn<InteractableCharge>(isStatic: false)
                .WithNoParameters()
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for saving vehicle explode instigator.", _target);
        _target = null;
    }

    // major tech debt: This prefix patch is doing too much
    
    // SDG.Unturned.InteractableVehicle
    /// <summary>
    /// Overriding prefix of <see cref="InteractableVehicle.explode"/> to set an instigator.
    /// </summary>
    private static bool Prefix(InteractableVehicle __instance)
    {
        WarfareVehicle vehicle = WarfareModule.Singleton.ServiceProvider.Resolve<VehicleService>().GetVehicle(__instance);

        EDamageOrigin lastDamageType = vehicle.DamageTracker.LatestDamageCause.GetValueOrDefault(EDamageOrigin.Unknown);
        if (lastDamageType == EDamageOrigin.Unknown)
            return true;

        CSteamID instigator2;
        switch (lastDamageType)
        {
            // no one at fault
            default:
            case EDamageOrigin.VehicleDecay:
                instigator2 = default;
                break;

            // blame driver
            case EDamageOrigin.Vehicle_Collision_Self_Damage:
            case EDamageOrigin.Zombie_Swipe:
            case EDamageOrigin.Mega_Zombie_Boulder:
            case EDamageOrigin.Animal_Attack:
            case EDamageOrigin.Zombie_Electric_Shock:
            case EDamageOrigin.Zombie_Stomp:
            case EDamageOrigin.Zombie_Fire_Breath:
            case EDamageOrigin.Radioactive_Zombie_Explosion:
            case EDamageOrigin.Flamable_Zombie_Explosion:
            case EDamageOrigin.ExplosionSpawnerComponent:
                if (__instance.passengers.Length > 0)
                {
                    if (__instance.passengers[0].player != null)
                    {
                        instigator2 = __instance.passengers[0].player.playerID.steamID;
                    }
                    // no current driver, check if the last driver exited the vehicle within the last 30 seconds
                    else if (vehicle.TranportTracker.LastKnownDriver.HasValue && (DateTime.UtcNow - vehicle.TranportTracker.LastKnownDriverExitTime.GetValueOrDefault()).TotalSeconds <= 30f)
                    {
                        instigator2 = vehicle.TranportTracker.LastKnownDriver.Value;
                    }
                    else instigator2 = CSteamID.Nil;
                }
                else
                {
                    instigator2 = CSteamID.Nil;
                }
                break;

            // use stored instigator
            case EDamageOrigin.Grenade_Explosion:
            case EDamageOrigin.Rocket_Explosion:
            case EDamageOrigin.Vehicle_Explosion:
            case EDamageOrigin.Useable_Gun:
            case EDamageOrigin.Useable_Melee:
            case EDamageOrigin.Bullet_Explosion:
            case EDamageOrigin.Food_Explosion:
            case EDamageOrigin.Trap_Explosion:
                instigator2 = vehicle.DamageTracker.LastKnownDamageInstigator.GetValueOrDefault();
                break;
        }

        PlayerDeathTrackingComponent? data = null;
        if (instigator2 != CSteamID.Nil)
        {
            Player? player = PlayerTool.getPlayer(instigator2);
            if (player != null)
            {
                data = PlayerDeathTrackingComponent.GetOrAdd(player);
                data.LastVehicleExploded = vehicle;
            }
        }

        Vector3 force = new Vector3(
            RandomUtility.GetFloat(__instance.asset.minExplosionForce.x, __instance.asset.maxExplosionForce.x),
            RandomUtility.GetFloat(__instance.asset.minExplosionForce.y, __instance.asset.maxExplosionForce.y),
            RandomUtility.GetFloat(__instance.asset.minExplosionForce.z, __instance.asset.maxExplosionForce.z)
        );

        __instance.GetComponent<Rigidbody>().AddForce(force);
        __instance.GetComponent<Rigidbody>().AddTorque(16f, 0.0f, 0.0f);

        if (__instance.asset.ShouldExplosionCauseDamage)
        {
            DamageTool.explode(__instance.transform.position, 8f, EDeathCause.VEHICLE,
                instigator2, 200f, 200f, 200f, 0.0f, 0.0f, 500f, 2000f, 500f, out _,
                damageOrigin: EDamageOrigin.Vehicle_Explosion);
        }

        for (int index = 0; index < __instance.passengers.Length; ++index)
        {
            Passenger passenger = __instance.passengers[index];
            if (passenger.player != null && passenger.player.player != null && !passenger.player.player.life.isDead)
            {
                if (__instance.asset.ShouldExplosionCauseDamage)
                    passenger.player.player.life.askDamage(101, Vector3.up * 101f, EDeathCause.VEHICLE, ELimb.SPINE, instigator2, out _);
                else
                    VehicleManager.forceRemovePlayer(__instance, passenger.player.playerID.steamID);
            }
        }

        // we dont need scrap items
        // __instance.DropScrapItems();
        
        VehicleManager.sendVehicleExploded(__instance);
        
        // the original method drops trunk items at the beginning, however we want to do this after vehicle exploding events are invoked
        // in case any event listeners need to remove their items
        __instance.dropTrunkItems();
        
        EffectAsset effect = __instance.asset.FindExplosionEffectAsset();
        if (effect != null)
        {
            EffectUtility.TriggerEffect(effect, Provider.GatherRemoteClientConnections(), __instance.transform.position, true);
        }

        if (data != null)
        {
            data.LastVehicleExploded = null;
        }
        return false;
    }
}