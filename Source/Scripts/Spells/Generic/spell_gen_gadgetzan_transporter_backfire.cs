using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_gadgetzan_transporter_backfire : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.TransporterMalfunctionPolymorph, GenericSpellIds.TransporterEviltwin, GenericSpellIds.TransporterMalfunctionMiss);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		var caster = GetCaster();
		var r      = RandomHelper.IRand(0, 119);

		if (r < 20) // Transporter Malfunction - 1/6 polymorph
			caster.CastSpell(caster, GenericSpellIds.TransporterMalfunctionPolymorph, true);
		else if (r < 100) // Evil Twin               - 4/6 evil twin
			caster.CastSpell(caster, GenericSpellIds.TransporterEviltwin, true);
		else // Transporter Malfunction - 1/6 miss the Target
			caster.CastSpell(caster, GenericSpellIds.TransporterMalfunctionMiss, true);
	}
}