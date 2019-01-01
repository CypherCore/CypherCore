/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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

        public override bool OnTrigger(Player player, AreaTriggerRecord trigger, bool entered)
        {
            if (!player.IsAlive())
                return false;

            Log.outDebug(LogFilter.ScriptsAi, "AreaTrigger {0} is using SmartTrigger script", trigger.Id);
            SmartScript script = new SmartScript();
            script.OnInitialize(trigger);
            script.ProcessEventsFor(SmartEvents.AreatriggerOntrigger, player, trigger.Id);
            return true;
        }
    }

    [Script]
    class SmartScene : SceneScript
    {
        public SmartScene() : base("SmartScene") { }

        public override void OnSceneStart(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            SmartScript smartScript = new SmartScript();
            smartScript.OnInitialize(sceneTemplate);
            smartScript.ProcessEventsFor(SmartEvents.SceneStart, player);
        }

        public override void OnSceneTriggerEvent(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate, string triggerName)
        {
            SmartScript smartScript = new SmartScript();
            smartScript.OnInitialize(sceneTemplate);
            smartScript.ProcessEventsFor(SmartEvents.SceneTrigger, player, 0, 0, false, null, null, triggerName);
        }

        public override void OnSceneCancel(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            SmartScript smartScript = new SmartScript();
            smartScript.OnInitialize(sceneTemplate);
            smartScript.ProcessEventsFor(SmartEvents.SceneCancel, player);
        }

        public override void OnSceneComplete(Player player, uint sceneInstanceID, SceneTemplate sceneTemplate)
        {
            SmartScript smartScript = new SmartScript();
            smartScript.OnInitialize(sceneTemplate);
            smartScript.ProcessEventsFor(SmartEvents.SceneComplete, player);
        }
    }
}
