using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Scripting.Interfaces.Spell
{
    public interface IBeforeCast : ISpellScript
    {
        public void BeforeCast();
    }
}
