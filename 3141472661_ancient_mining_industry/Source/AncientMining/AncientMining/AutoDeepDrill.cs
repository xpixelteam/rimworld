using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse.Sound;
using Verse;

namespace AncientMining
{
    public class Placeworker_AutoDeepDrill : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            CompProperties_DeepDrillAutomated compProperties =null;
            if (checkingDef is ThingDef thingDef)
            {
                compProperties = thingDef.GetCompProperties<CompProperties_DeepDrillAutomated>();
            }
            if (compProperties != null)
            {
                GenDraw.DrawRadiusRing(loc, compProperties.drillRadius);
            }
            return true;
        }
    }
    public static class VariableDeepDrillUtility
    {
        public static ThingDef GetNextResource(IntVec3 p, Map map, int gridCount)
        {
            GetNextResource(p, map, gridCount, out ThingDef result, out int num, out IntVec3 intVec);
            return result;
        }

        public static bool GetNextResource(IntVec3 p, Map map, int gridCount, out ThingDef resDef, out int countPresent, out IntVec3 cell)
        {
            for (int i = 0; i < gridCount; i++)
            {
                IntVec3 intVec = p + GenRadial.RadialPattern[i];
                if (GenGrid.InBounds(intVec, map))
                {
                    ThingDef thingDef = map.deepResourceGrid.ThingDefAt(intVec);
                    if (thingDef != null)
                    {
                        resDef = thingDef;
                        countPresent = map.deepResourceGrid.CountAt(intVec);
                        cell = intVec;
                        return true;
                    }
                }
            }
            resDef = GetBaseResource(map, p);
            countPresent = int.MaxValue;
            cell = p;
            return false;
        }

        public static ThingDef GetBaseResource(Map map, IntVec3 cell)
        {
            ThingDef result;
            if (!map.Biome.hasBedrock)
            {
                result = null;
            }
            else
            {
                Rand.PushState();
                Rand.Seed = cell.GetHashCode();
                ThingDef thingDef = GenCollection.RandomElement(from rock in Find.World.NaturalRockTypesIn(map.Tile)
                                                                          select rock.building.mineableThing);
                Rand.PopState();
                result = thingDef;
            }
            return result;
        }
    }
    public class CompProperties_DeepDrillAutomated : CompProperties
    {
        public CompProperties_DeepDrillAutomated()
        {
            this.compClass = typeof(CompDeepDrillAutomated);
        }

        public float yieldChance = 0.5f;

        public float drillRadius = 5f;

        public float yieldSize = 1f;

        public int workAmountPerProduction = 10000;

        public SoundDef operatingSustainer;

        public EffecterDef effecter;

        public Vector3 effectOffset = Vector3.zero;
    }
    public class CompDeepDrillAutomated : ThingComp
    {
        private CompProperties_DeepDrillAutomated Props
        {
            get
            {
                return this.props as CompProperties_DeepDrillAutomated;
            }
        }

        public float ProgressToNextPortionPercent
        {
            get
            {
                return this.portionProgress / (float)this.Props.workAmountPerProduction;
            }
        }

        public void ShutDown()
        {
            if (this.flickerComp != null)
            {
                this.flickerComp.SwitchIsOn = false;
            }
            if (this.linkableComp != null)
            {
                this.linkedFacilities = this.linkableComp.LinkedFacilitiesListForReading;
                foreach (Thing thing in this.linkedFacilities)
                {
                    CompFlickable compFlickable = ThingCompUtility.TryGetComp<CompFlickable>(thing);
                    if (compFlickable != null)
                    {
                        compFlickable.SwitchIsOn = false;
                    }
                }
            }
            Messages.Message(TranslatorFormattedStringExtensions.Translate("DeepDrillExhausted", Find.ActiveLanguageWorker.Pluralize(VariableDeepDrillUtility.GetBaseResource(this.parent.Map, this.parent.Position).label, -1)), this.parent, MessageTypeDefOf.TaskCompletion, true);
        }

        public void UpdateRadius()
        {
            this.cellsToScan = GenRadial.NumCellsInRadius(this.Props.drillRadius);
        }

        public void UpdateSustainer()
        {
            if (this.Props.operatingSustainer != null)
            {
                if (this.CanDrillNow())
                {
                    if (this.operatingSustainer == null || this.operatingSustainer.Ended)
                    {
                        this.operatingSustainer = SoundStarter.TrySpawnSustainer(this.Props.operatingSustainer, SoundInfo.InMap(this.parent, 0));
                    }
                    this.operatingSustainer.Maintain();
                }
                else
                {
                    this.operatingSustainer?.End();
                }
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            this.powerComp = ThingCompUtility.TryGetComp<CompPowerTrader>(this.parent);
            this.flickerComp = ThingCompUtility.TryGetComp<CompFlickable>(this.parent);
            this.linkableComp = ThingCompUtility.TryGetComp<CompAffectedByFacilities>(this.parent);
            this.UpdateRadius();
            if (this.parent.def.tickerType == 0)
            {
                Log.Error(this.parent.def.defName + "'s ticker type must NOT be never");
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<float>(ref this.portionProgress, "portionProgress", 0f, false);
            Scribe_Values.Look<float>(ref this.portionYieldPct, "portionYieldPct", 0f, false);
        }

        public void DrillWorkDone(int ticks)
        {
            if (this.effecter == null)
            {
                this.effecter = this.Props.effecter.Spawn(this.parent, this.parent.MapHeld, this.Props.effectOffset);
            }
            Effecter effecter = this.effecter;
            effecter?.EffectTick(this.parent, this.parent);
            float num = (float)ticks;
            this.portionProgress += num;
            this.portionYieldPct += num * 1f / (float)this.Props.workAmountPerProduction;
            if (this.portionProgress > (float)this.Props.workAmountPerProduction)
            {
                this.TryProducePortion(this.portionYieldPct);
                this.portionProgress = 0f;
                this.portionYieldPct = 0f;
            }
            this.lastWorkAmount = num / (float)ticks;
        }

        public void DrillWorkDone()
        {
            this.DrillWorkDone(1);
        }

        public override void PostDeSpawn(Map map)
        {
            this.portionProgress = 0f;
            this.portionYieldPct = 0f;
            if (this.operatingSustainer != null && !this.operatingSustainer.Ended)
            {
                this.operatingSustainer.End();
            }
        }

        private void TryProducePortion(float yieldPct)
        {
            bool flag = false;
            List<Thing> list = new List<Thing>();
            ThingDef thingDef;
            IntVec3 intVec;
            if (Rand.Chance(this.Props.yieldChance))
            {
                flag = this.GetNextResource(out thingDef, out int num, out intVec);
                int num2 = GenMath.RoundRandom((float)Mathf.Min(num, thingDef.deepCountPerPortion) * this.Props.yieldSize);
                Thing thing = ThingMaker.MakeThing(thingDef, null);
                thing.stackCount = num2;
                list.Add(thing);
                bool flag3 = flag;
                if (flag3)
                {
                    this.parent.Map.deepResourceGrid.SetAt(intVec, thingDef, num - num2);
                }
            }
            else
            {
                this.GetBedRockResource(out thingDef, out int num, out intVec);
                Thing item = ThingMaker.MakeThing(thingDef, null);
                list.Add(item);
            }
            if (thingDef != null)
            {
                this.dropProduct(list);
                bool flag5 = !flag || this.ValuableResourcesPresent();
                if (!flag5)
                {
                    if (VariableDeepDrillUtility.GetBaseResource(this.parent.Map, this.parent.Position) == null)
                    {
                        Messages.Message(Translator.Translate("DeepDrillExhaustedNoFallback"), this.parent, MessageTypeDefOf.TaskCompletion, true);
                    }
                    else
                    {
                        this.ShutDown();
                        for (int i = 0; i < this.cellsToScan; i++)
                        {
                            IntVec3 intVec2 = intVec + GenRadial.RadialPattern[i];
                            bool flag7 = GenGrid.InBounds(intVec2, this.parent.Map);
                            if (flag7)
                            {
                                ThingWithComps firstThingWithComp = GridsUtility.GetFirstThingWithComp<CompDeepDrill>(intVec2, this.parent.Map);
                                bool flag8 = firstThingWithComp != null && !firstThingWithComp.GetComp<CompDeepDrill>().ValuableResourcesPresent();
                                if (flag8)
                                {
                                    ForbidUtility.SetForbidden(firstThingWithComp, true, true);
                                }
                                ThingWithComps firstThingWithComp2 = GridsUtility.GetFirstThingWithComp<CompDeepDrillAutomated>(intVec2, this.parent.Map);
                                bool flag9 = firstThingWithComp2 != null && !firstThingWithComp2.GetComp<CompDeepDrillAutomated>().ValuableResourcesPresent();
                                if (flag9)
                                {
                                    firstThingWithComp2.GetComp<CompDeepDrillAutomated>().ShutDown();
                                }
                            }
                        }
                    }
                }
            }
        }

        public void dropProduct(List<Thing> Products)
        {
            if (!GenList.NullOrEmpty<Thing>(Products))
            {
                foreach (Thing thing in Products)
                {
                    bool flag2 = thing.stackCount <= 0;
                    if (flag2)
                    {
                        thing.stackCount = 1;
                    }
                    GenPlace.TryPlaceThing(thing, this.parent.Position, this.parent.Map, ThingPlaceMode.Near, null, (IntVec3 p) => p != this.parent.Position && p != this.parent.InteractionCell, default(Rot4));
                }
            }
        }

        private bool GetNextResource(out ThingDef resDef, out int countPresent, out IntVec3 cell)
        {
            return VariableDeepDrillUtility.GetNextResource(this.parent.Position, this.parent.Map, this.cellsToScan, out resDef, out countPresent, out cell);
        }

        private void GetBedRockResource(out ThingDef resDef, out int countPresent, out IntVec3 cell)
        {
            resDef = VariableDeepDrillUtility.GetBaseResource(this.parent.Map, this.parent.Position);
            countPresent = int.MaxValue;
            cell = this.parent.Position;
        }

        public bool CanDrillNow()
        {
            bool result;
            if (this.powerComp != null && !this.powerComp.PowerOn)
            {
                result = false;
            }
            else
            {
                bool flag2 = VariableDeepDrillUtility.GetBaseResource(this.parent.Map, this.parent.Position) != null;
                result = (flag2 || this.ValuableResourcesPresent());
            }
            return result;
        }

        public bool ValuableResourcesPresent()
        {
            return this.GetNextResource(out ThingDef thingDef, out int num, out IntVec3 intVec);
        }

        public override void CompTick()
        {
            if (this.powerComp == null || this.powerComp.PowerOn)
            {
                this.DrillWorkDone();
                this.UpdateSustainer();
            }
            else
            {
                if (this.effecter != null)
                {
                    this.effecter.Cleanup();
                }
            }
        }

        public override void CompTickRare()
        {
            if (this.powerComp == null || this.powerComp.PowerOn)
            {
                this.DrillWorkDone(250);
                this.UpdateSustainer();
            }
            else
            {
                if (effecter != null)
                {
                    this.effecter.Cleanup();
                }
            }
        }

        public override void CompTickLong()
        {
            if (this.powerComp == null || this.powerComp.PowerOn)
            {
                this.DrillWorkDone(2000);
                this.UpdateSustainer();
            }
            else
            {
                if (this.effecter != null)
                {
                    this.effecter.Cleanup();
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            bool showDevGizmos = DebugSettings.ShowDevGizmos;
            if (showDevGizmos)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "DEV: Produce portion (100% yield)";
                command_Action.action = delegate ()
                {
                    this.TryProducePortion(1f);
                };
                Command_Action command_Action2 = new Command_Action();
                command_Action2.defaultLabel = "DEV: Shut Down";
                command_Action2.action = delegate ()
                {
                    this.ShutDown();
                };
                yield return command_Action2;
            }
            yield break;
        }

        public override string CompInspectStringExtra()
        {
            bool spawned = this.parent.Spawned;
            string result;
            if (spawned)
            {
                ThingDef thingDef;
                int num;
                IntVec3 intVec;
                this.GetNextResource(out thingDef, out num, out intVec);
                bool flag = thingDef == null;
                if (flag)
                {
                    result = Translator.Translate("DeepDrillNoResources");
                }
                else
                {
                    result = Translator.Translate("ResourceBelow") + ": " + thingDef.LabelCap + "\n" + Translator.Translate("ProgressToNextPortion") + ": " + GenText.ToStringPercent(this.ProgressToNextPortionPercent, "F0");
                }
            }
            else
            {
                result = null;
            }
            return result;
        }

        private CompPowerTrader powerComp;
        private CompFlickable flickerComp;
        private CompAffectedByFacilities linkableComp;
        private Sustainer operatingSustainer;
        private List<Thing> linkedFacilities = new List<Thing>();
        private float portionProgress;
        private float portionYieldPct;
        private int cellsToScan;
        private Effecter effecter;
        private float lastWorkAmount;
    }
}
