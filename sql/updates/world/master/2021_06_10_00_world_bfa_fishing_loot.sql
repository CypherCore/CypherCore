-- raid dazaralor 8.1.0
DELETE FROM `fishing_loot_template` WHERE `Entry`=10076;
INSERT INTO `fishing_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(10076, 2000, 2000, 100, 0, 1, 1, 1, 1, 'dazaralor-raid fishing');

DELETE FROM `reference_loot_template` WHERE `Entry`=2000;
INSERT INTO `reference_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(2000, 152543, 0, 40, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 152544, 0, 39, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 152547, 0, 7, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 152549, 0, 7, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 162515, 0, 4, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 163131, 0, 0.01, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 27422 , 0, 0.001, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 6291  , 0, 0.001, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 133607, 0, 0.3, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 154788, 0, 0.7, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 160927, 0, 0.6, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 154770, 0, 0.6, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 154771, 0, 0.6, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 154796, 0, 0.6, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 160934, 0, 0.6, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 154779, 0, 0.6, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 154787, 0, 0.6, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 160930, 0, 0.5, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 160925, 0, 0.5, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 154785, 0, 0.5, 0, 1, 1, 1, 1, 'dazaralor-raid fishing'),
(2000, 154792, 0, 0.5, 0, 1, 1, 1, 1, 'dazaralor-raid fishing');
 
 
 
-- raid kultiras 8.1.5
DELETE FROM `fishing_loot_template` WHERE `Entry`=10057;
INSERT INTO `fishing_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(10057, 2001, 2001, 100, 0, 1, 1, 1, 1, 'crisoldelastormentas-raid fishing');

DELETE FROM `reference_loot_template` WHERE `Entry`=2001;
INSERT INTO `reference_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(2001, 152547, 0, 50, 0, 1, 1, 1, 1, 'crisoldelastormentas-raid fishing'),
(2001, 152548, 0, 47, 0, 1, 1, 1, 1, 'crisoldelastormentas-raid fishing'),
(2001, 162515, 0, 1.1, 0, 1, 1, 1, 1, 'crisoldelastormentas-raid fishing'),
(2001, 154770, 0, 1.1, 0, 1, 1, 1, 1, 'crisoldelastormentas-raid fishing'),
(2001, 154788, 0, 1.1, 0, 1, 1, 1, 1, 'crisoldelastormentas-raid fishing'),
(2001, 163131, 0, 0, 0, 1, 1, 1, 1, ''), --
(2001, 154787, 0, 1.1, 0, 1, 1, 1, 1, 'crisoldelastormentas-raid fishing');


-- raid nazjatar 8.2.0
DELETE FROM `fishing_loot_template` WHERE `Entry`=10425;
INSERT INTO `fishing_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(10425, 2002, 2002, 100, 0, 1, 1, 1, 1, 'nazjatar-raid fishing');

DELETE FROM `reference_loot_template` WHERE `Entry`=2002;
INSERT INTO `reference_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(2002, 41809 , 0, 11, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 41810 , 0, 4, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 43572 , 0, 4, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 41812 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 12238 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 83065 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 27437 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 168646, 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 43329 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 152543, 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 168262, 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 43571 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 83064 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 47399 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 167659, 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 133607, 0, 13, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 27422 , 0, 7, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 152545, 0, 7, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 124112, 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 41808 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 74866 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 152547, 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 6291  , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 8365  , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 13758 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 45195 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 27441 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 121372, 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 45189 , 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing'),
(2002, 163131, 0, 0, 0, 1, 1, 1, 1, ''), --
(2002, 121373, 0, 2, 0, 1, 1, 1, 1, 'nazjatar-raid fishing');



-- raid nyalotha 8.3.0
DELETE FROM `fishing_loot_template` WHERE `Entry`=10522;
INSERT INTO `fishing_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(10522, 2003, 2003, 100, 0, 1, 1, 1, 1, 'nyalotha-raid fishing');

DELETE FROM `reference_loot_template` WHERE `Entry`=2003;
INSERT INTO `reference_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(2003, 168646 , 0, 6, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 6458   , 0, 6, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 110294 , 0, 3, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 27425  , 0, 3, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 139406 , 0, 3, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 41808  , 0, 19, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 74866  , 0, 9, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 44475  , 0, 9, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 6299   , 0, 6, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 133607 , 0, 6, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 41807  , 0, 3, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 152545 , 0, 3, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 152547 , 0, 3, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 6308   , 0, 3, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 6303   , 0, 3, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 13889  , 0, 3, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 74860  , 0, 3, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 27441  , 0, 3, 0, 1, 1, 1, 1, 'nyalotha-raid fishing'),
(2003, 163131, 0, 0, 0, 1, 1, 1, 1, ''), --
(2003, 121373 , 0, 3, 0, 1, 1, 1, 1, 'nyalotha-raid fishing');

-- Tiragarde sound
DELETE FROM `fishing_loot_template` WHERE `Entry`=8567;
INSERT INTO `fishing_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(8567, 2004, 2004, 100, 0, 1, 1, 1, 1, 'Tiragarde sound fishing');

DELETE FROM `reference_loot_template` WHERE `Entry`=2004;
INSERT INTO `reference_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(2004, 162516 , 0, 0.9, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 152511 , 0, 0.001, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 152506 , 0, 0.001, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 159186 , 0, 0.001, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 152545 , 0, 47, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 152546 , 0, 47, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 163131 , 0, 0.04, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 152547 , 0, 0.001, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 152548 , 0, 0.001, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 162515 , 0, 0.001, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 160925 , 0, 0.8, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 154787 , 0, 0.8, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 160930 , 0, 0.7, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 154770 , 0, 0.7, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 154771 , 0, 0.7, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 154796 , 0, 0.7, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 160934 , 0, 0.7, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 154779 , 0, 0.7, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 154792 , 0, 0.7, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 160927 , 0, 0.001, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 154801 , 0, 0.001, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 154785 , 0, 0.001, 0, 1, 1, 1, 1, 'Tiragarde sound fishing'),
(2004, 154788 , 0, 0.001, 0, 1, 1, 1, 1, 'Tiragarde sound fishing');

-- drustvar
DELETE FROM `fishing_loot_template` WHERE `Entry`=8721;
INSERT INTO `fishing_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(8721, 2005, 2005, 100, 0, 1, 1, 1, 1, 'drustvar fishing');

DELETE FROM `reference_loot_template` WHERE `Entry`=2005;
INSERT INTO `reference_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(2005, 7307   , 0, 0.001, 0, 1, 1, 5, 5, 'drustvar fishing'),
(2005, 152511 , 0, 0.001, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 152545 , 0, 51, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 152546 , 0, 26, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 152547 , 0, 14, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 152548 , 0, 3, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 162515 , 0, 3, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 163131 , 0, 0.07, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 124669 , 0, 0.01 , 0, 1, 1, 2, 5, 'drustvar fishing'),
(2005, 124670 , 0, 0.01, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 160930 , 0, 0.5, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 160927 , 0, 0.5, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 154770 , 0, 0.5, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 154771 , 0, 0.5, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 154801 , 0, 0.5, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 154779 , 0, 0.5, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 154788 , 0, 0.5, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 154796 , 0, 0.4, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 160925 , 0, 0.4, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 160934 , 0, 0.4, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 154785 , 0, 0.4, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 154792 , 0, 0.4, 0, 1, 1, 1, 1, 'drustvar fishing'),
(2005, 154787 , 0, 0.4, 0, 1, 1, 1, 1, 'drustvar fishing');
  

-- stormsong valley
DELETE FROM `fishing_loot_template` WHERE `Entry`=9042;
INSERT INTO `fishing_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(9042, 2006, 2006, 100, 0, 1, 1, 1, 1, 'stormsong valley fishing');

DELETE FROM `reference_loot_template` WHERE `Entry`=2006;
INSERT INTO `reference_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(2006, 152546, 0, 29, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 152545, 0, 28, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 152548, 0, 22, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 152547, 0, 19, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 162515, 0, 3, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 163131, 0, 0.03, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 124669, 0, 0.001, 0, 1, 1, 5, 5, 'stormsong valley fishing'),
(2006, 124670, 0, 0.001, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 160927, 0, 0.16, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 160925, 0, 0.16, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 154801, 0, 0.15, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 154779, 0, 0.15, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 154792, 0, 0.15, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 154771, 0, 0.13, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 154796, 0, 0.13, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 160934, 0, 0.13, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 154785, 0, 0.13, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 154770, 0, 0.12, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 154788, 0, 0.12, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 154787, 0, 0.12, 0, 1, 1, 1, 1, 'stormsong valley fishing'),
(2006, 160930, 0, 0.11, 0, 1, 1, 1, 1, 'stormsong valley fishing');

 
 
 

-- mechagon
DELETE FROM `fishing_loot_template` WHERE `Entry`=10290;
INSERT INTO `fishing_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(10290, 2007, 2007, 100, 0, 1, 1, 1, 1, 'mechagon fishing'),
(10290, 169391, 0, 5, 1, 1, 1, 1, 1, 'mechagon fishing'),
(10290, 167671, 0, 0.7, 1, 1, 1, 1, 1, 'mechagon fishing'),
(10290, 167670, 0, 0.6, 1, 1, 1, 1, 1, 'mechagon fishing'),
(10290, 167669, 0, 0.13, 1, 1, 1, 1, 1, 'mechagon fishing');

DELETE FROM `reference_loot_template` WHERE `Entry`=2007;
INSERT INTO `reference_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(2007, 168262, 0, 34, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 167562, 0, 20, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 166846, 0, 5 , 0, 1, 1, 1, 9, 'mechagon fishing'),
(2007, 166971, 0, 1.1, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 166970, 0, 0.5, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 167661, 0, 0.4, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 167655, 0, 0.3, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 167654, 0, 0.3, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 167662, 0, 0.3, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 167658, 0, 0.2, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 167663, 0, 0.2, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 167656, 0, 0.19, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 167657, 0, 0.19, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 167659, 0, 0.13, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 167660, 0, 0.15, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 152546, 0, 15, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 152545, 0, 14, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 162515, 0, 3, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 163131, 0, 0.01, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 74866 , 0, 0.001, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 133607, 0, 0.001, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 154770, 0, 0.5, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 154771, 0, 0.5, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 154801, 0, 0.5, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 154796, 0, 0.5, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 160934, 0, 0.5, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 154785, 0, 0.5, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 154779, 0, 0.5, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 154788, 0, 0.5, 0, 1, 1, 1, 1, 'mechagon fishing'),
(2007, 154787, 0, 0.5, 0, 1, 1, 1, 1, 'mechagon fishing');
 

-- zuldazar 
DELETE FROM `fishing_loot_template` WHERE `Entry`=8499;
INSERT INTO `fishing_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(8499, 2011, 2011, 100, 0, 1, 1, 1, 1, 'zuldazar fishing');

DELETE FROM `reference_loot_template` WHERE `Entry`=2011; -- 2210339 total 
INSERT INTO `reference_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(2011, 152543, 0, 36.83, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 152544, 0, 37.53, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 162517, 0, 5.13, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 124109, 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 27437 , 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 143748, 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 167658, 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 162515, 0, 6.38, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 163131, 0, 0.5, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 27422 , 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 41808 , 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 124669, 0, 0.006, 0, 1, 1, 3, 4, 'zuldazar fishing'),
(2011, 152545, 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 74857 , 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 74866 , 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 152547, 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 152549, 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 124670, 0, 0.006, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 133607, 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 152548, 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 160930, 0, 1.03, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 160927, 0, 1.14, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 154770, 0, 1.04, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 154771, 0, 1.13, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 154801, 0, 1.08, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 160925, 0, 1.14, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 160934, 0, 1.08, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 154785, 0, 1.08, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 154779, 0, 1.13, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 154792, 0, 1.13, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 154788, 0, 1.13, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 154787, 0, 1.00, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 154796, 0, 0.92, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 45190 , 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 27441 , 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 121371, 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing'),
(2011, 45189 , 0, 0.001, 0, 1, 1, 1, 1, 'zuldazar fishing');


-- nazmir
DELETE FROM `fishing_loot_template` WHERE `Entry`=8500;
INSERT INTO `fishing_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(8500, 2008, 2008, 100, 0, 1, 1, 1, 1, 'nazmir fishing');

DELETE FROM `reference_loot_template` WHERE `Entry`=2008;
INSERT INTO `reference_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(2008, 152543, 0, 24, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 152544, 0, 24, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 152505, 0, 0.01, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 152549, 0, 25, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 152547, 0, 24, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 162515, 0, 3, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 163131, 0, 0.01, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 160925, 0, 0.3, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 160930, 0, 0.2, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 160927, 0, 0.2, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 154770, 0, 0.2, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 154771, 0, 0.2, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 154801, 0, 0.2, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 154796, 0, 0.2, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 160934, 0, 0.2, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 154785, 0, 0.2, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 154779, 0, 0.2, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 154792, 0, 0.2, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 154788, 0, 0.2, 0, 1, 1, 1, 1, 'nazmir fishing'),
(2008, 154787, 0, 0.2, 0, 1, 1, 1, 1, 'nazmir fishing');

-- voldun
DELETE FROM `fishing_loot_template` WHERE `Entry`=8501;
INSERT INTO `fishing_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(8501, 2009, 2009, 100, 0, 1, 1, 1, 1, 'voldun fishing');

DELETE FROM `reference_loot_template` WHERE `Entry`=2009;
INSERT INTO `reference_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(2009, 152544, 0, 47, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 152543, 0, 46, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 157844, 0, 0.001, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 162515, 0, 3, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 152547, 0, 2, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 152549, 0, 2, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 163131, 0, 0.03, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 154770, 0, 0.3, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 154801, 0, 0.3, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 154796, 0, 0.3, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 160925, 0, 0.3, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 154785, 0, 0.3, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 154779, 0, 0.3, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 154788, 0, 0.3, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 154787, 0, 0.3, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 160930, 0, 0.2, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 160927, 0, 0.2, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 160934, 0, 0.2, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 154771, 0, 0.2, 0, 1, 1, 1, 1, 'voldun fishing'),
(2009, 154792, 0, 0.2, 0, 1, 1, 1, 1, 'voldun fishing');

 
-- nazjatar
DELETE FROM `fishing_loot_template` WHERE `Entry`=10052;
INSERT INTO `fishing_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(10052, 2010, 2010, 100, 0, 1, 1, 1, 1, 'nazjatar fishing');


DELETE FROM `reference_loot_template` WHERE `Entry`=2010;
INSERT INTO `reference_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`, `Comment`) VALUES 
(2010, 168302, 0, 49, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 168646, 0, 48, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 167059, 0, 0.11, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 167659, 0, 0.1, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 162515, 0, 3, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 133607, 0, 0.001, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 143662, 0, 0.001, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 160930, 0, 0.3, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 154796, 0, 0.3, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 154779, 0, 0.3, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 154787, 0, 0.3, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 160927, 0, 0.2, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 154770, 0, 0.2, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 154771, 0, 0.2, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 154801, 0, 0.2, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 160925, 0, 0.2, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 160934, 0, 0.2, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 154785, 0, 0.2, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 154792, 0, 0.2, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 163131, 0, 0, 0, 1, 1, 1, 1, ''), --
(2010, 168155, 0, 0.1, 0, 1, 1, 1, 1, 'nazjatar fishing'),
(2010, 154788, 0, 0.2, 0, 1, 1, 1, 1, 'nazjatar fishing');

--  Great Sea Ray Mount
UPDATE `reference_loot_template` SET `Chance`= 0.25,`Comment`='Great Sea Ray Mount'  WHERE `Item`=163131;

