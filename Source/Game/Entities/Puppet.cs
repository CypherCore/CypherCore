// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.Entities
{
    public class Puppet : Minion
    {
        public Puppet(SummonPropertiesRecord properties, Unit owner) : base(properties, owner, false)
        {
            Cypher.Assert(owner.IsTypeId(TypeId.Player));
            UnitTypeMask |= UnitTypeMask.Puppet;
        }

        public override void InitStats(uint duration)
        {
            base.InitStats(duration);

            SetLevel(GetOwner().GetLevel());
            SetReactState(ReactStates.Passive);
        }

        public override void InitSummon()
        {
            base.InitSummon();

            if (!SetCharmedBy(GetOwner(), CharmType.Possess))
                Cypher.Assert(false);
        }

        public override void Update(uint diff)
        {
            base.Update(diff);

            //check if caster is channelling?
            if (IsInWorld)
                if (!IsAlive())
                    UnSummon();
            // @todo why long distance .die does not remove it
        }
    }
}