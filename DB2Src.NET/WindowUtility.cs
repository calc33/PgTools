using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WinForm = System.Windows.Forms;
using System.Windows.Controls;

namespace Db2Source
{
    public static class WindowUtil
    {
        public static void MoveFormNearby(Window window, FrameworkElement control, bool alignRightFirst, bool alignTopFirst, Rect rect)
        {
            Point p1 = control.PointToScreen(rect.Location);
            Point p2 = control.PointToScreen(new Point(rect.X + rect.Width, rect.Y + rect.Height));
            System.Drawing.Rectangle r = new System.Drawing.Rectangle((int)Math.Min(p1.X, p2.X), (int)Math.Min(p1.Y, p2.Y), (int)Math.Abs(p2.X - p1.X), (int)Math.Abs(p2.Y - p1.Y));

            WinForm.Screen sc = WinForm.Screen.FromRectangle(r);
            System.Drawing.Rectangle sr = sc.WorkingArea;
            int pX;
            int pY;
            if (alignRightFirst && sr.Left <= r.Right - window.ActualWidth)
            {
                pX = r.X + r.Width - (int)window.ActualWidth;
            }
            else if (r.Left + (int)window.ActualWidth <= sr.Right)
            {
                pX = r.Left;
            }
            else if (sr.Left <= r.Right - (int)window.ActualWidth)
            {
                pX = r.Right - (int)window.ActualWidth;
            }
            else
            {
                pX = sr.Right - (int)window.ActualWidth;
            }
            if (alignTopFirst && sr.Top <= r.Top - (int)window.ActualHeight)
            {
                pY = r.Top - (int)window.ActualHeight;
            }
            else if (r.Bottom + (int)window.ActualHeight <= sr.Bottom)
            {
                pY = r.Bottom;
            }
            else if (sr.Top <= r.Top - (int)window.ActualHeight)
            {
                pY = r.Top - (int)window.ActualHeight;
            }
            else
            {
                pY = sr.Bottom - (int)window.ActualHeight;
            }
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = pX;
            window.Top = pY;
        }
        public static void MoveFormNearby(Window window, FrameworkElement control, bool alignRightFirst, bool alignTopFirst)
        {
            MoveFormNearby(window, control, alignRightFirst, alignTopFirst, new Rect(0, 0, control.ActualWidth, control.ActualHeight));
        }

        public static Rect GetScreenRect(FrameworkElement control)
        {
            Point p1 = control.PointToScreen(new Point(0, 0));
            Point p2 = control.PointToScreen(new Point(control.ActualWidth, control.ActualHeight));
            System.Drawing.Rectangle r = new System.Drawing.Rectangle((int)p1.X, (int)p1.Y, (int)(p2.X - p1.X), (int)(p2.Y - p1.Y));
            WinForm.Screen sc = WinForm.Screen.FromRectangle(r);
            r = sc.WorkingArea;
            return new Rect(r.X, r.Y, r.Width, r.Height);
        }
    }
}
