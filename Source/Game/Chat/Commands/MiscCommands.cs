// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Movement;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Chat
{
    class MiscCommands
    {
        // Teleport to Player
        [CommandNonGroup("appear", RBACPermissions.CommandAppear)]
        static bool HandleAppearCommand(CommandHandler handler, StringArguments args)
        {
            Player target;
            ObjectGuid targetGuid;
            string targetName;
            if (!handler.ExtractPlayerTarget(args, out target, out targetGuid, out targetName))
                return false;

            Player _player = handler.GetSession().GetPlayer();
            if (target == _player || targetGuid == _player.GetGUID())
            {
                handler.SendSysMessage(CypherStrings.CantTeleportSelf);
                return false;
            }

            if (target != null)
            {
                // check online security
                if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                    return false;

                string chrNameLink = handler.PlayerLink(targetName);

                Map map = target.GetMap();
                if (map.IsBattlegroundOrArena())
                {
                    // only allow if gm mode is on
                    if (!_player.IsGameMaster())
                    {
                        handler.SendSysMessage(CypherStrings.CannotGoToBgGm, chrNameLink);
                        return false;
                    }
                    // if both players are in different bgs
                    else if (_player.GetBattlegroundId() != 0 && _player.GetBattlegroundId() != target.GetBattlegroundId())
                        _player.LeaveBattleground(false); // Note: should be changed so _player gets no Deserter debuff

                    // all's well, set bg id
                    // when porting out from the bg, it will be reset to 0
                    _player.SetBattlegroundId(target.GetBattlegroundId(), target.GetBattlegroundTypeId());
                    // remember current position as entry point for return at bg end teleportation
                    if (!_player.GetMap().IsBattlegroundOrArena())
                        _player.SetBattlegroundEntryPoint();
                }
                else if (map.IsDungeon())
                {
                    // we have to go to instance, and can go to player only if:
                    //   1) we are in his group (either as leader or as member)
                    //   2) we are not bound to any group and have GM mode on
                    if (_player.GetGroup() != null)
                    {
                        // we are in group, we can go only if we are in the player group
                        if (_player.GetGroup() != target.GetGroup())
                        {
                            handler.SendSysMessage(CypherStrings.CannotGoToInstParty, chrNameLink);
                            return false;
                        }
                    }
                    else
                    {
                        // we are not in group, let's verify our GM mode
                        if (!_player.IsGameMaster())
                        {
                            handler.SendSysMessage(CypherStrings.CannotGoToInstGm, chrNameLink);
                            return false;
                        }
                    }

                    if (map.IsRaid())
                    {
                        _player.SetRaidDifficultyID(target.GetRaidDifficultyID());
                        _player.SetLegacyRaidDifficultyID(target.GetLegacyRaidDifficultyID());
                    }
                    else
                        _player.SetDungeonDifficultyID(target.GetDungeonDifficultyID());
                }

                handler.SendSysMessage(CypherStrings.AppearingAt, chrNameLink);

                // stop flight if need
                if (_player.IsInFlight())
                    _player.FinishTaxiFlight();
                else
                    _player.SaveRecallPosition(); // save only in non-flight case

                // to point to see at target with same orientation
                float x, y, z;
                target.GetClosePoint(out x, out y, out z, _player.GetCombatReach(), 1.0f);

                _player.TeleportTo(target.GetMapId(), x, y, z, _player.GetAbsoluteAngle(target), TeleportToOptions.GMMode, target.GetInstanceId());
                PhasingHandler.InheritPhaseShift(_player, target);
                _player.UpdateObjectVisibility();
            }
            else
            {
                // check offline security
                if (handler.HasLowerSecurity(null, targetGuid))
                    return false;

                string nameLink = handler.PlayerLink(targetName);

                handler.SendSysMessage(CypherStrings.AppearingAt, nameLink);

                // to point where player stay (if loaded)
                WorldLocation loc;
                if (!Player.LoadPositionFromDB(out loc, out _, targetGuid))
                    return false;

                // stop flight if need
                if (_player.IsInFlight())
                    _player.FinishTaxiFlight();
                else
                    _player.SaveRecallPosition(); // save only in non-flight case

                loc.SetOrientation(_player.GetOrientation());
                _player.TeleportTo(loc);
            }

            return true;
        }

        [CommandNonGroup("bank", RBACPermissions.CommandBank)]
        static bool HandleBankCommand(CommandHandler handler)
        {
            handler.GetSession().SendShowBank(handler.GetSession().GetPlayer().GetGUID(), PlayerInteractionType.Banker);
            return true;
        }

        [CommandNonGroup("bindsight", RBACPermissions.CommandBindsight)]
        static bool HandleBindSightCommand(CommandHandler handler)
        {
            Unit unit = handler.GetSelectedUnit();
            if (unit == null)
                return false;

            handler.GetSession().GetPlayer().CastSpell(unit, 6277, true);
            return true;
        }

        [CommandNonGroup("combatstop", RBACPermissions.CommandCombatstop, true)]
        static bool HandleCombatStopCommand(CommandHandler handler, StringArguments args)
        {
            Player target = null;

            if (!args.Empty())
            {
                target = Global.ObjAccessor.FindPlayerByName(args.NextString());
                if (target == null)
                {
                    handler.SendSysMessage(CypherStrings.PlayerNotFound);
                    return false;
                }
            }

            if (target == null)
            {
                if (!handler.ExtractPlayerTarget(args, out target))
                    return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            target.CombatStop();
            return true;
        }

        [CommandNonGroup("cometome", RBACPermissions.CommandCometome)]
        static bool HandleComeToMeCommand(CommandHandler handler)
        {
            Creature caster = handler.GetSelectedCreature();
            if (caster == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCreature);
                return false;
            }

            Player player = handler.GetSession().GetPlayer();
            caster.GetMotionMaster().MovePoint(0, player.GetPositionX(), player.GetPositionY(), player.GetPositionZ());

            return true;
        }

        [CommandNonGroup("commands", RBACPermissions.CommandCommands, true)]
        static bool HandleCommandsCommand(CommandHandler handler)
        {
            ChatCommandNode.SendCommandHelpFor(handler, "");
            return true;
        }

        [CommandNonGroup("damage", RBACPermissions.CommandDamage)]
        static bool HandleDamageCommand(CommandHandler handler, uint damage, OptionalArg<SpellSchools> school, OptionalArg<SpellInfo> spellInfo)
        {
            Unit target = handler.GetSelectedUnit();
            if (target == null || handler.GetSession().GetPlayer().GetTarget().IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }
            Player player = target.ToPlayer();
            if (player != null)
                if (handler.HasLowerSecurity(player, ObjectGuid.Empty, false))
                    return false;

            if (!target.IsAlive())
                return true;

            Player attacker = handler.GetSession().GetPlayer();

            // flat melee damage without resistence/etc reduction
            if (!school.HasValue)
            {
                Unit.DealDamage(attacker, target, damage, null, DamageEffectType.Direct, SpellSchoolMask.Normal, null, false);
                if (target != attacker)
                    attacker.SendAttackStateUpdate(HitInfo.AffectsVictim, target, SpellSchoolMask.Normal, damage, 0, 0, VictimState.Hit, 0, 0);
                return true;
            }

            SpellSchoolMask schoolmask = (SpellSchoolMask)(1 << (int)school.Value);

            if (Unit.IsDamageReducedByArmor(schoolmask))
                damage = Unit.CalcArmorReducedDamage(handler.GetPlayer(), target, damage, null, WeaponAttackType.BaseAttack);

            // melee damage by specific school
            if (!spellInfo.HasValue)
            {
                DamageInfo dmgInfo = new(attacker, target, damage, null, schoolmask, DamageEffectType.SpellDirect, WeaponAttackType.BaseAttack);
                Unit.CalcAbsorbResist(dmgInfo);

                if (dmgInfo.GetDamage() == 0)
                    return true;

                damage = dmgInfo.GetDamage();

                uint absorb = dmgInfo.GetAbsorb();
                uint resist = dmgInfo.GetResist();
                Unit.DealDamageMods(attacker, target, ref damage, ref absorb);
                Unit.DealDamage(attacker, target, damage, null, DamageEffectType.Direct, schoolmask, null, false);
                attacker.SendAttackStateUpdate(HitInfo.AffectsVictim, target, schoolmask, damage, absorb, resist, VictimState.Hit, 0, 0);
                return true;
            }

            // non-melee damage

            SpellNonMeleeDamage damageInfo = new(attacker, target, spellInfo, new SpellCastVisual(spellInfo.Value.GetSpellXSpellVisualId(attacker), 0), spellInfo.Value.SchoolMask);
            damageInfo.damage = damage;
            Unit.DealDamageMods(damageInfo.attacker, damageInfo.target, ref damageInfo.damage, ref damageInfo.absorb);
            target.DealSpellDamage(damageInfo, true);
            target.SendSpellNonMeleeDamageLog(damageInfo);
            return true;
        }

        [CommandNonGroup("damage go", RBACPermissions.CommandDamage)]
        static bool HandleDamageGoCommand(CommandHandler handler, VariantArg<GameobjectLinkData, ulong> spawnId, int damage)
        {
            GameObject go = handler.GetObjectFromPlayerMapByDbGuid(spawnId);
            if (go == null)
            {
                handler.SendSysMessage(CypherStrings.CommandObjnotfound, spawnId);
                return false;
            }

            if (!go.IsDestructibleBuilding())
            {
                handler.SendSysMessage(CypherStrings.InvalidGameobjectType);
                return false;
            }

            go.ModifyHealth(-damage, handler.GetSession().GetPlayer());
            handler.SendSysMessage(CypherStrings.GameobjectDamaged, go.GetName(), spawnId, -damage, go.GetGoValue().Building.Health);
            return true;
        }

        [CommandNonGroup("dev", RBACPermissions.CommandDev)]
        static bool HandleDevCommand(CommandHandler handler, OptionalArg<bool> enableArg)
        {
            Player player = handler.GetSession().GetPlayer();

            if (!enableArg.HasValue)
            {
                handler.GetSession().SendNotification(player.IsDeveloper() ? CypherStrings.DevOn : CypherStrings.DevOff);
                return true;
            }

            if (enableArg.Value)
            {
                player.SetDeveloper(true);
                handler.GetSession().SendNotification(CypherStrings.DevOn);
            }
            else
            {
                player.SetDeveloper(false);
                handler.GetSession().SendNotification(CypherStrings.DevOff);
            }

            return true;
        }

        [CommandNonGroup("die", RBACPermissions.CommandDie)]
        static bool HandleDieCommand(CommandHandler handler)
        {
            Unit target = handler.GetSelectedUnit();
            if (target == null && handler.GetPlayer().GetTarget().IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                return false;
            }

            Player player = target.ToPlayer();
            if (player != null)
                if (handler.HasLowerSecurity(player, ObjectGuid.Empty, false))
                    return false;

            if (target.IsAlive())
                Unit.Kill(handler.GetSession().GetPlayer(), target);

            return true;
        }

        [CommandNonGroup("dismount", RBACPermissions.CommandDismount)]
        static bool HandleDismountCommand(CommandHandler handler)
        {
            Player player = handler.GetSelectedPlayerOrSelf();

            // If player is not mounted, so go out :)
            if (!player.IsMounted())
            {
                handler.SendSysMessage(CypherStrings.CharNonMounted);
                return false;
            }

            if (player.IsInFlight())
            {
                handler.SendSysMessage(CypherStrings.CharInFlight);
                return false;
            }

            player.Dismount();
            player.RemoveAurasByType(AuraType.Mounted);
            return true;
        }

        [CommandNonGroup("distance", RBACPermissions.CommandDistance)]
        static bool HandleGetDistanceCommand(CommandHandler handler, StringArguments args)
        {
            WorldObject obj;

            if (!args.Empty())
            {
                HighGuid guidHigh = 0;
                ulong guidLow = handler.ExtractLowGuidFromLink(args, ref guidHigh);
                if (guidLow == 0)
                    return false;
                switch (guidHigh)
                {
                    case HighGuid.Player:
                    {
                        obj = Global.ObjAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, guidLow));
                        if (obj == null)
                        {
                            handler.SendSysMessage(CypherStrings.PlayerNotFound);
                        }
                        break;
                    }
                    case HighGuid.Creature:
                    {
                        obj = handler.GetCreatureFromPlayerMapByDbGuid(guidLow);
                        if (obj == null)
                        {
                            handler.SendSysMessage(CypherStrings.CommandNocreaturefound);
                        }
                        break;
                    }
                    case HighGuid.GameObject:
                    {
                        obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);
                        if (obj == null)
                        {
                            handler.SendSysMessage(CypherStrings.CommandNogameobjectfound);
                        }
                        break;
                    }
                    default:
                        return false;
                }
                if (obj == null)
                    return false;
            }
            else
            {
                obj = handler.GetSelectedUnit();

                if (obj == null)
                {
                    handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                    return false;
                }
            }

            handler.SendSysMessage(CypherStrings.Distance, handler.GetSession().GetPlayer().GetDistance(obj), handler.GetSession().GetPlayer().GetDistance2d(obj), handler.GetSession().GetPlayer().GetExactDist(obj), handler.GetSession().GetPlayer().GetExactDist2d(obj));
            return true;
        }

        [CommandNonGroup("freeze", RBACPermissions.CommandFreeze)]
        static bool HandleFreezeCommand(CommandHandler handler, StringArguments args)
        {
            Player player = handler.GetSelectedPlayer(); // Selected player, if any. Might be null.
            int freezeDuration = 0; // Freeze Duration (in seconds)
            bool canApplyFreeze = false; // Determines if every possible argument is set so Freeze can be applied
            bool getDurationFromConfig = false; // If there's no given duration, we'll retrieve the world cfg value later

            if (args.Empty())
            {
                // Might have a selected player. We'll check it later
                // Get the duration from world cfg
                getDurationFromConfig = true;
            }
            else
            {
                // Get the args that we might have (up to 2)
                string arg1 = args.NextString();
                string arg2 = args.NextString();

                // Analyze them to see if we got either a playerName or duration or both
                if (!arg1.IsEmpty())
                {
                    if (arg1.IsNumber())
                    {
                        // case 2: .freeze duration
                        // We have a selected player. We'll check him later
                        if (!int.TryParse(arg1, out freezeDuration))
                            return false;
                        canApplyFreeze = true;
                    }
                    else
                    {
                        // case 3 or 4: .freeze player duration | .freeze player
                        // find the player
                        string name = arg1;
                        ObjectManager.NormalizePlayerName(ref name);
                        player = Global.ObjAccessor.FindPlayerByName(name);
                        // Check if we have duration set
                        if (!arg2.IsEmpty() && arg2.IsNumber())
                        {
                            if (!int.TryParse(arg2, out freezeDuration))
                                return false;
                            canApplyFreeze = true;
                        }
                        else
                            getDurationFromConfig = true;
                    }
                }
            }

            // Check if duration needs to be retrieved from config
            if (getDurationFromConfig)
            {
                freezeDuration = WorldConfig.GetIntValue(WorldCfg.GmFreezeDuration);
                canApplyFreeze = true;
            }

            // Player and duration retrieval is over
            if (canApplyFreeze)
            {
                if (player == null) // can be null if some previous selection failed
                {
                    handler.SendSysMessage(CypherStrings.CommandFreezeWrong);
                    return true;
                }
                else if (player == handler.GetSession().GetPlayer())
                {
                    // Can't freeze himself
                    handler.SendSysMessage(CypherStrings.CommandFreezeError);
                    return true;
                }
                else // Apply the effect
                {
                    // Add the freeze aura and set the proper duration
                    // Player combat status and flags are now handled
                    // in Freeze Spell AuraScript (OnApply)
                    Aura freeze = player.AddAura(9454, player);
                    if (freeze != null)
                    {
                        if (freezeDuration != 0)
                            freeze.SetDuration(freezeDuration * Time.InMilliseconds);
                        handler.SendSysMessage(CypherStrings.CommandFreeze, player.GetName());
                        // save player
                        player.SaveToDB();
                        return true;
                    }
                }
            }
            return false;
        }

        [CommandNonGroup("gps", RBACPermissions.CommandGps)]
        static bool HandleGPSCommand(CommandHandler handler, StringArguments args)
        {
            WorldObject obj;
            if (!args.Empty())
            {
                HighGuid guidHigh = 0;
                ulong guidLow = handler.ExtractLowGuidFromLink(args, ref guidHigh);
                if (guidLow == 0)
                    return false;
                switch (guidHigh)
                {
                    case HighGuid.Player:
                    {
                        obj = Global.ObjAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, guidLow));
                        if (obj == null)
                        {
                            handler.SendSysMessage(CypherStrings.PlayerNotFound);
                        }
                        break;
                    }
                    case HighGuid.Creature:
                    {
                        obj = handler.GetCreatureFromPlayerMapByDbGuid(guidLow);
                        if (obj == null)
                        {
                            handler.SendSysMessage(CypherStrings.CommandNocreaturefound);
                        }
                        break;
                    }
                    case HighGuid.GameObject:
                    {
                        obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);
                        if (obj == null)
                        {
                            handler.SendSysMessage(CypherStrings.CommandNogameobjectfound);
                        }
                        break;
                    }
                    default:
                        return false;
                }
                if (obj == null)
                    return false;
            }
            else
            {
                obj = handler.GetSelectedUnit();

                if (obj == null)
                {
                    handler.SendSysMessage(CypherStrings.SelectCharOrCreature);
                    return false;
                }
            }

            CellCoord cellCoord = GridDefines.ComputeCellCoord(obj.GetPositionX(), obj.GetPositionY());
            Cell cell = new(cellCoord);

            uint zoneId, areaId;
            obj.GetZoneAndAreaId(out zoneId, out areaId);
            uint mapId = obj.GetMapId();

            MapRecord mapEntry = CliDB.MapStorage.LookupByKey(mapId);
            AreaTableRecord zoneEntry = CliDB.AreaTableStorage.LookupByKey(zoneId);
            AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);

            float zoneX = obj.GetPositionX();
            float zoneY = obj.GetPositionY();

            Global.DB2Mgr.Map2ZoneCoordinates((int)zoneId, ref zoneX, ref zoneY);

            Map map = obj.GetMap();
            float groundZ = obj.GetMapHeight(obj.GetPositionX(), obj.GetPositionY(), MapConst.MaxHeight);
            float floorZ = obj.GetMapHeight(obj.GetPositionX(), obj.GetPositionY(), obj.GetPositionZ());

            GridCoord gridCoord = GridDefines.ComputeGridCoord(obj.GetPositionX(), obj.GetPositionY());

            // 63? WHY?
            int gridX = (int)((MapConst.MaxGrids - 1) - gridCoord.X_coord);
            int gridY = (int)((MapConst.MaxGrids - 1) - gridCoord.Y_coord);

            bool haveMap = TerrainInfo.ExistMap(mapId, gridX, gridY);
            bool haveVMap = TerrainInfo.ExistVMap(mapId, gridX, gridY);
            bool haveMMap = (Global.DisableMgr.IsPathfindingEnabled(mapId) && Global.MMapMgr.GetNavMesh(handler.GetSession().GetPlayer().GetMapId()) != null);

            if (haveVMap)
            {
                if (obj.IsOutdoors())
                    handler.SendSysMessage(CypherStrings.GpsPositionOutdoors);
                else
                    handler.SendSysMessage(CypherStrings.GpsPositionIndoors);
            }
            else
                handler.SendSysMessage(CypherStrings.GpsNoVmap);

            string unknown = handler.GetCypherString(CypherStrings.Unknown);

            handler.SendSysMessage(CypherStrings.MapPosition,
                mapId, (mapEntry != null ? mapEntry.MapName[handler.GetSessionDbcLocale()] : unknown),
                zoneId, (zoneEntry != null ? zoneEntry.AreaName[handler.GetSessionDbcLocale()] : unknown),
                areaId, (areaEntry != null ? areaEntry.AreaName[handler.GetSessionDbcLocale()] : unknown),
                obj.GetPositionX(), obj.GetPositionY(), obj.GetPositionZ(), obj.GetOrientation());

            Transport transport = obj.GetTransport<Transport>();
            if (transport != null)
            {
                handler.SendSysMessage(CypherStrings.TransportPosition, transport.GetGoInfo().MoTransport.SpawnMap, obj.GetTransOffsetX(), obj.GetTransOffsetY(), obj.GetTransOffsetZ(), obj.GetTransOffsetO(),
                    transport.GetEntry(), transport.GetName());
            }

            handler.SendSysMessage(CypherStrings.GridPosition, cell.GetGridX(), cell.GetGridY(), cell.GetCellX(), cell.GetCellY(), obj.GetInstanceId(),
                zoneX, zoneY, groundZ, floorZ, map.GetMinHeight(obj.GetPhaseShift(), obj.GetPositionX(), obj.GetPositionY()), haveMap, haveVMap, haveMMap);

            LiquidData liquidStatus;
            ZLiquidStatus status = map.GetLiquidStatus(obj.GetPhaseShift(), obj.GetPositionX(), obj.GetPositionY(), obj.GetPositionZ(), out liquidStatus);

            if (liquidStatus != null)
                handler.SendSysMessage(CypherStrings.LiquidStatus, liquidStatus.level, liquidStatus.depth_level, liquidStatus.entry, liquidStatus.type_flags, status);

            PhasingHandler.PrintToChat(handler, obj);

            return true;
        }

        [CommandNonGroup("guid", RBACPermissions.CommandGuid)]
        static bool HandleGUIDCommand(CommandHandler handler)
        {
            ObjectGuid guid = handler.GetSession().GetPlayer().GetTarget();

            if (guid.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.NoSelection);
                return false;
            }

            handler.SendSysMessage(CypherStrings.ObjectGuid, guid.ToString(), guid.GetHigh());
            return true;
        }

        [CommandNonGroup("help", RBACPermissions.CommandHelp, true)]
        static bool HandleHelpCommand(CommandHandler handler, Tail cmd)
        {
            ChatCommandNode.SendCommandHelpFor(handler, cmd);
            if (cmd.IsEmpty())
                ChatCommandNode.SendCommandHelpFor(handler, "help");

            return true;
        }

        [CommandNonGroup("hidearea", RBACPermissions.CommandHidearea)]
        static bool HandleHideAreaCommand(CommandHandler handler, uint areaId)
        {
            Player playerTarget = handler.GetSelectedPlayer();
            if (playerTarget == null)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(areaId);
            if (area == null)
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            if (area.AreaBit < 0)
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            int offset = (area.AreaBit / PlayerConst.ExploredZonesBits);

            uint val = 1u << (area.AreaBit % PlayerConst.ExploredZonesBits);
            playerTarget.RemoveExploredZones(offset, val);

            handler.SendSysMessage(CypherStrings.UnexploreArea);
            return true;
        }

        // move item to other slot
        [CommandNonGroup("itemmove", RBACPermissions.CommandItemmove)]
        static bool HandleItemMoveCommand(CommandHandler handler, byte srcSlot, byte dstSlot)
        {
            if (srcSlot == dstSlot)
                return true;

            if (handler.GetSession().GetPlayer().IsValidPos(InventorySlots.Bag0, srcSlot, true))
                return false;

            if (handler.GetSession().GetPlayer().IsValidPos(InventorySlots.Bag0, dstSlot, false))
                return false;

            ushort src = (ushort)((InventorySlots.Bag0 << 8) | srcSlot);
            ushort dst = (ushort)((InventorySlots.Bag0 << 8) | dstSlot);

            handler.GetSession().GetPlayer().SwapItem(src, dst);

            return true;
        }

        // kick player
        [CommandNonGroup("kick", RBACPermissions.CommandKick, true)]
        static bool HandleKickPlayerCommand(CommandHandler handler, StringArguments args)
        {
            Player target;
            string playerName;
            if (!handler.ExtractPlayerTarget(args, out target, out _, out playerName))
                return false;

            if (handler.GetSession() != null && target == handler.GetSession().GetPlayer())
            {
                handler.SendSysMessage(CypherStrings.CommandKickself);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            string kickReason = args.NextString("");
            string kickReasonStr = "No reason";
            if (kickReason != null)
                kickReasonStr = kickReason;

            if (WorldConfig.GetBoolValue(WorldCfg.ShowKickInWorld))
                Global.WorldMgr.SendWorldText(CypherStrings.CommandKickmessageWorld, (handler.GetSession() != null ? handler.GetSession().GetPlayerName() : "Server"), playerName, kickReasonStr);
            else
                handler.SendSysMessage(CypherStrings.CommandKickmessage, playerName);

            target.GetSession().KickPlayer("HandleKickPlayerCommand GM Command");

            return true;
        }

        [CommandNonGroup("linkgrave", RBACPermissions.CommandLinkgrave)]
        static bool HandleLinkGraveCommand(CommandHandler handler, uint graveyardId, OptionalArg<string> teamArg)
        {
            Team team;
            if (!teamArg.HasValue)
                team = 0;
            else if (teamArg.Value.Equals("horde", StringComparison.OrdinalIgnoreCase))
                team = Team.Horde;
            else if (teamArg.Value.Equals("alliance", StringComparison.OrdinalIgnoreCase))
                team = Team.Alliance;
            else
                return false;

            WorldSafeLocsEntry graveyard = Global.ObjectMgr.GetWorldSafeLoc(graveyardId);
            if (graveyard == null)
            {
                handler.SendSysMessage(CypherStrings.CommandGraveyardnoexist, graveyardId);
                return false;
            }

            Player player = handler.GetSession().GetPlayer();

            uint zoneId = player.GetZoneId();

            AreaTableRecord areaEntry = CliDB.AreaTableStorage.LookupByKey(zoneId);
            if (areaEntry == null || areaEntry.HasFlag(AreaFlags.IsSubzone))
            {
                handler.SendSysMessage(CypherStrings.CommandGraveyardwrongzone, graveyardId, zoneId);
                return false;
            }

            if (Global.ObjectMgr.AddGraveyardLink(graveyardId, zoneId, team, true))
                handler.SendSysMessage(CypherStrings.CommandGraveyardlinked, graveyardId, zoneId);
            else
                handler.SendSysMessage(CypherStrings.CommandGraveyardalrlinked, graveyardId, zoneId);

            return true;
        }

        [CommandNonGroup("listfreeze", RBACPermissions.CommandListfreeze)]
        static bool HandleListFreezeCommand(CommandHandler handler)
        {
            // Get names from DB
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_AURA_FROZEN);
            SQLResult result = DB.Characters.Query(stmt);
            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.CommandNoFrozenPlayers);
                return true;
            }

            // Header of the names
            handler.SendSysMessage(CypherStrings.CommandListFreeze);

            // Output of the results
            do
            {
                string player = result.Read<string>(0);
                int remaintime = result.Read<int>(1);
                // Save the frozen player to update remaining time in case of future .listfreeze uses
                // before the frozen state expires
                Player frozen = Global.ObjAccessor.FindPlayerByName(player);
                if (frozen != null)
                    frozen.SaveToDB();
                // Notify the freeze duration
                if (remaintime == -1) // Permanent duration
                    handler.SendSysMessage(CypherStrings.CommandPermaFrozenPlayer, player);
                else
                    // show time left (seconds)
                    handler.SendSysMessage(CypherStrings.CommandTempFrozenPlayer, player, remaintime / Time.InMilliseconds);
            }
            while (result.NextRow());

            return true;
        }

        [CommandNonGroup("mailbox", RBACPermissions.CommandMailbox)]
        static bool HandleMailBoxCommand(CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();

            handler.GetSession().SendShowMailBox(player.GetGUID());
            return true;
        }

        [CommandNonGroup("movegens", RBACPermissions.CommandMovegens)]
        static bool HandleMovegensCommand(CommandHandler handler)
        {
            Unit unit = handler.GetSelectedUnit();
            if (unit == null)
            {
                handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

                return false;
            }

            handler.SendSysMessage(CypherStrings.MovegensList, (unit.IsTypeId(TypeId.Player) ? "Player" : "Creature"), unit.GetGUID().ToString());

            if (unit.GetMotionMaster().Empty())
            {
                handler.SendSysMessage("Empty");
                return true;
            }

            float x, y, z;
            unit.GetMotionMaster().GetDestination(out x, out y, out z);

            var list = unit.GetMotionMaster().GetMovementGeneratorsInformation();
            foreach (MovementGeneratorInformation info in list)
            {
                switch (info.Type)
                {
                    case MovementGeneratorType.Idle:
                        handler.SendSysMessage(CypherStrings.MovegensIdle);
                        break;
                    case MovementGeneratorType.Random:
                        handler.SendSysMessage(CypherStrings.MovegensRandom);
                        break;
                    case MovementGeneratorType.Waypoint:
                        handler.SendSysMessage(CypherStrings.MovegensWaypoint);
                        break;
                    case MovementGeneratorType.Confused:
                        handler.SendSysMessage(CypherStrings.MovegensConfused);
                        break;
                    case MovementGeneratorType.Chase:
                        if (info.TargetGUID.IsEmpty())
                            handler.SendSysMessage(CypherStrings.MovegensChaseNull);
                        else if (info.TargetGUID.IsPlayer())
                            handler.SendSysMessage(CypherStrings.MovegensChasePlayer, info.TargetName, info.TargetGUID.ToString());
                        else
                            handler.SendSysMessage(CypherStrings.MovegensChaseCreature, info.TargetName, info.TargetGUID.ToString());
                        break;
                    case MovementGeneratorType.Follow:
                        if (info.TargetGUID.IsEmpty())
                            handler.SendSysMessage(CypherStrings.MovegensFollowNull);
                        else if (info.TargetGUID.IsPlayer())
                            handler.SendSysMessage(CypherStrings.MovegensFollowPlayer, info.TargetName, info.TargetGUID.ToString());
                        else
                            handler.SendSysMessage(CypherStrings.MovegensFollowCreature, info.TargetName, info.TargetGUID.ToString());
                        break;
                    case MovementGeneratorType.Home:
                        if (unit.IsTypeId(TypeId.Unit))
                            handler.SendSysMessage(CypherStrings.MovegensHomeCreature, x, y, z);
                        else
                            handler.SendSysMessage(CypherStrings.MovegensHomePlayer);
                        break;
                    case MovementGeneratorType.Flight:
                        handler.SendSysMessage(CypherStrings.MovegensFlight);
                        break;
                    case MovementGeneratorType.Point:
                        handler.SendSysMessage(CypherStrings.MovegensPoint, x, y, z);
                        break;
                    case MovementGeneratorType.Fleeing:
                        handler.SendSysMessage(CypherStrings.MovegensFear);
                        break;
                    case MovementGeneratorType.Distract:
                        handler.SendSysMessage(CypherStrings.MovegensDistract);
                        break;
                    case MovementGeneratorType.Effect:
                        handler.SendSysMessage(CypherStrings.MovegensEffect);
                        break;
                    default:
                        handler.SendSysMessage(CypherStrings.MovegensUnknown, info.Type);
                        break;
                }
            }
            return true;
        }

        // mute player for the specified duration
        [CommandNonGroup("mute", RBACPermissions.CommandMute, true)]
        static bool HandleMuteCommand(CommandHandler handler, OptionalArg<PlayerIdentifier> player, uint muteTime, Tail muteReason)
        {
            string muteReasonStr = muteReason;
            if (muteReason.IsEmpty())
                muteReasonStr = handler.GetCypherString(CypherStrings.NoReason);

            if (!player.HasValue)
                player = PlayerIdentifier.FromTarget(handler);
            if (player.Value == null)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            Player target = player.Value.GetConnectedPlayer();
            uint accountId = target != null ? target.GetSession().GetAccountId() : Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(player.Value.GetGUID());

            // find only player from same account if any
            if (target == null)
            {
                WorldSession session = Global.WorldMgr.FindSession(accountId);
                if (session != null)
                    target = session.GetPlayer();
            }

            // must have strong lesser security level
            if (handler.HasLowerSecurity(target, player.Value.GetGUID(), true))
                return false;

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_MUTE_TIME);
            string muteBy;
            Player gmPlayer = handler.GetPlayer();
            if (gmPlayer != null)
                muteBy = gmPlayer.GetName();
            else
                muteBy = handler.GetCypherString(CypherStrings.Console);

            if (target != null)
            {
                // Target is online, mute will be in effect right away.
                long mutedUntil = GameTime.GetGameTime() + muteTime * Time.Minute;
                target.GetSession().m_muteTime = mutedUntil;
                stmt.AddValue(0, mutedUntil);
            }
            else
            {
                stmt.AddValue(0, -(muteTime * Time.Minute));
            }

            stmt.AddValue(1, muteReasonStr);
            stmt.AddValue(2, muteBy);
            stmt.AddValue(3, accountId);
            DB.Login.Execute(stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_ACCOUNT_MUTE);
            stmt.AddValue(0, accountId);
            stmt.AddValue(1, muteTime);
            stmt.AddValue(2, muteBy);
            stmt.AddValue(3, muteReasonStr);
            DB.Login.Execute(stmt);

            string nameLink = handler.PlayerLink(player.Value.GetName());

            if (WorldConfig.GetBoolValue(WorldCfg.ShowMuteInWorld))
                Global.WorldMgr.SendWorldText(CypherStrings.CommandMutemessageWorld, muteBy, nameLink, muteTime, muteReasonStr);
            if (target != null)
            {
                target.SendSysMessage(CypherStrings.YourChatDisabled, muteTime, muteBy, muteReasonStr);
                handler.SendSysMessage(CypherStrings.YouDisableChat, nameLink, muteTime, muteReasonStr);
            }
            else
            {
                handler.SendSysMessage(CypherStrings.CommandDisableChatDelayed, nameLink, muteTime, muteReasonStr);
            }

            return true;
        }

        // mutehistory command
        [CommandNonGroup("mutehistory", RBACPermissions.CommandMutehistory, true)]
        static bool HandleMuteHistoryCommand(CommandHandler handler, string accountName)
        {
            uint accountId = Global.AccountMgr.GetId(accountName);
            if (accountId == 0)
            {
                handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                return false;
            }

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_MUTE_INFO);
            stmt.AddValue(0, accountId);

            SQLResult result = DB.Login.Query(stmt);
            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.CommandMutehistoryEmpty, accountName);
                return true;
            }

            handler.SendSysMessage(CypherStrings.CommandMutehistory, accountName);
            do
            {
                // we have to manually set the string for mutedate
                long sqlTime = result.Read<uint>(0);

                // set it to string
                string buffer = Time.UnixTimeToDateTime(sqlTime).ToShortTimeString();

                handler.SendSysMessage(CypherStrings.CommandMutehistoryOutput, buffer, result.Read<uint>(1), result.Read<string>(2), result.Read<string>(3));
            } while (result.NextRow());

            return true;
        }

        [CommandNonGroup("neargrave", RBACPermissions.CommandNeargrave)]
        static bool HandleNearGraveCommand(CommandHandler handler, OptionalArg<string> teamArg)
        {
            Team team;
            if (!teamArg.HasValue)
                team = 0;
            else if (teamArg.Value.Equals("horde", StringComparison.OrdinalIgnoreCase))
                team = Team.Horde;
            else if (teamArg.Value.Equals("alliance", StringComparison.OrdinalIgnoreCase))
                team = Team.Alliance;
            else
                return false;

            Player player = handler.GetSession().GetPlayer();
            uint zoneId = player.GetZoneId();

            WorldSafeLocsEntry graveyard = Global.ObjectMgr.GetClosestGraveyard(player, team, null);
            if (graveyard != null)
            {
                uint graveyardId = graveyard.Id;

                GraveyardData data = Global.ObjectMgr.FindGraveyardData(graveyardId, zoneId);
                if (data == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandGraveyarderror, graveyardId);
                    return false;
                }

                string team_name = handler.GetCypherString(CypherStrings.CommandGraveyardNoteam);

                if (team == 0)
                    team_name = handler.GetCypherString(CypherStrings.CommandGraveyardAny);
                else if (team == Team.Horde)
                    team_name = handler.GetCypherString(CypherStrings.CommandGraveyardHorde);
                else if (team == Team.Alliance)
                    team_name = handler.GetCypherString(CypherStrings.CommandGraveyardAlliance);

                handler.SendSysMessage(CypherStrings.CommandGraveyardnearest, graveyardId, team_name, zoneId);
            }
            else
            {
                string team_name = "";

                if (team == Team.Horde)
                    team_name = handler.GetCypherString(CypherStrings.CommandGraveyardHorde);
                else if (team == Team.Alliance)
                    team_name = handler.GetCypherString(CypherStrings.CommandGraveyardAlliance);

                if (team == 0)
                    handler.SendSysMessage(CypherStrings.CommandZonenograveyards, zoneId);
                else
                    handler.SendSysMessage(CypherStrings.CommandZonenografaction, zoneId, team_name);
            }

            return true;
        }

        [CommandNonGroup("pinfo", RBACPermissions.CommandPinfo, true)]
        static bool HandlePInfoCommand(CommandHandler handler, OptionalArg<PlayerIdentifier> arg)
        {
            if (!arg.HasValue)
                arg = PlayerIdentifier.FromTargetOrSelf(handler);
            if (arg.Value == null)
                return false;

            // Define ALL the player variables!
            Player target = arg.Value.GetConnectedPlayer();
            ObjectGuid targetGuid = arg.Value.GetGUID();
            string targetName = arg.Value.GetName();
            PreparedStatement stmt;

            /* The variables we extract for the command. They are
             * default as "does not exist" to prevent problems
             * The output is printed in the follow manner:
             *
             * Player %s %s (guid: %u)                   - I.    LANG_PINFO_PLAYER
             * ** GM Mode active, Phase: -1              - II.   LANG_PINFO_GM_ACTIVE (if GM)
             * ** Banned: (Type, Reason, Time, By)       - III.  LANG_PINFO_BANNED (if banned)
             * ** Muted: (Reason, Time, By)              - IV.   LANG_PINFO_MUTED (if muted)
             * * Account: %s (id: %u), GM Level: %u      - V.    LANG_PINFO_ACC_ACCOUNT
             * * Last Login: %u (Failed Logins: %u)      - VI.   LANG_PINFO_ACC_LASTLOGIN
             * * Uses OS: %s - Latency: %u ms            - VII.  LANG_PINFO_ACC_OS
             * * Registration Email: %s - Email: %s      - VIII. LANG_PINFO_ACC_REGMAILS
             * * Last IP: %u (Locked: %s)                - IX.   LANG_PINFO_ACC_IP
             * * Level: %u (%u/%u XP (%u XP left)        - X.    LANG_PINFO_CHR_LEVEL
             * * Race: %s %s, Class %s                   - XI.   LANG_PINFO_CHR_RACE
             * * Alive ?: %s                             - XII.  LANG_PINFO_CHR_ALIVE
             * * Phase: %s                               - XIII. LANG_PINFO_CHR_PHASE (if not GM)
             * * Money: %ug%us%uc                        - XIV.  LANG_PINFO_CHR_MONEY
             * * Map: %s, Area: %s                       - XV.   LANG_PINFO_CHR_MAP
             * * Guild: %s (Id: %u)                      - XVI.  LANG_PINFO_CHR_GUILD (if in guild)
             * ** Rank: %s                               - XVII. LANG_PINFO_CHR_GUILD_RANK (if in guild)
             * ** Note: %s                               - XVIII.LANG_PINFO_CHR_GUILD_NOTE (if in guild and has note)
             * ** O. Note: %s                            - XVIX. LANG_PINFO_CHR_GUILD_ONOTE (if in guild and has officer note)
             * * Played time: %s                         - XX.   LANG_PINFO_CHR_PLAYEDTIME
             * * Mails: %u Read/%u Total                 - XXI.  LANG_PINFO_CHR_MAILS (if has mails)
             *
             * Not all of them can be moved to the top. These should
             * place the most important ones to the head, though.
             *
             * For a cleaner overview, I segment each output in Roman numerals
             */

            // Account data print variables
            string userName = handler.GetCypherString(CypherStrings.Error);
            uint accId;
            ulong lowguid = targetGuid.GetCounter();
            string eMail = handler.GetCypherString(CypherStrings.Error);
            string regMail = handler.GetCypherString(CypherStrings.Error);
            uint security = 0;
            string lastIp = handler.GetCypherString(CypherStrings.Error);
            byte locked = 0;
            string lastLogin = handler.GetCypherString(CypherStrings.Error);
            uint failedLogins = 0;
            uint latency = 0;
            string OS = handler.GetCypherString(CypherStrings.Unknown);

            // Mute data print variables
            long muteTime = -1;
            string muteReason = handler.GetCypherString(CypherStrings.NoReason);
            string muteBy = handler.GetCypherString(CypherStrings.Unknown);

            // Ban data print variables
            long banTime = -1;
            string banType = handler.GetCypherString(CypherStrings.Unknown);
            string banReason = handler.GetCypherString(CypherStrings.NoReason);
            string bannedBy = handler.GetCypherString(CypherStrings.Unknown);

            // Character data print variables
            Race raceid;
            Class classid;
            Gender gender;
            Locale locale = handler.GetSessionDbcLocale();
            uint totalPlayerTime;
            uint level;
            string alive;
            ulong money;
            uint xp = 0;
            uint xptotal = 0;

            // Position data print
            uint mapId;
            uint areaId;
            string areaName = null;
            string zoneName = null;

            // Guild data print variables defined so that they exist, but are not necessarily used
            ulong guildId = 0;
            byte guildRankId = 0;
            string guildName = "";
            string guildRank = "";
            string note = "";
            string officeNote = "";

            // Mail data print is only defined if you have a mail

            if (target != null)
            {
                // check online security
                if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                    return false;

                accId = target.GetSession().GetAccountId();
                money = target.GetMoney();
                totalPlayerTime = target.GetTotalPlayedTime();
                level = target.GetLevel();
                latency = target.GetSession().GetLatency();
                raceid = target.GetRace();
                classid = target.GetClass();
                muteTime = target.GetSession().m_muteTime;
                mapId = target.GetMapId();
                areaId = target.GetAreaId();
                alive = target.IsAlive() ? handler.GetCypherString(CypherStrings.Yes) : handler.GetCypherString(CypherStrings.No);
                gender = target.GetNativeGender();
            }
            // get additional information from DB
            else
            {
                // check offline security
                if (handler.HasLowerSecurity(null, targetGuid))
                    return false;

                // Query informations from the DB
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_PINFO);
                stmt.AddValue(0, lowguid);
                SQLResult result = DB.Characters.Query(stmt);

                if (result.IsEmpty())
                    return false;

                totalPlayerTime = result.Read<uint>(0);
                level = result.Read<byte>(1);
                money = result.Read<ulong>(2);
                accId = result.Read<uint>(3);
                raceid = (Race)result.Read<byte>(4);
                classid = (Class)result.Read<byte>(5);
                mapId = result.Read<ushort>(6);
                areaId = result.Read<ushort>(7);
                gender = (Gender)result.Read<byte>(8);
                uint health = result.Read<uint>(9);
                PlayerFlags playerFlags = (PlayerFlags)result.Read<uint>(10);

                if (health == 0 || playerFlags.HasAnyFlag(PlayerFlags.Ghost))
                    alive = handler.GetCypherString(CypherStrings.No);
                else
                    alive = handler.GetCypherString(CypherStrings.Yes);
            }

            // Query the prepared statement for login data
            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_PINFO);
            stmt.AddValue(0, Global.RealmMgr.GetCurrentRealmId().Index);
            stmt.AddValue(1, accId);
            SQLResult result0 = DB.Login.Query(stmt);

            if (!result0.IsEmpty())
            {
                userName = result0.Read<string>(0);
                security = result0.Read<byte>(1);

                // Only fetch these fields if commander has sufficient rights)
                if (handler.HasPermission(RBACPermissions.CommandsPinfoCheckPersonalData) && // RBAC Perm. 48, Role 39
                    (handler.GetSession() == null || handler.GetSession().GetSecurity() >= (AccountTypes)security))
                {
                    eMail = result0.Read<string>(2);
                    regMail = result0.Read<string>(3);
                    lastIp = result0.Read<string>(4);
                    lastLogin = result0.Read<string>(5);
                }
                else
                {
                    eMail = handler.GetCypherString(CypherStrings.Unauthorized);
                    regMail = handler.GetCypherString(CypherStrings.Unauthorized);
                    lastIp = handler.GetCypherString(CypherStrings.Unauthorized);
                    lastLogin = handler.GetCypherString(CypherStrings.Unauthorized);
                }
                muteTime = (long)result0.Read<ulong>(6);
                muteReason = result0.Read<string>(7);
                muteBy = result0.Read<string>(8);
                failedLogins = result0.Read<uint>(9);
                locked = result0.Read<byte>(10);
                OS = result0.Read<string>(11);
            }

            // Creates a chat link to the character. Returns nameLink
            string nameLink = handler.PlayerLink(targetName);

            // Returns banType, banTime, bannedBy, banreason
            PreparedStatement stmt2 = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_PINFO_BANS);
            stmt2.AddValue(0, accId);
            SQLResult result2 = DB.Login.Query(stmt2);
            if (result2.IsEmpty())
            {
                banType = handler.GetCypherString(CypherStrings.Character);
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PINFO_BANS);
                stmt.AddValue(0, lowguid);
                result2 = DB.Characters.Query(stmt);
            }
            else
                banType = handler.GetCypherString(CypherStrings.Account);

            if (!result2.IsEmpty())
            {
                bool permanent = result2.Read<ulong>(1) != 0;
                banTime = !permanent ? result2.Read<uint>(0) : 0;
                bannedBy = result2.Read<string>(2);
                banReason = result2.Read<string>(3);
            }

            // Can be used to query data from Characters database
            stmt2 = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PINFO_XP);
            stmt2.AddValue(0, lowguid);
            SQLResult result4 = DB.Characters.Query(stmt2);

            if (!result4.IsEmpty())
            {
                xp = result4.Read<uint>(0); // Used for "current xp" output and "%u XP Left" calculation
                ulong gguid = result4.Read<ulong>(1); // We check if have a guild for the person, so we might not require to query it at all
                xptotal = Global.ObjectMgr.GetXPForLevel(level);

                if (gguid != 0)
                {
                    // Guild Data - an own query, because it may not happen.
                    PreparedStatement stmt3 = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_GUILD_MEMBER_EXTENDED);
                    stmt3.AddValue(0, lowguid);
                    SQLResult result5 = DB.Characters.Query(stmt3);
                    if (!result5.IsEmpty())
                    {
                        guildId = result5.Read<ulong>(0);
                        guildName = result5.Read<string>(1);
                        guildRank = result5.Read<string>(2);
                        guildRankId = result5.Read<byte>(3);
                        note = result5.Read<string>(4);
                        officeNote = result5.Read<string>(5);
                    }
                }
            }

            // Initiate output
            // Output I. LANG_PINFO_PLAYER
            handler.SendSysMessage(CypherStrings.PinfoPlayer, target != null ? "" : handler.GetCypherString(CypherStrings.Offline), nameLink, targetGuid.ToString());

            // Output II. LANG_PINFO_GM_ACTIVE if character is gamemaster
            if (target != null && target.IsGameMaster())
                handler.SendSysMessage(CypherStrings.PinfoGmActive);

            // Output III. LANG_PINFO_BANNED if ban exists and is applied
            if (banTime >= 0)
                handler.SendSysMessage(CypherStrings.PinfoBanned, banType, banReason, banTime > 0 ? Time.secsToTimeString((ulong)(banTime - GameTime.GetGameTime()), TimeFormat.ShortText) : handler.GetCypherString(CypherStrings.Permanently), bannedBy);

            // Output IV. LANG_PINFO_MUTED if mute is applied
            if (muteTime > 0)
                handler.SendSysMessage(CypherStrings.PinfoMuted, muteReason, Time.secsToTimeString((ulong)(muteTime - GameTime.GetGameTime()), TimeFormat.ShortText), muteBy);

            // Output V. LANG_PINFO_ACC_ACCOUNT
            handler.SendSysMessage(CypherStrings.PinfoAccAccount, userName, accId, security);

            // Output VI. LANG_PINFO_ACC_LASTLOGIN
            handler.SendSysMessage(CypherStrings.PinfoAccLastlogin, lastLogin, failedLogins);

            // Output VII. LANG_PINFO_ACC_OS
            handler.SendSysMessage(CypherStrings.PinfoAccOs, OS, latency);

            // Output VIII. LANG_PINFO_ACC_REGMAILS
            handler.SendSysMessage(CypherStrings.PinfoAccRegmails, regMail, eMail);

            // Output IX. LANG_PINFO_ACC_IP
            handler.SendSysMessage(CypherStrings.PinfoAccIp, lastIp, locked != 0 ? handler.GetCypherString(CypherStrings.Yes) : handler.GetCypherString(CypherStrings.No));

            // Output X. LANG_PINFO_CHR_LEVEL
            if (level != WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
                handler.SendSysMessage(CypherStrings.PinfoChrLevelLow, level, xp, xptotal, (xptotal - xp));
            else
                handler.SendSysMessage(CypherStrings.PinfoChrLevelHigh, level);

            // Output XI. LANG_PINFO_CHR_RACE
            handler.SendSysMessage(CypherStrings.PinfoChrRace, (gender == 0 ? handler.GetCypherString(CypherStrings.CharacterGenderMale) : handler.GetCypherString(CypherStrings.CharacterGenderFemale)),
                Global.DB2Mgr.GetChrRaceName(raceid, locale), Global.DB2Mgr.GetClassName(classid, locale));

            // Output XII. LANG_PINFO_CHR_ALIVE
            handler.SendSysMessage(CypherStrings.PinfoChrAlive, alive);

            // Output XIII. phases
            if (target != null)
                PhasingHandler.PrintToChat(handler, target);

            // Output XIV. LANG_PINFO_CHR_MONEY
            ulong gold = money / MoneyConstants.Gold;
            ulong silv = (money % MoneyConstants.Gold) / MoneyConstants.Silver;
            ulong copp = (money % MoneyConstants.Gold) % MoneyConstants.Silver;
            handler.SendSysMessage(CypherStrings.PinfoChrMoney, gold, silv, copp);

            // Position data
            MapRecord map = CliDB.MapStorage.LookupByKey(mapId);
            AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(areaId);
            if (area != null)
            {
                zoneName = area.AreaName[locale];

                if (area.HasFlag(AreaFlags.IsSubzone))
                {
                    AreaTableRecord zone = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);
                    if (zone != null)
                    {
                        areaName = zoneName;
                        zoneName = zone.AreaName[locale];
                    }
                }
            }

            if (zoneName == null)
                zoneName = handler.GetCypherString(CypherStrings.Unknown);

            if (areaName != null)
                handler.SendSysMessage(CypherStrings.PinfoChrMapWithArea, map.MapName[locale], zoneName, areaName);
            else
                handler.SendSysMessage(CypherStrings.PinfoChrMap, map.MapName[locale], zoneName);

            // Output XVII. - XVIX. if they are not empty
            if (!guildName.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.PinfoChrGuild, guildName, guildId);
                handler.SendSysMessage(CypherStrings.PinfoChrGuildRank, guildRank, guildRankId);
                if (!note.IsEmpty())
                    handler.SendSysMessage(CypherStrings.PinfoChrGuildNote, note);
                if (!officeNote.IsEmpty())
                    handler.SendSysMessage(CypherStrings.PinfoChrGuildOnote, officeNote);
            }

            // Output XX. LANG_PINFO_CHR_PLAYEDTIME
            handler.SendSysMessage(CypherStrings.PinfoChrPlayedtime, (Time.secsToTimeString(totalPlayerTime, TimeFormat.ShortText, true)));

            // Mail Data - an own query, because it may or may not be useful.
            // SQL: "SELECT SUM(CASE WHEN (checked & 1) THEN 1 ELSE 0 END) AS 'readmail', COUNT(*) AS 'totalmail' FROM mail WHERE `receiver` = ?"
            PreparedStatement stmt4 = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PINFO_MAILS);
            stmt4.AddValue(0, lowguid);
            SQLResult result6 = DB.Characters.Query(stmt4);
            if (!result6.IsEmpty())
            {
                uint readmail = (uint)result6.Read<double>(0);
                uint totalmail = (uint)result6.Read<ulong>(1);

                // Output XXI. LANG_INFO_CHR_MAILS if at least one mail is given
                if (totalmail >= 1)
                    handler.SendSysMessage(CypherStrings.PinfoChrMails, readmail, totalmail);
            }

            return true;
        }

        [CommandNonGroup("playall", RBACPermissions.CommandPlayall)]
        static bool HandlePlayAllCommand(CommandHandler handler, uint soundId, OptionalArg<uint> broadcastTextId)
        {
            if (!CliDB.SoundKitStorage.ContainsKey(soundId))
            {
                handler.SendSysMessage(CypherStrings.SoundNotExist, soundId);
                return false;
            }

            Global.WorldMgr.SendGlobalMessage(new PlaySound(handler.GetSession().GetPlayer().GetGUID(), soundId, broadcastTextId.GetValueOrDefault(0)));

            handler.SendSysMessage(CypherStrings.CommandPlayedToAll, soundId);
            return true;
        }

        [CommandNonGroup("possess", RBACPermissions.CommandPossess)]
        static bool HandlePossessCommand(CommandHandler handler)
        {
            Unit unit = handler.GetSelectedUnit();
            if (unit == null)
                return false;

            handler.GetSession().GetPlayer().CastSpell(unit, 530, true);
            return true;
        }

        [CommandNonGroup("pvpstats", RBACPermissions.CommandPvpstats, true)]
        static bool HandlePvPstatsCommand(CommandHandler handler)
        {
            if (WorldConfig.GetBoolValue(WorldCfg.BattlegroundStoreStatisticsEnable))
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PVPSTATS_FACTIONS_OVERALL);
                SQLResult result = DB.Characters.Query(stmt);

                if (!result.IsEmpty())
                {
                    uint horde_victories = result.Read<uint>(1);

                    if (!(result.NextRow()))
                        return false;

                    uint alliance_victories = result.Read<uint>(1);

                    handler.SendSysMessage(CypherStrings.Pvpstats, alliance_victories, horde_victories);
                }
                else
                    return false;
            }
            else
                handler.SendSysMessage(CypherStrings.PvpstatsDisabled);

            return true;
        }

        // Teleport player to last position
        [CommandNonGroup("recall", RBACPermissions.CommandRecall)]
        static bool HandleRecallCommand(CommandHandler handler, StringArguments args)
        {
            Player target;
            if (!handler.ExtractPlayerTarget(args, out target))
                return false;

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            if (target.IsBeingTeleported())
            {
                handler.SendSysMessage(CypherStrings.IsTeleported, handler.GetNameLink(target));

                return false;
            }

            // stop flight if need
            target.FinishTaxiFlight();

            target.Recall();
            return true;
        }

        [CommandNonGroup("repairitems", RBACPermissions.CommandRepairitems, true)]
        static bool HandleRepairitemsCommand(CommandHandler handler, StringArguments args)
        {
            Player target;
            if (!handler.ExtractPlayerTarget(args, out target))
                return false;

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            // Repair items
            target.DurabilityRepairAll(false, 0, false);

            handler.SendSysMessage(CypherStrings.YouRepairItems, handler.GetNameLink(target));
            if (handler.NeedReportToTarget(target))
                target.SendSysMessage(CypherStrings.YourItemsRepaired, handler.GetNameLink());

            return true;
        }

        [CommandNonGroup("respawn", RBACPermissions.CommandRespawn)]
        static bool HandleRespawnCommand(CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();

            // accept only explicitly selected target (not implicitly self targeting case)
            Creature target = !player.GetTarget().IsEmpty() ? handler.GetSelectedCreature() : null;
            if (target != null)
            {
                if (target.IsPet())
                {
                    handler.SendSysMessage(CypherStrings.SelectCreature);
                    return false;
                }

                if (target.IsDead())
                    target.Respawn();
                return true;
            }

            // First handle any creatures that still have a corpse around
            RespawnDo u_do = new();
            WorldObjectWorker<Creature> worker = new(player, u_do);
            Cell.VisitGridObjects(player, worker, player.GetGridActivationRange());

            // Now handle any that had despawned, but had respawn time logged.
            List<RespawnInfo> data = new();
            player.GetMap().GetRespawnInfo(data, SpawnObjectTypeMask.All);
            if (!data.Empty())
            {
                uint gridId = GridDefines.ComputeGridCoord(player.GetPositionX(), player.GetPositionY()).GetId();
                foreach (RespawnInfo info in data)
                    if (info.gridId == gridId)
                        player.GetMap().RemoveRespawnTime(info.type, info.spawnId);
            }

            return true;
        }

        [CommandNonGroup("revive", RBACPermissions.CommandRevive, true)]
        static bool HandleReviveCommand(CommandHandler handler, StringArguments args)
        {
            Player target;
            ObjectGuid targetGuid;
            if (!handler.ExtractPlayerTarget(args, out target, out targetGuid))
                return false;

            if (target != null)
            {
                target.ResurrectPlayer(0.5f);
                target.SpawnCorpseBones();
                target.SaveToDB();
            }
            else
                Player.OfflineResurrect(targetGuid, null);

            return true;
        }

        // Save all players in the world
        [CommandNonGroup("saveall", RBACPermissions.CommandSaveall, true)]
        static bool HandleSaveAllCommand(CommandHandler handler)
        {
            Global.ObjAccessor.SaveAllPlayers();
            handler.SendSysMessage(CypherStrings.PlayersSaved);
            return true;
        }

        [CommandNonGroup("save", RBACPermissions.CommandSave)]
        static bool HandleSaveCommand(CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();

            // save GM account without delay and output message
            if (handler.GetSession().HasPermission(RBACPermissions.CommandsSaveWithoutDelay))
            {
                Player target = handler.GetSelectedPlayer();
                if (target != null)
                    target.SaveToDB();
                else
                    player.SaveToDB();
                handler.SendSysMessage(CypherStrings.PlayerSaved);
                return true;
            }

            // save if the player has last been saved over 20 seconds ago
            uint saveInterval = WorldConfig.GetUIntValue(WorldCfg.IntervalSave);
            if (saveInterval == 0 || (saveInterval > 20 * Time.InMilliseconds && player.GetSaveTimer() <= saveInterval - 20 * Time.InMilliseconds))
                player.SaveToDB();

            return true;
        }

        [CommandNonGroup("showarea", RBACPermissions.CommandShowarea)]
        static bool HandleShowAreaCommand(CommandHandler handler, uint areaId)
        {
            Player playerTarget = handler.GetSelectedPlayer();
            if (playerTarget == null)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(areaId);
            if (area == null)
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            if (area.AreaBit < 0)
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            int offset = (area.AreaBit / PlayerConst.ExploredZonesBits);

            ulong val = 1ul << (area.AreaBit % PlayerConst.ExploredZonesBits);
            playerTarget.AddExploredZones(offset, val);

            handler.SendSysMessage(CypherStrings.ExploreArea);
            return true;
        }

        // Summon Player
        [CommandNonGroup("summon", RBACPermissions.CommandSummon)]
        static bool HandleSummonCommand(CommandHandler handler, StringArguments args)
        {
            Player target;
            ObjectGuid targetGuid;
            string targetName;
            if (!handler.ExtractPlayerTarget(args, out target, out targetGuid, out targetName))
                return false;

            Player _player = handler.GetSession().GetPlayer();
            if (target == _player || targetGuid == _player.GetGUID())
            {
                handler.SendSysMessage(CypherStrings.CantTeleportSelf);

                return false;
            }

            if (target != null)
            {
                string nameLink = handler.PlayerLink(targetName);
                // check online security
                if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                    return false;

                if (target.IsBeingTeleported())
                {
                    handler.SendSysMessage(CypherStrings.IsTeleported, nameLink);

                    return false;
                }

                Map map = _player.GetMap();
                if (map.IsBattlegroundOrArena())
                {
                    // only allow if gm mode is on
                    if (!_player.IsGameMaster())
                    {
                        handler.SendSysMessage(CypherStrings.CannotGoToBgGm, nameLink);
                        return false;
                    }
                    // if both players are in different bgs
                    else if (target.GetBattlegroundId() != 0 && _player.GetBattlegroundId() != target.GetBattlegroundId())
                        target.LeaveBattleground(false); // Note: should be changed so target gets no Deserter debuff

                    // all's well, set bg id
                    // when porting out from the bg, it will be reset to 0
                    target.SetBattlegroundId(_player.GetBattlegroundId(), _player.GetBattlegroundTypeId());
                    // remember current position as entry point for return at bg end teleportation
                    if (!target.GetMap().IsBattlegroundOrArena())
                        target.SetBattlegroundEntryPoint();
                }
                else if (map.IsDungeon())
                {
                    Map targetMap = target.GetMap();

                    Player targetGroupLeader = null;
                    Group targetGroup = target.GetGroup();
                    if (targetGroup != null)
                        targetGroupLeader = Global.ObjAccessor.GetPlayer(map, targetGroup.GetLeaderGUID());

                    // check if far teleport is allowed
                    if (targetGroupLeader == null || (targetGroupLeader.GetMapId() != map.GetId()) || (targetGroupLeader.GetInstanceId() != map.GetInstanceId()))
                    {
                        if ((targetMap.GetId() != map.GetId()) || (targetMap.GetInstanceId() != map.GetInstanceId()))
                        {
                            handler.SendSysMessage(CypherStrings.CannotSummonToInst);
                            return false;
                        }
                    }

                    // check if we're already in a different instance of the same map
                    if ((targetMap.GetId() == map.GetId()) && (targetMap.GetInstanceId() != map.GetInstanceId()))
                    {
                        handler.SendSysMessage(CypherStrings.CannotSummonInstInst, nameLink);
                        return false;
                    }
                }

                handler.SendSysMessage(CypherStrings.Summoning, nameLink, "");
                if (handler.NeedReportToTarget(target))
                    target.SendSysMessage(CypherStrings.SummonedBy, handler.PlayerLink(_player.GetName()));

                // stop flight if need
                if (_player.IsInFlight())
                    _player.FinishTaxiFlight();
                else
                    _player.SaveRecallPosition(); // save only in non-flight case

                // before GM
                float x, y, z;
                _player.GetClosePoint(out x, out y, out z, target.GetCombatReach());
                target.TeleportTo(_player.GetMapId(), x, y, z, target.GetOrientation(), 0, map.GetInstanceId());
                PhasingHandler.InheritPhaseShift(target, _player);
                target.UpdateObjectVisibility();
            }
            else
            {
                // check offline security
                if (handler.HasLowerSecurity(null, targetGuid))
                    return false;

                string nameLink = handler.PlayerLink(targetName);

                handler.SendSysMessage(CypherStrings.Summoning, nameLink, handler.GetCypherString(CypherStrings.Offline));

                // in point where GM stay
                Player.SavePositionInDB(new WorldLocation(_player.GetMapId(), _player.GetPositionX(), _player.GetPositionY(), _player.GetPositionZ(), _player.GetOrientation()), _player.GetZoneId(), targetGuid);
            }

            return true;
        }

        [CommandNonGroup("unbindsight", RBACPermissions.CommandUnbindsight)]
        static bool HandleUnbindSightCommand(CommandHandler handler)
        {
            Player player = handler.GetSession().GetPlayer();

            if (player.IsPossessing())
                return false;

            player.StopCastingBindSight();
            return true;
        }

        [CommandNonGroup("unfreeze", RBACPermissions.CommandUnfreeze)]
        static bool HandleUnFreezeCommand(CommandHandler handler, OptionalArg<string> targetNameArg)
        {
            string name = "";
            Player player;

            if (targetNameArg.HasValue)
            {
                name = targetNameArg;
                ObjectManager.NormalizePlayerName(ref name);
                player = Global.ObjAccessor.FindPlayerByName(name);
            }
            else // If no name was entered - use target
            {
                player = handler.GetSelectedPlayer();
                if (player != null)
                    name = player.GetName();
            }

            if (player != null)
            {
                handler.SendSysMessage(CypherStrings.CommandUnfreeze, name);

                // Remove Freeze spell (allowing movement and spells)
                // Player Flags + Neutral faction removal is now
                // handled on the Freeze Spell AuraScript (OnRemove)
                player.RemoveAurasDueToSpell(9454);
            }
            else
            {
                if (!targetNameArg.HasValue)
                {
                    // Check for offline players
                    ObjectGuid guid = Global.CharacterCacheStorage.GetCharacterGuidByName(name);
                    if (guid.IsEmpty())
                    {
                        handler.SendSysMessage(CypherStrings.CommandFreezeWrong);
                        return true;
                    }

                    // If player found: delete his freeze aura    
                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_AURA_FROZEN);
                    stmt.AddValue(0, guid.GetCounter());
                    DB.Characters.Execute(stmt);

                    handler.SendSysMessage(CypherStrings.CommandUnfreeze, name);
                    return true;
                }
                else
                {
                    handler.SendSysMessage(CypherStrings.CommandFreezeWrong);
                    return true;
                }
            }

            return true;
        }

        // unmute player
        [CommandNonGroup("unmute", RBACPermissions.CommandUnmute, true)]
        static bool HandleUnmuteCommand(CommandHandler handler, StringArguments args)
        {
            Player target;
            ObjectGuid targetGuid;
            string targetName;
            if (!handler.ExtractPlayerTarget(args, out target, out targetGuid, out targetName))
                return false;

            uint accountId = target != null ? target.GetSession().GetAccountId() : Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(targetGuid);

            // find only player from same account if any
            if (target == null)
            {
                WorldSession session = Global.WorldMgr.FindSession(accountId);
                if (session != null)
                    target = session.GetPlayer();
            }

            // must have strong lesser security level
            if (handler.HasLowerSecurity(target, targetGuid, true))
                return false;

            if (target != null)
            {
                if (target.GetSession().CanSpeak())
                {
                    handler.SendSysMessage(CypherStrings.ChatAlreadyEnabled);
                    return false;
                }

                target.GetSession().m_muteTime = 0;
            }

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_MUTE_TIME);
            stmt.AddValue(0, 0);
            stmt.AddValue(1, "");
            stmt.AddValue(2, "");
            stmt.AddValue(3, accountId);
            DB.Login.Execute(stmt);

            if (target != null)
                target.SendSysMessage(CypherStrings.YourChatEnabled);

            string nameLink = handler.PlayerLink(targetName);

            handler.SendSysMessage(CypherStrings.YouEnableChat, nameLink);

            return true;
        }

        [CommandNonGroup("unpossess", RBACPermissions.CommandUnpossess)]
        static bool HandleUnPossessCommand(CommandHandler handler)
        {
            Unit unit = handler.GetSelectedUnit();
            if (unit == null)
                unit = handler.GetSession().GetPlayer();

            unit.RemoveCharmAuras();

            return true;
        }

        [CommandNonGroup("unstuck", RBACPermissions.CommandUnstuck, true)]
        static bool HandleUnstuckCommand(CommandHandler handler, StringArguments args)
        {
            uint SPELL_UNSTUCK_ID = 7355;
            uint SPELL_UNSTUCK_VISUAL = 2683;

            // No args required for players
            if (handler.GetSession() != null && handler.GetSession().HasPermission(RBACPermissions.CommandsUseUnstuckWithArgs))
            {
                // 7355: "Stuck"
                var player1 = handler.GetSession().GetPlayer();
                if (player1 != null)
                    player1.CastSpell(player1, SPELL_UNSTUCK_ID, false);
                return true;
            }

            if (args.Empty())
                return false;

            string location_str = "inn";
            string loc = args.NextString();
            if (string.IsNullOrEmpty(loc))
                location_str = loc;

            Player player;
            ObjectGuid targetGUID;
            if (!handler.ExtractPlayerTarget(args, out player, out targetGUID))
                return false;

            if (player == null)
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_HOMEBIND);
                stmt.AddValue(0, targetGUID.GetCounter());
                SQLResult result = DB.Characters.Query(stmt);
                if (!result.IsEmpty())
                {
                    Player.SavePositionInDB(new WorldLocation(result.Read<ushort>(0), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), 0.0f), result.Read<ushort>(1), targetGUID);
                    return true;
                }

                return false;
            }

            if (player.IsInFlight() || player.IsInCombat())
            {
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SPELL_UNSTUCK_ID, Difficulty.None);
                if (spellInfo == null)
                    return false;

                Player caster = handler.GetSession().GetPlayer();
                if (caster != null)
                    caster.SendPacket(new DisplayGameError(GameError.ClientLockedOut));

                return false;
            }

            if (location_str == "inn")
            {
                player.TeleportTo(player.GetHomebind());
                return true;
            }

            if (location_str == "graveyard")
            {
                player.RepopAtGraveyard();
                return true;
            }

            //Not a supported argument
            return false;
        }

        [CommandNonGroup("wchange", RBACPermissions.CommandWchange)]
        static bool HandleChangeWeather(CommandHandler handler, uint type, float intensity)
        {
            // Weather is OFF
            if (!WorldConfig.GetBoolValue(WorldCfg.Weather))
            {
                handler.SendSysMessage(CypherStrings.WeatherDisabled);
                return false;
            }

            Player player = handler.GetSession().GetPlayer();
            uint zoneid = player.GetZoneId();

            Weather weather = player.GetMap().GetOrGenerateZoneDefaultWeather(zoneid);
            if (weather == null)
            {
                handler.SendSysMessage(CypherStrings.NoWeather);
                return false;
            }

            weather.SetWeather((WeatherType)type, intensity);
            return true;
        }
    }

    [CommandGroup("additem")]
    class MiscAddItemCommands
    {
        static bool HandleAddItemCommandHelper(CommandHandler handler, Player player, Player playerTarget, VariantArg<ItemLinkData, uint, string> itemArg, OptionalArg<int> countArg, OptionalArg<string> bonusListIdString, OptionalArg<byte> itemContextArg)
        {
            uint itemId = 0;
            List<uint> bonusListIDs = new();
            ItemContext itemContext = ItemContext.None;
            if (itemArg.Is<ItemLinkData>())
            {
                ItemLinkData itemLinkData = itemArg.GetValue();
                itemId = itemLinkData.Item.GetId();
                bonusListIDs = itemLinkData.ItemBonusListIDs;
                itemContext = (ItemContext)itemLinkData.Context;
            }
            else if (itemArg.Is<uint>())
                itemId = itemArg;
            else if (itemArg.Is<string>())
            {
                string itemName = itemArg;
                if (itemName.StartsWith('['))
                    itemName.Remove(0, 1);
                if (itemName.EndsWith(']'))
                    itemName.Remove(itemName.Length - 1, 1);

                var record = CliDB.ItemSparseStorage.Values.FirstOrDefault(sparse =>
                {
                    for (Locale i = Locale.enUS; i < Locale.Total; i += 1)
                        if (itemName == sparse.Display[i])
                            return true;
                    return false;
                });

                if (record == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandCouldnotfind, itemName);
                    return false;
                }

                itemId = record.Id;
            }

            int count = countArg.GetValueOrDefault(1);
            if (count == 0)
                count = 1;

            // semicolon separated bonuslist ids (parse them after all arguments are extracted by strtok!)
            if (bonusListIdString.HasValue)
            {
                var tokens = new StringArray(bonusListIdString.Value, ';');
                foreach (string token in tokens)
                {
                    if (uint.TryParse(token, out uint bonusListId))
                        bonusListIDs.Add(bonusListId);
                }
            }

            if (itemContextArg.HasValue)
            {
                itemContext = (ItemContext)itemContextArg.Value;
                if (itemContext < ItemContext.Max)
                {
                    var contextBonuses = ItemBonusMgr.GetBonusListsForItem(itemId, new(itemContext));
                    bonusListIDs.AddRange(contextBonuses);
                    bonusListIDs.Sort();
                }
            }

            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);
            if (itemTemplate == null)
            {
                handler.SendSysMessage(CypherStrings.CommandItemidinvalid, itemId);
                return false;
            }

            // Subtract
            if (count < 0)
            {
                uint destroyedItemCount = playerTarget.DestroyItemCount(itemId, (uint)-count, true, false);

                if (destroyedItemCount > 0)
                {
                    // output the amount of items successfully destroyed
                    handler.SendSysMessage(CypherStrings.Removeitem, itemId, destroyedItemCount, handler.GetNameLink(playerTarget));

                    // check to see if we were unable to destroy all of the amount requested.
                    uint unableToDestroyItemCount = (uint)(-count - destroyedItemCount);
                    if (unableToDestroyItemCount > 0)
                    {
                        // output message for the amount of items we couldn't destroy
                        handler.SendSysMessage(CypherStrings.RemoveitemFailure, itemId, unableToDestroyItemCount, handler.GetNameLink(playerTarget));
                    }
                }
                else
                {
                    // failed to destroy items of the amount requested
                    handler.SendSysMessage(CypherStrings.RemoveitemFailure, itemId, -count, handler.GetNameLink(playerTarget));
                }

                return true;
            }

            // Adding items
            // check space and find places
            List<ItemPosCount> dest = new();
            InventoryResult msg = playerTarget.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemId, (uint)count, out uint noSpaceForCount);
            if (msg != InventoryResult.Ok)                               // convert to possible store amount
                count -= (int)noSpaceForCount;

            if (count == 0 || dest.Empty())                         // can't add any
            {
                handler.SendSysMessage(CypherStrings.ItemCannotCreate, itemId, noSpaceForCount);
                return false;
            }

            Item item = playerTarget.StoreNewItem(dest, itemId, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(itemId), null, itemContext,
                bonusListIDs.Empty() ? null : bonusListIDs);

            // remove binding (let GM give it to another player later)
            if (player == playerTarget)
            {
                foreach (var itemPosCount in dest)
                {
                    Item item1 = player.GetItemByPos(itemPosCount.pos);
                    if (item1 != null)
                        item1.SetBinding(false);
                }
            }

            if (count > 0 && item != null)
            {
                player.SendNewItem(item, (uint)count, false, true);
                handler.SendSysMessage(CypherStrings.Additem, itemId, count, handler.GetNameLink(playerTarget));
                if (player != playerTarget)
                    playerTarget.SendNewItem(item, (uint)count, true, false);
            }

            if (noSpaceForCount > 0)
                handler.SendSysMessage(CypherStrings.ItemCannotCreate, itemId, noSpaceForCount);

            return true;


        }

        [Command("", RBACPermissions.CommandAdditem, true)]
        static bool HandleAddItemCommand(CommandHandler handler, VariantArg<ItemLinkData, uint, string> item, OptionalArg<int> countArg, OptionalArg<string> bonusListIdString, OptionalArg<byte> itemContextArg)
        {
            Player player = handler.GetSession().GetPlayer();
            Player playerTarget = handler.GetSelectedPlayerOrSelf();

            return HandleAddItemCommandHelper(handler, player, playerTarget, item, countArg, bonusListIdString, itemContextArg);
        }

        [Command("set", RBACPermissions.CommandAdditemset)]
        static bool HandleAddItemSetCommand(CommandHandler handler, VariantArg<ItemLinkData, uint> itemSetId, OptionalArg<string> bonuses, OptionalArg<byte> context)
        {
            // prevent generation all items with itemset field value '0'
            if (itemSetId == 0)
            {
                handler.SendSysMessage(CypherStrings.NoItemsFromItemsetFound, itemSetId);
                return false;
            }

            List<uint> bonusListIDs = new();

            // semicolon separated bonuslist ids (parse them after all arguments are extracted by strtok!)
            if (bonuses.HasValue)
            {
                var tokens = new StringArray(bonuses, ';');
                for (var i = 0; i < tokens.Length; ++i)
                {
                    if (uint.TryParse(tokens[i], out uint id))
                        bonusListIDs.Add(id);
                }
            }

            ItemContext itemContext = ItemContext.None;
            if (context.HasValue)
                itemContext = (ItemContext)context.Value;

            Player player = handler.GetSession().GetPlayer();
            Player playerTarget = handler.GetSelectedPlayer();
            if (playerTarget == null)
                playerTarget = player;

            Log.outDebug(LogFilter.Server, Global.ObjectMgr.GetCypherString(CypherStrings.Additemset), itemSetId);

            bool found = false;
            var its = Global.ObjectMgr.GetItemTemplates();
            foreach (var template in its)
            {
                if (template.Value.GetItemSet() != itemSetId)
                    continue;

                found = true;
                List<ItemPosCount> dest = new();
                InventoryResult msg = playerTarget.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, template.Value.GetId(), 1);
                if (msg == InventoryResult.Ok)
                {
                    List<uint> bonusListIDsForItem = new(bonusListIDs); // copy, bonuses for each depending on context might be different for each item
                    if (itemContext < ItemContext.Max)
                    {
                        var contextBonuses = ItemBonusMgr.GetBonusListsForItem(template.Value.GetId(), new(itemContext));
                        bonusListIDsForItem.AddRange(contextBonuses);
                    }

                    Item item = playerTarget.StoreNewItem(dest, template.Value.GetId(), true, 0, null, itemContext, bonusListIDsForItem.Empty() ? null : bonusListIDsForItem);
                    if (item == null)
                        continue;

                    // remove binding (let GM give it to another player later)
                    if (player == playerTarget)
                        item.SetBinding(false);

                    player.SendNewItem(item, 1, false, true);
                    if (player != playerTarget)
                        playerTarget.SendNewItem(item, 1, true, false);
                }
                else
                {
                    player.SendEquipError(msg, null, null, template.Value.GetId());
                    handler.SendSysMessage(CypherStrings.ItemCannotCreate, template.Value.GetId(), 1);
                }
            }

            if (!found)
            {
                handler.SendSysMessage(CypherStrings.CommandNoitemsetfound, itemSetId);
                return false;
            }
            return true;
        }

        [Command("to", RBACPermissions.CommandAdditemset)]
        static bool HandleAddItemToCommand(CommandHandler handler, PlayerIdentifier target, VariantArg<ItemLinkData, uint, string> item, OptionalArg<int> countArg, OptionalArg<string> bonusListIdString, OptionalArg<byte> itemContextArg)
        {
            Player player = handler.GetSession().GetPlayer();
            if (!target.IsConnected())
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            return HandleAddItemCommandHelper(handler, player, target.GetConnectedPlayer(), item, countArg, bonusListIdString, itemContextArg);
        }
    }
}