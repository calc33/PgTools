using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Db2Source
{
    public static class SessionCommands
    {
        public static RoutedCommand Kill = new RoutedCommand(Properties.Resources.KillCommand_Name, typeof(FrameworkElement));
        public static RoutedCommand AbortQuery = new RoutedCommand(Properties.Resources.AbortQueryCommand_Name, typeof(FrameworkElement));
    }
}
