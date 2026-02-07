using Verse;
using RimWorld;
using System.Linq;
using Verse.Sound;

namespace RavenRace
{
    public class CompTrapEffect_LegCrusher : CompTrapEffect
    {
        public override void OnTriggered(Pawn triggerer)
        {
            if (triggerer == null) return;

            // 1. 播放音效
            SoundDefOf.TrapSpring.PlayOneShot(new TargetInfo(parent.Position, parent.Map));

            // 2. 查找腿部
            var legs = triggerer.health.hediffSet.GetNotMissingParts()
                .Where(p => p.def.tags.Contains(BodyPartTagDefOf.MovingLimbCore) || p.def.defName.ToLower().Contains("leg"))
                .ToList();

            if (legs.Count > 0)
            {
                // 随机选一条腿
                BodyPartRecord targetLeg = legs.RandomElement();

                // 3. 造成伤害
                float damageAmount = 30f * RavenRaceMod.Settings.trapDamageMultiplier;

                DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, damageAmount, 2f, -1, parent, targetLeg);
                triggerer.TakeDamage(dinfo);

                // 4. 概率切断跟腱
                if (!triggerer.health.hediffSet.PartIsMissing(targetLeg) && Rand.Chance(0.6f))
                {
                    HediffDef def = DefenseDefOf.RavenHediff_TendonCut;
                    if (def == null) def = DefDatabase<HediffDef>.GetNamed("RavenHediff_TendonCut");

                    // [Fixed] CS0029: 必须先 MakeHediff，再添加
                    Hediff hediff = HediffMaker.MakeHediff(def, triggerer, targetLeg);
                    triggerer.health.AddHediff(hediff, targetLeg); // AddHediff 可以接受 (Hediff, BodyPartRecord)

                    Messages.Message($"{triggerer.LabelShort} 的跟腱被切断了！", triggerer, MessageTypeDefOf.NegativeEvent);
                }
            }
            else
            {
                // 没有腿？伤躯干
                float damageAmount = 15f * RavenRaceMod.Settings.trapDamageMultiplier;
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Stab, damageAmount, 1f, -1, parent, triggerer.RaceProps.body.corePart);
                triggerer.TakeDamage(dinfo);
            }

            // 5. 销毁陷阱
            if (parent is Building_RavenTrap trap)
            {
                parent.Destroy(DestroyMode.KillFinalize);
            }
        }
    }
}