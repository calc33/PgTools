using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// SequenceControl.xaml の相互作用ロジック
    /// </summary>
    public partial class SequenceControl: UserControl, ISchemaObjectWpfControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Sequence), typeof(SequenceControl));
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(SequenceControl));

        public Sequence Target
        {
            get
            {
                return (Sequence)GetValue(TargetProperty);
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

        private void TargetChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateTextBoxSource();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                TargetChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public SequenceControl()
        {
            InitializeComponent();
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
                    foreach (string s in ctx.GetSQL(Target, string.Empty, ";", 0, true, false, false))
                    {
                        buf.Append(s);
                    }
                }
                if (IsChecked(checkBoxSourceComment))
                {
                    if (!string.IsNullOrEmpty(Target.CommentText))
                    {
                        foreach (string s in ctx.GetSQL(Target.Comment, string.Empty, ";", 0, true))
                        {
                            buf.Append(s);
                        }
                    }
                    buf.AppendLine();
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

        private void UserControl_Initialized(object sender, EventArgs e)
        {

        }

        private void buttonApplySchema_Click(object sender, RoutedEventArgs e)
        {
            Db2SourceContext ctx = Target.Context;
        }

        private void buttonRevertSchema_Click(object sender, RoutedEventArgs e)
        {
            //Db2SourceContext ctx = Target.Context;
            //ctx.Revert(Target);
        }

        public void OnTabClosing(object sender, ref bool cancel) { }

        public void OnTabClosed(object sender) { }

        private void buttonOptions_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu;
            menu = (ContextMenu)Resources["dropSequenceContextMenu"];
            menu.PlacementTarget = buttonOptions;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;

        }

        private void menuItemDropProcedue_Click(object sender, RoutedEventArgs e)
        {
            Window owner = App.FindVisualParent<Window>(this);
            MessageBoxResult ret = MessageBox.Show(owner, (string)Resources["messageDropSequence"], Properties.Resources.MessageBoxCaption_Drop, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            Db2SourceContext ctx = Target.Context;
            string[] sql = ctx.GetDropSQL(Target, string.Empty, string.Empty, 0, false, false);
            SqlLogger logger = new SqlLogger();
            bool failed = false;
            try
            {
                ctx.ExecSqls(sql, logger.Log);
            }
            catch (Exception t)
            {
                logger.Buffer.AppendLine(ctx.GetExceptionMessage(t));
                failed = true;
            }
            string s = logger.Buffer.ToString().TrimEnd();
            if (!string.IsNullOrEmpty(s))
            {
                if (failed)
                {
                    MessageBox.Show(owner, s, Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show(owner, s, Properties.Resources.MessageBoxCaption_Result, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            if (failed)
            {
                return;
            }
            TabItem tab = App.FindLogicalParent<TabItem>(this);
            if (tab != null)
            {
                (tab.Parent as TabControl).Items.Remove(tab);
                Target.Release();
                MainWindow.Current.FilterTreeView(true);
            }
        }
    }
}
