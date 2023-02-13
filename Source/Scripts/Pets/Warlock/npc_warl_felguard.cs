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
        [CreatureScript(17252)]
        public class npc_warl_felguard : PetAI
        {
            public npc_warl_felguard(Creature creature) : base(creature)
            {
                Unit owner = me.GetOwner();
                if (owner == null)
                    return;

                creature.SetLevel(owner.GetLevel());
                creature.UpdateLevelDependantStats();
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
        }
    }
}