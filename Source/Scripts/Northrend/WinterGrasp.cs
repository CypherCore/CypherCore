/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.BattleFields;
using Game.Conditions;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Northrend
{
    [Script]
    class spell_wintergrasp_defender_teleport : SpellScript
    {
        SpellCastResult CheckCast()
        {
            BattleField wg = Global.BattleFieldMgr.GetBattlefieldByBattleId(1);
            if (wg != null)
            {
                Player target = GetExplTargetUnit().ToPlayer();
                if (target)
                    // check if we are in Wintergrasp at all, SotA uses same teleport spells
                    if ((target.GetZoneId() == 4197 && target.GetTeamId() != wg.GetDefenderTeam()) || target.HasAura(54643))
                        return SpellCastResult.BadTargets;
            }

            return SpellCastResult.SpellCastOk;
        }

        public override void Register()
        {
            OnCheckCast.Add(new CheckCastHandler(CheckCast));
        }
    }

    [Script]
    class spell_wintergrasp_defender_teleport_trigger : SpellScript
    {
        void HandleDummy(uint effindex)
        {
            Unit target = GetHitUnit();
            if (target)
            {
                WorldLocation loc = target.GetWorldLocation();
                SetExplTargetDest(loc);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class condition_is_wintergrasp_horde : ConditionScript
    {
        public condition_is_wintergrasp_horde() : base("condition_is_wintergrasp_horde") { }

        public override bool OnConditionCheck(Condition condition, ConditionSourceInfo sourceInfo)
        {
            BattleField wintergrasp = Global.BattleFieldMgr.GetBattlefieldByBattleId(BattlefieldIds.WG);
            if (wintergrasp.IsEnabled() && wintergrasp.GetDefenderTeam() == TeamId.Horde)
                return true;
            return false;
        }
    }

    [Script]
    class condition_is_wintergrasp_alliance : ConditionScript
    {
        public condition_is_wintergrasp_alliance() : base("condition_is_wintergrasp_alliance") { }

        public override bool OnConditionCheck(Condition condition, ConditionSourceInfo sourceInfo)
        {
            BattleField wintergrasp = Global.BattleFieldMgr.GetBattlefieldByBattleId(BattlefieldIds.WG);
            if (wintergrasp.IsEnabled() && wintergrasp.GetDefenderTeam() == TeamId.Alliance)
                return true;
            return false;
        }
    }
}
