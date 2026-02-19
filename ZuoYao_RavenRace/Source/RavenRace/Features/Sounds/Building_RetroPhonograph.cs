using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RavenRace.Features.Sounds
{
    /// <summary>
    /// 渡鸦复古唱片机，在开发者模式下提供音效测试按钮。
    /// </summary>
    public class Building_RetroPhonograph : Building
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            // 首先返回基类的Gizmos（如果有的话）
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            // 只在开发者模式下显示测试按钮
            if (Prefs.DevMode)
            {
                // 创建一个调试命令组
                var commandGroup = new Command_Action
                {
                    defaultLabel = "测试整蛊音效",
                    defaultDesc = "播放各种内置的彩蛋音效以进行测试。",
                    icon = TexCommand.DesirePower, // 使用一个通用的开发者图标
                    action = () =>
                    {
                        var options = new List<FloatMenuOption>
                        {
                            new FloatMenuOption("受击", () => PlaySound(RavenSoundDefOf.RavenMeme_TakeDamage)),
                            new FloatMenuOption("大统领寻宝", () => PlaySound(RavenSoundDefOf.RavenMeme_ArchonTreasure)),
                            new FloatMenuOption("Binah技能", () => PlaySound(RavenSoundDefOf.RavenMeme_BinahAbility)),
                            new FloatMenuOption("倒地", () => PlaySound(RavenSoundDefOf.RavenMeme_PawnDowned)),
                            new FloatMenuOption("看AV", () => PlaySound(RavenSoundDefOf.RavenMeme_WatchAV)),
                            new FloatMenuOption("社交失败", () => PlaySound(RavenSoundDefOf.RavenMeme_SocialFail)),
                            new FloatMenuOption("制作/建造失败", () => PlaySound(RavenSoundDefOf.RavenMeme_CraftFail)),
                            new FloatMenuOption("被侮辱", () => PlaySound(RavenSoundDefOf.RavenMeme_Insulted)),
                            new FloatMenuOption("死亡", () => PlaySound(RavenSoundDefOf.RavenMeme_PawnDeath)),
                            new FloatMenuOption("逃跑", () => PlaySound(RavenSoundDefOf.RavenMeme_Fleeing))
                        };
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };
                yield return commandGroup;
            }
        }

        /// <summary>
        /// 【修复】播放一个在地图内、有空间位置的音效。
        /// </summary>
        /// <param name="soundDef">要播放的SoundDef。</param>
        private void PlaySound(SoundDef soundDef)
        {
            if (soundDef != null)
            {
                // 【修复】使用 PlayOneShot 并提供 SoundInfo.InMap，从建筑自身发出声音。
                soundDef.PlayOneShot(SoundInfo.InMap(new TargetInfo(this)));
                Messages.Message($"正在播放: {soundDef.defName}", MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                Messages.Message("错误：音效定义未找到！", MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}