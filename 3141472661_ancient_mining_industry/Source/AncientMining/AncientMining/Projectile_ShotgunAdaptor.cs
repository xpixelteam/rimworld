using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AncientMining
{
    public class Projectile_ShotgunAdaptor : Projectile
    {
        public float rangeVariation => 1 - Rand.Sign * Extension.rangeVariationFactor;

        ShotgunAdaptorExtension Extension => def.GetModExtension<ShotgunAdaptorExtension>();
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            if (Extension.random)
            {
                Vector3 vector = intendedTarget.Cell.ToVector3() - origin;
                for (int i = 0; i < Extension.pelletCount; i++)
                {
                    Projectile thing = (Projectile)GenSpawn.Spawn(Extension.projectileDef, launcher.Position, Map);
                    thing.Launch(launcher, origin, (origin + vector.RotatedBy(Rand.Value * Extension.spread * Rand.Sign) * rangeVariation).ToIntVec3(), intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);

                    vector = vector.RotatedBy(Extension.spread / (Extension.pelletCount - 1) * 2);
                }
            }
            else
            {
                Vector3 vector = intendedTarget.Cell.ToVector3() - origin;
                vector = vector.RotatedBy(-Extension.spread);
                for (int i = 0; i < Extension.pelletCount; i++)
                {
                    Projectile thing = (Projectile)GenSpawn.Spawn(Extension.projectileDef, launcher.Position, Map);
                    thing.Launch(launcher, origin, (origin + vector * rangeVariation).ToIntVec3(), intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);

                    vector = vector.RotatedBy(Extension.spread / (Extension.pelletCount - 1) * 2);
                }
            }
            Destroy();
        }
    }

    public class ShotgunAdaptorExtension : DefModExtension
    {
        public bool random = false;

        public ThingDef projectileDef;

        public float spread = 5;

        public int pelletCount = 5;

        public float rangeVariationFactor = 0.25f;
    }

    public class Bullet_AlwaysIntercept : Bullet
    {
        IntVec3 cachedPosition;
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            cachedPosition = Position;
        }

        public override void Tick()
        {
            base.Tick();
            if (Position != cachedPosition)
            {
                CheckForFreeIntercept(Position);
                cachedPosition = Position;
            }
        }

        private bool CheckForFreeIntercept(IntVec3 c)
        {
            if (destination.ToIntVec3() == c)
            {
                return false;
            }
            List<Thing> thingList = c.GetThingList(base.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing thing = thingList[i];
                if (!CanHit(thing))
                {
                    continue;
                }
                float num2 = 0f;
                if (thing is Pawn pawn)
                {
                    num2 = 0.4f * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
                    if (pawn.GetPosture() != 0)
                    {
                        num2 *= 0.1f;
                    }
                    if (launcher != null && pawn.Faction != null && launcher.Faction != null && !pawn.Faction.HostileTo(launcher.Faction))
                    {
                        float num = VerbUtility.InterceptChanceFactorFromDistance(origin, c);
                        if (num <= 0f)
                        {
                            return false;
                        }
                        if (preventFriendlyFire)
                        {
                            num2 = 0f;
                        }
                        else
                        {
                            num2 *= Find.Storyteller.difficulty.friendlyFireChanceFactor;
                        }
                    }
                }
                if (num2 > 1E-05f)
                {
                    if (Rand.Chance(num2))
                    {
                        Impact(thing);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
