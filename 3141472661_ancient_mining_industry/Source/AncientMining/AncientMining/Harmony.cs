using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncientMining
{
    [UsedImplicitly]
    [StaticConstructorOnStartup]
    public class PatchMain
    {
        private static Harmony harmonyInstance;

        internal static Harmony HarmonyInstance
        {
            get
            {
                if (harmonyInstance == null)
                {
                    harmonyInstance = new Harmony("BreadMo_AncientMining");
                }
                return harmonyInstance;
            }
        }

        static PatchMain()
        {
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());


        }
    }

    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(Mineable), "PreApplyDamage")]
    static class PreApplyDamage_PostFix
    {
        [HarmonyPostfix]
        static void Postfix(ref DamageInfo dinfo, Mineable __instance)
        {
            if (__instance.def.building.mineableThing != null && __instance.def.building.mineableYieldWasteable && dinfo.Def == DamageDefOf.Mining && dinfo.Instigator != null && !(dinfo.Instigator is Pawn))
            {
                FieldInfo P_material = typeof(Mineable).GetField("yieldPct", BindingFlags.Instance | BindingFlags.NonPublic);
                P_material.SetValue(__instance, (float)P_material.GetValue(__instance) + (float)Mathf.Min(dinfo.Amount, __instance.HitPoints) / (float)__instance.MaxHitPoints);
            }
        }
    }
}
