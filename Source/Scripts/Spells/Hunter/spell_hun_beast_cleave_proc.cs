using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(118455)]
public class spell_hun_beast_cleave_proc : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		if (!GetCaster())
			return;

		if (eventInfo.GetActor().GetGUID() != GetTarget().GetGUID())
			return;

		if (eventInfo.GetDamageInfo().GetSpellInfo() != null && eventInfo.GetDamageInfo().GetSpellInfo().Id == HunterSpells.SPELL_HUNTER_BEAST_CLEAVE_DAMAGE)
			return;

		var player = GetCaster().ToPlayer();

		if (player != null)
			if (GetTarget().HasAura(aurEff.GetSpellInfo().Id, player.GetGUID()))
			{
				var args = new CastSpellExtraArgs(TriggerCastFlags.FullMask);
				args.AddSpellMod(SpellValueMod.BasePoint0, eventInfo.GetDamageInfo().GetDamage() * 0.75f);

				GetTarget().CastSpell(GetTarget(), HunterSpells.SPELL_HUNTER_BEAST_CLEAVE_DAMAGE, args);
			}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}