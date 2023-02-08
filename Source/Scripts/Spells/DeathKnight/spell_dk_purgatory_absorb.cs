using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(114556)]
public class spell_dk_purgatory_absorb : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		amount = -1;
	}

	private void Absorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, ref uint absorbAmount)
	{
		Unit target = GetTarget();

		if (dmgInfo.GetDamage() < target.GetHealth())
		{
			return;
		}

		// No damage received under Shroud of Purgatory
		if (target.ToPlayer().HasAura(DeathKnightSpells.SPELL_DK_SHROUD_OF_PURGATORY))
		{
			absorbAmount = dmgInfo.GetDamage();
			return;
		}

		if (target.ToPlayer().HasAura(DeathKnightSpells.SPELL_DK_PERDITION))
		{
			return;
		}

		float              bp   = dmgInfo.GetDamage();
		CastSpellExtraArgs args = new CastSpellExtraArgs();
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)bp);
		args.SetTriggerFlags(TriggerCastFlags.FullMask);
		target.CastSpell(target, DeathKnightSpells.SPELL_DK_SHROUD_OF_PURGATORY, args);
		target.CastSpell(target, DeathKnightSpells.SPELL_DK_PERDITION, TriggerCastFlags.FullMask);
		target.SetHealth(1);
		absorbAmount = dmgInfo.GetDamage();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
	}
}