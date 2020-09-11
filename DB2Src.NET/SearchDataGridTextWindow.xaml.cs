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
    /// SearchTextWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SearchDataGridTextWindow: Window
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(DataGridController), typeof(SearchDataGridTextWindow));
        //private DataGridController _target;
        //private int _startRow = -1;
        //private int _startCol = -1;
        private DataGridColumn _selectedColumn = null;

        private void UpdateComboBoxColumn()
        {
            DataGridColumn old = comboBoxColumn.SelectedItem as DataGridColumn;
            DataGridController obj = Target;
            List<DataGridColumn> l = new List<DataGridColumn>();
            if (obj != null)
            {
                l = new List<DataGridColumn>(obj.GetDataGridColumnsByDisplayIndex());
            }
            comboBoxColumn.ItemsSource = l;
            if (old == null || l.IndexOf(old) == -1)
            {
                comboBoxColumn.SelectedItem = obj.GetSelectedCell()?.Column;
            }
            else
            {
                comboBoxColumn.SelectedItem = old;
            }
            CommandBinding b;
            b = new CommandBinding(SearchCommands.FindNext, FindNextCommand_Executed);
            CommandBindings.Add(b);
            b = new CommandBinding(SearchCommands.FindPrevious, FindPreviousCommand_Executed);
            CommandBindings.Add(b);
        }
        private void InitContent()
        {
            UpdateComboBoxColumn();
            //comboBoxKeyword.SetBinding(ComboBox.TextProperty, new Binding("Target.SearchText") { ElementName = "window", Mode = BindingMode.TwoWay });
            //checkBoxCaseful.SetBinding(CheckBox.IsCheckedProperty, new Binding("Target.IgnoreCase") { ElementName = "window", Converter = new InvertBooleanConverter(), Mode = BindingMode.TwoWay });
            //checkBoxWordwrap.SetBinding(CheckBox.IsCheckedProperty, new Binding("Target.Wordwrap") { ElementName = "window", Mode = BindingMode.TwoWay });
            //checkBoxRegex.SetBinding(CheckBox.IsCheckedProperty, new Binding("Target.UseRegex") { ElementName = "window", Mode = BindingMode.TwoWay });
            //checkBoxColumn.SetBinding(CheckBox.IsCheckedProperty, new Binding("Target.UseSearchColumn") { ElementName = "window", Mode = BindingMode.TwoWay });
            //comboBoxColumn.SetBinding(ComboBox.SelectedItemProperty, new Binding("Target.SearchColumn") { ElementName = "window", Mode = BindingMode.TwoWay });
        }

        public DataGridController Target
        {
            get
            {
                return (DataGridController)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }

        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateComboBoxColumn();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                OnTargetPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public SearchDataGridTextWindow()
        {
            InitializeComponent();
        }


        private void FindNextCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGridController obj = Target;
            if (obj == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(comboBoxKeyword.Text))
            {
                return;
            }
            if (checkBoxColumn.IsChecked.HasValue && checkBoxColumn.IsChecked.Value)
            {
                _selectedColumn = comboBoxColumn.SelectedItem as DataGridColumn;
            }
            obj.SearchGridTextForward();
        }

        private void FindPreviousCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGridController obj = Target;
            if (obj == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(comboBoxKeyword.Text))
            {
                return;
            }
            obj.SearchGridTextBackward();
        }

        private void buttonNext_Click(object sender, RoutedEventArgs e)
        {
            SearchCommands.FindNext.Execute(null, this);
        }

        private void buttonPrevious_Click(object sender, RoutedEventArgs e)
        {
            SearchCommands.FindPrevious.Execute(null, this);
        }

        private void Window_LayoutUpdated(object sender, EventArgs e)
        {
            
        }
    }
}
