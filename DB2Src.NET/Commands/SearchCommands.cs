using System.Windows;
using System.Windows.Input;

namespace Db2Source
{
    public static class SearchCommands
    {
        public static RoutedCommand FindNext = new RoutedCommand(Properties.Resources.FindNextCommand_Name, typeof(FrameworkElement), new InputGestureCollection(new KeyGesture[] { new KeyGesture(Key.F3) }));
        public static RoutedCommand FindPrevious = new RoutedCommand(Properties.Resources.FindPreviousCommand_Name, typeof(FrameworkElement), new InputGestureCollection(new KeyGesture[] { new KeyGesture(Key.F3, ModifierKeys.Shift) }));
    }
}
