using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 70817 - Item - Shaman T10 Elemental 4P Bonus
internal class spell_sha_t10_elemental_4p_bonus : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		Unit caster = eventInfo.GetActor();
		Unit target = eventInfo.GetProcTarget();

		// try to find spell Flame Shock on the Target
		AuraEffect flameShock = target.GetAuraEffect(AuraType.PeriodicDamage, SpellFamilyNames.Shaman, new FlagArray128(0x10000000), caster.GetGUID());

		if (flameShock == null)
			return;

		Aura flameShockAura = flameShock.GetBase();

		int maxDuration = flameShockAura.GetMaxDuration();
		int newDuration = flameShockAura.GetDuration() + aurEff.GetAmount() * Time.InMilliseconds;

		flameShockAura.SetDuration(newDuration);

		// is it blizzlike to change max duration for FS?
		if (newDuration > maxDuration)
			flameShockAura.SetMaxDuration(newDuration);
	}
}