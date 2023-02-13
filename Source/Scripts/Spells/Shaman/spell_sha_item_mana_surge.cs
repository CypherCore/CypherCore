using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 23572 - Mana Surge
[SpellScript(23572)]
internal class spell_sha_item_mana_surge : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.ItemManaSurge);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetProcSpell() != null;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var costs = eventInfo.GetProcSpell().GetPowerCost();
		var m     = costs.Find(cost => cost.Power == PowerType.Mana);

		if (m != null)
		{
			var mana = MathFunctions.CalculatePct(m.Amount, 35);

			if (mana > 0)
			{
				CastSpellExtraArgs args = new(aurEff);
				args.AddSpellMod(SpellValueMod.BasePoint0, mana);
				GetTarget().CastSpell(GetTarget(), ShamanSpells.ItemManaSurge, args);
			}
		}
	}
}