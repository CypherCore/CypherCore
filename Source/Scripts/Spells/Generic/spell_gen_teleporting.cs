using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_teleporting : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		Unit target = GetHitUnit();

		if (!target.IsPlayer())
			return;

		// return from top
		if (target.ToPlayer().GetAreaId() == Misc.AreaVioletCitadelSpire)
			target.CastSpell(target, GenericSpellIds.TeleportSpireDown, true);
		// teleport atop
		else
			target.CastSpell(target, GenericSpellIds.TeleportSpireUp, true);
	}
}