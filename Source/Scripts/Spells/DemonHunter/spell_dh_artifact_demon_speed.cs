using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(201469)]
public class spell_dh_artifact_demon_speed : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var aurEff = caster.GetAuraEffect(DemonHunterSpells.SPELL_DH_DEMON_SPEED, 0);

		if (aurEff != null)
			for (byte i = 0; i < aurEff.GetAmount(); ++i)
				caster.GetSpellHistory().RestoreCharge(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_FEL_RUSH, Difficulty.None).ChargeCategoryId);
	}
}