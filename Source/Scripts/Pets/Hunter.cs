// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Combat;
using Game.Entities;
using Game.Scripting;
using System.Collections.Generic;

namespace Scripts.Pets.Hunter
{
    [Script]
    class npc_pet_hunter_snake_trap : ScriptedAI
    {
        const uint SpellHunterCripplingPoison = 30981; // Viper
        const uint SpellHunterDeadlyPoisonPassive = 34657; // Venomous Snake
        const uint SpellHunterMindNumbingPoison = 25810;  // Viper

        const uint NpcHunterViper = 19921;

        bool _isViper;
        uint _spellTimer;

        public npc_pet_hunter_snake_trap(Creature creature) : base(creature) { }

        public override void JustEngagedWith(Unit who) { }

        public override void JustAppeared()
        {
            _isViper = me.GetEntry() == NpcHunterViper ? true : false;

            me.SetMaxHealth((uint)(107 * (me.GetLevel() - 40) * 0.025f));
            // Add delta to make them not all hit the same time
            me.SetBaseAttackTime(WeaponAttackType.BaseAttack, me.GetBaseAttackTime(WeaponAttackType.BaseAttack) + RandomHelper.URand(0, 6));

            if (!_isViper && !me.HasAura(SpellHunterDeadlyPoisonPassive))
                DoCast(me, SpellHunterDeadlyPoisonPassive, true);
        }

        // Redefined for random target selection:
        public override void MoveInLineOfSight(Unit who) { }

        public override void UpdateAI(uint diff)
        {
            if (me.GetVictim() != null && me.GetVictim().HasBreakableByDamageCrowdControlAura())
            { // don't break cc
                me.GetThreatManager().ClearFixate();
                me.InterruptNonMeleeSpells(false);
                me.AttackStop();
                return;
            }

            if (me.IsSummon() && me.GetThreatManager().GetFixateTarget() == null)
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
                        DoCastVictim(RandomHelper.RAND(SpellHunterMindNumbingPoison, SpellHunterCripplingPoison));

                    _spellTimer = 3000;
                }
                else
                    _spellTimer -= diff;
            }
        }
    }
}