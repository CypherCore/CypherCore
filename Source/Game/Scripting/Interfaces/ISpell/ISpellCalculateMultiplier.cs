// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ISpellCalculateMultiplier : ISpellScript
    {
        /// <summary>
        /// Returned value represents the new modifier that will be used.
        /// Called at the end of the multiplier calc stack.
        /// Multipliers are multiplicitive. For a 10% damage increase do
        /// multiplier *= 1.1 
        /// </summary>
        /// <param name="multiplier"></param>
        /// <returns></returns>
        public double CalcMultiplier(double multiplier);
    }
}