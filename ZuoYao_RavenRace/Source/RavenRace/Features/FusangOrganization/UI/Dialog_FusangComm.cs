using RavenRace.Features.FusangOrganization.UI;
using RavenRace.Features.Operator;
using RavenRace.Features.StoryEngine;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using System.Linq;

namespace RavenRace
{
    [StaticConstructorOnStartup]
    public class Dialog_FusangComm : FusangWindowBase
    {
        // --- 公开字段 (供 UI Panel 调用) ---
        public readonly Thing radio;
        public readonly Faction fusangFaction;
        public readonly WorldComponent_OperatorManager operatorManager;
        public readonly WorldComponent_Fusang fusangWorldComp;

        // 剧情处理器
        private StoryHandler storyHandler;

        // UI 状态
        private Texture2D currentPortrait;
        private string fullDialogueText;
        private string displayedText;
        private int typewriterCharIndex;
        private float typewriterCounter;
        private const float CharsPerSecond = 50f;

        // 静态资源
        private static Texture2D iconSupplies, iconMilitary, iconIntel, iconInfluence;

        protected override float Margin => 0f;
        public override Vector2 InitialSize => new Vector2(950f, 650f);

        public Dialog_FusangComm(Thing radioSource) : base()
        {
            this.radio = radioSource;
            this.closeOnClickedOutside = true;

            this.fusangFaction = Find.FactionManager.FirstFactionOfDef(FusangDefOf.Fusang_Hidden);
            this.operatorManager = Find.World.GetComponent<WorldComponent_OperatorManager>();
            this.fusangWorldComp = Find.World.GetComponent<WorldComponent_Fusang>();

            this.storyHandler = new StoryHandler();

            // 初始化资源
            if (iconSupplies == null) iconSupplies = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Supplies") ?? BaseContent.BadTex;
            if (iconMilitary == null) iconMilitary = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Military") ?? BaseContent.BadTex;
            if (iconIntel == null) iconIntel = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Intel") ?? BaseContent.BadTex;
            if (iconInfluence == null) iconInfluence = ContentFinder<Texture2D>.Get("UI/Fusang/Icon_Influence") ?? BaseContent.BadTex;

            InitializeDialogue();
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            UpdateTypewriter();
        }

        // --- 窗口激活时的回调 (用于从子窗口返回时刷新) ---
        public override void PostOpen()
        {
            base.PostOpen();
            RefreshPortrait(); // 确保每次打开或从子界面返回时刷新立绘
        }

        public override void DoWindowContents(Rect inRect)
        {
            FusangUIStyle.DrawBackground(inRect);

            // 1. 标题栏
            DrawHeader(inRect);

            // 2. 左侧面板 (立绘 + 状态)
            // [修复] 传入 this，让 Panel 内部可以访问当前窗口实例，方便刷新
            float leftWidth = 260f;
            Rect leftRect = new Rect(15, 60, leftWidth, inRect.height - 75);
            FusangComm_UIPanels.DrawLeftPanel(leftRect, radio, currentPortrait, operatorManager);

            // 3. 右侧区域
            float rightX = leftRect.xMax + 15;
            float rightWidth = inRect.width - rightX - 15;

            // 资源条
            Rect resourceRect = new Rect(rightX, 60, rightWidth, 50f);
            FusangComm_UIPanels.DrawResourceBar(resourceRect, iconSupplies, iconMilitary, iconIntel, iconInfluence);

            // 对话框 (文字区域)
            float chatBoxHeight = 234f;
            Rect chatRect = new Rect(rightX, resourceRect.yMax + 10, rightWidth, chatBoxHeight);
            DrawChatBox(chatRect);

            // 4. 底部交互区域
            float panelY = chatRect.yMax + 10;
            float panelHeight = inRect.height - panelY - 15;
            Rect bottomPanelRect = new Rect(rightX, panelY, rightWidth, panelHeight);

            // [核心逻辑] 判断显示 选项 还是 功能面板
            // 如果 Story 没激活，或者当前节点是“结束节点”(closeDialogue=true)，则显示功能面板
            bool showFunctionPanel = !storyHandler.IsActive ||
                                     (storyHandler.CurrentNode != null && storyHandler.CurrentNode.closeDialogue);

            if (showFunctionPanel)
            {
                FusangComm_UIPanels.DrawFunctionPanel(bottomPanelRect, this);
            }
            else
            {
                DrawChoices(bottomPanelRect);
            }

            // [移除] 关闭按钮已按要求移除
        }

        private void DrawHeader(Rect inRect)
        {
            var titleRect = new Rect(20, 15, inRect.width - 40, 30);
            Text.Font = GameFont.Medium;
            GUI.color = FusangUIStyle.MainColor_Gold;
            Widgets.Label(titleRect, "RavenRace_FusangRadioTitle".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            GUI.color = FusangUIStyle.BorderColor;
            Widgets.DrawLineHorizontal(15, 50, inRect.width - 30);
            GUI.color = Color.white;
        }

        private void InitializeDialogue()
        {
            // 赠礼回执处理
            if (WorldComponent_OperatorManager.PostGiftMessage != null)
            {
                fullDialogueText = WorldComponent_OperatorManager.PostGiftMessage;
                WorldComponent_OperatorManager.PostGiftMessage = null;
                storyHandler.EndStory(); // 确保不处于 Story 模式，直接显示文本
                RefreshPortrait();
                return;
            }

            // 尝试启动 Story
            if (storyHandler.TryStartNewDialogue())
            {
                UpdateCurrentNodeDisplay();
            }
            else
            {
                // 兜底文本
                fullDialogueText = "......（信号保持静默）";
                storyHandler.EndStory();
                RefreshPortrait();
            }
        }

        // [新增] 专门用于刷新立绘的方法
        public void RefreshPortrait()
        {
            // 如果当前有 Story 且指定了立绘，优先用 Story 的
            if (storyHandler.CurrentNode != null && !storyHandler.CurrentNode.portraitPath.NullOrEmpty())
            {
                currentPortrait = ContentFinder<Texture2D>.Get(storyHandler.CurrentNode.portraitPath, true);
            }
            else
            {
                // 否则根据 OperatorManager 的好感度自动获取
                // 这解决了赠礼后好感度变化立绘不更新的问题
                currentPortrait = operatorManager.GetOperatorPortrait();
            }
        }

        private void UpdateCurrentNodeDisplay()
        {
            var node = storyHandler.CurrentNode;
            if (node == null) return;

            fullDialogueText = node.text.Replace("\\n", "\n");
            typewriterCharIndex = 0;
            displayedText = "";
            typewriterCounter = 0f;

            RefreshPortrait(); // 每次节点切换都检查立绘
        }

        private void UpdateTypewriter()
        {
            if (string.Equals(displayedText, fullDialogueText)) return;
            typewriterCounter += Time.deltaTime;
            int targetIndex = (int)(typewriterCounter * CharsPerSecond);
            if (targetIndex > typewriterCharIndex)
            {
                typewriterCharIndex = Mathf.Min(targetIndex, fullDialogueText.Length);
                displayedText = fullDialogueText.Substring(0, typewriterCharIndex);
            }
        }

        private void SkipTypewriter()
        {
            if (typewriterCharIndex < fullDialogueText.Length)
            {
                typewriterCharIndex = fullDialogueText.Length;
                displayedText = fullDialogueText;
                SoundDefOf.DialogBoxAppear.PlayOneShotOnCamera(null);
            }
        }

        private void DrawChatBox(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.MainColor_Black);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);
            Rect textRect = rect.ContractedBy(15);
            GUI.color = FusangUIStyle.TextColor;
            Widgets.Label(textRect, displayedText);
            GUI.color = Color.white;

            if (Widgets.ButtonInvisible(rect)) SkipTypewriter();
        }

        private void DrawChoices(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, FusangUIStyle.PanelColor);
            FusangUIStyle.DrawBorder(rect, FusangUIStyle.BorderColor);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect.ContractedBy(10f));

            var node = storyHandler.CurrentNode;

            // 遍历选项
            foreach (var choice in node.options)
            {
                if (!choice.conditions.TrueForAll(c => c.IsMet())) continue;

                Rect btnRect = listing.GetRect(32f);
                if (FusangUIStyle.DrawButton(btnRect, choice.text))
                {
                    HandleChoice(choice);
                }
                if (!choice.tooltip.NullOrEmpty()) TooltipHandler.TipRegion(btnRect, choice.tooltip);
                listing.Gap(8f);
            }

            listing.End();
        }

        private void HandleChoice(StoryOption choice)
        {
            SoundDefOf.Click.PlayOneShotOnCamera();
            storyHandler.SelectOption(choice);

            // 如果还有新节点，更新显示
            // 如果剧情结束(closeDialogue=true)，DoWindowContents 下一帧会自动切换到 FunctionPanel
            if (storyHandler.IsActive)
            {
                UpdateCurrentNodeDisplay();
            }
        }
    }
}