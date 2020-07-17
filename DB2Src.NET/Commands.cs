using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Db2Source
{
    public static class QueryCommands
    {
        public static readonly RoutedCommand NormalizeSQL = InitNormalizeSQL();
        private static RoutedCommand InitNormalizeSQL()
        {
            RoutedCommand ret = new RoutedCommand("クエリを正規化", typeof(TextBox));
            //ret.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
            return ret;
        }
    }
}
