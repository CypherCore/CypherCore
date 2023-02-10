using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DemonHunter;

[Script]
public class at_dh_artifact_fury_of_the_illidari : AreaTriggerAI
{
	public at_dh_artifact_fury_of_the_illidari(AreaTrigger areatrigger) : base(areatrigger)
	{
	}

	//   void OnInitialize() override
	//  {
	//     at->VariableStorage.Set<int32>("_timer", 500);
	//  }

	public override void OnUpdate(uint diff)
	{
		var caster = at.GetCaster();

		if (caster == null)
			return;

		//  int32 timer = at->VariableStorage.GetValue<int32>("_timer") + diff;
		/* if (timer >= 490)
			 {
			     at->VariableStorage.Set<int32>("_timer", timer - 490);
			     caster->CastSpell(at, SPELL_DH_FURY_OF_THE_ILLIDARI_MAINHAND, true);
			     caster->CastSpell(at, SPELL_DH_FURY_OF_THE_ILLIDARI_OFFHAND, true);
			 }
			 else
			     at->VariableStorage.Set("_timer", timer);*/
	}

	public override void OnRemove()
	{
		var caster = at.GetCaster();

		if (caster == null || !caster.ToPlayer())
			return;

		//   int32 rageOfTheIllidari = caster->VariableStorage.GetValue<int32>("Spells.RageOfTheIllidariDamage");
		// if (!rageOfTheIllidari)
		//     return;

		// caster->VariableStorage.Set<int32>("Spells.RageOfTheIllidariDamage", 0);

		// Cannot cast custom spell on position...
		var target = caster.SummonCreature(SharedConst.WorldTrigger, at, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(1));

		if (target != null)
			caster.CastSpell(at, DemonHunterSpells.SPELL_DH_RAGE_OF_THE_ILLIDARI_VISUAL, true);
		//  caster->m_Events.AddEventAtOffset(() =>
		// {
		//caster->CastCustomSpell(SPELL_DH_RAGE_OF_THE_ILLIDARI_DAMAGE, SpellValueMod.BasePoint0, rageOfTheIllidari, target, TriggerCastFlags.FullMask);
		//}, TimeSpan.FromMilliseconds(750), [caster, target);
	}
}