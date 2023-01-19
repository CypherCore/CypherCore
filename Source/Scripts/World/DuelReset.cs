// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.World.DuelReset
{
    [Script]
    class DuelResetScript : PlayerScript
    {
        bool _resetCooldowns;
        bool _resetHealthMana;

        public DuelResetScript() : base("DuelResetScript")
        {
            _resetCooldowns = WorldConfig.GetBoolValue(WorldCfg.ResetDuelCooldowns);
            _resetHealthMana = WorldConfig.GetBoolValue(WorldCfg.ResetDuelHealthMana);
        }

        // Called when a duel starts (after 3s countdown)
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

        // Called when a duel ends
        public override void OnDuelEnd(Player winner, Player loser, DuelCompleteType type)
        {
            // do not reset anything if DuelInterrupted or DuelFled
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

        static void ResetSpellCooldowns(Player player, bool onStartDuel)
        {
            // remove cooldowns on spells that have < 10 min Cd > 30 sec and has no onHold
            player.GetSpellHistory().ResetCooldowns(pair =>
            {
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(pair.Key, Difficulty.None);
                TimeSpan remainingCooldown = player.GetSpellHistory().GetRemainingCooldown(spellInfo);
                TimeSpan totalCooldown = TimeSpan.FromMilliseconds(spellInfo.RecoveryTime);
                TimeSpan categoryCooldown = TimeSpan.FromMilliseconds(spellInfo.CategoryRecoveryTime);

                void applySpellMod(ref TimeSpan value)
                {
                    int intValue = (int)value.TotalMilliseconds;
                    player.ApplySpellMod(spellInfo, SpellModOp.Cooldown, ref intValue, null);
                    value = TimeSpan.FromMilliseconds(intValue);
                };

                applySpellMod(ref totalCooldown);

                int cooldownMod = player.GetTotalAuraModifier(AuraType.ModCooldown);
                if (cooldownMod != 0)
                    totalCooldown += TimeSpan.FromMilliseconds(cooldownMod);

                if (!spellInfo.HasAttribute(SpellAttr6.NoCategoryCooldownMods))
                    applySpellMod(ref categoryCooldown);

                return remainingCooldown > TimeSpan.Zero
                    && !pair.Value.OnHold
                    && totalCooldown < TimeSpan.FromMinutes(10)
                    && categoryCooldown < TimeSpan.FromMinutes(10)
                    && remainingCooldown < TimeSpan.FromMinutes(10)
                    && (onStartDuel ? totalCooldown - remainingCooldown > TimeSpan.FromSeconds(30) : true)
                    && (onStartDuel ? categoryCooldown - remainingCooldown > TimeSpan.FromSeconds(30) : true);
            }, true);

            // pet cooldowns
            Pet pet = player.GetPet();
            if (pet)
                pet.GetSpellHistory().ResetAllCooldowns();
        }
    }
}