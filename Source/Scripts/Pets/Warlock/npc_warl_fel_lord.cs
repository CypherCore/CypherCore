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
        // 107024 - Fel Lord
        [CreatureScript(107024)]
        public class npc_warl_fel_lordAI : PetAI
        {
            public npc_warl_fel_lordAI(Creature creature) : base(creature)
            {
                Unit owner = me.GetOwner();
                if (owner == null)
                    return;

                creature.SetLevel(owner.GetLevel());
                creature.UpdateLevelDependantStats();
                creature.SetReactState(ReactStates.Assist);
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

            //public override void UpdateAI(uint UnnamedParameter)
            //{
            //    if (me.HasUnitState(UnitState.Casting))
            //        return;

            //    me.CastSpell(me, WarlockSpells.FEL_LORD_CLEAVE, false);
            //}
        }
    }
}