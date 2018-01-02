/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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
                player1.SetHealth(player1.GetMaxHealth());

                player2.SaveHealthBeforeDuel();
                player2.SetHealth(player2.GetMaxHealth());

                // check if player1 class uses mana
                if (player1.getPowerType() == PowerType.Mana || player1.GetClass() == Class.Druid)
                {
                    player1.SaveManaBeforeDuel();
                    player1.SetPower(PowerType.Mana, player1.GetMaxPower(PowerType.Mana));
                }

                // check if player2 class uses mana
                if (player2.getPowerType() == PowerType.Mana || player2.GetClass() == Class.Druid)
                {
                    player2.SaveManaBeforeDuel();
                    player2.SetPower(PowerType.Mana, player2.GetMaxPower(PowerType.Mana));
                }
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
                    if (winner.getPowerType() == PowerType.Mana || winner.GetClass() == Class.Druid)
                        winner.RestoreManaAfterDuel();

                    // check if player2 class uses mana
                    if (loser.getPowerType() == PowerType.Mana || loser.GetClass() == Class.Druid)
                        loser.RestoreManaAfterDuel();
                }
            }
        }

        void ResetSpellCooldowns(Player player, bool onStartDuel)
        {
            if (onStartDuel)
            {
                // remove cooldowns on spells that have < 10 min CD > 30 sec and has no onHold
                player.GetSpellHistory().ResetCooldowns(pair =>
                {
                    DateTime now = DateTime.Now;
                    uint cooldownDuration = pair.Value.CooldownEnd > now ? (uint)(pair.Value.CooldownEnd - now).TotalMilliseconds : 0;
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(pair.Key);
                    return spellInfo.RecoveryTime < 10 * Time.Minute * Time.InMilliseconds
                           && spellInfo.CategoryRecoveryTime < 10 * Time.Minute * Time.InMilliseconds
                           && !pair.Value.OnHold
                           && cooldownDuration > 0
                           && (spellInfo.RecoveryTime - cooldownDuration) > (Time.Minute / 2) * Time.InMilliseconds
                           && (spellInfo.CategoryRecoveryTime - cooldownDuration) > (Time.Minute / 2) * Time.InMilliseconds;
                }, true);
            }
            else
            {
                // remove cooldowns on spells that have < 10 min CD and has no onHold
                player.GetSpellHistory().ResetCooldowns(pair =>
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(pair.Key);
                    return spellInfo.RecoveryTime < 10 * Time.Minute * Time.InMilliseconds
                           && spellInfo.CategoryRecoveryTime < 10 * Time.Minute * Time.InMilliseconds
                           && !pair.Value.OnHold;
                }, true);
            }

            // pet cooldowns
            Pet pet = player.GetPet();
            if (pet)
                pet.GetSpellHistory().ResetAllCooldowns();
        }

        bool _resetCooldowns;
        bool _resetHealthMana;
    }
}
