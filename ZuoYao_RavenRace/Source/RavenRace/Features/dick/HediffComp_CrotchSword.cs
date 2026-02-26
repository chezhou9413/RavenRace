using ChezhouLib.ALLmap;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RavenRace.Features.dick
{
    public class HediffCompProperties_CrotchSword : HediffCompProperties
    {
        public string prefabKey = "dick_xuancai";
        public float targetScale = 3f;
        public float scaleSpeed = 0.05f;
        public int lifespanTicks = 600;
        public Vector3 crotchOffset = new Vector3(0f, 0.5f, -0.2f);

        //模型初始朝向修正
        public Vector3 baseRotation = new Vector3(90f, 0f, 0f);

        //技能参数
        public float slamDamage = 200f;         // 基础伤害
        public float slamRange = 50f;           // AOE长度
        public float slamWidth = 15f;           // AOE宽度

        //动画时长
        public int windupTicks = 50;            // 蓄力
        public int strikeTicks = 8;             // 劈砍
        public int holdTicks = 70;              // 砸地僵直
        public int returnTicks = 60;            // 收招

        //动画角度
        public float windupStartPitch = 0f;     // 起始角
        public float windupEndPitch = -90f;     // 蓄力角
        public float strikeLandPitch = 0f;      // 砸地角

        public HediffCompProperties_CrotchSword()
        {
            compClass = typeof(HediffComp_CrotchSword);
        }
    }

    public class HediffComp_CrotchSword : HediffComp
    {
        public HediffCompProperties_CrotchSword Props => (HediffCompProperties_CrotchSword)props;

        private GameObject _swordGo;
        private float _currentScale = 0f;
        private int _ageTicks = 0;
        private bool _isShrinking = false;

        private enum SlamPhase { None, Windup, Strike, Hold, Return }

        private SlamPhase _slamPhase = SlamPhase.None;
        private int _phaseTicks = 0;
        private LocalTargetInfo _slamTarget;
        private bool _hasDealtDamage = false;

        private float _currentSwingPitch = 0f;

        private bool _isSlamming => _slamPhase != SlamPhase.None;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            CreateSword();
        }

        private void CreateSword()
        {
            if (_swordGo != null) return;

            if (abDatabase.prefabDataBase.TryGetValue(Props.prefabKey, out GameObject prefab) && prefab != null)
            {
                _swordGo = UnityEngine.Object.Instantiate(prefab);
                _swordGo.name = $"CrotchSword_{Pawn.ThingID}_{parent.def.defName}";

                _currentScale = 0f;
                _swordGo.transform.localScale = Vector3.zero;
                _swordGo.SetActive(true);
            }
            else
            {
                Log.ErrorOnce($"[HediffComp_CrotchSword] 找不到预制体: {Props.prefabKey}", Props.prefabKey.GetHashCode());
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            IEnumerable<Gizmo> baseGizmos = base.CompGetGizmos();
            if (baseGizmos != null)
            {
                foreach (var gizmo in baseGizmos)
                {
                    yield return gizmo;
                }
            }

            if (Pawn != null && Pawn.Faction == Faction.OfPlayer && !Pawn.Dead && !_isShrinking)
            {
                Command_Target excaliburBtn = new Command_Target
                {
                    defaultLabel = "Ex咖喱棒",
                    defaultDesc = "朝着目标方向用力拍击地面，造成直线范围AOE伤害。",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                    targetingParams = new TargetingParameters
                    {
                        canTargetLocations = true,
                        canTargetPawns = true,
                        canTargetBuildings = true
                    },
                    action = delegate (LocalTargetInfo target)
                    {
                        StartSlam(target);
                    }
                };

                if (_isSlamming)
                {
                    excaliburBtn.Disable("正在释放中...");
                }

                yield return excaliburBtn;
            }
        }

        private void StartSlam(LocalTargetInfo target)
        {
            if (_isSlamming) return;

            _slamPhase = SlamPhase.Windup;
            _phaseTicks = 0;
            _slamTarget = target;
            _hasDealtDamage = false;
            _currentSwingPitch = Props.windupStartPitch;

            if (target.HasThing || target.Cell.IsValid)
            {
                Pawn.rotationTracker.FaceTarget(target);
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            //如果小人不在地图上、死了，销毁表现模型并直接返回
            if (Pawn == null || !Pawn.Spawned || Pawn.Map == null || Pawn.Dead)
            {
                DestroySword();
                return;
            }

            // 2. 读档恢复或模型意外丢失时的懒加载机制
            if (_swordGo == null)
            {
                CreateSword();

                // 如果尝试重建后依然失败（如缺失前置Mod），强行移除此Hediff防止死循环与报错
                if (_swordGo == null)
                {
                    Log.Warning($"[HediffComp_CrotchSword] 模型恢复失败，强行移除 Hediff 防止状态卡死。");
                    Pawn.health.RemoveHediff(parent);
                    return;
                }
            }

            //寿命计算与模型缩放逻辑
            _ageTicks++;

            if (!_isShrinking && _ageTicks >= Props.lifespanTicks)
            {
                _slamPhase = SlamPhase.None;
                _isShrinking = true;
            }

            if (_isShrinking)
            {
                _currentScale -= Props.scaleSpeed;
                if (_currentScale <= 0f)
                {
                    _currentScale = 0f;
                    DestroySword();
                    Pawn.health.RemoveHediff(parent);
                    return; //移除自身后必须终止本帧 Tick
                }
            }
            else
            {
                _currentScale += Props.scaleSpeed;
                if (_currentScale > Props.targetScale)
                    _currentScale = Props.targetScale;
            }

            //处理攻击动画
            if (_slamPhase != SlamPhase.None)
            {
                ProcessSlamAnimation();
            }

            //应用变换
            _swordGo.transform.localScale = Vector3.one * _currentScale;
            UpdateTransform();
        }

        //将原有的庞大 switch 逻辑抽取为独立方法
        private void ProcessSlamAnimation()
        {
            _phaseTicks++;

            switch (_slamPhase)
            {
                case SlamPhase.Windup:
                    {
                        float t = Mathf.Clamp01((float)_phaseTicks / Props.windupTicks);
                        t = t * t * (3f - 2f * t);
                        _currentSwingPitch = Mathf.Lerp(Props.windupStartPitch, Props.windupEndPitch, t);

                        if (_phaseTicks >= Props.windupTicks)
                        {
                            _slamPhase = SlamPhase.Strike;
                            _phaseTicks = 0;
                        }
                        break;
                    }

                case SlamPhase.Strike:
                    {
                        float t = Mathf.Clamp01((float)_phaseTicks / Props.strikeTicks);
                        t = t * t;
                        _currentSwingPitch = Mathf.Lerp(Props.windupEndPitch, Props.strikeLandPitch, t);

                        if (!_hasDealtDamage && _phaseTicks >= Props.strikeTicks / 2)
                        {
                            DealLineDamage(_slamTarget);
                            _hasDealtDamage = true;
                        }

                        if (_phaseTicks >= Props.strikeTicks)
                        {
                            _currentSwingPitch = Props.strikeLandPitch;
                            _slamPhase = SlamPhase.Hold;
                            _phaseTicks = 0;
                        }
                        break;
                    }

                case SlamPhase.Hold:
                    {
                        float decayRatio = 1f - Mathf.Clamp01((float)_phaseTicks / Props.holdTicks);
                        float tremble = Mathf.Sin(_phaseTicks * 2.5f) * 3f * decayRatio;
                        _currentSwingPitch = Props.strikeLandPitch + tremble;

                        if (_phaseTicks >= Props.holdTicks)
                        {
                            _currentSwingPitch = Props.strikeLandPitch;
                            _slamPhase = SlamPhase.Return;
                            _phaseTicks = 0;
                        }
                        break;
                    }

                case SlamPhase.Return:
                    {
                        float t = Mathf.Clamp01((float)_phaseTicks / Props.returnTicks);
                        t = t * t * (3f - 2f * t);
                        _currentSwingPitch = Mathf.Lerp(Props.strikeLandPitch, Props.windupStartPitch, t);

                        if (_phaseTicks >= Props.returnTicks)
                        {
                            _currentSwingPitch = Props.windupStartPitch;
                            _slamPhase = SlamPhase.None;
                            _phaseTicks = 0;
                        }
                        break;
                    }
            }
        }

        private void DealLineDamage(LocalTargetInfo target)
        {
            Vector3 startPos = Pawn.DrawPos;
            Vector3 endPos = target.HasThing ? target.Thing.DrawPos : target.Cell.ToVector3Shifted();

            if (startPos == endPos) endPos.x += 0.1f;

            Vector3 direction = (endPos - startPos).normalized;

            SpawnShockwaveEffects(startPos, direction);

            //防止单次挥砍多段判定
            HashSet<Thing> alreadyHit = new HashSet<Thing>();

            for (float dist = 1f; dist <= Props.slamRange; dist += 0.5f)
            {
                Vector3 checkPos = startPos + direction * dist;
                IntVec3 cell = checkPos.ToIntVec3();

                if (!cell.InBounds(Pawn.Map)) break;

                List<Thing> thingsHit = new List<Thing>();
                thingsHit.AddRange(cell.GetThingList(Pawn.Map));

                for (int i = thingsHit.Count - 1; i >= 0; i--)
                {
                    Thing t = thingsHit[i];

                    if (t != Pawn && (t is Pawn || t is Building) && !alreadyHit.Contains(t))
                    {
                        alreadyHit.Add(t);

                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, Props.slamDamage, 2f, -1f, Pawn, null, Pawn.def);
                        t.TakeDamage(dinfo);

                        SpawnHitExplosion(t.Position, Pawn.Map);
                    }
                }
            }

            IntVec3 impactCell = endPos.ToIntVec3();
            if (impactCell.InBounds(Pawn.Map))
            {
                GenExplosion.DoExplosion(
                    center: impactCell,
                    map: Pawn.Map,
                    radius: Props.slamWidth * 0.5f,
                    damType: DamageDefOf.Blunt,
                    instigator: Pawn,
                    damAmount: (int)(Props.slamDamage * 0.5f),
                    armorPenetration: 1f,
                    explosionSound: SoundDefOf.Explosion_FirefoamPopper,
                    weapon: null,
                    projectile: null,
                    intendedTarget: null,
                    postExplosionSpawnThingDef: null,
                    chanceToStartFire: 0f,
                    doVisualEffects: true
                );
            }
        }

        private void SpawnShockwaveEffects(Vector3 startPos, Vector3 direction)
        {
            float spacing = 1.5f;

            for (float dist = spacing; dist <= Props.slamRange; dist += spacing)
            {
                Vector3 effectPos = startPos + direction * dist;
                IntVec3 cell = effectPos.ToIntVec3();

                if (!cell.InBounds(Pawn.Map)) break;

                float jitter = Rand.Range(-0.15f, 0.15f);
                Vector3 jitteredPos = effectPos + new Vector3(jitter, 0f, jitter);

                FleckMaker.ThrowLightningGlow(jitteredPos, Pawn.Map, Rand.Range(0.6f, 1.2f));
                FleckMaker.ThrowDustPuff(jitteredPos, Pawn.Map, Rand.Range(0.5f, 1.0f));

                if (dist % (spacing * 2f) < spacing)
                {
                    FleckMaker.ThrowMicroSparks(jitteredPos, Pawn.Map);
                }
            }
        }

        //纯视觉特效，规避 GenExplosion 音效崩溃
        private void SpawnHitExplosion(IntVec3 cell, Map map)
        {
            if (!cell.InBounds(map)) return;

            Vector3 loc = cell.ToVector3Shifted();

            FleckMaker.ThrowMicroSparks(loc, map);
            FleckMaker.ThrowDustPuff(loc + new Vector3(Rand.Range(-0.2f, 0.2f), 0f, Rand.Range(-0.2f, 0.2f)), map, 1.2f);
            //含受击音效
        }

        private void UpdateTransform()
        {
            if (_swordGo == null || Pawn == null) return;

            Vector3 drawPos = Pawn.DrawPos;
            float angle = Pawn.Rotation.AsAngle;

            if (_isSlamming && _slamTarget.IsValid)
            {
                Vector3 targetPos = _slamTarget.HasThing ? _slamTarget.Thing.DrawPos : _slamTarget.Cell.ToVector3Shifted();
                angle = (targetPos - drawPos).AngleFlat();
            }

            float dynamicYOffset = 0f;
            Vector3 directionTweak = Vector3.zero;

            if (Pawn.Rotation == Rot4.North) dynamicYOffset = -0.05f;
            else if (Pawn.Rotation == Rot4.South) dynamicYOffset = 0.05f;
            else if (Pawn.Rotation == Rot4.East)
            {
                dynamicYOffset = 0.04f;
                directionTweak = new Vector3(-0.2f, 0f, 0.1f);
            }
            else if (Pawn.Rotation == Rot4.West)
            {
                dynamicYOffset = 0.04f;
                directionTweak = new Vector3(0.2f, 0f, 0.1f);
            }

            Vector3 targetPosWithOffset = drawPos + new Vector3(Props.crotchOffset.x, dynamicYOffset, Props.crotchOffset.z) + directionTweak;
            Vector3 rotatedOffset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0, 0, 0.3f);

            _swordGo.transform.position = targetPosWithOffset + rotatedOffset;

            Quaternion pawnFacing = Quaternion.Euler(0f, angle, 0f);
            Quaternion modelCorrection = Quaternion.Euler(Props.baseRotation.x, Props.baseRotation.y, Props.baseRotation.z);

            if (_isSlamming)
            {
                Quaternion swingRotation = Quaternion.Euler(_currentSwingPitch, 0f, 0f);
                _swordGo.transform.rotation = pawnFacing * swingRotation * modelCorrection;
            }
            else
            {
                _swordGo.transform.rotation = pawnFacing * modelCorrection;
            }
        }

        private void DestroySword()
        {
            if (_swordGo != null)
            {
                UnityEngine.Object.Destroy(_swordGo);
                _swordGo = null;
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            DestroySword();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();

            // 剔除了 PostLoadInit 中的实例化逻辑，全部交给 Tick 处理
            Scribe_Values.Look(ref _currentScale, "currentScale", 0f);
            Scribe_Values.Look(ref _ageTicks, "ageTicks", 0);
            Scribe_Values.Look(ref _isShrinking, "isShrinking", false);
            Scribe_Values.Look(ref _slamPhase, "slamPhase", SlamPhase.None);
            Scribe_Values.Look(ref _phaseTicks, "phaseTicks", 0);
            Scribe_TargetInfo.Look(ref _slamTarget, "slamTarget");
            Scribe_Values.Look(ref _hasDealtDamage, "hasDealtDamage", false);
            Scribe_Values.Look(ref _currentSwingPitch, "currentSwingPitch", 0f);
        }
    }
}