﻿using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(new uint[] { 48278, 48440, 48441, 48442 })]

    public class npc_mining_monkey : ScriptedAI
    {
        public npc_mining_monkey(Creature creature) : base(creature)
        {
            Instance = creature.GetInstanceScript();
        }

        public InstanceScript Instance;
        public uint Phase;
        public uint UiTimer;

        public override void Reset()
        {
            base.Reset();
            Phase = 1;
            UiTimer = 2000;
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            base.DamageTaken(attacker, ref damage, damageType, spellInfo);

            if (!me)
            {
                return;
            }

            if (Phase == 1)
            {
                if (me.GetHealth() - damage <= me.GetMaxHealth() * 0.15)
                {
                    Phase++;
                }
            }
        }

        public override void JustEnteredCombat(Unit who)
        {
            base.JustEnteredCombat(who);

            if (!me)
            {
                return;
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!me || me.GetAI() != null || !UpdateVictim())
            {
                return;
            }

            switch (Phase)
            {
                case 1:
                    Unit victim = me.GetVictim();
                    if (victim != null)
                    {
                        if (me.IsInRange(victim, 0, 35.0f, true))
                        {
                            me.SetUnitFlag(UnitFlags.Pacified);
                            me.SetUnitFlag(UnitFlags.Stunned);
                            if (UiTimer <= diff)
                            {
                                me.CastSpell(victim, IsHeroic() ? DMSpells.SPELL_THROW_H : DMSpells.SPELL_THROW);
                                UiTimer = 2000;
                            }
                            else
                            {
                                UiTimer -= diff;
                            }
                        }
                        else
                        {
                            me.RemoveUnitFlag(UnitFlags.Pacified);
                            me.RemoveUnitFlag(UnitFlags.Stunned);
                        }
                    }
                    break;
                case 2:
                    Talk(0);
                    me.RemoveUnitFlag(UnitFlags.Uninteractible);
                    Phase++;
                    break;
                default:
                    me.DoFleeToGetAssistance();
                    break;
            }
        }
    }
}