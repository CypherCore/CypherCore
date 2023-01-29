// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Conditions;

namespace Game.Scripting.Interfaces.ICondition
{
    public interface IConditionCheck : IScriptObject
    {
        bool OnConditionCheck(Condition condition, ConditionSourceInfo sourceInfo);
    }
}