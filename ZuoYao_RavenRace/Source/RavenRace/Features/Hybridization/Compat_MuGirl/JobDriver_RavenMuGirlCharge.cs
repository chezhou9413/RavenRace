using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using RavenRace.Compat.Epona;
using Verse.Sound;

namespace RavenRace.Compat.MuGirl
{
    // ==========================================
    // 1. Ability Effect
    // ==========================================
    public class CompProperties_AbilityMuGirlCharge : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityMuGirlCharge()
        {
            this.compClass = typeof(CompAbilityEffect_MuGirlCharge);
        }
    }

    public class CompAbilityEffect_MuGirlCharge : CompAbilityEffect
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = this.parent.pawn;
            if (pawn == null || target.Pawn == null) return;

            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Raven_Job_MuGirlCharge"), target.Pawn);
            job.ability = this.parent;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages)) return false;
            if (target.Pawn == null) return false;

            // [修改] 范围限制逻辑已移交给 Harmony Patch (Verb.Range)
            // 这里只需要检查基本的可达性即可

            if (!this.parent.pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
            {
                if (throwMessages) Messages.Message("CannotReach".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }
    }

    // ==========================================
    // 2. Job Driver (保持原样，无需大改，确认逻辑即可)
    // ==========================================
    public class JobDriver_RavenMuGirlCharge : JobDriver
    {
        private const float SmallPawnThreshold = 1.0f;
        private HashSet<Pawn> hitPawns = new HashSet<Pawn>();
        private const int SmokeGenerationInterval = 3;
        private int ticksSinceSmokeGeneration = 0;
        private Hediff chargeHediff;
        private Hediff speedHediff;
        private bool isEponaEnhanced = false;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            if (RavenRaceMod.Settings.enableMuGirlCompat)
            {
                isEponaEnhanced = EponaCompatUtility.IsEponaActive && EponaCompatUtility.HasEponaBloodline(pawn);
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Pawn target = job.targetA.Thing as Pawn;
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDespawnedOrNull(TargetIndex.A);

            // Toil 1: 准备
            yield return new Toil
            {
                initAction = () =>
                {
                    if (MuGirlCompatUtility.MooGirl_Charge != null)
                    {
                        chargeHediff = HediffMaker.MakeHediff(MuGirlCompatUtility.MooGirl_Charge, pawn);
                        chargeHediff.Severity = isEponaEnhanced ? 1.0f : 0.5f;
                        pawn.health.AddHediff(chargeHediff);
                    }

                    if (isEponaEnhanced)
                    {
                        HediffDef velocityDef = DefDatabase<HediffDef>.GetNamedSilentFail("Raven_Hediff_ChargeVelocity");
                        if (velocityDef != null)
                        {
                            speedHediff = HediffMaker.MakeHediff(velocityDef, pawn);
                            speedHediff.Severity = 1.0f;
                            pawn.health.AddHediff(speedHediff);
                        }
                    }

                    GenerateSmokeBehindPawn();
                    SoundDef.Named("Pawn_Melee_BigBash_HitPawn")?.PlayOneShot(pawn);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            // Toil 2: 移动
            Toil gotoToil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            gotoToil.tickAction = () =>
            {
                float radius = isEponaEnhanced ? 4.0f : 1.9f;
                var nearbyPawns = GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, radius, true).OfType<Pawn>();

                foreach (Pawn p in nearbyPawns)
                {
                    if (p == pawn || p == target) continue;
                    if (hitPawns.Contains(p)) continue;
                    if (!p.HostileTo(pawn)) continue;

                    DamageInfo dinfo;
                    float dmgMult = isEponaEnhanced ? 2.0f : 1.0f;

                    if (p.BodySize < SmallPawnThreshold)
                        dinfo = new DamageInfo(DamageDefOf.Crush, Rand.Range(4f, 6f) * dmgMult, 0f, -1, pawn);
                    else
                        dinfo = new DamageInfo(DamageDefOf.Blunt, Rand.Range(2f, 4f) * dmgMult, 0f, -1, pawn);

                    p.TakeDamage(dinfo);
                    hitPawns.Add(p);

                    if (!p.Dead)
                    {
                        int flyDist = isEponaEnhanced ? 6 : 2;
                        Vector3 dir = (p.Position.ToVector3() - pawn.Position.ToVector3()).normalized;
                        IntVec3 flyDest = p.Position + (dir * flyDist).ToIntVec3();
                        DoKnockback(p, flyDest);
                        FleckMaker.ThrowMicroSparks(p.DrawPos, pawn.Map);
                    }
                }

                if (++ticksSinceSmokeGeneration >= SmokeGenerationInterval)
                {
                    ticksSinceSmokeGeneration = 0;
                    float smokeSize = isEponaEnhanced ? 3.0f : 1.5f;
                    FleckMaker.ThrowSmoke(pawn.DrawPos, pawn.Map, smokeSize);
                }
            };
            yield return gotoToil;

            // Toil 3: 终结
            Toil attack = new Toil
            {
                initAction = () =>
                {
                    if (chargeHediff != null) pawn.health.RemoveHediff(chargeHediff);
                    if (speedHediff != null) pawn.health.RemoveHediff(speedHediff);

                    if (target == null || !target.Spawned) return;

                    float finalDmg = isEponaEnhanced ? 100f : 40f;
                    target.TakeDamage(new DamageInfo(DamageDefOf.Cut, finalDmg, 2f, -1, pawn));

                    if (MuGirlCompatUtility.MooGirl_Stun != null)
                    {
                        target.health.AddHediff(MuGirlCompatUtility.MooGirl_Stun);
                    }

                    if (!target.Dead)
                    {
                        int flyDist = isEponaEnhanced ? 15 : 5;
                        Vector3 dir = (target.Position.ToVector3() - pawn.Position.ToVector3()).normalized;
                        IntVec3 flyDest = target.Position + (dir * flyDist).ToIntVec3();
                        DoKnockback(target, flyDest);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            attack.AddFinishAction(() =>
            {
                if (job.ability != null)
                {
                    job.ability.StartCooldown(job.ability.def.cooldownTicksRange.RandomInRange);
                }
            });

            yield return attack;
        }

        private void GenerateSmokeBehindPawn()
        {
            if (job.targetA.Thing == null) return;
            Vector3 dir = (pawn.DrawPos - job.targetA.Thing.DrawPos).normalized;
            for (int i = 1; i <= 3; i++)
            {
                IntVec3 pos = pawn.Position + (dir * i).ToIntVec3();
                if (pos.InBounds(pawn.Map))
                    FleckMaker.ThrowSmoke(pos.ToVector3Shifted(), pawn.Map, Rand.Range(1.5f, 2.0f));
            }
        }

        private IntVec3 GetKnockbackDest(Pawn victim, float range)
        {
            Vector3 vect = (victim.Position - pawn.Position).ToVector3().Yto0().normalized * range;
            IntVec3 dest = victim.Position + vect.ToIntVec3();

            ShootLine shootLine = new ShootLine(victim.Position, dest);
            List<IntVec3> list = shootLine.Points().ToList();

            if (list.Count == 0) return victim.Position;

            Map map = pawn.Map;
            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].InBounds(map) || list[i].Impassable(map))
                {
                    return list[Mathf.Max(0, i - 1)];
                }
            }
            return dest;
        }

        private void DoKnockback(Pawn victim, IntVec3 destCell)
        {
            if (!destCell.InBounds(pawn.Map)) return;
            PawnFlyer flyer = PawnFlyer.MakeFlyer(ThingDefOf.PawnFlyer, victim, destCell, null, null, false);
            if (flyer != null)
            {
                FleckMaker.ThrowDustPuff(victim.Position.ToVector3Shifted() + Gen.RandomHorizontalVector(0.5f), pawn.Map, 2f);
                GenSpawn.Spawn(flyer, destCell, pawn.Map, WipeMode.Vanish);
            }
        }
    }
}