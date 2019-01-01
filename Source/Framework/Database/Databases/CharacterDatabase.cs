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

namespace Framework.Database
{
    public class CharacterDatabase : MySqlBase<CharStatements>
    {
        public override void PreparedStatements()
        {
            const string SelectItemInstanceContent = "ii.guid, ii.itemEntry, ii.creatorGuid, ii.giftCreatorGuid, ii.count, ii.duration, ii.charges, ii.flags, ii.enchantments, ii.randomPropertyType, ii.randomPropertyId, " +
                "ii.durability, ii.playedTime, ii.text, ii.upgradeId, ii.battlePetSpeciesId, ii.battlePetBreedData, ii.battlePetLevel, ii.battlePetDisplayId, ii.context, ii.bonusListIDs, " +
                "iit.itemModifiedAppearanceAllSpecs, iit.itemModifiedAppearanceSpec1, iit.itemModifiedAppearanceSpec2, iit.itemModifiedAppearanceSpec3, iit.itemModifiedAppearanceSpec4, " +
                "iit.spellItemEnchantmentAllSpecs, iit.spellItemEnchantmentSpec1, iit.spellItemEnchantmentSpec2, iit.spellItemEnchantmentSpec3, iit.spellItemEnchantmentSpec4, " +
                "ig.gemItemId1, ig.gemBonuses1, ig.gemContext1, ig.gemScalingLevel1, ig.gemItemId2, ig.gemBonuses2, ig.gemContext2, ig.gemScalingLevel2, ig.gemItemId3, ig.gemBonuses3, ig.gemContext3, ig.gemScalingLevel3, " +
                "im.fixedScalingLevel, im.artifactKnowledgeLevel";

            PrepareStatement(CharStatements.DEL_QUEST_POOL_SAVE, "DELETE FROM pool_quest_save WHERE pool_id = ?");
            PrepareStatement(CharStatements.INS_QUEST_POOL_SAVE, "INSERT INTO pool_quest_save (pool_id, quest_id) VALUES (?, ?)");
            PrepareStatement(CharStatements.DEL_NONEXISTENT_GUILD_BANK_ITEM, "DELETE FROM guild_bank_item WHERE guildid = ? AND TabId = ? AND SlotId = ?");
            PrepareStatement(CharStatements.DEL_EXPIRED_BANS, "UPDATE character_banned SET active = 0 WHERE unbandate <= UNIX_TIMESTAMP() AND unbandate <> bandate");
            PrepareStatement(CharStatements.SEL_GUID_BY_NAME, "SELECT guid FROM characters WHERE name = ?");
            PrepareStatement(CharStatements.SEL_CHECK_NAME, "SELECT 1 FROM characters WHERE name = ?");
            PrepareStatement(CharStatements.SEL_CHECK_GUID, "SELECT 1 FROM characters WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_SUM_CHARS, "SELECT COUNT(guid) FROM characters WHERE account = ?");
            PrepareStatement(CharStatements.SEL_CHAR_CREATE_INFO, "SELECT level, race, class FROM characters WHERE account = ? LIMIT 0, ?");
            PrepareStatement(CharStatements.INS_CHARACTER_BAN, "INSERT INTO character_banned VALUES (?, UNIX_TIMESTAMP(), UNIX_TIMESTAMP()+?, ?, ?, 1)");
            PrepareStatement(CharStatements.UPD_CHARACTER_BAN, "UPDATE character_banned SET active = 0 WHERE guid = ? AND active != 0");
            PrepareStatement(CharStatements.DEL_CHARACTER_BAN, "DELETE cb FROM character_banned cb INNER JOIN characters c ON c.guid = cb.guid WHERE c.account = ?");
            PrepareStatement(CharStatements.SEL_BANINFO, "SELECT bandate, unbandate-bandate, active, unbandate, banreason, bannedby FROM character_banned WHERE guid = ? ORDER BY bandate ASC");
            PrepareStatement(CharStatements.SEL_GUID_BY_NAME_FILTER, "SELECT guid, name FROM characters WHERE name LIKE CONCAT('%%', ?, '%%')");
            PrepareStatement(CharStatements.SEL_BANINFO_LIST, "SELECT bandate, unbandate, bannedby, banreason FROM character_banned WHERE guid = ? ORDER BY unbandate");
            PrepareStatement(CharStatements.SEL_BANNED_NAME, "SELECT characters.name FROM characters, character_banned WHERE character_banned.guid = ? AND character_banned.guid = characters.guid");
            PrepareStatement(CharStatements.SEL_MAIL_LIST_COUNT, "SELECT COUNT(id) FROM mail WHERE receiver = ? ");
            PrepareStatement(CharStatements.SEL_MAIL_LIST_INFO, "SELECT id, sender, (SELECT name FROM characters WHERE guid = sender) AS sendername, receiver, (SELECT name FROM characters WHERE guid = receiver) AS receivername, " +
                "subject, deliver_time, expire_time, money, has_items FROM mail WHERE receiver = ? ");
            PrepareStatement(CharStatements.SEL_MAIL_LIST_ITEMS, "SELECT itemEntry,count FROM item_instance WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_ENUM, "SELECT c.guid, c.name, c.race, c.class, c.gender, c.skin, c.face, c.hairStyle, c.hairColor, c.facialStyle, c.customDisplay1, c.customDisplay2, c.customDisplay3, c.level, c.zone, c.map, c.position_x, c.position_y, c.position_z, " +
                "gm.guildid, c.playerFlags, c.at_login, cp.entry, cp.modelid, cp.level, c.equipmentCache, cb.guid, c.slot, c.logout_time, c.activeTalentGroup, c.lastLoginBuild " +
                "FROM characters AS c LEFT JOIN character_pet AS cp ON c.guid = cp.owner AND cp.slot = ? LEFT JOIN guild_member AS gm ON c.guid = gm.guid " +
                "LEFT JOIN character_banned AS cb ON c.guid = cb.guid AND cb.active = 1 WHERE c.account = ? AND c.deleteInfos_Name IS NULL");
            PrepareStatement(CharStatements.SEL_ENUM_DECLINED_NAME, "SELECT c.guid, c.name, c.race, c.class, c.gender, c.skin, c.face, c.hairStyle, c.hairColor, c.facialStyle, c.customDisplay1, c.customDisplay2, c.customDisplay3, c.level, c.zone, c.map, " +
                "c.position_x, c.position_y, c.position_z, gm.guildid, c.playerFlags, c.at_login, cp.entry, cp.modelid, cp.level, c.equipmentCache, " +
                "cb.guid, c.slot, c.logout_time, c.activeTalentGroup, c.lastLoginBuild, cd.genitive FROM characters AS c LEFT JOIN character_pet AS cp ON c.guid = cp.owner AND cp.slot = ? " +
                "LEFT JOIN character_declinedname AS cd ON c.guid = cd.guid LEFT JOIN guild_member AS gm ON c.guid = gm.guid " +
                "LEFT JOIN character_banned AS cb ON c.guid = cb.guid AND cb.active = 1 WHERE c.account = ? AND c.deleteInfos_Name IS NULL");
            PrepareStatement(CharStatements.SEL_UNDELETE_ENUM, "SELECT c.guid, c.deleteInfos_Name, c.race, c.class, c.gender, c.skin, c.face, c.hairStyle, c.hairColor, c.facialStyle, c.customDisplay1, c.customDisplay2, c.customDisplay3, c.level, c.zone, c.map, c.position_x, c.position_y, c.position_z, " +
                "gm.guildid, c.playerFlags, c.at_login, cp.entry, cp.modelid, cp.level, c.equipmentCache, cb.guid, c.slot, c.logout_time, c.activeTalentGroup, c.lastLoginBuild " +
                "FROM characters AS c LEFT JOIN character_pet AS cp ON c.guid = cp.owner AND cp.slot = ? LEFT JOIN guild_member AS gm ON c.guid = gm.guid " +
                "LEFT JOIN character_banned AS cb ON c.guid = cb.guid AND cb.active = 1 WHERE c.deleteInfos_Account = ? AND c.deleteInfos_Name IS NOT NULL");
            PrepareStatement(CharStatements.SEL_UNDELETE_ENUM_DECLINED_NAME, "SELECT c.guid, c.deleteInfos_Name, c.race, c.class, c.gender, c.skin, c.face, c.hairStyle, c.hairColor, c.facialStyle, c.customDisplay1, c.customDisplay2, c.customDisplay3, c.level, c.zone, c.map, " +
                "c.position_x, c.position_y, c.position_z, gm.guildid, c.playerFlags, c.at_login, cp.entry, cp.modelid, cp.level, c.equipmentCache, " +
                "cb.guid, c.slot, c.logout_time, c.activeTalentGroup, c.lastLoginBuild, cd.genitive FROM characters AS c LEFT JOIN character_pet AS cp ON c.guid = cp.owner AND cp.slot = ? " +
                "LEFT JOIN character_declinedname AS cd ON c.guid = cd.guid LEFT JOIN guild_member AS gm ON c.guid = gm.guid " +
                "LEFT JOIN character_banned AS cb ON c.guid = cb.guid AND cb.active = 1 WHERE c.deleteInfos_Account = ? AND c.deleteInfos_Name IS NOT NULL");
            PrepareStatement(CharStatements.SEL_FREE_NAME, "SELECT name, at_login FROM characters WHERE guid = ? AND NOT EXISTS (SELECT NULL FROM characters WHERE name = ?)");
            PrepareStatement(CharStatements.SEL_GUID_RACE_ACC_BY_NAME, "SELECT guid, race, account FROM characters WHERE name = ?");
            PrepareStatement(CharStatements.SEL_CHAR_LEVEL, "SELECT level FROM characters WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHAR_ZONE, "SELECT zone FROM characters WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHAR_POSITION_XYZ, "SELECT map, position_x, position_y, position_z FROM characters WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHAR_POSITION, "SELECT position_x, position_y, position_z, orientation, map, taxi_path FROM characters WHERE guid = ?");

            PrepareStatement(CharStatements.DEL_BATTLEGROUND_RANDOM_ALL, "DELETE FROM character_battleground_random");
            PrepareStatement(CharStatements.DEL_BATTLEGROUND_RANDOM, "DELETE FROM character_battleground_random WHERE guid = ?");
            PrepareStatement(CharStatements.INS_BATTLEGROUND_RANDOM, "INSERT INTO character_battleground_random (guid) VALUES (?)");

            PrepareStatement(CharStatements.SEL_CHARACTER, "SELECT c.guid, account, name, race, class, gender, level, xp, money, skin, face, hairStyle, hairColor, facialStyle, customDisplay1, customDisplay2, customDisplay3, inventorySlots, bankSlots, restState, playerFlags, playerFlagsEx, " +
                "position_x, position_y, position_z, map, orientation, taximask, cinematic, totaltime, leveltime, rest_bonus, logout_time, is_logout_resting, resettalents_cost, " +
                "resettalents_time, primarySpecialization, trans_x, trans_y, trans_z, trans_o, transguid, extra_flags, stable_slots, at_login, zone, online, death_expire_time, taxi_path, dungeonDifficulty, " +
                "totalKills, todayKills, yesterdayKills, chosenTitle, watchedFaction, drunk, " +
                "health, power1, power2, power3, power4, power5, power6, instance_id, activeTalentGroup, lootSpecId, exploredZones, knownTitles, actionBars, grantableLevels, raidDifficulty, legacyRaidDifficulty, fishingSteps, " +
                "honor, honorLevel, honorRestState, honorRestBonus " +
                "FROM characters c LEFT JOIN character_fishingsteps cfs ON c.guid = cfs.guid WHERE c.guid = ?");

            PrepareStatement(CharStatements.SEL_GROUP_MEMBER, "SELECT guid FROM group_member WHERE memberGuid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_INSTANCE, "SELECT id, permanent, map, difficulty, extendState, resettime, entranceId FROM character_instance LEFT JOIN instance ON instance = id WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_AURAS, "SELECT casterGuid, itemGuid, spell, effectMask, recalculateMask, stackCount, maxDuration, remainTime, remainCharges, castItemLevel FROM character_aura WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_AURA_EFFECTS, "SELECT casterGuid, itemGuid, spell, effectMask, effectIndex, amount, baseAmount FROM character_aura_effect WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_SPELL, "SELECT spell, active, disabled FROM character_spell WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS, "SELECT quest, status, timer FROM character_queststatus WHERE guid = ? AND status <> 0");
            PrepareStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES, "SELECT quest, objective, data FROM character_queststatus_objectives WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES_CRITERIA, "SELECT questObjectiveId FROM character_queststatus_objectives_criteria WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS, "SELECT criteriaId, counter, date FROM character_queststatus_objectives_criteria_progress WHERE guid = ?");

            PrepareStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_DAILY, "SELECT quest, time FROM character_queststatus_daily WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_WEEKLY, "SELECT quest FROM character_queststatus_weekly WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_MONTHLY, "SELECT quest FROM character_queststatus_monthly WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_SEASONAL, "SELECT quest, event FROM character_queststatus_seasonal WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_DAILY, "DELETE FROM character_queststatus_daily WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_WEEKLY, "DELETE FROM character_queststatus_weekly WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_MONTHLY, "DELETE FROM character_queststatus_monthly WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_SEASONAL, "DELETE FROM character_queststatus_seasonal WHERE guid = ?");
            PrepareStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_DAILY, "INSERT INTO character_queststatus_daily (guid, quest, time) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_WEEKLY, "INSERT INTO character_queststatus_weekly (guid, quest) VALUES (?, ?)");
            PrepareStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_MONTHLY, "INSERT INTO character_queststatus_monthly (guid, quest) VALUES (?, ?)");
            PrepareStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_SEASONAL, "INSERT INTO character_queststatus_seasonal (guid, quest, event) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.DEL_RESET_CHARACTER_QUESTSTATUS_DAILY, "DELETE FROM character_queststatus_daily");
            PrepareStatement(CharStatements.DEL_RESET_CHARACTER_QUESTSTATUS_WEEKLY, "DELETE FROM character_queststatus_weekly");
            PrepareStatement(CharStatements.DEL_RESET_CHARACTER_QUESTSTATUS_MONTHLY, "DELETE FROM character_queststatus_monthly");
            PrepareStatement(CharStatements.DEL_RESET_CHARACTER_QUESTSTATUS_SEASONAL_BY_EVENT, "DELETE FROM character_queststatus_seasonal WHERE event = ?");

            PrepareStatement(CharStatements.SEL_CHARACTER_REPUTATION, "SELECT faction, standing, flags FROM character_reputation WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_INVENTORY, "SELECT " + SelectItemInstanceContent + ", bag, slot FROM character_inventory ci JOIN item_instance ii ON ci.item = ii.guid LEFT JOIN item_instance_gems ig ON ii.guid = ig.itemGuid LEFT JOIN item_instance_transmog iit ON ii.guid = iit.itemGuid LEFT JOIN item_instance_modifiers im ON ii.guid = im.itemGuid WHERE ci.guid = ? ORDER BY (ii.flags & 0x80000) ASC, bag ASC, slot ASC");
            PrepareStatement(CharStatements.SEL_CHARACTER_ACTIONS, "SELECT a.button, a.action, a.type FROM character_action as a, characters as c WHERE a.guid = c.guid AND a.spec = c.activeTalentGroup AND a.guid = ? ORDER BY button");
            PrepareStatement(CharStatements.SEL_CHARACTER_MAILCOUNT, "SELECT COUNT(id) FROM mail WHERE receiver = ? AND (checked & 1) = 0 AND deliver_time <= ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_MAILDATE, "SELECT MIN(deliver_time) FROM mail WHERE receiver = ? AND (checked & 1) = 0");
            PrepareStatement(CharStatements.SEL_MAIL_COUNT, "SELECT COUNT(*) FROM mail WHERE receiver = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_SOCIALLIST, "SELECT cs.friend, c.account, cs.flags, cs.note FROM character_social cs JOIN characters c ON c.guid = cs.friend WHERE cs.guid = ? AND c.deleteinfos_name IS NULL LIMIT 255");
            PrepareStatement(CharStatements.SEL_CHARACTER_HOMEBIND, "SELECT mapId, zoneId, posX, posY, posZ FROM character_homebind WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_SPELLCOOLDOWNS, "SELECT spell, item, time, categoryId, categoryEnd FROM character_spell_cooldown WHERE guid = ? AND time > UNIX_TIMESTAMP()");
            PrepareStatement(CharStatements.SEL_CHARACTER_SPELL_CHARGES, "SELECT categoryId, rechargeStart, rechargeEnd FROM character_spell_charges WHERE guid = ? AND rechargeEnd > UNIX_TIMESTAMP() ORDER BY rechargeEnd");
            PrepareStatement(CharStatements.SEL_CHARACTER_DECLINEDNAMES, "SELECT genitive, dative, accusative, instrumental, prepositional FROM character_declinedname WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_GUILD_MEMBER, "SELECT guildid, rank FROM guild_member WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_GUILD_MEMBER_EXTENDED, "SELECT g.guildid, g.name, gr.rname, gr.rid, gm.pnote, gm.offnote " +
                "FROM guild g JOIN guild_member gm ON g.guildid = gm.guildid " +
                "JOIN guild_rank gr ON g.guildid = gr.guildid AND gm.rank = gr.rid WHERE gm.guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_ACHIEVEMENTS, "SELECT achievement, date FROM character_achievement WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_CRITERIAPROGRESS, "SELECT criteria, counter, date FROM character_achievement_progress WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_EQUIPMENTSETS, "SELECT setguid, setindex, name, iconname, ignore_mask, AssignedSpecIndex, item0, item1, item2, item3, item4, item5, item6, item7, item8, " +
                "item9, item10, item11, item12, item13, item14, item15, item16, item17, item18 FROM character_equipmentsets WHERE guid = ? ORDER BY setindex");
            PrepareStatement(CharStatements.SEL_CHARACTER_TRANSMOG_OUTFITS, "SELECT setguid, setindex, name, iconname, ignore_mask, appearance0, appearance1, appearance2, appearance3, appearance4, " +
                "appearance5, appearance6, appearance7, appearance8, appearance9, appearance10, appearance11, appearance12, appearance13, appearance14, appearance15, appearance16, " +
                "appearance17, appearance18, mainHandEnchant, offHandEnchant FROM character_transmog_outfits WHERE guid = ? ORDER BY setindex");
            PrepareStatement(CharStatements.SEL_CHARACTER_BGDATA, "SELECT instanceId, team, joinX, joinY, joinZ, joinO, joinMapId, taxiStart, taxiEnd, mountSpell FROM character_battleground_data WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_GLYPHS, "SELECT talentGroup, glyphId FROM character_glyphs WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_TALENTS, "SELECT talentId, talentGroup FROM character_talent WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_PVP_TALENTS, "SELECT talentId0, talentId1, talentId2, talentId3, talentGroup FROM character_pvp_talent WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_SKILLS, "SELECT skill, value, max FROM character_skills WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_RANDOMBG, "SELECT guid FROM character_battleground_random WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_BANNED, "SELECT guid FROM character_banned WHERE guid = ? AND active = 1");
            PrepareStatement(CharStatements.SEL_CHARACTER_QUESTSTATUSREW, "SELECT quest FROM character_queststatus_rewarded WHERE guid = ? AND active = 1");
            PrepareStatement(CharStatements.SEL_ACCOUNT_INSTANCELOCKTIMES, "SELECT instanceId, releaseTime FROM account_instance_times WHERE accountId = ?");

            PrepareStatement(CharStatements.SEL_CHARACTER_ACTIONS_SPEC, "SELECT button, action, type FROM character_action WHERE guid = ? AND spec = ? ORDER BY button");
            PrepareStatement(CharStatements.SEL_MAILITEMS, "SELECT " + SelectItemInstanceContent + ", ii.owner_guid FROM mail_items mi JOIN item_instance ii ON mi.item_guid = ii.guid LEFT JOIN item_instance_gems ig ON ii.guid = ig.itemGuid LEFT JOIN item_instance_transmog iit ON ii.guid = iit.itemGuid LEFT JOIN item_instance_modifiers im ON ii.guid = im.itemGuid WHERE mail_id = ?");
            PrepareStatement(CharStatements.SEL_AUCTION_ITEMS, "SELECT " + SelectItemInstanceContent + " FROM auctionhouse ah JOIN item_instance ii ON ah.itemguid = ii.guid LEFT JOIN item_instance_gems ig ON ii.guid = ig.itemGuid LEFT JOIN item_instance_transmog iit ON ii.guid = iit.itemGuid LEFT JOIN item_instance_modifiers im ON ii.guid = im.itemGuid");
            PrepareStatement(CharStatements.SEL_AUCTIONS, "SELECT id, auctioneerguid, itemguid, itemEntry, count, itemowner, buyoutprice, time, buyguid, lastbid, startbid, deposit FROM auctionhouse ah INNER JOIN item_instance ii ON ii.guid = ah.itemguid");
            PrepareStatement(CharStatements.INS_AUCTION, "INSERT INTO auctionhouse (id, auctioneerguid, itemguid, itemowner, buyoutprice, time, buyguid, lastbid, startbid, deposit) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_AUCTION, "DELETE FROM auctionhouse WHERE id = ?");
            PrepareStatement(CharStatements.UPD_AUCTION_BID, "UPDATE auctionhouse SET buyguid = ?, lastbid = ? WHERE id = ?");
            PrepareStatement(CharStatements.INS_MAIL, "INSERT INTO mail(id, messageType, stationery, mailTemplateId, sender, receiver, subject, body, has_items, expire_time, deliver_time, money, cod, checked) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_MAIL_BY_ID, "DELETE FROM mail WHERE id = ?");
            PrepareStatement(CharStatements.INS_MAIL_ITEM, "INSERT INTO mail_items(mail_id, item_guid, receiver) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.DEL_MAIL_ITEM, "DELETE FROM mail_items WHERE item_guid = ?");
            PrepareStatement(CharStatements.DEL_INVALID_MAIL_ITEM, "DELETE FROM mail_items WHERE item_guid = ?");
            PrepareStatement(CharStatements.DEL_EMPTY_EXPIRED_MAIL, "DELETE FROM mail WHERE expire_time < ? AND has_items = 0 AND body = ''");
            PrepareStatement(CharStatements.SEL_EXPIRED_MAIL, "SELECT id, messageType, sender, receiver, has_items, expire_time, cod, checked, mailTemplateId FROM mail WHERE expire_time < ?");
            PrepareStatement(CharStatements.SEL_EXPIRED_MAIL_ITEMS, "SELECT item_guid, itemEntry, mail_id FROM mail_items mi INNER JOIN item_instance ii ON ii.guid = mi.item_guid LEFT JOIN mail mm ON mi.mail_id = mm.id WHERE mm.id IS NOT NULL AND mm.expire_time < ?");
            PrepareStatement(CharStatements.UPD_MAIL_RETURNED, "UPDATE mail SET sender = ?, receiver = ?, expire_time = ?, deliver_time = ?, cod = 0, checked = ? WHERE id = ?");
            PrepareStatement(CharStatements.UPD_MAIL_ITEM_RECEIVER, "UPDATE mail_items SET receiver = ? WHERE item_guid = ?");
            PrepareStatement(CharStatements.UPD_ITEM_OWNER, "UPDATE item_instance SET owner_guid = ? WHERE guid = ?");

            PrepareStatement(CharStatements.SEL_ITEM_REFUNDS, "SELECT paidMoney, paidExtendedCost FROM item_refund_instance WHERE item_guid = ? AND player_guid = ? LIMIT 1");
            PrepareStatement(CharStatements.SEL_ITEM_BOP_TRADE, "SELECT allowedPlayers FROM item_soulbound_trade_data WHERE itemGuid = ? LIMIT 1");
            PrepareStatement(CharStatements.DEL_ITEM_BOP_TRADE, "DELETE FROM item_soulbound_trade_data WHERE itemGuid = ? LIMIT 1");
            PrepareStatement(CharStatements.INS_ITEM_BOP_TRADE, "INSERT INTO item_soulbound_trade_data VALUES (?, ?)");
            PrepareStatement(CharStatements.REP_INVENTORY_ITEM, "REPLACE INTO character_inventory (guid, bag, slot, item) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.REP_ITEM_INSTANCE, "REPLACE INTO item_instance (itemEntry, owner_guid, creatorGuid, giftCreatorGuid, count, duration, charges, flags, enchantments, randomPropertyType, randomPropertyId, durability, playedTime, text, upgradeId, battlePetSpeciesId, battlePetBreedData, battlePetLevel, battlePetDisplayId, context, bonusListIDs, guid) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.UPD_ITEM_INSTANCE, "UPDATE item_instance SET itemEntry = ?, owner_guid = ?, creatorGuid = ?, giftCreatorGuid = ?, count = ?, duration = ?, charges = ?, flags = ?, enchantments = ?, randomPropertyType = ?, randomPropertyId = ?, durability = ?, playedTime = ?, text = ?, upgradeId = ?, battlePetSpeciesId = ?, battlePetBreedData = ?, battlePetLevel = ?, battlePetDisplayId = ?, context = ?, bonusListIDs = ? WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_ITEM_INSTANCE_ON_LOAD, "UPDATE item_instance SET duration = ?, flags = ?, durability = ?, upgradeId = ? WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_ITEM_INSTANCE, "DELETE FROM item_instance WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_ITEM_INSTANCE_BY_OWNER, "DELETE FROM item_instance WHERE owner_guid = ?");
            PrepareStatement(CharStatements.INS_ITEM_INSTANCE_GEMS, "INSERT INTO item_instance_gems (itemGuid, gemItemId1, gemBonuses1, gemContext1, gemScalingLevel1, gemItemId2, gemBonuses2, gemContext2, gemScalingLevel2, gemItemId3, gemBonuses3, gemContext3, gemScalingLevel3) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS, "DELETE FROM item_instance_gems WHERE itemGuid = ?");
            PrepareStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS_BY_OWNER, "DELETE iig FROM item_instance_gems iig LEFT JOIN item_instance ii ON iig.itemGuid = ii.guid WHERE ii.owner_guid = ?");
            PrepareStatement(CharStatements.INS_ITEM_INSTANCE_TRANSMOG, "INSERT INTO item_instance_transmog (itemGuid, itemModifiedAppearanceAllSpecs, itemModifiedAppearanceSpec1, itemModifiedAppearanceSpec2, itemModifiedAppearanceSpec3, itemModifiedAppearanceSpec4, " +
                "spellItemEnchantmentAllSpecs, spellItemEnchantmentSpec1, spellItemEnchantmentSpec2, spellItemEnchantmentSpec3, spellItemEnchantmentSpec4) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG, "DELETE FROM item_instance_transmog WHERE itemGuid = ?");
            PrepareStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG_BY_OWNER, "DELETE iit FROM item_instance_transmog iit LEFT JOIN item_instance ii ON iit.itemGuid = ii.guid WHERE ii.owner_guid = ?");
            PrepareStatement(CharStatements.SEL_ITEM_INSTANCE_ARTIFACT, "SELECT a.itemGuid, a.xp, a.artifactAppearanceId, a.artifactTierId, ap.artifactPowerId, ap.purchasedRank FROM item_instance_artifact_powers ap LEFT JOIN item_instance_artifact a ON ap.itemGuid = a.itemGuid INNER JOIN character_inventory ci ON ci.item = ap.itemGuid WHERE ci.guid = ?");
            PrepareStatement(CharStatements.INS_ITEM_INSTANCE_ARTIFACT, "INSERT INTO item_instance_artifact (itemGuid, xp, artifactAppearanceId, artifactTierId) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT, "DELETE FROM item_instance_artifact WHERE itemGuid = ?");
            PrepareStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_BY_OWNER, "DELETE iia FROM item_instance_artifact iia LEFT JOIN item_instance ii ON iia.itemGuid = ii.guid WHERE ii.owner_guid = ?");
            PrepareStatement(CharStatements.INS_ITEM_INSTANCE_ARTIFACT_POWERS, "INSERT INTO item_instance_artifact_powers (itemGuid, artifactPowerId, purchasedRank) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS, "DELETE FROM item_instance_artifact_powers WHERE itemGuid = ?");
            PrepareStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS_BY_OWNER, "DELETE iiap FROM item_instance_artifact_powers iiap LEFT JOIN item_instance ii ON iiap.itemGuid = ii.guid WHERE ii.owner_guid = ?");
            PrepareStatement(CharStatements.INS_ITEM_INSTANCE_MODIFIERS, "INSERT INTO item_instance_modifiers (itemGuid, fixedScalingLevel, artifactKnowledgeLevel) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS, "DELETE FROM item_instance_modifiers WHERE itemGuid = ?");
            PrepareStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS_BY_OWNER, "DELETE im FROM item_instance_modifiers im LEFT JOIN item_instance ii ON im.itemGuid = ii.guid WHERE ii.owner_guid = ?");
            PrepareStatement(CharStatements.UPD_GIFT_OWNER, "UPDATE character_gifts SET guid = ? WHERE item_guid = ?");
            PrepareStatement(CharStatements.DEL_GIFT, "DELETE FROM character_gifts WHERE item_guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_GIFT_BY_ITEM, "SELECT entry, flags FROM character_gifts WHERE item_guid = ?");
            PrepareStatement(CharStatements.SEL_ACCOUNT_BY_NAME, "SELECT account FROM characters WHERE name = ?");
            PrepareStatement(CharStatements.UPD_ACCOUNT_BY_GUID, "UPDATE characters SET account = ? WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_ACCOUNT_INSTANCE_LOCK_TIMES, "DELETE FROM account_instance_times WHERE accountId = ?");
            PrepareStatement(CharStatements.INS_ACCOUNT_INSTANCE_LOCK_TIMES, "INSERT INTO account_instance_times (accountId, instanceId, releaseTime) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.SEL_MATCH_MAKER_RATING, "SELECT matchMakerRating FROM character_arena_stats WHERE guid = ? AND slot = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_COUNT, "SELECT account, COUNT(guid) FROM characters WHERE account = ? GROUP BY account");
            PrepareStatement(CharStatements.UPD_NAME_BY_GUID, "UPDATE characters SET name = ? WHERE guid = ?");

            // Guild handling
            // 0: uint32, 1: string, 2: uint32, 3: string, 4: string, 5: uint64, 6-10: uint32, 11: uint64
            PrepareStatement(CharStatements.INS_GUILD, "INSERT INTO guild (guildid, name, leaderguid, info, motd, createdate, EmblemStyle, EmblemColor, BorderStyle, BorderColor, BackgroundColor, BankMoney) VALUES(?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GUILD, "DELETE FROM guild WHERE guildid = ?"); // 0: uint32
                                                                                                       // 0: string, 1: uint32
            PrepareStatement(CharStatements.UPD_GUILD_NAME, "UPDATE guild SET name = ? WHERE guildid = ?");
            // 0: uint32, 1: uint32, 2: uint8, 4: string, 5: string
            PrepareStatement(CharStatements.INS_GUILD_MEMBER, "INSERT INTO guild_member (guildid, guid, rank, pnote, offnote) VALUES (?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GUILD_MEMBER, "DELETE FROM guild_member WHERE guid = ?"); // 0: uint32
            PrepareStatement(CharStatements.DEL_GUILD_MEMBERS, "DELETE FROM guild_member WHERE guildid = ?"); // 0: uint32
                                                                                                                      // 0: uint32, 1: uint8, 3: string, 4: uint32, 5: uint32
            PrepareStatement(CharStatements.INS_GUILD_RANK, "INSERT INTO guild_rank (guildid, rid, rname, rights, BankMoneyPerDay) VALUES (?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GUILD_RANKS, "DELETE FROM guild_rank WHERE guildid = ?"); // 0: uint32
            PrepareStatement(CharStatements.DEL_GUILD_RANK, "DELETE FROM guild_rank WHERE guildid = ? AND rid = ?"); // 0: uint32, 1: uint8
            PrepareStatement(CharStatements.INS_GUILD_BANK_TAB, "INSERT INTO guild_bank_tab (guildid, TabId) VALUES (?, ?)"); // 0: uint32, 1: uint8
            PrepareStatement(CharStatements.DEL_GUILD_BANK_TAB, "DELETE FROM guild_bank_tab WHERE guildid = ? AND TabId = ?"); // 0: uint32, 1: uint8
            PrepareStatement(CharStatements.DEL_GUILD_BANK_TABS, "DELETE FROM guild_bank_tab WHERE guildid = ?"); // 0: uint32
                                                                                                                          // 0: uint32, 1: uint8, 2: uint8, 3: uint32, 4: uint32
            PrepareStatement(CharStatements.INS_GUILD_BANK_ITEM, "INSERT INTO guild_bank_item (guildid, TabId, SlotId, item_guid) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GUILD_BANK_ITEM, "DELETE FROM guild_bank_item WHERE guildid = ? AND TabId = ? AND SlotId = ?"); // 0: uint32, 1: uint8, 2: uint8
            PrepareStatement(CharStatements.SEL_GUILD_BANK_ITEMS, "SELECT " + SelectItemInstanceContent + ", guildid, TabId, SlotId FROM guild_bank_item gbi INNER JOIN item_instance ii ON gbi.item_guid = ii.guid LEFT JOIN item_instance_gems ig ON ii.guid = ig.itemGuid LEFT JOIN item_instance_transmog iit ON ii.guid = iit.itemGuid LEFT JOIN item_instance_modifiers im ON ii.guid = im.itemGuid");
            PrepareStatement(CharStatements.DEL_GUILD_BANK_ITEMS, "DELETE FROM guild_bank_item WHERE guildid = ?"); // 0: uint32
                                                                                                                            // 0: uint32, 1: uint8, 2: uint8, 3: uint8, 4: uint32
            PrepareStatement(CharStatements.INS_GUILD_BANK_RIGHT, "INSERT INTO guild_bank_right (guildid, TabId, rid, gbright, SlotPerDay) VALUES (?, ?, ?, ?, ?) " +
                "ON DUPLICATE KEY UPDATE gbright = VALUES(gbright), SlotPerDay = VALUES(SlotPerDay)");
            PrepareStatement(CharStatements.DEL_GUILD_BANK_RIGHTS, "DELETE FROM guild_bank_right WHERE guildid = ?"); // 0: uint32
            PrepareStatement(CharStatements.DEL_GUILD_BANK_RIGHTS_FOR_RANK, "DELETE FROM guild_bank_right WHERE guildid = ? AND rid = ?"); // 0: uint32, 1: uint8
                                                                                                                                                   // 0-1: uint32, 2-3: uint8, 4-5: uint32, 6: uint16, 7: uint8, 8: uint64
            PrepareStatement(CharStatements.INS_GUILD_BANK_EVENTLOG, "INSERT INTO guild_bank_eventlog (guildid, LogGuid, TabId, EventType, PlayerGuid, ItemOrMoney, ItemStackCount, DestTabId, TimeStamp) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GUILD_BANK_EVENTLOG, "DELETE FROM guild_bank_eventlog WHERE guildid = ? AND LogGuid = ? AND TabId = ?"); // 0: uint32, 1: uint32, 2: uint8
            PrepareStatement(CharStatements.DEL_GUILD_BANK_EVENTLOGS, "DELETE FROM guild_bank_eventlog WHERE guildid = ?"); // 0: uint32
                                                                                                                                    // 0-1: uint32, 2: uint8, 3-4: uint32, 5: uint8, 6: uint64
            PrepareStatement(CharStatements.INS_GUILD_EVENTLOG, "INSERT INTO guild_eventlog (guildid, LogGuid, EventType, PlayerGuid1, PlayerGuid2, NewRank, TimeStamp) VALUES (?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GUILD_EVENTLOG, "DELETE FROM guild_eventlog WHERE guildid = ? AND LogGuid = ?"); // 0: uint32, 1: uint32
            PrepareStatement(CharStatements.DEL_GUILD_EVENTLOGS, "DELETE FROM guild_eventlog WHERE guildid = ?"); // 0: uint32
            PrepareStatement(CharStatements.UPD_GUILD_MEMBER_PNOTE, "UPDATE guild_member SET pnote = ? WHERE guid = ?"); // 0: string, 1: uint32
            PrepareStatement(CharStatements.UPD_GUILD_MEMBER_OFFNOTE, "UPDATE guild_member SET offnote = ? WHERE guid = ?"); // 0: string, 1: uint32
            PrepareStatement(CharStatements.UPD_GUILD_MEMBER_RANK, "UPDATE guild_member SET rank = ? WHERE guid = ?"); // 0: uint8, 1: uint32
            PrepareStatement(CharStatements.UPD_GUILD_MOTD, "UPDATE guild SET motd = ? WHERE guildid = ?"); // 0: string, 1: uint32
            PrepareStatement(CharStatements.UPD_GUILD_INFO, "UPDATE guild SET info = ? WHERE guildid = ?"); // 0: string, 1: uint32
            PrepareStatement(CharStatements.UPD_GUILD_LEADER, "UPDATE guild SET leaderguid = ? WHERE guildid = ?"); // 0: uint32, 1: uint32
            PrepareStatement(CharStatements.UPD_GUILD_RANK_NAME, "UPDATE guild_rank SET rname = ? WHERE rid = ? AND guildid = ?"); // 0: string, 1: uint8, 2: uint32
            PrepareStatement(CharStatements.UPD_GUILD_RANK_RIGHTS, "UPDATE guild_rank SET rights = ? WHERE rid = ? AND guildid = ?"); // 0: uint32, 1: uint8, 2: uint32
                                                                                                                                              // 0-5: uint32
            PrepareStatement(CharStatements.UPD_GUILD_EMBLEM_INFO, "UPDATE guild SET EmblemStyle = ?, EmblemColor = ?, BorderStyle = ?, BorderColor = ?, BackgroundColor = ? WHERE guildid = ?");
            // 0: string, 1: string, 2: uint32, 3: uint8
            PrepareStatement(CharStatements.UPD_GUILD_BANK_TAB_INFO, "UPDATE guild_bank_tab SET TabName = ?, TabIcon = ? WHERE guildid = ? AND TabId = ?");
            PrepareStatement(CharStatements.UPD_GUILD_BANK_MONEY, "UPDATE guild SET BankMoney = ? WHERE guildid = ?"); // 0: uint64, 1: uint32
                                                                                                                               // 0: uint8, 1: uint32, 2: uint8, 3: uint32
            PrepareStatement(CharStatements.UPD_GUILD_RANK_BANK_MONEY, "UPDATE guild_rank SET BankMoneyPerDay = ? WHERE rid = ? AND guildid = ?"); // 0: uint32, 1: uint8, 2: uint32
            PrepareStatement(CharStatements.UPD_GUILD_BANK_TAB_TEXT, "UPDATE guild_bank_tab SET TabText = ? WHERE guildid = ? AND TabId = ?"); // 0: string, 1: uint32, 2: uint8

            PrepareStatement(CharStatements.INS_GUILD_MEMBER_WITHDRAW_TABS, "INSERT INTO guild_member_withdraw (guid, tab0, tab1, tab2, tab3, tab4, tab5, tab6, tab7) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?) " +
                "ON DUPLICATE KEY UPDATE tab0 = VALUES (tab0), tab1 = VALUES (tab1), tab2 = VALUES (tab2), tab3 = VALUES (tab3), tab4 = VALUES (tab4), tab5 = VALUES (tab5), tab6 = VALUES (tab6), tab7 = VALUES (tab7)");
            PrepareStatement(CharStatements.INS_GUILD_MEMBER_WITHDRAW_MONEY, "INSERT INTO guild_member_withdraw (guid, money) VALUES (?, ?) ON DUPLICATE KEY UPDATE money = VALUES (money)");
            PrepareStatement(CharStatements.DEL_GUILD_MEMBER_WITHDRAW, "TRUNCATE guild_member_withdraw");

            // 0: uint32, 1: uint32, 2: uint32
            PrepareStatement(CharStatements.SEL_CHAR_DATA_FOR_GUILD, "SELECT name, level, class, gender, zone, account FROM characters WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_GUILD_ACHIEVEMENT, "DELETE FROM guild_achievement WHERE guildId = ? AND achievement = ?");
            PrepareStatement(CharStatements.INS_GUILD_ACHIEVEMENT, "INSERT INTO guild_achievement (guildId, achievement, date, guids) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GUILD_ACHIEVEMENT_CRITERIA, "DELETE FROM guild_achievement_progress WHERE guildId = ? AND criteria = ?");
            PrepareStatement(CharStatements.INS_GUILD_ACHIEVEMENT_CRITERIA, "INSERT INTO guild_achievement_progress (guildId, criteria, counter, date, completedGuid) VALUES (?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_ALL_GUILD_ACHIEVEMENTS, "DELETE FROM guild_achievement WHERE guildId = ? AND achievement NOT IN (5407,5408,5409,5410,5411,5985,6126,6628,6678,6679,6680,8257,8512,8513,9397,9399,10380)");
            PrepareStatement(CharStatements.DEL_ALL_GUILD_ACHIEVEMENT_CRITERIA, "DELETE FROM guild_achievement_progress WHERE guildId = ?");
            PrepareStatement(CharStatements.SEL_GUILD_ACHIEVEMENT, "SELECT achievement, date, guids FROM guild_achievement WHERE guildId = ?");
            PrepareStatement(CharStatements.SEL_GUILD_ACHIEVEMENT_CRITERIA, "SELECT criteria, counter, date, completedGuid FROM guild_achievement_progress WHERE guildId = ?");
            PrepareStatement(CharStatements.INS_GUILD_NEWS, "INSERT INTO guild_newslog (guildid, LogGuid, EventType, PlayerGuid, Flags, Value, Timestamp) VALUES (?, ?, ?, ?, ?, ?, ?)" +
                " ON DUPLICATE KEY UPDATE LogGuid = VALUES (LogGuid), EventType = VALUES (EventType), PlayerGuid = VALUES (PlayerGuid), Flags = VALUES (Flags), Value = VALUES (Value), Timestamp = VALUES (Timestamp)");

            // Chat channel handling
            PrepareStatement(CharStatements.SEL_CHANNEL, "SELECT name, announce, ownership, password, bannedList FROM channels WHERE name = ? AND team = ?");
            PrepareStatement(CharStatements.INS_CHANNEL, "INSERT INTO channels(name, team, lastUsed) VALUES (?, ?, UNIX_TIMESTAMP())");
            PrepareStatement(CharStatements.UPD_CHANNEL, "UPDATE channels SET announce = ?, ownership = ?, password = ?, bannedList = ?, lastUsed = UNIX_TIMESTAMP() WHERE name = ? AND team = ?");
            PrepareStatement(CharStatements.UPD_CHANNEL_USAGE, "UPDATE channels SET lastUsed = UNIX_TIMESTAMP() WHERE name = ? AND team = ?");
            PrepareStatement(CharStatements.UPD_CHANNEL_OWNERSHIP, "UPDATE channels SET ownership = ? WHERE name LIKE ?");
            PrepareStatement(CharStatements.DEL_OLD_CHANNELS, "DELETE FROM channels WHERE ownership = 1 AND lastUsed + ? < UNIX_TIMESTAMP()");

            // Equipmentsets
            PrepareStatement(CharStatements.UPD_EQUIP_SET, "UPDATE character_equipmentsets SET name=?, iconname=?, ignore_mask=?, AssignedSpecIndex=?, item0=?, item1=?, item2=?, item3=?, " +
                "item4=?, item5=?, item6=?, item7=?, item8=?, item9=?, item10=?, item11=?, item12=?, item13=?, item14=?, item15=?, item16=?, " + 
                "item17=?, item18=? WHERE guid=? AND setguid=? AND setindex=?");
            PrepareStatement(CharStatements.INS_EQUIP_SET, "INSERT INTO character_equipmentsets (guid, setguid, setindex, name, iconname, ignore_mask, AssignedSpecIndex, item0, item1, item2, item3, " +
                "item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15, item16, item17, item18) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_EQUIP_SET, "DELETE FROM character_equipmentsets WHERE setguid=?");
            PrepareStatement(CharStatements.UPD_TRANSMOG_OUTFIT, "UPDATE character_transmog_outfits SET name=?, iconname=?, ignore_mask=?, appearance0=?, appearance1=?, appearance2=?, appearance3=?, " +
                "appearance4=?, appearance5=?, appearance6=?, appearance7=?, appearance8=?, appearance9=?, appearance10=?, appearance11=?, appearance12=?, appearance13=?, appearance14=?, " +
                "appearance15=?, appearance16=?, appearance17=?, appearance18=?, mainHandEnchant=?, offHandEnchant=? WHERE guid=? AND setguid=? AND setindex=?");
            PrepareStatement(CharStatements.INS_TRANSMOG_OUTFIT, "INSERT INTO character_transmog_outfits (guid, setguid, setindex, name, iconname, ignore_mask, appearance0, appearance1, appearance2, " +
                "appearance3, appearance4, appearance5, appearance6, appearance7, appearance8, appearance9, appearance10, appearance11, appearance12, appearance13, appearance14, appearance15, " +
                "appearance16, appearance17, appearance18, mainHandEnchant, offHandEnchant) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_TRANSMOG_OUTFIT, "DELETE FROM character_transmog_outfits WHERE setguid=?");

            // Auras
            PrepareStatement(CharStatements.INS_AURA, "INSERT INTO character_aura (guid, casterGuid, itemGuid, spell, effectMask, recalculateMask, stackCount, maxDuration, remainTime, remainCharges, castItemLevel) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.INS_AURA_EFFECT, "INSERT INTO character_aura_effect (guid, casterGuid, itemGuid, spell, effectMask, effectIndex, amount, baseAmount) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?)");

            // Currency
            PrepareStatement(CharStatements.SEL_PLAYER_CURRENCY, "SELECT Currency, Quantity, WeeklyQuantity, TrackedQuantity, Flags FROM character_currency WHERE CharacterGuid = ?");
            PrepareStatement(CharStatements.UPD_PLAYER_CURRENCY, "UPDATE character_currency SET Quantity = ?, WeeklyQuantity = ?, TrackedQuantity = ?, Flags = ? WHERE CharacterGuid = ? AND Currency = ?");
            PrepareStatement(CharStatements.REP_PLAYER_CURRENCY, "REPLACE INTO character_currency (CharacterGuid, Currency, Quantity, WeeklyQuantity, TrackedQuantity, Flags) VALUES (?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_PLAYER_CURRENCY, "DELETE FROM character_currency WHERE CharacterGuid = ?");

            // Account data
            PrepareStatement(CharStatements.SEL_ACCOUNT_DATA, "SELECT type, time, data FROM account_data WHERE accountId = ?");
            PrepareStatement(CharStatements.REP_ACCOUNT_DATA, "REPLACE INTO account_data (accountId, type, time, data) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_ACCOUNT_DATA, "DELETE FROM account_data WHERE accountId = ?");
            PrepareStatement(CharStatements.SEL_PLAYER_ACCOUNT_DATA, "SELECT type, time, data FROM character_account_data WHERE guid = ?");
            PrepareStatement(CharStatements.REP_PLAYER_ACCOUNT_DATA, "REPLACE INTO character_account_data(guid, type, time, data) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_PLAYER_ACCOUNT_DATA, "DELETE FROM character_account_data WHERE guid = ?");

            // Tutorials
            PrepareStatement(CharStatements.SEL_TUTORIALS, "SELECT tut0, tut1, tut2, tut3, tut4, tut5, tut6, tut7 FROM account_tutorial WHERE accountId = ?");
            PrepareStatement(CharStatements.SEL_HAS_TUTORIALS, "SELECT 1 FROM account_tutorial WHERE accountId = ?");
            PrepareStatement(CharStatements.INS_TUTORIALS, "INSERT INTO account_tutorial(tut0, tut1, tut2, tut3, tut4, tut5, tut6, tut7, accountId) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.UPD_TUTORIALS, "UPDATE account_tutorial SET tut0 = ?, tut1 = ?, tut2 = ?, tut3 = ?, tut4 = ?, tut5 = ?, tut6 = ?, tut7 = ? WHERE accountId = ?");
            PrepareStatement(CharStatements.DEL_TUTORIALS, "DELETE FROM account_tutorial WHERE accountId = ?");

            // Instance saves
            PrepareStatement(CharStatements.INS_INSTANCE_SAVE, "INSERT INTO instance (id, map, resettime, difficulty, completedEncounters, data, entranceId) VALUES (?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.UPD_INSTANCE_DATA, "UPDATE instance SET completedEncounters=?, data=?, entranceId=? WHERE id=?");

            // Game event saves
            PrepareStatement(CharStatements.DEL_GAME_EVENT_SAVE, "DELETE FROM game_event_save WHERE eventEntry = ?");
            PrepareStatement(CharStatements.INS_GAME_EVENT_SAVE, "INSERT INTO game_event_save (eventEntry, state, next_start) VALUES (?, ?, ?)");

            // Game event condition saves
            PrepareStatement(CharStatements.DEL_ALL_GAME_EVENT_CONDITION_SAVE, "DELETE FROM game_event_condition_save WHERE eventEntry = ?");
            PrepareStatement(CharStatements.DEL_GAME_EVENT_CONDITION_SAVE, "DELETE FROM game_event_condition_save WHERE eventEntry = ? AND condition_id = ?");
            PrepareStatement(CharStatements.INS_GAME_EVENT_CONDITION_SAVE, "INSERT INTO game_event_condition_save (eventEntry, condition_id, done) VALUES (?, ?, ?)");

            // Petitions
            PrepareStatement(CharStatements.SEL_PETITION, "SELECT ownerguid, name FROM petition WHERE petitionguid = ?");
            PrepareStatement(CharStatements.SEL_PETITION_SIGNATURE, "SELECT playerguid FROM petition_sign WHERE petitionguid = ?");
            PrepareStatement(CharStatements.DEL_ALL_PETITION_SIGNATURES, "DELETE FROM petition_sign WHERE playerguid = ?");
            PrepareStatement(CharStatements.SEL_PETITION_BY_OWNER, "SELECT petitionguid FROM petition WHERE ownerguid = ?");
            PrepareStatement(CharStatements.SEL_PETITION_SIGNATURES, "SELECT ownerguid, (SELECT COUNT(playerguid) FROM petition_sign WHERE petition_sign.petitionguid = ?) AS signs FROM petition WHERE petitionguid = ?");
            PrepareStatement(CharStatements.SEL_PETITION_SIG_BY_ACCOUNT, "SELECT playerguid FROM petition_sign WHERE player_account = ? AND petitionguid = ?");
            PrepareStatement(CharStatements.SEL_PETITION_OWNER_BY_GUID, "SELECT ownerguid FROM petition WHERE petitionguid = ?");
            PrepareStatement(CharStatements.SEL_PETITION_SIG_BY_GUID, "SELECT ownerguid, petitionguid FROM petition_sign WHERE playerguid = ?");

            // Arena teams
            PrepareStatement(CharStatements.SEL_CHARACTER_ARENAINFO, "SELECT arenaTeamId, weekGames, seasonGames, seasonWins, personalRating FROM arena_team_member WHERE guid = ?");
            PrepareStatement(CharStatements.INS_ARENA_TEAM, "INSERT INTO arena_team (arenaTeamId, name, captainGuid, type, rating, backgroundColor, emblemStyle, emblemColor, borderStyle, borderColor) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.INS_ARENA_TEAM_MEMBER, "INSERT INTO arena_team_member (arenaTeamId, guid) VALUES (?, ?)");
            PrepareStatement(CharStatements.DEL_ARENA_TEAM, "DELETE FROM arena_team where arenaTeamId = ?");
            PrepareStatement(CharStatements.DEL_ARENA_TEAM_MEMBERS, "DELETE FROM arena_team_member WHERE arenaTeamId = ?");
            PrepareStatement(CharStatements.UPD_ARENA_TEAM_CAPTAIN, "UPDATE arena_team SET captainGuid = ? WHERE arenaTeamId = ?");
            PrepareStatement(CharStatements.DEL_ARENA_TEAM_MEMBER, "DELETE FROM arena_team_member WHERE arenaTeamId = ? AND guid = ?");
            PrepareStatement(CharStatements.UPD_ARENA_TEAM_STATS, "UPDATE arena_team SET rating = ?, weekGames = ?, weekWins = ?, seasonGames = ?, seasonWins = ?, rank = ? WHERE arenaTeamId = ?");
            PrepareStatement(CharStatements.UPD_ARENA_TEAM_MEMBER, "UPDATE arena_team_member SET personalRating = ?, weekGames = ?, weekWins = ?, seasonGames = ?, seasonWins = ? WHERE arenaTeamId = ? AND guid = ?");
            PrepareStatement(CharStatements.DEL_CHARACTER_ARENA_STATS, "DELETE FROM character_arena_stats WHERE guid = ?");
            PrepareStatement(CharStatements.REP_CHARACTER_ARENA_STATS, "REPLACE INTO character_arena_stats (guid, slot, matchMakerRating) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.SEL_PLAYER_ARENA_TEAMS, "SELECT arena_team_member.arenaTeamId FROM arena_team_member JOIN arena_team ON arena_team_member.arenaTeamId = arena_team.arenaTeamId WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_ARENA_TEAM_NAME, "UPDATE arena_team SET name = ? WHERE arenaTeamId = ?");

            // Character battleground data
            PrepareStatement(CharStatements.INS_PLAYER_BGDATA, "INSERT INTO character_battleground_data (guid, instanceId, team, joinX, joinY, joinZ, joinO, joinMapId, taxiStart, taxiEnd, mountSpell) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_PLAYER_BGDATA, "DELETE FROM character_battleground_data WHERE guid = ?");

            // Character homebind
            PrepareStatement(CharStatements.INS_PLAYER_HOMEBIND, "INSERT INTO character_homebind (guid, mapId, zoneId, posX, posY, posZ) VALUES (?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.UPD_PLAYER_HOMEBIND, "UPDATE character_homebind SET mapId = ?, zoneId = ?, posX = ?, posY = ?, posZ = ? WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_PLAYER_HOMEBIND, "DELETE FROM character_homebind WHERE guid = ?");

            // Corpse
            PrepareStatement(CharStatements.SEL_CORPSES, "SELECT posX, posY, posZ, orientation, mapId, displayId, itemCache, bytes1, bytes2, flags, dynFlags, time, corpseType, instanceId, guid FROM corpse WHERE mapId = ? AND instanceId = ?");
            PrepareStatement(CharStatements.INS_CORPSE, "INSERT INTO corpse (guid, posX, posY, posZ, orientation, mapId, displayId, itemCache, bytes1, bytes2, flags, dynFlags, time, corpseType, instanceId) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CORPSE, "DELETE FROM corpse WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CORPSES_FROM_MAP, "DELETE cp, c FROM corpse_phases cp INNER JOIN corpse c ON cp.OwnerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?");
            PrepareStatement(CharStatements.SEL_CORPSE_PHASES, "SELECT cp.OwnerGuid, cp.PhaseId FROM corpse_phases cp LEFT JOIN corpse c ON cp.OwnerGuid = c.guid WHERE c.mapId = ? AND c.instanceId = ?");
            PrepareStatement(CharStatements.INS_CORPSE_PHASES, "INSERT INTO corpse_phases (OwnerGuid, PhaseId) VALUES (?, ?)");
            PrepareStatement(CharStatements.DEL_CORPSE_PHASES, "DELETE FROM corpse_phases WHERE OwnerGuid = ?");
            PrepareStatement(CharStatements.SEL_CORPSE_LOCATION, "SELECT mapId, posX, posY, posZ, orientation FROM corpse WHERE guid = ?");

            // Creature respawn
            PrepareStatement(CharStatements.SEL_CREATURE_RESPAWNS, "SELECT guid, respawnTime FROM creature_respawn WHERE mapId = ? AND instanceId = ?");
            PrepareStatement(CharStatements.REP_CREATURE_RESPAWN, "REPLACE INTO creature_respawn (guid, respawnTime, mapId, instanceId) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CREATURE_RESPAWN, "DELETE FROM creature_respawn WHERE guid = ? AND mapId = ? AND instanceId = ?");
            PrepareStatement(CharStatements.DEL_CREATURE_RESPAWN_BY_INSTANCE, "DELETE FROM creature_respawn WHERE mapId = ? AND instanceId = ?");
            PrepareStatement(CharStatements.SEL_MAX_CREATURE_RESPAWNS, "SELECT MAX(respawnTime), instanceId FROM creature_respawn WHERE instanceId > 0 GROUP BY instanceId");

            // Gameobject respawn
            PrepareStatement(CharStatements.SEL_GO_RESPAWNS, "SELECT guid, respawnTime FROM gameobject_respawn WHERE mapId = ? AND instanceId = ?");
            PrepareStatement(CharStatements.REP_GO_RESPAWN, "REPLACE INTO gameobject_respawn (guid, respawnTime, mapId, instanceId) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GO_RESPAWN, "DELETE FROM gameobject_respawn WHERE guid = ? AND mapId = ? AND instanceId = ?");
            PrepareStatement(CharStatements.DEL_GO_RESPAWN_BY_INSTANCE, "DELETE FROM gameobject_respawn WHERE mapId = ? AND instanceId = ?");

            // GM Bug
            PrepareStatement(CharStatements.SEL_GM_BUGS, "SELECT id, playerGuid, note, createTime, mapId, posX, posY, posZ, facing, closedBy, assignedTo, comment FROM gm_bug");
            PrepareStatement(CharStatements.REP_GM_BUG, "REPLACE INTO gm_bug (id, playerGuid, note, createTime, mapId, posX, posY, posZ, facing, closedBy, assignedTo, comment) VALUES (?, ?, ?, UNIX_TIMESTAMP(NOW()), ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GM_BUG, "DELETE FROM gm_bug WHERE id = ?");
            PrepareStatement(CharStatements.DEL_ALL_GM_BUGS, "TRUNCATE TABLE gm_bug");

            // GM Complaint
            PrepareStatement(CharStatements.SEL_GM_COMPLAINTS, "SELECT id, playerGuid, note, createTime, mapId, posX, posY, posZ, facing, targetCharacterGuid, complaintType, reportLineIndex, assignedTo, closedBy, comment FROM gm_complaint");
            PrepareStatement(CharStatements.REP_GM_COMPLAINT, "REPLACE INTO gm_complaint (id, playerGuid, note, createTime, mapId, posX, posY, posZ, facing, targetCharacterGuid, complaintType, reportLineIndex, assignedTo, closedBy, comment) VALUES (?, ?, ?, UNIX_TIMESTAMP(NOW()), ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GM_COMPLAINT, "DELETE FROM gm_complaint WHERE id = ?");
            PrepareStatement(CharStatements.SEL_GM_COMPLAINT_CHATLINES, "SELECT timestamp, text FROM gm_complaint_chatlog WHERE complaintId = ? ORDER BY lineId ASC");
            PrepareStatement(CharStatements.INS_GM_COMPLAINT_CHATLINE, "INSERT INTO gm_complaint_chatlog (complaintId, lineId, timestamp, text) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GM_COMPLAINT_CHATLOG, "DELETE FROM gm_complaint_chatlog WHERE complaintId = ?");
            PrepareStatement(CharStatements.DEL_ALL_GM_COMPLAINTS, "TRUNCATE TABLE gm_complaint");
            PrepareStatement(CharStatements.DEL_ALL_GM_COMPLAINT_CHATLOGS, "TRUNCATE TABLE gm_complaint_chatlog");

            // GM Suggestion
            PrepareStatement(CharStatements.SEL_GM_SUGGESTIONS, "SELECT id, playerGuid, note, createTime, mapId, posX, posY, posZ, facing, closedBy, assignedTo, comment FROM gm_suggestion");
            PrepareStatement(CharStatements.REP_GM_SUGGESTION, "REPLACE INTO gm_suggestion (id, playerGuid, note, createTime, mapId, posX, posY, posZ, facing, closedBy, assignedTo, comment) VALUES (?, ?, ?, UNIX_TIMESTAMP(NOW()), ?, ?, ?, ?, ?, ? ,? ,?)");
            PrepareStatement(CharStatements.DEL_GM_SUGGESTION, "DELETE FROM gm_suggestion WHERE id = ?");
            PrepareStatement(CharStatements.DEL_ALL_GM_SUGGESTIONS, "TRUNCATE TABLE gm_suggestion");

            // LFG Data
            PrepareStatement(CharStatements.INS_LFG_DATA, "INSERT INTO lfg_data (guid, dungeon, state) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.DEL_LFG_DATA, "DELETE FROM lfg_data WHERE guid = ?");

            // Player saving
            PrepareStatement(CharStatements.INS_CHARACTER, "INSERT INTO characters (guid, account, name, race, class, gender, level, xp, money, skin, face, hairStyle, hairColor, facialStyle, customDisplay1, customDisplay2, customDisplay3, inventorySlots, bankSlots, restState, playerFlags, playerFlagsEx, " +
                "map, instance_id, dungeonDifficulty, raidDifficulty, legacyRaidDifficulty, position_x, position_y, position_z, orientation, trans_x, trans_y, trans_z, trans_o, transguid, " +
                "taximask, cinematic, totaltime, leveltime, rest_bonus, logout_time, is_logout_resting, resettalents_cost, resettalents_time, primarySpecialization, " +
                "extra_flags, stable_slots, at_login, zone, death_expire_time, taxi_path, totalKills, todayKills, yesterdayKills, chosenTitle, watchedFaction, drunk, health, power1, power2, power3, " +
                "power4, power5, power6, latency, activeTalentGroup, lootSpecId, exploredZones, equipmentCache, knownTitles, actionBars, grantableLevels, lastLoginBuild) VALUES " +
                "(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)");
            PrepareStatement(CharStatements.UPD_CHARACTER, "UPDATE characters SET name=?,race=?,class=?,gender=?,level=?,xp=?,money=?,skin=?,face=?,hairStyle=?,hairColor=?,facialStyle=?,customDisplay1=?,customDisplay2=?,customDisplay3=?,inventorySlots=?,bankSlots=?,restState=?,playerFlags=?,playerFlagsEx=?," +
                "map=?,instance_id=?,dungeonDifficulty=?,raidDifficulty=?,legacyRaidDifficulty=?,position_x=?,position_y=?,position_z=?,orientation=?,trans_x=?,trans_y=?,trans_z=?,trans_o=?,transguid=?,taximask=?,cinematic=?,totaltime=?,leveltime=?,rest_bonus=?," +
                "logout_time=?,is_logout_resting=?,resettalents_cost=?,resettalents_time=?,primarySpecialization=?,extra_flags=?,stable_slots=?,at_login=?,zone=?,death_expire_time=?,taxi_path=?," +
                "totalKills=?,todayKills=?,yesterdayKills=?,chosenTitle=?," +
                "watchedFaction=?,drunk=?,health=?,power1=?,power2=?,power3=?,power4=?,power5=?,power6=?,latency=?,activeTalentGroup=?,lootSpecId=?,exploredZones=?," +
                "equipmentCache=?,knownTitles=?,actionBars=?,grantableLevels=?,online=?,honor=?,honorLevel=?,honorRestState=?,honorRestBonus=?,lastLoginBuild=? WHERE guid=?");

            PrepareStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG, "UPDATE characters SET at_login = at_login | ? WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_REM_AT_LOGIN_FLAG, "UPDATE characters set at_login = at_login & ~ ? WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_ALL_AT_LOGIN_FLAGS, "UPDATE characters SET at_login = at_login | ?");
            PrepareStatement(CharStatements.INS_BUG_REPORT, "INSERT INTO bugreport (type, content) VALUES(?, ?)");
            PrepareStatement(CharStatements.UPD_PETITION_NAME, "UPDATE petition SET name = ? WHERE petitionguid = ?");
            PrepareStatement(CharStatements.INS_PETITION_SIGNATURE, "INSERT INTO petition_sign (ownerguid, petitionguid, playerguid, player_account) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.UPD_ACCOUNT_ONLINE, "UPDATE characters SET online = 0 WHERE account = ?");
            PrepareStatement(CharStatements.INS_GROUP, "INSERT INTO groups (guid, leaderGuid, lootMethod, looterGuid, lootThreshold, icon1, icon2, icon3, icon4, icon5, icon6, icon7, icon8, groupType, difficulty, raidDifficulty, legacyRaidDifficulty, masterLooterGuid) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.INS_GROUP_MEMBER, "INSERT INTO group_member (guid, memberGuid, memberFlags, subgroup, roles) VALUES(?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GROUP_MEMBER, "DELETE FROM group_member WHERE memberGuid = ?");
            PrepareStatement(CharStatements.DEL_GROUP_INSTANCE_PERM_BINDING, "DELETE FROM group_instance WHERE guid = ? AND instance = ?");
            PrepareStatement(CharStatements.UPD_GROUP_LEADER, "UPDATE groups SET leaderGuid = ? WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_GROUP_TYPE, "UPDATE groups SET groupType = ? WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_GROUP_MEMBER_SUBGROUP, "UPDATE group_member SET subgroup = ? WHERE memberGuid = ?");
            PrepareStatement(CharStatements.UPD_GROUP_MEMBER_FLAG, "UPDATE group_member SET memberFlags = ? WHERE memberGuid = ?");
            PrepareStatement(CharStatements.UPD_GROUP_DIFFICULTY, "UPDATE groups SET difficulty = ? WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_GROUP_RAID_DIFFICULTY, "UPDATE groups SET raidDifficulty = ? WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_GROUP_LEGACY_RAID_DIFFICULTY, "UPDATE groups SET legacyRaidDifficulty = ? WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_INVALID_SPELL_SPELLS, "DELETE FROM character_spell WHERE spell = ?");
            PrepareStatement(CharStatements.UPD_DELETE_INFO, "UPDATE characters SET deleteInfos_Name = name, deleteInfos_Account = account, deleteDate = UNIX_TIMESTAMP(), name = '', account = 0 WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_RESTORE_DELETE_INFO, "UPDATE characters SET name = ?, account = ?, deleteDate = NULL, deleteInfos_Name = NULL, deleteInfos_Account = NULL WHERE deleteDate IS NOT NULL AND guid = ?");
            PrepareStatement(CharStatements.UPD_ZONE, "UPDATE characters SET zone = ? WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_LEVEL, "UPDATE characters SET level = ?, xp = 0 WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_INVALID_ACHIEV_PROGRESS_CRITERIA, "DELETE FROM character_achievement_progress WHERE criteria = ?");
            PrepareStatement(CharStatements.DEL_INVALID_ACHIEV_PROGRESS_CRITERIA_GUILD, "DELETE FROM guild_achievement_progress WHERE criteria = ?");
            PrepareStatement(CharStatements.DEL_INVALID_ACHIEVMENT, "DELETE FROM character_achievement WHERE achievement = ?");
            PrepareStatement(CharStatements.DEL_INVALID_PET_SPELL, "DELETE FROM pet_spell WHERE spell = ?");
            PrepareStatement(CharStatements.DEL_GROUP_INSTANCE_BY_INSTANCE, "DELETE FROM group_instance WHERE instance = ?");
            PrepareStatement(CharStatements.DEL_GROUP_INSTANCE_BY_GUID, "DELETE FROM group_instance WHERE guid = ? AND instance = ?");
            PrepareStatement(CharStatements.REP_GROUP_INSTANCE, "REPLACE INTO group_instance (guid, instance, permanent) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.UPD_INSTANCE_RESETTIME, "UPDATE instance SET resettime = ? WHERE id = ?");
            PrepareStatement(CharStatements.UPD_GLOBAL_INSTANCE_RESETTIME, "UPDATE instance_reset SET resettime = ? WHERE mapid = ? AND difficulty = ?");
            PrepareStatement(CharStatements.UPD_CHAR_ONLINE, "UPDATE characters SET online = 1 WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_CHAR_NAME_AT_LOGIN, "UPDATE characters SET name = ?, at_login = ? WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_WORLDSTATE, "UPDATE worldstates SET value = ? WHERE entry = ?");
            PrepareStatement(CharStatements.INS_WORLDSTATE, "INSERT INTO worldstates (entry, value) VALUES (?, ?)");
            PrepareStatement(CharStatements.DEL_CHAR_INSTANCE_BY_INSTANCE_GUID, "DELETE FROM character_instance WHERE guid = ? AND instance = ?");
            PrepareStatement(CharStatements.UPD_CHAR_INSTANCE, "UPDATE character_instance SET instance = ?, permanent = ?, extendState = ? WHERE guid = ? AND instance = ?");
            PrepareStatement(CharStatements.INS_CHAR_INSTANCE, "INSERT INTO character_instance (guid, instance, permanent, extendState) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.UPD_GENDER_AND_APPEARANCE, "UPDATE characters SET gender = ?, skin = ?, face = ?, hairStyle = ?, hairColor = ?, facialStyle = ?, customDisplay1 = ?, customDisplay2 = ?, customDisplay3 = ? WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHARACTER_SKILL, "DELETE FROM character_skills WHERE guid = ? AND skill = ?");
            PrepareStatement(CharStatements.UPD_CHARACTER_SOCIAL_FLAGS, "UPDATE character_social SET flags = ? WHERE guid = ? AND friend = ?");
            PrepareStatement(CharStatements.INS_CHARACTER_SOCIAL, "INSERT INTO character_social (guid, friend, flags) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CHARACTER_SOCIAL, "DELETE FROM character_social WHERE guid = ? AND friend = ?");
            PrepareStatement(CharStatements.UPD_CHARACTER_SOCIAL_NOTE, "UPDATE character_social SET note = ? WHERE guid = ? AND friend = ?");
            PrepareStatement(CharStatements.UPD_CHARACTER_POSITION, "UPDATE characters SET position_x = ?, position_y = ?, position_z = ?, orientation = ?, map = ?, zone = ?, trans_x = 0, trans_y = 0, trans_z = 0, transguid = 0, taxi_path = '' WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_AURA_FROZEN, "SELECT characters.name, character_aura.remainTime FROM characters LEFT JOIN character_aura ON (characters.guid = character_aura.guid) WHERE character_aura.spell = 9454");
            PrepareStatement(CharStatements.SEL_CHARACTER_ONLINE, "SELECT name, account, map, zone FROM characters WHERE online > 0");
            PrepareStatement(CharStatements.SEL_CHAR_DEL_INFO_BY_GUID, "SELECT guid, deleteInfos_Name, deleteInfos_Account, deleteDate FROM characters WHERE deleteDate IS NOT NULL AND guid = ?");
            PrepareStatement(CharStatements.SEL_CHAR_DEL_INFO_BY_NAME, "SELECT guid, deleteInfos_Name, deleteInfos_Account, deleteDate FROM characters WHERE deleteDate IS NOT NULL AND deleteInfos_Name LIKE CONCAT('%%', ?, '%%')");
            PrepareStatement(CharStatements.SEL_CHAR_DEL_INFO, "SELECT guid, deleteInfos_Name, deleteInfos_Account, deleteDate FROM characters WHERE deleteDate IS NOT NULL");
            PrepareStatement(CharStatements.SEL_CHARS_BY_ACCOUNT_ID, "SELECT guid FROM characters WHERE account = ?");
            PrepareStatement(CharStatements.SEL_CHAR_PINFO, "SELECT totaltime, level, money, account, race, class, map, zone, gender, health, playerFlags FROM characters WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_PINFO_BANS, "SELECT unbandate, bandate = unbandate, bannedby, banreason FROM character_banned WHERE guid = ? AND active ORDER BY bandate ASC LIMIT 1");
            //0: lowGUID
            PrepareStatement(CharStatements.SEL_PINFO_MAILS, "SELECT SUM(CASE WHEN (checked & 1) THEN 1 ELSE 0 END) AS 'readmail', COUNT(*) AS 'totalmail' FROM mail WHERE `receiver` = ?");
            //0: lowGUID
            PrepareStatement(CharStatements.SEL_PINFO_XP, "SELECT a.xp, b.guid FROM characters a LEFT JOIN guild_member b ON a.guid = b.guid WHERE a.guid = ?");
            PrepareStatement(CharStatements.SEL_CHAR_HOMEBIND, "SELECT mapId, zoneId, posX, posY, posZ FROM character_homebind WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHAR_GUID_NAME_BY_ACC, "SELECT guid, name FROM characters WHERE account = ?");
            PrepareStatement(CharStatements.SEL_POOL_QUEST_SAVE, "SELECT quest_id FROM pool_quest_save WHERE pool_id = ?");
            PrepareStatement(CharStatements.SEL_CHAR_CUSTOMIZE_INFO, "SELECT name, race, class, gender, at_login FROM characters WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHAR_RACE_OR_FACTION_CHANGE_INFOS, "SELECT at_login, knownTitles FROM characters WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_INSTANCE, "SELECT data, completedEncounters, entranceId FROM instance WHERE map = ? AND id = ?");
            PrepareStatement(CharStatements.SEL_PERM_BIND_BY_INSTANCE, "SELECT guid FROM character_instance WHERE instance = ? and permanent = 1");
            PrepareStatement(CharStatements.SEL_CHAR_COD_ITEM_MAIL, "SELECT id, messageType, mailTemplateId, sender, subject, body, money, has_items FROM mail WHERE receiver = ? AND has_items <> 0 AND cod <> 0");
            PrepareStatement(CharStatements.SEL_CHAR_SOCIAL, "SELECT DISTINCT guid FROM character_social WHERE friend = ?");
            PrepareStatement(CharStatements.SEL_CHAR_OLD_CHARS, "SELECT guid, deleteInfos_Account FROM characters WHERE deleteDate IS NOT NULL AND deleteDate < ?");
            PrepareStatement(CharStatements.SEL_ARENA_TEAM_ID_BY_PLAYER_GUID, "SELECT arena_team_member.arenateamid FROM arena_team_member JOIN arena_team ON arena_team_member.arenateamid = arena_team.arenateamid WHERE guid = ? AND type = ? LIMIT 1");
            PrepareStatement(CharStatements.SEL_MAIL, "SELECT id, messageType, sender, receiver, subject, body, has_items, expire_time, deliver_time, money, cod, checked, stationery, mailTemplateId FROM mail WHERE receiver = ? ORDER BY id DESC");
            PrepareStatement(CharStatements.DEL_CHAR_AURA_FROZEN, "DELETE FROM character_aura WHERE spell = 9454 AND guid = ?");
            PrepareStatement(CharStatements.SEL_CHAR_INVENTORY_COUNT_ITEM, "SELECT COUNT(itemEntry) FROM character_inventory ci INNER JOIN item_instance ii ON ii.guid = ci.item WHERE itemEntry = ?");
            PrepareStatement(CharStatements.SEL_MAIL_COUNT_ITEM, "SELECT COUNT(itemEntry) FROM mail_items mi INNER JOIN item_instance ii ON ii.guid = mi.item_guid WHERE itemEntry = ?");
            PrepareStatement(CharStatements.SEL_AUCTIONHOUSE_COUNT_ITEM, "SELECT COUNT(itemEntry) FROM auctionhouse ah INNER JOIN item_instance ii ON ii.guid = ah.itemguid WHERE itemEntry = ?");
            PrepareStatement(CharStatements.SEL_GUILD_BANK_COUNT_ITEM, "SELECT COUNT(itemEntry) FROM guild_bank_item gbi INNER JOIN item_instance ii ON ii.guid = gbi.item_guid WHERE itemEntry = ?");
            PrepareStatement(CharStatements.SEL_CHAR_INVENTORY_ITEM_BY_ENTRY, "SELECT ci.item, cb.slot AS bag, ci.slot, ci.guid, c.account, c.name FROM characters c " +
                "INNER JOIN character_inventory ci ON ci.guid = c.guid " +
                "INNER JOIN item_instance ii ON ii.guid = ci.item " +
                "LEFT JOIN character_inventory cb ON cb.item = ci.bag WHERE ii.itemEntry = ? LIMIT ?");
            PrepareStatement(CharStatements.SEL_MAIL_ITEMS_BY_ENTRY, "SELECT mi.item_guid, m.sender, m.receiver, cs.account, cs.name, cr.account, cr.name " +
                "FROM mail m INNER JOIN mail_items mi ON mi.mail_id = m.id INNER JOIN item_instance ii ON ii.guid = mi.item_guid " +
                "INNER JOIN characters cs ON cs.guid = m.sender INNER JOIN characters cr ON cr.guid = m.receiver WHERE ii.itemEntry = ? LIMIT ?");
            PrepareStatement(CharStatements.SEL_AUCTIONHOUSE_ITEM_BY_ENTRY, "SELECT ah.itemguid, ah.itemowner, c.account, c.name FROM auctionhouse ah INNER JOIN characters c ON c.guid = ah.itemowner INNER JOIN item_instance ii ON ii.guid = ah.itemguid WHERE ii.itemEntry = ? LIMIT ?");
            PrepareStatement(CharStatements.SEL_GUILD_BANK_ITEM_BY_ENTRY, "SELECT gi.item_guid, gi.guildid, g.name FROM guild_bank_item gi INNER JOIN guild g ON g.guildid = gi.guildid INNER JOIN item_instance ii ON ii.guid = gi.item_guid WHERE ii.itemEntry = ? LIMIT ?");
            PrepareStatement(CharStatements.DEL_CHAR_ACHIEVEMENT, "DELETE FROM character_achievement WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_PROGRESS, "DELETE FROM character_achievement_progress WHERE guid = ?");
            PrepareStatement(CharStatements.INS_CHAR_ACHIEVEMENT, "INSERT INTO character_achievement (guid, achievement, date) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_PROGRESS_BY_CRITERIA, "DELETE FROM character_achievement_progress WHERE guid = ? AND criteria = ?");
            PrepareStatement(CharStatements.INS_CHAR_ACHIEVEMENT_PROGRESS, "INSERT INTO character_achievement_progress (guid, criteria, counter, date) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CHAR_REPUTATION_BY_FACTION, "DELETE FROM character_reputation WHERE guid = ? AND faction = ?");
            PrepareStatement(CharStatements.INS_CHAR_REPUTATION_BY_FACTION, "INSERT INTO character_reputation (guid, faction, standing, flags) VALUES (?, ?, ? , ?)");
            PrepareStatement(CharStatements.DEL_ITEM_REFUND_INSTANCE, "DELETE FROM item_refund_instance WHERE item_guid = ?");
            PrepareStatement(CharStatements.INS_ITEM_REFUND_INSTANCE, "INSERT INTO item_refund_instance (item_guid, player_guid, paidMoney, paidExtendedCost) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GROUP, "DELETE FROM groups WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_GROUP_MEMBER_ALL, "DELETE FROM group_member WHERE guid = ?");
            PrepareStatement(CharStatements.INS_CHAR_GIFT, "INSERT INTO character_gifts (guid, item_guid, entry, flags) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_INSTANCE_BY_INSTANCE, "DELETE FROM instance WHERE id = ?");
            PrepareStatement(CharStatements.DEL_CHAR_INSTANCE_BY_INSTANCE, "DELETE FROM character_instance WHERE instance = ?");
            PrepareStatement(CharStatements.DEL_EXPIRED_CHAR_INSTANCE_BY_MAP_DIFF, "DELETE FROM character_instance USING character_instance LEFT JOIN instance ON character_instance.instance = id WHERE (extendState = 0 or permanent = 0) and map = ? and difficulty = ?");
            PrepareStatement(CharStatements.DEL_GROUP_INSTANCE_BY_MAP_DIFF, "DELETE FROM group_instance USING group_instance LEFT JOIN instance ON group_instance.instance = id WHERE map = ? and difficulty = ?");
            PrepareStatement(CharStatements.DEL_EXPIRED_INSTANCE_BY_MAP_DIFF, "DELETE FROM instance WHERE map = ? and difficulty = ? and (SELECT guid FROM character_instance WHERE extendState != 0 AND instance = id LIMIT 1) IS NULL");
            PrepareStatement(CharStatements.UPD_EXPIRE_CHAR_INSTANCE_BY_MAP_DIFF, "UPDATE character_instance LEFT JOIN instance ON character_instance.instance = id SET extendState = extendState-1 WHERE map = ? and difficulty = ?");
            PrepareStatement(CharStatements.DEL_MAIL_ITEM_BY_ID, "DELETE FROM mail_items WHERE mail_id = ?");
            PrepareStatement(CharStatements.INS_PETITION, "INSERT INTO petition (ownerguid, petitionguid, name) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.DEL_PETITION_BY_GUID, "DELETE FROM petition WHERE petitionguid = ?");
            PrepareStatement(CharStatements.DEL_PETITION_SIGNATURE_BY_GUID, "DELETE FROM petition_sign WHERE petitionguid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_DECLINED_NAME, "DELETE FROM character_declinedname WHERE guid = ?");
            PrepareStatement(CharStatements.INS_CHAR_DECLINED_NAME, "INSERT INTO character_declinedname (guid, genitive, dative, accusative, instrumental, prepositional) VALUES (?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.UPD_CHAR_RACE, "UPDATE characters SET race = ? WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_SKILL_LANGUAGES, "DELETE FROM character_skills WHERE skill IN (98, 113, 759, 111, 313, 109, 115, 315, 673, 137) AND guid = ?");
            PrepareStatement(CharStatements.INS_CHAR_SKILL_LANGUAGE, "INSERT INTO `character_skills` (guid, skill, value, max) VALUES (?, ?, 300, 300)");
            PrepareStatement(CharStatements.UPD_CHAR_TAXI_PATH, "UPDATE characters SET taxi_path = '' WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_CHAR_TAXIMASK, "UPDATE characters SET taximask = ? WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_QUESTSTATUS, "DELETE FROM character_queststatus WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES, "DELETE FROM character_queststatus_objectives WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA, "DELETE FROM character_queststatus_objectives_criteria WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS, "DELETE FROM character_queststatus_objectives_criteria_progress WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS_BY_CRITERIA, "DELETE FROM character_queststatus_objectives_criteria_progress WHERE guid = ? AND criteriaId = ?");
            PrepareStatement(CharStatements.DEL_CHAR_SOCIAL_BY_GUID, "DELETE FROM character_social WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_SOCIAL_BY_FRIEND, "DELETE FROM character_social WHERE friend = ?");
            PrepareStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_BY_ACHIEVEMENT, "DELETE FROM character_achievement WHERE achievement = ? AND guid = ?");
            PrepareStatement(CharStatements.UPD_CHAR_ACHIEVEMENT, "UPDATE character_achievement SET achievement = ? where achievement = ? AND guid = ?");
            PrepareStatement(CharStatements.UPD_CHAR_INVENTORY_FACTION_CHANGE, "UPDATE item_instance ii, character_inventory ci SET ii.itemEntry = ? WHERE ii.itemEntry = ? AND ci.guid = ? AND ci.item = ii.guid");
            PrepareStatement(CharStatements.DEL_CHAR_SPELL_BY_SPELL, "DELETE FROM character_spell WHERE spell = ? AND guid = ?");
            PrepareStatement(CharStatements.UPD_CHAR_SPELL_FACTION_CHANGE, "UPDATE character_spell SET spell = ? where spell = ? AND guid = ?");
            PrepareStatement(CharStatements.SEL_CHAR_REP_BY_FACTION, "SELECT standing FROM character_reputation WHERE faction = ? AND guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_REP_BY_FACTION, "DELETE FROM character_reputation WHERE faction = ? AND guid = ?");
            PrepareStatement(CharStatements.UPD_CHAR_REP_FACTION_CHANGE, "UPDATE character_reputation SET faction = ?, standing = ? WHERE faction = ? AND guid = ?");
            PrepareStatement(CharStatements.UPD_CHAR_TITLES_FACTION_CHANGE, "UPDATE characters SET knownTitles = ? WHERE guid = ?");
            PrepareStatement(CharStatements.RES_CHAR_TITLES_FACTION_CHANGE, "UPDATE characters SET chosenTitle = 0 WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_SPELL_COOLDOWNS, "DELETE FROM character_spell_cooldown WHERE guid = ?");
            PrepareStatement(CharStatements.INS_CHAR_SPELL_COOLDOWN, "INSERT INTO character_spell_cooldown (guid, spell, item, time, categoryId, categoryEnd) VALUES (?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CHAR_SPELL_CHARGES, "DELETE FROM character_spell_charges WHERE guid = ?");
            PrepareStatement(CharStatements.INS_CHAR_SPELL_CHARGES, "INSERT INTO character_spell_charges (guid, categoryId, rechargeStart, rechargeEnd) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CHARACTER, "DELETE FROM characters WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_ACTION, "DELETE FROM character_action WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_AURA, "DELETE FROM character_aura WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_AURA_EFFECT, "DELETE FROM character_aura_effect WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_GIFT, "DELETE FROM character_gifts WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_INSTANCE, "DELETE FROM character_instance WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_INVENTORY, "DELETE FROM character_inventory WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED, "DELETE FROM character_queststatus_rewarded WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_REPUTATION, "DELETE FROM character_reputation WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_SPELL, "DELETE FROM character_spell WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_MAIL, "DELETE FROM mail WHERE receiver = ?");
            PrepareStatement(CharStatements.DEL_MAIL_ITEMS, "DELETE FROM mail_items WHERE receiver = ?");
            PrepareStatement(CharStatements.DEL_CHAR_ACHIEVEMENTS, "DELETE FROM character_achievement WHERE guid = ? AND achievement NOT IN (456,457,458,459,460,461,462,463,464,465,466,467,1400,1402,1404,1405,1406,1407,1408,1409,1410,1411,1412,1413,1414,1415,1416,1417,1418,1419,1420,1421,1422,1423,1424,1425,1426,1427,1463,3117,3259,4078,4576,4998,4999,5000,5001,5002,5003,5004,5005,5006,5007,5008,5381,5382,5383,5384,5385,5386,5387,5388,5389,5390,5391,5392,5393,5394,5395,5396,6433,6523,6524,6743,6744,6745,6746,6747,6748,6749,6750,6751,6752,6829,6859,6860,6861,6862,6863,6864,6865,6866,6867,6868,6869,6870,6871,6872,6873)");
            PrepareStatement(CharStatements.DEL_CHAR_EQUIPMENTSETS, "DELETE FROM character_equipmentsets WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_TRANSMOG_OUTFITS, "DELETE FROM character_transmog_outfits WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_GUILD_EVENTLOG_BY_PLAYER, "DELETE FROM guild_eventlog WHERE PlayerGuid1 = ? OR PlayerGuid2 = ?");
            PrepareStatement(CharStatements.DEL_GUILD_BANK_EVENTLOG_BY_PLAYER, "DELETE FROM guild_bank_eventlog WHERE PlayerGuid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_GLYPHS, "DELETE FROM character_glyphs WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_TALENT, "DELETE FROM character_talent WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_PVP_TALENT, "DELETE FROM character_pvp_talent WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_SKILLS, "DELETE FROM character_skills WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_CHAR_MONEY, "UPDATE characters SET money = ? WHERE guid = ?");
            PrepareStatement(CharStatements.INS_CHAR_ACTION, "INSERT INTO character_action (guid, spec, button, action, type) VALUES (?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.UPD_CHAR_ACTION, "UPDATE character_action SET action = ?, type = ? WHERE guid = ? AND button = ? AND spec = ?");
            PrepareStatement(CharStatements.DEL_CHAR_ACTION_BY_BUTTON_SPEC, "DELETE FROM character_action WHERE guid = ? and button = ? and spec = ?");
            PrepareStatement(CharStatements.DEL_CHAR_INVENTORY_BY_ITEM, "DELETE FROM character_inventory WHERE item = ?");
            PrepareStatement(CharStatements.DEL_CHAR_INVENTORY_BY_BAG_SLOT, "DELETE FROM character_inventory WHERE bag = ? AND slot = ? AND guid = ?");
            PrepareStatement(CharStatements.UPD_MAIL, "UPDATE mail SET has_items = ?, expire_time = ?, deliver_time = ?, money = ?, cod = ?, checked = ? WHERE id = ?");
            PrepareStatement(CharStatements.REP_CHAR_QUESTSTATUS, "REPLACE INTO character_queststatus (guid, quest, status, timer) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CHAR_QUESTSTATUS_BY_QUEST, "DELETE FROM character_queststatus WHERE guid = ? AND quest = ?");
            PrepareStatement(CharStatements.REP_CHAR_QUESTSTATUS_OBJECTIVES, "REPLACE INTO character_queststatus_objectives (guid, quest, objective, data) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_BY_QUEST, "DELETE FROM character_queststatus_objectives WHERE guid = ? AND quest = ?");
            PrepareStatement(CharStatements.INS_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA, "INSERT INTO character_queststatus_objectives_criteria (guid, questObjectiveId) VALUES (?, ?)");
            PrepareStatement(CharStatements.INS_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS, "INSERT INTO character_queststatus_objectives_criteria_progress (guid, criteriaId, counter, date) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.INS_CHAR_QUESTSTATUS_REWARDED, "INSERT IGNORE INTO character_queststatus_rewarded (guid, quest, active) VALUES (?, ?, 1)");
            PrepareStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED_BY_QUEST, "DELETE FROM character_queststatus_rewarded WHERE guid = ? AND quest = ?");
            PrepareStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_FACTION_CHANGE, "UPDATE character_queststatus_rewarded SET quest = ? WHERE quest = ? AND guid = ?");
            PrepareStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_ACTIVE, "UPDATE character_queststatus_rewarded SET active = 1 WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_ACTIVE_BY_QUEST, "UPDATE character_queststatus_rewarded SET active = 0 WHERE quest = ? AND guid = ?");
            PrepareStatement(CharStatements.DEL_INVALID_QUEST_PROGRESS_CRITERIA, "DELETE FROM character_queststatus_objectives_criteria WHERE questObjectiveId = ?");
            PrepareStatement(CharStatements.DEL_CHAR_SKILL_BY_SKILL, "DELETE FROM character_skills WHERE guid = ? AND skill = ?");
            PrepareStatement(CharStatements.INS_CHAR_SKILLS, "INSERT INTO character_skills (guid, skill, value, max) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.UPD_CHAR_SKILLS, "UPDATE character_skills SET value = ?, max = ? WHERE guid = ? AND skill = ?");
            PrepareStatement(CharStatements.INS_CHAR_SPELL, "INSERT INTO character_spell (guid, spell, active, disabled) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CHAR_STATS, "DELETE FROM character_stats WHERE guid = ?");
            PrepareStatement(CharStatements.INS_CHAR_STATS, "INSERT INTO character_stats (guid, maxhealth, maxpower1, maxpower2, maxpower3, maxpower4, maxpower5, maxpower6, strength, agility, stamina, intellect, " +
                "armor, resHoly, resFire, resNature, resFrost, resShadow, resArcane, blockPct, dodgePct, parryPct, critPct, rangedCritPct, spellCritPct, attackPower, rangedAttackPower, " +
                "spellPower, resilience) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_PETITION_BY_OWNER, "DELETE FROM petition WHERE ownerguid = ?");
            PrepareStatement(CharStatements.DEL_PETITION_SIGNATURE_BY_OWNER, "DELETE FROM petition_sign WHERE ownerguid = ?");
            PrepareStatement(CharStatements.INS_CHAR_GLYPHS, "INSERT INTO character_glyphs VALUES(?, ?, ?)");
            PrepareStatement(CharStatements.INS_CHAR_TALENT, "INSERT INTO character_talent (guid, talentId, talentGroup) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.INS_CHAR_PVP_TALENT, "INSERT INTO character_pvp_talent (guid, talentId0, talentId1, talentId2, talentId3, talentGroup) VALUES (?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.UPD_CHAR_LIST_SLOT, "UPDATE characters SET slot = ? WHERE guid = ? AND account = ?");
            PrepareStatement(CharStatements.INS_CHAR_FISHINGSTEPS, "INSERT INTO character_fishingsteps (guid, fishingSteps) VALUES (?, ?)");
            PrepareStatement(CharStatements.DEL_CHAR_FISHINGSTEPS, "DELETE FROM character_fishingsteps WHERE guid = ?");

            // Void Storage
            PrepareStatement(CharStatements.SEL_CHAR_VOID_STORAGE, "SELECT itemId, itemEntry, slot, creatorGuid, randomPropertyType, randomProperty, suffixFactor, upgradeId, fixedScalingLevel, artifactKnowledgeLevel, context, bonusListIDs FROM character_void_storage WHERE playerGuid = ?");
            PrepareStatement(CharStatements.REP_CHAR_VOID_STORAGE_ITEM, "REPLACE INTO character_void_storage (itemId, playerGuid, itemEntry, slot, creatorGuid, randomPropertyType, randomProperty, suffixFactor, upgradeId, fixedScalingLevel, artifactKnowledgeLevel, context, bonusListIDs) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CHAR_VOID_STORAGE_ITEM_BY_CHAR_GUID, "DELETE FROM character_void_storage WHERE playerGuid = ?");
            PrepareStatement(CharStatements.DEL_CHAR_VOID_STORAGE_ITEM_BY_SLOT, "DELETE FROM character_void_storage WHERE slot = ? AND playerGuid = ?");

            // CompactUnitFrame profiles
            PrepareStatement(CharStatements.SEL_CHAR_CUF_PROFILES, "SELECT id, name, frameHeight, frameWidth, sortBy, healthText, boolOptions, topPoint, bottomPoint, leftPoint, topOffset, bottomOffset, leftOffset FROM character_cuf_profiles WHERE guid = ?");
            PrepareStatement(CharStatements.REP_CHAR_CUF_PROFILES, "REPLACE INTO character_cuf_profiles (guid, id, name, frameHeight, frameWidth, sortBy, healthText, boolOptions, topPoint, bottomPoint, leftPoint, topOffset, bottomOffset, leftOffset) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CHAR_CUF_PROFILES_BY_ID, "DELETE FROM character_cuf_profiles WHERE guid = ? AND id = ?");
            PrepareStatement(CharStatements.DEL_CHAR_CUF_PROFILES, "DELETE FROM character_cuf_profiles WHERE guid = ?");

            // Guild Finder
            PrepareStatement(CharStatements.REP_GUILD_FINDER_APPLICANT, "REPLACE INTO guild_finder_applicant (guildId, playerGuid, availability, classRole, interests, comment, submitTime) VALUES(?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GUILD_FINDER_APPLICANT, "DELETE FROM guild_finder_applicant WHERE guildId = ? AND playerGuid = ?");
            PrepareStatement(CharStatements.REP_GUILD_FINDER_GUILD_SETTINGS, "REPLACE INTO guild_finder_guild_settings (guildId, availability, classRoles, interests, level, listed, comment) VALUES(?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_GUILD_FINDER_GUILD_SETTINGS, "DELETE FROM guild_finder_guild_settings WHERE guildId = ?");

            // Items that hold loot or money
            PrepareStatement(CharStatements.SEL_ITEMCONTAINER_ITEMS, "SELECT item_id, item_count, follow_rules, ffa, blocked, counted, under_threshold, needs_quest, rnd_type, rnd_prop, rnd_suffix, context, bonus_list_ids FROM item_loot_items WHERE container_id = ?");
            PrepareStatement(CharStatements.DEL_ITEMCONTAINER_ITEMS, "DELETE FROM item_loot_items WHERE container_id = ?");
            PrepareStatement(CharStatements.DEL_ITEMCONTAINER_ITEM, "DELETE FROM item_loot_items WHERE container_id = ? AND item_id = ?");
            PrepareStatement(CharStatements.INS_ITEMCONTAINER_ITEMS, "INSERT INTO item_loot_items (container_id, item_id, item_count, follow_rules, ffa, blocked, counted, under_threshold, needs_quest, rnd_type, rnd_prop, rnd_suffix, context, bonus_list_ids) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.SEL_ITEMCONTAINER_MONEY, "SELECT money FROM item_loot_money WHERE container_id = ?");
            PrepareStatement(CharStatements.DEL_ITEMCONTAINER_MONEY, "DELETE FROM item_loot_money WHERE container_id = ?");
            PrepareStatement(CharStatements.INS_ITEMCONTAINER_MONEY, "INSERT INTO item_loot_money (container_id, money) VALUES (?, ?)");

            // Calendar
            PrepareStatement(CharStatements.REP_CALENDAR_EVENT, "REPLACE INTO calendar_events (EventID, Owner, Title, Description, EventType, TextureID, Date, Flags, LockDate) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CALENDAR_EVENT, "DELETE FROM calendar_events WHERE EventID = ?");
            PrepareStatement(CharStatements.REP_CALENDAR_INVITE, "REPLACE INTO calendar_invites (InviteID, EventID, Invitee, Sender, Status, ResponseTime, ModerationRank, Note) VALUES (?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CALENDAR_INVITE, "DELETE FROM calendar_invites WHERE InviteID = ?");

            // Pet
            PrepareStatement(CharStatements.SEL_PET_SLOTS, "SELECT owner, slot FROM character_pet WHERE owner = ?  AND slot >= ? AND slot <= ? ORDER BY slot");
            PrepareStatement(CharStatements.SEL_PET_SLOTS_DETAIL, "SELECT owner, id, entry, level, name, modelid FROM character_pet WHERE owner = ? AND slot >= ? AND slot <= ? ORDER BY slot");
            PrepareStatement(CharStatements.SEL_PET_ENTRY, "SELECT entry FROM character_pet WHERE owner = ? AND id = ? AND slot >= ? AND slot <= ?");
            PrepareStatement(CharStatements.SEL_PET_SLOT_BY_ID, "SELECT slot, entry FROM character_pet WHERE owner = ? AND id = ?");
            PrepareStatement(CharStatements.SEL_PET_SPELL_LIST, "SELECT DISTINCT pet_spell.spell FROM pet_spell, character_pet WHERE character_pet.owner = ? AND character_pet.id = pet_spell.guid AND character_pet.id <> ?");
            PrepareStatement(CharStatements.SEL_CHAR_PET, "SELECT id FROM character_pet WHERE owner = ? AND id <> ?");
            PrepareStatement(CharStatements.SEL_CHAR_PETS, "SELECT id FROM character_pet WHERE owner = ?");
            PrepareStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME_BY_OWNER, "DELETE FROM character_pet_declinedname WHERE owner = ?");
            PrepareStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME, "DELETE FROM character_pet_declinedname WHERE id = ?");
            PrepareStatement(CharStatements.INS_CHAR_PET_DECLINEDNAME, "INSERT INTO character_pet_declinedname (id, owner, genitive, dative, accusative, instrumental, prepositional) VALUES (?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.SEL_PET_AURA, "SELECT casterGuid, spell, effectMask, recalculateMask, stackCount, maxDuration, remainTime, remainCharges FROM pet_aura WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_PET_AURA_EFFECT, "SELECT casterGuid, spell, effectMask, effectIndex, amount, baseAmount FROM pet_aura_effect WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_PET_SPELL, "SELECT spell, active FROM pet_spell WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_PET_SPELL_COOLDOWN, "SELECT spell, time, categoryId, categoryEnd FROM pet_spell_cooldown WHERE guid = ? AND time > UNIX_TIMESTAMP()");
            PrepareStatement(CharStatements.SEL_PET_DECLINED_NAME, "SELECT genitive, dative, accusative, instrumental, prepositional FROM character_pet_declinedname WHERE owner = ? AND id = ?");
            PrepareStatement(CharStatements.DEL_PET_AURAS, "DELETE FROM pet_aura WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_PET_AURA_EFFECTS, "DELETE FROM pet_aura_effect WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_PET_SPELLS, "DELETE FROM pet_spell WHERE guid = ?");
            PrepareStatement(CharStatements.DEL_PET_SPELL_COOLDOWNS, "DELETE FROM pet_spell_cooldown WHERE guid = ?");
            PrepareStatement(CharStatements.INS_PET_SPELL_COOLDOWN, "INSERT INTO pet_spell_cooldown (guid, spell, time, categoryId, categoryEnd) VALUES (?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.SEL_PET_SPELL_CHARGES, "SELECT categoryId, rechargeStart, rechargeEnd FROM pet_spell_charges WHERE guid = ? AND rechargeEnd > UNIX_TIMESTAMP() ORDER BY rechargeEnd");
            PrepareStatement(CharStatements.DEL_PET_SPELL_CHARGES, "DELETE FROM pet_spell_charges WHERE guid = ?");
            PrepareStatement(CharStatements.INS_PET_SPELL_CHARGES, "INSERT INTO pet_spell_charges (guid, categoryId, rechargeStart, rechargeEnd) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_PET_SPELL_BY_SPELL, "DELETE FROM pet_spell WHERE guid = ? and spell = ?");
            PrepareStatement(CharStatements.INS_PET_SPELL, "INSERT INTO pet_spell (guid, spell, active) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.INS_PET_AURA, "INSERT INTO pet_aura (guid, casterGuid, spell, effectMask, recalculateMask, stackCount, maxDuration, remainTime, remainCharges) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.INS_PET_AURA_EFFECT, "INSERT INTO pet_aura_effect (guid, casterGuid, spell, effectMask, effectIndex, amount, baseAmount) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.SEL_CHAR_PET_BY_ENTRY, "SELECT id, entry, owner, modelid, level, exp, Reactstate, slot, name, renamed, curhealth, curmana, abdata, savetime, CreatedBySpell, PetType, specialization FROM character_pet WHERE owner = ? AND id = ?");
            PrepareStatement(CharStatements.SEL_CHAR_PET_BY_ENTRY_AND_SLOT_2, "SELECT id, entry, owner, modelid, level, exp, Reactstate, slot, name, renamed, curhealth, curmana, abdata, savetime, CreatedBySpell, PetType, specialization FROM character_pet WHERE owner = ? AND entry = ? AND (slot = ? OR slot > ?)");
            PrepareStatement(CharStatements.SEL_CHAR_PET_BY_SLOT, "SELECT id, entry, owner, modelid, level, exp, Reactstate, slot, name, renamed, curhealth, curmana, abdata, savetime, CreatedBySpell, PetType, specialization FROM character_pet WHERE owner = ? AND (slot = ? OR slot > ?) ");
            PrepareStatement(CharStatements.SEL_CHAR_PET_BY_ENTRY_AND_SLOT, "SELECT id, entry, owner, modelid, level, exp, Reactstate, slot, name, renamed, curhealth, curmana, abdata, savetime, CreatedBySpell, PetType, specialization FROM character_pet WHERE owner = ? AND slot = ?");
            PrepareStatement(CharStatements.DEL_CHAR_PET_BY_OWNER, "DELETE FROM character_pet WHERE owner = ?");
            PrepareStatement(CharStatements.UPD_CHAR_PET_NAME, "UPDATE character_pet SET name = ?, renamed = 1 WHERE owner = ? AND id = ?");
            PrepareStatement(CharStatements.UPD_CHAR_PET_SLOT_BY_SLOT_EXCLUDE_ID, "UPDATE character_pet SET slot = ? WHERE owner = ? AND slot = ? AND id <> ?");
            PrepareStatement(CharStatements.UPD_CHAR_PET_SLOT_BY_SLOT, "UPDATE character_pet SET slot = ? WHERE owner = ? AND slot = ?");
            PrepareStatement(CharStatements.UPD_CHAR_PET_SLOT_BY_ID, "UPDATE character_pet SET slot = ? WHERE owner = ? AND id = ?");
            PrepareStatement(CharStatements.DEL_CHAR_PET_BY_ID, "DELETE FROM character_pet WHERE id = ?");
            PrepareStatement(CharStatements.DEL_CHAR_PET_BY_SLOT, "DELETE FROM character_pet WHERE owner = ? AND (slot = ? OR slot > ?)");
            PrepareStatement(CharStatements.DEL_ALL_PET_SPELLS_BY_OWNER, "DELETE FROM pet_spell WHERE guid in (SELECT id FROM character_pet WHERE owner=?)");
            PrepareStatement(CharStatements.UPD_PET_SPECS_BY_OWNER, "UPDATE character_pet SET specialization = 0 WHERE owner=?");
            PrepareStatement(CharStatements.INS_PET, "INSERT INTO character_pet (id, entry, owner, modelid, level, exp, Reactstate, slot, name, renamed, curhealth, curmana, abdata, savetime, CreatedBySpell, PetType, specialization) " +
                            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");

            // PvPstats
            PrepareStatement(CharStatements.SEL_PVPSTATS_MAXID, "SELECT MAX(id) FROM pvpstats_battlegrounds");
            PrepareStatement(CharStatements.INS_PVPSTATS_BATTLEGROUND, "INSERT INTO pvpstats_battlegrounds (id, winner_faction, bracket_id, type, date) VALUES (?, ?, ?, ?, NOW())");
            PrepareStatement(CharStatements.INS_PVPSTATS_PLAYER, "INSERT INTO pvpstats_players (battleground_id, character_guid, winner, score_killing_blows, score_deaths, score_honorable_kills, score_bonus_honor, score_damage_done, score_healing_done, attr_1, attr_2, attr_3, attr_4, attr_5) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.SEL_PVPSTATS_FACTIONS_OVERALL, "SELECT winner_faction, COUNT(*) AS count FROM pvpstats_battlegrounds WHERE DATEDIFF(NOW(), date) < 7 GROUP BY winner_faction ORDER BY winner_faction ASC");

            // QuestTracker
            PrepareStatement(CharStatements.INS_QUEST_TRACK, "INSERT INTO quest_tracker (id, character_guid, quest_accept_time, core_hash, core_revision) VALUES (?, ?, NOW(), ?, ?)");
            PrepareStatement(CharStatements.UPD_QUEST_TRACK_GM_COMPLETE, "UPDATE quest_tracker SET completed_by_gm = 1 WHERE id = ? AND character_guid = ? ORDER BY quest_accept_time DESC LIMIT 1");
            PrepareStatement(CharStatements.UPD_QUEST_TRACK_COMPLETE_TIME, "UPDATE quest_tracker SET quest_complete_time = NOW() WHERE id = ? AND character_guid = ? ORDER BY quest_accept_time DESC LIMIT 1");
            PrepareStatement(CharStatements.UPD_QUEST_TRACK_ABANDON_TIME, "UPDATE quest_tracker SET quest_abandon_time = NOW() WHERE id = ? AND character_guid = ? ORDER BY quest_accept_time DESC LIMIT 1");

            // Garrison
            PrepareStatement(CharStatements.SEL_CHARACTER_GARRISON, "SELECT siteLevelId, followerActivationsRemainingToday FROM character_garrison WHERE guid = ?");
            PrepareStatement(CharStatements.INS_CHARACTER_GARRISON, "INSERT INTO character_garrison (guid, siteLevelId, followerActivationsRemainingToday) VALUES (?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CHARACTER_GARRISON, "DELETE FROM character_garrison WHERE guid = ?");
            PrepareStatement(CharStatements.UPD_CHARACTER_GARRISON_FOLLOWER_ACTIVATIONS, "UPDATE character_garrison SET followerActivationsRemainingToday = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_GARRISON_BLUEPRINTS, "SELECT buildingId FROM character_garrison_blueprints WHERE guid = ?");
            PrepareStatement(CharStatements.INS_CHARACTER_GARRISON_BLUEPRINTS, "INSERT INTO character_garrison_blueprints (guid, buildingId) VALUES (?, ?)");
            PrepareStatement(CharStatements.DEL_CHARACTER_GARRISON_BLUEPRINTS, "DELETE FROM character_garrison_blueprints WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_GARRISON_BUILDINGS, "SELECT plotInstanceId, buildingId, timeBuilt, active FROM character_garrison_buildings WHERE guid = ?");
            PrepareStatement(CharStatements.INS_CHARACTER_GARRISON_BUILDINGS, "INSERT INTO character_garrison_buildings (guid, plotInstanceId, buildingId, timeBuilt, active) VALUES (?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CHARACTER_GARRISON_BUILDINGS, "DELETE FROM character_garrison_buildings WHERE guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_GARRISON_FOLLOWERS, "SELECT dbId, followerId, quality, level, itemLevelWeapon, itemLevelArmor, xp, currentBuilding, currentMission, status FROM character_garrison_followers WHERE guid = ?");
            PrepareStatement(CharStatements.INS_CHARACTER_GARRISON_FOLLOWERS, "INSERT INTO character_garrison_followers (dbId, guid, followerId, quality, level, itemLevelWeapon, itemLevelArmor, xp, currentBuilding, currentMission, status) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_CHARACTER_GARRISON_FOLLOWERS, "DELETE gfab, gf FROM character_garrison_follower_abilities gfab INNER JOIN character_garrison_followers gf ON gfab.dbId = gf.dbId WHERE gf.guid = ?");
            PrepareStatement(CharStatements.SEL_CHARACTER_GARRISON_FOLLOWER_ABILITIES, "SELECT gfab.dbId, gfab.abilityId FROM character_garrison_follower_abilities gfab INNER JOIN character_garrison_followers gf ON gfab.dbId = gf.dbId WHERE guid = ? ORDER BY gfab.slot");
            PrepareStatement(CharStatements.INS_CHARACTER_GARRISON_FOLLOWER_ABILITIES, "INSERT INTO character_garrison_follower_abilities (dbId, abilityId, slot) VALUES (?, ?, ?)");

            // Black Market
            PrepareStatement(CharStatements.SEL_BLACKMARKET_AUCTIONS, "SELECT marketId, currentBid, time, numBids, bidder FROM blackmarket_auctions");
            PrepareStatement(CharStatements.DEL_BLACKMARKET_AUCTIONS, "DELETE FROM blackmarket_auctions WHERE marketId = ?");
            PrepareStatement(CharStatements.UPD_BLACKMARKET_AUCTIONS, "UPDATE blackmarket_auctions SET currentBid = ?, time = ?, numBids = ?, bidder = ? WHERE marketId = ?");
            PrepareStatement(CharStatements.INS_BLACKMARKET_AUCTIONS, "INSERT INTO blackmarket_auctions (marketId, currentBid, time, numBids, bidder) VALUES (?, ?, ?, ? ,?)");

            // Scenario
            PrepareStatement(CharStatements.SEL_SCENARIO_INSTANCE_CRITERIA_FOR_INSTANCE, "SELECT criteria, counter, date FROM instance_scenario_progress WHERE id = ?");
            PrepareStatement(CharStatements.DEL_SCENARIO_INSTANCE_CRITERIA, "DELETE FROM instance_scenario_progress WHERE id = ? AND criteria = ?");
            PrepareStatement(CharStatements.INS_SCENARIO_INSTANCE_CRITERIA, "INSERT INTO instance_scenario_progress (id, criteria, counter, date) VALUES (?, ?, ?, ?)");
            PrepareStatement(CharStatements.DEL_SCENARIO_INSTANCE_CRITERIA_FOR_INSTANCE, "DELETE FROM instance_scenario_progress WHERE id = ?");
        }
    }

    public enum CharStatements
    {
        DEL_QUEST_POOL_SAVE,
        INS_QUEST_POOL_SAVE,
        DEL_NONEXISTENT_GUILD_BANK_ITEM,
        DEL_EXPIRED_BANS,
        SEL_GUID_BY_NAME,
        SEL_CHECK_NAME,
        SEL_CHECK_GUID,
        SEL_SUM_CHARS,
        SEL_CHAR_CREATE_INFO,
        INS_CHARACTER_BAN,
        UPD_CHARACTER_BAN,
        DEL_CHARACTER_BAN,
        SEL_BANINFO,
        SEL_GUID_BY_NAME_FILTER,
        SEL_BANINFO_LIST,
        SEL_BANNED_NAME,
        SEL_MAIL_LIST_COUNT,
        SEL_MAIL_LIST_INFO,
        SEL_MAIL_LIST_ITEMS,
        SEL_ENUM,
        SEL_ENUM_DECLINED_NAME,
        SEL_UNDELETE_ENUM,
        SEL_UNDELETE_ENUM_DECLINED_NAME,
        SEL_FREE_NAME,
        SEL_GUID_RACE_ACC_BY_NAME,
        SEL_CHAR_LEVEL,
        SEL_CHAR_ZONE,
        SEL_CHAR_POSITION_XYZ,
        SEL_CHAR_POSITION,

        DEL_BATTLEGROUND_RANDOM_ALL,
        DEL_BATTLEGROUND_RANDOM,
        INS_BATTLEGROUND_RANDOM,

        SEL_CHARACTER,
        SEL_GROUP_MEMBER,
        SEL_CHARACTER_INSTANCE,
        SEL_CHARACTER_AURAS,
        SEL_CHARACTER_AURA_EFFECTS,
        SEL_CHARACTER_SPELL,

        SEL_CHARACTER_QUESTSTATUS,
        SEL_CHARACTER_QUESTSTATUS_OBJECTIVES,
        SEL_CHARACTER_QUESTSTATUS_OBJECTIVES_CRITERIA,
        SEL_CHARACTER_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS,
        SEL_CHARACTER_QUESTSTATUS_DAILY,
        SEL_CHARACTER_QUESTSTATUS_WEEKLY,
        SEL_CHARACTER_QUESTSTATUS_MONTHLY,
        SEL_CHARACTER_QUESTSTATUS_SEASONAL,
        DEL_CHARACTER_QUESTSTATUS_DAILY,
        DEL_CHARACTER_QUESTSTATUS_WEEKLY,
        DEL_CHARACTER_QUESTSTATUS_MONTHLY,
        DEL_CHARACTER_QUESTSTATUS_SEASONAL,
        INS_CHARACTER_QUESTSTATUS_DAILY,
        INS_CHARACTER_QUESTSTATUS_WEEKLY,
        INS_CHARACTER_QUESTSTATUS_MONTHLY,
        INS_CHARACTER_QUESTSTATUS_SEASONAL,
        DEL_RESET_CHARACTER_QUESTSTATUS_DAILY,
        DEL_RESET_CHARACTER_QUESTSTATUS_WEEKLY,
        DEL_RESET_CHARACTER_QUESTSTATUS_MONTHLY,
        DEL_RESET_CHARACTER_QUESTSTATUS_SEASONAL_BY_EVENT,

        SEL_CHARACTER_REPUTATION,
        SEL_CHARACTER_INVENTORY,
        SEL_CHARACTER_ACTIONS,
        SEL_CHARACTER_ACTIONS_SPEC,
        SEL_CHARACTER_MAILCOUNT,
        SEL_CHARACTER_MAILDATE,
        SEL_MAIL_COUNT,
        SEL_CHARACTER_SOCIALLIST,
        SEL_CHARACTER_HOMEBIND,
        SEL_CHARACTER_SPELLCOOLDOWNS,
        SEL_CHARACTER_SPELL_CHARGES,
        SEL_CHARACTER_DECLINEDNAMES,
        SEL_GUILD_MEMBER,
        SEL_GUILD_MEMBER_EXTENDED,
        SEL_CHARACTER_ARENAINFO,
        SEL_CHARACTER_ACHIEVEMENTS,
        SEL_CHARACTER_CRITERIAPROGRESS,
        SEL_CHARACTER_EQUIPMENTSETS,
        SEL_CHARACTER_BGDATA,
        SEL_CHARACTER_GLYPHS,
        SEL_CHARACTER_TALENTS,
        SEL_CHARACTER_PVP_TALENTS,
        SEL_CHARACTER_TRANSMOG_OUTFITS,
        SEL_CHARACTER_SKILLS,
        SEL_CHARACTER_RANDOMBG,
        SEL_CHARACTER_BANNED,
        SEL_CHARACTER_QUESTSTATUSREW,
        SEL_ACCOUNT_INSTANCELOCKTIMES,
        SEL_MAILITEMS,
        SEL_AUCTION_ITEMS,
        INS_AUCTION,
        DEL_AUCTION,
        UPD_AUCTION_BID,
        SEL_AUCTIONS,
        INS_MAIL,
        DEL_MAIL_BY_ID,
        INS_MAIL_ITEM,
        DEL_MAIL_ITEM,
        DEL_INVALID_MAIL_ITEM,
        DEL_EMPTY_EXPIRED_MAIL,
        SEL_EXPIRED_MAIL,
        SEL_EXPIRED_MAIL_ITEMS,
        UPD_MAIL_RETURNED,
        UPD_MAIL_ITEM_RECEIVER,
        UPD_ITEM_OWNER,
        SEL_ITEM_REFUNDS,
        SEL_ITEM_BOP_TRADE,
        DEL_ITEM_BOP_TRADE,
        INS_ITEM_BOP_TRADE,
        REP_INVENTORY_ITEM,
        REP_ITEM_INSTANCE,
        UPD_ITEM_INSTANCE,
        UPD_ITEM_INSTANCE_ON_LOAD,
        DEL_ITEM_INSTANCE,
        DEL_ITEM_INSTANCE_BY_OWNER,
        INS_ITEM_INSTANCE_GEMS,
        DEL_ITEM_INSTANCE_GEMS,
        DEL_ITEM_INSTANCE_GEMS_BY_OWNER,
        INS_ITEM_INSTANCE_TRANSMOG,
        DEL_ITEM_INSTANCE_TRANSMOG,
        DEL_ITEM_INSTANCE_TRANSMOG_BY_OWNER,
        SEL_ITEM_INSTANCE_ARTIFACT,
        INS_ITEM_INSTANCE_ARTIFACT,
        DEL_ITEM_INSTANCE_ARTIFACT,
        DEL_ITEM_INSTANCE_ARTIFACT_BY_OWNER,
        INS_ITEM_INSTANCE_ARTIFACT_POWERS,
        DEL_ITEM_INSTANCE_ARTIFACT_POWERS,
        DEL_ITEM_INSTANCE_ARTIFACT_POWERS_BY_OWNER,
        INS_ITEM_INSTANCE_MODIFIERS,
        DEL_ITEM_INSTANCE_MODIFIERS,
        DEL_ITEM_INSTANCE_MODIFIERS_BY_OWNER,
        UPD_GIFT_OWNER,
        DEL_GIFT,
        SEL_CHARACTER_GIFT_BY_ITEM,
        SEL_ACCOUNT_BY_NAME,
        UPD_ACCOUNT_BY_GUID,
        DEL_ACCOUNT_INSTANCE_LOCK_TIMES,
        INS_ACCOUNT_INSTANCE_LOCK_TIMES,
        SEL_MATCH_MAKER_RATING,
        SEL_CHARACTER_COUNT,
        UPD_NAME_BY_GUID,

        INS_GUILD,
        DEL_GUILD,
        UPD_GUILD_NAME,
        INS_GUILD_MEMBER,
        DEL_GUILD_MEMBER,
        DEL_GUILD_MEMBERS,
        INS_GUILD_RANK,
        DEL_GUILD_RANKS,
        DEL_GUILD_RANK,
        INS_GUILD_BANK_TAB,
        DEL_GUILD_BANK_TAB,
        DEL_GUILD_BANK_TABS,
        INS_GUILD_BANK_ITEM,
        DEL_GUILD_BANK_ITEM,
        SEL_GUILD_BANK_ITEMS,
        DEL_GUILD_BANK_ITEMS,
        INS_GUILD_BANK_RIGHT,
        DEL_GUILD_BANK_RIGHTS,
        DEL_GUILD_BANK_RIGHTS_FOR_RANK,
        INS_GUILD_BANK_EVENTLOG,
        DEL_GUILD_BANK_EVENTLOG,
        DEL_GUILD_BANK_EVENTLOGS,
        INS_GUILD_EVENTLOG,
        DEL_GUILD_EVENTLOG,
        DEL_GUILD_EVENTLOGS,
        UPD_GUILD_MEMBER_PNOTE,
        UPD_GUILD_MEMBER_OFFNOTE,
        UPD_GUILD_MEMBER_RANK,
        UPD_GUILD_MOTD,
        UPD_GUILD_INFO,
        UPD_GUILD_LEADER,
        UPD_GUILD_RANK_NAME,
        UPD_GUILD_RANK_RIGHTS,
        UPD_GUILD_EMBLEM_INFO,
        UPD_GUILD_BANK_TAB_INFO,
        UPD_GUILD_BANK_MONEY,
        UPD_GUILD_RANK_BANK_MONEY,
        UPD_GUILD_BANK_TAB_TEXT,
        INS_GUILD_MEMBER_WITHDRAW_TABS,
        INS_GUILD_MEMBER_WITHDRAW_MONEY,
        DEL_GUILD_MEMBER_WITHDRAW,
        SEL_CHAR_DATA_FOR_GUILD,
        DEL_GUILD_ACHIEVEMENT,
        INS_GUILD_ACHIEVEMENT,
        DEL_GUILD_ACHIEVEMENT_CRITERIA,
        INS_GUILD_ACHIEVEMENT_CRITERIA,
        DEL_ALL_GUILD_ACHIEVEMENTS,
        DEL_ALL_GUILD_ACHIEVEMENT_CRITERIA,
        SEL_GUILD_ACHIEVEMENT,
        SEL_GUILD_ACHIEVEMENT_CRITERIA,
        INS_GUILD_NEWS,

        SEL_CHANNEL,
        INS_CHANNEL,
        UPD_CHANNEL,
        UPD_CHANNEL_USAGE,
        UPD_CHANNEL_OWNERSHIP,
        DEL_OLD_CHANNELS,

        UPD_EQUIP_SET,
        INS_EQUIP_SET,
        DEL_EQUIP_SET,

        UPD_TRANSMOG_OUTFIT,
        INS_TRANSMOG_OUTFIT,
        DEL_TRANSMOG_OUTFIT,

        INS_AURA,
        INS_AURA_EFFECT,

        SEL_PLAYER_CURRENCY,
        UPD_PLAYER_CURRENCY,
        REP_PLAYER_CURRENCY,
        DEL_PLAYER_CURRENCY,

        SEL_ACCOUNT_DATA,
        REP_ACCOUNT_DATA,
        DEL_ACCOUNT_DATA,
        SEL_PLAYER_ACCOUNT_DATA,
        REP_PLAYER_ACCOUNT_DATA,
        DEL_PLAYER_ACCOUNT_DATA,

        SEL_TUTORIALS,
        SEL_HAS_TUTORIALS,
        INS_TUTORIALS,
        UPD_TUTORIALS,
        DEL_TUTORIALS,

        INS_INSTANCE_SAVE,
        UPD_INSTANCE_DATA,

        DEL_GAME_EVENT_SAVE,
        INS_GAME_EVENT_SAVE,

        DEL_ALL_GAME_EVENT_CONDITION_SAVE,
        DEL_GAME_EVENT_CONDITION_SAVE,
        INS_GAME_EVENT_CONDITION_SAVE,

        INS_ARENA_TEAM,
        INS_ARENA_TEAM_MEMBER,
        DEL_ARENA_TEAM,
        DEL_ARENA_TEAM_MEMBERS,
        UPD_ARENA_TEAM_CAPTAIN,
        DEL_ARENA_TEAM_MEMBER,
        UPD_ARENA_TEAM_STATS,
        UPD_ARENA_TEAM_MEMBER,
        DEL_CHARACTER_ARENA_STATS,
        REP_CHARACTER_ARENA_STATS,
        SEL_PLAYER_ARENA_TEAMS,
        UPD_ARENA_TEAM_NAME,

        SEL_PETITION,
        SEL_PETITION_SIGNATURE,
        DEL_ALL_PETITION_SIGNATURES,
        SEL_PETITION_BY_OWNER,
        SEL_PETITION_SIGNATURES,
        SEL_PETITION_SIG_BY_ACCOUNT,
        SEL_PETITION_OWNER_BY_GUID,
        SEL_PETITION_SIG_BY_GUID,

        INS_PLAYER_BGDATA,
        DEL_PLAYER_BGDATA,

        INS_PLAYER_HOMEBIND,
        UPD_PLAYER_HOMEBIND,
        DEL_PLAYER_HOMEBIND,

        SEL_CORPSES,
        INS_CORPSE,
        DEL_CORPSE,
        DEL_CORPSES_FROM_MAP,
        SEL_CORPSE_PHASES,
        INS_CORPSE_PHASES,
        DEL_CORPSE_PHASES,
        SEL_CORPSE_LOCATION,

        SEL_CREATURE_RESPAWNS,
        REP_CREATURE_RESPAWN,
        DEL_CREATURE_RESPAWN,
        DEL_CREATURE_RESPAWN_BY_INSTANCE,
        SEL_MAX_CREATURE_RESPAWNS,

        SEL_GO_RESPAWNS,
        REP_GO_RESPAWN,
        DEL_GO_RESPAWN,
        DEL_GO_RESPAWN_BY_INSTANCE,

        SEL_GM_BUGS,
        REP_GM_BUG,
        DEL_GM_BUG,
        DEL_ALL_GM_BUGS,

        SEL_GM_COMPLAINTS,
        REP_GM_COMPLAINT,
        DEL_GM_COMPLAINT,
        SEL_GM_COMPLAINT_CHATLINES,
        INS_GM_COMPLAINT_CHATLINE,
        DEL_GM_COMPLAINT_CHATLOG,
        DEL_ALL_GM_COMPLAINTS,
        DEL_ALL_GM_COMPLAINT_CHATLOGS,

        SEL_GM_SUGGESTIONS,
        REP_GM_SUGGESTION,
        DEL_GM_SUGGESTION,
        DEL_ALL_GM_SUGGESTIONS,

        INS_CHARACTER,
        UPD_CHARACTER,

        UPD_ADD_AT_LOGIN_FLAG,
        UPD_REM_AT_LOGIN_FLAG,
        UPD_ALL_AT_LOGIN_FLAGS,
        INS_BUG_REPORT,
        UPD_PETITION_NAME,
        INS_PETITION_SIGNATURE,
        UPD_ACCOUNT_ONLINE,
        INS_GROUP,
        INS_GROUP_MEMBER,
        DEL_GROUP_MEMBER,
        DEL_GROUP_INSTANCE_PERM_BINDING,
        UPD_GROUP_LEADER,
        UPD_GROUP_TYPE,
        UPD_GROUP_MEMBER_SUBGROUP,
        UPD_GROUP_MEMBER_FLAG,
        UPD_GROUP_DIFFICULTY,
        UPD_GROUP_RAID_DIFFICULTY,
        UPD_GROUP_LEGACY_RAID_DIFFICULTY,
        DEL_INVALID_SPELL_SPELLS,
        UPD_DELETE_INFO,
        UPD_RESTORE_DELETE_INFO,
        UPD_ZONE,
        UPD_LEVEL,
        DEL_INVALID_ACHIEV_PROGRESS_CRITERIA,
        DEL_INVALID_ACHIEV_PROGRESS_CRITERIA_GUILD,
        DEL_INVALID_ACHIEVMENT,
        DEL_INVALID_PET_SPELL,
        DEL_GROUP_INSTANCE_BY_INSTANCE,
        DEL_GROUP_INSTANCE_BY_GUID,
        REP_GROUP_INSTANCE,
        UPD_INSTANCE_RESETTIME,
        UPD_GLOBAL_INSTANCE_RESETTIME,
        UPD_CHAR_ONLINE,
        UPD_CHAR_NAME_AT_LOGIN,
        UPD_WORLDSTATE,
        INS_WORLDSTATE,
        DEL_CHAR_INSTANCE_BY_INSTANCE_GUID,
        UPD_CHAR_INSTANCE,
        INS_CHAR_INSTANCE,
        UPD_GENDER_AND_APPEARANCE,
        DEL_CHARACTER_SKILL,
        UPD_CHARACTER_SOCIAL_FLAGS,
        INS_CHARACTER_SOCIAL,
        DEL_CHARACTER_SOCIAL,
        UPD_CHARACTER_SOCIAL_NOTE,
        UPD_CHARACTER_POSITION,

        INS_LFG_DATA,
        DEL_LFG_DATA,

        SEL_CHARACTER_AURA_FROZEN,
        SEL_CHARACTER_ONLINE,

        SEL_CHAR_DEL_INFO_BY_GUID,
        SEL_CHAR_DEL_INFO_BY_NAME,
        SEL_CHAR_DEL_INFO,

        SEL_CHARS_BY_ACCOUNT_ID,
        SEL_CHAR_PINFO,
        SEL_PINFO_XP,
        SEL_PINFO_MAILS,
        SEL_PINFO_BANS,
        SEL_CHAR_HOMEBIND,
        SEL_CHAR_GUID_NAME_BY_ACC,
        SEL_POOL_QUEST_SAVE,
        SEL_CHAR_CUSTOMIZE_INFO,
        SEL_CHAR_RACE_OR_FACTION_CHANGE_INFOS,
        SEL_INSTANCE,
        SEL_PERM_BIND_BY_INSTANCE,
        SEL_CHAR_COD_ITEM_MAIL,
        SEL_CHAR_SOCIAL,
        SEL_CHAR_OLD_CHARS,
        SEL_ARENA_TEAM_ID_BY_PLAYER_GUID,
        SEL_MAIL,
        DEL_CHAR_AURA_FROZEN,
        SEL_CHAR_INVENTORY_COUNT_ITEM,
        SEL_MAIL_COUNT_ITEM,
        SEL_AUCTIONHOUSE_COUNT_ITEM,
        SEL_GUILD_BANK_COUNT_ITEM,
        SEL_CHAR_INVENTORY_ITEM_BY_ENTRY,
        SEL_MAIL_ITEMS_BY_ENTRY,
        SEL_AUCTIONHOUSE_ITEM_BY_ENTRY,
        SEL_GUILD_BANK_ITEM_BY_ENTRY,
        DEL_CHAR_ACHIEVEMENT,
        DEL_CHAR_ACHIEVEMENT_PROGRESS,
        INS_CHAR_ACHIEVEMENT,
        DEL_CHAR_ACHIEVEMENT_PROGRESS_BY_CRITERIA,
        INS_CHAR_ACHIEVEMENT_PROGRESS,
        DEL_CHAR_REPUTATION_BY_FACTION,
        INS_CHAR_REPUTATION_BY_FACTION,
        DEL_ITEM_REFUND_INSTANCE,
        INS_ITEM_REFUND_INSTANCE,
        DEL_GROUP,
        DEL_GROUP_MEMBER_ALL,
        INS_CHAR_GIFT,
        DEL_INSTANCE_BY_INSTANCE,
        DEL_CHAR_INSTANCE_BY_INSTANCE,
        DEL_EXPIRED_CHAR_INSTANCE_BY_MAP_DIFF,
        DEL_GROUP_INSTANCE_BY_MAP_DIFF,
        DEL_EXPIRED_INSTANCE_BY_MAP_DIFF,
        UPD_EXPIRE_CHAR_INSTANCE_BY_MAP_DIFF,
        DEL_MAIL_ITEM_BY_ID,
        INS_PETITION,
        DEL_PETITION_BY_GUID,
        DEL_PETITION_SIGNATURE_BY_GUID,
        DEL_CHAR_DECLINED_NAME,
        INS_CHAR_DECLINED_NAME,
        UPD_CHAR_RACE,
        DEL_CHAR_SKILL_LANGUAGES,
        INS_CHAR_SKILL_LANGUAGE,
        UPD_CHAR_TAXI_PATH,
        UPD_CHAR_TAXIMASK,
        DEL_CHAR_QUESTSTATUS,
        DEL_CHAR_QUESTSTATUS_OBJECTIVES,
        DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA,
        DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS,
        DEL_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS_BY_CRITERIA,
        DEL_CHAR_SOCIAL_BY_GUID,
        DEL_CHAR_SOCIAL_BY_FRIEND,
        DEL_CHAR_ACHIEVEMENT_BY_ACHIEVEMENT,
        UPD_CHAR_ACHIEVEMENT,
        UPD_CHAR_INVENTORY_FACTION_CHANGE,
        DEL_CHAR_SPELL_BY_SPELL,
        UPD_CHAR_SPELL_FACTION_CHANGE,
        SEL_CHAR_REP_BY_FACTION,
        DEL_CHAR_REP_BY_FACTION,
        UPD_CHAR_REP_FACTION_CHANGE,
        UPD_CHAR_TITLES_FACTION_CHANGE,
        RES_CHAR_TITLES_FACTION_CHANGE,
        DEL_CHAR_SPELL_COOLDOWNS,
        INS_CHAR_SPELL_COOLDOWN,
        DEL_CHAR_SPELL_CHARGES,
        INS_CHAR_SPELL_CHARGES,
        DEL_CHARACTER,
        DEL_CHAR_ACTION,
        DEL_CHAR_AURA,
        DEL_CHAR_AURA_EFFECT,
        DEL_CHAR_GIFT,
        DEL_CHAR_INSTANCE,
        DEL_CHAR_INVENTORY,
        DEL_CHAR_QUESTSTATUS_REWARDED,
        DEL_CHAR_REPUTATION,
        DEL_CHAR_SPELL,
        DEL_MAIL,
        DEL_MAIL_ITEMS,
        DEL_CHAR_ACHIEVEMENTS,
        DEL_CHAR_EQUIPMENTSETS,
        DEL_CHAR_TRANSMOG_OUTFITS,
        DEL_GUILD_EVENTLOG_BY_PLAYER,
        DEL_GUILD_BANK_EVENTLOG_BY_PLAYER,
        DEL_CHAR_GLYPHS,
        DEL_CHAR_TALENT,
        DEL_CHAR_PVP_TALENT,
        DEL_CHAR_SKILLS,
        UPD_CHAR_MONEY,
        INS_CHAR_ACTION,
        UPD_CHAR_ACTION,
        DEL_CHAR_ACTION_BY_BUTTON_SPEC,
        DEL_CHAR_INVENTORY_BY_ITEM,
        DEL_CHAR_INVENTORY_BY_BAG_SLOT,
        UPD_MAIL,
        REP_CHAR_QUESTSTATUS,
        DEL_CHAR_QUESTSTATUS_BY_QUEST,
        REP_CHAR_QUESTSTATUS_OBJECTIVES,
        DEL_CHAR_QUESTSTATUS_OBJECTIVES_BY_QUEST,
        INS_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA,
        INS_CHAR_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS,
        INS_CHAR_QUESTSTATUS_REWARDED,
        DEL_CHAR_QUESTSTATUS_REWARDED_BY_QUEST,
        UPD_CHAR_QUESTSTATUS_REWARDED_FACTION_CHANGE,
        UPD_CHAR_QUESTSTATUS_REWARDED_ACTIVE,
        UPD_CHAR_QUESTSTATUS_REWARDED_ACTIVE_BY_QUEST,
        DEL_INVALID_QUEST_PROGRESS_CRITERIA,
        DEL_CHAR_SKILL_BY_SKILL,
        INS_CHAR_SKILLS,
        UPD_CHAR_SKILLS,
        INS_CHAR_SPELL,
        DEL_CHAR_STATS,
        INS_CHAR_STATS,
        DEL_PETITION_BY_OWNER,
        DEL_PETITION_SIGNATURE_BY_OWNER,
        INS_CHAR_GLYPHS,
        INS_CHAR_TALENT,
        INS_CHAR_PVP_TALENT,
        UPD_CHAR_LIST_SLOT,
        INS_CHAR_FISHINGSTEPS,
        DEL_CHAR_FISHINGSTEPS,

        SEL_CHAR_VOID_STORAGE,
        REP_CHAR_VOID_STORAGE_ITEM,
        DEL_CHAR_VOID_STORAGE_ITEM_BY_CHAR_GUID,
        DEL_CHAR_VOID_STORAGE_ITEM_BY_SLOT,

        SEL_CHAR_CUF_PROFILES,
        REP_CHAR_CUF_PROFILES,
        DEL_CHAR_CUF_PROFILES_BY_ID,
        DEL_CHAR_CUF_PROFILES,

        REP_GUILD_FINDER_APPLICANT,
        DEL_GUILD_FINDER_APPLICANT,
        REP_GUILD_FINDER_GUILD_SETTINGS,
        DEL_GUILD_FINDER_GUILD_SETTINGS,

        REP_CALENDAR_EVENT,
        DEL_CALENDAR_EVENT,
        REP_CALENDAR_INVITE,
        DEL_CALENDAR_INVITE,

        SEL_PET_AURA,
        SEL_PET_AURA_EFFECT,
        SEL_PET_SPELL,
        SEL_PET_SPELL_COOLDOWN,
        SEL_PET_DECLINED_NAME,
        DEL_PET_AURAS,
        DEL_PET_AURA_EFFECTS,
        DEL_PET_SPELL_COOLDOWNS,
        INS_PET_SPELL_COOLDOWN,
        SEL_PET_SPELL_CHARGES,
        DEL_PET_SPELL_CHARGES,
        INS_PET_SPELL_CHARGES,
        DEL_PET_SPELL_BY_SPELL,
        INS_PET_SPELL,
        INS_PET_AURA,
        INS_PET_AURA_EFFECT,

        DEL_PET_SPELLS,
        DEL_CHAR_PET_BY_OWNER,
        DEL_CHAR_PET_DECLINEDNAME_BY_OWNER,
        SEL_CHAR_PET_BY_ENTRY_AND_SLOT,
        SEL_PET_SLOTS,
        SEL_PET_SLOTS_DETAIL,
        SEL_PET_ENTRY,
        SEL_PET_SLOT_BY_ID,
        SEL_PET_SPELL_LIST,
        SEL_CHAR_PET,
        SEL_CHAR_PETS,
        INS_CHAR_PET,
        SEL_CHAR_PET_BY_ENTRY,
        SEL_CHAR_PET_BY_ENTRY_AND_SLOT_2,
        SEL_CHAR_PET_BY_SLOT,
        DEL_CHAR_PET_DECLINEDNAME,
        INS_CHAR_PET_DECLINEDNAME,
        UPD_CHAR_PET_NAME,
        UPD_CHAR_PET_SLOT_BY_SLOT_EXCLUDE_ID,
        UPD_CHAR_PET_SLOT_BY_SLOT,
        UPD_CHAR_PET_SLOT_BY_ID,
        DEL_CHAR_PET_BY_ID,
        DEL_CHAR_PET_BY_SLOT,
        DEL_ALL_PET_SPELLS_BY_OWNER,
        UPD_PET_SPECS_BY_OWNER,
        INS_PET,

        SEL_ITEMCONTAINER_ITEMS,
        DEL_ITEMCONTAINER_ITEMS,
        DEL_ITEMCONTAINER_ITEM,
        INS_ITEMCONTAINER_ITEMS,
        SEL_ITEMCONTAINER_MONEY,
        DEL_ITEMCONTAINER_MONEY,
        INS_ITEMCONTAINER_MONEY,

        SEL_PVPSTATS_MAXID,
        INS_PVPSTATS_BATTLEGROUND,
        INS_PVPSTATS_PLAYER,
        SEL_PVPSTATS_FACTIONS_OVERALL,

        INS_QUEST_TRACK,
        UPD_QUEST_TRACK_GM_COMPLETE,
        UPD_QUEST_TRACK_COMPLETE_TIME,
        UPD_QUEST_TRACK_ABANDON_TIME,

        SEL_CHARACTER_GARRISON,
        INS_CHARACTER_GARRISON,
        DEL_CHARACTER_GARRISON,
        UPD_CHARACTER_GARRISON_FOLLOWER_ACTIVATIONS,
        SEL_CHARACTER_GARRISON_BLUEPRINTS,
        INS_CHARACTER_GARRISON_BLUEPRINTS,
        DEL_CHARACTER_GARRISON_BLUEPRINTS,
        SEL_CHARACTER_GARRISON_BUILDINGS,
        INS_CHARACTER_GARRISON_BUILDINGS,
        DEL_CHARACTER_GARRISON_BUILDINGS,
        SEL_CHARACTER_GARRISON_FOLLOWERS,
        INS_CHARACTER_GARRISON_FOLLOWERS,
        DEL_CHARACTER_GARRISON_FOLLOWERS,
        SEL_CHARACTER_GARRISON_FOLLOWER_ABILITIES,
        INS_CHARACTER_GARRISON_FOLLOWER_ABILITIES,

        SEL_BLACKMARKET_AUCTIONS,
        DEL_BLACKMARKET_AUCTIONS,
        UPD_BLACKMARKET_AUCTIONS,
        INS_BLACKMARKET_AUCTIONS,

        SEL_SCENARIO_INSTANCE_CRITERIA_FOR_INSTANCE,
        DEL_SCENARIO_INSTANCE_CRITERIA,
        INS_SCENARIO_INSTANCE_CRITERIA,
        DEL_SCENARIO_INSTANCE_CRITERIA_FOR_INSTANCE,

        MAX_CHARACTERDATABASE_STATEMENTS
    }
}
