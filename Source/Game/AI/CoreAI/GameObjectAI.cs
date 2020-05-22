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

using Framework.Dynamic;
using Game.Entities;
using Game.Spells;
using Framework.Constants;

namespace Game.AI
{
    public class GameObjectAI
    {
        public GameObjectAI(GameObject gameObject)
        {
            me = gameObject;
            _scheduler = new TaskScheduler();
            _events = new EventMap();
        }

        public virtual void UpdateAI(uint diff) { }

        public virtual void InitializeAI() { Reset(); }

        public virtual void Reset() { }

        // Pass parameters between AI
        public virtual void DoAction(int param = 0) { }
        public virtual void SetGUID(ulong guid, int id = 0) { }
        public virtual ulong GetGUID(int id = 0) { return 0; }

        /// <summary>
        /// Called when a player opens a gossip dialog with the gameobject.
        /// </summary>
        public virtual bool GossipHello(Player player, bool reportUse) { return false; }

        /// <summary>
        /// Called when a player selects a gossip item in the gameobject's gossip menu.
        /// </summary>
        public virtual bool GossipSelect(Player player, uint menuId, uint gossipListId) { return false; }

        /// <summary>
        /// Called when a player selects a gossip with a code in the gameobject's gossip menu.
        /// </summary>
        public virtual bool GossipSelectCode(Player player, uint sender, uint action, string code) { return false; }

        /// <summary>
        /// Called when a player accepts a quest from the gameobject.
        /// </summary>
        public virtual void QuestAccept(Player player, Quest quest) { }

        /// <summary>
        /// Called when a player completes a quest and is rewarded, opt is the selected item's index or 0
        /// </summary>
        public virtual void QuestReward(Player player, Quest quest, uint opt) { }

        /// <summary>
        /// Called when the dialog status between a player and the gameobject is requested.
        /// </summary>
        public virtual uint GetDialogStatus(Player player) { return 100; }
        public virtual void Destroyed(Player player, uint eventId) { }
        public virtual void Damaged(Player player, uint eventId) { }

        public virtual void SetData64(uint id, ulong value) { }
        public virtual ulong GetData64(uint id) { return 0; }
        public virtual uint GetData(uint id) { return 0; }
        public virtual void SetData(uint id, uint value) { }

        public virtual void OnGameEvent(bool start, ushort eventId) { }
        public virtual void OnLootStateChanged(uint state, Unit unit) { }
        public virtual void OnStateChanged(GameObjectState state) { }
        public virtual void EventInform(uint eventId) { }
        public virtual void SpellHit(Unit unit, SpellInfo spellInfo) { }

        protected TaskScheduler _scheduler;
        protected EventMap _events;

        public GameObject me;
    }

    public class NullGameObjectAI : GameObjectAI
    {
        public NullGameObjectAI(GameObject g) : base(g) { }

        public override void UpdateAI(uint diff) { }
    }
}
