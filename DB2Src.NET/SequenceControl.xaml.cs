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
		public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Sequence), typeof(SequenceControl), new PropertyMetadata(new PropertyChangedCallback(OnTargetPropertyChanged)));
		public static readonly DependencyProperty CurrentValueProperty = DependencyProperty.Register("CurrentValue", typeof(long?), typeof(SequenceControl), new PropertyMetadata(new PropertyChangedCallback(OnCurrentValuePropertyChanged)));
		public static readonly DependencyProperty OwnedColumnValueProperty = DependencyProperty.Register("OwnedColumnValue", typeof(long?), typeof(SequenceControl), new PropertyMetadata(new PropertyChangedCallback(OnOwnedColumnValuePropertyChanged)));
		public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(SequenceControl));
        public static readonly DependencyProperty OrdinalDescriptionProperty = DependencyProperty.Register("OrdinalDescription", typeof(string), typeof(SequenceControl), new PropertyMetadata(new PropertyChangedCallback(OnOrdinalDescriptionChanged)));


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

        private static readonly string[] _settingControlNames = new string[] { "checkBoxDrop", "checkBoxSourceMain", "checkBoxSourceComment" };
        public string[] SettingCheckBoxNames { get { return _settingControlNames; } }

        public long? CurrentValue
        {
            get { return (long?)GetValue(CurrentValueProperty); }
            set { SetValue(CurrentValueProperty, value); }
        }

        public long? OwnedColumnValue
        {
            get { return (long?)GetValue(OwnedColumnValueProperty); }
            set { SetValue(OwnedColumnValueProperty, value); }
        }

        public string OrdinalDescription
        {
            get
            {
                return (string)GetValue(OrdinalDescriptionProperty);
            }
        }

        private void UpdateOrdinalDescription()
        {
            bool isAsc = Target == null || 0 < Target.Increment;
			SetValue(OrdinalDescriptionProperty, (string)Resources[isAsc ? "textMaximum" : "textMinimum"]);
            buttonResetSequence.ToolTip = (string)Resources[isAsc ? "buttonResetSequenceTooltipMaximum" : "buttonResetSequenceTooltipMinimum"];
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

        private void UpdateCurrentValue()
        {
            Db2SourceContext ctx = Target?.Context;
            if (ctx == null)
            {
                return;
            }
            using (var conn = ctx.NewConnection(true))
            {
                CurrentValue = ctx.GetCurrentSequenceValue(Target, conn);
                if (Target.HasOwnedColumn)
                {
					Column c = Target.OwnedColumn;
					OwnedColumnValue = 0 < Target.Increment ? ctx.GetMaxValueOfColumn(c, conn) : ctx.GetMinValueOfColumn(c, conn);
				}
                else
                {
                    OwnedColumnValue = null;
                }
			}
		}

        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateTextBoxSource();
            UpdateCurrentValue();
            UpdateOrdinalDescription();
		}

        private static void OnTargetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as SequenceControl)?.OnTargetPropertyChanged(e);
        }

		private void OnCurrentValuePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		private static void OnCurrentValuePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
		{
			(target as SequenceControl)?.OnCurrentValuePropertyChanged(e);
		}

		private void OnOwnedColumnValuePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		private static void OnOwnedColumnValuePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
		{
			(target as SequenceControl)?.OnOwnedColumnValuePropertyChanged(e);
		}

		private void OnOrdinalDescriptionChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		private static void OnOrdinalDescriptionChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
		{
			(target as SequenceControl)?.OnOrdinalDescriptionChanged(e);
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
                if (IsChecked(checkBoxDrop))
                {
                    foreach (string s in ctx.GetDropSQL(Target, true, string.Empty, ";", 0, false, true))
                    {
                        buf.Append(s);
                    }
                    buf.AppendLine();
                }
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

        private void checkBoxSource_Unchecked(object sender, RoutedEventArgs e)
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

        public void Dispose()
        {
            BindingOperations.ClearAllBindings(this);
        }

        public void OnTabClosed(object sender)
        {
            Dispose();
        }

        private void buttonOptions_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu;
            menu = (ContextMenu)FindResource("dropSequenceContextMenu");
            menu.PlacementTarget = buttonOptions;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;

        }

        private void menuItemDropProcedue_Click(object sender, RoutedEventArgs e)
        {
            Window owner = Window.GetWindow(this);
            MessageBoxResult ret = MessageBox.Show(owner, (string)FindResource("messageDropSequence"), Properties.Resources.MessageBoxCaption_Drop, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            Db2SourceContext ctx = Target.Context;
            string[] sql = ctx.GetDropSQL(Target, true, string.Empty, string.Empty, 0, false, false);
            StringBuilderLogger logger = new StringBuilderLogger();
            bool failed = false;
            try
            {
                ctx.ExecSqls(sql, logger.Log);
            }
            catch (Exception t)
            {
                logger.LogException(t, ctx);
                failed = true;
            }
            logger.ShowLogByMessageBox(owner, failed);
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

        private void buttonRefreshSchema_Click(object sender, RoutedEventArgs e)
        {
            Target?.Context?.Refresh(Target);
        }

        private void buttonResetSequence_Click(object sender, RoutedEventArgs e)
        {
            if (Target == null)
            {
                return;
            }
            if (!Target.HasOwnedColumn)
            {
                return;
            }
            Db2SourceContext ctx = Target.Context;
            if (ctx == null)
            {
                return;
            }

            Column c = Target.OwnedColumn;
            UpdateCurrentValue();
            long? vCur = CurrentValue;
            long? vNew = OwnedColumnValue;
            if (!vNew.HasValue)
            {
                MessageBox.Show((string)Resources["messageEmptyOwnedColumn"], Properties.Resources.MessageBoxCaption_Info, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
			string msg = string.Format((string)Resources["messageUpdateSequenceByOwnedColumnFmt"], OrdinalDescription);
			MessageBoxResult ret = MessageBox.Show(msg, Properties.Resources.MessageBoxCaption_Confirm, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
			if (ret != MessageBoxResult.OK)
			{
				return;
			}
			bool isOK = vCur.HasValue && (0 < Target.Increment ? vNew.Value <= vCur.Value : vCur.Value <= vNew.Value);
            if (isOK)
            {
                ret = MessageBox.Show(string.Format((string)Resources["messageConfirmUpdateSequenceFmt"], vCur, vNew),
					Properties.Resources.MessageBoxCaption_Confirm, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (ret != MessageBoxResult.Yes)
                {
                    return;
                }
                UpdateCurrentValue();
                vNew = OwnedColumnValue;
            }
            using (var conn = ctx.NewConnection(true))
            {
                ctx.SetSequenceValue(vNew.Value, Target, conn);
            }
            UpdateCurrentValue();
        }
		private void buttonRefreshCurrentValue_Click(object sender, RoutedEventArgs e)
		{
            UpdateCurrentValue();
		}
	}
}
