using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface ICalculateResistAbsorb : ISpellScript
    {
        void CalculateResistAbsorb(DamageInfo damageInfo, ref uint resistAmount, ref int absorbAmount);
    }
}
