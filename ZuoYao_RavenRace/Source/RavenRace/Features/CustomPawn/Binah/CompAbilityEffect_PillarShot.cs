using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.CustomPawn.Binah
{
    public class CompProperties_AbilityPillarShot : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityPillarShot()
        {
            this.compClass = typeof(CompAbilityEffect_PillarShot);
        }
    }

    public class CompAbilityEffect_PillarShot : CompAbilityEffect
    {
        private static readonly ThingDef[] PillarDefs = new ThingDef[4];

        static CompAbilityEffect_PillarShot()
        {
            PillarDefs[0] = BinahDefOf.Raven_Projectile_Binah_Pillar_I;
            PillarDefs[1] = BinahDefOf.Raven_Projectile_Binah_Pillar_II;
            PillarDefs[2] = BinahDefOf.Raven_Projectile_Binah_Pillar_III;
            PillarDefs[3] = BinahDefOf.Raven_Projectile_Binah_Pillar_IV;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            if (caster == null || !caster.Spawned) return;

            var manager = caster.Map.GetComponent<PillarSpawnerManager>();
            if (manager == null)
            {
                manager = new PillarSpawnerManager(caster.Map);
                caster.Map.components.Add(manager);
            }

            manager.Register(new PillarSpawner(caster));
        }
    }

    public class PillarSpawner
    {
        private Pawn caster;
        private List<Mote> activeMotes = new List<Mote>();
        private int startTick;
        private int currentStep = 0;

        private const int SPAWN_INTERVAL = 6;
        private const int LAUNCH_DELAY = 100;
        private const float RADIUS = 5.5f;  //原来是3.5

        public bool IsComplete { get; private set; } = false;

        public PillarSpawner(Pawn caster)
        {
            this.caster = caster;
            this.startTick = Find.TickManager.TicksGame;
        }

        public void Tick()
        {
            if (IsComplete || caster == null || !caster.Spawned || caster.Map == null)
            {
                CleanupMotes();
                IsComplete = true;
                return;
            }

            int elapsed = Find.TickManager.TicksGame - startTick;

            if (currentStep < 8 && elapsed >= currentStep * SPAWN_INTERVAL)
            {
                SpawnPillarMote(currentStep);
                currentStep++;
            }

            if (elapsed >= LAUNCH_DELAY)
            {
                Launch();
                CleanupMotes();
                IsComplete = true;
            }
        }

        private void SpawnPillarMote(int index)
        {
            // 角度计算：从正上方(0°)顺时针旋转
            // RimWorld 坐标系：0=北, 90=东, 180=南, 270=西
            // 我们希望顺序是：北 -> 东北 -> 东 ...
            float angle = index * 45f;

            Vector3 offset = new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * RADIUS,
                0,
                Mathf.Cos(angle * Mathf.Deg2Rad) * RADIUS
            );
            Vector3 pos = caster.DrawPos + offset;

            ThingDef moteDef = GetPillarMoteDef(index % 4);
            if (moteDef == null) return;

            Mote mote = MoteMaker.MakeStaticMote(pos, caster.Map, moteDef, 2.0f);
            if (mote != null)
            {
                // [修复] 设置旋转以呈放射状
                // 假设贴图本身是竖直向上的柱子
                // 当在北方(0°)时，它应该竖直 (0°旋转)
                // 当在东方(90°)时，它应该向右倒 (90°旋转)
                mote.exactRotation = angle;
                mote.solidTimeOverride = 6.0f;
                activeMotes.Add(mote);
            }
        }

        private void Launch()
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Vector3 offset = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * RADIUS, 0, Mathf.Cos(angle * Mathf.Deg2Rad) * RADIUS);
                Vector3 spawnPos = caster.DrawPos + offset;
                Vector3 targetPos = caster.DrawPos + offset * 60f;

                ThingDef projDef = GetPillarProjectileDef(i % 4);
                if (projDef != null)
                {
                    Projectile proj = (Projectile)GenSpawn.Spawn(projDef, spawnPos.ToIntVec3(), caster.Map);
                    proj.Launch(
                        caster,
                        spawnPos,
                        new LocalTargetInfo(targetPos.ToIntVec3()),
                        new LocalTargetInfo(targetPos.ToIntVec3()),
                        ProjectileHitFlags.All
                    );
                }
            }
        }

        private ThingDef GetPillarMoteDef(int index)
        {
            switch (index)
            {
                case 0: return BinahDefOf.Raven_Mote_Binah_Pillar_I;
                case 1: return BinahDefOf.Raven_Mote_Binah_Pillar_II;
                case 2: return BinahDefOf.Raven_Mote_Binah_Pillar_III;
                case 3: return BinahDefOf.Raven_Mote_Binah_Pillar_IV;
                default: return null;
            }
        }

        private ThingDef GetPillarProjectileDef(int index)
        {
            switch (index)
            {
                case 0: return BinahDefOf.Raven_Projectile_Binah_Pillar_I;
                case 1: return BinahDefOf.Raven_Projectile_Binah_Pillar_II;
                case 2: return BinahDefOf.Raven_Projectile_Binah_Pillar_III;
                case 3: return BinahDefOf.Raven_Projectile_Binah_Pillar_IV;
                default: return null;
            }
        }

        private void CleanupMotes()
        {
            foreach (var m in activeMotes)
            {
                if (m != null && !m.Destroyed) m.Destroy();
            }
            activeMotes.Clear();
        }
    }

    public class PillarSpawnerManager : MapComponent
    {
        private List<PillarSpawner> spawners = new List<PillarSpawner>();
        public PillarSpawnerManager(Map map) : base(map) { }
        public void Register(PillarSpawner s) => spawners.Add(s);
        public override void MapComponentTick()
        {
            for (int i = spawners.Count - 1; i >= 0; i--)
            {
                spawners[i].Tick();
                if (spawners[i].IsComplete) spawners.RemoveAt(i);
            }
        }
    }
}