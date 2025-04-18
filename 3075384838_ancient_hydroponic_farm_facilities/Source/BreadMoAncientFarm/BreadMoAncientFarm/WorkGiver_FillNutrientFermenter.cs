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
using UnityEngine;
using Verse.AI;

namespace BreadMoAncientFarm
{
    public class WorkGiver_FillNutrientFermenter : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(BMAFDefOf.NutrientSolutionFermenter);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }
            CompNutrientToResource CompNutrientToResource = t.TryGetComp<CompNutrientToResource>();
            if (CompNutrientToResource == null || !CompNutrientToResource.CanBeOn() || CompNutrientToResource.nutrition >= CompNutrientToResource.Props.nutritionStorage || CompNutrientToResource.innerContainer.Any() || (!forced && !CompNutrientToResource.autoLoad))
            {
                return false;
            }
            if (t.IsBurning())
            {
                return false;
            }
            if (FindNutrition(pawn, CompNutrientToResource).Thing == null)
            {
                JobFailReason.Is("NoFood".Translate());
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompNutrientToResource CompNutrientToResource = t.TryGetComp<CompNutrientToResource>();
            if (CompNutrientToResource == null)
            {
                return null;
            }
            if (CompNutrientToResource.nutrition < CompNutrientToResource.Props.nutritionStorage)
            {
                ThingCount thingCount = FindNutrition(pawn, CompNutrientToResource);
                if (thingCount.Thing != null)
                {
                    Job job = HaulAIUtility.HaulToContainerJob(pawn, thingCount.Thing, t);
                    job.count = Mathf.Min(job.count, thingCount.Count);
                    return job;
                }
            }
            return null;
        }

        private ThingCount FindNutrition(Pawn pawn, CompNutrientToResource pod)
        {
            Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, Validator);
            if (thing == null)
            {
                return default(ThingCount);
            }
            int b = Mathf.CeilToInt((pod.Props.nutritionStorage - pod.nutrition) / thing.GetStatValue(StatDefOf.Nutrition));
            return new ThingCount(thing, Mathf.Min(thing.stackCount, b));
            bool Validator(Thing x)
            {
                if (x.IsForbidden(pawn) || !pawn.CanReserve(x))
                {
                    return false;
                }
                if (!pod.CanAcceptNutrition(x))
                {
                    return false;
                }
                return true;
            }
        }
    }

}
