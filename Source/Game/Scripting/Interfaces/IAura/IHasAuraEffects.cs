using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Scripting.Interfaces.Aura
{
    public interface IHasAuraEffects
    {
        List<IAuraEffectHandler> Effects { get; }
    }
}
