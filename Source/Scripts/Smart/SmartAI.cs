/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.DataStorage;

namespace Scripts.Smart
{
    [Script]
    class SmartTrigger : AreaTriggerScript
    {
        public SmartTrigger() : base("SmartTrigger") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger)
        {
            if (!player.IsAlive())
                return false;

            Log.outDebug(LogFilter.ScriptsAi, "AreaTrigger {0} is using SmartTrigger script", trigger.Id);
            SmartScript script = new();
            script.OnInitialize(player, trigger);
            script.ProcessEventsFor(SmartEvents.AreatriggerOntrigger, player, trigger.Id);
            return true;
        }
    }

    [Script]
    class SmartAreaTriggerEntityScript : AreaTriggerEntityScript
    {
        public SmartAreaTriggerEntityScript() : base("SmartAreaTriggerAI") { }

        public override AreaTriggerAI GetAI(AreaTrigger areaTrigger)
        {
            return new SmartAreaTriggerAI(areaTrigger);
        }
    }

    [Script]
    class SmartScene : SceneScript
    {
        public SmartScene() : base("SmartScene") { }

        public override void OnSceneStart(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            SmartScript smartScript = new();
            smartScript.OnInitialize(null, null, sceneTemplate);
            smartScript.ProcessEventsFor(SmartEvents.SceneStart, player);
        }

        public override void OnSceneTriggerEvent(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate, string triggerName)
        {
            SmartScript smartScript = new();
            smartScript.OnInitialize(null, null, sceneTemplate);
            smartScript.ProcessEventsFor(SmartEvents.SceneTrigger, player, 0, 0, false, null, null, triggerName);
        }

        public override void OnSceneCancel(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            SmartScript smartScript = new();
            smartScript.OnInitialize(null, null, sceneTemplate);
            smartScript.ProcessEventsFor(SmartEvents.SceneCancel, player);
        }

        public override void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            SmartScript smartScript = new();
            smartScript.OnInitialize(null, null, sceneTemplate);
            smartScript.ProcessEventsFor(SmartEvents.SceneComplete, player);
        }
    }

    [Script]
    class SmartQuest : QuestScript
    {
        public SmartQuest() : base("SmartQuest") { }

        // Called when a quest status change
        public override void OnQuestStatusChange(Player player, Quest quest, QuestStatus oldStatus, QuestStatus newStatus)
        {
            SmartScript smartScript = new();
            smartScript.OnInitialize(null, null, null, quest);
            switch (newStatus)
            {
                case QuestStatus.Incomplete:
                    smartScript.ProcessEventsFor(SmartEvents.QuestAccepted, player);
                    break;
                case QuestStatus.Complete:
                    smartScript.ProcessEventsFor(SmartEvents.QuestCompletion, player);
                    break;
                case QuestStatus.Failed:
                    smartScript.ProcessEventsFor(SmartEvents.QuestFail, player);
                    break;
                case QuestStatus.Rewarded:
                    smartScript.ProcessEventsFor(SmartEvents.QuestRewarded, player);
                    break;
                case QuestStatus.None:
                default:
                    break;
            }
        }

        // Called when a quest objective data change
        public override void OnQuestObjectiveChange(Player player, Quest quest, QuestObjective objective, int oldAmount, int newAmount)
        {
            ushort slot = player.FindQuestSlot(quest.Id);
            if (slot < SharedConst.MaxQuestLogSize && player.IsQuestObjectiveComplete(slot, quest, objective))
            {
                SmartScript smartScript = new();
                smartScript.OnInitialize(null, null, null, quest);
                smartScript.ProcessEventsFor(SmartEvents.QuestObjCompletion, player, objective.Id);
            }
        }
    }
}
