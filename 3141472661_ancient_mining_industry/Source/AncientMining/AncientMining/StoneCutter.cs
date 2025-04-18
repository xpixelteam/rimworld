using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AncientMining
{
    [StaticConstructorOnStartup]
    public class CompAutoStoneCutter : ThingComp, IThingHolder
    {
        CompProperties_AutoStoneCutter Props => props as CompProperties_AutoStoneCutter;

        private CompFlickable flickComp;

        private CompPowerTrader powerComp;

        private CompRefuelable fuelComp;

        protected ThingOwner stoneStorage;

        private Sustainer Sustainer;

        List<IntVec3> pickUpCells;

        public int worksDone;

        public int chunkCheckDelay;

        public CompAutoStoneCutter()
        {
            stoneStorage = new ThingOwner<Thing>(this, oneStackOnly: false);
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return stoneStorage;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            flickComp = parent.GetComp<CompFlickable>();
            powerComp = parent.GetComp<CompPowerTrader>();
            fuelComp = parent.GetComp<CompRefuelable>();
            pickUpCells = GenAdjFast.AdjacentCells8Way(parent.Position, parent.Rotation, parent.RotatedSize);
            TryPickUpChunk();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref worksDone, "worksDone", 0);
            Scribe_Deep.Look(ref stoneStorage, "innerContainer", this);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (shouldBeOn)
            {
                TryPickUpChunk();
                WorkDone(1);
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            GenDraw.DrawFieldEdges(GenAdjFast.AdjacentCells8Way(parent.Position, parent.Rotation, parent.RotatedSize));
        }

        public override void CompTickRare()
        {
            base.CompTickRare(); if (shouldBeOn)
            {
                TryPickUpChunk();
                WorkDone(GenTicks.TickRareInterval);
            }
        }

        public override void CompTickLong()
        {
            base.CompTickLong(); if (shouldBeOn)
            {
                TryPickUpChunk();
                WorkDone(GenTicks.TickLongInterval);
            }
        }

        public virtual void WorkDone(int workAmount)
        {
            chunkCheckDelay -= workAmount;
            if (stoneStorage.NullOrEmpty())
            {
                return;
            }
            worksDone += workAmount;
            if (Sustainer == null)
            {
                Sustainer = Props.sustainer?.TrySpawnSustainer(SoundInfo.InMap(parent));
            }
            Sustainer.Maintain();
            if (worksDone >= Props.workAmount)
            {
                CutChunk();
                worksDone = 0;
            }
        }

        public override string CompInspectStringExtra()
        {
            return worksDone.ToString();
        }

        public void CutChunk()
        {
            Thing thing = stoneStorage.First();
            List<Thing> Products = new List<Thing>();
            Products = thing.ButcherProducts(null, Props.efficiency).ToList();
            thing.Destroy();
            if (Rand.Chance(Props.byProductChance))
            {
                ThingDef thingDef = DefDatabase<ThingDef>.AllDefs.RandomElementByWeight((ThingDef def) => def.deepCommonality);
                Thing t = ThingMaker.MakeThing(thingDef);
                t.stackCount = (int)Math.Max(1, thingDef.deepLumpSizeRange.RandomInRange * Props.byProductModifier);
                Products.Add(t);
            }
            foreach (Thing product in Products)
            {
                GenPlace.TryPlaceThing(product, parent.Position, parent.Map, ThingPlaceMode.Near, null, (IntVec3 p) => p != parent.Position && p != parent.InteractionCell);
            }
            if (Sustainer != null)
            {
                Sustainer.End();
                Sustainer = null;
            }
            TryPickUpChunk(true);
        }

        public void TryPickUpChunk(bool forced = false)
        {
            if (shouldBeOn && stoneStorage.NullOrEmpty() && (chunkCheckDelay < 0 || forced))
            {
                Thing t = TryFindChunk();
                if (t == null)
                {
                    chunkCheckDelay = Props.chunkCheckDelay;
                    return;
                }
                t.DeSpawn();
                stoneStorage.TryAddOrTransfer(t);
            }
        }

        public Thing TryFindChunk()
        {
            foreach (var cell in pickUpCells)
            {
                foreach (ThingDef def in StoneCutterUtil.AllowedDefs)
                {
                    Thing t = cell.GetFirstThing(parent.Map, def);
                    if (t != null)
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        private static readonly Material stoneMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f));

        private static readonly Material filledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f));

        private static readonly Material emptyMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f));

        private static readonly Vector2 BarSize = new Vector2(1f, 0.14f);

        private Material StoneMat
        {
            get
            {
                if (stoneStorage.Any() && stoneStorage.First().DrawColor != Color.white)
                {
                    return SolidColorMaterials.SimpleSolidColorMaterial(stoneStorage.First().DrawColor);
                }
                return stoneMat;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (!stoneStorage.NullOrEmpty())
            {
                GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
                r.center = parent.DrawPos + Vector3.up * 0.1f + Vector3.back * 0.1f;
                r.size = BarSize;
                r.fillPercent = 1;
                r.filledMat = StoneMat;
                r.unfilledMat = emptyMat;
                r.margin = 0.15f;
                GenDraw.DrawFillableBar(r);
            }
            if (worksDone > 0)
            {
                GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
                r.center = parent.DrawPos + Vector3.up * 0.1f + Vector3.forward * 0.1f;
                r.size = BarSize;
                r.fillPercent = (float)worksDone / Props.workAmount;
                r.filledMat = filledMat;
                r.unfilledMat = emptyMat;
                r.margin = 0.15f;
                GenDraw.DrawFillableBar(r);
            }
        }

        public bool shouldBeOn
        {
            get
            {
                if (flickComp != null && !flickComp.SwitchIsOn)
                {
                    return false;
                }
                if (powerComp != null && !powerComp.PowerOn)
                {
                    return false;
                }
                if (fuelComp != null && !fuelComp.HasFuel)
                {
                    return false;
                }
                return true;
            }
        }
    }

    public class CompProperties_AutoStoneCutter : CompProperties
    {
        public float efficiency = 0.5f;

        public int workAmount = 6000;

        public int chunkCheckDelay = 600;

        public float byProductChance = 0.01f;

        public float byProductModifier = 1f;

        public SoundDef sustainer;

        public CompProperties_AutoStoneCutter()
        {
            compClass = typeof(CompAutoStoneCutter);
        }
    }
    [StaticConstructorOnStartup]
    public static class StoneCutterUtil
    {
        static StoneCutterUtil()
        {
            RefreshAllowedDefs();
        }

        private static IEnumerable<ThingDef> allowedDefs;

        public static List<ThingDef> AllowedDefs
        {
            get
            {
                if (allowedDefs == null)
                {
                    RefreshAllowedDefs();
                }
                return allowedDefs.ToList();
            }
        }

        public static void RefreshAllowedDefs()
        {
            List<ThingDef> allDefsListForReading5 = DefDatabase<ThingDef>.AllDefsListForReading;
            foreach (var def in allDefsListForReading5)
            {
                if (IsChunk(def.thingCategories))
                {
                    allowedDefs = allowedDefs.AddItem(def);
                }
            }
        }

        public static bool IsChunk(List<ThingCategoryDef> thingCategory)
        {
            foreach (ThingCategoryDef x in thingCategory ?? Enumerable.Empty<ThingCategoryDef>())
            {
                if (IsChunk(x)) return true;
            }
            return false;
        }

        public static bool IsChunk(ThingCategoryDef thingCategory)
        {
            if (thingCategory == ThingCategoryDefOf.StoneChunks || ThingCategoryDefOf.StoneChunks.childCategories.Contains(thingCategory)) return true;
            return false;
        }
    }
}
