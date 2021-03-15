using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// ComplexTypeControl.xaml の相互作用ロジック
    /// </summary>
    public partial class ComplexTypeControl: UserControl, ISchemaObjectWpfControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(ComplexType), typeof(ComplexTypeControl));
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(ComplexTypeControl));

        public ComplexType Target
        {
            get
            {
                return (ComplexType)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }
        SchemaObject ISchemaObjectControl.Target
        {
            get
            {
                return (SchemaObject)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }
        public string SelectedTabKey
        {
            get
            {
                return (tabControlMain.SelectedItem as TabItem)?.Header?.ToString();
            }
            set
            {
                foreach (TabItem item in tabControlMain.Items)
                {
                    if (item.Header.ToString() == value)
                    {
                        tabControlMain.SelectedItem = item;
                        break;
                    }
                }
            }
        }

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

        public ComplexTypeControl()
        {
            InitializeComponent();
        }

        private void UpdateDataGridColumns()
        {
            dataGridColumns.ItemsSource = Target.Columns.AllColumns;
        }
        private void TargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            //dataGridColumns.ItemsSource = Target.Columns;
            UpdateDataGridColumns();
            UpdateTextBoxSource();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                TargetPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        private static bool IsChecked(CheckBox checkBox)
        {
            return checkBox.IsChecked.HasValue && checkBox.IsChecked.Value;
        }
        private void UpdateTextBoxSource()
        {
            if (textBoxSource == null)
            {
                return;
            }
            if (Target == null)
            {
                textBoxSource.Text = string.Empty;
                return;
            }
            Db2SourceContext ctx = Target.Context;
            try
            {
                StringBuilder buf = new StringBuilder();
                if (IsChecked(checkBoxSourceMain))
                {
                    foreach (string s in ctx.GetSQL(Target, string.Empty, ";", 0, true))
                    {
                        buf.AppendLine(s);
                    }
                }
                int lastLength = buf.Length;
                if (IsChecked(checkBoxSourceComment))
                {
                    lastLength = buf.Length;
                    if (!string.IsNullOrEmpty(Target.CommentText))
                    {
                        foreach (string s in ctx.GetSQL(Target.Comment, string.Empty, ";", 0, true))
                        {
                            buf.Append(s);
                        }
                    }
                    foreach (Column c in Target.Columns)
                    {
                        if (!string.IsNullOrEmpty(c.CommentText))
                        {
                            foreach (string s in ctx.GetSQL(c.Comment, string.Empty, ";", 0, true))
                            {
                                buf.Append(s);
                            }
                        }
                    }
                    if (lastLength < buf.Length)
                    {
                        buf.AppendLine();
                    }
                }
                textBoxSource.Text = buf.ToString();
            }
            catch (Exception t)
            {
                textBoxSource.Text = t.ToString();
            }
        }

        private void checkBoxSource_Checked(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxSource();
        }

        private void checkBoxSource_Unhecked(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxSource();
        }

        private void buttonApplySchema_Click(object sender, RoutedEventArgs e)
        {
            Db2SourceContext ctx = Target.Context;
            List<string> sqls = new List<string>();
            if ((Target.Comment != null) && Target.Comment.IsModified())
            {
                sqls.AddRange(ctx.GetSQL(Target.Comment, string.Empty, string.Empty, 0, false));
            }
            for (int i = 0; i < Target.Columns.Count; i++)
            {
                Column newC = Target.Columns[i, false];
                if (newC.IsModified())
                {
                    Column oldC = Target.Columns[i, true];
                    sqls.AddRange(ctx.GetAlterColumnSQL(newC, oldC));
                }
                if ((newC.Comment != null) && newC.Comment.IsModified())
                {
                    sqls.AddRange(ctx.GetSQL(newC.Comment, string.Empty, string.Empty, 0, false));
                }
            }
            try
            {
                if (sqls.Count != 0)
                {
                    ctx.ExecSqlsWithLog(sqls);
                }
            }
            finally
            {
                ctx.Revert(Target);
            }
            IsEditing = false;
        }

        private void buttonRevertSchema_Click(object sender, RoutedEventArgs e)
        {
            Db2SourceContext ctx = Target.Context;
            ctx.Revert(Target);
            IsEditing = false;
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
        }

        private void checkBoxShowHidden_Click(object sender, RoutedEventArgs e)
        {
            UpdateDataGridColumns();
        }

        public void OnTabClosing(object sender, ref bool cancel)
        {
        }

        public void OnTabClosed(object sender) { }

        private void buttonSearchSchema_Click(object sender, RoutedEventArgs e)
        {
            SearchDataGridWindow win = new SearchDataGridWindow();
            FrameworkElement elem = sender as FrameworkElement ?? dataGridColumns;
            WindowLocator.LocateNearby(elem, win, NearbyLocation.UpLeft);
            win.Owner = Window.GetWindow(this);
            win.Target = dataGridColumns;
            win.Show();
        }
    }
}
