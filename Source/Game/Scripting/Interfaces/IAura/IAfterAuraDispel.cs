using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IAfterAuraDispel : IAuraScript
    {
        void HandleDispel(DispelInfo dispelInfo);
    }
}
