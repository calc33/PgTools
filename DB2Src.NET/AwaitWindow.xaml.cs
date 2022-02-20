using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// AwaitWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AwaitWindow: Window
    {
        private DispatcherTimer _timer;
        public Task AwaitTask { get; set; }
        public AwaitWindow()
        {
            InitializeComponent();
        }

        private void StopTimer()
        {
            _timer.Stop();
            _timer = null;
            Dispatcher.InvokeAsync(Close);
        }
        private void CheckTask(object sender, EventArgs e)
        {
            if (AwaitTask == null)
            {
                return;
            }
            if (!AwaitTask.IsCompleted)
            {
                return;
            }
            Task t = AwaitTask;
            AwaitTask = null;
            if (t.IsFaulted)
            {
                App.HandleThreadException(t.Exception);
            }
            StopTimer();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _timer = new DispatcherTimer(new TimeSpan(1000000), DispatcherPriority.Normal, CheckTask, Dispatcher);
        }
        public void WaitTask(Task task)
        {
            AwaitTask = task;
            ShowDialog();
        }
    }
}
