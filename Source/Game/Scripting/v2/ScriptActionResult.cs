// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Scripting.v2
{
    public class ActionResultSetter<T>
    {
        ActionBase _action;
        T _result;

        public ActionResultSetter(ActionBase action, T result)
        {
            _action = action;
            _result = result;
        }

        public void SetResult(T result)
        {
            _result = result;
            _action.MarkCompleted();
        }
    }

    public class ActionResultSetter
    {
        ActionBase _action;

        public ActionResultSetter(ActionBase action)
        {
            _action = action;
        }

        public void SetResult()
        {
            _action.MarkCompleted();
        }
    }
}
