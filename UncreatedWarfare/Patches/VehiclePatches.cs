﻿using HarmonyLib;
using SDG.Unturned;
using System;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare;

public static partial class Patches
{
    [HarmonyPatch]
    public class VehiclePatches
    {
        // SDG.Unturned.PlayerAnimator
        /// <summary>
        /// Postfix of <see cref="InteractableVehicle.tryAddPlayer(out byte, Player)"/> to control which seats the player enters.
        /// </summary>
        [HarmonyPatch(typeof(InteractableVehicle), nameof(InteractableVehicle.tryAddPlayer))]
        [HarmonyPostfix]
        static void TryAddPlayerPostfix(ref byte seat, Player player, InteractableVehicle __instance, ref bool __result)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (__result)
            {
                if (VehicleBay.VehicleExists(__instance.asset.GUID, out VehicleData vehicleData))
                {
                    UCPlayer? enterer = UCPlayer.FromPlayer(player);

                    if (enterer != null)
                    {
                        if (VehicleData.IsEmplacement(vehicleData.Type))
                        {
                            if (!VehicleBay.TryGetFirstNonDriverSeat(__instance, out seat))
                            {
                                __result = false;
                                return;
                            }
                        }
                        else if (vehicleData.Type == EVehicleType.JET)
                        {
                            if (VehicleBay.CountCrewmen(__instance, vehicleData) >= 2)
                            {
                                __result = false;
                                return;
                            }
                        }

                        UCPlayer? owner = UCPlayer.FromCSteamID(__instance.lockedOwner);

                        if (vehicleData.RequiredClass != EClass.NONE) // vehicle requires crewman or pilot
                        {
                            if (enterer.KitClass == vehicleData.RequiredClass) // for crewman trying to enter a crewed vehicle
                            {
                                if (seat == 0)
                                {
                                    bool canEnterDriverSeat = owner is null || enterer == owner || VehicleBay.IsOwnerInVehicle(__instance, owner) || (owner is not null && owner.Squad != null && owner.Squad.Members.Contains(enterer) || (owner!.Position - __instance.transform.position).sqrMagnitude > Math.Pow(200, 2));

                                    if (!canEnterDriverSeat)
                                    {
                                        if (!VehicleBay.TryGetFirstNonDriverSeat(__instance, out seat))
                                        {
                                            if (owner!.Squad == null)
                                                enterer.SendChat(T.VehicleWaitForOwner, owner);
                                            else
                                                enterer.SendChat(T.VehicleWaitForOwnerOrSquad, owner, owner.Squad);

                                            __result = false;
                                        }
                                    }
                                }
                            }
                            else // for non crewman trying to enter a crewed vehicle
                            {
                                if (!VehicleBay.TryGetFirstNonCrewSeat(__instance, vehicleData, out seat))
                                {
                                    enterer.SendChat(T.VehicleNoPassengerSeats);
                                    __result = false;
                                }
                            }
                        }
                        else
                        {
                            if (seat == 0)
                            {
                                bool canEnterDriverSeat = owner is null || enterer == owner || (owner.Squad != null && owner.Squad.Members.Contains(enterer)) || (owner.Position - __instance.transform.position).sqrMagnitude > Math.Pow(200, 2) || (vehicleData.Type == EVehicleType.LOGISTICS && FOB.GetNearestFOB(__instance.transform.position, EFOBRadius.FULL_WITH_BUNKER_CHECK, __instance.lockedGroup.m_SteamID) != null);

                                if (!canEnterDriverSeat)
                                {
                                    if (!VehicleBay.TryGetFirstNonDriverSeat(__instance, out seat))
                                    {
                                        if (owner!.Squad == null)
                                            enterer.SendChat(T.VehicleWaitForOwner, owner);
                                        else
                                            enterer.SendChat(T.VehicleWaitForOwnerOrSquad, owner, owner.Squad);

                                        __result = false;
                                    }
                                }

                            }
                        }
                    }
                }
                if (seat == 0 && __instance.transform.TryGetComponent(out VehicleComponent c))
                {
                    c.LastDriver = player.channel.owner.playerID.steamID.m_SteamID;
                    c.LastDriverTime = Time.realtimeSinceStartup;
                }
            }
        }
    }
}
