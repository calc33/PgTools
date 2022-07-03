using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// CrossGrid.xaml の相互作用ロジック
    /// </summary>
    public partial class CrossGrid : UserControl
    {
        public static readonly DependencyProperty RowAxisesProperty = DependencyProperty.Register("RowAxises", typeof(AxisCollection), typeof(CrossGrid));
        public static readonly DependencyProperty ColumnAxisesProperty = DependencyProperty.Register("ColumnAxises", typeof(AxisCollection), typeof(CrossGrid));
        public static readonly DependencyProperty FilterAxisesProperty = DependencyProperty.Register("FilterAxises", typeof(AxisCollection), typeof(CrossGrid));
        //public static readonly DependencyProperty AnimationAxisesProperty = DependencyProperty.Register("AnimationAxises", typeof(AxisCollection), typeof(CrossGrid));
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(CrossTable), typeof(CrossGrid));

        private CrossTable _table;

        private List<AxisEntry> _rowAxisEntries = new List<AxisEntry>();
        private List<AxisEntry> _columnAxisEntries = new List<AxisEntry>();

        public AxisCollection RowAxises
        {
            get { return (AxisCollection)GetValue(RowAxisesProperty); }
            set { SetValue(RowAxisesProperty, value); }
        }

        public AxisCollection ColumnAxises
        {
            get { return (AxisCollection)GetValue(ColumnAxisesProperty); }
            set { SetValue(ColumnAxisesProperty, value); }
        }

        public AxisCollection FilterAxises
        {
            get { return (AxisCollection)GetValue(FilterAxisesProperty); }
            set { SetValue(FilterAxisesProperty, value); }
        }

        public CrossTable ItemsSource
        {
            get { return (CrossTable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public CrossGrid()
        {
            InitializeComponent();
            _table = new CrossTable();
            BindingOperations.SetBinding(_table, CrossTable.ItemsSourceProperty, new Binding("ItemsSource") { Source = this });
        }

        private void UpdateRowAxisEntries()
        {
            List<AxisEntry> l = new List<AxisEntry>();

            _rowAxisEntries = l;

        }

        private bool _isUpdateControlsPosted = false;
        private void UpdateControls()
        {
            try
            {
                if (ItemsSource == null)
                {
                    return;
                }
                ItemsSource.Axises.Clear();
                foreach (Axis axis in RowAxises)
                {
                    ItemsSource.Axises.Add(axis);
                }
                foreach (Axis axis in ColumnAxises)
                {
                    ItemsSource.Axises.Add(axis);
                }
                foreach (Axis axis in FilterAxises)
                {
                    ItemsSource.Axises.Add(axis);
                }
            }
            finally
            {
                _isUpdateControlsPosted = false;
            }
        }

        private void DelayedUpdateControls()
        {
            if (_isUpdateControlsPosted)
            {
                return;
            }
            _isUpdateControlsPosted = true;
            Dispatcher.InvokeAsync(UpdateControls, DispatcherPriority.Normal);
        }

        private void ItemsSourcePropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DelayedUpdateControls();
        }
        private void RowAxisesPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DelayedUpdateControls();
        }
        private void ColumnAxisesPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DelayedUpdateControls();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ItemsSourceProperty)
            {
                ItemsSourcePropertyChanged(e);
            }
            if (e.Property == RowAxisesProperty)
            {
                RowAxisesPropertyChanged(e);
            }
            if (e.Property == ColumnAxisesProperty)
            {
                ColumnAxisesPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }
    }
}
