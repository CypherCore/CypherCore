using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script("spell_gen_sunreaver_disguise")]
[Script("spell_gen_silver_covenant_disguise")]
internal class spell_gen_dalaran_disguise : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.Id switch
		       {
			       GenericSpellIds.SunreaverTrigger      => ValidateSpellInfo(GenericSpellIds.SunreaverFemale, GenericSpellIds.SunreaverMale),
			       GenericSpellIds.SilverCovenantTrigger => ValidateSpellInfo(GenericSpellIds.SilverCovenantFemale, GenericSpellIds.SilverCovenantMale),
			       _                                     => false
		       };
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		var player = GetHitPlayer();

		if (player)
		{
			var gender = player.GetNativeGender();

			var spellId = GetSpellInfo().Id;

			switch (spellId)
			{
				case GenericSpellIds.SunreaverTrigger:
					spellId = gender == Gender.Female ? GenericSpellIds.SunreaverFemale : GenericSpellIds.SunreaverMale;

					break;
				case GenericSpellIds.SilverCovenantTrigger:
					spellId = gender == Gender.Female ? GenericSpellIds.SilverCovenantFemale : GenericSpellIds.SilverCovenantMale;

					break;
				default:
					break;
			}

			GetCaster().CastSpell(player, spellId, true);
		}
	}
}