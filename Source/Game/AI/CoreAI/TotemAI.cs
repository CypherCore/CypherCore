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
using Game.Entities;
using Game.Maps;

namespace Game.AI
{
    public class TotemAI : NullCreatureAI
    {
        ObjectGuid _victimGuid;

        public TotemAI(Creature creature) : base(creature)
        {
            Cypher.Assert(creature.IsTotem(), $"TotemAI: AI assigned to a no-totem creature ({creature.GetGUID()})!");
            _victimGuid = ObjectGuid.Empty;
        }

        public override void UpdateAI(uint diff)
        {
            if (me.ToTotem().GetTotemType() != TotemType.Active)
                return;

            if (!me.IsAlive() || me.IsNonMeleeSpellCast(false))
                return;

            // Search spell
            var spellInfo = Global.SpellMgr.GetSpellInfo(me.ToTotem().GetSpell(), me.GetMap().GetDifficultyID());
            if (spellInfo == null)
                return;

            // Get spell range
            float max_range = spellInfo.GetMaxRange(false);

            // SpellModOp.Range not applied in this place just because not existence range mods for attacking totems

            Unit victim = !_victimGuid.IsEmpty() ? Global.ObjAccessor.GetUnit(me, _victimGuid) : null;

            // Search victim if no, not attackable, or out of range, or friendly (possible in case duel end)
            if (victim == null || !victim.IsTargetableForAttack() || !me.IsWithinDistInMap(victim, max_range) || me.IsFriendlyTo(victim) || !me.CanSeeOrDetect(victim))
            {
                var u_check = new NearestAttackableUnitInObjectRangeCheck(me, me.GetCharmerOrOwnerOrSelf(), max_range);
                var checker = new UnitLastSearcher(me, u_check);
                Cell.VisitAllObjects(me, checker, max_range);
                victim = checker.GetTarget();
            }

            // If have target
            if (victim != null)
            {
                // remember
                _victimGuid = victim.GetGUID();

                // attack
                me.CastSpell(victim, me.ToTotem().GetSpell());
            }
            else
                _victimGuid.Clear();
        }

        public override void AttackStart(Unit victim) { }
    }
}
