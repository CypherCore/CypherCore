using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	// 8042 Earth Shock
	[SpellScript(51556)]
	public class spell_sha_earth_shock : SpellScript, IHasSpellEffects, ISpellOnTakePower
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		public void TakePower(SpellPowerCost powerCost)
		{
			_takenPower = powerCost.Amount;
		}

		private void HandleCalcDamage(int UnnamedParameter)
		{
			SetHitDamage(MathFunctions.CalculatePct(GetHitDamage(), _takenPower));
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleCalcDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		}

		private int _takenPower = 0;
	}
}