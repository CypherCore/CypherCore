using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script("spell_item_deathbringers_will_normal", ItemSpellIds.StrengthOfTheTaunka, ItemSpellIds.AgilityOfTheVrykul, ItemSpellIds.PowerOfTheTaunka, ItemSpellIds.AimOfTheIronDwarves, ItemSpellIds.SpeedOfTheVrykul)]
[Script("spell_item_deathbringers_will_heroic", ItemSpellIds.StrengthOfTheTaunkaHero, ItemSpellIds.AgilityOfTheVrykulHero, ItemSpellIds.PowerOfTheTaunkaHero, ItemSpellIds.AimOfTheIronDwarvesHero, ItemSpellIds.SpeedOfTheVrykulHero)]
internal class spell_item_deathbringers_will : AuraScript, IHasAuraEffects
{
	private readonly uint _agilitySpellId;
	private readonly uint _apSpellId;
	private readonly uint _criticalSpellId;
	private readonly uint _hasteSpellId;

	private readonly uint _strengthSpellId;

	public spell_item_deathbringers_will(uint strengthSpellId, uint agilitySpellId, uint apSpellId, uint criticalSpellId, uint hasteSpellId)
	{
		_strengthSpellId = strengthSpellId;
		_agilitySpellId  = agilitySpellId;
		_apSpellId       = apSpellId;
		_criticalSpellId = criticalSpellId;
		_hasteSpellId    = hasteSpellId;
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(_strengthSpellId, _agilitySpellId, _apSpellId, _criticalSpellId, _hasteSpellId);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		uint[][] triggeredSpells =
		{
			//CLASS_NONE
			Array.Empty<uint>(),
			//CLASS_WARRIOR
			new uint[]
			{
				_strengthSpellId, _criticalSpellId, _hasteSpellId
			},
			//CLASS_PALADIN
			new uint[]
			{
				_strengthSpellId, _criticalSpellId, _hasteSpellId
			},
			//CLASS_HUNTER
			new uint[]
			{
				_agilitySpellId, _criticalSpellId, _apSpellId
			},
			//CLASS_ROGUE
			new uint[]
			{
				_agilitySpellId, _hasteSpellId, _apSpellId
			},
			//CLASS_PRIEST
			Array.Empty<uint>(),
			//CLASS_DEATH_KNIGHT
			new uint[]
			{
				_strengthSpellId, _criticalSpellId, _hasteSpellId
			},
			//CLASS_SHAMAN
			new uint[]
			{
				_agilitySpellId, _hasteSpellId, _apSpellId
			},
			//CLASS_MAGE
			Array.Empty<uint>(),
			//CLASS_WARLOCK
			Array.Empty<uint>(),
			//CLASS_UNK
			Array.Empty<uint>(),
			//CLASS_DRUID
			new uint[]
			{
				_strengthSpellId, _agilitySpellId, _hasteSpellId
			}
		};

		PreventDefaultAction();
		var caster       = eventInfo.GetActor();
		var randomSpells = triggeredSpells[(int)caster.GetClass()];

		if (randomSpells.Empty())
			return;

		var spellId = randomSpells.SelectRandom();
		caster.CastSpell(caster, spellId, new CastSpellExtraArgs(aurEff));
	}
}