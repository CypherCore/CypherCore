// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;
using Game.Maps;

namespace Game
{
    internal class GameEventAIHookWorker : Notifier
    {
        private readonly bool _activate;

        private readonly ushort _eventId;

        public GameEventAIHookWorker(ushort eventId, bool activate)
        {
            _eventId = eventId;
            _activate = activate;
        }

        public override void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];

                if (creature.IsInWorld &&
                    creature.IsAIEnabled())
                    creature.GetAI().OnGameEvent(_activate, _eventId);
            }
        }

        public override void Visit(IList<GameObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                GameObject gameObject = objs[i];

                if (gameObject.IsInWorld)
                    gameObject.GetAI().OnGameEvent(_activate, _eventId);
            }
        }
    }
}