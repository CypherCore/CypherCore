// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Database;
using System;
using System.Collections.Generic;

namespace Framework.ClientBuild
{
    public class ClientBuildHelper
    {
        static List<ClientBuildInfo> _builds = new();

        public static void LoadBuildInfo()
        {
            _builds.Clear();

            //                                         0             1             2              3              4
            SQLResult result = DB.Login.Query("SELECT majorVersion, minorVersion, bugfixVersion, hotfixVersion, build FROM build_info ORDER BY build ASC");
            if (!result.IsEmpty())
            {
                do
                {
                    ClientBuildInfo build = new();
                    build.MajorVersion = result.Read<uint>(0);
                    build.MinorVersion = result.Read<uint>(1);
                    build.BugfixVersion = result.Read<uint>(2);
                    string hotfixVersion = result.Read<string>(3);
                    if (!hotfixVersion.IsEmpty() && hotfixVersion.Length < build.HotfixVersion.Length)
                        build.HotfixVersion = hotfixVersion.ToCharArray();

                    build.Build = result.Read<uint>(4);
                }
                while (result.NextRow());
            }

            //                                          0        1           2       3       4
            result = DB.Login.Query("SELECT `build`, `platform`, `arch`, `type`, `key` FROM `build_auth_key`");
            if (!result.IsEmpty())
            {
                do
                {
                    uint build = result.Read<uint>(0);
                    var buildInfo = _builds.Find(p => p.Build == build);
                    if (buildInfo == null)
                    {
                        Log.outError(LogFilter.Sql, $"ClientBuildHealper.LoadBuildInfo: Unknown `build` {build} in `build_auth_key` - missing from `build_info`, skipped.");
                        continue;
                    }

                    string platformType = result.Read<string>(1);
                    if (!IsPlatformTypeValid(platformType))
                    {
                        Log.outError(LogFilter.Sql, $"ClientBuild::LoadBuildInfo: Invalid platform {platformType} for `build` {build} in `build_auth_key`, skipped.");
                        continue;
                    }

                    string arch = result.Read<string>(2);
                    if (!IsArcValid(arch))
                    {
                        Log.outError(LogFilter.Sql, $"ClientBuild::LoadBuildInfo: Invalid `arch` {arch} for `build` {build} in `build_auth_key`, skipped.");
                        continue;
                    }

                    string type = result.Read<string>(3);
                    if (!IsTypeValid(type))
                    {
                        Log.outError(LogFilter.Sql, $"ClientBuild::LoadBuildInfo: Invalid `type` {type} for `build` {build} in `build_auth_key`, skipped.");
                        continue;
                    }

                    ClientBuildAuthKey buildKey = new()
                    {
                        Variant = new() { Platform = platformType.ToFourCC(), Arch = arch.ToFourCC(), Type = type.ToFourCC() },
                        Key = result.Read<byte[]>(4)
                    };

                    buildInfo.AuthKeys.Add(buildKey);

                } while (result.NextRow());
            }
        }

        public static ClientBuildInfo GetBuildInfo(uint build)
        {
            return _builds.Find(x => x.Build == build);
        }

        public static uint GetMinorMajorBugfixVersionForBuild(uint build)
        {
            ClientBuildInfo buildInfo = _builds.Find(p => p.Build < build);
            return buildInfo != null ? (buildInfo.MajorVersion * 10000 + buildInfo.MinorVersion * 100 + buildInfo.BugfixVersion) : 0;
        }

        public static bool IsValid(string platform)
        {
            switch (platform)
            {
                case "Win":
                case "Wn64":
                case "WinA":
                case "Mac":
                case "Mc64":
                case "MacA":
                    return true;
                default:
                    break;
            }

            return false;
        }

        static bool IsPlatformTypeValid(string platformType)
        {
            if (platformType.Length > 4)
                return false;

            switch (platformType)
            {
                case "Win":
                case "Mac":
                    return true;
                default:
                    break;
            }

            return false;
        }

        static bool IsArcValid(string arch)
        {
            if (arch.Length > 4)
                return false;

            switch (arch)
            {
                case "x86":
                case "x64":
                case "A32":
                case "A64":
                case "WA32":
                    return true;
                default:
                    break;
            }

            return false;
        }

        static bool IsTypeValid(string type)
        {
            if (type.Length > 4)
                return false;

            switch (type)
            {
                case "WoW":
                case "WoWC":
                case "WoWB":
                case "WoWE":
                case "WoWT":
                case "WoWR":
                    return true;
                default:
                    break;
            }

            return false;
        }
    }

    public class ClientBuildVariantId
    {
        public int Platform;
        public int Arch;
        public int Type;
    }

    public class ClientBuildAuthKey
    {
        public static int Size = 16;

        public ClientBuildVariantId Variant;
        public byte[] Key = new byte[Size];
    }

    public class ClientBuildInfo
    {
        public uint Build;
        public uint MajorVersion;
        public uint MinorVersion;
        public uint BugfixVersion;
        public char[] HotfixVersion = new char[4];
        public List<ClientBuildAuthKey> AuthKeys = new();
    }
}
