﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// ShowNearby で表示位置の指定に使用
    /// </summary>
    public enum NearbyLocation
    {
        /// <summary>
        /// 下側に表示、左揃え
        /// </summary>
        DownLeft,
        /// <summary>
        /// 下側に表示、右揃え
        /// </summary>
        DownRight,
        /// <summary>
        /// 上側に表示、左揃え
        /// </summary>
        UpLeft,
        /// <summary>
        /// 上側に表示、右揃え
        /// </summary>
        UpRight,
        /// <summary>
        /// 左側に表示、上揃え
        /// </summary>
        LeftSideTop,
        /// <summary>
        /// 左側に表示、下揃え
        /// </summary>
        LeftSideBottom,
        /// <summary>
        /// 右側に表示、上揃え
        /// </summary>
        RightSideTop,
        /// <summary>
        /// 右側に表示、下揃え
        /// </summary>
        RightSideBottom,
    }
    public class WindowLocator
    {
        public static void LocateNearby(FrameworkElement target, Rect rectOnTarget, Window window, NearbyLocation location)
        {
            new WindowLocator(target, rectOnTarget, window, location);
        }
        public static void LocateNearby(FrameworkElement target, Window window, NearbyLocation location)
        {
            new WindowLocator(target, window, location);
        }

        public static Rect GetWorkingAreaOf(FrameworkElement element)
        {
            Point p = element.PointToScreen(new Point());
            System.Windows.Forms.Screen sc = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)p.X, (int)p.Y));
            return new Rect(sc.WorkingArea.X, sc.WorkingArea.Y, sc.WorkingArea.Width, sc.WorkingArea.Height);
        }
        private static readonly Dictionary<NearbyLocation, NearbyLocation[]> NearbyLocationCandidates = new Dictionary<NearbyLocation, NearbyLocation[]>()
        {
            { NearbyLocation.DownLeft, new NearbyLocation[] { NearbyLocation.DownRight, NearbyLocation.UpLeft, NearbyLocation.UpRight } },
            { NearbyLocation.DownRight, new NearbyLocation[] { NearbyLocation.DownLeft, NearbyLocation.UpRight, NearbyLocation.UpLeft } },
            { NearbyLocation.UpLeft, new NearbyLocation[] { NearbyLocation.UpRight, NearbyLocation.DownLeft, NearbyLocation.DownRight } },
            { NearbyLocation.UpRight, new NearbyLocation[] { NearbyLocation.UpLeft, NearbyLocation.DownRight, NearbyLocation.DownLeft } },
            { NearbyLocation.LeftSideTop, new NearbyLocation[] { NearbyLocation.LeftSideBottom, NearbyLocation.RightSideTop, NearbyLocation.RightSideBottom } },
            { NearbyLocation.LeftSideBottom, new NearbyLocation[] { NearbyLocation.LeftSideTop, NearbyLocation.RightSideBottom, NearbyLocation.RightSideTop } },
            { NearbyLocation.RightSideTop, new NearbyLocation[] { NearbyLocation.RightSideBottom, NearbyLocation.LeftSideTop, NearbyLocation.LeftSideBottom } },
            { NearbyLocation.RightSideBottom, new NearbyLocation[] { NearbyLocation.RightSideTop, NearbyLocation.LeftSideBottom, NearbyLocation.LeftSideTop } },
        };

        private Rect _placement;
        private Rect _workingArea;
        private Window _window;
        private NearbyLocation _location;
        internal WindowLocator(FrameworkElement target, Rect rectOnTarget, Window window, NearbyLocation location)
        {
            _placement = new Rect(target.PointToScreen(rectOnTarget.TopLeft), target.PointToScreen(rectOnTarget.BottomRight));
            _workingArea = GetWorkingAreaOf(target);
            _location = location;
            _window = window;
            _window.LayoutUpdated += Window_LayoutUpdated;
            Rect r = GetWindowPlacement(false);
            _window.WindowStartupLocation = WindowStartupLocation.Manual;
            _window.Left = r.X;
            _window.Top = r.Y;
        }
        internal WindowLocator(FrameworkElement target, Window window, NearbyLocation location)
        {
            _placement = new Rect(target.PointToScreen(new Point()), target.PointToScreen(new Point(target.ActualWidth, target.ActualHeight)));
            _workingArea = GetWorkingAreaOf(target);
            _location = location;
            _window = window;
            _window.LayoutUpdated += Window_LayoutUpdated;
            Rect r = GetWindowPlacement(false);
            _window.WindowStartupLocation = WindowStartupLocation.Manual;
            _window.Left = r.X;
            _window.Top = r.Y;
        }

        private void Dispose()
        {
            _window.LayoutUpdated -= Window_LayoutUpdated;
            _window = null;
        }

        private Rect CalcPlacement(NearbyLocation location, Rect windowRect)
        {
            switch (location)
            {
                case NearbyLocation.DownLeft:
                    return new Rect(_placement.Left - windowRect.Left, _placement.Bottom - windowRect.Top, windowRect.Width, windowRect.Height);
                case NearbyLocation.DownRight:
                    return new Rect(_placement.Right - windowRect.Right, _placement.Bottom - windowRect.Top, windowRect.Width, windowRect.Height);
                case NearbyLocation.UpLeft:
                    return new Rect(_placement.Left - windowRect.Left, _placement.Top - windowRect.Bottom, windowRect.Width, windowRect.Height);
                case NearbyLocation.UpRight:
                    return new Rect(_placement.Right - windowRect.Right, _placement.Top - windowRect.Bottom, windowRect.Width, windowRect.Height);
                case NearbyLocation.LeftSideTop:
                    return new Rect(_placement.Left - windowRect.Right, _placement.Top - windowRect.Top, windowRect.Width, windowRect.Height);
                case NearbyLocation.LeftSideBottom:
                    return new Rect(_placement.Left - windowRect.Right, _placement.Bottom - windowRect.Bottom, windowRect.Width, windowRect.Height);
                case NearbyLocation.RightSideTop:
                    return new Rect(_placement.Right - windowRect.Left, _placement.Top - windowRect.Top, windowRect.Width, windowRect.Height);
                case NearbyLocation.RightSideBottom:
                    return new Rect(_placement.Right - windowRect.Left, _placement.Bottom - windowRect.Bottom, windowRect.Width, windowRect.Height);
                default:
                    return new Rect(_placement.Left - windowRect.Left, _placement.Bottom - windowRect.Top, windowRect.Width, windowRect.Height);
            }
        }
        private Rect GetWindowPlacement(bool useActualSize)
        {
            Rect windowRect;
            if (useActualSize)
            {
                FrameworkElement c = _window.Content as FrameworkElement;
                Point p = new Point();
                if (_window.IsVisible)
                {
                    p = c.PointToScreen(p);
                    p.X -= _window.Left;
                    p.Y -= _window.Top;
                }
                //windowRect = new Rect(p.X, p.Y, c.ActualWidth, c.ActualHeight);
                // windowの横幅の差異は透明な領域だが上下は不透明な領域
                windowRect = new Rect(p.X, 0, c.ActualWidth, _window.ActualHeight);
            }
            else
            {
                windowRect = new Rect(0, 0, _window.Width, _window.Height);
            }
            Rect ret = CalcPlacement(_location, windowRect);
            if (_workingArea.Contains(ret)) {
                return ret;
            }
            foreach (NearbyLocation loc in NearbyLocationCandidates[_location])
            {
                Rect r = CalcPlacement(_location, windowRect);
                if (_workingArea.Contains(ret))
                {
                    return r;
                }
            }
            if (_workingArea.Right < ret.Right)
            {
                ret.X += (_workingArea.Right - ret.Right);
            }
            if (_workingArea.Bottom < ret.Bottom)
            {
                ret.Y += (_workingArea.Bottom - ret.Bottom);
            }
            if (ret.Left < _workingArea.Left)
            {
                ret.X = _workingArea.Left;
            }
            if (ret.Top < _workingArea.Top)
            {
                ret.Y = _workingArea.Top;
            }
            return ret;
        }
        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            if (!_window.IsVisible)
            {
                return;
            }
            Rect r = GetWindowPlacement(true);
            _window.Left = r.X;
            _window.Top = r.Y;
            Dispose();
        }
        //private static readonly Thickness ResizeDelta = new Thickness(10, 0, 15, 0);
    }

    public class CloseOnDeactiveWindowHelper
    {
        private Window _window;
        public CloseOnDeactiveWindowHelper(Window window, bool closeOnEscKey)
        {
            if (window == null)
            {
                throw new ArgumentNullException("window");
            }
            _window = window;
            _window.Closing += Window_Closing;
            _window.Deactivated += Window_Deactivated;
            if (closeOnEscKey)
            {
                _window.PreviewKeyUp += _window_PreviewKeyUp;
            }
        }

        private void _window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _window.Close();
                e.Handled = true;
            }
        }

        private bool _isClosing = false;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _isClosing = true;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (_isClosing)
            {
                return;
            }
            Dispatcher.CurrentDispatcher.InvokeAsync(_window.Close);
        }
    }
}
