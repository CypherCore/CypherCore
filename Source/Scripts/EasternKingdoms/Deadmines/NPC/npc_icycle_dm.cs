// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(49481)]
    public class npc_icycle_dm : NullCreatureAI
    {
        public npc_icycle_dm(Creature creature) : base(creature)
        {
            me.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.NonAttackable | UnitFlags.Pacified);
            me.SetReactState(ReactStates.Passive);
            me.SetDisplayId(28470);
        }

        public uint HitTimer;

        public override void Reset()
        {
            HitTimer = 2500;
        }

        public override void UpdateAI(uint diff)
        {
            if (HitTimer <= diff)
            {
                DoCast(me, 92201);
                DoCast(me, 62453);
            }
            else
            {
                HitTimer -= diff;
            }
        }
    }
}
