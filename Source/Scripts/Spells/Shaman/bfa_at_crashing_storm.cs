using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IDynamicObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Shaman
{
    // 6826
    [Script]
    public class bfa_at_crashing_storm : AreaTriggerAI
    {
        public bfa_at_crashing_storm(AreaTrigger areatrigger) : base(areatrigger)
        {
        }

        public uint damageTimer;

        public override void OnInitialize()
        {
            damageTimer = 0;
        }

        public override void OnUpdate(uint diff)
        {
            damageTimer += diff;
            if (damageTimer >= 2 * Time.InMilliseconds)
            {
                CheckPlayers();
                damageTimer = 0;
            }
        }

        public void CheckPlayers()
        {
            Unit caster = at.GetCaster();
            if (caster != null)
            {
                float radius = 2.5f;

                List<Unit> targetList = caster.GetPlayerListInGrid(radius);
                if (targetList.Count != 0)
                {
                    foreach (Player player in targetList)
                    {
                        if (!player.IsGameMaster())
                        {
                            caster.CastSpell(player, ShamanSpells.SPELL_CRASHING_STORM_TALENT_DAMAGE, true);
                        }
                    }
                }
            }
        }
    }
}
