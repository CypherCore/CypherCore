// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.AI
{
    class ScheduledChangeAI : CreatureAI
    {
        public ScheduledChangeAI(Creature creature) : base(creature) { }

        public override void MoveInLineOfSight(Unit unit) { }

        public override void AttackStart(Unit unit) { }

        public override void JustStartedThreateningMe(Unit unit) { }

        public override void JustEnteredCombat(Unit unit) { }

        public override void UpdateAI(uint diff) { }

        public override void JustAppeared() { }

        public override void EnterEvadeMode(EvadeReason why) { }

        public override void OnCharmed(bool isNew) { }
    }
}
