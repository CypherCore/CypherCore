using System.Collections.Generic;

namespace Game.Scripting.Interfaces.IAura
{
    public interface IHasAuraEffects
    {
        List<IAuraEffectHandler> AuraEffects { get; }
    }
}