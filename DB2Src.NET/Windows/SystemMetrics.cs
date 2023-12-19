using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using System.Drawing;

namespace Db2Source
{
    public enum MinimizedArrange
    {
        FromBottomLeft = 0,
        FromBottomRight = 1,
        FromTopLeft = 2,
        FromTopRight = 3,
        BottomLeftUp = 4,
        BottomRightUp = 5,
        FromTopLeftDown = 2,
        FromTopRightDown = 3,
        Hide = 8
    }

    [Flags]
    public enum DigitizerDevices
    {
        IntegratedTouch = 1,
        ExternalTouch = 2,
        IntegratedPen = 4,
        ExternalPen = 8,
        MultiInput = 0x40,
        Ready = 0x80,
    }

    public static class SystemMetrics
    {
        public static MinimizedArrange Arrange { get { return (MinimizedArrange)NativeMethods.GetSystemMetrics(SM_ARRANGE); } }
        public static int Cleanboot { get { return NativeMethods.GetSystemMetrics(SM_CLEANBOOT); } }
        public static int CMonitors { get { return NativeMethods.GetSystemMetrics(SM_CMONITORS); } }
        public static int CMouseButtons { get { return NativeMethods.GetSystemMetrics(SM_CMOUSEBUTTONS); } }
        public static int ConvertibleSlateMode { get { return NativeMethods.GetSystemMetrics(SM_CONVERTIBLESLATEMODE); } }
        public static int CxBorder { get { return NativeMethods.GetSystemMetrics(SM_CXBORDER); } }
        public static int CxCursor { get { return NativeMethods.GetSystemMetrics(SM_CXCURSOR); } }
        //public static int CxDlgFrame { get { return NativeMethods.GetSystemMetrics(SM_CXDLGFRAME); } }
        public static int CxDoubleClk { get { return NativeMethods.GetSystemMetrics(SM_CXDOUBLECLK); } }
        public static int CxDrag { get { return NativeMethods.GetSystemMetrics(SM_CXDRAG); } }
        public static int CxEdge { get { return NativeMethods.GetSystemMetrics(SM_CXEDGE); } }
        public static int CxFixedFrame { get { return NativeMethods.GetSystemMetrics(SM_CXFIXEDFRAME); } }
        public static int CxFocusBorder { get { return NativeMethods.GetSystemMetrics(SM_CXFOCUSBORDER); } }
        //public static int CxFrame { get { return NativeMethods.GetSystemMetrics(SM_CXFRAME); } }
        public static int CxFullscreen { get { return NativeMethods.GetSystemMetrics(SM_CXFULLSCREEN); } }
        public static int CxHScroll { get { return NativeMethods.GetSystemMetrics(SM_CXHSCROLL); } }
        public static int CxHThumb { get { return NativeMethods.GetSystemMetrics(SM_CXHTHUMB); } }
        public static int CxIcon { get { return NativeMethods.GetSystemMetrics(SM_CXICON); } }
        public static int CxIconSpacing { get { return NativeMethods.GetSystemMetrics(SM_CXICONSPACING); } }
        public static int CxMaximized { get { return NativeMethods.GetSystemMetrics(SM_CXMAXIMIZED); } }
        public static int CxMaxTrack { get { return NativeMethods.GetSystemMetrics(SM_CXMAXTRACK); } }
        public static int CxMenuCheck { get { return NativeMethods.GetSystemMetrics(SM_CXMENUCHECK); } }
        public static int CxMenuSize { get { return NativeMethods.GetSystemMetrics(SM_CXMENUSIZE); } }
        public static int CxMin { get { return NativeMethods.GetSystemMetrics(SM_CXMIN); } }
        public static int CxMinimized { get { return NativeMethods.GetSystemMetrics(SM_CXMINIMIZED); } }
        public static int CxMinSpacing { get { return NativeMethods.GetSystemMetrics(SM_CXMINSPACING); } }
        public static int CxMinTrack { get { return NativeMethods.GetSystemMetrics(SM_CXMINTRACK); } }
        public static int CxPaddedBorder { get { return NativeMethods.GetSystemMetrics(SM_CXPADDEDBORDER); } }
        public static int CxScreen { get { return NativeMethods.GetSystemMetrics(SM_CXSCREEN); } }
        public static int CxSize { get { return NativeMethods.GetSystemMetrics(SM_CXSIZE); } }
        public static int CxSizeFrame { get { return NativeMethods.GetSystemMetrics(SM_CXSIZEFRAME); } }
        public static int CxSmIcon { get { return NativeMethods.GetSystemMetrics(SM_CXSMICON); } }
        public static int CxSmSize { get { return NativeMethods.GetSystemMetrics(SM_CXSMSIZE); } }
        public static int CxVirtualScreen { get { return NativeMethods.GetSystemMetrics(SM_CXVIRTUALSCREEN); } }
        public static int CxVScroll { get { return NativeMethods.GetSystemMetrics(SM_CXVSCROLL); } }
        public static int CyBorder { get { return NativeMethods.GetSystemMetrics(SM_CYBORDER); } }
        public static int CyCaption { get { return NativeMethods.GetSystemMetrics(SM_CYCAPTION); } }
        public static int CyCursor { get { return NativeMethods.GetSystemMetrics(SM_CYCURSOR); } }
        //public static int CyDlgFrame { get { return NativeMethods.GetSystemMetrics(SM_CYDLGFRAME); } }
        public static int CyDoubleClk { get { return NativeMethods.GetSystemMetrics(SM_CYDOUBLECLK); } }
        public static int CyDrag { get { return NativeMethods.GetSystemMetrics(SM_CYDRAG); } }
        public static int CyEdge { get { return NativeMethods.GetSystemMetrics(SM_CYEDGE); } }
        public static int CyFixedFrame { get { return NativeMethods.GetSystemMetrics(SM_CYFIXEDFRAME); } }
        public static int CyFocusBorder { get { return NativeMethods.GetSystemMetrics(SM_CYFOCUSBORDER); } }
        //public static int CyFrame { get { return NativeMethods.GetSystemMetrics(SM_CYFRAME); } }
        public static int CyFullscreen { get { return NativeMethods.GetSystemMetrics(SM_CYFULLSCREEN); } }
        public static int CyHScroll { get { return NativeMethods.GetSystemMetrics(SM_CYHSCROLL); } }
        public static int CyIcon { get { return NativeMethods.GetSystemMetrics(SM_CYICON); } }
        public static int CyIconSpacing { get { return NativeMethods.GetSystemMetrics(SM_CYICONSPACING); } }
        public static int CyKanjiWindow { get { return NativeMethods.GetSystemMetrics(SM_CYKANJIWINDOW); } }
        public static int CyMaximized { get { return NativeMethods.GetSystemMetrics(SM_CYMAXIMIZED); } }
        public static int CyMaxTrack { get { return NativeMethods.GetSystemMetrics(SM_CYMAXTRACK); } }
        public static int CyMenu { get { return NativeMethods.GetSystemMetrics(SM_CYMENU); } }
        public static int CyMenuCheck { get { return NativeMethods.GetSystemMetrics(SM_CYMENUCHECK); } }
        public static int CyMenuSize { get { return NativeMethods.GetSystemMetrics(SM_CYMENUSIZE); } }
        public static int CyMin { get { return NativeMethods.GetSystemMetrics(SM_CYMIN); } }
        public static int CyMinimized { get { return NativeMethods.GetSystemMetrics(SM_CYMINIMIZED); } }
        public static int CyMinSpacing { get { return NativeMethods.GetSystemMetrics(SM_CYMINSPACING); } }
        public static int CyMinTrack { get { return NativeMethods.GetSystemMetrics(SM_CYMINTRACK); } }
        public static int CyScreen { get { return NativeMethods.GetSystemMetrics(SM_CYSCREEN); } }
        public static int CySize { get { return NativeMethods.GetSystemMetrics(SM_CYSIZE); } }
        public static int CySizeFrame { get { return NativeMethods.GetSystemMetrics(SM_CYSIZEFRAME); } }
        public static int CySmCaption { get { return NativeMethods.GetSystemMetrics(SM_CYSMCAPTION); } }
        public static int CySmIcon { get { return NativeMethods.GetSystemMetrics(SM_CYSMICON); } }
        public static int CySmSize { get { return NativeMethods.GetSystemMetrics(SM_CYSMSIZE); } }
        public static int CyVirtualScreen { get { return NativeMethods.GetSystemMetrics(SM_CYVIRTUALSCREEN); } }
        public static int CyVScroll { get { return NativeMethods.GetSystemMetrics(SM_CYVSCROLL); } }
        public static int CyVThumb { get { return NativeMethods.GetSystemMetrics(SM_CYVTHUMB); } }
        public static int DbcsEnabled { get { return NativeMethods.GetSystemMetrics(SM_DBCSENABLED); } }
        public static int Debug { get { return NativeMethods.GetSystemMetrics(SM_DEBUG); } }
        public static DigitizerDevices Digitizer { get { return (DigitizerDevices)NativeMethods.GetSystemMetrics(SM_DIGITIZER); } }
        public static int Immenabled { get { return NativeMethods.GetSystemMetrics(SM_IMMENABLED); } }
        public static int MaximumTouches { get { return NativeMethods.GetSystemMetrics(SM_MAXIMUMTOUCHES); } }
        public static int MediaCenter { get { return NativeMethods.GetSystemMetrics(SM_MEDIACENTER); } }
        public static int MenuDropAlignment { get { return NativeMethods.GetSystemMetrics(SM_MENUDROPALIGNMENT); } }
        public static int MideastEnabled { get { return NativeMethods.GetSystemMetrics(SM_MIDEASTENABLED); } }
        public static int MousePresent { get { return NativeMethods.GetSystemMetrics(SM_MOUSEPRESENT); } }
        public static int MouseHorizontalWheelPresent { get { return NativeMethods.GetSystemMetrics(SM_MOUSEHORIZONTALWHEELPRESENT); } }
        public static int MouseWheelPresent { get { return NativeMethods.GetSystemMetrics(SM_MOUSEWHEELPRESENT); } }
        public static int Network { get { return NativeMethods.GetSystemMetrics(SM_NETWORK); } }
        public static int PenWindows { get { return NativeMethods.GetSystemMetrics(SM_PENWINDOWS); } }
        public static int RemoteControl { get { return NativeMethods.GetSystemMetrics(SM_REMOTECONTROL); } }
        public static int RemoteSession { get { return NativeMethods.GetSystemMetrics(SM_REMOTESESSION); } }
        public static int SameDisplayFormat { get { return NativeMethods.GetSystemMetrics(SM_SAMEDISPLAYFORMAT); } }
        public static int Secure { get { return NativeMethods.GetSystemMetrics(SM_SECURE); } }
        public static int ServerR2 { get { return NativeMethods.GetSystemMetrics(SM_SERVERR2); } }
        public static int Showsounds { get { return NativeMethods.GetSystemMetrics(SM_SHOWSOUNDS); } }
        public static int Shuttingdown { get { return NativeMethods.GetSystemMetrics(SM_SHUTTINGDOWN); } }
        public static int SlowMachine { get { return NativeMethods.GetSystemMetrics(SM_SLOWMACHINE); } }
        public static int Starter { get { return NativeMethods.GetSystemMetrics(SM_STARTER); } }
        public static int SwapButton { get { return NativeMethods.GetSystemMetrics(SM_SWAPBUTTON); } }
        public static int SystemDocked { get { return NativeMethods.GetSystemMetrics(SM_SYSTEMDOCKED); } }
        public static int TabletPc { get { return NativeMethods.GetSystemMetrics(SM_TABLETPC); } }
        public static int XVirtualScreen { get { return NativeMethods.GetSystemMetrics(SM_XVIRTUALSCREEN); } }
        public static int YVirtualScreen { get { return NativeMethods.GetSystemMetrics(SM_YVIRTUALSCREEN); } }

        public static Size Border { get { return new Size(CxBorder, CyBorder); } }
        public static Size Cursor { get { return new Size(CxCursor, CyCursor); } }
        //public static Size DlgFrame { get { return new Size(CxDlgFrame, CyDlgFrame); } }
        public static Size DoubleClk { get { return new Size(CxDoubleClk, CyDoubleClk); } }
        public static Size Drag { get { return new Size(CxDrag, CyDrag); } }
        public static Size Edge { get { return new Size(CxEdge, CyEdge); } }
        public static Size FixedFrame { get { return new Size(CxFixedFrame, CyFixedFrame); } }
        public static Size FocusBorder { get { return new Size(CxFocusBorder, CyFocusBorder); } }
        //public static Size Frame { get { return new Size(CxFrame, CyFrame); } }
        public static Size Fullscreen { get { return new Size(CxFullscreen, CyFullscreen); } }
        public static Size HScroll { get { return new Size(CxHScroll, CyHScroll); } }
        public static Size Icon { get { return new Size(CxIcon, CyIcon); } }
        public static Size IconSpacing { get { return new Size(CxIconSpacing, CyIconSpacing); } }
        public static Size Maximized { get { return new Size(CxMaximized, CyMaximized); } }
        public static Size MaxTrack { get { return new Size(CxMaxTrack, CyMaxTrack); } }
        public static Size MenuCheck { get { return new Size(CxMenuCheck, CyMenuCheck); } }
        public static Size MenuSize { get { return new Size(CxMenuSize, CyMenuSize); } }
        public static Size Min { get { return new Size(CxMin, CyMin); } }
        public static Size Minimized { get { return new Size(CxMinimized, CyMinimized); } }
        public static Size MinSpacing { get { return new Size(CxMinSpacing, CyMinSpacing); } }
        public static Size MinTrack { get { return new Size(CxMinTrack, CyMinTrack); } }
        public static Size Screen { get { return new Size(CxScreen, CyScreen); } }
        public static Size Size { get { return new Size(CxSize, CySize); } }
        public static Size SizeFrame { get { return new Size(CxSizeFrame, CySizeFrame); } }
        public static Size SmIcon { get { return new Size(CxSmIcon, CySmIcon); } }
        public static Size SmSize { get { return new Size(CxSmSize, CySmSize); } }
        public static Rectangle VirtualScreen { get { return new Rectangle(XVirtualScreen, YVirtualScreen, CxVirtualScreen, CyVirtualScreen); } }
        public static Size VScroll { get { return new Size(CxVScroll, CyVScroll); } }


        private const int SM_ARRANGE = 56;
        private const int SM_CLEANBOOT = 67;
        private const int SM_CMONITORS = 80;
        private const int SM_CMOUSEBUTTONS = 43;
        private const int SM_CONVERTIBLESLATEMODE = 0x2003;
        private const int SM_CXBORDER = 5;
        private const int SM_CXCURSOR = 13;
        private const int SM_CXDOUBLECLK = 36;
        private const int SM_CXDRAG = 68;
        private const int SM_CXEDGE = 45;
        private const int SM_CXFIXEDFRAME = 7;
        private const int SM_CXFOCUSBORDER = 83;
        private const int SM_CXFULLSCREEN = 16;
        private const int SM_CXHSCROLL = 21;
        private const int SM_CXHTHUMB = 10;
        private const int SM_CXICON = 11;
        private const int SM_CXICONSPACING = 38;
        private const int SM_CXMAXIMIZED = 61;
        private const int SM_CXMAXTRACK = 59;
        private const int SM_CXMENUCHECK = 71;
        private const int SM_CXMENUSIZE = 54;
        private const int SM_CXMIN = 28;
        private const int SM_CXMINIMIZED = 57;
        private const int SM_CXMINSPACING = 47;
        private const int SM_CXMINTRACK = 34;
        private const int SM_CXPADDEDBORDER = 92;
        private const int SM_CXSCREEN = 0;
        private const int SM_CXSIZE = 30;
        private const int SM_CXSIZEFRAME = 32;
        private const int SM_CXSMICON = 49;
        private const int SM_CXSMSIZE = 52;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CXVSCROLL = 2;
        private const int SM_CYBORDER = 6;
        private const int SM_CYCAPTION = 4;
        private const int SM_CYCURSOR = 14;
        private const int SM_CYDOUBLECLK = 37;
        private const int SM_CYDRAG = 69;
        private const int SM_CYEDGE = 46;
        private const int SM_CYFIXEDFRAME = 8;
        private const int SM_CYFOCUSBORDER = 84;
        private const int SM_CYFULLSCREEN = 17;
        private const int SM_CYHSCROLL = 3;
        private const int SM_CYICON = 12;
        private const int SM_CYICONSPACING = 39;
        private const int SM_CYKANJIWINDOW = 18;
        private const int SM_CYMAXIMIZED = 62;
        private const int SM_CYMAXTRACK = 60;
        private const int SM_CYMENU = 15;
        private const int SM_CYMENUCHECK = 72;
        private const int SM_CYMENUSIZE = 55;
        private const int SM_CYMIN = 29;
        private const int SM_CYMINIMIZED = 58;
        private const int SM_CYMINSPACING = 48;
        private const int SM_CYMINTRACK = 35;
        private const int SM_CYSCREEN = 1;
        private const int SM_CYSIZE = 31;
        private const int SM_CYSIZEFRAME = 33;
        private const int SM_CYSMCAPTION = 51;
        private const int SM_CYSMICON = 50;
        private const int SM_CYSMSIZE = 53;
        private const int SM_CYVIRTUALSCREEN = 79;
        private const int SM_CYVSCROLL = 20;
        private const int SM_CYVTHUMB = 9;
        private const int SM_DBCSENABLED = 42;
        private const int SM_DEBUG = 22;
        private const int SM_DIGITIZER = 94;
        private const int SM_IMMENABLED = 82;
        private const int SM_MAXIMUMTOUCHES = 95;
        private const int SM_MEDIACENTER = 87;
        private const int SM_MENUDROPALIGNMENT = 40;
        private const int SM_MIDEASTENABLED = 74;
        private const int SM_MOUSEPRESENT = 19;
        private const int SM_MOUSEHORIZONTALWHEELPRESENT = 91;
        private const int SM_MOUSEWHEELPRESENT = 75;
        private const int SM_NETWORK = 63;
        private const int SM_PENWINDOWS = 41;
        private const int SM_REMOTECONTROL = 0x2001;
        private const int SM_REMOTESESSION = 0x1000;
        private const int SM_SAMEDISPLAYFORMAT = 81;
        private const int SM_SECURE = 44;
        private const int SM_SERVERR2 = 89;
        private const int SM_SHOWSOUNDS = 70;
        private const int SM_SHUTTINGDOWN = 0x2000;
        private const int SM_SLOWMACHINE = 73;
        private const int SM_STARTER = 88;
        private const int SM_SWAPBUTTON = 23;
        private const int SM_SYSTEMDOCKED = 0x2004;
        private const int SM_TABLETPC = 86;
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private class NativeMethods
        {
            [DllImport("User32.dll", CharSet = CharSet.Auto)]
            public static extern int GetSystemMetrics(int nIndex);
        }
    }
}
