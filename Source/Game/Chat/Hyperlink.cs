// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System;

namespace Game.Chat
{
    class Hyperlink
    {
        public static ChatCommandResult TryParse(out dynamic value, Type type, CommandHandler handler, string arg)
        {
            value = default;

            HyperlinkInfo info = ParseSingleHyperlink(arg);
            // invalid hyperlinks cannot be consumed
            if (info == null)
                return default;

            if (!typeof(IHyperlink).IsAssignableFrom(type))
                return default;

            IHyperlink hyperLinkValue = (IHyperlink)Activator.CreateInstance(type);

            // check if we got the right tag
            if (info.Tag != hyperLinkValue.tag())
                return default;

            // store value
            if (!hyperLinkValue.Parse(info.Data))
                return new ChatCommandResult(handler.GetCypherString(CypherStrings.CmdparserLinkdataInvalid));

            value = hyperLinkValue;

            // finally, skip any potential delimiters
            var (token, next) = info.Tail.Tokenize();
            if (token.IsEmpty()) /* empty token = first character is delimiter, skip past it */
                return new ChatCommandResult(next);
            else
                return new ChatCommandResult(info.Tail);
        }

        public static bool CheckAllLinks(string str)
        {
            // Step 1: Disallow all control sequences except ||, |H, |h, |c and |r
            {
                int pos = 0;
                while ((pos = str.IndexOf('|', pos)) != -1)
                {
                    char next = str[pos + 1];
                    if (next == 'H' || next == 'h' || next == 'c' || next == 'r' || next == '|')
                        pos += 2;
                    else
                        return false;
                }
            }

            // Step 2: Parse all link sequences
            // They look like this: |c<color>|H<linktag>:<linkdata>|h[<linktext>]|h|r
            // - <color> is 8 hex characters AARRGGBB
            // - <linktag> is arbitrary length [a-z_]
            // - <linkdata> is arbitrary length, no | contained
            // - <linktext> is printable
            {
                int pos = 0;
                while ((pos = str.IndexOf('|', pos)) != -1)
                {
                    if (str[pos + 1] == '|') // this is an escaped pipe character (||)
                    {
                        pos += 2;
                        continue;
                    }

                    HyperlinkInfo info = ParseSingleHyperlink(str.Substring(pos));
                    if (info == null)// todo fix me || !ValidateLinkInfo(info))
                        return false;

                    // tag is fine, find the next one
                    pos = str.Length - info.Tail.Length;
                }
            }

            // all tags are valid
            return true;
        }

        public static HyperlinkInfo ParseSingleHyperlink(string str)
        {
            if (str.IsEmpty())
                return null;

            string color = "";
            string tag = "";
            string data = "";
            string text = "";

            int pos = 0;

            //color tag
            if (str[pos++] != '|' || str[pos++] != 'c')
                return null;

            if (str.Length < 8)
                return null;

            if (str[pos] == 'n')
            {
                // numeric color id
                pos++;
                int endOfColor = str.IndexOf(":", pos);
                if (endOfColor != -1)
                {
                    color = str.Substring(pos, endOfColor - pos);
                    pos = endOfColor + 1;
                }
                else
                    return null;
            }
            else
            {
                // hex color
                color = str.Substring(pos, 8);
                pos += 8;
            }

            if (str[pos++] != '|' || str[pos++] != 'H')
                return null;

            // tag+data part follows
            int delimPos = str.IndexOf('|', pos);
            if (delimPos != -1)
            {
                tag = str.Substring(pos, delimPos - pos);
                pos = (delimPos + 1);
            }
            else
                return null;

            // split tag if : is present (data separator)
            int dataStart = tag.IndexOf(':');
            if (dataStart != -1)
            {
                data = tag.Substring(dataStart + 1);
                tag = tag.Substring(0, dataStart);
            }

            // ok, next should be link data end tag...
            if (str[pos++] != 'h')
                return null;

            // extract text, must be between []
            if (str[pos] != '[')
                return null;

            int openBrackets = 0;
            for (int nameItr = pos; nameItr < str.Length; ++nameItr)
            {
                switch (str[nameItr])
                {
                    case '[':
                        ++openBrackets;
                        break;
                    case ']':
                        --openBrackets;
                        break;
                    default:
                        break;
                }

                if (openBrackets == 0)
                {
                    text = str.Substring(pos + 1, (nameItr - 1) - pos);
                    pos = nameItr + 1;
                    break;
                }
            }

            // check end tag
            if (str[pos++] != '|' || str[pos++] != 'h' || str[pos++] != '|' || str[pos++] != 'r')
                return null;

            // ok, valid hyperlink, return info
            return new(str.Substring(pos), color, tag, data, text);
        }        
    }

    class HyperlinkInfo
    {
        public string Tail;
        public HyperlinkColor color;
        public string Tag;
        public string Data;
        public string Text;

        public HyperlinkInfo(string t, string c, string ta, string d, string te)
        {
            Tail = t;
            color = new(c);
            Tag = ta;
            Data = d;
            Text = te;
        }
    }

    struct HyperlinkColor(string c)
    {
        public string data = c;
    }
}