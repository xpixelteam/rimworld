using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BreadMoAncientFarm
{
    public class CompNutrientToResource : CompResourceTrader, IThingHolder, IStoreSettingsParent
    {
        public float storage;
        public float nutrition;
        public int nextProcessTick;
        public ThingOwner innerContainer;
        private StorageSettings allowedNutritionSettings;
        public bool autoLoad = true;
        public CompProperties_NutrientToResource Props => (CompProperties_NutrientToResource)props;

        public bool StorageTabVisible => true;

        public CompNutrientToResource()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            ResourceOn = true;
            nextProcessTick = Find.TickManager.TicksGame + Props.ticksPerCycle;
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            allowedNutritionSettings = new StorageSettings(this);
            if (parent.def.building.defaultStorageSettings != null)
            {
                allowedNutritionSettings.CopyFrom(parent.def.building.defaultStorageSettings);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Values.Look(ref nutrition, "nutrition", 0f);
            Scribe_Values.Look(ref storage, "storage", 0f);
            Scribe_Values.Look(ref nextProcessTick, "nextProcessTick", 0);
            Scribe_Values.Look(ref autoLoad, "autoLoad", true);
            if (allowedNutritionSettings == null)
            {
                allowedNutritionSettings = new StorageSettings(this);
                if (parent.def.building.defaultStorageSettings != null)
                {
                    allowedNutritionSettings.CopyFrom(parent.def.building.defaultStorageSettings);
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("BM_NutritionCache".Translate() + nutrition.ToString());
            stringBuilder.AppendInNewLine(base.CompInspectStringExtra());
            return stringBuilder.ToString().Trim();
        }

        private void LiquifyNutrition()
        {
            foreach (Thing item in innerContainer)
            {
                Log.Message(item.ToString());
                float num = item.GetStatValue(StatDefOf.Nutrition) * (float)item.stackCount;
                if (num > 0f && !(item is Pawn))
                {
                    nutrition += num;
                    item.Destroy();
                }
            }
        }

        public override void CompTick()
        {
            int ticksGame = Find.TickManager.TicksGame;
            if (nutrition < Props.nutritionStorage)
            {
                LiquifyNutrition();
                if (nutrition < Props.nutritionStorage / 4)
                {
                    autoLoad = true;
                }
            }
            else
            {
                autoLoad = false;
            }
            if (ticksGame >= nextProcessTick)
            {
                if (CanBeOn() && nutrition >= 0 && storage < 1)
                {
                    storage += Math.Min(nutrition, Props.nutrientConsumptionRate) * Props.exchangeRate;
                    nutrition -= Props.nutrientConsumptionRate;
                    if (nutrition < 0)
                    {
                        nutrition = 0;
                    }
                }
                PushToNet();
                nextProcessTick = ticksGame + Props.ticksPerCycle;
            }
        }

        private void PushToNet()
        {
            if (PipeNet.connectors.Count > 1)
            {
                if (PipeNet.AvailableCapacity > storage)
                {
                    PipeNet.DistributeAmongStorage(storage, out var _);
                    storage = 0f;
                }
                else
                {
                    PipeNet.DistributeAmongStorage(PipeNet.AvailableCapacity, out var _);
                    storage -= PipeNet.AvailableCapacity;
                }
                return;
            }
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public StorageSettings GetStoreSettings()
        {
            return allowedNutritionSettings;
        }

        public StorageSettings GetParentStoreSettings()
        {
            return parent.def.building.fixedStorageSettings;
        }

        public void Notify_SettingsChanged()
        {
        }

        public bool CanAcceptNutrition(Thing thing)
        {
            return allowedNutritionSettings.AllowedToAccept(thing);
        }
    }

    public class CompProperties_NutrientToResource : CompProperties_ResourceTrader
    {
        public ThingDef thingdef;
        public float nutrientConsumptionRate = 1f;
        public float nutritionStorage = 4f;
        public float exchangeRate = 16;
        public int ticksPerCycle = 20000;
        public CompProperties_NutrientToResource()
        {
            compClass = typeof(CompNutrientToResource);
        }
    }
}
