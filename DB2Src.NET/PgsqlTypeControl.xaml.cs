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
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Db2Source
{
    /// <summary>
    /// PgsqlTypeControl.xaml の相互作用ロジック
    /// </summary>
    public partial class PgsqlTypeControl : UserControl, ISchemaObjectWpfControl
    {
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(PgsqlTypeControl));
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Type_), typeof(PgsqlTypeControl), new PropertyMetadata(null, OnTargetPropertyChanged));

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

        public Type_ Target
        {
            get
            {
                return (Type_)GetValue(TargetProperty);
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

        private static readonly string[] _settingControlNames = new string[] { "checkBoxDrop", "checkBoxSourceMain", "checkBoxSourceComment" };
        public string[] SettingCheckBoxNames { get { return _settingControlNames; } }

        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateTextBoxSource();
        }

        private static void OnTargetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as PgsqlTypeControl)?.OnTargetPropertyChanged(e);
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
                if (IsChecked(checkBoxDrop))
                {
                    foreach (string s in ctx.GetDropSQL(Target, true, String.Empty, ";", 0, false, true))
                    {
                        buf.Append(s);
                    }
                    buf.AppendLine();
                }
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

        public PgsqlTypeControl()
        {
            InitializeComponent();
        }

        private void buttonApplySchema_Click(object sender, RoutedEventArgs e)
        {

        }

        private void buttonRevertSchema_Click(object sender, RoutedEventArgs e)
        {

        }

        private void checkBoxSource_Checked(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxSource();
        }

        private void checkBoxSource_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateTextBoxSource();
        }

        public void OnTabClosing(object sender, ref bool cancel) { }

        public void Dispose()
        {
            BindingOperations.ClearAllBindings(this);
        }

        public void OnTabClosed(object sender)
        {
            Dispose();
        }

        private void buttonRefreshSchema_Click(object sender, RoutedEventArgs e)
        {
            Target?.Context?.Refresh(Target);
        }
    }
}
