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
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;

namespace Game.Chat
{
    [CommandGroup("modify")]
    class ModifyCommand
    {
        [Command("hp", RBACPermissions.CommandModifyHp)]
        static bool HandleModifyHPCommand(CommandHandler handler, int hp, int? maxHp)
        {
            Player target = handler.GetSelectedPlayerOrSelf();
            if (CheckModifyResources(handler, target, ref hp, ref maxHp))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeHp, CypherStrings.YoursHpChanged, hp, maxHp);
                target.SetMaxHealth((uint)maxHp.Value);
                target.SetHealth((uint)hp);
                return true;
            }
            return false;
        }

        [Command("mana", RBACPermissions.CommandModifyMana)]
        static bool HandleModifyManaCommand(CommandHandler handler, int mana, int? maxMana)
        {
            Player target = handler.GetSelectedPlayerOrSelf();

            if (CheckModifyResources(handler, target, ref mana, ref maxMana))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeMana, CypherStrings.YoursManaChanged, mana, maxMana.Value);
                target.SetMaxPower(PowerType.Mana, maxMana.Value);
                target.SetPower(PowerType.Mana, mana);
                return true;
            }

            return false;
        }

        [Command("energy", RBACPermissions.CommandModifyEnergy)]
        static bool HandleModifyEnergyCommand(CommandHandler handler, int energy, int? maxEnergy)
        {
            Player target = handler.GetSelectedPlayerOrSelf();
            byte energyMultiplier = 10;
            if (CheckModifyResources(handler, target, ref energy, ref maxEnergy, energyMultiplier))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeEnergy, CypherStrings.YoursEnergyChanged, energy / energyMultiplier, maxEnergy.Value / energyMultiplier);
                target.SetMaxPower(PowerType.Energy, maxEnergy.Value);
                target.SetPower(PowerType.Energy, energy);
                return true;
            }
            return false;
        }

        [Command("rage", RBACPermissions.CommandModifyRage)]
        static bool HandleModifyRageCommand(CommandHandler handler, int rage, int? maxRage)
        {
            Player target = handler.GetSelectedPlayerOrSelf();
            byte rageMultiplier = 10;
            if (CheckModifyResources(handler, target, ref rage, ref maxRage, rageMultiplier))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeRage, CypherStrings.YoursRageChanged, rage / rageMultiplier, maxRage.Value / rageMultiplier);
                target.SetMaxPower(PowerType.Rage, maxRage.Value);
                target.SetPower(PowerType.Rage, rage);
                return true;
            }
            return false;
        }

        [Command("runicpower", RBACPermissions.CommandModifyRunicpower)]
        static bool HandleModifyRunicPowerCommand(CommandHandler handler, int rune, int? maxRune)
        {
            Player target = handler.GetSelectedPlayerOrSelf();
            byte runeMultiplier = 10;
            if (CheckModifyResources(handler, target, ref rune, ref maxRune, runeMultiplier))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeRunicPower, CypherStrings.YoursRunicPowerChanged, rune / runeMultiplier, maxRune.Value / runeMultiplier);
                target.SetMaxPower(PowerType.RunicPower, maxRune.Value);
                target.SetPower(PowerType.RunicPower, rune);
                return true;
            }
            return false;
        }

        [Command("faction", RBACPermissions.CommandModifyFaction)]
        static bool HandleModifyFactionCommand(CommandHandler handler, StringArguments args)
        {
            string pfactionid = handler.ExtractKeyFromLink(args, "Hfaction");

            Creature target = handler.GetSelectedCreature();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            if (!uint.TryParse(pfactionid, out uint factionid))
            {
                uint _factionid = target.GetFaction();
                uint _flag = target.m_unitData.Flags;
                ulong _npcflag = (ulong)target.m_unitData.NpcFlags[0] << 32 | target.m_unitData.NpcFlags[1];
                uint _dyflag = target.m_objectData.DynamicFlags;
                handler.SendSysMessage(CypherStrings.CurrentFaction, target.GetGUID().ToString(), _factionid, _flag, _npcflag, _dyflag);
                return true;
            }

            if (!uint.TryParse(args.NextString(), out uint flag))
                flag = target.m_unitData.Flags;

            if (!ulong.TryParse(args.NextString(), out ulong npcflag))
                npcflag = (ulong)target.m_unitData.NpcFlags[0] << 32 | target.m_unitData.NpcFlags[1];

            if (!uint.TryParse(args.NextString(), out uint dyflag))
                dyflag = target.m_objectData.DynamicFlags;

            if (!CliDB.FactionTemplateStorage.ContainsKey(factionid))
            {
                handler.SendSysMessage(CypherStrings.WrongFaction, factionid);
                return false;
            }

            handler.SendSysMessage(CypherStrings.YouChangeFaction, target.GetGUID().ToString(), factionid, flag, npcflag, dyflag);

            target.SetFaction(factionid);
            target.ReplaceAllUnitFlags((UnitFlags)flag);
            target.ReplaceAllNpcFlags((NPCFlags)(npcflag & 0xFFFFFFFF));
            target.ReplaceAllNpcFlags2((NPCFlags2)(npcflag >> 32));
            target.ReplaceAllDynamicFlags((UnitDynFlags)dyflag);

            return true;
        }

        [Command("spell", RBACPermissions.CommandModifySpell)]
        static bool HandleModifySpellCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            byte spellflatid = args.NextByte();
            if (spellflatid == 0)
                return false;

            byte op = args.NextByte();
            if (op == 0)
                return false;

            ushort val = args.NextUInt16();
            if (val == 0)
                return false;

            if (!ushort.TryParse(args.NextString(), out ushort mark))
                mark = 65535;

            Player target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            handler.SendSysMessage(CypherStrings.YouChangeSpellflatid, spellflatid, val, mark, handler.GetNameLink(target));
            if (handler.NeedReportToTarget(target))
                target.SendSysMessage(CypherStrings.YoursSpellflatidChanged, handler.GetNameLink(), spellflatid, val, mark);

            SetSpellModifier packet = new(ServerOpcodes.SetFlatSpellModifier);
            SpellModifierInfo spellMod = new();
            spellMod.ModIndex = op;
            SpellModifierData modData;
            modData.ClassIndex = spellflatid;
            modData.ModifierValue = val;
            spellMod.ModifierData.Add(modData);
            packet.Modifiers.Add(spellMod);
            target.SendPacket(packet);

            return true;
        }

        [Command("talentpoints", RBACPermissions.CommandModifyTalentpoints)]
        static bool HandleModifyTalentCommand(CommandHandler handler) { return false; }

        [Command("scale", RBACPermissions.CommandModifyScale)]
        static bool HandleModifyScaleCommand(CommandHandler handler, StringArguments args)
        {
            float Scale;
            Unit target = handler.GetSelectedUnit();
            if (CheckModifySpeed(args, handler, target, out Scale, 0.1f, 10.0f, false))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeSize, CypherStrings.YoursSizeChanged, Scale);
                Creature creatureTarget = target.ToCreature();
                if (creatureTarget)
                    creatureTarget.SetDisplayId(creatureTarget.GetDisplayId(), Scale);
                else
                    target.SetObjectScale(Scale);
                return true;
            }
            return false;
        }

        [Command("mount", RBACPermissions.CommandModifyMount)]
        static bool HandleModifyMountCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            if (!uint.TryParse(args.NextString(), out uint mount))
                return false;

            if (!CliDB.CreatureDisplayInfoStorage.HasRecord(mount))
            {
                handler.SendSysMessage(CypherStrings.NoMount);
                return false;
            }

            Player target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            float speed;
            if (!CheckModifySpeed(args, handler, target, out speed, 0.1f, 50.0f))
                return false;

            NotifyModification(handler, target, CypherStrings.YouGiveMount, CypherStrings.MountGived);
            target.Mount(mount);
            target.SetSpeedRate(UnitMoveType.Run, speed);
            target.SetSpeedRate(UnitMoveType.Flight, speed);
            return true;
        }

        [Command("money", RBACPermissions.CommandModifyMoney)]
        static bool HandleModifyMoneyCommand(CommandHandler handler, StringArguments args)
        {
            Player target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            long moneyToAdd = args.NextInt64();
            ulong targetMoney = target.GetMoney();

            if (moneyToAdd < 0)
            {
                long newmoney = (long)targetMoney + moneyToAdd;

                Log.outDebug(LogFilter.ChatSystem, Global.ObjectMgr.GetCypherString(CypherStrings.CurrentMoney), targetMoney, moneyToAdd, newmoney);
                if (newmoney <= 0)
                {
                    handler.SendSysMessage(CypherStrings.YouTakeAllMoney, handler.GetNameLink(target));
                    if (handler.NeedReportToTarget(target))
                        target.SendSysMessage(CypherStrings.YoursAllMoneyGone, handler.GetNameLink());

                    target.SetMoney(0);
                }
                else
                {
                    ulong moneyToAddMsg = (ulong)(moneyToAdd * -1);
                    if (newmoney > (long)PlayerConst.MaxMoneyAmount)
                        newmoney = (long)PlayerConst.MaxMoneyAmount;

                    handler.SendSysMessage(CypherStrings.YouTakeMoney, moneyToAddMsg, handler.GetNameLink(target));
                    if (handler.NeedReportToTarget(target))
                        target.SendSysMessage(CypherStrings.YoursMoneyTaken, handler.GetNameLink(), moneyToAddMsg);
                    target.SetMoney((ulong)newmoney);
                }
            }
            else
            {
                handler.SendSysMessage(CypherStrings.YouGiveMoney, moneyToAdd, handler.GetNameLink(target));
                if (handler.NeedReportToTarget(target))
                    target.SendSysMessage(CypherStrings.YoursMoneyGiven, handler.GetNameLink(), moneyToAdd);

                if ((ulong)moneyToAdd >= PlayerConst.MaxMoneyAmount)
                    moneyToAdd = Convert.ToInt64(PlayerConst.MaxMoneyAmount);

                moneyToAdd = (long)Math.Min((ulong)moneyToAdd, (PlayerConst.MaxMoneyAmount - targetMoney));

                target.ModifyMoney(moneyToAdd);
            }

            Log.outDebug(LogFilter.ChatSystem, Global.ObjectMgr.GetCypherString(CypherStrings.NewMoney), targetMoney, moneyToAdd, target.GetMoney());
            return true;
        }

        [Command("honor", RBACPermissions.CommandModifyHonor)]
        static bool HandleModifyHonorCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            Player target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            int amount = args.NextInt32();

            //target.ModifyCurrency(CurrencyTypes.HonorPoints, amount, true, true);
            handler.SendSysMessage("NOT IMPLEMENTED: {0} honor NOT added.", amount);

            //handler.SendSysMessage(CypherStrings.CommandModifyHonor, handler.GetNameLink(target), target.GetCurrency((uint)CurrencyTypes.HonorPoints));
            return true;
        }

        [Command("drunk", RBACPermissions.CommandModifyDrunk)]
        static bool HandleModifyDrunkCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            byte drunklevel = args.NextByte();
            if (drunklevel > 100)
                drunklevel = 100;

            Player target = handler.GetSelectedPlayerOrSelf();
            if (target)
                target.SetDrunkValue(drunklevel);

            return true;
        }

        [Command("reputation", RBACPermissions.CommandModifyReputation)]
        static bool HandleModifyRepCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            Player target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            string factionTxt = handler.ExtractKeyFromLink(args, "Hfaction");
            if (string.IsNullOrEmpty(factionTxt))
                return false;

            if (!uint.TryParse(factionTxt, out uint factionId))
                return false;

            string rankTxt = args.NextString();
            if (factionId == 0 || !int.TryParse(rankTxt, out int amount))
                return false;

            var factionEntry = CliDB.FactionStorage.LookupByKey(factionId);
            if (factionEntry == null)
            {
                handler.SendSysMessage(CypherStrings.CommandFactionUnknown, factionId);
                return false;
            }

            if (factionEntry.ReputationIndex < 0)
            {
                handler.SendSysMessage(CypherStrings.CommandFactionNorepError, factionEntry.Name[handler.GetSessionDbcLocale()], factionId);
                return false;
            }

            // try to find rank by name
            if ((amount == 0) && !(amount < 0) && !rankTxt.IsNumber())
            {
                string rankStr = rankTxt.ToLower();

                int i = 0;
                int r = 0;

                for (; i != ReputationMgr.ReputationRankThresholds.Length - 1; ++i, ++r)
                {
                    string rank = handler.GetCypherString(ReputationMgr.ReputationRankStrIndex[r]);
                    if (string.IsNullOrEmpty(rank))
                        continue;

                    if (rank.Equals(rankStr, StringComparison.OrdinalIgnoreCase))
                        break;

                    if (i == ReputationMgr.ReputationRankThresholds.Length - 1)
                    {
                        handler.SendSysMessage(CypherStrings.CommandInvalidParam, rankTxt);
                        return false;
                    }

                    amount = ReputationMgr.ReputationRankThresholds[i];

                    string deltaTxt = args.NextString();
                    if (!string.IsNullOrEmpty(deltaTxt))
                    {
                        int toNextRank = 0;
                        var nextThresholdIndex = i;
                        ++nextThresholdIndex;
                        if (nextThresholdIndex != ReputationMgr.ReputationRankThresholds.Length - 1)
                            toNextRank = nextThresholdIndex - i;

                        if (!int.TryParse(deltaTxt, out int delta) || delta < 0 || delta >= toNextRank)
                        {
                            handler.SendSysMessage(CypherStrings.CommandFactionDelta, Math.Max(0, toNextRank - 1));
                            return false;
                        }
                        amount += delta;
                    }
                }
            }

            target.GetReputationMgr().SetOneFactionReputation(factionEntry, amount, false);
            target.GetReputationMgr().SendState(target.GetReputationMgr().GetState(factionEntry));
            handler.SendSysMessage(CypherStrings.CommandModifyRep, factionEntry.Name[handler.GetSessionDbcLocale()], factionId, handler.GetNameLink(target), target.GetReputationMgr().GetReputation(factionEntry));

            return true;
        }

        [Command("phase", RBACPermissions.CommandModifyPhase)]
        static bool HandleModifyPhaseCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            uint phaseId = args.NextUInt32();
            uint visibleMapId = args.NextUInt32();

            if (phaseId != 0 && !CliDB.PhaseStorage.ContainsKey(phaseId))
            {
                handler.SendSysMessage(CypherStrings.PhaseNotfound);
                return false;
            }

            Unit target = handler.GetSelectedUnit();

            if (visibleMapId != 0)
            {
                MapRecord visibleMap = CliDB.MapStorage.LookupByKey(visibleMapId);
                if (visibleMap == null || visibleMap.ParentMapID != target.GetMapId())
                {
                    handler.SendSysMessage(CypherStrings.PhaseNotfound);
                    return false;
                }

                if (!target.GetPhaseShift().HasVisibleMapId(visibleMapId))
                    PhasingHandler.AddVisibleMapId(target, visibleMapId);
                else
                    PhasingHandler.RemoveVisibleMapId(target, visibleMapId);
            }

            if (phaseId != 0)
            {
                if (!target.GetPhaseShift().HasPhase(phaseId))
                    PhasingHandler.AddPhase(target, phaseId, true);
                else
                    PhasingHandler.RemovePhase(target, phaseId, true);
            }

            return true;
        }

        [Command("power", RBACPermissions.CommandModifyPower)]
        static bool HandleModifyPowerCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            Player target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            string powerTypeToken = args.NextString();
            if (powerTypeToken.IsEmpty())
                return false;

            PowerTypeRecord powerType = Global.DB2Mgr.GetPowerTypeByName(powerTypeToken);
            if (powerType == null)
            {
                handler.SendSysMessage(CypherStrings.InvalidPowerName);
                return false;
            }

            if (target.GetPowerIndex(powerType.PowerTypeEnum) == (int)PowerType.Max)
            {
                handler.SendSysMessage(CypherStrings.InvalidPowerName);
                return false;
            }

            int powerAmount = args.NextInt32();
            if (powerAmount < 1)
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            NotifyModification(handler, target, CypherStrings.YouChangePower, CypherStrings.YourPowerChanged, powerType.NameGlobalStringTag, powerAmount, powerAmount);
            powerAmount *= powerType.DisplayModifier;
            target.SetMaxPower(powerType.PowerTypeEnum, powerAmount);
            target.SetPower(powerType.PowerTypeEnum, powerAmount);
            return true;
        }

        [Command("standstate", RBACPermissions.CommandModifyStandstate)]
        static bool HandleModifyStandStateCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            uint anim_id = args.NextUInt32();
            handler.GetSession().GetPlayer().SetEmoteState((Emote)anim_id);

            return true;
        }

        [Command("gender", RBACPermissions.CommandModifyGender)]
        static bool HandleModifyGenderCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            Player target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            PlayerInfo info = Global.ObjectMgr.GetPlayerInfo(target.GetRace(), target.GetClass());
            if (info == null)
                return false;

            string gender_str = args.NextString();
            Gender gender;

            if (gender_str == "male")            // MALE
            {
                if (target.GetGender() == Gender.Male)
                    return true;

                gender = Gender.Male;
            }
            else if (gender_str == "female")    // FEMALE
            {
                if (target.GetGender() == Gender.Female)
                    return true;

                gender = Gender.Female;
            }
            else
            {
                handler.SendSysMessage(CypherStrings.MustMaleOrFemale);
                return false;
            }

            // Set gender
            target.SetGender(gender);
            target.SetNativeGender(gender);

            // Change display ID
            target.InitDisplayIds();

            target.RestoreDisplayId(false);
            Global.CharacterCacheStorage.UpdateCharacterGender(target.GetGUID(), (byte)gender);

            // Generate random customizations
            List<ChrCustomizationChoice> customizations = new();

            var options = Global.DB2Mgr.GetCustomiztionOptions(target.GetRace(), gender);
            WorldSession worldSession = target.GetSession();
            foreach (ChrCustomizationOptionRecord option in options)
            {
                ChrCustomizationReqRecord optionReq = CliDB.ChrCustomizationReqStorage.LookupByKey(option.ChrCustomizationReqID);
                if (optionReq != null && !worldSession.MeetsChrCustomizationReq(optionReq, target.GetClass(), false, customizations))
                    continue;

                // Loop over the options until the first one fits
                var choicesForOption = Global.DB2Mgr.GetCustomiztionChoices(option.Id);
                foreach (ChrCustomizationChoiceRecord choiceForOption in choicesForOption)
                {
                    var choiceReq = CliDB.ChrCustomizationReqStorage.LookupByKey(choiceForOption.ChrCustomizationReqID);
                    if (choiceReq != null && !worldSession.MeetsChrCustomizationReq(choiceReq, target.GetClass(), false, customizations))
                        continue;

                    ChrCustomizationChoiceRecord choiceEntry = choicesForOption[0];
                    ChrCustomizationChoice choice = new();
                    choice.ChrCustomizationOptionID = option.Id;
                    choice.ChrCustomizationChoiceID = choiceEntry.Id;
                    customizations.Add(choice);
                    break;
                }
            }

            target.SetCustomizations(customizations);

            handler.SendSysMessage(CypherStrings.YouChangeGender, handler.GetNameLink(target), gender);

            if (handler.NeedReportToTarget(target))
                target.SendSysMessage(CypherStrings.YourGenderChanged, gender, handler.GetNameLink());

            return true;
        }

        [Command("currency", RBACPermissions.CommandModifyCurrency)]
        static bool HandleModifyCurrencyCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            Player target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);

                return false;
            }

            uint currencyId = args.NextUInt32();
            if (!CliDB.CurrencyTypesStorage.ContainsKey(currencyId))
                return false;

            uint amount = args.NextUInt32();
            if (amount == 0)
                return false;

            target.ModifyCurrency((CurrencyTypes)currencyId, (int)amount, true, true);

            return true;
        }

        [Command("xp", RBACPermissions.CommandModifyXp)]
        static bool HandleModifyXPCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            int xp = args.NextInt32();

            if (xp < 1)
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            Player target = handler.GetSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            // we can run the command
            target.GiveXP((uint)xp, null);
            return true;
        }

        [CommandNonGroup("morph", RBACPermissions.CommandMorph)]
        static bool HandleModifyMorphCommand(CommandHandler handler, StringArguments args)
        {
            if (args.Empty())
                return false;

            uint display_id = args.NextUInt32();

            Unit target = handler.GetSelectedUnit();
            if (!target)
                target = handler.GetSession().GetPlayer();

            // check online security
            else if (target.IsTypeId(TypeId.Player) && handler.HasLowerSecurity(target.ToPlayer(), ObjectGuid.Empty))
                return false;

            target.SetDisplayId(display_id);

            return true;
        }

        [CommandNonGroup("demorph", RBACPermissions.CommandDemorph)]
        static bool HandleDeMorphCommand(CommandHandler handler)
        {
            Unit target = handler.GetSelectedUnit();
            if (!target)
                target = handler.GetSession().GetPlayer();

            // check online security
            else if (target.IsTypeId(TypeId.Player) && handler.HasLowerSecurity(target.ToPlayer(), ObjectGuid.Empty))
                return false;

            target.DeMorph();

            return true;
        }

        [CommandGroup("speed")]
        class ModifySpeed
        {
            [Command("", RBACPermissions.CommandModifySpeed)]
            static bool HandleModifySpeedCommand(CommandHandler handler, StringArguments args)
            {
                return HandleModifyASpeedCommand(handler, args);
            }

            [Command("all", RBACPermissions.CommandModifySpeedAll)]
            static bool HandleModifyASpeedCommand(CommandHandler handler, StringArguments args)
            {
                float allSpeed;
                Player target = handler.GetSelectedPlayerOrSelf();
                if (CheckModifySpeed(args, handler, target, out allSpeed, 0.1f, 50.0f))
                {
                    NotifyModification(handler, target, CypherStrings.YouChangeAspeed, CypherStrings.YoursAspeedChanged, allSpeed);
                    target.SetSpeedRate(UnitMoveType.Walk, allSpeed);
                    target.SetSpeedRate(UnitMoveType.Run, allSpeed);
                    target.SetSpeedRate(UnitMoveType.Swim, allSpeed);
                    target.SetSpeedRate(UnitMoveType.Flight, allSpeed);
                    return true;
                }
                return false;
            }

            [Command("swim", RBACPermissions.CommandModifySpeedSwim)]
            static bool HandleModifySwimCommand(CommandHandler handler, StringArguments args)
            {
                float swimSpeed;
                Player target = handler.GetSelectedPlayerOrSelf();
                if (CheckModifySpeed(args, handler, target, out swimSpeed, 0.1f, 50.0f))
                {
                    NotifyModification(handler, target, CypherStrings.YouChangeSwimSpeed, CypherStrings.YoursSwimSpeedChanged, swimSpeed);
                    target.SetSpeedRate(UnitMoveType.Swim, swimSpeed);
                    return true;
                }
                return false;
            }

            [Command("backwalk", RBACPermissions.CommandModifySpeedBackwalk)]
            static bool HandleModifyBWalkCommand(CommandHandler handler, StringArguments args)
            {
                float backSpeed;
                Player target = handler.GetSelectedPlayerOrSelf();
                if (CheckModifySpeed(args, handler, target, out backSpeed, 0.1f, 50.0f))
                {
                    NotifyModification(handler, target, CypherStrings.YouChangeBackSpeed, CypherStrings.YoursBackSpeedChanged, backSpeed);
                    target.SetSpeedRate(UnitMoveType.RunBack, backSpeed);
                    return true;
                }
                return false;
            }

            [Command("fly", RBACPermissions.CommandModifySpeedFly)]
            static bool HandleModifyFlyCommand(CommandHandler handler, StringArguments args)
            {
                float flySpeed;
                Player target = handler.GetSelectedPlayerOrSelf();
                if (CheckModifySpeed(args, handler, target, out flySpeed, 0.1f, 50.0f, false))
                {
                    NotifyModification(handler, target, CypherStrings.YouChangeFlySpeed, CypherStrings.YoursFlySpeedChanged, flySpeed);
                    target.SetSpeedRate(UnitMoveType.Flight, flySpeed);
                    return true;
                }
                return false;
            }

            [Command("walk", RBACPermissions.CommandModifySpeedWalk)]
            static bool HandleModifyWalkSpeedCommand(CommandHandler handler, StringArguments args)
            {
                float Speed;
                Player target = handler.GetSelectedPlayerOrSelf();
                if (CheckModifySpeed(args, handler, target, out Speed, 0.1f, 50.0f))
                {
                    NotifyModification(handler, target, CypherStrings.YouChangeSpeed, CypherStrings.YoursSpeedChanged, Speed);
                    target.SetSpeedRate(UnitMoveType.Run, Speed);
                    return true;
                }
                return false;
            }
        }

        static void NotifyModification(CommandHandler handler, Unit target, CypherStrings resourceMessage, CypherStrings resourceReportMessage, params object[] args)
        {
            Player player = target.ToPlayer();
            if (player)
            {
                handler.SendSysMessage(resourceMessage, new object[] { handler.GetNameLink(player) }.Combine(args));
                if (handler.NeedReportToTarget(player))
                    player.SendSysMessage(resourceReportMessage, new object[] { handler.GetNameLink() }.Combine(args));
            }
        }

        static bool CheckModifyResources(CommandHandler handler, Player target, ref int res, ref int? resmax, byte multiplier = 1)
        {
            res *= multiplier;
            resmax *= multiplier;

            if (resmax == 0)
                resmax = res;

            if (res < 1 || resmax < 1 || resmax < res)
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            return true;
        }

        static bool CheckModifySpeed(StringArguments args, CommandHandler handler, Unit target, out float speed, float minimumBound, float maximumBound, bool checkInFlight = true)
        {
            speed = 0f;
            if (args.Empty())
                return false;

            speed = args.NextSingle();

            if (speed > maximumBound || speed < minimumBound)
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            Player player = target.ToPlayer();
            if (player)
            {
                // check online security
                if (handler.HasLowerSecurity(player, ObjectGuid.Empty))
                    return false;

                if (player.IsInFlight() && checkInFlight)
                {
                    handler.SendSysMessage(CypherStrings.CharInFlight, handler.GetNameLink(player));
                    return false;
                }
            }
            return true;
        }
    }
}
