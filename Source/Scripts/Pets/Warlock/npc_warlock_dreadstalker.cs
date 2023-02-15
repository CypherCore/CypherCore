// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Bgs.Protocol.Notification.V1;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ICreature;
using Scripts.Spells.Warlock;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_glubtok;

namespace Scripts.Pets
{
    namespace Warlock
    {
        // Dreadstalker - 98035
        [CreatureScript(98035)]
        public class npc_warlock_dreadstalker : PetAI
        {
            public bool firstTick = true;

            public npc_warlock_dreadstalker(Creature creature) : base(creature)
            {
                Unit owner = me.GetOwner();
                if (owner == null || owner.ToPlayer() == null)
                    return;

                creature.SetLevel(owner.GetLevel());
                creature.UpdateLevelDependantStats();
                creature.SetReactState(ReactStates.Aggressive);
                creature.SetCreatorGUID(owner.GetGUID());

                var summon = creature.ToTempSummon();

                if (summon != null)
                {
                    summon.SetCanFollowOwner(true);
                }
            }

            public override void UpdateAI(uint UnnamedParameter)
            {
                if (firstTick)
                {
                    Unit owner = me.GetOwner();

                    if (!me.GetOwner() ||
                        !me.GetOwner().ToPlayer())
                        return;

                    Unit target = owner.ToPlayer().GetSelectedUnit();

                    if (target)
                    {
                        me.CastSpell(target, WarlockSpells.DREADSTALKER_CHARGE, true);
                    }

                    firstTick = false;
                }

                base.UpdateAI(UnnamedParameter);
            }
        }
    }
}