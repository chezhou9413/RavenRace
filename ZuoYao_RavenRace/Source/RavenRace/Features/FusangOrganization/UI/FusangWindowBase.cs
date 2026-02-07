using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace RavenRace.Features.FusangOrganization.UI
{
    /// <summary>
    /// 扶桑电台相关窗口的基类。
    /// 统一处理：不暂停游戏、拦截误触地图、启用时间控制快捷键。
    /// </summary>
    public abstract class FusangWindowBase : Window
    {
        protected FusangWindowBase()
        {
            this.forcePause = false; // 全局不暂停，允许游戏背景运行
            this.absorbInputAroundWindow = true; // 拦截鼠标点击，防止误触地图上的东西
            this.doCloseX = false; // 默认不使用原版右上角X，由子类UI自己画
            this.doCloseButton = false; // 不显示底部的Close按钮
            this.preventCameraMotion = false; // 允许移动相机（可选）
        }
        public override void WindowUpdate()
        {
            base.WindowUpdate();          
            // 确保窗口当前能够接收输入
            if (Find.WindowStack.GetsInput(this))
            {
                HandleTimeControls();
            }
        }

        private void HandleTimeControls()
        {
            if (KeyBindingDefOf.TogglePause.JustPressed)
            {
                Find.TickManager.TogglePaused();
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Pause, KnowledgeAmount.SpecificInteraction);
                return;
            }

            // 2. 正常速度 (1)
            if (KeyBindingDefOf.TimeSpeed_Normal.JustPressed)
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                SoundDefOf.Clock_Normal.PlayOneShotOnCamera();
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.TimeControls, KnowledgeAmount.SpecificInteraction);
                return;
            }

            // 3. 快进 (2)
            if (KeyBindingDefOf.TimeSpeed_Fast.JustPressed)
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Fast;
                SoundDefOf.Clock_Fast.PlayOneShotOnCamera();
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.TimeControls, KnowledgeAmount.SpecificInteraction);
                return;
            }

            // 4. 超快 (3)
            if (KeyBindingDefOf.TimeSpeed_Superfast.JustPressed)
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
                SoundDefOf.Clock_Superfast.PlayOneShotOnCamera();
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.TimeControls, KnowledgeAmount.SpecificInteraction);
                return;
            }

            // 5. 极快 (4)
            if (KeyBindingDefOf.TimeSpeed_Ultrafast.JustPressed)
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
                SoundDefOf.Clock_Superfast.PlayOneShotOnCamera();
                return;
            }
        }
    }
}