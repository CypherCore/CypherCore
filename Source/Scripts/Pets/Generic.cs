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
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Pets
{
    namespace Generic
    {
        struct SpellIds
        {
            //Mojo
            public const uint FeelingFroggy = 43906;
            public const uint SeductionVisual = 43919;

            //SoulTrader
            public const uint EtherealOnSummon = 50052;
            public const uint EtherealPetRemoveAura = 50055;

            //LichPet
            public const uint LichOnSummon = 69735;
            public const uint LichRemoveAura = 69736;
        }

        struct TextIds
        {
            //Mojo
            public const uint SayMojo = 0;

            //SoulTrader
            public const uint SaySoulTraderInto = 0;
        }

        [Script]
        class npc_pet_gen_soul_trader : ScriptedAI
        {
            public npc_pet_gen_soul_trader(Creature creature) : base(creature) { }

            public override void LeavingWorld()
            {
                Unit owner = me.GetOwner();
                if (owner != null)
                    DoCast(owner, SpellIds.EtherealPetRemoveAura);
            }

            public override void JustAppeared()
            {
                Talk(TextIds.SaySoulTraderInto);

                Unit owner = me.GetOwner();
                if (owner != null)
                    DoCast(owner, SpellIds.EtherealOnSummon);

                base.JustAppeared();
            }
        }

        [Script]
        class npc_pet_lich : ScriptedAI
        {
            public npc_pet_lich(Creature creature) : base(creature) { }

            public override void LeavingWorld()
            {
                Unit owner = me.GetOwner();
                if (owner !=  null)
                    DoCast(owner, SpellIds.LichRemoveAura);
            }

            public override void JustAppeared()
            {
                Unit owner = me.GetOwner();
                if (owner != null)
                    DoCast(owner, SpellIds.LichOnSummon);

                base.JustAppeared();
            }
        }
    }
}
