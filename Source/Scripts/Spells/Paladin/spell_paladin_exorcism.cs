using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting.BaseScripts;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin
{
    //383185,
    [SpellScript(383185)]
    public class spell_paladin_exorcism : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
            {
                int damage = (int)player.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
                int dot_damage = (int)(damage * 0.23f * 6);
                int dot_duration = 12;
                GetHitUnit().CastSpell(GetHitUnit(), PaladinSpells.EXORCISM_DF, damage);

                if (GetHitUnit().HasAura(26573))
                {
                    List<Unit> targets = new List<Unit>();
                    AnyUnfriendlyUnitInObjectRangeCheck check = new AnyUnfriendlyUnitInObjectRangeCheck(GetHitUnit(), player, 7);
                    UnitListSearcher searcher = new UnitListSearcher(GetHitUnit(), targets, check);
                    for (List<Unit>.Enumerator i = targets.GetEnumerator(); i.MoveNext();)
                    {
                        GetHitUnit().CastSpell(i.Current, PaladinSpells.EXORCISM_DF, damage);
                    }
                }

                if (GetHitUnit().GetCreatureType() == CreatureType.Undead || GetHitUnit().GetCreatureType() == CreatureType.Demon)
                {
                    GetHitUnit().CastSpell(GetHitUnit(), AuraType.ModStun, true);
                }
            }
        }
    }
}
