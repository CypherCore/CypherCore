/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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

namespace Scripts.Pets.Priest
{
    struct SpellIds
    {
        public const uint GlyphOfShadowFiend = 58228;
        public const uint ShadowFiendDeath = 57989;
        public const uint LightWellCharges = 59907;
    }

    [Script]
    class npc_pet_pri_lightwell : CreatureScript
    {
        public npc_pet_pri_lightwell() : base("npc_pet_pri_lightwell") { }

        class npc_pet_pri_lightwellAI : PassiveAI
        {
            public npc_pet_pri_lightwellAI(Creature creature) : base(creature)
            {
                DoCast(creature, SpellIds.LightWellCharges, false);
            }

            public override void EnterEvadeMode(EvadeReason why)
            {
                if (!me.IsAlive())
                    return;

                me.DeleteThreatList();
                me.CombatStop(true);
                me.ResetPlayerDamageReq();
            }
        }

        public override CreatureAI GetAI(Creature creature)
        {
            return new npc_pet_pri_lightwellAI(creature);
        }
    }

    [Script]
    class npc_pet_pri_shadowfiend : CreatureScript
    {
        public npc_pet_pri_shadowfiend() : base("npc_pet_pri_shadowfiend") { }

        class npc_pet_pri_shadowfiendAI : PetAI
        {
            public npc_pet_pri_shadowfiendAI(Creature creature) : base(creature) { }

            public override void IsSummonedBy(Unit summoner)
            {
                if (summoner.HasAura(SpellIds.GlyphOfShadowFiend))
                    DoCastAOE(SpellIds.ShadowFiendDeath);
            }

        }

        public override CreatureAI GetAI(Creature creature)
        {
            return new npc_pet_pri_shadowfiendAI(creature);
        }
    }
}
