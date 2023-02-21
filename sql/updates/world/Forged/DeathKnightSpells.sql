-- Copyright Forged Wow LLC
-- Licensed under GPL v3.0 https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE 

DELETE FROM spell_script_names WHERE spell_id IN (47482, 47484, 47481, 390270, 207289, 390260, 390259, 55090, 70890, 390279);
INSERT INTO spell_script_names
(spell_id, ScriptName)
VALUES(47482, 'spell_dk_ghoul_leap');
INSERT INTO spell_script_names
(spell_id, ScriptName)
VALUES(47484, 'spell_dk_ghoul_huddle');
INSERT INTO spell_script_names
(spell_id, ScriptName)
VALUES(47481, 'spell_dk_ghoul_gnaw');
INSERT INTO spell_script_names
(spell_id, ScriptName)
VALUES(390270, 'spell_dk_coil_of_devastation');
INSERT INTO spell_script_names
(spell_id, ScriptName)
VALUES(207289, 'spell_dk_unholy_assault');
INSERT INTO spell_script_names
(spell_id, ScriptName)
VALUES(390260, 'spell_dk_commander_of_the_dead_aura');
INSERT INTO spell_script_names
(spell_id, ScriptName)
VALUES(390259, 'spell_dk_commander_of_the_dead_aura_proc');
INSERT INTO spell_script_names
(spell_id, ScriptName)
VALUES(55090, 'spell_dk_scourge_strike');
INSERT INTO spell_script_names
(spell_id, ScriptName)
VALUES(70890, 'spell_dk_scourge_strike_trigger');

DELETE FROM spell_proc WHERE SpellId IN (390270, 390259);
INSERT INTO spell_proc
(SpellId, SchoolMask, SpellFamilyName, SpellFamilyMask0, SpellFamilyMask1, SpellFamilyMask2, SpellFamilyMask3, ProcFlags, ProcFlags2, SpellTypeMask, SpellPhaseMask, HitMask, AttributesMask, DisableEffectsMask, ProcsPerMinute, Chance, Cooldown, Charges)
VALUES(390270, 32, 15, 8192, 2048, 16385, 0, 65536, 4, 0, 2, 0, 2, 0, 0.0, 101.0, 0, 0);
INSERT INTO spell_proc
(SpellId, SchoolMask, SpellFamilyName, SpellFamilyMask0, SpellFamilyMask1, SpellFamilyMask2, SpellFamilyMask3, ProcFlags, ProcFlags2, SpellTypeMask, SpellPhaseMask, HitMask, AttributesMask, DisableEffectsMask, ProcsPerMinute, Chance, Cooldown, Charges)
VALUES(390259, 0, 15, 0, 0, 32, 0, 16384, 0, 0, 2, 0, 0, 0, 0.0, 101.0, 0, 0);

DELETE FROM areatrigger_create_properties WHERE Id IN (25740, 4485);
INSERT INTO areatrigger_create_properties
(Id, AreaTriggerId, MoveCurveId, ScaleCurveId, MorphCurveId, FacingCurveId, AnimId, AnimKitId, DecalPropertiesId, TimeToTarget, TimeToTargetScale, Shape, ShapeData0, ShapeData1, ShapeData2, ShapeData3, ShapeData4, ShapeData5, ShapeData6, ShapeData7, ScriptName, VerifiedBuild)
VALUES(25740, 25740, 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 8.0, 8.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 'at_dk_unholy_aura', 40120);
INSERT INTO areatrigger_create_properties
(Id, AreaTriggerId, MoveCurveId, ScaleCurveId, MorphCurveId, FacingCurveId, AnimId, AnimKitId, DecalPropertiesId, TimeToTarget, TimeToTargetScale, Shape, ShapeData0, ShapeData1, ShapeData2, ShapeData3, ShapeData4, ShapeData5, ShapeData6, ShapeData7, ScriptName, VerifiedBuild)
VALUES(4485, 9225, 0, 0, 0, 0, -1, 0, 0, 0, 10000, 0, 8.0, 8.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 'at_dk_death_and_decay', 47067);

INSERT INTO areatrigger_template
(Id, IsServerSide, `Type`, Flags, Data0, Data1, Data2, Data3, Data4, Data5, Data6, Data7, VerifiedBuild)
VALUES(25740, 0, 4, 4, 8.0, 8.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 43340);

