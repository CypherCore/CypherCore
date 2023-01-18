using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Pets
{
    namespace Warlock
    {
        public static class WarlockPetSpells
        {
            public const uint SPELL_WARLOCK_FEL_FIREBOLT = 104318;
        }


        // Wild Imp - 99739
        [Script]
        public class npc_pet_warlock_wild_imp : PetAI
        {
            public npc_pet_warlock_wild_imp(Creature creature) : base(creature)
            {
                Unit owner = me.GetOwner();
                if (me.GetOwner())
                {
                    me.SetLevel(owner.GetLevel());
                    me.SetMaxHealth(owner.GetMaxHealth() / 3);
                    me.SetHealth(owner.GetHealth() / 3);
                }
            }

            public override void UpdateAI(uint UnnamedParameter)
            {
                Unit owner = me.GetOwner();
                if (owner == null)
                {
                    return;
                }

                Unit target = GetTarget();
                ObjectGuid newtargetGUID = owner.GetTarget();
                if (newtargetGUID.IsEmpty() || newtargetGUID == _targetGUID)
                {
                    CastSpellOnTarget(owner, target);
                    return;
                }

                Unit newTarget = ObjectAccessor.Instance.GetUnit(me, newtargetGUID);
                if (ObjectAccessor.Instance.GetUnit(me, newtargetGUID))
                {
                    if (target != newTarget && me.IsValidAttackTarget(newTarget))
                    {
                        target = newTarget;
                    }
                }

                CastSpellOnTarget(owner, target);
            }

            private Unit GetTarget()
            {
                return ObjectAccessor.Instance.GetUnit(me, _targetGUID);
            }

            private void CastSpellOnTarget(Unit owner, Unit target)
            {
                if (target != null && me.IsValidAttackTarget(target) && !me.HasUnitState(UnitState.Casting) && !me.VariableStorage.GetValue("controlled", false))
                {
                    _targetGUID = target.GetGUID();
                    var result = me.CastSpell(target, WarlockPetSpells.SPELL_WARLOCK_FEL_FIREBOLT, new CastSpellExtraArgs(TriggerCastFlags.IgnorePowerAndReagentCost).SetOriginalCaster(owner.GetGUID()));
                }
            }

            private ObjectGuid _targetGUID = new ObjectGuid();
        }
    }
}
