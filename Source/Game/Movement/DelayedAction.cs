// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;

namespace Game.Movement
{
    internal class DelayedAction
    {
        private readonly Action _action;
        private readonly MotionMasterDelayedActionType _type;
        private readonly Func<bool> _validator;

        public DelayedAction(Action action, Func<bool> validator, MotionMasterDelayedActionType type)
        {
            _action = action;
            _validator = validator;
            _type = type;
        }

        public DelayedAction(Action action, MotionMasterDelayedActionType type)
        {
            _action = action;
            _validator = () => true;
            _type = type;
        }

        public void Resolve()
        {
            if (_validator())
                _action();
        }
    }
}