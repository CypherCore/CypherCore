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
using Framework.Dynamic;
using Game.Entities;
using Game.Spells;

namespace Game.AI
{
    public class GameObjectAI
    {
        protected TaskScheduler _scheduler;
        protected EventMap _events;

        public GameObject me;

        public GameObjectAI(GameObject go)
        {
            me = go;
            _scheduler = new TaskScheduler();
            _events = new EventMap();
        }

        public virtual void UpdateAI(uint diff) { }

        public virtual void InitializeAI() { Reset(); }

        public virtual void Reset() { }

        // Pass parameters between AI
        public virtual void DoAction(int param = 0) { }
        public virtual void SetGUID(ObjectGuid guid, int id = 0) { }
        public virtual ObjectGuid GetGUID(int id = 0) { return ObjectGuid.Empty; }

        /// <summary>
        /// Called when the dialog status between a player and the gameobject is requested.
        /// </summary>
        public virtual QuestGiverStatus? GetDialogStatus(Player player) { return null; }

        /// <summary>
        /// Called when a player opens a gossip dialog with the gameobject.
        /// </summary>
        public virtual bool GossipHello(Player player) { return false; }

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
        public virtual void QuestReward(Player player, Quest quest, LootItemType type, uint opt) { }

        // Called when a Player clicks a GameObject, before GossipHello
        // prevents achievement tracking if returning true
        public virtual bool OnReportUse(Player player) { return false; }

        public virtual void Destroyed(WorldObject attacker, uint eventId) { }
        public virtual void Damaged(WorldObject attacker, uint eventId) { }

        public virtual void SetData64(uint id, ulong value) { }
        public virtual ulong GetData64(uint id) { return 0; }
        public virtual uint GetData(uint id) { return 0; }
        public virtual void SetData(uint id, uint value) { }

        public virtual void OnGameEvent(bool start, ushort eventId) { }
        public virtual void OnLootStateChanged(uint state, Unit unit) { }
        public virtual void OnStateChanged(GameObjectState state) { }
        public virtual void EventInform(uint eventId) { }

        // Called when hit by a spell
        public virtual void SpellHit(WorldObject caster, SpellInfo spellInfo) { }

        // Called when spell hits a target
        public virtual void SpellHitTarget(WorldObject target, SpellInfo spellInfo) { }

        // Called when the gameobject summon successfully other creature
        public virtual void JustSummoned(Creature summon) { }

        public virtual void SummonedCreatureDespawn(Creature summon) { }
        public virtual void SummonedCreatureDies(Creature summon, Unit killer) { }
    }
}
