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
using Framework.Database;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game
{
    public class WardenCheckManager : Singleton<WardenCheckManager>
    {
        WardenCheckManager() { }

        public void LoadWardenChecks()
        {
            // Check if Warden is enabled by config before loading anything
            if (!WorldConfig.GetBoolValue(WorldCfg.WardenEnabled))
            {
                Log.outInfo(LogFilter.Warden, "Warden disabled, loading checks skipped.");
                return;
            }

            //                              0    1     2     3        4       5      6      7
            SQLResult result = DB.World.Query("SELECT id, type, data, result, address, length, str, comment FROM warden_checks ORDER BY id ASC");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Warden checks. DB table `warden_checks` is empty!");
                return;
            }

            uint count = 0;
            do
            {
                ushort id = result.Read<ushort>(0);
                WardenCheckType checkType = (WardenCheckType)result.Read<byte>(1);
                string data = result.Read<string>(2);
                string checkResult = result.Read<string>(3);
                uint address = result.Read<uint>(4);
                byte length = result.Read<byte>(5);
                string str = result.Read<string>(6);
                string comment = result.Read<string>(7);

                WardenCheck wardenCheck = new WardenCheck();
                wardenCheck.Type = checkType;
                wardenCheck.CheckId = id;

                // Initialize action with default action from config
                wardenCheck.Action = (WardenActions)WorldConfig.GetIntValue(WorldCfg.WardenClientFailAction);

                if (checkType == WardenCheckType.PageA || checkType == WardenCheckType.PageB || checkType == WardenCheckType.Driver)
                {
                    wardenCheck.Data = new BigInteger(data.ToByteArray());
                    int len = data.Length / 2;

                    if (wardenCheck.Data.ToByteArray().Length < len)
                    {
                        byte[] temp = wardenCheck.Data.ToByteArray();
                        Array.Reverse(temp);
                        wardenCheck.Data = new BigInteger(temp);
                    }
                }

                if (checkType == WardenCheckType.Memory || checkType == WardenCheckType.Module)
                    MemChecksIdPool.Add(id);
                else
                    OtherChecksIdPool.Add(id);

                if (checkType == WardenCheckType.Memory || checkType == WardenCheckType.PageA || checkType == WardenCheckType.PageB || checkType == WardenCheckType.Proc)
                {
                    wardenCheck.Address = address;
                    wardenCheck.Length = length;
                }

                // PROC_CHECK support missing
                if (checkType == WardenCheckType.Memory || checkType == WardenCheckType.MPQ || checkType == WardenCheckType.LuaStr || checkType == WardenCheckType.Driver || checkType == WardenCheckType.Module)
                    wardenCheck.Str = str;

                CheckStore[id] = wardenCheck;

                if (checkType == WardenCheckType.MPQ || checkType == WardenCheckType.Memory)
                {
                    BigInteger Result = new BigInteger(checkResult.ToByteArray());
                    int len = checkResult.Length / 2;
                    if (Result.ToByteArray().Length < len)
                    {
                        byte[] temp = Result.ToByteArray();
                        Array.Reverse(temp);
                        Result = new BigInteger(temp);
                    }
                    CheckResultStore[id] = Result;
                }

                if (comment.IsEmpty())
                    wardenCheck.Comment = "Undocumented Check";
                else
                    wardenCheck.Comment = comment;

                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} warden checks.", count);
        }

        public void LoadWardenOverrides()
        {
            // Check if Warden is enabled by config before loading anything
            if (!WorldConfig.GetBoolValue(WorldCfg.WardenEnabled))
            {
                Log.outInfo(LogFilter.Warden, "Warden disabled, loading check overrides skipped.");
                return;
            }

            //                                               0        1
            SQLResult result = DB.Characters.Query("SELECT wardenId, action FROM warden_action");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Warden action overrides. DB table `warden_action` is empty!");
                return;
            }

            uint count = 0;
            do
            {
                ushort checkId = result.Read<ushort>(0);
                WardenActions action = (WardenActions)result.Read<byte>(1);

                // Check if action value is in range (0-2, see WardenActions enum)
                if (action > WardenActions.Ban)
                    Log.outError(LogFilter.Warden, "Warden check override action out of range (ID: {0}, action: {1})", checkId, action);
                // Check if check actually exists before accessing the CheckStore vector
                else if (checkId > CheckStore.Count)
                    Log.outError(LogFilter.Warden, "Warden check action override for non-existing check (ID: {0}, action: {1}), skipped", checkId, action);
                else
                {
                    CheckStore[checkId].Action = action;
                    ++count;
                }
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} warden action overrides.", count);
        }

        public WardenCheck GetWardenDataById(ushort Id)
        {
            if (Id < CheckStore.Count)
                return CheckStore[Id];

            return null;
        }

        public BigInteger GetWardenResultById(ushort Id)
        {
            return CheckResultStore.LookupByKey(Id);
        }

        public List<ushort> MemChecksIdPool = new List<ushort>();
        public List<ushort> OtherChecksIdPool = new List<ushort>();
        List<WardenCheck> CheckStore = new List<WardenCheck>();
        Dictionary<uint, BigInteger> CheckResultStore = new Dictionary<uint, BigInteger>();
    }

    public enum WardenActions
    {
        Log,
        Kick,
        Ban
    }

    public enum WardenCheckType
    {
        Memory = 0xF3, // 243: byte moduleNameIndex + uint Offset + byte Len (check to ensure memory isn't modified)
        PageA = 0xB2, // 178: uint Seed + byte[20] SHA1 + uint Addr + byte Len (scans all pages for specified hash)
        PageB = 0xBF, // 191: uint Seed + byte[20] SHA1 + uint Addr + byte Len (scans only pages starts with MZ+PE headers for specified hash)
        MPQ = 0x98, // 152: byte fileNameIndex (check to ensure MPQ file isn't modified)
        LuaStr = 0x8B, // 139: byte luaNameIndex (check to ensure LUA string isn't used)
        Driver = 0x71, // 113: uint Seed + byte[20] SHA1 + byte driverNameIndex (check to ensure driver isn't loaded)
        Timing = 0x57, //  87: empty (check to ensure GetTickCount() isn't detoured)
        Proc = 0x7E, // 126: uint Seed + byte[20] SHA1 + byte moluleNameIndex + byte procNameIndex + uint Offset + byte Len (check to ensure proc isn't detoured)
        Module = 0xD9  // 217: uint Seed + byte[20] SHA1 (check to ensure module isn't injected)
    }

    public class WardenCheck
    {
        public WardenCheckType Type;
        public BigInteger Data;
        public uint Address;                                         // PROC_CHECK, MEM_CHECK, PAGE_CHECK
        public byte Length;                                           // PROC_CHECK, MEM_CHECK, PAGE_CHECK
        public string Str;                                        // LUA, MPQ, DRIVER
        public string Comment;
        public ushort CheckId;
        public WardenActions Action;
    }
}
