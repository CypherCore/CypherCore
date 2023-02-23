using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraOnRemove : IAuraScript
    {
        void AuraRemoved();
    }
}
