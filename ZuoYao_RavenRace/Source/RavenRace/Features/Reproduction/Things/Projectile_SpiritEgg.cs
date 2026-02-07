using System;
using Verse;
using RimWorld;

namespace RavenRace.Features.Reproduction
{
    public class SpiritEggProjectile : Projectile
    {
        public Thing storedEgg;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref storedEgg, "storedEgg");
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            IntVec3 position = base.Position;

            if (storedEgg != null)
            {
                GenSpawn.Spawn(storedEgg, position, map);
                storedEgg.SetForbidden(true, false);
            }
            else
            {
                Thing newEgg = ThingMaker.MakeThing(RavenDefOf.Raven_SpiritEgg);
                GenSpawn.Spawn(newEgg, position, map);
                newEgg.SetForbidden(true, false);
            }

            if (hitThing is Pawn hitPawn && !hitPawn.Dead)
            {
                HediffDef stunDef = HediffDefOf.Anesthetic;
                hitPawn.health.AddHediff(stunDef);

                Messages.Message("RavenRace_Msg_EggLaunch_Impact".Translate(hitPawn.LabelShort), hitPawn, MessageTypeDefOf.NeutralEvent);
            }

            base.Impact(hitThing, blockedByShield);
        }
    }
}