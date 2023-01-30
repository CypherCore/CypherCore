// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.BattleFields;
using Game.BattleGrounds;

namespace Game.Entities
{
    public class SpellArea
    {
        public uint AreaId { get; set; }           // zone/subzone/or 0 is not limited to zone
        public int AuraSpell { get; set; }        // spell aura must be applied for spell apply)if possitive) and it must not be applied in other case
        public SpellAreaFlag Flags { get; set; }   // if SPELL_AREA_FLAG_AUTOCAST then auto applied at area enter, in other case just allowed to cast || if SPELL_AREA_FLAG_AUTOREMOVE then auto removed inside area (will allways be removed on leaved even without flag)
        public Gender Gender { get; set; }         // can be applied only to Gender
        public uint QuestEnd { get; set; }         // quest end (quest must not be rewarded for spell apply)
        public uint QuestEndStatus { get; set; }   // QuestStatus that the quest_end must have in order to keep the spell (if the quest_end's status is different than this, the spell will be dropped)
        public uint QuestStart { get; set; }       // quest start (quest must be active or rewarded for spell apply)
        public uint QuestStartStatus { get; set; } // QuestStatus that quest_start must have in order to keep the spell
        public ulong RaceMask { get; set; }        // can be applied only to races
        public uint SpellId { get; set; }

        // helpers
        public bool IsFitToRequirements(Player player, uint newZone, uint newArea)
        {
            if (Gender != Gender.None) // not in expected Gender
                if (player == null ||
                    Gender != player.GetNativeGender())
                    return false;

            if (RaceMask != 0) // not in expected race
                if (player == null ||
                    !Convert.ToBoolean(RaceMask & (ulong)SharedConst.GetMaskForRace(player.GetRace())))
                    return false;

            if (AreaId != 0) // not in expected zone
                if (newZone != AreaId &&
                    newArea != AreaId)
                    return false;

            if (QuestStart != 0) // not in expected required quest State
                if (player == null ||
                    (((1 << (int)player.GetQuestStatus(QuestStart)) & QuestStartStatus) == 0))
                    return false;

            if (QuestEnd != 0) // not in expected forbidden quest State
                if (player == null ||
                    (((1 << (int)player.GetQuestStatus(QuestEnd)) & QuestEndStatus) == 0))
                    return false;

            if (AuraSpell != 0) // not have expected aura
                if (player == null ||
                    (AuraSpell > 0 && !player.HasAura((uint)AuraSpell)) ||
                    (AuraSpell < 0 && player.HasAura((uint)-AuraSpell)))
                    return false;

            if (player)
            {
                Battleground bg = player.GetBattleground();

                if (bg)
                    return bg.IsSpellAllowed(SpellId, player);
            }

            // Extra conditions -- leaving the possibility add extra conditions...
            switch (SpellId)
            {
                case 91604: // No fly Zone - Wintergrasp
                    {
                        if (!player)
                            return false;

                        BattleField Bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.GetMap(), player.GetZoneId());

                        if (Bf == null ||
                            Bf.CanFlyIn() ||
                            (!player.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed) && !player.HasAuraType(AuraType.Fly)))
                            return false;

                        break;
                    }
                case 56618: // Horde Controls Factory Phase Shift
                case 56617: // Alliance Controls Factory Phase Shift
                    {
                        if (!player)
                            return false;

                        BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.GetMap(), player.GetZoneId());

                        if (bf == null ||
                            bf.GetTypeId() != (int)BattleFieldTypes.WinterGrasp)
                            return false;

                        // team that controls the workshop in the specified area
                        uint team = bf.GetData(newArea);

                        if (team == TeamId.Horde)
                            return SpellId == 56618;
                        else if (team == TeamId.Alliance)
                            return SpellId == 56617;

                        break;
                    }
                case 57940: // Essence of Wintergrasp - Northrend
                case 58045: // Essence of Wintergrasp - Wintergrasp
                    {
                        if (!player)
                            return false;

                        BattleField battlefieldWG = Global.BattleFieldMgr.GetBattlefieldByBattleId(player.GetMap(), 1);

                        if (battlefieldWG != null)
                            return battlefieldWG.IsEnabled() && (player.GetTeamId() == battlefieldWG.GetDefenderTeam()) && !battlefieldWG.IsWarTime();

                        break;
                    }
                case 74411: // Battleground- Dampening
                    {
                        if (!player)
                            return false;

                        BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.GetMap(), player.GetZoneId());

                        if (bf != null)
                            return bf.IsWarTime();

                        break;
                    }
            }

            return true;
        }
    }
}