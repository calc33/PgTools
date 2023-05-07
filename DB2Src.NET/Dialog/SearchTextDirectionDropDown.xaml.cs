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
    /// SearchTextDirectionDropDown.xaml の相互作用ロジック
    /// </summary>
    public partial class SearchTextDirectionDropDown: Window
    {
        private Button _target;
        public Button Target
        {
            get { return _target; }
            set
            {
                _target = value;
                if (_target != null)
                {
                    FontFamily = _target.FontFamily;
                    FontSize = _target.FontSize;
                    FontStretch = _target.FontStretch;
                    FontStyle = _target.FontStyle;
                    FontWeight = _target.FontWeight;
                    WindowLocator.LocateNearby(_target, this, NearbyLocation.DownLeft);
                }
            }
        }
        public SearchTextDirectionDropDown()
        {
            InitializeComponent();
            new CloseOnDeactiveWindowHelper(this, true);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Target == null)
            {
                return;
            }
            Button b = sender as Button;
            Target.Tag = b.Tag;
            Target.RenderTransform = b.RenderTransform?.Clone();
            Dispatcher.InvokeAsync(Close, DispatcherPriority.ApplicationIdle);
        }
    }
}
