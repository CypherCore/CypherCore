using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ICreature;
using Game.Spells;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        // Darkglare - 103673
        [CreatureScript(103673)]
        public class npc_pet_warlock_darkglare_PetAI : PetAI
        {
            public npc_pet_warlock_darkglare_PetAI(Creature creature) : base(creature)
            {
                Unit owner = me.GetOwner();

                if (owner == null)
                    return;

                creature.SetLevel(owner.GetLevel());
                creature.UpdateLevelDependantStats();
                creature.SetReactState(ReactStates.Assist);
            }

            public override void UpdateAI(uint UnnamedParameter)
            {
                Unit owner = me.GetOwner();

                if (owner == null)
                    return;

                var target = me.GetAttackerForHelper();

                if (target != null)
                {
                    target.RemoveAura(WarlockSpells.DOOM, owner.GetGUID());
                    me.CastSpell(target, WarlockSpells.EYE_LASER, new CastSpellExtraArgs(TriggerCastFlags.None).SetOriginalCaster(owner.GetGUID()));
                }
            }
        }
    }
}