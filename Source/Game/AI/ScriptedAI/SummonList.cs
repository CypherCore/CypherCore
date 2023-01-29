// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;

namespace Game.AI
{
    public class SummonList : List<ObjectGuid>
    {
        private readonly Creature _me;

        public SummonList(Creature creature)
        {
            _me = creature;
        }

        public void Summon(Creature summon)
        {
            Add(summon.GetGUID());
        }

        public void DoZoneInCombat(uint entry = 0)
        {
            foreach (var id in this)
            {
                Creature summon = ObjectAccessor.GetCreature(_me, id);

                if (summon &&
                    summon.IsAIEnabled() &&
                    (entry == 0 || summon.GetEntry() == entry))
                    summon.GetAI().DoZoneInCombat(null);
            }
        }

        public void DespawnEntry(uint entry)
        {
            foreach (var id in this)
            {
                Creature summon = ObjectAccessor.GetCreature(_me, id);

                if (!summon)
                {
                    Remove(id);
                }
                else if (summon.GetEntry() == entry)
                {
                    Remove(id);
                    summon.DespawnOrUnsummon();
                }
            }
        }

        public void DespawnAll()
        {
            while (!this.Empty())
            {
                Creature summon = ObjectAccessor.GetCreature(_me, this.FirstOrDefault());
                RemoveAt(0);

                if (summon)
                    summon.DespawnOrUnsummon();
            }
        }

        public void Despawn(Creature summon)
        {
            Remove(summon.GetGUID());
        }

        public void DespawnIf(ICheck<ObjectGuid> predicate)
        {
            this.RemoveAll(predicate);
        }

        public void DespawnIf(Predicate<ObjectGuid> predicate)
        {
            RemoveAll(predicate);
        }

        public void RemoveNotExisting()
        {
            foreach (var id in this)
                if (!ObjectAccessor.GetCreature(_me, id))
                    Remove(id);
        }

        public void DoAction(int info, ICheck<ObjectGuid> predicate, ushort max = 0)
        {
            // We need to use a copy of SummonList here, otherwise original SummonList would be modified
            List<ObjectGuid> listCopy = new(this);
            listCopy.RandomResize(predicate.Invoke, max);
            DoActionImpl(info, listCopy);
        }

        public void DoAction(int info, Predicate<ObjectGuid> predicate, ushort max = 0)
        {
            // We need to use a copy of SummonList here, otherwise original SummonList would be modified
            List<ObjectGuid> listCopy = new(this);
            listCopy.RandomResize(predicate, max);
            DoActionImpl(info, listCopy);
        }

        public bool HasEntry(uint entry)
        {
            foreach (var id in this)
            {
                Creature summon = ObjectAccessor.GetCreature(_me, id);

                if (summon && summon.GetEntry() == entry)
                    return true;
            }

            return false;
        }

        private void DoActionImpl(int action, List<ObjectGuid> summons)
        {
            foreach (var guid in summons)
            {
                Creature summon = ObjectAccessor.GetCreature(_me, guid);

                if (summon && summon.IsAIEnabled())
                    summon.GetAI().DoAction(action);
            }
        }
    }
}