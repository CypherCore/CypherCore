// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Scripting.Interfaces.IFormula
{
    public interface IFormulaOnGroupRateCaclulation : IScriptObject
    {
        void OnGroupRateCalculation(float rate, uint count, bool isRaid);
    }
}