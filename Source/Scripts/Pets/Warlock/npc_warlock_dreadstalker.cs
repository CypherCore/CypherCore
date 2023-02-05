using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ICreature;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        // Dreadstalker - 98035
        [Script]
        public class npc_warlock_dreadstalker : ScriptObjectAutoAddDBBound, ICreatureGetAI
        {
            public class npc_warlock_dreadstalkerAI : ScriptedAI
            {
                public bool firstTick = true;

                public npc_warlock_dreadstalkerAI(Creature creature) : base(creature)
                {
                }

                public override void UpdateAI(uint UnnamedParameter)
                {
                    if (firstTick)
                    {
                        Unit owner = me.GetOwner();

                        if (!me.GetOwner() ||
                            !me.GetOwner().ToPlayer())
                            return;

                        me.SetMaxHealth(owner.CountPctFromMaxHealth(40));
                        me.SetHealth(me.GetMaxHealth());

                        Unit target = owner.ToPlayer().GetSelectedUnit();

                        if (owner.ToPlayer().GetSelectedUnit())
                            me.CastSpell(target, WarlockSpells.DREADSTALKER_CHARGE, true);

                        firstTick = false;

                        //me->CastSpell(SPELL_WARLOCK_SHARPENED_DREADFANGS_BUFF, SPELLVALUE_BASE_POINT0, owner->GetAuraEffectAmount(SPELL_WARLOCK_SHARPENED_DREADFANGS, EFFECT_0), me, true);
                    }

                    UpdateVictim();
                    DoMeleeAttackIfReady();
                }
            }

            public npc_warlock_dreadstalker() : base("npc_warlock_dreadstalker")
            {
            }

            public CreatureAI GetAI(Creature creature)
            {
                return new npc_warlock_dreadstalkerAI(creature);
            }
        }
    }
}