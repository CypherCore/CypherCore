using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAuraOnProc : IAuraScript
    {
        void OnProc(ProcEventInfo info);
    }
}
