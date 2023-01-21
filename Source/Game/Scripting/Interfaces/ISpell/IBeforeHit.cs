using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;

namespace Game.Scripting.Interfaces.Spell
{
    public interface IBeforeHit : ISpellScript
    {
        public void BeforeHit(SpellMissInfo missInfo);
    }
}
