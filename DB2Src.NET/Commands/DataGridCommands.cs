using System.Windows.Controls;
using System.Windows.Input;

namespace Db2Source
{
    public static class DataGridCommands
    {
        public static readonly RoutedCommand SelectAllCells = InitSelectAllCells();
        public static readonly RoutedCommand CopyTable = InitCopyTable();
        public static readonly RoutedCommand CopyTableContent = InitCopyTableContent();
        public static readonly RoutedCommand CopyTableAsInsert = InitCopyTableAsInsert();
        public static readonly RoutedCommand CopyTableAsUpdate = InitCopyTableAsUpdate();
        public static readonly RoutedCommand CopyTableAsCopy = InitCopyTableAsCopy();
        public static readonly RoutedCommand CheckAll = InitCheckAll();
        public static readonly RoutedCommand UncheckAll = InitUncheckAll();

        private static RoutedCommand InitSelectAllCells()
        {
            RoutedCommand ret = new RoutedCommand("表全体を選択", typeof(DataGrid));
            ret.InputGestures.Add(new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift));
            return ret;
        }
        private static RoutedCommand InitCopyTable()
        {
            RoutedCommand ret = new RoutedCommand("表をコピー", typeof(DataGrid));
            ret.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
            return ret;
        }
        private static RoutedCommand InitCopyTableContent()
        {
            RoutedCommand ret = new RoutedCommand("表をコピー(データのみ)", typeof(DataGrid));
            ret.InputGestures.Add(new KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Shift));
            return ret;
        }
        private static RoutedCommand InitCopyTableAsInsert()
        {
            RoutedCommand ret = new RoutedCommand("表をINSERT文形式でコピー", typeof(DataGrid));
            ret.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control | ModifierKeys.Shift));
            return ret;
        }
        private static RoutedCommand InitCopyTableAsUpdate()
        {
            RoutedCommand ret = new RoutedCommand("表をUPDATE文形式でコピー", typeof(DataGrid));
            ret.InputGestures.Add(new KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Shift));
            return ret;
        }
        private static RoutedCommand InitCopyTableAsCopy()
        {
            RoutedCommand ret = new RoutedCommand("表をCOPY文形式でコピー", typeof(DataGrid));
            ret.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
            return ret;
        }
        private static RoutedCommand InitCheckAll()
        {
            RoutedCommand ret = new RoutedCommand("すべてチェック", typeof(DataGrid));
            return ret;
        }
        private static RoutedCommand InitUncheckAll()
        {
            RoutedCommand ret = new RoutedCommand("すべてチェックをはずす", typeof(DataGrid));
            return ret;
        }
    }
}
