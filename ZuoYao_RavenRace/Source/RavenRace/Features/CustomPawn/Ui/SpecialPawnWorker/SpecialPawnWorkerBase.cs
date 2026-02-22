using RavenRace.Features.CustomPawn.Ui.RaveExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RavenRace.Features.CustomPawn.Ui.SpecialPawnWorker
{
    public class SpecialPawnWorkerBase
    {
        public PawnKindDef def;
        public RaveCustomPawnUiData ext;
        //是否满足解锁条件，重写此方法以实现自定义解锁逻辑。默认返回true，表示解锁。
        public virtual bool UnlockCondition() => true;

        //解锁瞬间触发一次，可做通知、奖励、生成等逻辑。
        public virtual void OnUnlocked() { }

     
        //仍处于未解锁状态时持续调用，可做进度追踪、提示等逻辑。
        public virtual void OnLocked() { }

        //UI召唤按钮点击时的回调函数，可以重写加你自己的逻辑
        public virtual void UiSummon(Pawn pawn) { }

        //UI收回按钮点击时的回调函数，可以重写加你自己的逻辑
        public virtual void UiReclaim(Pawn pawn) { }
    }
}
