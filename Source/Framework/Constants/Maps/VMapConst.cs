// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Framework.Constants
{
    public enum VmapDisableTypes : byte
    {
        AreaFlag = 0x1,
        Height = 0x2,
        LOS = 0x4,
        LiquidStatus = 0x8
    }

    public enum VMAPLoadResult
    {
        Error,
        OK,
        Ignored
    }

    public enum LoadResult : byte
    {
        Success,
        FileNotFound,
        VersionMismatch,
        ReadFromFileFailed,
        DisabledInConfig
    }
}
