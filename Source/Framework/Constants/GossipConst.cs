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
 */﻿

namespace Framework.Constants
{
    public enum GossipOption
    {
        None = 0,                    //Unit_Npc_Flag_None                (0)
        Gossip = 1,                    //Unit_Npc_Flag_Gossip              (1)
        Questgiver = 2,                    //Unit_Npc_Flag_Questgiver          (2)
        Vendor = 3,                    //Unit_Npc_Flag_Vendor              (128)
        Taxivendor = 4,                    //Unit_Npc_Flag_Taxivendor          (8192)
        Trainer = 5,                    //Unit_Npc_Flag_Trainer             (16)
        Spirithealer = 6,                    //Unit_Npc_Flag_Spirithealer        (16384)
        Spiritguide = 7,                    //Unit_Npc_Flag_Spiritguide         (32768)
        Innkeeper = 8,                    //Unit_Npc_Flag_Innkeeper           (65536)
        Banker = 9,                    //Unit_Npc_Flag_Banker              (131072)
        Petitioner = 10,                   //Unit_Npc_Flag_Petitioner          (262144)
        Tabarddesigner = 11,                   //Unit_Npc_Flag_Tabarddesigner      (524288)
        Battlefield = 12,                   //Unit_Npc_Flag_Battlefieldperson   (1048576)
        Auctioneer = 13,                   //Unit_Npc_Flag_Auctioneer          (2097152)
        Stablepet = 14,                   //Unit_Npc_Flag_Stable              (4194304)
        Armorer = 15,                   //Unit_Npc_Flag_Armorer             (4096)
        Unlearntalents = 16,                   //Unit_Npc_Flag_Trainer             (16) (Bonus Option For Trainer)
        Unlearnpettalents_Old = 17,     // deprecated
        Learndualspec = 18,                   //Unit_Npc_Flag_Trainer             (16) (Bonus Option For Trainer)
        Outdoorpvp = 19,                   //Added By Code (Option For Outdoor Pvp Creatures)
        Transmogrifier = 20,                   //UNIT_NPC_FLAG_TRANSMOGRIFIER
        Max
    }

    public enum GossipOptionIcon
    {
        Chat = 0,                    // White Chat Bubble
        Vendor = 1,                    // Brown Bag
        Taxi = 2,                    // Flightmarker (Paperplane)
        Trainer = 3,                    // Brown Book (Trainer)
        Interact1 = 4,                    // Golden Interaction Wheel
        Interact2 = 5,                    // Golden Interaction Wheel
        MoneyBag = 6,                    // Brown Bag (With Gold Coin In Lower Corner)
        Talk = 7,                    // White Chat Bubble (With "..." Inside)
        Tabard = 8,                    // White Tabard
        Battle = 9,                    // Two Crossed Swords
        Dot = 10,                   // Yellow Dot/Point
        Chat11 = 11,                   // White Chat Bubble
        Chat12 = 12,                   // White Chat Bubble
        Chat13 = 13,                   // White Chat Bubble
        Unk14 = 14,                   // Invalid - Do Not Use
        Unk15 = 15,                   // Invalid - Do Not Use
        Chat16 = 16,                   // White Chat Bubble
        Chat17 = 17,                   // White Chat Bubble
        Chat18 = 18,                   // White Chat Bubble
        Chat19 = 19,                   // White Chat Bubble
        Chat20 = 20,                   // White Chat Bubble
        Chat21 = 21,                   // transmogrifier?
        Max
    }

    public struct eTradeskill
    {
        // Skill Defines
        public const uint TradeskillAlchemy = 1;
        public const uint TradeskillBlacksmithing = 2;
        public const uint TradeskillCooking = 3;
        public const uint TradeskillEnchanting = 4;
        public const uint TradeskillEngineering = 5;
        public const uint TradeskillFirstaid = 6;
        public const uint TradeskillHerbalism = 7;
        public const uint TradeskillLeatherworking = 8;
        public const uint TradeskillPoisons = 9;
        public const uint TradeskillTailoring = 10;
        public const uint TradeskillMining = 11;
        public const uint TradeskillFishing = 12;
        public const uint TradeskillSkinning = 13;
        public const uint TradeskillJewlcrafting = 14;
        public const uint TradeskillInscription = 15;

        public const uint TradeskillLevelNone = 0;
        public const uint TradeskillLevelApprentice = 1;
        public const uint TradeskillLevelJourneyman = 2;
        public const uint TradeskillLevelExpert = 3;
        public const uint TradeskillLevelArtisan = 4;
        public const uint TradeskillLevelMaster = 5;
        public const uint TradeskillLevelGrandMaster = 6;

        // Gossip Defines
        public const uint GossipActionTrade = 1;
        public const uint GossipActionTrain = 2;
        public const uint GossipActionTaxi = 3;
        public const uint GossipActionGuild = 4;
        public const uint GossipActionBattle = 5;
        public const uint GossipActionBank = 6;
        public const uint GossipActionInn = 7;
        public const uint GossipActionHeal = 8;
        public const uint GossipActionTabard = 9;
        public const uint GossipActionAuction = 10;
        public const uint GossipActionInnInfo = 11;
        public const uint GossipActionUnlearn = 12;
        public const uint GossipActionInfoDef = 1000;

        public const uint GossipSenderMain = 1;
        public const uint GossipSenderInnInfo = 2;
        public const uint GossipSenderInfo = 3;
        public const uint GossipSenderSecProftrain = 4;
        public const uint GossipSenderSecClasstrain = 5;
        public const uint GossipSenderSecBattleinfo = 6;
        public const uint GossipSenderSecBank = 7;
        public const uint GossipSenderSecInn = 8;
        public const uint GossipSenderSecMailbox = 9;
        public const uint GossipSenderSecStablemaster = 10;
    }
}
