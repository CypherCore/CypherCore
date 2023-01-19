using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;

namespace Game.Scripting.Interfaces.Spell
{
    public interface ICheckCastHander : ISpellScript
    {
        SpellCastResult CheckCast();
    }
}
