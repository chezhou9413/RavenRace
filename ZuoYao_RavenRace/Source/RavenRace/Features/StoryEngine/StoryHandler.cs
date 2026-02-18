using System.Linq;
using Verse;
using System.Collections.Generic;
using RavenRace.Features.Operator;

namespace RavenRace.Features.StoryEngine
{
    /// <summary>
    /// 剧情逻辑处理器
    /// </summary>
    public class StoryHandler
    {
        public StoryDef CurrentStory { get; private set; }
        public StoryNode CurrentNode { get; private set; }

        // 检查当前是否有活跃的剧情节点
        public bool IsActive => CurrentStory != null && CurrentNode != null;

        /// <summary>
        /// 尝试启动一段新的对话。
        /// </summary>
        public bool TryStartNewDialogue()
        {
            // 1. 查找所有条件满足的剧情
            var validStories = DefDatabase<StoryDef>.AllDefsListForReading
                .Where(def => def.conditions.TrueForAll(c => c.IsMet()))
                .OrderByDescending(def => def.priority) // 优先级高的先触发
                .ToList();

            if (validStories.NullOrEmpty())
            {
                EndStory();
                return false;
            }

            // 2. 选取优先级最高的
            StartStory(validStories.First());
            return true;
        }

        public void StartStory(StoryDef story)
        {
            CurrentStory = story;

            if (story.randomStart && !story.nodes.NullOrEmpty())
            {
                JumpToNode(story.nodes.RandomElement().id);
            }
            else
            {
                JumpToNode(story.initialNodeID);
            }
        }

        /// <summary>
        /// 跳转到指定节点并执行进入动作
        /// </summary>
        public void JumpToNode(string nodeID)
        {
            if (CurrentStory == null) return;

            var node = CurrentStory.nodes.FirstOrDefault(n => n.id == nodeID);
            if (node != null)
            {
                CurrentNode = node;
                ExecuteActions(node.onEnterActions);

                // [核心修改] 如果进入的是结束节点，立即标记剧情完成(Flag)，
                // 但不置空 CurrentNode，以便 UI 继续显示最后一句话。
                if (node.closeDialogue)
                {
                    MarkStoryComplete();
                }
            }
            else
            {
                Log.Error($"[RavenRace] StoryEngine: Missing node '{nodeID}' in story '{CurrentStory.defName}'");
                EndStory();
            }
        }

        public void SelectOption(StoryOption option)
        {
            if (option == null) return;

            // 1. 执行动作
            ExecuteActions(option.actions);

            // 2. 逻辑流转
            if (option.closeDialogue)
            {
                // 如果选项直接导致关闭，标记完成，并保留当前节点显示（或者视需求跳转到空）
                // 在新逻辑下，通常建议跳转到一个 closeDialogue=true 的 Response 节点
                MarkStoryComplete();
                // 此时 CurrentNode 保持不变，UI 层会检测到 closeDialogue=true 并切换面板
            }
            else if (!string.IsNullOrEmpty(option.nextNodeID))
            {
                JumpToNode(option.nextNodeID);
            }
            else
            {
                // 没有下个节点，视为结束
                MarkStoryComplete();
            }
        }

        private void ExecuteActions(List<StoryAction> actions)
        {
            if (actions.NullOrEmpty()) return;
            foreach (var act in actions) act.Execute();
        }

        // 标记当前剧情已完成（写入 Flag）
        private void MarkStoryComplete()
        {
            if (CurrentStory != null && !string.IsNullOrEmpty(CurrentStory.completeFlag))
            {
                StoryWorldComponent.SetFlag(CurrentStory.completeFlag, true);
            }
            // 注意：这里我们不再调用 EndStory() 清空 CurrentNode，
            // 而是让 CurrentNode 停留在最后一句话上，交给 UI 去判断是否显示功能面板。
        }

        // 强制中止/完全重置
        public void EndStory()
        {
            CurrentStory = null;
            CurrentNode = null;
        }
    }
}