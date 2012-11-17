﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using YetAnotherRelogger.Helpers.Bot;
using YetAnotherRelogger.Helpers.Tools;
using YetAnotherRelogger.Properties;
using System.Linq;

namespace YetAnotherRelogger.Helpers
{
    public static class AutoPosition
    {
        [Serializable]
        internal class ScreensClass
        {
            public int Order { get; set; }
            public bool Enabled { get; set; }
            public WinAPI.DisplayDevice DisplayDevice { get; set; }

            public string Name
            {
                get { return DisplayDevice.DeviceName; }
            }

            public Rectangle Bounds
            { // Get current screen Rectangle
                get
                {
                    var rect = new Rectangle();
                    // Check if Working area should be forced
                    if (Settings.Default.AutoPosForceWorkingArea)
                        return new Rectangle((int)Settings.Default.AutoPosForceWorkingAreaX,(int)Settings.Default.AutoPosForceWorkingAreaY,(int)Settings.Default.AutoPosForceWorkingAreaW,(int)Settings.Default.AutoPosForceWorkingAreaH);
                    try
                    {
                        var s = Screen.AllScreens.FirstOrDefault(x => (x != null) && x.DeviceName.Equals(DisplayDevice.DeviceName));
                        if (s != null)
                            rect = s.Bounds;
                        else
                        {
                            Logger.Instance.WriteGlobal("ERROR: screen \"{0}\" does not exist! using primary screen sizes");
                            rect = Screen.PrimaryScreen.Bounds;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.WriteGlobal(ex.ToString());
                    }
                    return rect;
                }
            }

            public Rectangle WorkingArea
            { // Get current screen Rectangle
                get
                {
                    var rect = new Rectangle();
                    // Check if Working area should be forced
                    if (Settings.Default.AutoPosForceWorkingArea)
                        return new Rectangle((int)Settings.Default.AutoPosForceWorkingAreaX,(int)Settings.Default.AutoPosForceWorkingAreaY,(int)Settings.Default.AutoPosForceWorkingAreaW,(int)Settings.Default.AutoPosForceWorkingAreaH);
                    try
                    {
                        var s = Screen.AllScreens.FirstOrDefault(x => (x != null) && x.DeviceName.Equals(DisplayDevice.DeviceName));
                        if (s != null)
                            rect = s.WorkingArea;
                        else
                        {
                            Logger.Instance.WriteGlobal("ERROR: screen \"{0}\" does not exist! using primary screen sizes");
                            rect = Screen.PrimaryScreen.WorkingArea;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.WriteGlobal(ex.ToString());
                    }
                    return rect;
                }
            }
        }

        public static void ManualPositionWindow(IntPtr handle, int x, int y, int w, int h, BotClass bot=null)
        {
            // Set window position and size
            try
            {
                WinAPI.RECT rct;
                if (WinAPI.GetWindowRect(new HandleRef(bot, handle), out rct))
                {
                    if (w <= 0)w = rct.Width;
                    if (h <= 0)h = rct.Heigth;

                    Logger.Instance.Write(bot, "ManualPosition window:{0}: X:{1} Y:{2} W:{3} H:{4}", handle, x, y, w, h);
                    if (!WinAPI.SetWindowPos(handle, IntPtr.Zero, x, y, w, h, WinAPI.SetWindowPosFlags.SWP_NOACTIVATE | WinAPI.SetWindowPosFlags.SWP_NOSENDCHANGING))
                        Logger.Instance.Write(bot, "ManualPosition window:{0}: Failed!", handle);
                }
                else
                    Logger.Instance.Write(bot, "ManualPosition Failed to get window rectangle");
            }
            catch (Exception ex)
            {
                Logger.Instance.Write(bot, "ManualPosition Error: " + ex);
            }
        }
        
        #region UpdateScreens
        public static void UpdateScreens()
        {
            
            var tmpList = new List<ScreensClass>();
            var d = new WinAPI.DisplayDevice();
            d.cb = Marshal.SizeOf(d);
            try
            {
                Logger.Instance.WriteGlobal("####[ Detecting Screens ]####");
                for (uint id = 0; WinAPI.EnumDisplayDevices(null, id, ref d, 0); id++)
                {
                    if (d.StateFlags.HasFlag(WinAPI.DisplayDeviceStateFlags.AttachedToDesktop))
                    {
                        var screen = (Settings.Default.AutoPosScreens != null ? Settings.Default.AutoPosScreens.FirstOrDefault(x => x != null && x.DisplayDevice.DeviceKey == d.DeviceKey) : null);
                        var s = new ScreensClass()
                                    {
                                        Enabled = (screen != null ? screen.Enabled : true),
                                        Order = (int) id,
                                        DisplayDevice = d
                                    };
                        tmpList.Add(s);
                        Logger.Instance.WriteGlobal("-{0} Screen {1}: X:{2},Y:{3},W:{4},H:{5} Enabled:{6}", s.DisplayDevice.StateFlags.HasFlag(WinAPI.DisplayDeviceStateFlags.PrimaryDevice) ? " Primary" : "", id, s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height, s.Enabled);

                        Logger.Instance.WriteGlobal("Name: {0}", s.Name);
                        Logger.Instance.WriteGlobal("Device: {0}",s.DisplayDevice.DeviceString);
                        Logger.Instance.WriteGlobal("WorkingArea: X:{0},Y:{1},W:{2},H:{3}", s.WorkingArea.X, s.WorkingArea.Y, s.WorkingArea.Width, s.WorkingArea.Height);
                    }
                    d.cb = Marshal.SizeOf(d);
                }
                // Print screens to log
                Logger.Instance.WriteGlobal("######################");
                Settings.Default.AutoPosScreens = tmpList;
                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteGlobal("Error: Failed to update screens: " + ex);
            }
        }
        #endregion

        public static void PositionWindows()
        {
            try
            {
                var workingScreens = Settings.Default.AutoPosScreens.Where(x=>x.Enabled).ToList();
                if (workingScreens.Count == 0) return;
                workingScreens.Sort((s1, s2) => s1.Order.CompareTo(s2.Order));

                var bots = BotSettings.Instance.Bots.Where(x => x.IsEnabled);

                var sc = 0; // Screen counter
                var dy = 0; // Diablo Y-Axis counter
                var dx = 0; // Diablo X-Axis counter

                // Calculated window height
                var addy = (Settings.Default.AutoPosDiabloCascade ? 30 : Settings.Default.AutoPosDiabloH);

                foreach (var bot in bots)
                {
                    var screen = workingScreens[sc]; // set current screen 

                    // Calculate demonbuddy position
                    if (!(bot.Demonbuddy.ManualPosSize && !Settings.Default.ForceAutoPos))
                    {
                        // todo
                    }

                    // Calculate diablo position
                    if (!(bot.Diablo.ManualPosSize && !Settings.Default.ForceAutoPos))
                    {
                        var y = (int)(addy * dy); // get next position on Y-Axis of the screen
                        // check if window pos+height does exceed screen working area
                        if ((y + addy) > screen.WorkingArea.Height)
                        {
                            dy = y = 0; // reset counters + Y-Axis position
                            dx++; // move to next X-Axis "line"
                        }
                        var x = (int)(Settings.Default.AutoPosDiabloW * dx); // get next position on X-Axis of the screen
                        // check if window pos+width does exceed screen working area
                        if ((x + Settings.Default.AutoPosDiabloW) > screen.WorkingArea.Width)
                        {
                            if (!Settings.Default.AutoPosForceWorkingArea)
                            {
                                sc++;
                                // Check if screen count is bigger than actual screens available
                                if (sc > workingScreens.Count - 1) 
                                    sc = 0; // reset to first screen
                            }
                            dx = x = 0; // reset counters + X-Axis position
                            dy = y = 0; // reset counters + Y-Axis position
                        }

                        if (bot.Diablo.MainWindowHandle != IntPtr.Zero)
                            RepositionWindow(bot.Diablo.MainWindowHandle, x + screen.WorkingArea.X,y + screen.WorkingArea.Y, (int) Settings.Default.AutoPosDiabloW,(int) Settings.Default.AutoPosDiabloH);
                        dy++; // move to next Y-Axis "line"
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteGlobal("Error: AutoPosition failed: " + ex);
            }

        }

        private static void RepositionWindow(IntPtr handle,int x,int y, int w, int h)
        {
            // Set window position and size
            try
            {
                WinAPI.RECT rct;
                if (WinAPI.GetWindowRect(new HandleRef(null, handle), out rct))
                {
                    if (w <= 0) w = rct.Width;
                    if (h <= 0) h = rct.Heigth;

                    Debug.WriteLine("handle: {0} X:{1},Y:{2},W:{3},H:{4}", handle, rct.Left, rct.Top, rct.Width, rct.Heigth);
                    if (rct.Heigth == h && rct.Width == w && rct.Left == x && rct.Top == y)
                    {
                        Logger.Instance.WriteGlobal("No need to reposition: {0}", handle);
                        return;
                    }

                    if (!WinAPI.SetWindowPos(handle, IntPtr.Zero, x, y, w, h, WinAPI.SetWindowPosFlags.SWP_NOACTIVATE | WinAPI.SetWindowPosFlags.SWP_NOSENDCHANGING))
                        Logger.Instance.WriteGlobal("AutoPosition window:{0}: Failed!", handle);
                }
                else
                    Logger.Instance.WriteGlobal("AutoPosition Failed to get window rectangle ({0})", handle);
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteGlobal(ex.ToString());
            }
        }
    }
}
