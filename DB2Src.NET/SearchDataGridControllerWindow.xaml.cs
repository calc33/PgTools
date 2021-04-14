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
    public partial class SearchDataGridControllerWindow: Window
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(DataGridController), typeof(SearchDataGridControllerWindow));
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
                l = new List<DataGridColumn>(obj.GetDisplayDataGridColumns());
            }
            comboBoxColumn.ItemsSource = l;
            if (old == null || l.IndexOf(old) == -1)
            {
                comboBoxColumn.SelectedItem = obj.GetSelectedCell(true)?.Column;
            }
            else
            {
                comboBoxColumn.SelectedItem = old;
            }
        }
        private void InitContent()
        {
            UpdateComboBoxColumn();
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

        public SearchDataGridControllerWindow()
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
            bool found = obj.SearchGridTextForward();
            textBlockNotFound.Visibility = found ? Visibility.Hidden : Visibility.Visible;
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
            bool found = obj.SearchGridTextBackward();
            textBlockNotFound.Visibility = found ? Visibility.Hidden : Visibility.Visible;
        }

        private void buttonNext_Click(object sender, RoutedEventArgs e)
        {
            SearchCommands.FindNext.Execute(null, this);
        }

        private void buttonPrevious_Click(object sender, RoutedEventArgs e)
        {
            SearchCommands.FindPrevious.Execute(null, this);
        }

        private void window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                    {
                        SearchCommands.FindPrevious.Execute(null, this);
                        e.Handled = true;
                        return;
                    }
                    if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
                    {
                        SearchCommands.FindNext.Execute(null, this);
                        e.Handled = true;
                        return;
                    }
                    return;
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    return;
            }
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            CommandBinding b;
            b = new CommandBinding(SearchCommands.FindNext, FindNextCommand_Executed);
            CommandBindings.Add(b);
            b = new CommandBinding(SearchCommands.FindPrevious, FindPreviousCommand_Executed);
            CommandBindings.Add(b);
            comboBoxKeyword.Focus();
        }

        private void comboBoxColumn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            checkBoxColumn.IsChecked = (comboBoxColumn.SelectedIndex != -1);
        }
    }
}
