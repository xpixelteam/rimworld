using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PipeSystem;
using RimWorld;
using Verse;
using Verse.Sound;

namespace BreadMoAncientFarm
{
    public class CompFuelToResource : CompResourceTrader
    {
        private CompRefuelable compRefuelable;
        private float storage;
        private int nextProcessTick;
        private List<IntVec3> adjCells;
        public CompProperties_FuelToResource Props => (CompProperties_FuelToResource)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            ResourceOn = true;
            nextProcessTick = Find.TickManager.TicksGame + Props.ticksPerCycle;
            adjCells = GenAdj.CellsAdjacent8Way(parent).ToList();
            compRefuelable = parent.TryGetComp<CompRefuelable>();
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref storage, "storage", 0f);
            Scribe_Values.Look(ref nextProcessTick, "nextProcessTick", 0);
        }

        public override void CompTick()
        {
            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame >= nextProcessTick)
            {
                if (CanBeOn() && compRefuelable.Fuel >= 0 && storage < 1)
                {
                    storage += Math.Min(compRefuelable.Fuel, Props.fuelConsumptionRate) * Props.exchangeRate;
                    compRefuelable.ConsumeFuel(Props.fuelConsumptionRate);
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
    }

    public class CompProperties_FuelToResource : CompProperties_ResourceTrader
    {
        public ThingDef thingdef;
        public float fuelConsumptionRate = 1f;
        public float exchangeRate = 16;
        public int ticksPerCycle = 20000;
        public CompProperties_FuelToResource()
        {
            compClass = typeof(CompFuelToResource);
        }
    }
}
