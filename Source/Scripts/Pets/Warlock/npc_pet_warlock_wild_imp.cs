// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using Scripts.Spells.Warlock;
using static Game.AI.SmartAction;
using static Game.AI.SmartEvent;

namespace Scripts.Pets
{
    namespace Warlock
    {
        // Wild Imp - 99739
        [CreatureScript(55659)]
        public class npc_pet_warlock_wild_imp : SmartAI
        {
            private ObjectGuid _targetGUID = new();

            public npc_pet_warlock_wild_imp(Creature creature) : base(creature)
            {
                Unit owner = me.GetOwner();

                if (me.GetOwner())
                {
                    me.SetLevel(owner.GetLevel());
                    me.SetMaxHealth(owner.GetMaxHealth() / 3);
                    me.SetHealth(owner.GetHealth() / 3);

                    if (owner.IsPlayer())
                    {
                        var p = owner.ToPlayer();
                        p.AddAura(296553, p);
                    }
                }


                creature.UpdateLevelDependantStats();
                creature.SetReactState(ReactStates.Aggressive);
                creature.SetCreatorGUID(owner.GetGUID());

                var summon = creature.ToTempSummon();

                if (summon != null)
                {
                    summon.SetCanFollowOwner(true);
                    summon.GetMotionMaster().Clear();
                    summon.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, summon.GetFollowAngle());
                }
            }

            public override void UpdateAI(uint UnnamedParameter)
            {
                Unit owner = me.GetOwner();

                if (owner == null)
                    return;

                Unit target = GetTarget();
                ObjectGuid newtargetGUID = owner.GetTarget();

                if (newtargetGUID.IsEmpty() ||
                    newtargetGUID == _targetGUID)
                {
                    CastSpellOnTarget(owner, target);

                    return;
                }

                Unit newTarget = ObjectAccessor.Instance.GetUnit(me, newtargetGUID);

                if (ObjectAccessor.Instance.GetUnit(me, newtargetGUID))
                    if (target != newTarget &&
                        me.IsValidAttackTarget(newTarget))
                        target = newTarget;

                CastSpellOnTarget(owner, target);
            }

            public override void OnDespawn()
            {
                var caster = me.GetOwner();

                if (caster == null) return; 

                if (caster.GetCreatureListWithEntryInGrid(55659).Count == 0)
                    caster.RemoveAura(296553);
            }

            private Unit GetTarget()
            {
                return ObjectAccessor.Instance.GetUnit(me, _targetGUID);
            }

            private void CastSpellOnTarget(Unit owner, Unit target)
            {
                if (target != null &&
                    me.IsValidAttackTarget(target) &&
                    !me.HasUnitState(UnitState.Casting) &&
                    !me.VariableStorage.GetValue("controlled", false))
                {
                    _targetGUID = target.GetGUID();
                    me.CastSpell(target, WarlockSpells.FEL_FIREBOLT, new CastSpellExtraArgs(TriggerCastFlags.IgnorePowerAndReagentCost).SetOriginalCaster(owner.GetGUID()));
                }
            }
        }
    }
}