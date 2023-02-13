using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 77762 - Lava Surge
[SpellScript(77762)]
internal class spell_sha_lava_surge_proc : SpellScript, ISpellAfterHit
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.LavaBurst);
	}

	public override bool Load()
	{
		return GetCaster().IsTypeId(TypeId.Player);
	}

	public void AfterHit()
	{
		GetCaster().GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(ShamanSpells.LavaBurst, GetCastDifficulty()).ChargeCategoryId);
	}
}