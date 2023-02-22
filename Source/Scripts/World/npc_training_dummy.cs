// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using Scripts.World.NpcSpecial;
using static System.Net.Mime.MediaTypeNames;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_glubtok;

namespace Scripts.World.NpcSpecial
{
    [CreatureScript(new uint[] {
        17578,
        24792,
        30527,
        31143,
        31144,
        31146,
        32541,
        32542,
        32543,
        32545,
        32546,
        32666,
        32667,
        44171,
        44389,
        44548,
        44614,
        44703,
        44794,
        44820,
        44848,
        44937,
        46647,
        48304,
        60197,
        64446,
        67127,
        70245,
        79414,
        79987,
        80017,
        87317,
        87318,
        87320,
        87321,
        87322,
        87329,
        87760,
        87761,
        87762,
        88288,
        88289,
        88314,
        88316,
        88835,
        88836,
        88837,
        88967,
        89078,
        89321,
        92164,
        92165,
        92166,
        92167,
        92168,
        92169,
        93828,
        96442,
        97668,
        98581,
        107557,
        108420,
        109595,
        111824,
        113858,
        113859,
        113860,
        113862,
        113863,
        113864,
        113871,
        113963,
        113964,
        113966,
        113967,
        114832,
        114840,
        117881,
        126340,
        126712,
        126781,
        127019,
        129485,
        131975,
        131983,
        131985,
        131989,
        131990,
        131992,
        131994,
        131997,
        131998,
        132036,
        132976,
        134324,
        138048,
        143509,
        143947,
        144074,
        144075,
        144076,
        144077,
        144078,
        144079,
        144080,
        144081,
        144082,
        144083,
        144085,
        144086,
        149860,
        151022,
        153285,
        153292,
        163534,
        173072,
        173942,
        174435,
        174567,
        174568,
        174569,
        174570,
        174571,
        175449,
        175450,
        175455,
        175456,
        188352,
        189082,
        190621,
        190623,
        190624,
        193563,
        194643,
        194644,
        194645,
        194646,
        194648,
        196394,
        197833,
        197834,
        198594})]
    internal class npc_training_dummy : NullCreatureAI
    {
        private readonly Dictionary<ObjectGuid, TimeSpan> _combatTimer = new();

        public npc_training_dummy(Creature creature) : base(creature)
        {
            creature.SetControlled(true, UnitState.Stunned);
            creature.SetControlled(true, UnitState.Root);
            creature.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBack, true);
            creature.SetUnitFlag3(UnitFlags3.UnconsciousOnDeath);
        }

        public override void JustEnteredCombat(Unit who)
        {
            _combatTimer[who.GetGUID()] = TimeSpan.FromSeconds(15);
        }

        public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (!attacker)
                return;

            _combatTimer[attacker.GetGUID()] = TimeSpan.FromSeconds(15);
        }

        public override void UpdateAI(uint diff)
        {
            foreach (var key in _combatTimer.Keys.ToList())
            {
                _combatTimer[key] -= TimeSpan.FromMilliseconds(diff);

                if (_combatTimer[key] <= TimeSpan.Zero)
                {
                    // The Attacker has not dealt any Damage to the dummy for over 5 seconds. End combat.
                    var pveRefs = me.GetCombatManager().GetPvECombatRefs();
                    var it = pveRefs.LookupByKey(key);

                    it?.EndCombat();

                    _combatTimer.Remove(key);
                }
            }

        }

        public override void Reset()
        {
            me.SetControlled(true, UnitState.Stunned);
            me.SetControlled(true, UnitState.Root);
            me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBack, true);
            base.Reset();
        }
    }

//    class npc_training_dummy_start_zones : public CreatureScript
//{
//public:
//    npc_training_dummy_start_zones() : CreatureScript("npc_training_dummy_start_zones") { }

//    struct npc_training_dummy_start_zonesAI : Scripted_NoMovementAI
//    {
//        npc_training_dummy_start_zonesAI(Creature* creature) : Scripted_NoMovementAI(creature)
//        { }

//        uint32 resetTimer;

//        void Reset()
//        {
//            me->SetControlled(true, UNIT_STATE_STUNNED);//disable rotate
//            me->ApplySpellImmune(0, IMMUNITY_EFFECT, EFFECT_KNOCK_BACK, true);//imune to knock aways like blast wave

//            resetTimer = 5000;
//        }

//        void EnterEvadeMode()
//        {
//            if (!_EnterEvadeMode())
//                return;

//            Reset();
//        }

//        void DamageTaken(Unit* doneBy, uint32& damage)
//        {
//            resetTimer = 5000;
//            damage = 0;

//            if (doneBy->HasAura(SEAL_OF_COMMAND))
//                if (doneBy->ToPlayer())
//                    doneBy->ToPlayer()->KilledMonsterCredit(44175, 0);
//        }

//        void EnterCombat(Unit* /*who*/)
//        {
//            return;
//        }

//        void SpellHit(Unit* Caster, const SpellInfo* Spell)
//        {
//            switch (Spell->Id)
//            {
//                case MOONFIRE:
//                case CHARGE:
//                case STEADY_SHOT:
//                case EVISCERATION:
//                case SHADOW_WORD_PAIN_1:
//                case SHADOW_WORD_PAIN_2:
//                case FROST_NOVA:
//                case CORRUPTION_1:
//                case CORRUPTION_2:
//                case CORRUPTION_3:
//                case TIGER_PALM:
//                    if (Caster->ToPlayer())
//                        Caster->ToPlayer()->KilledMonsterCredit(44175, 0);
//                    break;
//                default:
//                    break;
//            }
//}

//void UpdateAI(uint32 const diff)
//{
//    if (!UpdateVictim())
//        return;

//    if (!me->HasUnitState(UNIT_STATE_STUNNED))
//        me->SetControlled(true, UNIT_STATE_STUNNED);//disable rotate

//    if (resetTimer <= diff)
//    {
//        EnterEvadeMode();
//        resetTimer = 5000;
//    }
//    else
//        resetTimer -= diff;
//}
//void MoveInLineOfSight(Unit* /*who*/) { return; }
//    };

//CreatureAI* GetAI(Creature* creature) const
//    {
//        return new npc_training_dummy_start_zonesAI(creature);
//    }
//};
}