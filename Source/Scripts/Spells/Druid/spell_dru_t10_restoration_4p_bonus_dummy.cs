using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid
{
	[Script] // 70664 - Druid T10 Restoration 4P Bonus (Rejuvenation)
	internal class spell_dru_t10_restoration_4p_bonus_dummy : AuraScript, IAuraCheckProc, IHasAuraEffects
	{
		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(DruidSpellIds.RejuvenationT10Proc);
		}

		public bool CheckProc(ProcEventInfo eventInfo)
		{
			var spellInfo = eventInfo.GetSpellInfo();

			if (spellInfo == null ||
			    spellInfo.Id == DruidSpellIds.RejuvenationT10Proc)
				return false;

			var healInfo = eventInfo.GetHealInfo();

			if (healInfo == null ||
			    healInfo.GetHeal() == 0)
				return false;

			var caster = eventInfo.GetActor().ToPlayer();

			if (!caster)
				return false;

			return caster.GetGroup() || caster != eventInfo.GetProcTarget();
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		}

		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();

			var                amount = (int)eventInfo.GetHealInfo().GetHeal();
			CastSpellExtraArgs args   = new(aurEff);
			args.AddSpellMod(SpellValueMod.BasePoint0, (int)eventInfo.GetHealInfo().GetHeal());
			eventInfo.GetActor().CastSpell((Unit)null, DruidSpellIds.RejuvenationT10Proc, args);
		}
	}
}