using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[Script]
public class at_demon_hunter_mana_rift : AreaTriggerAI
{
	public at_demon_hunter_mana_rift(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	public override void OnUnitExit(Unit unit)
	{
		var caster = at.GetCaster();

		if (caster == null || unit == null)
			return;

		var spellProto = Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SPELL_DH_MANA_RIFT_SPELL, Difficulty.None);

		if (spellProto == null)
			return;

		if (at.IsRemoved())
			if (caster.IsValidAttackTarget(unit))
			{
				var hpBp   = unit.CountPctFromMaxHealth(spellProto.GetEffect(1).BasePoints);
				var manaBp = unit.CountPctFromMaxPower(PowerType.Mana, spellProto.GetEffect(2).BasePoints);
				var args   = new CastSpellExtraArgs();
				args.AddSpellMod(SpellValueMod.BasePoint0, hpBp);
				args.AddSpellMod(SpellValueMod.BasePoint0, manaBp);
				args.SetTriggerFlags(TriggerCastFlags.FullMask);
				caster.CastSpell(unit, DemonHunterSpells.SPELL_DH_MANA_RIFT_DAMAGE, args);
			}
	}
}