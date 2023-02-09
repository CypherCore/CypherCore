using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	//202192 - Resonance totem
	[SpellScript(202192)]
	public class spell_sha_resonance_effect : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		private void HandlePeriodic(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			if (caster.GetOwner())
				caster.GetOwner().ModifyPower(PowerType.Maelstrom, +1);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicEnergize));
		}
	}
}