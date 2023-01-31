using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ICreature;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        // 107024 - Fel Lord
        [Script]
        public class npc_warl_fel_lord : ScriptObjectAutoAddDBBound, ICreatureGetAI
        {
            public class npc_warl_fel_lordAI : CreatureAI
            {
                public npc_warl_fel_lordAI(Creature creature) : base(creature)
                {
                }

                public override void Reset()
                {
                    Unit owner = me.GetOwner();

                    if (owner == null)
                        return;

                    me.SetMaxHealth(owner.GetMaxHealth());
                    me.SetHealth(me.GetMaxHealth());
                    me.SetControlled(true, UnitState.Root);
                }

                public override void UpdateAI(uint UnnamedParameter)
                {
                    if (me.HasUnitState(UnitState.Casting))
                        return;

                    me.CastSpell(me, SpellIds.FEL_LORD_CLEAVE, false);
                }
            }

            public npc_warl_fel_lord() : base("npc_warl_fel_lord")
            {
            }

            public CreatureAI GetAI(Creature creature)
            {
                return new npc_warl_fel_lordAI(creature);
            }
        }
    }
}