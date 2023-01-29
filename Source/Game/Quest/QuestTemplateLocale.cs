// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;

namespace Game
{
    public class QuestTemplateLocale
    {
        public StringArray AreaDescription { get; set; } = new((int)Locale.Total);
        public StringArray LogDescription { get; set; } = new((int)Locale.Total);
        public StringArray LogTitle { get; set; } = new((int)Locale.Total);
        public StringArray PortraitGiverName { get; set; } = new((int)Locale.Total);
        public StringArray PortraitGiverText { get; set; } = new((int)Locale.Total);
        public StringArray PortraitTurnInName { get; set; } = new((int)Locale.Total);
        public StringArray PortraitTurnInText { get; set; } = new((int)Locale.Total);
        public StringArray QuestCompletionLog { get; set; } = new((int)Locale.Total);
        public StringArray QuestDescription { get; set; } = new((int)Locale.Total);
    }
}