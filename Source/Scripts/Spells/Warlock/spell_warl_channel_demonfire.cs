using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// Channel Demonfire - 196447
	[SpellScript(196447)]
	public class spell_warl_channel_demonfire : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void HandlePeriodic(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var enemies  = new List<Unit>();
			var check    = new AnyUnfriendlyUnitInObjectRangeCheck(caster, caster, 100.0f);
			var searcher = new UnitListSearcher(caster, enemies, check);
			Cell.VisitAllObjects(caster, searcher, 100.0f);
			enemies.RemoveIf(new UnitAuraCheck<Unit>(false, WarlockSpells.IMMOLATE_DOT, caster.GetGUID()));

			if (enemies.Count == 0)
				return;

			var target = enemies.SelectRandom();
			caster.CastSpell(target, WarlockSpells.CHANNEL_DEMONFIRE_DAMAGE, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
		}
	}
}