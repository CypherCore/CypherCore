// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;

namespace Game
{
    public class QuestObjective
    {
        public int Amount { get; set; }
        public string Description;
        public QuestObjectiveFlags Flags { get; set; }
        public uint Flags2 { get; set; }
        public uint Id { get; set; }
        public int ObjectID { get; set; }
        public float ProgressBarWeight { get; set; }
        public uint QuestID { get; set; }
        public sbyte StorageIndex { get; set; }
        public QuestObjectiveType Type { get; set; }
        public int[] VisualEffects = Array.Empty<int>();

        public bool IsStoringValue()
        {
            switch (Type)
            {
                case QuestObjectiveType.Monster:
                case QuestObjectiveType.Item:
                case QuestObjectiveType.GameObject:
                case QuestObjectiveType.TalkTo:
                case QuestObjectiveType.PlayerKills:
                case QuestObjectiveType.WinPvpPetBattles:
                case QuestObjectiveType.HaveCurrency:
                case QuestObjectiveType.ObtainCurrency:
                case QuestObjectiveType.IncreaseReputation:
                    return true;
                default:
                    break;
            }

            return false;
        }

        public bool IsStoringFlag()
        {
            switch (Type)
            {
                case QuestObjectiveType.AreaTrigger:
                case QuestObjectiveType.WinPetBattleAgainstNpc:
                case QuestObjectiveType.DefeatBattlePet:
                case QuestObjectiveType.CriteriaTree:
                case QuestObjectiveType.AreaTriggerEnter:
                case QuestObjectiveType.AreaTriggerExit:
                    return true;
                default:
                    break;
            }

            return false;
        }

        public static bool CanAlwaysBeProgressedInRaid(QuestObjectiveType type)
        {
            switch (type)
            {
                case QuestObjectiveType.Item:
                case QuestObjectiveType.Currency:
                case QuestObjectiveType.LearnSpell:
                case QuestObjectiveType.MinReputation:
                case QuestObjectiveType.MaxReputation:
                case QuestObjectiveType.Money:
                case QuestObjectiveType.HaveCurrency:
                case QuestObjectiveType.IncreaseReputation:
                    return true;
                default:
                    break;
            }

            return false;
        }
    }
}