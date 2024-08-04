// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Chat
{
    class HyperlinkDataTokenizer
    {
        public HyperlinkDataTokenizer(string arg, bool allowEmptyTokens = false)
        {
            _arg = new(arg);
            _allowEmptyTokens = allowEmptyTokens;
        }

        public bool TryConsumeTo(out dynamic val, Type type)
        {
            val = default;

            if (IsEmpty())
                return _allowEmptyTokens;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                    val = _arg.NextByte(":");
                    return true;
                case TypeCode.Int16:
                    val = _arg.NextUInt16(":");
                    return true;
                case TypeCode.Int32:
                    val = _arg.NextUInt32(":");
                    return true;
                case TypeCode.Int64:
                    val = _arg.NextUInt64(":");
                    return true;
                case TypeCode.Byte:
                    val = _arg.NextByte(":");
                    return true;
                case TypeCode.UInt16:
                    val = _arg.NextUInt16(":");
                    return true;
                case TypeCode.UInt32:
                    val = _arg.NextUInt32(":");
                    return true;
                case TypeCode.UInt64:
                    val = _arg.NextUInt64(":");
                    return true;
                case TypeCode.Single:
                    val = _arg.NextSingle(":");
                    return true;
                case TypeCode.Double:
                    val = _arg.NextDouble(":");
                    return true;
                case TypeCode.String:
                    val = _arg.NextString(":");
                    return true;
                case TypeCode.Object:
                {
                    switch (type.Name)
                    {
                        case nameof(AchievementRecord):
                            val = CliDB.AchievementStorage.LookupByKey(_arg.NextUInt32(":"));
                            if (val != null)
                                return true;
                            break;
                        case nameof(CurrencyTypesRecord):
                            val = CliDB.CurrencyTypesStorage.LookupByKey(_arg.NextUInt32(":"));
                            if (val != null)
                                return true;
                            break;
                        case nameof(GameTele):
                            val = Global.ObjectMgr.GetGameTele(_arg.NextUInt32(":"));
                            if (val != null)
                                return true;
                            break;
                        case nameof(ItemTemplate):
                            val = Global.ObjectMgr.GetItemTemplate(_arg.NextUInt32(":"));
                            if (val != null)
                                return true;
                            break;
                        case nameof(Quest):
                            val = Global.ObjectMgr.GetQuestTemplate(_arg.NextUInt32(":"));
                            if (val != null)
                                return true;
                            break;
                        case nameof(SpellInfo):
                            val = Global.SpellMgr.GetSpellInfo(_arg.NextUInt32(":"), Framework.Constants.Difficulty.None);
                            if (val != null)
                                return true;
                            break;
                        case nameof(ObjectGuid):
                            val = ObjectGuid.FromString(_arg.NextString(":"));
                            if (val != ObjectGuid.FromStringFailed)
                                return true;
                            break;
                        default:
                            return false;
                    }
                    break;
                }
            }

            return false;
        }

        public bool IsEmpty() { return _arg.Empty(); }

        StringArguments _arg;
        bool _allowEmptyTokens;
    }
}