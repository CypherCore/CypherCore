using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(258920)]
public class spell_dh_immolation_aura : SpellScript, ISpellOnCast
{
	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(DemonHunterSpells.SPELL_DH_CLEANSED_BY_FLAME, DemonHunterSpells.SPELL_DH_CLEANSED_BY_FLAME_DISPEL, DemonHunterSpells.SPELL_DH_FALLOUT, ShatteredSoulsSpells.SPELL_DH_SHATTERED_SOULS_MISSILE);
	}

	public void OnCast()
	{
		var caster = GetCaster();

		if (caster.HasAura(DemonHunterSpells.SPELL_DH_CLEANSED_BY_FLAME))
			caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_CLEANSED_BY_FLAME_DISPEL, true);

		/*
			if (RandomHelper.randChance(40) && caster->HasAura(SPELL_DH_FALLOUT))
			    caster->CastSpell(caster, SPELL_DH_SHATTERED_SOULS_MISSILE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)SPELL_DH_LESSER_SOUL_SHARD));
			*/
	}
}