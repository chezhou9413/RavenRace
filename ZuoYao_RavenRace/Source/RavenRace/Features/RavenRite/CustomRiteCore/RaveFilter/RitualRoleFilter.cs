using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RavenRace.Features.RavenRite.CustomRiteCore.RaveFilter
{
    //角色候选人过滤条件基类，子类重写两个虚方法实现自定义限制。
    public abstract class RitualRoleFilter
    {
        //返回true表示该Pawn满足条件，可以被分配到此角色
        public abstract bool CanAssign(Pawn pawn);

        //返回禁用原因；仅在CanAssign返回false时才会被调用
        public abstract string GetDisabledReason(Pawn pawn);
    }
}
