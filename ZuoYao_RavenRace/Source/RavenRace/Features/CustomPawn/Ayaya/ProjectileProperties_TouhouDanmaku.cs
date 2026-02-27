using Verse;

namespace RavenRace.Features.CustomPawn.Ayaya // <-- 请替换为你的Mod的命名空间
{
    public class ProjectileProperties_TouhouDanmaku : ProjectileProperties
    {
        // 定义我们的新属性：碰撞半径。
        // 默认值为0.5f，这基本上只覆盖中心格子，与原版行为类似。
        // 如果设置为1.0f，它会检测一个3x3的区域。
        public float collisionRadius = 0.5f;
    }
}