﻿using BattleTech;
using Harmony;
using IRBTModUtils.Extension;
using System;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Patches
{

    [HarmonyPatch(typeof(AbstractActor), "InitEffectStats")]
    static class AbstractActor_InitEffectStats
    {
        static void Postfix(AbstractActor __instance)
        {
            Mod.Log.Debug?.Write($"AA:IES entered- setting CanShootAfterSprinting for actor:{__instance.DisplayName}");
            __instance.StatCollection.Set(ModStats.CanShootAfterSprinting, true);
        }
    }

    [HarmonyPatch(typeof(AbstractActor), "OnNewRound")]
    [HarmonyPatch(new Type[] { typeof(int) })]
    static class AbstractActor_OnNewRound
    {
        static void Postfix(AbstractActor __instance)
        {
            Mod.ActivationLog.Debug?.Write($"AA:ONR entered - for actor: {__instance.DisplayName} with TD.IsInterleaved: {__instance.Combat.TurnDirector.IsInterleaved}. " +
                $"Setting CanShootAfterSprinting: {__instance.Combat.TurnDirector.IsInterleaved}");
            //This is an easy place to put this where it will always be checked. This is the key to full non-interleaved combat.
            __instance.StatCollection.Set(ModStats.CanShootAfterSprinting, __instance.Combat.TurnDirector.IsInterleaved);

            // Invalidate their melee state
            ModState.InvalidateMeleeStates(__instance);
        }
    }

    [HarmonyPatch(typeof(AbstractActor), "OnRecomputePathing")]
    static class AbstractActor_OnRecomputePathing
    {
        static void Prefix(AbstractActor __instance)
        {
            Mod.MoveLog.Info?.Write($"Recomputing pathing for actor: {__instance.DistinctId()}");
        }

        static void Postfix(AbstractActor __instance)
        {
            if (__instance.Pathing != null)
            {
                Traverse pathingT = Traverse.Create(__instance.Pathing);

                Traverse walkGridT = pathingT.Property("WalkingGrid");
                PathNodeGrid walkGrid = walkGridT.GetValue<PathNodeGrid>();
                Traverse sprintGridT = pathingT.Property("SprintingGrid");
                PathNodeGrid sprintGrid = sprintGridT.GetValue<PathNodeGrid>();
                Traverse backwardGridT = pathingT.Property("BackwardGrid");
                PathNodeGrid backwardGrid = backwardGridT.GetValue<PathNodeGrid>();

                Mod.MoveLog.Info?.Write($" -- after aa:orp reset, actor: {__instance.DistinctId()} has costLeft: {__instance?.Pathing.CostLeft}  " +
                    $"maxDistance => walk: {walkGrid?.MaxDistance}  sprint: {sprintGrid?.MaxDistance}  backwards: {backwardGrid?.MaxDistance}");
            }

        }
    }

    [HarmonyPatch(typeof(AbstractActor), "ResetPathing")]
    static class AbstractActor_ResetPathing
    {
        static void Prefix(AbstractActor __instance)
        {
            Mod.MoveLog.Info?.Write($"Resetting pathing for actor: {__instance.DistinctId()}");
        }

        static void Postfix(AbstractActor __instance)
        {
            if (__instance.Pathing != null)
            {
                Traverse pathingT = Traverse.Create(__instance.Pathing);

                Traverse walkGridT = pathingT.Property("WalkingGrid");
                PathNodeGrid walkGrid = walkGridT.GetValue<PathNodeGrid>();
                Traverse sprintGridT = pathingT.Property("SprintingGrid");
                PathNodeGrid sprintGrid = sprintGridT.GetValue<PathNodeGrid>();
                Traverse backwardGridT = pathingT.Property("BackwardGrid");
                PathNodeGrid backwardGrid = backwardGridT.GetValue<PathNodeGrid>();

                Mod.MoveLog.Info?.Write($" -- after aa:rp reset, actor: {__instance.DistinctId()} has costLeft: {__instance?.Pathing.CostLeft}  " +
                    $"maxDistance => walk: {walkGrid?.MaxDistance}  sprint: {sprintGrid?.MaxDistance}  backwards: {backwardGrid?.MaxDistance}");
            }
        }
    }

    [HarmonyPatch(typeof(Pathing), "ResetPathGrid")]
    static class Pathing_ResetPathGrid
    {
        static void Prefix(Pathing __instance, Vector3 origin, float beginAngle, AbstractActor actor, bool justStoodUp)
        {
            Mod.MoveLog.Info?.Write($"Resetting path grid for actor: {actor.DistinctId()} for origin: {origin} and beginAngle: {beginAngle}");
        }

        static void Postfix(Pathing __instance, Vector3 origin, float beginAngle, AbstractActor actor, bool justStoodUp)
        {

            Traverse instanceT = Traverse.Create(__instance);
            
            Traverse walkGridT = instanceT.Property("WalkingGrid");
            PathNodeGrid walkGrid = walkGridT.GetValue<PathNodeGrid>();
            Traverse sprintGridT = instanceT.Property("SprintingGrid");
            PathNodeGrid sprintGrid = sprintGridT.GetValue<PathNodeGrid>();
            Traverse backwardGridT = instanceT.Property("BackwardGrid");
            PathNodeGrid backwardGrid = backwardGridT.GetValue<PathNodeGrid>();

            Mod.MoveLog.Info?.Write($" -- after p:rpg reset, actor: {actor.DistinctId()} has costLeft: {__instance.CostLeft}  " +
                $"maxDistance => walk: {walkGrid?.MaxDistance}  sprint: {sprintGrid?.MaxDistance}  backwards: {backwardGrid?.MaxDistance}");
        }
    }
}
