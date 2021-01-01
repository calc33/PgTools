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
    /// DatabaseControl.xaml の相互作用ロジック
    /// </summary>
    public partial class DatabaseControl : UserControl
    {
        public DatabaseControl()
        {
            InitializeComponent();
        }
    }
    public class NameValue
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
