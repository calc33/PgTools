using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public static readonly DependencyProperty IsTargetModifiedProperty = DependencyProperty.Register("IsTargetModified", typeof(bool), typeof(SequenceControl));

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
        public bool IsTargetModified
        {
            get
            {
                return (bool)GetValue(IsTargetModifiedProperty);
            }
            set
            {
                SetValue(IsTargetModifiedProperty, value);
            }
        }

        private void UpdateIsTargetModified()
        {
            IsTargetModified = Target.IsModified();
        }
        private void TargetChanged(DependencyPropertyChangedEventArgs e)
        {
            Target.PropertyChanged += Target_PropertyChanged;
            Target.CommentChanged += Target_CommentChanged;
            UpdateTextBoxSource();
            UpdateIsTargetModified();
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
                    buf.Append(ctx.GetSQL(Target, string.Empty, ";", 0, true, true));
                }
                if (IsChecked(checkBoxSourceComment))
                {
                    if (!string.IsNullOrEmpty(Target.CommentText))
                    {
                        buf.Append(ctx.GetSQL(Target.Comment, string.Empty, ";", 0, true));
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
        private void Target_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateIsTargetModified();
        }
        private void Target_ColumnPropertyChanged(object sender, CollectionOperationEventArgs<Parameter> e)
        {
            UpdateIsTargetModified();
        }
        private void Target_CommentChanged(object sender, CommentChangedEventArgs e)
        {
            UpdateIsTargetModified();
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
    }
}
