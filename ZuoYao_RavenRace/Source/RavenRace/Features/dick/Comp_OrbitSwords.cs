using ChezhouLib.ALLmap;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RavenRace.Features.dick
{
    public class CompProperties_OrbitSwords : CompProperties
    {
        public float detectRadius = 12f;
        public float flySpeed = 0.10f;
        public float attackSpeed = 0.55f;
        public float returnSpeed = 0.08f;
        public float orbitAroundTarget = 1.8f;
        public float orbitAroundSpeed = 2.8f;
        public int aimTicks = 25;
        public int slashDamage = 20;
        public int thrustDamage = 13;
        public int betweenAttackTicks = 50;

        // 攻击表现与手感
        public float attackWindupScale = 0.35f;
        public int hitPauseTicks = 4;
        public float attackAccelPower = 2.2f;
        public float slashArcDegrees = 65f;

        // 特殊攻击参数
        public int spinSlashDamage = 16;
        public int diveDamage = 28;
        public int flurryDamagePerHit = 6;
        public int flurryHitCount = 4;
        public int spiralDamage = 18;
        public float diveHeight = 3.5f;
        public int diveHoverTicks = 18;
        public float spinSlashRadius = 1.2f;

        // 入体相关
        public HediffDef hideHediff;

        public CompProperties_OrbitSwords()
        {
            compClass = typeof(Comp_OrbitSwords);
        }
    }

    public enum SwordState
    {
        Orbiting,
        FlyToTarget,
        CircleTarget,
        Aiming,
        Thrusting,
        Slashing,
        SpinSlashing,
        DiveRising,
        DiveHovering,
        DivePlunging,
        Flurrying,
        SpiralDrilling,
        HitPause,
        Returning,

        // 特殊状态
        DrillIntoButt,
        Hidden
    }

    public enum AttackType
    {
        Thrust,
        Slash,
        SpinSlash,
        Dive,
        Flurry,
        Spiral
    }

    public class SwordAgent
    {
        public GameObject go;
        public SwordState state = SwordState.Orbiting;
        public Pawn target;
        public AttackType nextAttack;
        public int timer = 0;
        public bool hitDealt = false;
        public float circleAngle = 0f;
        public int phaseOffset = 0;
        public Vector3 attackEnd;
        public Vector3 attackStart;
        public Vector3 orbitSlot;
        public Vector3 currentPos;
        public Quaternion currentRot;
        public bool initialized = false;

        public float attackProgress;
        public float attackTotalDist;
        public SwordState stateAfterPause;
        public Vector3 windupPos;

        public Vector3 slashPivot;
        public float slashStartAngle;
        public float slashEndAngle;
        public float slashRadius;
        public float slashProgress;

        public float spinSelfAngle;

        public Vector3 diveApex;
        public Vector3 diveTarget;
        public Quaternion diveStartRot;

        public int flurryHitsRemaining;
        public int flurrySubTimer;
        public bool flurrySubHit;

        public float spiralAngle;
        public float spiralRadius;
        public float spiralProgress;
        public Vector3 spiralStart;

        public bool useDirectRot = false;
        public Quaternion directRot;
        public Quaternion targetRot;
    }

    public class Comp_OrbitSwords : ThingComp
    {
        private CompProperties_OrbitSwords Props => (CompProperties_OrbitSwords)props;
        private List<SwordAgent> _agents = new List<SwordAgent>();

        private float _angle = 0f;
        private float _selfSpin = 0f;

        private const float OrbitRadius = 2f;
        private const float OrbitHeight = 0.9f;
        private const float OrbitSpeed = 2.5f;
        private const float SpinSpeed = 8f;
        private const float ModelScale = 3f;
        private const float OvershootDist = 1.6f;

        private static readonly string[] PrefabKeys =
        {
            "dick_light", "dick_mohu", "dick_masaike", "dick_err", "dick_dack"
        };

        // 获取当前装备该武器的 Pawn
        private Pawn OwnerPawn
        {
            get
            {
                if (parent.ParentHolder is Pawn_EquipmentTracker eq) return eq.pawn;
                if (parent.ParentHolder is Pawn_ApparelTracker ap) return ap.pawn;
                return null;
            }
        }

        private static readonly (AttackType type, float weight)[] AttackWeights =
        {
            (AttackType.Thrust,    3.0f),
            (AttackType.Slash,     3.0f),
            (AttackType.SpinSlash, 2.0f),
            (AttackType.Dive,      1.0f),
            (AttackType.Flurry,    2.0f),
            (AttackType.Spiral,    2.0f),
        };

        // --- 生命周期管理 ---

        private void CreateSwords()
        {
            for (int i = 0; i < PrefabKeys.Length; i++)
            {
                string key = PrefabKeys[i];
                if (!abDatabase.prefabDataBase.TryGetValue(key, out GameObject prefab) || prefab == null)
                { Log.Error($"[Comp_OrbitSwords] 找不到预制体: {key}"); continue; }

                GameObject inst = UnityEngine.Object.Instantiate(prefab);
                inst.name = $"OrbitSword_{key}";
                inst.transform.localScale = Vector3.one * ModelScale;
                inst.SetActive(true);

                _agents.Add(new SwordAgent
                {
                    go = inst,
                    phaseOffset = i * (Props.betweenAttackTicks / PrefabKeys.Length),
                    circleAngle = i * (360f / PrefabKeys.Length),
                    initialized = false
                });
            }
        }

        private void DestroySwords()
        {
            foreach (var a in _agents) { if (a.go != null) UnityEngine.Object.Destroy(a.go); }
            _agents.Clear();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            DestroySwords();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            DestroySwords();
        }

        public bool IsHidden()
        {
            return _agents.Count > 0 && (_agents[0].state == SwordState.Hidden || _agents[0].state == SwordState.DrillIntoButt);
        }

        public void TriggerHide()
        {
            if (OwnerPawn == null || Props.hideHediff == null) return;

            // 强制打断所有飞剑当前动作，转为入体状态
            foreach (var a in _agents)
            {
                a.target = null;
                a.state = SwordState.DrillIntoButt;
            }
        }

        // --- 主逻辑更新 ---

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = OwnerPawn;

            // 未被装备时清理模型
            if (pawn == null)
            {
                if (_agents.Count > 0) DestroySwords();
                return;
            }
            // 首次装备初始化
            else if (_agents.Count == 0)
            {
                CreateSwords();
            }

            if (!pawn.Spawned) return;

            _angle = (_angle + OrbitSpeed) % 360f;
            _selfSpin = (_selfSpin + SpinSpeed) % 360f;

            Vector3 center = GetOrbitCenter();
            float step = 360f / _agents.Count;

            for (int i = 0; i < _agents.Count; i++)
            {
                float rad = (_angle + i * step) * Mathf.Deg2Rad;
                _agents[i].orbitSlot = center + new Vector3(Mathf.Cos(rad) * OrbitRadius, 0f, Mathf.Sin(rad) * OrbitRadius);
                if (!_agents[i].initialized)
                {
                    _agents[i].currentPos = _agents[i].orbitSlot;
                    _agents[i].initialized = true;
                }
            }

            if (Find.TickManager.TicksGame % 10 == 0)
                AssignTargets();

            foreach (var a in _agents)
                TickSword(a, center);
        }

        // --- 索敌与目标分配 ---

        private void AssignTargets()
        {
            if (OwnerPawn?.Map == null) return;
            var enemies = GatherEnemies();

            foreach (var a in _agents)
            {
                // 跳过特殊状态的飞剑
                if (a.state == SwordState.DrillIntoButt || a.state == SwordState.Hidden)
                    continue;

                if (a.target != null && (a.target.Dead || !a.target.Spawned || a.target.Downed))
                    a.target = null;

                if (enemies.Count == 0)
                {
                    if (a.state == SwordState.Orbiting || a.state == SwordState.Returning) continue;
                    if (a.state == SwordState.FlyToTarget || a.state == SwordState.CircleTarget)
                    { a.target = null; a.state = SwordState.Returning; }
                    continue;
                }

                switch (a.state)
                {
                    case SwordState.Orbiting:
                        a.target = PickBestTarget(a, enemies);
                        a.state = SwordState.FlyToTarget;
                        break;
                    case SwordState.CircleTarget:
                        if (a.target == null) a.target = PickBestTarget(a, enemies);
                        break;
                    case SwordState.Returning:
                        a.target = PickBestTarget(a, enemies);
                        a.state = SwordState.FlyToTarget;
                        break;
                    case SwordState.FlyToTarget:
                        if (a.target == null) a.target = PickBestTarget(a, enemies);
                        break;
                }
            }
        }

        private Pawn PickBestTarget(SwordAgent agent, List<Pawn> enemies)
        {
            if (enemies.Count == 0) return null;
            Dictionary<Pawn, int> assignCount = new Dictionary<Pawn, int>();
            foreach (var e in enemies) assignCount[e] = 0;
            foreach (var a in _agents)
            {
                if (a == agent) continue;
                if (a.target != null && assignCount.ContainsKey(a.target))
                    assignCount[a.target]++;
            }
            Pawn best = null;
            float bestScore = float.MaxValue;
            foreach (var e in enemies)
            {
                float dist = Vector3.Distance(agent.currentPos, e.DrawPos);
                float score = assignCount[e] * 8f + dist;
                if (score < bestScore) { bestScore = score; best = e; }
            }
            return best ?? enemies[Rand.Range(0, enemies.Count)];
        }

        private List<Pawn> GatherEnemies()
        {
            var result = new List<Pawn>();
            if (OwnerPawn?.Map == null) return result;
            foreach (Pawn p in OwnerPawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (!p.Spawned || p.Dead || p.Downed) continue;
                if (!OwnerPawn.HostileTo(p)) continue;
                if (OwnerPawn.Position.DistanceTo(p.Position) > Props.detectRadius) continue;
                if (!GenSight.LineOfSight(OwnerPawn.Position, p.Position, OwnerPawn.Map)) continue;
                result.Add(p);
            }
            return result;
        }

        private AttackType RollAttackType()
        {
            float totalWeight = 0f;
            foreach (var pair in AttackWeights) totalWeight += pair.weight;
            float roll = Rand.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var pair in AttackWeights)
            {
                cumulative += pair.weight;
                if (roll <= cumulative) return pair.type;
            }
            return AttackType.Thrust;
        }

        // --- 状态机 Tick 分发 ---

        private void TickSword(SwordAgent a, Vector3 orbitCenter)
        {
            if (a.go == null) return;

            a.useDirectRot = false;

            switch (a.state)
            {
                case SwordState.Orbiting: TickOrbiting(a, orbitCenter); break;
                case SwordState.FlyToTarget: TickFlyToTarget(a); break;
                case SwordState.CircleTarget: TickCircleTarget(a); break;
                case SwordState.Aiming: TickAiming(a); break;
                case SwordState.Thrusting: TickThrusting(a); break;
                case SwordState.Slashing: TickSlashing(a); break;
                case SwordState.SpinSlashing: TickSpinSlashing(a); break;
                case SwordState.DiveRising: TickDiveRising(a); break;
                case SwordState.DiveHovering: TickDiveHovering(a); break;
                case SwordState.DivePlunging: TickDivePlunging(a); break;
                case SwordState.Flurrying: TickFlurrying(a); break;
                case SwordState.SpiralDrilling: TickSpiralDrilling(a); break;
                case SwordState.HitPause: TickHitPause(a); break;
                case SwordState.Returning: TickReturning(a, orbitCenter); break;
                case SwordState.DrillIntoButt: TickDrillIntoButt(a); break;
                case SwordState.Hidden: TickHidden(a, orbitCenter); break;
            }

            // 潜伏状态不更新变换以节省性能
            if (a.state == SwordState.Hidden) return;

            if (a.useDirectRot)
            {
                a.currentRot = a.directRot;
            }
            else
            {
                a.currentRot = Quaternion.RotateTowards(a.currentRot, a.targetRot, GetRotSpeed(a.state));
            }

            a.go.transform.position = a.currentPos;
            a.go.transform.rotation = a.currentRot;
        }

        private float GetRotSpeed(SwordState state)
        {
            switch (state)
            {
                case SwordState.Orbiting: return 8f;
                case SwordState.FlyToTarget: return 12f;
                case SwordState.CircleTarget: return 10f;
                case SwordState.Aiming: return 6f;
                case SwordState.Thrusting:
                case SwordState.Slashing:
                case SwordState.DivePlunging:
                case SwordState.Flurrying: return 25f;
                case SwordState.DiveRising: return 6f;
                case SwordState.DiveHovering: return 5f;
                case SwordState.HitPause: return 2f;
                case SwordState.Returning: return 8f;
                case SwordState.DrillIntoButt: return 30f; // 增加入体时的自转表现
                default: return 10f;
            }
        }

        private Quaternion SwordLookRotation(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            dir.Normalize();
            dir.y = 90f;
            return Quaternion.LookRotation(dir, Vector3.up);
        }

        private static readonly Quaternion DownwardRot = Quaternion.LookRotation(
            Vector3.down + Vector3.forward * 0.001f, Vector3.forward);


        // --- 特殊机制：入体逻辑 ---

        private void TickDrillIntoButt(SwordAgent a)
        {
            if (OwnerPawn == null) return;

            Vector3 buttPos = OwnerPawn.DrawPos;
            Vector3 dir = a.currentPos - buttPos;

            if (dir.sqrMagnitude > 0.001f)
            {
                a.useDirectRot = true;
                a.directRot = SwordLookRotation(dir);
            }

            a.currentPos = Vector3.MoveTowards(a.currentPos, buttPos, Props.flySpeed * 1.5f);

            // 抵达目标点后隐藏
            if (Vector3.Distance(a.currentPos, buttPos) < 0.2f)
            {
                a.go?.SetActive(false);
                a.state = SwordState.Hidden;

                // 检查是否所有飞剑均已完全入体
                bool allHidden = true;
                foreach (var agent in _agents)
                {
                    if (agent.state != SwordState.Hidden)
                    {
                        allHidden = false;
                        break;
                    }
                }

                // 全部入体后赋予相关强化 Hediff
                if (allHidden && Props.hideHediff != null)
                {
                    if (!OwnerPawn.health.hediffSet.HasHediff(Props.hideHediff))
                    {
                        OwnerPawn.health.AddHediff(Props.hideHediff);
                    }
                }
            }
        }

        private void TickHidden(SwordAgent a, Vector3 orbitCenter)
        {
            if (OwnerPawn == null) return;

            // 存在其他仍在归元途中的飞剑时，挂机等待
            foreach (var agent in _agents)
            {
                if (agent.state == SwordState.DrillIntoButt) return;
            }

            // 当入体 Hediff 结束或被移除时，飞剑重新释出
            if (Props.hideHediff != null && !OwnerPawn.health.hediffSet.HasHediff(Props.hideHediff))
            {
                a.go?.SetActive(true);
                a.state = SwordState.Orbiting;
                a.currentPos = OwnerPawn.DrawPos;
            }
        }

        // --- 攻击与行为动作逻辑 ---

        private void TickOrbiting(SwordAgent a, Vector3 orbitCenter)
        {
            a.currentPos = Vector3.MoveTowards(a.currentPos, a.orbitSlot, 0.3f);
            Vector3 toCenter = (orbitCenter - a.currentPos).normalized;
            toCenter.y = 90f;
            Quaternion baseRot = Quaternion.LookRotation(toCenter, Vector3.up);
            a.targetRot = baseRot * Quaternion.Euler(0f, _selfSpin, 0f);
        }

        private void TickFlyToTarget(SwordAgent a)
        {
            if (!ValidateTarget(a)) return;

            Vector3 tPos = SafeTargetPos(a);
            float rad = a.circleAngle * Mathf.Deg2Rad;
            Vector3 circlePos = tPos + new Vector3(
                Mathf.Cos(rad) * Props.orbitAroundTarget, 0f,
                Mathf.Sin(rad) * Props.orbitAroundTarget);

            a.targetRot = SwordLookRotation(a.currentPos - tPos);
            a.currentPos = Vector3.MoveTowards(a.currentPos, circlePos, Props.flySpeed);

            if (Vector3.Distance(a.currentPos, circlePos) < 0.2f)
            {
                a.timer = Props.betweenAttackTicks + a.phaseOffset;
                a.phaseOffset = 0;
                a.state = SwordState.CircleTarget;
            }
        }

        private void TickCircleTarget(SwordAgent a)
        {
            if (!ValidateTarget(a)) return;

            a.circleAngle = (a.circleAngle + Props.orbitAroundSpeed) % 360f;
            Vector3 tPos = SafeTargetPos(a);
            float rad = a.circleAngle * Mathf.Deg2Rad;
            Vector3 circlePos = tPos + new Vector3(
                Mathf.Cos(rad) * Props.orbitAroundTarget, 0f,
                Mathf.Sin(rad) * Props.orbitAroundTarget);

            a.currentPos = Vector3.MoveTowards(a.currentPos, circlePos, Props.flySpeed * 1.5f);
            a.targetRot = SwordLookRotation(a.currentPos - tPos);

            if (a.timer > 0) { a.timer--; return; }

            a.nextAttack = RollAttackType();
            a.hitDealt = false;

            if (a.nextAttack == AttackType.Dive)
            {
                SetupDive(a);
                a.state = SwordState.DiveRising;
                return;
            }

            a.timer = (a.nextAttack == AttackType.Flurry) ? Props.aimTicks / 2 : Props.aimTicks;
            Vector3 dir = (tPos - a.currentPos); dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            dir.Normalize();
            a.windupPos = a.currentPos - dir * Props.attackWindupScale;
            a.state = SwordState.Aiming;
        }

        private void TickAiming(SwordAgent a)
        {
            if (!ValidateTarget(a)) return;

            Vector3 tPos = SafeTargetPos(a);
            int totalAim = (a.nextAttack == AttackType.Flurry) ? Props.aimTicks / 2 : Props.aimTicks;
            float aimProgress = 1f - (float)a.timer / Mathf.Max(totalAim, 1);

            if (aimProgress < 0.6f)
                a.currentPos = Vector3.MoveTowards(a.currentPos, a.windupPos, Props.flySpeed * 0.8f);

            a.targetRot = SwordLookRotation(a.currentPos - tPos);

            float shakeIntensity = 0.04f * (1f - aimProgress * 0.7f);
            Vector3 dir = (tPos - a.currentPos); dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                Vector3 perp = new Vector3(-dir.z, 0f, dir.x);
                float shakePhase = Find.TickManager.TicksGame * (0.6f + aimProgress * 0.8f);
                a.currentPos += perp * (Mathf.Sin(shakePhase) * shakeIntensity);
            }

            a.timer--;
            if (a.timer > 0) return;

            a.hitDealt = false;
            a.attackStart = a.currentPos;

            switch (a.nextAttack)
            {
                case AttackType.Thrust:
                    CalcThrustEnd(a);
                    a.attackProgress = 0f;
                    a.attackTotalDist = Vector3.Distance(a.currentPos, a.attackEnd);
                    a.state = SwordState.Thrusting;
                    break;
                case AttackType.Slash:
                    SetupSlashArc(a);
                    a.state = SwordState.Slashing;
                    break;
                case AttackType.SpinSlash:
                    SetupSpinSlash(a);
                    a.state = SwordState.SpinSlashing;
                    break;
                case AttackType.Flurry:
                    SetupFlurry(a);
                    a.state = SwordState.Flurrying;
                    break;
                case AttackType.Spiral:
                    SetupSpiral(a);
                    a.state = SwordState.SpiralDrilling;
                    break;
                default:
                    CalcThrustEnd(a);
                    a.attackProgress = 0f;
                    a.attackTotalDist = Vector3.Distance(a.currentPos, a.attackEnd);
                    a.state = SwordState.Thrusting;
                    break;
            }
        }

        private void TickThrusting(SwordAgent a)
        {
            if (!IsTargetAlive(a.target)) { TransitionAfterAttack(a); return; }

            float rawStep = Props.attackSpeed / Mathf.Max(a.attackTotalDist, 0.1f);
            a.attackProgress = Mathf.Clamp01(a.attackProgress + rawStep * 1.2f);
            float curved = Mathf.Pow(a.attackProgress, Props.attackAccelPower);
            a.currentPos = Vector3.Lerp(a.attackStart, a.attackEnd, curved);

            a.targetRot = SwordLookRotation(a.attackStart - a.attackEnd);

            if (!a.hitDealt && IsTargetAlive(a.target) &&
                Vector3.Distance(a.currentPos, SafeTargetPos(a)) < 0.55f)
            {
                TryDealDamage(a.target, DamageDefOf.Stab, Props.thrustDamage);
                a.hitDealt = true;
                EnterHitPause(a, SwordState.Thrusting);
                return;
            }
            if (a.attackProgress >= 1f) TransitionAfterAttack(a);
        }

        private void TickSlashing(SwordAgent a)
        {
            if (!IsTargetAlive(a.target)) { TransitionAfterAttack(a); return; }

            float arcLen = a.slashRadius * Mathf.Abs(a.slashEndAngle - a.slashStartAngle) * Mathf.Deg2Rad;
            float rawStep = Props.attackSpeed / Mathf.Max(arcLen, 0.1f);
            a.slashProgress = Mathf.Clamp01(a.slashProgress + rawStep * 1.1f);
            float curved = Mathf.Pow(a.slashProgress, Props.attackAccelPower * 0.8f);

            float currentAngle = Mathf.Lerp(a.slashStartAngle, a.slashEndAngle, curved);
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 arcPos = a.slashPivot + new Vector3(
                Mathf.Cos(rad) * a.slashRadius, 0f, Mathf.Sin(rad) * a.slashRadius);
            arcPos.y = a.currentPos.y;
            a.currentPos = arcPos;

            Vector3 tangent = new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            if (a.slashEndAngle < a.slashStartAngle) tangent = -tangent;
            a.targetRot = SwordLookRotation(tangent) * Quaternion.Euler(0f, 90f, 0f);

            if (!a.hitDealt && IsTargetAlive(a.target) &&
                Vector3.Distance(a.currentPos, SafeTargetPos(a)) < 0.7f)
            {
                TryDealDamage(a.target, DamageDefOf.Cut, Props.slashDamage);
                a.hitDealt = true;
                TryFleck(a.target, FleckDefOf.MetaPuff);
                EnterHitPause(a, SwordState.Slashing);
                return;
            }
            if (a.slashProgress >= 1f) TransitionAfterAttack(a);
        }

        private void SetupSpinSlash(SwordAgent a)
        {
            if (a.target == null) return;
            Vector3 tPos = SafeTargetPos(a);
            Vector3 offset = a.currentPos - tPos; offset.y = 0f;

            a.slashPivot = tPos;
            a.slashRadius = Props.spinSlashRadius;
            a.slashStartAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
            float sign = Rand.Bool ? 1f : -1f;
            a.slashEndAngle = a.slashStartAngle + sign * 270f;
            a.slashProgress = 0f;
            a.spinSelfAngle = 0f;
        }

        private void TickSpinSlashing(SwordAgent a)
        {
            if (!IsTargetAlive(a.target)) { TransitionAfterAttack(a); return; }

            a.slashProgress = Mathf.Clamp01(a.slashProgress + 0.025f);
            float curved = Mathf.Pow(a.slashProgress, 1.3f);

            float currentAngle = Mathf.Lerp(a.slashStartAngle, a.slashEndAngle, curved);
            float rad = currentAngle * Mathf.Deg2Rad;

            float radiusMod = 1f - 0.5f * Mathf.Sin(curved * Mathf.PI);
            float r = a.slashRadius * (0.5f + radiusMod * 0.5f);

            Vector3 arcPos = a.slashPivot + new Vector3(Mathf.Cos(rad) * r, 0f, Mathf.Sin(rad) * r);
            arcPos.y = a.currentPos.y;
            a.currentPos = arcPos;

            a.spinSelfAngle += 35f;
            Vector3 toTarget = (a.slashPivot - a.currentPos);
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f) toTarget = Vector3.forward;
            toTarget.Normalize();
            toTarget.y = 90f;

            a.useDirectRot = true;
            a.directRot = Quaternion.LookRotation(toTarget, Vector3.up) * Quaternion.Euler(0f, a.spinSelfAngle, 0f);

            if (!a.hitDealt && curved > 0.3f && curved < 0.7f && IsTargetAlive(a.target) &&
                Vector3.Distance(a.currentPos, SafeTargetPos(a)) < 0.9f)
            {
                TryDealDamage(a.target, DamageDefOf.Cut, Props.spinSlashDamage);
                a.hitDealt = true;
                TryFleck(a.target, FleckDefOf.PsycastSkipFlashEntry, 1.2f);
            }

            if (a.slashProgress >= 1f) TransitionAfterAttack(a);
        }

        private void SetupDive(SwordAgent a)
        {
            if (a.target == null) return;
            Vector3 tPos = SafeTargetPos(a);

            float offsetX = Rand.Range(-0.5f, 0.5f);
            float offsetZ = Rand.Range(-0.5f, 0.5f);
            a.diveApex = new Vector3(tPos.x + offsetX, tPos.y + Props.diveHeight, tPos.z + offsetZ);
            a.diveTarget = tPos;
            a.attackStart = a.currentPos;
            a.attackProgress = 0f;
            a.diveStartRot = a.currentRot;
        }

        private void TickDiveRising(SwordAgent a)
        {
            if (!ValidateTarget(a)) return;

            a.attackProgress = Mathf.Clamp01(a.attackProgress + 0.04f);
            float curved = 1f - Mathf.Pow(1f - a.attackProgress, 2f);
            a.currentPos = Vector3.Lerp(a.attackStart, a.diveApex, curved);

            float flipProgress = Mathf.Clamp01(a.attackProgress * 1.2f);
            a.targetRot = Quaternion.Slerp(a.diveStartRot, DownwardRot, flipProgress);

            if (a.attackProgress >= 1f)
            {
                a.timer = Props.diveHoverTicks;
                a.state = SwordState.DiveHovering;
            }
        }

        private void TickDiveHovering(SwordAgent a)
        {
            if (!ValidateTarget(a)) return;

            if (IsTargetAlive(a.target))
                a.diveTarget = SafeTargetPos(a);

            a.targetRot = DownwardRot;

            float hoverProgress = 1f - (float)a.timer / Mathf.Max(Props.diveHoverTicks, 1);
            float shake = 0.02f + hoverProgress * 0.04f;
            a.currentPos += new Vector3(
                Mathf.Sin(Find.TickManager.TicksGame * 1.2f) * shake,
                Mathf.Sin(Find.TickManager.TicksGame * 0.7f) * shake * 0.3f,
                Mathf.Cos(Find.TickManager.TicksGame * 1.5f) * shake);

            a.timer--;
            if (a.timer <= 0)
            {
                a.attackStart = a.currentPos;
                a.attackEnd = a.diveTarget - new Vector3(0f, 0.3f, 0f);
                a.attackProgress = 0f;
                a.attackTotalDist = Vector3.Distance(a.currentPos, a.attackEnd);
                a.hitDealt = false;
                a.state = SwordState.DivePlunging;
            }
        }

        private void TickDivePlunging(SwordAgent a)
        {
            if (!IsTargetAlive(a.target)) { TransitionAfterAttack(a); return; }

            float rawStep = (Props.attackSpeed * 1.8f) / Mathf.Max(a.attackTotalDist, 0.1f);
            a.attackProgress = Mathf.Clamp01(a.attackProgress + rawStep);
            float curved = Mathf.Pow(a.attackProgress, 3f);
            a.currentPos = Vector3.Lerp(a.attackStart, a.attackEnd, curved);

            a.targetRot = DownwardRot;

            if (!a.hitDealt && IsTargetAlive(a.target) &&
                Vector3.Distance(a.currentPos, SafeTargetPos(a)) < 0.6f)
            {
                TryDealDamage(a.target, DamageDefOf.Stab, Props.diveDamage);
                a.hitDealt = true;
                TryFleck(a.target, FleckDefOf.PsycastSkipFlashEntry, 2f);
                TryFleck(a.target, FleckDefOf.MetaPuff);
                EnterHitPause(a, SwordState.DivePlunging, Props.hitPauseTicks + 3);
                return;
            }
            if (a.attackProgress >= 1f) TransitionAfterAttack(a);
        }

        private void SetupFlurry(SwordAgent a)
        {
            a.flurryHitsRemaining = Props.flurryHitCount;
            a.flurrySubTimer = 0;
            a.flurrySubHit = false;
            SetupFlurrySubThrust(a);
        }

        private void SetupFlurrySubThrust(SwordAgent a)
        {
            if (!IsTargetAlive(a.target)) return;
            Vector3 tPos = SafeTargetPos(a);
            Vector3 dir = (tPos - a.currentPos); dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            dir.Normalize();

            float randAngle = Rand.Range(-20f, 20f);
            Vector3 thrustDir = Quaternion.Euler(0f, randAngle, 0f) * dir;

            a.attackStart = a.currentPos;
            a.attackEnd = tPos + thrustDir * 0.4f;
            a.attackProgress = 0f;
            a.attackTotalDist = Vector3.Distance(a.currentPos, a.attackEnd);
            a.flurrySubHit = false;
            a.flurrySubTimer = 12;
        }

        private void TickFlurrying(SwordAgent a)
        {
            if (!IsTargetAlive(a.target)) { TransitionAfterAttack(a); return; }

            a.flurrySubTimer--;

            if (a.flurrySubTimer > 6)
            {
                float step = Props.attackSpeed * 1.5f / Mathf.Max(a.attackTotalDist, 0.1f);
                a.attackProgress = Mathf.Clamp01(a.attackProgress + step);
                float curved = Mathf.Pow(a.attackProgress, 1.8f);
                a.currentPos = Vector3.Lerp(a.attackStart, a.attackEnd, curved);

                a.targetRot = SwordLookRotation(a.attackStart - a.attackEnd);

                if (!a.flurrySubHit && IsTargetAlive(a.target) &&
                    Vector3.Distance(a.currentPos, SafeTargetPos(a)) < 0.6f)
                {
                    TryDealDamage(a.target, DamageDefOf.Stab, Props.flurryDamagePerHit);
                    a.flurrySubHit = true;
                    TryFleckSparks(a.target);
                }
            }
            else
            {
                Vector3 tPos = SafeTargetPos(a);
                Vector3 dir = (tPos - a.currentPos); dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
                dir.Normalize();
                Vector3 retreatPos = tPos - dir * Props.orbitAroundTarget * 0.7f;
                a.currentPos = Vector3.MoveTowards(a.currentPos, retreatPos, Props.attackSpeed * 1.2f);

                a.targetRot = SwordLookRotation(a.currentPos - tPos);
            }

            if (a.flurrySubTimer <= 0)
            {
                a.flurryHitsRemaining--;
                if (a.flurryHitsRemaining <= 0 || !IsTargetAlive(a.target))
                {
                    TransitionAfterAttack(a);
                    return;
                }
                SetupFlurrySubThrust(a);
            }
        }

        private void SetupSpiral(SwordAgent a)
        {
            if (a.target == null) return;
            Vector3 tPos = SafeTargetPos(a);
            Vector3 offset = a.currentPos - tPos; offset.y = 0f;

            a.spiralStart = a.currentPos;
            a.spiralAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
            a.spiralRadius = Mathf.Max(offset.magnitude, 1f);
            a.spiralProgress = 0f;
            a.attackStart = a.currentPos;
        }

        private void TickSpiralDrilling(SwordAgent a)
        {
            if (!IsTargetAlive(a.target)) { TransitionAfterAttack(a); return; }

            a.spiralProgress = Mathf.Clamp01(a.spiralProgress + 0.02f);
            float curved = Mathf.Pow(a.spiralProgress, 1.5f);

            a.spiralAngle += 18f;
            float currentRadius = a.spiralRadius * (1f - curved);

            Vector3 tPos = SafeTargetPos(a);
            Vector3 basePos = Vector3.Lerp(a.spiralStart, tPos, curved);

            float rad = a.spiralAngle * Mathf.Deg2Rad;
            Vector3 spiralOffset = new Vector3(
                Mathf.Cos(rad) * currentRadius, 0f, Mathf.Sin(rad) * currentRadius);
            a.currentPos = basePos + spiralOffset;

            Vector3 toTarget = (tPos - a.currentPos);
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f) toTarget = Vector3.forward;
            toTarget.Normalize();
            toTarget.y = 90f;

            a.useDirectRot = true;
            a.directRot = Quaternion.LookRotation(toTarget, Vector3.up) * Quaternion.Euler(0f, a.spiralAngle * 2f, 0f);

            if (!a.hitDealt && curved > 0.75f && IsTargetAlive(a.target) &&
                Vector3.Distance(a.currentPos, tPos) < 0.7f)
            {
                TryDealDamage(a.target, DamageDefOf.Stab, Props.spiralDamage);
                a.hitDealt = true;
                TryFleckSparks(a.target);
                TryFleck(a.target, FleckDefOf.PsycastSkipFlashEntry, 1f);
                EnterHitPause(a, SwordState.SpiralDrilling);
                return;
            }

            if (a.spiralProgress >= 1f)
            {
                Vector3 throughDir = (tPos - a.spiralStart);
                throughDir.y = 0f;
                if (throughDir.sqrMagnitude < 0.001f) throughDir = Vector3.forward;
                throughDir.Normalize();
                a.attackEnd = tPos + throughDir * OvershootDist;
                a.attackStart = a.currentPos;
                a.attackProgress = 0f;
                a.attackTotalDist = Vector3.Distance(a.currentPos, a.attackEnd);
                a.state = SwordState.Thrusting;
            }
        }

        private void TickHitPause(SwordAgent a)
        {
            Vector3 shakeDir = (a.attackEnd - a.attackStart);
            shakeDir.y = 0f;
            if (shakeDir.sqrMagnitude > 0.001f)
            {
                shakeDir.Normalize();
                float shake = Mathf.Sin(Find.TickManager.TicksGame * 3f) * 0.015f;
                a.currentPos += new Vector3(-shakeDir.z, 0f, shakeDir.x) * shake;
            }

            a.timer--;
            if (a.timer <= 0)
            {
                switch (a.stateAfterPause)
                {
                    case SwordState.Thrusting:
                        a.attackProgress = Mathf.Clamp01(a.attackProgress + 0.15f);
                        a.state = SwordState.Thrusting;
                        break;
                    case SwordState.Slashing:
                        a.slashProgress = Mathf.Clamp01(a.slashProgress + 0.15f);
                        a.state = SwordState.Slashing;
                        break;
                    case SwordState.DivePlunging:
                        a.attackProgress = Mathf.Clamp01(a.attackProgress + 0.2f);
                        a.state = SwordState.DivePlunging;
                        break;
                    case SwordState.SpiralDrilling:
                        a.spiralProgress = Mathf.Clamp01(a.spiralProgress + 0.15f);
                        a.state = SwordState.SpiralDrilling;
                        break;
                    default:
                        TransitionAfterAttack(a);
                        break;
                }
            }
        }

        private void TickReturning(SwordAgent a, Vector3 orbitCenter)
        {
            Vector3 toCenter = (orbitCenter - a.currentPos).normalized;
            toCenter.y = 90f;
            a.targetRot = Quaternion.LookRotation(toCenter, Vector3.up);

            a.currentPos = Vector3.MoveTowards(a.currentPos, a.orbitSlot, Props.returnSpeed);
            if (Vector3.Distance(a.currentPos, a.orbitSlot) < 0.15f)
                a.state = SwordState.Orbiting;
        }

        private void TransitionAfterAttack(SwordAgent a)
        {
            a.circleAngle = (a.circleAngle + 180f) % 360f;
            a.timer = Props.betweenAttackTicks;

            if (!IsTargetAlive(a.target))
            {
                a.target = null;
                var enemies = GatherEnemies();
                if (enemies.Count > 0)
                {
                    a.target = PickBestTarget(a, enemies);
                    a.state = SwordState.FlyToTarget;
                    return;
                }
                a.state = SwordState.Returning;
                return;
            }
            a.state = SwordState.CircleTarget;
        }

        private void EnterHitPause(SwordAgent a, SwordState resumeState, int pauseTicks = -1)
        {
            a.stateAfterPause = resumeState;
            a.timer = pauseTicks > 0 ? pauseTicks : Props.hitPauseTicks;
            a.state = SwordState.HitPause;
        }

        private void CalcThrustEnd(SwordAgent a)
        {
            if (a.target == null) return;
            Vector3 tPos = SafeTargetPos(a);
            Vector3 dir = (tPos - a.currentPos); dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            dir.Normalize();
            a.attackEnd = tPos + dir * OvershootDist;
        }

        private void SetupSlashArc(SwordAgent a)
        {
            if (a.target == null) return;
            Vector3 tPos = SafeTargetPos(a);
            a.slashPivot = tPos;
            Vector3 offset = a.currentPos - tPos; offset.y = 0f;
            a.slashRadius = Mathf.Max(offset.magnitude, 0.5f);
            a.slashStartAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
            float sweepSign = Rand.Bool ? 1f : -1f;
            a.slashEndAngle = a.slashStartAngle + sweepSign * Props.slashArcDegrees;
            a.slashProgress = 0f;
        }

        private bool IsTargetAlive(Pawn t)
        {
            return t != null && !t.Dead && !t.Downed && t.Spawned;
        }

        private Vector3 SafeTargetPos(SwordAgent a)
        {
            if (a.target != null && a.target.Spawned && !a.target.Destroyed)
            {
                var p = a.target.DrawPos;
                p.y += OrbitHeight;
                return p;
            }
            return a.attackEnd;
        }

        private bool ValidateTarget(SwordAgent a)
        {
            if (IsTargetAlive(a.target)) return true;
            a.target = null;
            var enemies = GatherEnemies();
            if (enemies.Count > 0)
            {
                a.target = PickBestTarget(a, enemies);
                a.state = SwordState.FlyToTarget;
                return false;
            }
            a.state = SwordState.Returning;
            return false;
        }

        private void TryDealDamage(Pawn target, DamageDef dd, int dmg)
        {
            if (target == null || target.Dead || !target.Spawned) return;
            BodyPartRecord part = target.health.hediffSet.GetRandomNotMissingPart(
                dd, BodyPartHeight.Undefined, BodyPartDepth.Outside);
            target.TakeDamage(new DamageInfo(dd, dmg, 0.5f, instigator: OwnerPawn, hitPart: part));
            TryFleckSparks(target);
        }

        private void TryFleck(Pawn target, FleckDef def, float scale = 1f)
        {
            if (target == null || !target.Spawned || target.Map == null) return;
            FleckMaker.Static(target.DrawPos, target.Map, def, scale);
        }

        private void TryFleckSparks(Pawn target)
        {
            if (target == null || !target.Spawned || target.Map == null) return;
            FleckMaker.ThrowMicroSparks(target.DrawPos, target.Map);
        }

        private Vector3 GetOrbitCenter()
        {
            var c = OwnerPawn.DrawPos;
            c.y += OrbitHeight;
            return c;
        }
    }
}