using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(new uint[] { 3409, 8679, 108211 })]
public class spell_rog_poisons_SpellScript : SpellScript, ISpellBeforeHit
{
	private void RemovePreviousPoisons()
	{
		Player plr = GetCaster().ToPlayer();
		if (plr != null)
		{
			if (plr.HasAura(ePoisons.WoundPoison))
			{
				plr.RemoveAura(ePoisons.WoundPoison);
			}
			if (plr.HasAura(ePoisons.MindNumbingPoison))
			{
				plr.RemoveAura(ePoisons.MindNumbingPoison);
			}
			if (plr.HasAura(ePoisons.CripplingPoison))
			{
				plr.RemoveAura(ePoisons.CripplingPoison);
			}
			if (plr.HasAura(ePoisons.LeechingPoison))
			{
				plr.RemoveAura(ePoisons.LeechingPoison);
			}
			if (plr.HasAura(ePoisons.ParalyticPoison))
			{
				plr.RemoveAura(ePoisons.ParalyticPoison);
			}
			if (plr.HasAura(ePoisons.DeadlyPoison))
			{
				plr.RemoveAura(ePoisons.DeadlyPoison);
			}
			if (plr.HasAura(ePoisons.InstantPoison))
			{
				plr.RemoveAura(ePoisons.InstantPoison);
			}
		}
	}

	public void BeforeHit(SpellMissInfo missInfo)
	{
		if (missInfo != SpellMissInfo.None)
		{
			return;
		}

		Player _player = GetCaster().ToPlayer();
		if (_player != null)
		{
			RemovePreviousPoisons();
		}
	}
}