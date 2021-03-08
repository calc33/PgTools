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
    /// BasicTypeControl.xaml の相互作用ロジック
    /// </summary>
    public partial class BasicTypeControl : UserControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(BasicType), typeof(BasicTypeControl));

        public BasicType Target
        {
            get
            {
                return (BasicType)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }

        public BasicTypeControl()
        {
            InitializeComponent();
        }

        private void buttonApplySchema_Click(object sender, RoutedEventArgs e)
        {

        }

        private void buttonRevertSchema_Click(object sender, RoutedEventArgs e)
        {

        }

        private void checkBoxSource_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void checkBoxSource_Unhecked(object sender, RoutedEventArgs e)
        {

        }
    }
}
