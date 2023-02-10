using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	[SpellScript(108359)]
	public class spell_warl_dark_regeneration : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			if (GetTarget())
			{
				var pet = GetTarget().GetGuardianPet();

				if (pet != null)
					pet.CastSpell(pet, WarlockSpells.DARK_REGENERATION, true);
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.ObsModHealth, AuraEffectHandleModes.Real));
		}
	}
}