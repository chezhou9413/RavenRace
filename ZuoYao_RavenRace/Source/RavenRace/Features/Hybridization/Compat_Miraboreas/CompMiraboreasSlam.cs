using Verse;

namespace RavenRace.Compat.Miraboreas
{
    /// <summary>
    /// 用于在Hediff的XML中存储拍击效果参数的CompProperties。
    /// </summary>
    public class CompProperties_MiraboreasSlam : HediffCompProperties
    {
        public float slamRadius = 1.9f;
        public float slamDamageFactor = 0.65f;

        public CompProperties_MiraboreasSlam()
        {
            this.compClass = typeof(CompMiraboreasSlam);
        }
    }

    /// <summary>
    /// 一个简单的数据容器组件，附加在Hediff上，让Harmony补丁可以读取其XML中定义的属性。
    /// </summary>
    public class CompMiraboreasSlam : HediffComp
    {
        public CompProperties_MiraboreasSlam Props => (CompProperties_MiraboreasSlam)this.props;
    }
}