using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    // Spirit link
    [SpellScript(98021)]
    public class spell_sha_spirit_link : SpellScript, ISpellOnHit
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Load()
        {
            averagePercentage = 0.0f;
            return true;
        }

        private void FilterTargets(List<WorldObject> unitList)
        {
            uint targetCount = 0;
            for (List<WorldObject>.Enumerator itr = unitList.GetEnumerator(); itr.MoveNext();)
            {
                Unit target = itr.Current.ToUnit();
                if (target != null)
                {
                    targets[target.GetGUID()] = target.GetHealthPct();
                    averagePercentage += target.GetHealthPct();
                    ++targetCount;
                }
            }

            averagePercentage /= targetCount;
        }

        public void OnHit()
        {
            Unit target = GetHitUnit();
            if (target != null)
            {
                if (!targets.ContainsKey(target.GetGUID()))
                {
                    return;
                }

                float bp0 = 0.0f;
                float bp1 = 0.0f;
                float percentage = targets[target.GetGUID()];
                ulong currentHp = target.CountPctFromMaxHealth((int)percentage);
                ulong desiredHp = target.CountPctFromMaxHealth((int)averagePercentage);

                if (desiredHp > currentHp)
                    bp1 = desiredHp - currentHp;
                else
                    bp0 = currentHp - desiredHp;

                CastSpellExtraArgs args = new CastSpellExtraArgs();
                GetCaster().CastSpell(target, 98021, new CastSpellExtraArgs(TriggerCastFlags.None)
                    .AddSpellMod(SpellValueMod.BasePoint0, (int)bp0)
                    .AddSpellMod(SpellValueMod.BasePoint1, (int)bp1));
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitCasterAreaRaid));
        }

        private SortedDictionary<ObjectGuid, float> targets = new SortedDictionary<ObjectGuid, float>();
        private float averagePercentage;
    }
}
