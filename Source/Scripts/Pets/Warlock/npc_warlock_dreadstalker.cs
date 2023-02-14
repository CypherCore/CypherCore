// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
        // Dreadstalker - 98035
        [CreatureScript(98035)]
        public class npc_warlock_dreadstalkerAI : PetAI
        {
            public bool firstTick = true;

            public npc_warlock_dreadstalkerAI(Creature creature) : base(creature)
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

                base.UpdateAI(UnnamedParameter);
            }
        }
    }
}