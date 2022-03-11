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
using Game.Combat;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Pets
{
    namespace Hunter
    {
        struct SpellIds
        {
            public const uint CripplingPoison = 30981;   // Viper
            public const uint DeadlyPoisonPassive = 34657;   // Venomous Snake
            public const uint MindNumbingPoison = 25810;    // Viper
        }

        struct CreatureIds
        {
            public const int Viper = 19921;
        }

        [Script]
        class npc_pet_hunter_snake_trap : ScriptedAI
        {
            public npc_pet_hunter_snake_trap(Creature creature) : base(creature) { }

            public override void JustEngagedWith(Unit who) { }

            public override void JustAppeared()
            {
                _isViper = me.GetEntry() == CreatureIds.Viper ? true : false;

                me.SetMaxHealth((uint)(107 * (me.GetLevel() - 40) * 0.025f));
                // Add delta to make them not all hit the same time
                me.SetBaseAttackTime(WeaponAttackType.BaseAttack, me.GetBaseAttackTime(WeaponAttackType.BaseAttack) + RandomHelper.URand(0, 6) * Time.InMilliseconds);

                if (!_isViper && !me.HasAura(SpellIds.DeadlyPoisonPassive))
                    DoCast(me, SpellIds.DeadlyPoisonPassive, new CastSpellExtraArgs(true));
            }

            // Redefined for random target selection:
            public override void MoveInLineOfSight(Unit who) { }

            public override void UpdateAI(uint diff)
            {
                if (me.GetVictim() && me.GetVictim().HasBreakableByDamageCrowdControlAura())
                { // don't break cc
                    me.GetThreatManager().ClearFixate();
                    me.InterruptNonMeleeSpells(false);
                    me.AttackStop();
                    return;
                }

                if (me.IsSummon() && !me.GetThreatManager().GetFixateTarget())
                { // find new target
                    Unit summoner = me.ToTempSummon().GetSummonerUnit();
                    List<Unit> targets = new();

                    void addTargetIfValid(CombatReference refe)
                    {
                        Unit enemy = refe.GetOther(summoner);
                        if (!enemy.HasBreakableByDamageCrowdControlAura() && me.CanCreatureAttack(enemy) && me.IsWithinDistInMap(enemy, me.GetAttackDistance(enemy)))
                            targets.Add(enemy);
                    }

                    foreach (var pair in summoner.GetCombatManager().GetPvPCombatRefs())
                        addTargetIfValid(pair.Value);

                    if (targets.Empty())
                        foreach (var pair in summoner.GetCombatManager().GetPvECombatRefs())
                            addTargetIfValid(pair.Value);

                    foreach (Unit target in targets)
                        me.EngageWithTarget(target);

                    if (!targets.Empty())
                    {
                        Unit target = targets.SelectRandom();
                        me.GetThreatManager().FixateTarget(target);
                    }
                }

                if (!UpdateVictim())
                    return;

                // Viper
                if (_isViper)
                {
                    if (_spellTimer <= diff)
                    {
                        if (RandomHelper.URand(0, 2) == 0) // 33% chance to cast
                            DoCastVictim(RandomHelper.RAND(SpellIds.MindNumbingPoison, SpellIds.CripplingPoison));

                        _spellTimer = 3000;
                    }
                    else
                        _spellTimer -= diff;
                }

                DoMeleeAttackIfReady();
            }

            bool _isViper;
            uint _spellTimer;
        }
    }
}
