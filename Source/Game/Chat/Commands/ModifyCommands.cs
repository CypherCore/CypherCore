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

using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game.Chat
{
    [CommandGroup("modify", RBACPermissions.CommandModify)]
    class ModifyCommand
    {
        [Command("hp", RBACPermissions.CommandModifyHp)]
        static bool HandleModifyHPCommand(StringArguments args, CommandHandler handler)
        {
            int hp, hpmax = 0;
            Player target = handler.getSelectedPlayerOrSelf();
            if (CheckModifyResources(args, handler, target, out hp, out hpmax))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeHp, CypherStrings.YoursHpChanged, hp, hpmax);
                target.SetMaxHealth((uint)hpmax);
                target.SetHealth((uint)hp);
                return true;
            }
            return false;
        }

        [Command("mana", RBACPermissions.CommandModifyMana)]
        static bool HandleModifyManaCommand(StringArguments args, CommandHandler handler)
        {
            int mana, manamax;
            Player target = handler.getSelectedPlayerOrSelf();

            if (CheckModifyResources(args, handler, target, out mana, out manamax))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeMana, CypherStrings.YoursManaChanged, mana, manamax);
                target.SetMaxPower(PowerType.Mana, manamax);
                target.SetPower(PowerType.Mana, mana);
                return true;
            }

            return false;
        }

        [Command("energy", RBACPermissions.CommandModifyEnergy)]
        static bool HandleModifyEnergyCommand(StringArguments args, CommandHandler handler)
        {
            int energy, energymax;
            Player target = handler.getSelectedPlayerOrSelf();
            byte energyMultiplier = 10;
            if (CheckModifyResources(args, handler, target, out energy, out energymax, energyMultiplier))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeEnergy, CypherStrings.YoursEnergyChanged, energy / energyMultiplier, energymax / energyMultiplier);
                target.SetMaxPower(PowerType.Energy, energymax);
                target.SetPower(PowerType.Energy, energy);
                return true;
            }
            return false;
        }

        [Command("rage", RBACPermissions.CommandModifyRage)]
        static bool HandleModifyRageCommand(StringArguments args, CommandHandler handler)
        {
            int rage, ragemax;
            Player target = handler.getSelectedPlayerOrSelf();
            byte rageMultiplier = 10;
            if (CheckModifyResources(args, handler, target, out rage, out ragemax, rageMultiplier))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeRage, CypherStrings.YoursRageChanged, rage / rageMultiplier, ragemax / rageMultiplier);
                target.SetMaxPower(PowerType.Rage, ragemax);
                target.SetPower(PowerType.Rage, rage);
                return true;
            }
            return false;
        }

        [Command("runicpower", RBACPermissions.CommandModifyRunicpower)]
        static bool HandleModifyRunicPowerCommand(StringArguments args, CommandHandler handler)
        {
            int rune, runemax;
            Player target = handler.getSelectedPlayerOrSelf();
            byte runeMultiplier = 10;
            if (CheckModifyResources(args, handler, target, out rune, out runemax, runeMultiplier))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeRunicPower, CypherStrings.YoursRunicPowerChanged, rune / runeMultiplier, runemax / runeMultiplier);
                target.SetMaxPower(PowerType.RunicPower, runemax);
                target.SetPower(PowerType.RunicPower, rune);
                return true;
            }
            return false;
        }

        [Command("faction", RBACPermissions.CommandModifyFaction)]
        static bool HandleModifyFactionCommand(StringArguments args, CommandHandler handler)
        {
            string pfactionid = handler.extractKeyFromLink(args, "Hfaction");

            Creature target = handler.getSelectedCreature();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            if (!uint.TryParse(pfactionid, out uint factionid))
            {
                uint _factionid = target.getFaction();
                uint _flag = target.GetUInt32Value(UnitFields.Flags);
                ulong _npcflag = target.GetUInt64Value(UnitFields.NpcFlags);
                uint _dyflag = target.GetUInt32Value(ObjectFields.DynamicFlags);
                handler.SendSysMessage(CypherStrings.CurrentFaction, target.GetGUID().ToString(), _factionid, _flag, _npcflag, _dyflag);
                return true;
            }

            if (!uint.TryParse(args.NextString(), out uint flag))
                flag = target.GetUInt32Value(UnitFields.Flags);

            if (!ulong.TryParse(args.NextString(), out ulong npcflag))
                npcflag = target.GetUInt64Value(UnitFields.NpcFlags);

            if (!uint.TryParse(args.NextString(), out uint dyflag))
                dyflag = target.GetUInt32Value(ObjectFields.DynamicFlags);

            if (!CliDB.FactionTemplateStorage.ContainsKey(factionid))
            {
                handler.SendSysMessage(CypherStrings.WrongFaction, factionid);
                return false;
            }

            handler.SendSysMessage(CypherStrings.YouChangeFaction, target.GetGUID().ToString(), factionid, flag, npcflag, dyflag);

            target.SetFaction(factionid);
            target.SetUInt32Value(UnitFields.Flags, flag);
            target.SetUInt64Value(UnitFields.NpcFlags, npcflag);
            target.SetUInt32Value(ObjectFields.DynamicFlags, dyflag);

            return true;
        }

        [Command("spell", RBACPermissions.CommandModifySpell)]
        static bool HandleModifySpellCommand(StringArguments args, CommandHandler handler)
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

            Player target = handler.getSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            handler.SendSysMessage(CypherStrings.YouChangeSpellflatid, spellflatid, val, mark, handler.GetNameLink(target));
            if (handler.needReportToTarget(target))
                target.SendSysMessage(CypherStrings.YoursSpellflatidChanged, handler.GetNameLink(), spellflatid, val, mark);

            SetSpellModifier packet = new SetSpellModifier(ServerOpcodes.SetFlatSpellModifier);
            SpellModifierInfo spellMod = new SpellModifierInfo();
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
        static bool HandleModifyTalentCommand(StringArguments args, CommandHandler handler) { return false; }

        [Command("scale", RBACPermissions.CommandModifyScale)]
        static bool HandleModifyScaleCommand(StringArguments args, CommandHandler handler)
        {
            float Scale;
            Unit target = handler.getSelectedUnit();
            if (CheckModifySpeed(args, handler, target, out Scale, 0.1f, 10.0f, false))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeSize, CypherStrings.YoursSizeChanged, Scale);
                Creature creatureTarget = target.ToCreature();
                if (creatureTarget)
                    creatureTarget.SetFloatValue(UnitFields.DisplayScale, Scale);
                else
                    target.SetObjectScale(Scale);
                return true;
            }
            return false;
        }

        [Command("mount", RBACPermissions.CommandModifyMount)]
        static bool HandleModifyMountCommand(StringArguments args, CommandHandler handler)
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

            Player target = handler.getSelectedPlayerOrSelf();
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
        static bool HandleModifyMoneyCommand(StringArguments args, CommandHandler handler)
        {
            Player target = handler.getSelectedPlayerOrSelf();
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
                    if (handler.needReportToTarget(target))
                        target.SendSysMessage(CypherStrings.YoursAllMoneyGone, handler.GetNameLink());

                    target.SetMoney(0);
                }
                else
                {
                    ulong moneyToAddMsg = (ulong)(moneyToAdd * -1);
                    if (newmoney > (long)PlayerConst.MaxMoneyAmount)
                        newmoney = (long)PlayerConst.MaxMoneyAmount;

                    handler.SendSysMessage(CypherStrings.YouTakeMoney, moneyToAddMsg, handler.GetNameLink(target));
                    if (handler.needReportToTarget(target))
                        target.SendSysMessage(CypherStrings.YoursMoneyTaken, handler.GetNameLink(), moneyToAddMsg);
                    target.SetMoney((ulong)newmoney);
                }
            }
            else
            {
                handler.SendSysMessage(CypherStrings.YouGiveMoney, moneyToAdd, handler.GetNameLink(target));
                if (handler.needReportToTarget(target))
                    target.SendSysMessage(CypherStrings.YoursMoneyGiven, handler.GetNameLink(), moneyToAdd);

                if ((ulong)moneyToAdd >= PlayerConst.MaxMoneyAmount)
                    moneyToAdd = Convert.ToInt64(PlayerConst.MaxMoneyAmount);

                moneyToAdd = (long)Math.Min((ulong)moneyToAdd, (PlayerConst.MaxMoneyAmount - targetMoney));

                target.ModifyMoney(moneyToAdd);
            }

            Log.outDebug(LogFilter.ChatSystem, Global.ObjectMgr.GetCypherString(CypherStrings.NewMoney), targetMoney, moneyToAdd, target.GetMoney());
            return true;
        }

        [Command("bit", RBACPermissions.CommandModifyBit)]
        static bool HandleModifyBitCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Unit target = handler.getSelectedUnit();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);

                return false;
            }

            // check online security
            if (target.IsTypeId(TypeId.Player) && handler.HasLowerSecurity(target.ToPlayer(), ObjectGuid.Empty))
                return false;

            ushort field = args.NextUInt16();
            int bit = args.NextInt32();

            if (field < (int)ObjectFields.End || field >= target.valuesCount)
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }
            if (bit < 1 || bit > 32)
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            if (target.HasFlag(field, (1 << (bit - 1))))
            {
                target.RemoveFlag(field, (1 << (bit - 1)));
                handler.SendSysMessage(CypherStrings.RemoveBit, bit, field);
            }
            else
            {
                target.SetFlag(field, (1 << (bit - 1)));
                handler.SendSysMessage(CypherStrings.SetBit, bit, field);
            }
            return true;
        }

        [Command("honor", RBACPermissions.CommandModifyHonor)]
        static bool HandleModifyHonorCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target = handler.getSelectedPlayerOrSelf();
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
        static bool HandleModifyDrunkCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            byte drunklevel = args.NextByte();
            if (drunklevel > 100)
                drunklevel = 100;

            Player target = handler.getSelectedPlayerOrSelf();
            if (target)
                target.SetDrunkValue(drunklevel);

            return true;
        }

        [Command("reputation", RBACPermissions.CommandModifyReputation)]
        static bool HandleModifyRepCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target = handler.getSelectedPlayerOrSelf();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            string factionTxt = handler.extractKeyFromLink(args, "Hfaction");
            if (string.IsNullOrEmpty(factionTxt))
                return false;

            if (!uint.TryParse(factionTxt, out uint factionId))
                return false;

            int amount = 0;
            string rankTxt = args.NextString();
            if (factionId == 0 || !int.TryParse(rankTxt, out amount))
                return false;

            if ((amount == 0) && !(amount < 0) && !rankTxt.IsNumber())
            {
                string rankStr = rankTxt.ToLower();

                int r = 0;
                amount = -42000;
                for (; r < (int)ReputationRank.Max; ++r)
                {
                    string rank = handler.GetCypherString(ReputationMgr.ReputationRankStrIndex[r]);
                    if (string.IsNullOrEmpty(rank))
                        continue;

                    if (rank.Equals(rankStr))
                    {
                        string deltaTxt = args.NextString();
                        if (!string.IsNullOrEmpty(deltaTxt))
                        {
                            if (!int.TryParse(deltaTxt, out int delta) || delta < 0 || (delta > ReputationMgr.PointsInRank[r] - 1))
                            {
                                handler.SendSysMessage(CypherStrings.CommandFactionDelta, (ReputationMgr.PointsInRank[r] - 1));
                                return false;
                            }
                            amount += delta;
                        }
                        break;
                    }
                    amount += ReputationMgr.PointsInRank[r];
                }
                if (r >= (int)ReputationRank.Max)
                {
                    handler.SendSysMessage(CypherStrings.CommandFactionInvparam, rankTxt);
                    return false;
                }
            }

            FactionRecord factionEntry = CliDB.FactionStorage.LookupByKey(factionId);
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

            target.GetReputationMgr().SetOneFactionReputation(factionEntry, amount, false);
            target.GetReputationMgr().SendState(target.GetReputationMgr().GetState(factionEntry));
            handler.SendSysMessage(CypherStrings.CommandModifyRep, factionEntry.Name[handler.GetSessionDbcLocale()], factionId, handler.GetNameLink(target), target.GetReputationMgr().GetReputation(factionEntry));

            return true;
        }

        [Command("phase", RBACPermissions.CommandModifyPhase)]
        static bool HandleModifyPhaseCommand(StringArguments args, CommandHandler handler)
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

            Unit target = handler.getSelectedUnit();

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
        static bool HandleModifyPowerCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target = handler.getSelectedPlayerOrSelf();
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
        static bool HandleModifyStandStateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            uint anim_id = args.NextUInt32();
            handler.GetSession().GetPlayer().SetUInt32Value(UnitFields.NpcEmotestate, anim_id);

            return true;
        }

        [Command("gender", RBACPermissions.CommandModifyGender)]
        static bool HandleModifyGenderCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target = handler.getSelectedPlayerOrSelf();
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
            target.SetByteValue(UnitFields.Bytes0, 3, (byte)gender);
            target.SetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender, (byte)gender);

            // Change display ID
            target.InitDisplayIds();

            handler.SendSysMessage(CypherStrings.YouChangeGender, handler.GetNameLink(target), gender);

            if (handler.needReportToTarget(target))
                target.SendSysMessage(CypherStrings.YourGenderChanged, gender, handler.GetNameLink());

            return true;
        }

        [Command("currency", RBACPermissions.CommandModifyCurrency)]
        static bool HandleModifyCurrencyCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target = handler.getSelectedPlayerOrSelf();
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

        [CommandNonGroup("morph", RBACPermissions.CommandMorph)]
        static bool HandleModifyMorphCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            uint display_id = args.NextUInt32();

            Unit target = handler.getSelectedUnit();
            if (!target)
                target = handler.GetSession().GetPlayer();

            // check online security
            else if (target.IsTypeId(TypeId.Player) && handler.HasLowerSecurity(target.ToPlayer(), ObjectGuid.Empty))
                return false;

            target.SetDisplayId(display_id);

            return true;
        }

        [CommandNonGroup("demorph", RBACPermissions.CommandDemorph)]
        static bool HandleDeMorphCommand(StringArguments args, CommandHandler handler)
        {
            Unit target = handler.getSelectedUnit();
            if (!target)
                target = handler.GetSession().GetPlayer();

            // check online security
            else if (target.IsTypeId(TypeId.Player) && handler.HasLowerSecurity(target.ToPlayer(), ObjectGuid.Empty))
                return false;

            target.DeMorph();

            return true;
        }

        [Command("xp", RBACPermissions.CommandModifyXp)]
        static bool HandleModifyXPCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            int xp = args.NextInt32();

            if (xp < 1)
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            Player target = handler.getSelectedPlayerOrSelf();
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

        [CommandGroup("speed", RBACPermissions.CommandModifySpeed)]
        class ModifySpeed
        {
            [Command("", RBACPermissions.CommandModifySpeed)]
            static bool HandleModifySpeedCommand(StringArguments args, CommandHandler handler)
            {
                return HandleModifyASpeedCommand(args, handler);
            }

            [Command("all", RBACPermissions.CommandModifySpeedAll)]
            static bool HandleModifyASpeedCommand(StringArguments args, CommandHandler handler)
            {
                float allSpeed;
                Player target = handler.getSelectedPlayerOrSelf();
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
            static bool HandleModifySwimCommand(StringArguments args, CommandHandler handler)
            {
                float swimSpeed;
                Player target = handler.getSelectedPlayerOrSelf();
                if (CheckModifySpeed(args, handler, target, out swimSpeed, 0.1f, 50.0f))
                {
                    NotifyModification(handler, target, CypherStrings.YouChangeSwimSpeed, CypherStrings.YoursSwimSpeedChanged, swimSpeed);
                    target.SetSpeedRate(UnitMoveType.Swim, swimSpeed);
                    return true;
                }
                return false;
            }

            [Command("backwalk", RBACPermissions.CommandModifySpeedBackwalk)]
            static bool HandleModifyBWalkCommand(StringArguments args, CommandHandler handler)
            {
                float backSpeed;
                Player target = handler.getSelectedPlayerOrSelf();
                if (CheckModifySpeed(args, handler, target, out backSpeed, 0.1f, 50.0f))
                {
                    NotifyModification(handler, target, CypherStrings.YouChangeBackSpeed, CypherStrings.YoursBackSpeedChanged, backSpeed);
                    target.SetSpeedRate(UnitMoveType.RunBack, backSpeed);
                    return true;
                }
                return false;
            }

            [Command("fly", RBACPermissions.CommandModifySpeedFly)]
            static bool HandleModifyFlyCommand(StringArguments args, CommandHandler handler)
            {
                float flySpeed;
                Player target = handler.getSelectedPlayerOrSelf();
                if (CheckModifySpeed(args, handler, target, out flySpeed, 0.1f, 50.0f, false))
                {
                    NotifyModification(handler, target, CypherStrings.YouChangeFlySpeed, CypherStrings.YoursFlySpeedChanged, flySpeed);
                    target.SetSpeedRate(UnitMoveType.Flight, flySpeed);
                    return true;
                }
                return false;
            }

            [Command("walk", RBACPermissions.CommandModifySpeedWalk)]
            static bool HandleModifyWalkSpeedCommand(StringArguments args, CommandHandler handler)
            {
                float Speed;
                Player target = handler.getSelectedPlayerOrSelf();
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
                if (handler.needReportToTarget(player))
                    player.SendSysMessage(resourceReportMessage, new object[] { handler.GetNameLink() }.Combine(args));
            }
        }

        static bool CheckModifyResources(StringArguments args, CommandHandler handler, Player target, out int res, out int resmax, byte multiplier = 1)
        {
            res = 0;
            resmax = 0;

            if (args.Empty())
                return false;

            res = args.NextInt32() * multiplier;
            resmax = args.NextInt32() * multiplier;

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
