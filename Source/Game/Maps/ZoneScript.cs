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

using Framework.Dynamic;
using Game.Entities;

namespace Game.Maps
{
    public class ZoneScript
    {
        public virtual uint GetCreatureEntry(ulong guidlow, CreatureData data) { return data.id; }
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

        public virtual void ProcessEvent(WorldObject obj, uint eventId) { }

        protected EventMap _events = new EventMap();
    }
}
