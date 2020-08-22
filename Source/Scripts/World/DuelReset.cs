/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.World
{
    [Script]
    class DuelResetScript : PlayerScript
    {
        public DuelResetScript() : base("DuelResetScript")
        {
            _resetCooldowns = WorldConfig.GetBoolValue(WorldCfg.ResetDuelCooldowns);
            _resetHealthMana = WorldConfig.GetBoolValue(WorldCfg.ResetDuelHealthMana);
        }

        public override void OnDuelStart(Player player1, Player player2)
        {
            // Cooldowns reset
            if (_resetCooldowns)
            {
                player1.GetSpellHistory().SaveCooldownStateBeforeDuel();
                player2.GetSpellHistory().SaveCooldownStateBeforeDuel();

                ResetSpellCooldowns(player1, true);
                ResetSpellCooldowns(player2, true);
            }

            // Health and mana reset
            if (_resetHealthMana)
            {
                player1.SaveHealthBeforeDuel();
                player1.SaveManaBeforeDuel();
                player1.ResetAllPowers();

                player2.SaveHealthBeforeDuel();
                player2.SaveManaBeforeDuel();
                player2.ResetAllPowers();
            }
        }

        public override void OnDuelEnd(Player winner, Player loser, DuelCompleteType type)
        {
            // do not reset anything if DUEL_INTERRUPTED or DUEL_FLED
            if (type == DuelCompleteType.Won)
            {
                // Cooldown restore
                if (_resetCooldowns)
                {
                    ResetSpellCooldowns(winner, false);
                    ResetSpellCooldowns(loser, false);

                    winner.GetSpellHistory().RestoreCooldownStateAfterDuel();
                    loser.GetSpellHistory().RestoreCooldownStateAfterDuel();
                }

                // Health and mana restore
                if (_resetHealthMana)
                {
                    winner.RestoreHealthAfterDuel();
                    loser.RestoreHealthAfterDuel();

                    // check if player1 class uses mana
                    if (winner.GetPowerType() == PowerType.Mana || winner.GetClass() == Class.Druid)
                        winner.RestoreManaAfterDuel();

                    // check if player2 class uses mana
                    if (loser.GetPowerType() == PowerType.Mana || loser.GetClass() == Class.Druid)
                        loser.RestoreManaAfterDuel();
                }
            }
        }

        void ResetSpellCooldowns(Player player, bool onStartDuel)
        {
            // remove cooldowns on spells that have < 10 min CD > 30 sec and has no onHold
            player.GetSpellHistory().ResetCooldowns(itr =>
            {
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(itr.Key, Difficulty.None);
                uint remainingCooldown = player.GetSpellHistory().GetRemainingCooldown(spellInfo);
                uint totalCooldown = spellInfo.RecoveryTime;
                uint categoryCooldown = spellInfo.CategoryRecoveryTime;

                player.ApplySpellMod(spellInfo.Id, SpellModOp.Cooldown, ref totalCooldown, null);
                int cooldownMod = player.GetTotalAuraModifier(AuraType.ModCooldown);
                if (cooldownMod != 0)
                    totalCooldown += (uint)(cooldownMod * Time.InMilliseconds);

                if (!spellInfo.HasAttribute(SpellAttr6.IgnoreCategoryCooldownMods))
                    player.ApplySpellMod(spellInfo.Id, SpellModOp.Cooldown, ref categoryCooldown, null);

                return remainingCooldown > 0
                        && !itr.Value.OnHold
                        && TimeSpan.FromMilliseconds(totalCooldown) < TimeSpan.FromMinutes(10)
                        && TimeSpan.FromMilliseconds(categoryCooldown) < TimeSpan.FromMinutes(10)
                        && TimeSpan.FromMilliseconds(remainingCooldown) < TimeSpan.FromMinutes(10)
                        && (onStartDuel ? TimeSpan.FromMilliseconds(totalCooldown - remainingCooldown) > TimeSpan.FromSeconds(30) : true)
                        && (onStartDuel ? TimeSpan.FromMilliseconds(categoryCooldown - remainingCooldown) > TimeSpan.FromSeconds(30) : true);
            }, true);

            // pet cooldowns
            Pet pet = player.GetPet();
            if (pet)
                pet.GetSpellHistory().ResetAllCooldowns();
        }

        bool _resetCooldowns;
        bool _resetHealthMana;
    }
}
