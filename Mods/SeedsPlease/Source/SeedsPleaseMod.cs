﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;
using RimWorld;
using Verse;

namespace SeedsPlease
{
    public class SeedsPleaseMod : Mod
    {
        internal static readonly List<string> knownPrefixes = new List<string>() {
            "VG_Plant", "VGP_", "RC2_", "RC_Plant", "TKKN_Plant", "TKKN_", "TM_", "Plant_", "WildPlant", "Wild", "Plant", "tree", "Tree"
        };

        public SeedsPleaseMod(ModContentPack content) : base(content)
        {
            new Harmony("rimworld.seedsplease").PatchAll();
        }
    }

    [HarmonyPatch (typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PostResolve))]
    static class DefGenerator_GenerateImpliedDefs_PostResolve_Patch
    {
        static void Postfix() {
            var report = new System.Text.StringBuilder();
            if (SeedDef.AddMissingSeeds(report)) {
                ResourceCounter.ResetDefs();

                Log.Warning("SeedsPlease :: Some Seeds were autogenerated." +
                	" Don't rely on autogenerated seeds," +
                	" share the generated xml for proper support.\n\n" +
                     report);
            }
        }
    }

    [HarmonyPatch (typeof(Command_SetPlantToGrow), "IsPlantAvailable")]
    static class Command_IsPlantAvailable_Patch
    {
        public static void Postfix(ThingDef plantDef, Map map, ref bool __result)
        {
            if (__result && plantDef?.blueprintDef is SeedDef) {
                __result = map.listerThings.ThingsOfDef(plantDef.blueprintDef).Count > 0;
            }
        }
    }

    [HarmonyPatch]
    static class Command_IsPlantAvailable_PatchForDubs
    {
        static MethodBase target;

        static bool Prepare()
        {
            Type type;

            var mod = LoadedModManager.RunningMods.FirstOrDefault(m => m.Name == "Dubs Mint Menus");
            if (mod == null) {
                return false;
            }

            type = mod.assemblies.loadedAssemblies
                        .FirstOrDefault(a => a.GetName().Name == "DubsMintMenus")?
                        .GetType("DubsMintMenus.Dialog_FancyDanPlantSetterBob");

            if (type == null) {
                Log.Warning("SeedsPlease :: Can't patch DubsMintMenu. No Dialog_FancyDanPlantSetterBob");

                return false;
            }

            target = AccessTools.DeclaredMethod(type, "IsPlantAvailable");

            if (target == null) {
                Log.Warning("SeedsPlease :: Can't patch DubsMintMenu. No IsPlantAvailable");

                return false;
            }

            return true;
        }

        static MethodBase TargetMethod()
        {
            return target;
        }

        static void Postfix(ThingDef plantDef, Map map, ref bool __result)
        {
            Command_IsPlantAvailable_Patch.Postfix(plantDef, map, ref __result);
        }
    }

    [HarmonyPatch]
    static class JobDriver_SowAll_CanStart_Patch
    {
        static MethodBase target;

        static bool Prepare()
        {
            var mod = LoadedModManager.RunningMods.FirstOrDefault(m => m.Name == "Achtung!");
            if (mod == null) {
                return false;
            }

            var type = mod.assemblies.loadedAssemblies
                        .FirstOrDefault(a => a.GetName().Name == "AchtungMod")?
                        .GetType("AchtungMod.JobDriver_SowAll");

            if (type == null) {
                Log.Warning("SeedsPlease :: Can't patch Achtung! No JobDriver_SowAll");

                return false;
            }

            target = AccessTools.DeclaredMethod(type, "CanStart");
            if (target == null) {
                Log.Warning("SeedsPlease :: Can't patch Achtung! No JobDriver_SowAll.CanStart");

                return false;
            }

            return true;
        }

        static MethodBase TargetMethod()
        {
            return target;
        }

        static bool Prefix()
        {
            return false;
        }
    }
}