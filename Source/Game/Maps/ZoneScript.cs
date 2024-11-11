// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;

namespace Game.Maps
{
    public class ZoneScript
    {
        protected EventMap _events = new();
        protected TaskScheduler _scheduler = new();

        public virtual void TriggerGameEvent(uint gameEventId, WorldObject source = null, WorldObject target = null)
        {
            if (source != null)
                GameEvents.Trigger(gameEventId, source, target);
            else
                ProcessEvent(null, gameEventId, null);
        }

        public virtual uint GetCreatureEntry(ulong guidlow, CreatureData data) { return data.Id; }
        public virtual uint GetGameObjectEntry(ulong spawnId, uint entry) { return entry; }

        public virtual void OnCreatureCreate(Creature creature) { }
        public virtual void OnCreatureRemove(Creature creature) { }

        public virtual void OnGameObjectCreate(GameObject go) { }
        public virtual void OnGameObjectRemove(GameObject go) { }

        public virtual void OnAreaTriggerCreate(AreaTrigger areaTrigger) { }
        public virtual void OnAreaTriggerRemove(AreaTrigger areaTrigger) { }

        public virtual void OnUnitDeath(Unit unit) { }

        // Triggers when the CreatureGroup no longer has any alive members (either last alive member dies or is removed from the group)
        public virtual void OnCreatureGroupDepleted(CreatureGroup creatureGroup) { }

        //All-purpose data storage 64 bit
        public virtual ObjectGuid GetGuidData(uint DataId) { return ObjectGuid.Empty; }
        public virtual void SetGuidData(uint DataId, ObjectGuid Value) { }

        public virtual ulong GetData64(uint dataId) { return 0; }
        public virtual void SetData64(uint dataId, ulong value) { }

        //All-purpose data storage 32 bit
        public virtual uint GetData(uint dataId) { return 0; }
        public virtual void SetData(uint dataId, uint value) { }

        public virtual void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker) { }
        public virtual void DoAction(uint actionId, WorldObject source = null, WorldObject target = null) { }
        public virtual void OnFlagStateChange(GameObject flagInBase, FlagState oldValue, FlagState newValue, Player player) { }

        public virtual bool CanCaptureFlag(AreaTrigger areaTrigger, Player player) { return false; }
        public virtual void OnCaptureFlag(AreaTrigger areaTrigger, Player player) { }

        // This hook is used with GAMEOBJECT_TYPE_FLAGSTAND. Newer gameobjects use GAMEOBJECT_TYPE_NEW_FLAG and should use `OnFlagStateChange`
        public virtual void OnFlagTaken(GameObject flag, Player player) { }
        // This hook is used with GAMEOBJECT_TYPE_FLAGSTAND. Newer gameobjects use GAMEOBJECT_TYPE_NEW_FLAG and should use `OnFlagStateChange`. The GameObject doesn't exist anymore, but the ObjectGuid does
        public virtual void OnFlagDropped(ObjectGuid flagGuid, Player player) { }
    }

    public class ControlZoneHandler
    {
        public virtual void HandleCaptureEventHorde(GameObject controlZone) { }
        public virtual void HandleCaptureEventAlliance(GameObject controlZone) { }
        public virtual void HandleContestedEventHorde(GameObject controlZone) { }
        public virtual void HandleContestedEventAlliance(GameObject controlZone) { }
        public virtual void HandleProgressEventHorde(GameObject controlZone) { }
        public virtual void HandleProgressEventAlliance(GameObject controlZone) { }
        public virtual void HandleNeutralEventHorde(GameObject controlZone) { HandleNeutralEvent(controlZone); }
        public virtual void HandleNeutralEventAlliance(GameObject controlZone) { HandleNeutralEvent(controlZone); }
        public virtual void HandleNeutralEvent(GameObject controlZone) { }
    }
}
