using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_cannon_blast : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.CannonBlast);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		var                bp     = GetEffectValue();
		var                target = GetHitUnit();
		CastSpellExtraArgs args   = new(TriggerCastFlags.FullMask);
		args.AddSpellMod(SpellValueMod.BasePoint0, bp);
		target.CastSpell(target, GenericSpellIds.CannonBlastDamage, args);
	}
}