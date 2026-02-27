using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound; // 确保引用了Sound命名空间

namespace RavenRace.Features.CustomPawn.Ayaya 
{
    // 用于在XML中配置扇形射击的参数
    public class VerbProperties_Danmaku : VerbProperties
    {
        // 每次射击发射的弹幕数量
        public int projectilesPerShot = 1;
        // 整个扇形展开的总角度
        public float spreadAngle = 0f;
    }

    // 自定义的射击动作类
    public class Verb_ShootDanmaku : Verb_Shoot
    {
        // 方便地获取我们的自定义属性
        private VerbProperties_Danmaku DanmakuVerbProps => verbProps as VerbProperties_Danmaku;

        // 重写核心的射击方法
        protected override bool TryCastShot()
        {
            // 如果自定义属性无效，则退回原版射击逻辑
            if (DanmakuVerbProps == null || DanmakuVerbProps.projectilesPerShot <= 1)
            {
                return base.TryCastShot();
            }

            int projectilesToLaunch = DanmakuVerbProps.projectilesPerShot;
            float totalAngle = DanmakuVerbProps.spreadAngle;
            // 计算每个弹幕之间的夹角 (如果只有一发，则没有夹角)
            float angleStep = (projectilesToLaunch > 1) ? totalAngle / (projectilesToLaunch - 1) : 0f;
            // 计算起始角度，让整个扇形对称
            float startAngle = (projectilesToLaunch > 1) ? -totalAngle / 2f : 0f;

            // 获取原始目标，用于计算中心方向
            LocalTargetInfo originalTarget = CurrentTarget;
            // 确保caster存在
            if (caster == null) return false;

            Vector3 shotDirection = (originalTarget.Cell - caster.Position).ToVector3();

            // 循环发射每一个弹幕
            for (int i = 0; i < projectilesToLaunch; i++)
            {
                // 计算当前弹幕的角度偏移
                float currentAngle = startAngle + (i * angleStep);

                // 使用四元数旋转中心方向向量，得到新的方向
                Vector3 rotatedDirection = Quaternion.AngleAxis(currentAngle, Vector3.up) * shotDirection;

                // 创建一个新的目标信息，这个目标点在新的方向上，距离足够远
                // 加上caster.Position来确保目标点是世界坐标
                LocalTargetInfo newTarget = new LocalTargetInfo(caster.Position + rotatedDirection.ToIntVec3());

                // 使用与原版几乎相同的逻辑来生成和发射抛射体
                // 注意：这里我们传入了新的 newTarget
                Projectile projectile = (Projectile)GenSpawn.Spawn(this.verbProps.defaultProjectile, caster.Position, caster.Map);

                // 修正 #1：使用 caster.DrawPos 替代 DrawPos
                projectile.Launch(caster, caster.DrawPos, newTarget, originalTarget, ProjectileHitFlags.IntendedTarget, equipment: this.EquipmentSource);
            }

            // 发射后触发原版的后处理（如播放声音、进入冷却）
            if (this.verbProps.soundCast != null)
            {
                // 修正 #2：使用 SoundInfo.InMap(caster) 替代 new SoundInfo(caster)
                this.verbProps.soundCast.PlayOneShot(SoundInfo.InMap(this.caster));
            }
            if (this.CasterIsPawn)
            {
                this.CasterPawn.records.Increment(RecordDefOf.ShotsFired);
            }

            return true; // 表示射击成功
        }
    }
}