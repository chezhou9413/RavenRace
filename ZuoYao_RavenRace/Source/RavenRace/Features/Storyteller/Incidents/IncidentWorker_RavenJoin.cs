using System.Linq;
using Verse;
using RimWorld;

namespace RavenRace.Features.Storyteller.Incidents
{
    /// <summary>
    /// 渡鸦加入事件 Worker。
    /// 逻辑参考了 IncidentWorker_WandererJoin，但改为发送 ChoiceLetter 而不是直接 Spawn。
    /// </summary>
    public class IncidentWorker_RavenJoin : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            Map map = (Map)parms.target;
            return map != null && map.IsPlayerHome;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // 1. 确定 PawnKind (渡鸦殖民者)
            PawnKindDef kind = PawnKindDef.Named("Raven_Colonist");

            // 2. 生成 Pawn (仅在内存中，暂不 Spawn)
            // 严格参考原版 PawnGenerationRequest 构造
            PawnGenerationRequest request = new PawnGenerationRequest(
                kind: kind,
                faction: Faction.OfPlayer, // 预设为玩家派系，但在 Spawn 前它是隐藏的
                context: PawnGenerationContext.NonPlayer,
                tile: -1,
                forceGenerateNewPawn: true,
                fixedBiologicalAge: 20f, // 年轻力壮
                forceAddFreeWarmLayerIfNeeded: true
            );

            Pawn pawn = PawnGenerator.GeneratePawn(request);
            if (pawn == null) return false;

            // 3. 构建自定义信件
            // [修复] 确保使用 def.letterDef (它现在指向了 Raven_Letter_AcceptJoiner)
            // LetterMaker 会根据 letterDef.letterClass 反射创建 ChoiceLetter_RavenJoin 实例
            Letter let = LetterMaker.MakeLetter(def.letterLabel, def.letterText, def.letterDef, null, null);

            // 安全检查与转换
            ChoiceLetter_RavenJoin letter = let as ChoiceLetter_RavenJoin;
            if (letter == null)
            {
                Log.Error($"[RavenRace] Incident {def.defName} created a letter of type {let?.GetType().Name}, but expected ChoiceLetter_RavenJoin. Check XML letterDef configuration.");
                return false;
            }

            // 使用 TaggedString 赋值给 Text (注意大写 T)
            letter.Label = "RavenRace_LetterLabel_FirstRavenJoin".Translate();
            letter.Text = "RavenRace_LetterText_FirstRavenJoin".Translate(pawn.Name.ToStringFull, pawn.story.Title, pawn.ageTracker.AgeBiologicalYears);

            letter.joiner = pawn;
            letter.map = map;
            letter.lookTargets = new LookTargets(map.Parent); // 指向地图

            // 4. 发送信件
            Find.LetterStack.ReceiveLetter(letter);

            return true;
        }
    }
}