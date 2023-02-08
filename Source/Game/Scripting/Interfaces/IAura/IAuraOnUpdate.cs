using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Spells;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraOnUpdate : IAuraScript
    {
        void AuraOnUpdate(uint diff);
    }
}
