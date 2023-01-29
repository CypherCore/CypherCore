// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class AIRelocationNotifier : Notifier
    {
        private readonly Unit _iunit;
        private readonly bool _isCreature;

        public AIRelocationNotifier(Unit unit)
        {
            _iunit = unit;
            _isCreature = unit.IsTypeId(TypeId.Unit);
        }

        public override void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                CreatureUnitRelocationWorker(creature, _iunit);

                if (_isCreature)
                    CreatureUnitRelocationWorker(_iunit.ToCreature(), creature);
            }
        }
    }

}