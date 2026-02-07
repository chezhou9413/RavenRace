using Verse;

namespace RavenRace
{
    public enum FusangResourceType
    {
        Resources,      // 基础物资
        Military,       // 军事力量
        Intel,          // 情报网
        Influence       // 影响力
    }

    /// <summary>
    /// 资源数据类
    /// </summary>
    public class FusangResource : IExposable
    {
        public FusangResourceType type;
        public int amount;
        public int max = 1000;

        public FusangResource() { }

        public FusangResource(FusangResourceType type, int startAmount = 0)
        {
            this.type = type;
            this.amount = startAmount;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref type, "type");
            Scribe_Values.Look(ref amount, "amount");
            Scribe_Values.Look(ref max, "max", 1000);
        }
    }
}