using System.Collections.Generic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
    //214871 - Odyn's fury
    [SpellScript(214871)]
    internal class spell_warr_odyns_fury : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void Absorb(AuraEffect UnnamedParameter, DamageInfo UnnamedParameter2, ref uint absorbAmount)
        {
            absorbAmount = 0;
        }

        public override void Register()
        {
            AuraEffects.Add(new EffectAbsorbHandler(Absorb, 0));
        }
    }
}
