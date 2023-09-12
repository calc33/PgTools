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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Db2Source
{
    /// <summary>
    /// IndentSettingsControl.xaml の相互作用ロジック
    /// </summary>
    public partial class IndentSettingsControl : UserControl
    {
        public static readonly DependencyProperty IndentOffsetVisibilityProperty = DependencyProperty.Register("IndentOffsetVisibility", typeof(Visibility), typeof(IndentSettingsControl), new PropertyMetadata(Visibility.Visible));

        public Visibility IndentOffsetVisibility
        {
            get { return (Visibility)GetValue(IndentOffsetVisibilityProperty); }
            set { SetValue(IndentOffsetVisibilityProperty, value); }
        }

        public IndentSettingsControl()
        {
            InitializeComponent();
        }
    }
}
