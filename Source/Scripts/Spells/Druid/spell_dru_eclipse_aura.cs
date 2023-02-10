using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 48517 Eclipse (Solar) + 48518 Eclipse (Lunar)
	internal class spell_dru_eclipse_aura : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.EclipseLunarSpellCnt, DruidSpellIds.EclipseSolarSpellCnt, DruidSpellIds.EclipseDummy);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleRemoved, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
		}

		private void HandleRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			var auraEffDummy = GetTarget().GetAuraEffect(DruidSpellIds.EclipseDummy, 0);

			if (auraEffDummy == null)
				return;

			var spellId = GetSpellInfo().Id == DruidSpellIds.EclipseSolarAura ? DruidSpellIds.EclipseLunarSpellCnt : DruidSpellIds.EclipseSolarSpellCnt;
			spell_dru_eclipse_common.SetSpellCount(GetTarget(), spellId, (uint)auraEffDummy.GetAmount());
		}
	}
}