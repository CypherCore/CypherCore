// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Scripting.v2
{
    public class ActionBase
    {
        bool _isReady;

        public virtual bool IsReady()
        {
            return _isReady;
        }

        public void MarkCompleted()
        {
            _isReady = true;
        }
    }

    public class WaitAction : ActionBase
    {
        DateTime _waitEnd;

        public WaitAction(DateTime waitEnd)
        {
            _waitEnd = waitEnd;
        }

        public override bool IsReady()
        {
            return _waitEnd <= GameTime.Now();
        }
    }

    public class ActionResult<T> : ActionBase
    {
        T _result;

        public static ActionResultSetter<T> GetResultSetter(ActionResult<T> action)
        {
            T resultPtr = action._result;
            return new ActionResultSetter<T>(action, resultPtr);
        }
    }

    public class ActionResult : ActionBase
    {
        public static ActionResultSetter GetResultSetter(ActionResult action)
        {
            return new ActionResultSetter(action);
        }
    }

    public class MultiActionResult<InnerResult> : ActionResult
    {
        public List<ActionResult<InnerResult>> Results = new();

        public ActionResult<InnerResult> CreateAndGetResult()
        {
            ActionResult<InnerResult> result = new();
            Results.Add(result);
            return result;
        }

        public override bool IsReady()
        {
            return Results.All(result => result.IsReady());
        }
    }
}