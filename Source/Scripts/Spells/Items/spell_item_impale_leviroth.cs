using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_impale_leviroth : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		if (Global.ObjectMgr.GetCreatureTemplate(CreatureIds.Leviroth) == null)
			return false;

		return true;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		Creature target = GetHitCreature();

		if (target)
			if (target.GetEntry() == CreatureIds.Leviroth &&
			    !target.HealthBelowPct(95))
			{
				target.CastSpell(target, ItemSpellIds.LevirothSelfImpale, true);
				target.ResetPlayerDamageReq();
			}
	}
}