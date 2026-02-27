using RimWorld;
using System.Collections.Generic;
using Verse;
using UnityEngine; // 需要引用UnityEngine来获取角度

namespace RavenRace.Features.CustomPawn.Ayaya // 你的命名空间
{
    public class Projectile_TouhouDanmaku : Projectile
    {
        private static readonly List<Thing> TmpThings = new List<Thing>();
        private HashSet<Pawn> checkedPawnsInTick = new HashSet<Pawn>();

        // 方便地获取我们自定义的 projectile 属性
        private ProjectileProperties_TouhouDanmaku DanmakuProps => def.projectile as ProjectileProperties_TouhouDanmaku;

        protected override void Tick()
        {
            // 安全检查，如果属性、发射者或地图无效，则执行默认行为
            if (DanmakuProps == null || this.launcher == null || this.Map == null)
            {
                base.Tick();
                return;
            }

            // 如果已经命中或销毁，则不再执行
            if (this.Destroyed)
            {
                return;
            }

            checkedPawnsInTick.Clear();

            // 使用GenRadial工具类获取以抛射体为中心、指定半径内的所有格子
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(this.Position, DanmakuProps.collisionRadius, true))
            {
                if (!cell.InBounds(this.Map))
                {
                    continue;
                }

                List<Thing> thingsInCell = this.Map.thingGrid.ThingsListAtFast(cell);
                if (thingsInCell.Count > 0)
                {
                    TmpThings.Clear();
                    TmpThings.AddRange(thingsInCell);

                    for (int i = 0; i < TmpThings.Count; i++)
                    {
                        Pawn pawn = TmpThings[i] as Pawn;
                        if (pawn != null)
                        {
                            if (checkedPawnsInTick.Contains(pawn))
                            {
                                continue;
                            }
                            checkedPawnsInTick.Add(pawn);

                            // 检查目标有效性
                            if (pawn != this.launcher && pawn.HostileTo(this.launcher))
                            {
                                // --- 核心修正部分 ---
                                // 1. 手动造成伤害
                                ApplyDamage(pawn);

                                // 2. 直接销毁弹丸
                                this.Destroy(DestroyMode.Vanish);

                                // 3. 立即返回，终止后续所有逻辑
                                return;
                            }
                        }
                    }
                }
            }

            // 如果没有命中任何东西，继续正常飞行
            base.Tick();
        }

        /// <summary>
        /// 一个新的辅助方法，用于手动创建并施加伤害。
        /// </summary>
        /// <param name="hitPawn">被命中的Pawn</param>
        private void ApplyDamage(Pawn hitPawn)
        {
            // 从XML中获取伤害定义和基础伤害值
            float amount = this.def.projectile.GetDamageAmount(this.launcher);
            float armorPen = this.def.projectile.GetArmorPenetration(this.launcher);
            DamageDef damageDef = this.def.projectile.damageDef;

            // 计算伤害角度，通常是弹丸的飞行方向
            float angle = this.ExactRotation.eulerAngles.y;

            // 创建伤害信息 (DamageInfo)
            DamageInfo dinfo = new DamageInfo(
                damageDef,
                amount,
                armorPen,
                angle,
                this.launcher, // 伤害来源
                null,         // 命中部位 (null表示随机)
                this.EquipmentDef, // 造成伤害的武器
                DamageInfo.SourceCategory.ThingOrUnknown,
                this.intendedTarget.Thing // 预定目标 (可以保持原样)
            );

            // 对目标施加伤害
            hitPawn.TakeDamage(dinfo);
        }

        /// <summary>
        /// 当子弹飞行到终点或撞墙时，依然会调用这个方法。
        /// 我们可以保留它，让它处理非Pawn的命中情况。
        /// </summary>
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            // 如果撞到的是我们已经处理过的Pawn，就不再重复处理
            if (hitThing is Pawn)
            {
                // 如果是手动销毁的，hitThing可能为null
                if (hitThing != null)
                {
                    // 在这里可以添加一些命中效果，如爆炸或声音
                }
            }
            else // 如果撞到的是墙壁或其他非Pawn物体
            {
                base.Impact(hitThing, blockedByShield);
            }
        }
    }
}