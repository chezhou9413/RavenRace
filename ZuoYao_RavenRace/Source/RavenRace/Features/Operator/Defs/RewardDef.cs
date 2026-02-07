using Verse;

namespace RavenRace.Features.Operator.Rewards
{
    public class RewardDef : Def
    {
        public int requiredFavorability; // 达到该好感度可领取
        public ThingDef rewardThing;       // 奖励的物品Def
    }
}