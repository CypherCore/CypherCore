using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 34779 - Freezing Circle
internal class spell_freezing_circle : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.FreezingCirclePitOfSaronNormal, GenericSpellIds.FreezingCirclePitOfSaronHeroic, GenericSpellIds.FreezingCircle, GenericSpellIds.FreezingCircleScenario);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDamage(int effIndex)
	{
		var  caster  = GetCaster();
		uint spellId = 0;
		var  map     = caster.GetMap();

		if (map.IsDungeon())
			spellId = map.IsHeroic() ? GenericSpellIds.FreezingCirclePitOfSaronHeroic : GenericSpellIds.FreezingCirclePitOfSaronNormal;
		else
			spellId = map.GetId() == Misc.MapIdBloodInTheSnowScenario ? GenericSpellIds.FreezingCircleScenario : GenericSpellIds.FreezingCircle;

		var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, GetCastDifficulty());

		if (spellInfo != null)
			if (!spellInfo.GetEffects().Empty())
				SetHitDamage(spellInfo.GetEffect(0).CalcValue());
	}
}