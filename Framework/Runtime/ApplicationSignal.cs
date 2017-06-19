/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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

using System.Runtime.InteropServices;
using System;
using System.Diagnostics;

namespace Framework.Runtime
{
    public delegate bool EventHandler(CtrlType sig);

    public class ApplicationSignal
    {
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        public static void RemoveConsoleQuickEditMode()
        {
            IntPtr handle = GetStdHandle(-10);

            // get current console mode
            uint consoleMode;
            if (!GetConsoleMode(handle, out consoleMode))
            {
                // ERROR: Unable to get console mode.
                return;
            }

            // Clear the quick edit bit in the mode flags
            consoleMode &= ~0x0040u;

            // set the new mode
            SetConsoleMode(handle, consoleMode);
        }
    }

    public enum CtrlType
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }
}
