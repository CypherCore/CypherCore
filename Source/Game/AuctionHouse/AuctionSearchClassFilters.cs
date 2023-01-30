// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game
{
    public class AuctionSearchClassFilters
    {
        public class SubclassFilter
        {
            public ulong[] InvTypes = new ulong[ItemConst.MaxItemSubclassTotal];
            public FilterType SubclassMask;
        }

        public enum FilterType : uint
        {
            SkipClass = 0,
            SkipSubclass = 0xFFFFFFFF,
            SkipInvtype = 0xFFFFFFFF
        }

        public SubclassFilter[] Classes = new SubclassFilter[(int)ItemClass.Max];

        public AuctionSearchClassFilters()
        {
            for (var i = 0; i < (int)ItemClass.Max; ++i)
                Classes[i] = new SubclassFilter();
        }
    }
}