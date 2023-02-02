// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Dynamic;
using Game.Entities;

namespace Game.Maps
{
    public class ZoneScript
    {
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

        public virtual void OnUnitDeath(Unit unit) { }

        //All-purpose data storage 64 bit
        public virtual ObjectGuid GetGuidData(uint DataId) { return ObjectGuid.Empty; }
        public virtual void SetGuidData(uint DataId, ObjectGuid Value) { }

        public virtual ulong GetData64(uint dataId) { return 0; }
        public virtual void SetData64(uint dataId, ulong value) { }

        //All-purpose data storage 32 bit
        public virtual uint GetData(uint dataId) { return 0; }
        public virtual void SetData(uint dataId, uint value) { }

        public virtual void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker) { }

        protected EventMap _events = new();
    }
}
