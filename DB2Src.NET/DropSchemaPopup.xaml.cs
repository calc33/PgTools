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

namespace Db2Source
{
    /// <summary>
    /// DropSchemaPopup.xaml の相互作用ロジック
    /// </summary>
    public partial class DropSchemaPopup : Window
    {
        public event EventHandler<EventArgs> SchemaDrop;
        public DropSchemaPopup()
        {
            InitializeComponent();
        }
        private bool _closing = false;
        private void window_Deactivated(object sender, EventArgs e)
        {
            if (!_closing)
            {
                Close();
            }
        }

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _closing = true;
        }

        private void buttonDrop_Click(object sender, RoutedEventArgs e)
        {
            SchemaDrop?.Invoke(this, EventArgs.Empty);
        }
    }
}
