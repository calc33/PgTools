using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// TableInfoControl.xaml の相互作用ロジック
    /// </summary>
    public partial class TableInfoControl : UserControl
    {
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(TableInfoControl));
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Table), typeof(TableInfoControl));
        public static readonly DependencyProperty DataGridColumnsMaxHeightProperty = DependencyProperty.Register("DataGridColumnsMaxHeight", typeof(double), typeof(TableInfoControl));

        public bool IsEditing
        {
            get
            {
                return (bool)GetValue(IsEditingProperty);
            }
            set
            {
                SetValue(IsEditingProperty, value);
            }
        }

        public Table Target
        {
            get
            {
                return (Table)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }

        public double DataGridColumnsMaxHeight
        {
            get
            {
                return (double)GetValue(DataGridColumnsMaxHeightProperty);
            }
            set
            {
                SetValue(DataGridColumnsMaxHeightProperty, value);
            }
        }

        private void UpdateDataGridColumns()
        {
            bool? flg = checkBoxShowHidden.IsChecked;
            if (flg.HasValue && flg.Value)
            {
                dataGridColumns.ItemsSource = Target.Columns.AllColumns;
            }
            else
            {
                dataGridColumns.ItemsSource = Target.Columns;
            }
        }

        private void UpdateDataGridIndexes()
        {
            dataGridIndexes.ItemsSource = null;
            dataGridIndexes.ItemsSource = Target.Indexes;
        }

        private WeakReference<SearchDataGridWindow> _searchWindowDataGridColumns = null;
        private SearchDataGridWindow RequireSearchWindowDataGridColumns()
        {
            SearchDataGridWindow win;
            if (_searchWindowDataGridColumns != null && _searchWindowDataGridColumns.TryGetTarget(out win))
            {
                return win;
            }
            win = new SearchDataGridWindow();
            win.Target = dataGridColumns;
            win.Owner = Window.GetWindow(this);
            win.Closed += SearchWindowDataGridColumns_Closed;
            _searchWindowDataGridColumns = new WeakReference<SearchDataGridWindow>(win);
            return win;
        }

        private void SearchWindowDataGridColumns_Closed(object sender, EventArgs e)
        {
            _searchWindowDataGridColumns = null;
        }

        private void FindCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchDataGridWindow win = RequireSearchWindowDataGridColumns();
            win.Show();
        }

        private void FindNextCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
        }

        private void FindPreviousCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void TargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateDataGridColumns();
            UpdateDataGridIndexes();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                TargetPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public TableInfoControl()
        {
            InitializeComponent();
        }

        private void buttonSearchSchema_Click(object sender, RoutedEventArgs e)
        {
            SearchDataGridWindow win = new SearchDataGridWindow();
            FrameworkElement elem = sender as FrameworkElement ?? dataGridColumns;
            WindowLocator.LocateNearby(elem, win, NearbyLocation.UpLeft);
            win.Owner = Window.GetWindow(this);
            App.CopyFont(win, win.Owner);
            win.Target = dataGridColumns;
            win.Show();
        }

        private void checkBoxShowHidden_Click(object sender, RoutedEventArgs e)
        {
            UpdateDataGridColumns();
        }


        private void buttonApplySchema_Click(object sender, RoutedEventArgs e)
        {
            Db2SourceContext ctx = Target.Context;
            List<string> sqls = new List<string>();
            if ((Target.Comment != null) && Target.Comment.IsModified)
            {
                sqls.AddRange(ctx.GetSQL(Target.Comment, string.Empty, string.Empty, 0, false));
            }
            for (int i = 0; i < Target.Columns.Count; i++)
            {
                Column newC = Target.Columns[i, false];
                if (newC.IsModified)
                {
                    Column oldC = Target.Columns[i, true];
                    sqls.AddRange(ctx.GetAlterColumnSQL(newC, oldC));
                }
                if ((newC.Comment != null) && newC.Comment.IsModified)
                {
                    sqls.AddRange(ctx.GetSQL(newC.Comment, string.Empty, string.Empty, 0, false));
                }
            }
            if (sqls.Count != 0)
            {
                ctx.ExecSqlsWithLog(sqls);
            }
            ctx.Revert(Target);
            IsEditing = false;
        }

        private void buttonRevertSchema_Click(object sender, RoutedEventArgs e)
        {
            Db2SourceContext ctx = Target.Context;
            ctx.Revert(Target);
            IsEditing = false;
        }

        private void dataGridColumns_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DataGrid g = sender as DataGrid;
            double w = e.NewSize.Width;
            double h = Math.Max(g.FontSize + 4.0, e.NewSize.Height / 2.0);
            DataGridColumnsMaxHeight = h;
            foreach (DataGridColumn c in g.Columns)
            {
                c.MaxWidth = w;
            }
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            CommandBinding b;
            b = new CommandBinding(ApplicationCommands.Find, FindCommand_Executed);
            dataGridColumns.CommandBindings.Add(b);
            b = new CommandBinding(SearchCommands.FindNext, FindNextCommand_Executed);
            dataGridColumns.CommandBindings.Add(b);
            b = new CommandBinding(SearchCommands.FindPrevious, FindPreviousCommand_Executed);
            dataGridColumns.CommandBindings.Add(b);
        }

        private void buttonOptions_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = Resources["dropTableContextMenu"] as ContextMenu;
            menu.PlacementTarget = buttonOptions;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private void menuItemDropTable_Click(object sender, RoutedEventArgs e)
        {
            Window owner = Window.GetWindow(this);
            MessageBoxResult ret = MessageBox.Show(owner, (string)Resources["messageDropTable"], Properties.Resources.MessageBoxCaption_Drop, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            MenuItem menu = sender as MenuItem;
            TableControl ctl = App.FindLogicalParent<TableControl>(this);
            ctl?.DropTarget((bool)menu.Tag);
        }
    }
}
