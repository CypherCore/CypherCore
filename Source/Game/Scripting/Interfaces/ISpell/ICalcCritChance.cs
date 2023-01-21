using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ICalcCritChance : ISpellScript
    {
        void CalcCritChance(Unit victim, ref float chance);
    }
}
