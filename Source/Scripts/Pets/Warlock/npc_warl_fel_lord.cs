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
        [CreatureScript(107024)]
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

                me.CastSpell(me, WarlockSpells.FEL_LORD_CLEAVE, false);
            }
        }
    }
}