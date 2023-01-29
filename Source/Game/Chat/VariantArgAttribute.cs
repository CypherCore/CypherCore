// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game.Chat
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class VariantArgAttribute : Attribute
    {
        public VariantArgAttribute(params Type[] types)
        {
            Types = types;
        }

        public Type[] Types { get; set; }
    }
}