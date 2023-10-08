// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;

namespace Game
{
    class GameEvents
    {
        public static void Trigger(uint gameEventId, WorldObject source, WorldObject target)
        {
            Cypher.Assert(source != null || target != null, "At least one of [source] or [target] must be provided");

            WorldObject refForMapAndZoneScript = source ?? target;

            ZoneScript zoneScript = refForMapAndZoneScript.GetZoneScript();
            if (zoneScript == null && refForMapAndZoneScript.IsPlayer())
                zoneScript = refForMapAndZoneScript.FindZoneScript();

            if (zoneScript != null)
                zoneScript.ProcessEvent(target, gameEventId, source);

            Global.ScriptMgr.OnEventTrigger(target, source, gameEventId);

            GameObject goTarget = target?.ToGameObject();
            if (goTarget != null)
            {
                GameObjectAI goAI = goTarget.GetAI();
                if (goAI != null)
                    goAI.EventInform(gameEventId);
            }

            Player sourcePlayer = source?.ToPlayer();
            if (sourcePlayer != null)
                TriggerForPlayer(gameEventId, sourcePlayer);

            Map map = refForMapAndZoneScript.GetMap();
            TriggerForMap(gameEventId, map, source, target);
        }

        public static void TriggerForPlayer(uint gameEventId, Player source)
        {
            Map map = source.GetMap();
            if (map.Instanceable())
            {
                source.StartCriteriaTimer(CriteriaStartEvent.SendEvent, gameEventId);
                source.ResetCriteria(CriteriaFailEvent.SendEvent, gameEventId);
            }

            source.UpdateCriteria(CriteriaType.PlayerTriggerGameEvent, gameEventId, 0, 0, source);

            if (map.IsScenario())
                source.UpdateCriteria(CriteriaType.AnyoneTriggerGameEventScenario, gameEventId, 0, 0, source);
        }

        public static void TriggerForMap(uint gameEventId, Map map, WorldObject source = null, WorldObject target = null)
        {
            map.ScriptsStart(ScriptsType.Event, gameEventId, source, target);
        }
    }
}
