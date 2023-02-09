using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_seaforium_blast : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.PlantChargesCreditAchievement);
	}

	public override bool Load()
	{
		// OriginalCaster is always available in Spell.prepare
		return GetGObjCaster().GetOwnerGUID().IsPlayer();
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(AchievementCredit, 1, SpellEffectName.GameObjectDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void AchievementCredit(int effIndex)
	{
		// but in effect handling OriginalCaster can become null
		var owner = GetGObjCaster().GetOwner();

		if (owner != null)
		{
			var go = GetHitGObj();

			if (go)
				if (go.GetGoInfo().type == GameObjectTypes.DestructibleBuilding)
					owner.CastSpell(null, GenericSpellIds.PlantChargesCreditAchievement, true);
		}
	}
}