using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AncientMining
{
    public class Building_BoringMachine : Building
    {
        ModExtension_BoringMachine extension;

        int progressTicks => extension.progressRequired;

        int ticksTillProgress;

        int lastPowerCheck;

        float progress
        {
            get
            {
                return (float)ticksTillProgress / progressTicks;
            }
            set
            {
                ticksTillProgress = (int)(progressTicks * value);
            }
        }

        private CompFlickable flickComp;

        private CompPowerTrader powerComp;

        private CompRefuelable fuelComp;

        private Sustainer Sustainer;

        bool turnedOn;

        bool makeWall;

        bool ValidateNextCell()
        {
            foreach (IntVec3 cell in BoringCells)
            {
                if (!cell.InBounds(Map))
                {
                    turnedOn = false;
                    return false;
                }
                else if (!cell.GetTerrain(Map).affordances.Contains(TerrainAffordanceDefOf.Heavy))
                {
                    turnedOn = false;
                    return false;
                }
            }
            return true;
        }

        public bool shouldBeOn
        {
            get
            {
                if (!turnedOn)
                {
                    return false;
                }
                if (flickComp != null && !flickComp.SwitchIsOn)
                {
                    return false;
                }
                if (powerComp != null)
                {
                    if (lastPowerCheck < Find.TickManager.TicksGame && !powerComp.PowerOn)
                    {
                        return false;
                    }
                }
                if (fuelComp != null && !fuelComp.HasFuel)
                {
                    return false;
                }
                return true;
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            stringBuilder.AppendInNewLine("Progress: " + progress.ToStringPercent());
            stringBuilder.AppendInNewLine("Turned On: " + shouldBeOn.ToString());
            return stringBuilder.ToString();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            flickComp = GetComp<CompFlickable>();
            powerComp = GetComp<CompPowerTrader>();
            fuelComp = GetComp<CompRefuelable>();
            extension = def.GetModExtension<ModExtension_BoringMachine>();
        }

        public override void DrawExtraSelectionOverlays()
        {
            GenDraw.DrawFieldEdges(BoringCells, Color.white);
            GenDraw.DrawFieldEdges(DumpingCells, Color.white);
        }

        public Vector3 Rot4ToV3(Rot4 rot)
        {
            switch (rot.AsInt)
            {
                case 0:
                    return Vector3.forward;
                case 1:
                    return Vector3.right;
                case 2:
                    return Vector3.back;
                case 3:
                    return Vector3.left;
            }
            return Vector3.zero;
        }

        List<IntVec3> boringCells;

        public List<IntVec3> BoringCells
        {
            get
            {
                if (boringCells == null)
                {
                    return GetBoringCells().ToList();
                }
                return boringCells;
            }
        }

        public IEnumerable<IntVec3> GetBoringCells()
        {
            boringCells = GetAdjCellsStatic(Position, Rotation, this.def.size).ToList();
            return BoringCells;
        }

        List<IntVec3> dumpingCells;

        List<IntVec3> wallCells;

        public List<IntVec3> WallCells
        {
            get
            {
                if (wallCells == null)
                {
                    GetDumpingCells();
                }
                return wallCells;
            }
        }

        public List<IntVec3> DumpingCells
        {
            get
            {
                if (dumpingCells == null)
                {
                    return GetDumpingCells().ToList();
                }
                return dumpingCells;
            }
        }

        public IEnumerable<IntVec3> GetDumpingCells()
        {
            dumpingCells = GetAdjCellsStatic(Position, Rotation, this.def.size, 2).ToList();
            dumpingCells.SortBy(x => x.x + x.z);

            IntVec3 f = dumpingCells.First();
            IntVec3 l = dumpingCells.Last();


            wallCells = new List<IntVec3>() { f, l };

            dumpingCells.Remove(f);
            dumpingCells.Remove(l);
            return DumpingCells;
        }

        public static IEnumerable<IntVec3> GetAdjCellsStatic(IntVec3 Position, Rot4 Rotation, IntVec2 Size, int additionalRot = 0)
        {
            GenAdj.AdjustForRotation(ref Position, ref Size, Rotation);
            int minX = Position.x - (Size.x - 1) / 2;
            int minZ = Position.z - (Size.z - 1) / 2;
            int maxX = minX + Size.x - 1;
            int maxZ = minZ + Size.z - 1;

            int rot = Rotation.AsInt += additionalRot;
            if (rot > 3)
            {
                rot -= 4;
            }

            switch (rot)
            {
                case 0:
                    for (int x4 = minX; x4 <= maxX; x4++)
                    {
                        yield return new IntVec3(x4, Position.y, maxZ + 1);
                    }
                    break;
                case 1:
                    for (int x4 = minZ; x4 <= maxZ; x4++)
                    {
                        yield return new IntVec3(maxX + 1, Position.y, x4);
                    }
                    break;
                case 2:
                    for (int x4 = minX; x4 <= maxX; x4++)
                    {
                        yield return new IntVec3(x4, Position.y, minZ - 1);
                    }
                    break;
                case 3:
                    for (int x4 = minZ; x4 <= maxZ; x4++)
                    {
                        yield return new IntVec3(minX - 1, Position.y, x4);
                    }
                    break;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            yield return new Command_Action
            {
                action = delegate
                {
                    if (ValidateNextCell())
                    {
                        turnedOn = !turnedOn;
                    }
                },
                defaultLabel = (turnedOn ? extension.toggleGizmoLabel : extension.toggleGizmoOffLabel).Translate(),
                icon = ContentFinder<Texture2D>.Get(extension.toggleGizmoIcon, reportFailure: false)
            };
            yield return new Command_Action
            {
                action = delegate
                {
                    makeWall = !makeWall;
                },
                defaultLabel = (makeWall ? extension.wallGizmoLabel : extension.wallGizmoOffLabel).Translate(),
                icon = ContentFinder<Texture2D>.Get(extension.wallGizmoIcon, reportFailure: false)
            };
        }

        public override void Tick()
        {
            base.Tick();

            if (Spawned)
            {
                if (progress == 1)
                {
                    DoDrillDamage(true);
                    IntVec3 pos = Position + Rot4ToV3(Rotation).ToIntVec3();
                    Rot4 rot = Rotation;
                    Map map = Map;
                    bool selected = Find.Selector.IsSelected(this);

                    DeSpawn();
                    GenSpawn.Spawn(this, pos, map, rot);

                    if (selected)
                    {
                        Find.Selector.Select(this);
                    }

                    ticksTillProgress = 0;
                    GetBoringCells();
                    GetDumpingCells();
                    if (makeWall)
                    {
                        DeployWalls();
                    }
                    DeployCables();
                    ValidateNextCell();

                    if (powerComp != null)
                    {
                        lastPowerCheck = Find.TickManager.TicksGame + extension.powerCheckDelay;
                    }
                }
                if (shouldBeOn)
                {
                    if (extension.tickInterval == 1 || this.IsHashIntervalTick(extension.tickInterval))
                    {
                        ticksTillProgress++;
                        DoDrillDamage();
                    }
                    if (Sustainer == null)
                    {
                        Sustainer = extension?.sustainer?.TrySpawnSustainer(SoundInfo.InMap(this));
                    }
                    Sustainer.Maintain();
                }
                else if (Sustainer != null)
                {
                    Sustainer.End();
                    Sustainer = null;
                }
            }
        }

        public void DeployWalls()
        {
            foreach (IntVec3 cell in WallCells)
            {
                Thing thing = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                thing.SetFactionDirect(this.factionInt);
                GenSpawn.Spawn(thing, cell, Map, WipeMode.VanishOrMoveAside);
                if (fuelComp != null)
                {
                    fuelComp.ConsumeFuel(ThingDefOf.Wall.CostStuffCount);
                }
            }
        }

        public void DeployCables()
        {
            foreach (IntVec3 cell in WallCells)
            {
                Thing thing = ThingMaker.MakeThing(ThingDefOf.PowerConduit);
                thing.SetFactionDirect(this.factionInt);
                GenSpawn.Spawn(thing, cell, Map, WipeMode.VanishOrMoveAside);
                if (fuelComp != null)
                {
                    fuelComp.ConsumeFuel(1);
                }
            }
        }

        public void DoDrillDamage(bool finalize = false)
        {
            bool shouldOutletSprew = false;
            foreach (IntVec3 cell in BoringCells)
            {
                if (!cell.InBounds(Map))
                {
                    break;
                }
                var list = cell.GetThingList(Map);
                for (int num = list.Count - 1; num >= 0; num--)
                {
                    Thing thing = list[num];
                    if (thing == this)
                    {
                        continue;
                    }

                    if (thing.def.category == ThingCategory.Building && thing.def.building.isEdifice)
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Mining, finalize ? 99999 : 1, instigator: this);
                        if (finalize)
                        {
                            thing.TakeDamage(dinfo);
                            if (!thing.Destroyed)
                            {
                                thing.Kill(dinfo);
                            }
                        }
                        else
                        {
                            thing.TakeDamage(dinfo);
                        }

                        Vector3 fleckPos = cell.ToVector3Shifted() + (progress - 0.5f) * Rot4ToV3(Rotation) + (Rand.Value - 0.5f) * Rot4ToV3(Rotation).RotatedBy(90);
                        fleckPos.y = this.DrawPos.y + (Rotation == Rot4.North ? -0.1f : 0.1f);
                        FleckMaker.ThrowDustPuffThick(fleckPos, Map, 1, thing.DrawColor);

                        shouldOutletSprew = true;
                    }
                    else if (thing.def.category == ThingCategory.Pawn && thing.Faction != Faction.OfPlayer)
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, finalize ? thing.HitPoints : 1, 114514, instigator: this);
                        if (finalize)
                        {
                            thing.Kill(dinfo);
                        }
                        else
                        {
                            thing.TakeDamage(dinfo);
                        }
                    }
                    else if (thing.def.category == ThingCategory.Item)
                    {
                        thing.DeSpawn();
                        GenSpawn.Spawn(thing, DumpingCells.RandomElement(), Map);
                    }
                }
                if (finalize)
                {
                    for (int num = list.Count - 1; num >= 0; num--)
                    {
                        Thing thing = list[num];
                        if (thing.def.category == ThingCategory.Item)
                        {
                            thing.DeSpawn();
                            GenSpawn.Spawn(thing, DumpingCells.RandomElement(), Map);
                        }
                    }
                }
                if (extension != null && shouldOutletSprew)
                {
                    extension.OutletFleckPos(Rotation, out Vector3 fleckpos, out FloatRange angle);

                    FleckCreationData dataStatic = FleckMaker.GetDataStatic(DrawPos + fleckpos, Map, extension.fleck);
                    dataStatic.rotation = Rand.Value * 360;
                    dataStatic.velocitySpeed = extension.OutletFleckSpeed.RandomInRange;
                    dataStatic.velocityAngle = angle.RandomInRange;
                    Map.flecks.CreateFleck(dataStatic);
                }
            }
        }

        public override Vector3 DrawPos => base.DrawPos + progress * Rot4ToV3(this.Rotation);
    }

    public class PlaceWorker_BoringMachine : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            IEnumerable<IntVec3> cells = Building_BoringMachine.GetAdjCellsStatic(loc, rot, checkingDef.Size);
            GenDraw.DrawFieldEdges(cells.ToList(), Color.white);
            return true;
        }
    }

    public class ModExtension_BoringMachine : DefModExtension
    {
        public ThingDef wallStuff;

        public int damage = 1;

        public int tickInterval = 1;

        public int progressRequired = 60;

        public FleckDef fleck;

        public string toggleGizmoIcon = "";

        public string toggleGizmoLabel = "";

        public string toggleGizmoOffLabel = "";

        public string wallGizmoIcon = "";

        public string wallGizmoLabel = "";

        public string wallGizmoOffLabel = "";

        public int powerCheckDelay = 360;

        public Vector3 NorthOutletFleckPos = Vector3.zero;

        public Vector3 SouthOutletFleckPos = Vector3.zero;

        public Vector3 EastOutletFleckPos = Vector3.zero;

        public Vector3 WestOutletFleckPos = Vector3.zero;

        public FloatRange NorthOutletFleckAngle = new FloatRange(-10, 10);

        public FloatRange SouthOutletFleckAngle = new FloatRange(170, 190);

        public FloatRange EastOutletFleckAngle = new FloatRange(80, 100);

        public FloatRange WestOutletFleckAngle = new FloatRange(260, 280);

        public FloatRange OutletFleckSpeed = new FloatRange(1, 2);

        public SoundDef sustainer;

        public void OutletFleckPos(Rot4 rot, out Vector3 pos, out FloatRange angle)
        {
            pos = Vector3.zero;
            angle = new FloatRange(0, 0);
            switch (rot.AsInt)
            {
                case 0:
                    pos = NorthOutletFleckPos;
                    angle = NorthOutletFleckAngle;
                    return;
                    ;
                case 1:
                    pos = EastOutletFleckPos;
                    angle = EastOutletFleckAngle;
                    return;
                    ;
                case 2:
                    pos = SouthOutletFleckPos;
                    angle = SouthOutletFleckAngle;
                    return;
                    ;
                case 3:
                    pos = WestOutletFleckPos;
                    angle = WestOutletFleckAngle;
                    return;
            }
        }
    }
}
