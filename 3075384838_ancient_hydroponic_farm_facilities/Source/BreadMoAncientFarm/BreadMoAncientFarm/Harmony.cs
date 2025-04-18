using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PipeSystem;
using RimWorld;
using HarmonyLib;
using Verse;

namespace BreadMoAncientFarm
{
    [UsedImplicitly]
    [StaticConstructorOnStartup]
    public class HarmonyMain
    {
        private static Harmony harmonyInstance;

        internal static Harmony HarmonyInstance
        {
            get
            {
                if (harmonyInstance == null)
                {
                    harmonyInstance = new Harmony("BreadMoAncientFarm");
                }
                return harmonyInstance;
            }
        }

        static HarmonyMain()
        {
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [StaticConstructorOnStartup]
    public class Harmony_PlantGrower
    {
        [HarmonyPatch(typeof(Building_PlantGrower), "TickRare")]
        static class TickRare_PostFix
        {
            [HarmonyPostfix]
            static void PostFix(Building_PlantGrower __instance)
            {
                CompFertilizer comp = __instance.TryGetComp<CompFertilizer>();
                if (comp != null)
                {
                    comp.CompTickRare();
                }
            }
        }
    }
}
