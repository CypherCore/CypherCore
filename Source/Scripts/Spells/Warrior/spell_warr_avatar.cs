using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
    // Avatar - 107574
    [SpellScript(107574)]
    public class spell_warr_avatar : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

        private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
            {
                player.RemoveMovementImpairingAuras(true);
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real));
        }
    }
}
