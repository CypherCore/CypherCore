// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;

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
            smartScript.OnInitialize(player, null, sceneTemplate);
            smartScript.ProcessEventsFor(SmartEvents.SceneStart, player);
        }

        public override void OnSceneTriggerEvent(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate, string triggerName)
        {
            SmartScript smartScript = new();
            smartScript.OnInitialize(player, null, sceneTemplate);
            smartScript.ProcessEventsFor(SmartEvents.SceneTrigger, player, 0, 0, false, null, null, triggerName);
        }

        public override void OnSceneCancel(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            SmartScript smartScript = new();
            smartScript.OnInitialize(player, null, sceneTemplate);
            smartScript.ProcessEventsFor(SmartEvents.SceneCancel, player);
        }

        public override void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            SmartScript smartScript = new();
            smartScript.OnInitialize(player, null, sceneTemplate);
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
            smartScript.OnInitialize(player, null, null, quest);
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
                smartScript.OnInitialize(player, null, null, quest);
                smartScript.ProcessEventsFor(SmartEvents.QuestObjCompletion, player, objective.Id);
            }
        }
    }

    [Script]
    class SmartEventTrigger : EventScript
    {
        public SmartEventTrigger() : base("SmartEventTrigger") { }

        public override void OnTrigger(WorldObject obj, WorldObject invoker, uint eventId)
        {
            Log.outDebug(LogFilter.ScriptsAi, $"Event {eventId} is using SmartEventTrigger script");
            SmartScript script = new();
            // Set invoker as BaseObject if there isn't target for GameEvents::Trigger
            script.OnInitialize(obj ?? invoker, null, null, null, eventId);
            script.ProcessEventsFor(SmartEvents.SendEventTrigger, invoker.ToUnit(), 0, 0, false, null, invoker.ToGameObject());
        }
    }
}
