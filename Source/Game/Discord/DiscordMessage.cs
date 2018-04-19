using System;
using System.Collections.Generic;
using System.Text;
using Framework.Constants;

namespace Game.Discord
{
    public class DiscordMessage
    {
        public DiscordMessageChannel Channel { get; set; }
        public string Message { get; set; }

        //Channel Specific
        public string CharacterName { get; set; }
        public bool IsGm { get; set; }
    }
}
