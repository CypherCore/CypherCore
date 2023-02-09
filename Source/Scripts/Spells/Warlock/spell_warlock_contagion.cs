using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 233494 - Contagion
	[SpellScript(233494)]
	public class spell_warlock_contagion : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void PeriodicTick(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();
			var target = GetTarget();

			if (caster == null || target == null)
				return;

			var uaspells = new List<uint>()
			               {
				               WarlockSpells.UNSTABLE_AFFLICTION_DOT5,
				               WarlockSpells.UNSTABLE_AFFLICTION_DOT4,
				               WarlockSpells.UNSTABLE_AFFLICTION_DOT3,
				               WarlockSpells.UNSTABLE_AFFLICTION_DOT2,
				               WarlockSpells.UNSTABLE_AFFLICTION_DOT1
			               };

			var hasUa = false;

			foreach (var ua in uaspells)
				if (target.HasAura(ua, caster.GetGUID()))
					hasUa = true;

			if (!hasUa)
				Remove();
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.ModSchoolMaskDamageFromCaster));
		}
	}
}