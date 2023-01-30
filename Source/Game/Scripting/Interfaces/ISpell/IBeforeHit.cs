using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;

namespace Game.Scripting.Interfaces.ISpell
{
    public interface IBeforeHit : ISpellScript
    {
        public void BeforeHit(SpellMissInfo missInfo);
    }
}
