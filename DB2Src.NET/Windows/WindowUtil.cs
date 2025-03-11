using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WinDrawing = System.Drawing;
using WinForm = System.Windows.Forms;

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
        /// <summary>
        /// 上に重ねる
        /// </summary>
        Overlap,
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

        private static WinDrawing.Rectangle GetMaxRect()
        {
            WinDrawing.Rectangle ret = WinDrawing.Rectangle.Empty;
            foreach (WinForm.Screen screen in WinForm.Screen.AllScreens)
            {
                var r = screen.WorkingArea;
                WinDrawing.Point p = new WinDrawing.Point(Math.Min(ret.X, r.X), Math.Min(ret.Y, r.Y));
                WinDrawing.Size s = new WinDrawing.Size(Math.Max(ret.Right, r.Right) - p.X, Math.Max(ret.Bottom, r.Bottom) - p.Y);
                ret = new WinDrawing.Rectangle(p, s);
            }
            return ret;
        }
        private static Size GetMaxWindowSize(Window window)
        {
            WinDrawing.Rectangle rect = GetMaxRect();
            Point p1 = window.PointFromScreen(new Point(rect.Left, rect.Top));
            Point p2 = window.PointFromScreen(new Point(rect.Right, rect.Bottom));
            WinDrawing.Size s = SystemMetrics.SizeFrame;
            return new Size(Math.Abs(p2.X - p1.X) + s.Width * 2, Math.Abs(p2.Y - p1.Y) + s.Height * 2);
        }

        public static void AdjustMaxSizeToScreen(Window window)
        {
            if (!window.IsVisible)
            {
                return;
            }
            Size s = GetMaxWindowSize(window);
            window.MaxWidth = s.Width;
            window.MaxHeight = s.Height;
        }

        public static Size GetSizeOfCurrentScreen(Window window)
        {
            Rect rect = GetWorkingAreaOf(window);
            Point p1 = window.PointFromScreen(new Point(rect.Left, rect.Top));
            Point p2 = window.PointFromScreen(new Point(rect.Right, rect.Bottom));
            WinDrawing.Size s = SystemMetrics.SizeFrame;
            return new Size(Math.Abs(p2.X - p1.X) + s.Width * 2, Math.Abs(p2.Y - p1.Y) + s.Height * 2);
        }
        public static void AdjustMaxSizeToCurrentScreen(Window window)
        {
            if (!window.IsVisible)
            {
                return;
            }
            Size s = GetSizeOfCurrentScreen(window);
            window.MaxWidth = s.Width;
            window.MaxHeight = s.Height;
        }

        public static void LocateIntoCurrentScreen(Window window)
        {
            Point p = window.PointToScreen(new Point(window.Width / 2, window.Height / 2));
            WinForm.Screen screen = WinForm.Screen.FromPoint(new WinDrawing.Point((int)p.X, (int)p.Y));
            Point p1 = new Point(screen.WorkingArea.Left, screen.WorkingArea.Top);
            Point p2 = new Point(screen.WorkingArea.Right, screen.WorkingArea.Bottom);
            window.Left = Math.Max(Math.Min(window.Left, p2.X - window.ActualWidth), p1.X);
            window.Top = Math.Max(Math.Min(window.Top, p2.Y - window.ActualHeight), p1.Y);
        }

        public static void AdjustMaxHeightToScreen(Window window)
        {
            if (!window.IsVisible)
            {
                return;
            }
            Size s = GetMaxWindowSize(window);
            window.MaxHeight = s.Height;
            LocateIntoCurrentScreen(window);
        }

        public static Rect GetWorkingAreaOf(FrameworkElement element)
        {
            Point p = element.PointToScreen(new Point(element.ActualWidth / 2, element.ActualHeight / 2));
            WinForm.Screen sc = WinForm.Screen.FromPoint(new WinDrawing.Point((int)p.X, (int)p.Y));
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
            { NearbyLocation.Overlap, new NearbyLocation[0] },
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
            _window.Loaded += Window_Loaded;
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
            _window.Loaded += Window_Loaded;
            Rect r = GetWindowPlacement(false);
            _window.WindowStartupLocation = WindowStartupLocation.Manual;
            _window.Left = r.X;
            _window.Top = r.Y;
        }

        private void Dispose()
        {
            _window.LayoutUpdated -= Window_LayoutUpdated;
            _window.Loaded -= Window_Loaded;
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
                case NearbyLocation.Overlap:
                    return new Rect(_placement.TopLeft, windowRect.Size);
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
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // ウィンドウの初期化処理が終わったらWindowLocatorは解放
            _window.Dispatcher.InvokeAsync(Dispose, DispatcherPriority.ApplicationIdle);
        }

        //private static readonly Thickness ResizeDelta = new Thickness(10, 0, 15, 0);
    }

    /// <summary>
    /// 継続的に送られてくるイベントが途切れたら処理を実行するクラス
    /// </summary>
    public class AggregatedEventDispatcher
    {
        private Dispatcher _dispatcher;
        private TimeSpan _interval;
        private DispatcherTimer _timer;
        private DateTime _scheduled;
        private Action _action;

        public AggregatedEventDispatcher(Dispatcher dispatcher, Action action, TimeSpan interval)
        {
			_dispatcher = dispatcher;
			_timer = null;
			_interval = interval;
            _action = action;
        }

        internal void Touch()
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 200), DispatcherPriority.Normal, DispatcherTimer_Tick, _dispatcher);
            }
            _timer.Start();
            _scheduled = DateTime.Now + _interval;
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (_scheduled < DateTime.Now)
            {
                return;
            }
            _timer.Stop();
            _action?.Invoke();
        }
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
                Dispatcher.CurrentDispatcher.InvokeAsync(_window.Close);
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
