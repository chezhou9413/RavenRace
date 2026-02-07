using System.Linq;
using Verse;
using RimWorld;

namespace RavenRace.Features.Operator.Gifting
{
    [StaticConstructorOnStartup]
    public static class GiftingManager
    {
        private static readonly GiftReactionDef defaultReaction;

        static GiftingManager()
        {
            defaultReaction = DefDatabase<GiftReactionDef>.GetNamed("Raven_GiftReaction_Default");
        }

        public static void HandleGift(Thing gift, int count, out int favorChange)
        {
            favorChange = 0;
            if (gift == null || count <= 0) return;

            // [修复] 暂时只使用默认反应，直到您提供ThingCategories.xml
            var reaction = defaultReaction;
            float totalValue = gift.MarketValue * count;

            favorChange = (int)(totalValue * reaction.favorChangePerValue);
            favorChange = reaction.favorChangePerValue > 0 ?
                          System.Math.Min(favorChange, reaction.maxFavorChange) :
                          System.Math.Max(favorChange, reaction.maxFavorChange);

            WorldComponent_OperatorManager.ChangeFavorability(favorChange);

            // [核心修复] 不再直接显示消息，而是设置静态变量
            string message = reaction.specialMessage;
            if (string.IsNullOrEmpty(message))
            {
                message = favorChange >= 5 ? "“嗯，谢了。”" :
                          favorChange > 0 ? "“……收到了。”" :
                          "“……？”";
            }
            WorldComponent_OperatorManager.PostGiftMessage = message;
        }
    }
}