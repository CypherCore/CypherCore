using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 187874 - Crash Lightning
internal class spell_sha_crash_lightning : SpellScript, ISpellAfterCast, IHasSpellEffects
{
	private int _targetsHit;

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.CrashLightningCleave);
	}

	public void AfterCast()
	{
		if (_targetsHit >= 2)
			GetCaster().CastSpell(GetCaster(), ShamanSpells.CrashLightningCleave, true);

		var gatheringStorms = GetCaster().GetAuraEffect(ShamanSpells.GatheringStorms, 0);

		if (gatheringStorms != null)
		{
			CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
			args.AddSpellMod(SpellValueMod.BasePoint0, (int)(gatheringStorms.GetAmount() * _targetsHit));
			GetCaster().CastSpell(GetCaster(), ShamanSpells.GatheringStormsBuff, args);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitConeCasterToDestEnemy));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void CountTargets(List<WorldObject> targets)
	{
		_targetsHit = targets.Count;
	}
}