using RavenRace.Features.CustomPawn.Ui.SpecialPawnWorker;
using System;
using Verse;

namespace RavenRace.Features.CustomPawn.Ui.RaveExtension
{
    public class RaveCustomPawnUiData : DefModExtension
    {
        //运行类，必须继承自 SpecialPawnWorkerBase
        public Type workerClass;
        //角色描述文本
        public String CustomPawnUiDes;
        //主UI排序
        public float pos;
        public SpecialPawnWorkerBase Worker { get; private set; }

        public override void ResolveReferences(Def parentDef)
        {
            base.ResolveReferences(parentDef);

            if (workerClass != null)
            {
                Worker = (SpecialPawnWorkerBase)Activator.CreateInstance(workerClass);
                Worker.def = (PawnKindDef)parentDef;
                Worker.ext = this;
            }
        }
    }

}
