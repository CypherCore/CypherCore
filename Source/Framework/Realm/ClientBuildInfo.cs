// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Database;
using System.Linq;

namespace Framework.ClientBuild
{
    struct ClientBuildPlatformType
    {
        public static int Windows = "Win".fourcc();
        public static int macOS = "Mac".fourcc();
    }

    struct ClientBuildArch
    {
        public static int x86 = "x86".fourcc();
        public static int x64 = "x64".fourcc();
        public static int Arm32 = "A32".fourcc();
        public static int Arm64 = "A64".fourcc();
        public static int WA32 = "WA32".fourcc();
    }

    struct ClientBuildType
    {
        public static int Retail = "WoW".fourcc();
        public static int RetailChina = "WoWC".fourcc();
        public static int Beta = "WoWB".fourcc();
        public static int BetaRelease = "WoWE".fourcc();
        public static int Ptr = "WoWT".fourcc();
        public static int PtrRelease = "WoWR".fourcc();
    }

    public class ClientBuildHelper
    {
        static List<ClientBuildInfo> _builds = new();

        public static void LoadBuildInfo()
        {
            _builds.Clear();

            //                                         0             1             2              3              4      5              6              7
            SQLResult result = DB.Login.Query("SELECT majorVersion, minorVersion, bugfixVersion, hotfixVersion, build, win64AuthSeed, mac64AuthSeed, macArmAuthSeed FROM build_info ORDER BY build ASC");
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

                    string win64AuthSeedHexStr = result.Read<string>(5);
                    if (win64AuthSeedHexStr.Length == ClientBuildAuthKey.Size * 2)
                    {
                        ClientBuildAuthKey buildKey = new();
                        buildKey.Variant = new() { Platform = ClientBuildPlatformType.Windows, Arch = ClientBuildArch.x64, Type = ClientBuildType.Retail };
                        buildKey.Key = win64AuthSeedHexStr.ToByteArray();
                        build.AuthKeys.Add(buildKey);
                    }

                    string mac64AuthSeedHexStr = result.Read<string>(6);
                    if (mac64AuthSeedHexStr.Length == ClientBuildAuthKey.Size * 2)
                    {
                        ClientBuildAuthKey buildKey = new();
                        buildKey.Variant = new() { Platform = ClientBuildPlatformType.macOS, Arch = ClientBuildArch.x64, Type = ClientBuildType.Retail };
                        buildKey.Key = mac64AuthSeedHexStr.ToByteArray();
                        build.AuthKeys.Add(buildKey);
                    }

                    string macArmAuthSeedHexStr = result.Read<string>(7);
                    if (macArmAuthSeedHexStr.Length == ClientBuildAuthKey.Size * 2)
                    {
                        ClientBuildAuthKey buildKey = new();
                        buildKey.Variant = new() { Platform = ClientBuildPlatformType.macOS, Arch = ClientBuildArch.Arm64, Type = ClientBuildType.Retail };
                        buildKey.Key = macArmAuthSeedHexStr.ToByteArray();
                        build.AuthKeys.Add(buildKey);
                    }

                    _builds.Add(build);

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
