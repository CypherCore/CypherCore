using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraEnterLeaveCombat : IAuraScript
    {
        void EnterLeaveCombat(bool isNowInCombat);
    }
}
