using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 70723 - Item - Druid T10 Balance 4P Bonus
	internal class spell_dru_t10_balance_4p_bonus : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.Languish);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}

		private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();

			var damageInfo = eventInfo.GetDamageInfo();

			if (damageInfo == null ||
			    damageInfo.GetDamage() == 0)
				return;

			var caster = eventInfo.GetActor();
			var target = eventInfo.GetProcTarget();

			var spellInfo = Global.SpellMgr.GetSpellInfo(DruidSpellIds.Languish, GetCastDifficulty());
			var amount    = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
			amount /= (int)spellInfo.GetMaxTicks();

			CastSpellExtraArgs args = new(aurEff);
			args.AddSpellMod(SpellValueMod.BasePoint0, amount);
			caster.CastSpell(target, DruidSpellIds.Languish, args);
		}
	}
}