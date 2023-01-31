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
        [Script]
        // Darkglare - 103673
        public class npc_pet_warlock_darkglare : ScriptObjectAutoAddDBBound, ICreatureGetAI
        {
            public class npc_pet_warlock_darkglare_PetAI : PetAI
            {
                public npc_pet_warlock_darkglare_PetAI(Creature creature) : base(creature)
                {
                }

                public override void UpdateAI(uint UnnamedParameter)
                {
                    Unit owner = me.GetOwner();

                    if (owner == null)
                        return;

                    var target = me.GetAttackerForHelper();

                    if (target != null)
                    {
                        target.RemoveAura(SpellIds.DOOM, owner.GetGUID());
                        me.CastSpell(target, SpellIds.EYE_LASER, new CastSpellExtraArgs(TriggerCastFlags.None).SetOriginalCaster(owner.GetGUID()));
                    }
                }
            }

            public npc_pet_warlock_darkglare() : base("npc_pet_warlock_darkglare")
            {
            }

            //C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
            //ORIGINAL LINE: CreatureAI* GetAI(Creature* creature) const override
            public CreatureAI GetAI(Creature creature)
            {
                return new npc_pet_warlock_darkglare_PetAI(creature);
            }
        }
    }
}