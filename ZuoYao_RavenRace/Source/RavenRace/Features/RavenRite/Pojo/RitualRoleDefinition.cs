using RavenRace.Features.RavenRite.RaveFilter;
using System;
using UnityEngine;
using Verse;

namespace RavenRace.Features.RavenRite.Pojo
{
    public class RitualRoleDefinition
    {
        public string RoleId;
        public string Label;
        public int MaxCount = 1;
        public bool Required = true;
        public Color? SlotColor = null;
        public Type filterClass = null;
        private RitualRoleFilter filterInt = null;
        public RitualRoleFilter Filter
        {
            get
            {
                if (filterInt == null && filterClass != null)
                {
                    filterInt = (RitualRoleFilter)Activator.CreateInstance(filterClass);
                }
                return filterInt;
            }
            set => filterInt = value;
        }

        public RitualRoleDefinition() { }

        public RitualRoleDefinition(string roleId,string label,int maxCount = 1,bool required = true,RitualRoleFilter filter = null)
        {
            RoleId = roleId;
            Label = label;
            MaxCount = maxCount;
            Required = required;
            filterInt = filter;
        }

        public bool CanAssignPawn(Pawn pawn)=>Filter == null || Filter.CanAssign(pawn);

        public string GetDisabledReason(Pawn pawn)=>Filter?.GetDisabledReason(pawn) ?? string.Empty;
    }
}