using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using rjw; // 确保引用RJW
using rjw.Modules.Interactions;

namespace RavenRace.RJWCompat.UI
{
    /// <summary>
    /// 一个自定义的对话框窗口，用于选择RJW的性爱互动。
    /// 它取代了原有的FloatMenu，提供了分类和更好的用户体验，并解决了时序问题。
    /// </summary>
    public class Dialog_SelectRjwInteraction : Window
    {
        // 窗口尺寸
        private Vector2 scrollPosition = Vector2.zero;
        public override Vector2 InitialSize => new Vector2(480f, 640f);

        // 参与者
        private readonly Pawn caster;
        private readonly Pawn target;

        // 分类后的互动列表
        private readonly Dictionary<string, List<InteractionDef>> categorizedInteractions;

        // 构造函数
        public Dialog_SelectRjwInteraction(Pawn caster, Pawn target)
        {
            this.caster = caster;
            this.target = target;

            // 窗口属性
            forcePause = true;        // 强制暂停游戏，解决时序问题
            absorbInputAroundWindow = true; // 吸收窗口外的点击
            closeOnClickedOutside = true;

            // 获取并分类所有可用的互动
            categorizedInteractions = GetAndCategorizeInteractions();
        }

        /// <summary>
        /// 绘制窗口内容的核心方法
        /// </summary>
        public override void DoWindowContents(Rect inRect)
        {
            // 标题
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 40f), $"选择对 {target.LabelShortCap} 的互动方式");
            Text.Font = GameFont.Small;

            // 滚动视图区域
            Rect scrollRect = new Rect(inRect.x, inRect.y + 45f, inRect.width, inRect.height - 45f);
            float viewHeight = CalculateViewHeight(); // 动态计算内容高度
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            // 绘制每个分类
            if (categorizedInteractions.NullOrEmpty())
            {
                listing.Label("没有找到可用的RJW互动。");
            }
            else
            {
                foreach (var category in categorizedInteractions.OrderBy(kvp => kvp.Key))
                {
                    listing.GapLine(12f);
                    listing.Label($"-- {category.Key} --"); // 分类标题
                    listing.Gap(4f);

                    foreach (var interaction in category.Value)
                    {
                        if (listing.ButtonText(interaction.LabelCap))
                        {
                            // 玩家点击按钮后，执行操作并关闭窗口
                            StartRjwSexJob(caster, target, interaction);
                            Close();
                        }
                    }
                }
            }

            listing.End();
            Widgets.EndScrollView();
        }

        /// <summary>
        /// 获取所有可用的RJW互动，并进行分类。
        /// </summary>
        private Dictionary<string, List<InteractionDef>> GetAndCategorizeInteractions()
        {
            var allRjwInteractions = DefDatabase<InteractionDef>.AllDefs
                .Where(def => def.HasModExtension<SexInteractionExtension>())
                .ToList();

            var validInteractions = new List<InteractionDef>();

            // 将 isRape 设为 true 来获取最广泛的互动列表，因为某些互动只在特定条件下可用。
            // 最终我们使用的是非强奸的 Job，所以这只是为了获取选项。
            SexProps props = new SexProps { pawn = caster, partner = target, isRape = true };
            var cache = new SexInteractionFinder.Internal.FinderCache(props);

            foreach (var interactionDef in allRjwInteractions)
            {
                var extension = interactionDef.GetModExtension<SexInteractionExtension>();
                if (extension == null) continue;

                bool initiatorSatisfied = SexInteractionFinder.Internal.TrySatisfyRequirement(caster, cache.initiatorParts, extension.initiatorRequirement, props, out _);
                bool recipientSatisfied = SexInteractionFinder.Internal.TrySatisfyRequirement(target, cache.recipientParts, extension.recipientRequirement, props, out _);

                if (initiatorSatisfied && recipientSatisfied)
                {
                    validInteractions.Add(interactionDef);
                }
            }

            var result = new Dictionary<string, List<InteractionDef>>();
            foreach (var interaction in validInteractions.OrderBy(i => i.label))
            {
                string category = GetCategoryFor(interaction);

                if (!result.ContainsKey(category))
                {
                    result[category] = new List<InteractionDef>();
                }
                result[category].Add(interaction);
            }
            return result;
        }

        /// <summary>
        /// 根据互动定义获取其分类。
        /// </summary>
        private string GetCategoryFor(InteractionDef def)
        {
            var sexInteraction = new SexInteraction(def);
            switch (sexInteraction.Sextype)
            {
                case xxx.rjwSextype.Vaginal: return "常规性交";
                case xxx.rjwSextype.Anal: return "逆向性交";
                case xxx.rjwSextype.Oral:
                case xxx.rjwSextype.Fellatio:
                case xxx.rjwSextype.Cunnilingus:
                case xxx.rjwSextype.Rimming:
                case xxx.rjwSextype.Sixtynine: return "口部性交";
                case xxx.rjwSextype.Boobjob: return "乳交";
                case xxx.rjwSextype.Handjob:
                case xxx.rjwSextype.Footjob:
                case xxx.rjwSextype.Fingering: return "手/足/指交";
                case xxx.rjwSextype.DoublePenetration: return "双重渗透";
                case xxx.rjwSextype.Fisting: return "拳交";
                default: return "其他互动";
            }
        }

        /// <summary>
        /// 计算滚动视图内容的总高度
        /// </summary>
        private float CalculateViewHeight()
        {
            float height = 0f;
            if (categorizedInteractions.NullOrEmpty()) return 30f;

            const float buttonHeight = 30f;
            const float categoryHeaderHeight = 24f;
            const float gapHeight = 16f;

            foreach (var category in categorizedInteractions)
            {
                height += categoryHeaderHeight + gapHeight + (category.Value.Count * buttonHeight);
            }
            return height + 12f;
        }

        /// <summary>
        /// 创建并分配一个在机制上是“自愿”的性爱Job，以避免关系惩罚。
        /// </summary>
        private void StartRjwSexJob(Pawn pawn, Pawn partner, InteractionDef selectedInteraction)
        {
            // [最终解决方案] 使用 "Quickie" 这个自愿的、不需要床的 JobDef。
            var jobDef = DefDatabase<JobDef>.GetNamed("Quickie");
            if (jobDef == null)
            {
                Log.Error("[RavenRace RJWCompat] Cannot find JobDef named 'Quickie'. This is a standard RJW JobDef.");
                return;
            }

            // ========================================================================
            // 【核心修正】 RJW 的 Quickie 工作逻辑会检查 FailOn(() => pawn.Drafted)。
            // 因此，如果玩家处于征召状态（通常使用技能时都是），必须先取消征召。
            // ========================================================================
            if (pawn.Drafted)
            {
                pawn.drafter.Drafted = false;
            }

            // 同时为了防止目标乱跑或还在战斗，也强制停止目标的工作
            // 如果目标被征召，也尝试取消征召（如果是玩家单位）
            if (partner.IsColonistPlayerControlled && partner.Drafted)
            {
                partner.drafter.Drafted = false;
            }
            partner.jobs.StopAll(); // 强行打断，让目标准备好接受互动

            // 创建Job实例。Quickie的目标是伴侣。
            Job job = JobMaker.MakeJob(jobDef, partner);

            // [关键] 通过job.interaction字段，将玩家选择的互动方式安全地传递给RJW的JobDriver。
            // JobDriver_SexQuick会读取这个字段，并自己生成一个 isRape = false 的 SexProps。
            job.interaction = selectedInteraction;

            Log.Message($"[RavenRace RJWCompat] Player selected '{selectedInteraction.defName}'. Force-undrafted and assigning RJW's 'Quickie' job to {pawn.LabelShort}.");

            // 分配Job
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }
}