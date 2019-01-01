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
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Pets
{
    [Script]
    class npc_pet_dk_ebon_gargoyle : CasterAI
    {
        public npc_pet_dk_ebon_gargoyle(Creature creature) : base(creature) { }

        public override void InitializeAI()
        {
            base.InitializeAI();
            ObjectGuid ownerGuid = me.GetOwnerGUID();
            if (ownerGuid.IsEmpty())
                return;

            // Find victim of Summon Gargoyle spell
            List<Unit> targets = new List<Unit>();
            var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(me, me, 30.0f);
            var searcher = new UnitListSearcher(me, targets, u_check);
            Cell.VisitAllObjects(me, searcher, 30.0f);
            foreach (var iter in targets)
            {
                if (iter.GetAura(SpellSummonGargoyle1, ownerGuid) != null)
                {
                    me.Attack(iter, false);
                    break;
                }
            }
        }

        public override void JustDied(Unit killer)
        {
            // Stop Feeding Gargoyle when it dies
            Unit owner = me.GetOwner();
            if (owner)
                owner.RemoveAurasDueToSpell(SpellSummonGargoyle2);
        }

        // Fly away when dismissed
        public override void SpellHit(Unit source, SpellInfo spell)
        {
            if (spell.Id != SpellDismissGargoyle || !me.IsAlive())
                return;

            Unit owner = me.GetOwner();
            if (!owner || owner != source)
                return;

            // Stop Fighting
            me.ApplyModFlag(UnitFields.Flags, UnitFlags.NonAttackable, true);

            // Sanctuary
            me.CastSpell(me, SpellSanctuary, true);
            me.SetReactState(ReactStates.Passive);

            //! HACK: Creature's can't have MOVEMENTFLAG_FLYING
            // Fly Away
            me.SetCanFly(true);
            me.SetSpeedRate(UnitMoveType.Flight, 0.75f);
            me.SetSpeedRate(UnitMoveType.Run, 0.75f);
            float x = me.GetPositionX() + 20 * (float)Math.Cos(me.GetOrientation());
            float y = me.GetPositionY() + 20 * (float)Math.Sin(me.GetOrientation());
            float z = me.GetPositionZ() + 40;
            me.GetMotionMaster().Clear(false);
            me.GetMotionMaster().MovePoint(0, x, y, z);

            // Despawn as soon as possible
            me.DespawnOrUnsummon(4 * Time.InMilliseconds);
        }

        const uint SpellSummonGargoyle1 = 49206;
        const uint SpellSummonGargoyle2 = 50514;
        const uint SpellDismissGargoyle = 50515;
        const uint SpellSanctuary = 54661;
    }

    [Script]
    class npc_pet_dk_guardian : AggressorAI
    {
        public npc_pet_dk_guardian(Creature creature) : base(creature) { }

        public override bool CanAIAttack(Unit target)
        {
            if (!target)
                return false;
            Unit owner = me.GetOwner();
            if (owner && !target.IsInCombatWith(owner))
                return false;
            return base.CanAIAttack(target);
        }
    }
}
