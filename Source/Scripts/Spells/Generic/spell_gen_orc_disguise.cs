using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_orc_disguise : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.OrcDisguiseTrigger, GenericSpellIds.OrcDisguiseMale, GenericSpellIds.OrcDisguiseFemale);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		var caster = GetCaster();
		var target = GetHitPlayer();

		if (target)
		{
			var gender = target.GetNativeGender();

			if (gender == Gender.Male)
				caster.CastSpell(target, GenericSpellIds.OrcDisguiseMale, true);
			else
				caster.CastSpell(target, GenericSpellIds.OrcDisguiseFemale, true);
		}
	}
}