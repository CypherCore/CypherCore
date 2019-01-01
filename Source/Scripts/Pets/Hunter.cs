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
using Game.Scripting;

namespace Scripts.Pets
{
    [Script]
    class npc_pet_hunter_snake_trap : ScriptedAI
    {
        public npc_pet_hunter_snake_trap(Creature creature) : base(creature) { }

        public override void EnterCombat(Unit who) { }

        public override void Reset()
        {
            _spellTimer = 0;

            CreatureTemplate Info = me.GetCreatureTemplate();

            _isViper = Info.Entry == NpcViper ? true : false;

            me.SetMaxHealth((uint)(107 * (me.getLevel() - 40) * 0.025f));
            // Add delta to make them not all hit the same time
            uint delta = (RandomHelper.Rand32() % 7) * 100;
            me.SetStatFloatValue(UnitFields.BaseAttackTime, Info.BaseAttackTime + delta);
            //me.SetStatFloatValue(UnitFields.RangedAttackPower, (float)Info.AttackPower);

            // Start attacking attacker of owner on first ai update after spawn - move in line of sight may choose better target
            if (!me.GetVictim() && me.IsSummon())
            {
                Unit owner = me.ToTempSummon().GetSummoner();
                if (owner)
                    if (owner.getAttackerForHelper())
                        AttackStart(owner.getAttackerForHelper());
            }

            if (!_isViper)
                DoCast(me, SpellDeadlyPoisonPassive, true);
        }

        // Redefined for random target selection:
        public override void MoveInLineOfSight(Unit who)
        {
            if (!me.GetVictim() && me.CanCreatureAttack(who))
            {
                if (me.GetDistanceZ(who) > SharedConst.CreatureAttackRangeZ)
                    return;

                float attackRadius = me.GetAttackDistance(who);
                if (me.IsWithinDistInMap(who, attackRadius) && me.IsWithinLOSInMap(who))
                {
                    if ((RandomHelper.Rand32() % 5) == 0)
                    {
                        me.setAttackTimer(WeaponAttackType.BaseAttack, (RandomHelper.Rand32() % 10) * 100);
                        _spellTimer = (RandomHelper.Rand32() % 10) * 100;
                        AttackStart(who);
                    }
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() || !me.GetVictim())
                return;

            if (me.GetVictim().HasBreakableByDamageCrowdControlAura(me))
            {
                me.InterruptNonMeleeSpells(false);
                return;
            }

            //Viper
            if (_isViper)
            {
                if (_spellTimer <= diff)
                {
                    if (RandomHelper.IRand(0, 2) == 0) //33% chance to cast
                        DoCastVictim(RandomHelper.RAND(SpellMindNumbingPoison, SpellCripplingPoison));

                    _spellTimer = 3000;
                }
                else
                    _spellTimer -= diff;
            }

            DoMeleeAttackIfReady();
        }

        bool _isViper;
        uint _spellTimer;

        const uint SpellCripplingPoison = 30981;   // Viper
        const uint SpellDeadlyPoisonPassive = 34657;   // Venomous Snake
        const uint SpellMindNumbingPoison = 25810;    // Viper

        const int NpcViper = 19921;
    }
}
