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
        // 103679 - Soul Effigy
        [CreatureScript(103679)]
        public class npc_warlock_soul_effigy : CreatureAI
        {
            public npc_warlock_soul_effigy(Creature creature) : base(creature)
            {
                Unit owner = me.GetOwner();
                if (owner == null)
                    return;

                creature.SetLevel(owner.GetLevel());
                creature.UpdateLevelDependantStats();
                creature.SetReactState(ReactStates.Aggressive);
            }

            public override void Reset()
            {
                me.SetControlled(true, UnitState.Root);
                me.CastSpell(me, WarlockSpells.SOUL_EFFIGY_AURA, true);
            }

            public override void UpdateAI(uint UnnamedParameter)
            {
            }
        }
    }
}