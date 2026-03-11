using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RavenRace.Features.RavenRite.CustomRiteCore.Pojo
{
    //玩家确认后的完整选择结果。
    public class PromotionRitualSelection
    {
        //各角色分配结果,Key=RoleId,Value=分配到该角色的Pawn列表
        public Dictionary<string, List<Pawn>> RoleAssignments = new Dictionary<string, List<Pawn>>();
        //普通参与者
        public List<Pawn> Participants = new List<Pawn>();
        //所有参与仪式的人，去重
        public IEnumerable<Pawn> AllInvolved => RoleAssignments.Values.SelectMany(x => x).Concat(Participants).Distinct();
        //快捷获取某角色第一个Pawn
        public Pawn GetFirst(string roleId) => RoleAssignments.TryGetValue(roleId, out var list) ? list.FirstOrDefault() : null;
    }
}
