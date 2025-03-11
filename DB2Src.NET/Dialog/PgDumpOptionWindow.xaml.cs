using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using IO = System.IO;
using Microsoft.Win32;
using Db2Source.Windows;

namespace Db2Source
{
    /// <summary>
    /// PgDumpOptionWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class PgDumpOptionWindow : Window
    {
        private class ComboBoxPathItem
        {
            public string Text { get; set; }
            public string Path { get; set; }

            public ComboBoxPathItem() { }

            public ComboBoxPathItem(PgsqlInstallation installation, string exe)
            {
                Path = IO.Path.Combine(installation.BinDirectory, exe);
                Text = string.Format("{0}({1})", installation.Name, installation.BinDirectory);
            }

            public override string ToString()
            {
                return Text;
            }
        }
        private Db2SourceContext _dataSet;
        public Db2SourceContext DataSet
        {
            get
            {
                return _dataSet;
            }
            set
            {
                if (_dataSet == value)
                {
                    return;
                }
                _dataSet = value;
                DataSetChanged();
            }
        }

        public PgDumpOptionWindow()
        {
            InitializeComponent();
        }

        private void buttonSelectPath_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            PgDumpFormatOption opt = comboBoxFormat.SelectedItem as PgDumpFormatOption;
            if (opt == null)
            {
                opt = comboBoxFormat.Items[0] as PgDumpFormatOption;
            }
            dlg.DefaultExt = opt.DefaultExt;
            dlg.Filter = opt.DialogFilter;
            dlg.FileName = textBoxPath.Text;
            bool? b = dlg.ShowDialog(this);
            if (!b.HasValue || !b.Value)
            {
                return;
            }
            textBoxPath.Text = dlg.FileName;
        }
        private void buttonSelectDir_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog()
            {
                Title = (string)FindResource("FolderBrowserDialog_Title"),
                SelectedPath = textBoxDir.Text
            };
            DialogResult ret = dlg.ShowDialog(this);
            if (ret != Db2Source.DialogResult.OK)
            {
                return;
            }
            textBoxDir.Text = dlg.SelectedPath;
        }

        private static bool IsChecked(ToggleButton button)
        {
            bool? b = button.IsChecked;
            return b.HasValue && b.Value;
        }
        private static string[] SplitByWhiteSpace(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return StrUtil.EmptyStringArray;
            }
            int i = 0;
            int i0 = 0;
            int n = value.Length;
            List<string> l = new List<string>();
            while (i < n)
            {
                for (; i < n && char.IsWhiteSpace(value, i); i++) ;
                i0 = i;
                for (; i < n && !char.IsWhiteSpace(value, i); i++)
                {
                    if (char.IsSurrogate(value, i))
                    {
                        i++;
                    }
                }
                if (i0 < i)
                {
                    l.Add(value.Substring(i0, i - i0));
                }
                i0 = i;
            }
            if (i0 < i)
            {
                l.Add(value.Substring(i0, i - i0));
            }
            return l.ToArray();
        }

        private string _exportDir;
        private string _exportFile;
        private string GetCommandLineArgs()
        {
            StringBuilder buf = new StringBuilder();
            NpgsqlConnectionInfo info = DataSet.ConnectionInfo as NpgsqlConnectionInfo;
            buf.AppendFormat("-h {0} -p {1} -d {2} -U {3}", ShellUtil.TryQuote(info.ServerName), info.ServerPort, ShellUtil.TryQuote(info.DatabaseName), ShellUtil.TryQuote(info.UserName));
            string s = comboBoxEncoding.SelectedValue.ToString();
            if (!string.IsNullOrEmpty(s))
            {
                buf.Append(" -E ");
                buf.Append(s);
            }
            if (!string.IsNullOrEmpty(_exportFile))
            {
                buf.Append(" -f ");
                buf.Append(ShellUtil.TryQuote(_exportFile));
            }
            if (IsChecked(radioButtonExportSchema))
            {
                buf.Append(" -s");
            }
            else if (IsChecked(radioButtonExportData))
            {
                buf.Append(" -a");
            }
            string fmt = comboBoxFormat.SelectedValue.ToString();
            if (!string.IsNullOrEmpty(fmt))
            {
                buf.Append(" -F");
                buf.Append(fmt);
            }
            if (comboBoxCompressLevel.IsEnabled)
            {
                s = comboBoxCompressLevel.SelectedValue.ToString();
                if (!string.IsNullOrEmpty(s))
                {
                    buf.Append(' ');
                    buf.Append(s);
                }
            }
            if (IsChecked(checkBoxBlobs))
            {
                buf.Append(" -b");
            }
            if (IsChecked(checkBoxClean))
            {
                buf.Append(" -c");
            }
            if (IsChecked(checkBoxCreate))
            {
                buf.Append(" -C");
            }
            if (IsChecked(radioButtonSchema))
            {
                foreach (CheckBox cb in wrapPanelSchemas.Children)
                {
                    if (IsChecked(cb))
                    {
                        buf.Append(" -n ");
                        TextBlock tb = cb.Content as TextBlock;
                        buf.Append(tb.Text);
                    }
                }
            }
            if (IsChecked(radioButtonTable))
            {
                foreach (string tbl in SplitByWhiteSpace(textBoxTables.Text))
                {
                    buf.Append(" -t ");
                    buf.Append(tbl);
                }
            }
            if (IsChecked(radioButtonExcludeTable))
            {
                foreach (string tbl in SplitByWhiteSpace(textBoxExcludeTables.Text))
                {
                    buf.Append(" -T ");
                    buf.Append(tbl);
                }
            }
            if (IsChecked(radioButtonExcludeTableData))
            {
                foreach (string tbl in SplitByWhiteSpace(textBoxExcludeTables.Text))
                {
                    buf.Append(" --exclude-table-data=");
                    buf.Append(tbl);
                }
            }
            if (IsChecked(checkBoxUseJob))
            {
                int nJob;
                if (!int.TryParse(textBoxNumJobs.Text, out nJob))
                {
                    textBoxLog.AppendText((string)FindResource("messageInvalidJobs"));
                }
                else
                {
                    buf.Append(" -j ");
                    buf.Append(nJob);
                }
            }
            if (IsChecked(checkBoxLockTimeout))
            {
                int timeout;
                if (!int.TryParse(textBoxLockTimeout.Text, out timeout))
                {
                    textBoxLog.AppendText((string)FindResource("messageInvalidLockTime"));
                }
                else
                {
                    buf.Append(" --lock-wait-timeout=");
                    buf.Append(timeout);
                }
            }
            if (IsChecked(checkBoxExportOid))
            {
                buf.Append(" -o");
            }
            return buf.ToString();
        }

        private List<string> _outputBuffer = new List<string>();
        private object _outputBufferLock = new object();
        private void UpdateTextBoxLog()
        {
            if (_outputBuffer.Count == 0)
            {
                return;
            }
            List<string> buf;
            lock (_outputBufferLock)
            {
                buf = _outputBuffer;
                _outputBuffer = new List<string>();
            }
            foreach (string s in buf)
            {
                textBoxLog.AppendText(s);
            }
            textBoxLog.ScrollToEnd();
        }

        private void RunningProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //if (string.IsNullOrEmpty(e.Data))
            //{
            //    return;
            //}
            lock (_outputBufferLock)
            {
                _outputBuffer.Add(e.Data + Environment.NewLine);
            }
            Dispatcher.InvokeAsync(UpdateTextBoxLog);
        }

        private bool AnalyzeInput()
        {
            PgDumpFormatOption opt = comboBoxFormat.SelectedItem as PgDumpFormatOption;
            if (opt == null)
            {
                MessageBox.Show(this, (string)FindResource("messageNoFormat"), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            string path;
            if (opt.IsFile)
            {
                if (string.IsNullOrEmpty(textBoxPath.Text))
                {
                    MessageBox.Show(this, (string)FindResource("messageNoFilePath"), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    textBoxPath.Focus();
                    return false;
                }
                path = textBoxPath.Text;
            }
            else
            {
                if (string.IsNullOrEmpty(textBoxDir.Text))
                {
                    MessageBox.Show(this, (string)FindResource("messageNoDirectory"), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    textBoxDir.Focus();
                    return false;
                }
                path = textBoxDir.Text;
            }
            _exportDir = IO.Path.GetDirectoryName(path);
            _exportFile = IO.Path.GetFileName(path);
            if (IsChecked(checkBoxUseJob) && !int.TryParse(textBoxNumJobs.Text, out _))
            {
                MessageBox.Show(this, (string)FindResource("messageInvalidJobs"), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                textBoxNumJobs.Focus();
                textBoxNumJobs.SelectAll();
                return false;
            }
            if (IsChecked(checkBoxLockTimeout) && !int.TryParse(textBoxLockTimeout.Text, out _))
            {
                MessageBox.Show(this, (string)FindResource("messageInvalidLockTime"), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                textBoxLockTimeout.Focus();
                textBoxLockTimeout.SelectAll();
                return false;
            }
            return true;
        }

        private void buttonExport_Click(object sender, RoutedEventArgs e)
        {
            if (!AnalyzeInput())
            {
                return;
            }
            string exe = ShellUtil.TryQuote(comboBoxPgDump.SelectedValue.ToString());
            string cmd = string.Format("/k \"CHCP 65001 > NUL & {0} {1}\"", exe, GetCommandLineArgs());
            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = cmd,
                CreateNoWindow = false,
                UseShellExecute = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                RedirectStandardInput = false,
                WorkingDirectory = _exportDir
            };
            Process.Start(info);
            SaveSettingsToConnectionInfo();
            SaveSettingsToRegistry();
        }

        private void buttonCheckCommandLine_Click(object sender, RoutedEventArgs e)
        {
            if (!AnalyzeInput())
            {
                return;
            }
            string exe = ShellUtil.TryQuote(comboBoxPgDump.SelectedValue.ToString());
            string cmd = exe + " " + GetCommandLineArgs();
            textBoxLog.Clear();
            textBoxLog.AppendText("cd " + ShellUtil.TryQuote(_exportDir) + Environment.NewLine);
            textBoxLog.AppendText(cmd + Environment.NewLine);
            SaveSettingsToConnectionInfo();
            SaveSettingsToRegistry();
        }

        private void InitComboBoxPgDump()
        {
            string exe = "pg_dump.exe";
            string path = App.GetExecutableFromPath(exe);
            List<ComboBoxPathItem> l = new List<ComboBoxPathItem>();
            int p = -1;
            foreach (PgsqlInstallation ins in PgsqlInstallation.Installations)
            {
                ComboBoxPathItem item = new ComboBoxPathItem(ins, exe);
                if (string.Compare(item.Path, path, true) == 0)
                {
                    p = l.Count;
                }
                l.Add(item);
            }
            if (p == -1)
            {
                if (string.IsNullOrEmpty(path))
                {
                    l.Add(new ComboBoxPathItem() { Text = "pg_dump", Path = exe });
                }
                else
                {
                    l.Add(new ComboBoxPathItem() { Text = path, Path = path });
                }
                p = 0;
            }
            comboBoxPgDump.ItemsSource = l;
            comboBoxPgDump.SelectedIndex = p;
        }

        private void UpdateWrapPanelSchemas()
        {
            if (DataSet == null)
            {
                return;
            }
            wrapPanelSchemas.Children.Clear();
            List<Schema> l = new List<Schema>(DataSet.Schemas);
            l.Sort();
            foreach (Schema sch in l)
            {
                if (sch.IsHidden)
                {
                    continue;
                }
                TextBlock tb = new TextBlock()
                {
                    Text = sch.Name,
                    Background = Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 2, 0),
                    Padding = new Thickness(0)
                };
                CheckBox cb = new CheckBox()
                {
                    Tag = sch,
                    Content = tb,
                    IsChecked = !sch.IsHidden,
                    Margin = new Thickness(2),
                };
                Binding b = new Binding("IsChecked")
                {
                    ElementName = "radioButtonSchema"
                };
                cb.SetBinding(IsEnabledProperty, b);
                b = new Binding("IsChecked")
                {
                    ElementName = "radioButtonSchema"
                };
                tb.SetBinding(IsEnabledProperty, b);
                wrapPanelSchemas.Children.Add(cb);
            }
        }

        private void UpdateComboBoxEncoding()
        {
            List<Tuple<string, string>> l = new List<Tuple<string, string>>();
            if (DataSet == null)
            {
                l.Add(new Tuple<string, string>(string.Empty, (string)FindResource("messageDefaultValue")));
                return;
            }
            else
            {
                l.Add(new Tuple<string, string>(string.Empty, string.Format((string)FindResource("messageDefaultValueFmt"), DataSet.GetServerEncoding())));
                foreach (string s in DataSet.GetEncodings())
                {
                    l.Add(new Tuple<string, string>(s, s));
                }
            }
            comboBoxEncoding.DisplayMemberPath = "Item2";
            comboBoxEncoding.SelectedValuePath = "Item1";
            comboBoxEncoding.ItemsSource = l;
            comboBoxEncoding.SelectedValue = string.Empty;
        }

        private RegistryBinding _registryBinding;
        private void RequireRegistryBindings()
        {
            if (_registryBinding != null)
            {
                return;
            }
            _registryBinding = new RegistryBinding(this);
            _registryBinding.Register(this);
            _registryBinding.Register(this, comboBoxPgDump);
            _registryBinding.Register(this, comboBoxFormat);
            //_registryBinding.Register(this, checkBoxCompress);
            _registryBinding.Register(this, comboBoxCompressLevel);
			//_registryBinding.Register(this, textBoxPath);
			//_registryBinding.Register(this, textBoxDir);
			_registryBinding.Register(this, textBoxPath);
			_registryBinding.Register(this, textBoxDir);
			//_registryBinding.Register(this, radioButtonExportAll);
			_registryBinding.Register(this, checkBoxClean);
            _registryBinding.Register(this, checkBoxCreate);
            _registryBinding.Register(this, radioButtonSchema);
            //_registryBinding.Register(this, wrapPanelSchemas);
            //_registryBinding.Register(this, radioButtonTable);
            //_registryBinding.Register(this, textBoxTables);
            //_registryBinding.Register(this, radioButtonExcludeTable);
            //_registryBinding.Register(this, textBoxExcludeTables);
            //_registryBinding.Register(this, radioButtonExcludeTableData);
            //_registryBinding.Register(this, textBoxExcludeTablesData);
            _registryBinding.Register(this, checkBoxFoldOption);
            _registryBinding.Register(this, checkBoxUseJob);
            _registryBinding.Register(this, textBoxNumJobs);
            _registryBinding.Register(this, checkBoxLockTimeout);
            _registryBinding.Register(this, textBoxLockTimeout);
            _registryBinding.Register(this, checkBoxBlobs);
            _registryBinding.Register(this, checkBoxExportOid);
        }
        public RegistryBinding RegistryBinding
        {
            get
            {
                RequireRegistryBindings();
                return _registryBinding;
            }
        }

        private void LoadSettingsFromRegistry()
        {
            switch (App.RegistryFinder.GetInt32(GetType().Name, "radioButtonExport", 0))
            {
                case 0:
                    radioButtonExportAll.IsChecked = true;
                    break;
                case 1:
                    radioButtonExportSchema.IsChecked = true;
                    break;
                case 2:
                    radioButtonExportData.IsChecked = true;
                    break;
                default:
                    radioButtonExportAll.IsChecked = true;
                    break;
            }
            RegistryBinding.Load(App.RegistryFinder);
        }
        private void SaveSettingsToRegistry()
        {
            int index = 0;
            if (radioButtonExportAll.IsChecked ?? false)
            {
                index = 0;
            }
            else if (radioButtonExportSchema.IsChecked ?? false)
            {
                index = 1;
            }
            else if (radioButtonExportData.IsChecked ?? false)
            {
                index = 2;
            }
            App.RegistryFinder.SetValue(0, GetType().Name, "radioButtonExport", index);

            RegistryBinding.Save(App.RegistryFinder);
        }

        private string GetCheckedInWrapPanelSchemas()
        {
            if (!(radioButtonSchema.IsChecked ?? false))
            {
                return string.Empty;
            }
            StringBuilder buf = new StringBuilder();
            string prefix = string.Empty;
            foreach (UIElement element in wrapPanelSchemas.Children)
            {
                CheckBox checkBox = element as CheckBox;
                if (checkBox == null)
                {
                    continue;
                }
                buf.Append(prefix);
                buf.Append(checkBox.Content.ToString());
                prefix = ",";
            }
            return buf.ToString();
        }
        private void SetCheckedInWrapPanelSchemas(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                radioButtonSchema.IsChecked = false;
                return;
            }
            Dictionary<string, bool> dict = new Dictionary<string, bool>();
            foreach (string s in value.Split(','))
            {
                dict[s] = true;
            }
            foreach (UIElement element in wrapPanelSchemas.Children)
            {
                CheckBox checkBox = element as CheckBox;
                if (checkBox == null)
                {
                    continue;
                }
                bool chk;
                if (!dict.TryGetValue(checkBox.Content.ToString(), out chk))
                {
                    chk = false;
                }
                checkBox.IsChecked = chk;
            }
            radioButtonSchema.IsChecked = true;
        }

        private void LoadSettingsFromConnectionInfo()
        {
            NpgsqlConnectionInfo info = DataSet.ConnectionInfo as NpgsqlConnectionInfo;
            if (info == null)
            {
                return;
            }
            textBoxDir.Text = info.PgDumpDirectory;
            comboBoxEncoding.SelectedValue = info.PgDumpEncoding ?? string.Empty;
            SetCheckedInWrapPanelSchemas(info.PgDumpSchemas);
            textBoxTables.Text = info.PgDumpTables;
            radioButtonTable.IsChecked = !string.IsNullOrEmpty(textBoxTables.Text);
            textBoxExcludeTables.Text = info.PgDumpExcludeTables;
            radioButtonExcludeTable.IsChecked = !string.IsNullOrEmpty(textBoxExcludeTables.Text);
            textBoxExcludeTablesData.Text = info.PgDumpExcludeTablesData;
            radioButtonExcludeTableData.IsChecked = !string.IsNullOrEmpty(textBoxExcludeTablesData.Text);
        }

        private void SaveSettingsToConnectionInfo()
        {
            NpgsqlConnectionInfo info = DataSet.ConnectionInfo as NpgsqlConnectionInfo;
            if (info == null)
            {
                return;
            }
            string dir = textBoxDir.Text;
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                dir = string.Empty;
            }
            if (!string.IsNullOrEmpty(dir))
            {
                info.PgDumpDirectory = dir;
            }
            info.PgDumpSchemas = GetCheckedInWrapPanelSchemas();
            info.PgDumpEncoding = (string)comboBoxEncoding.SelectedValue;
            info.PgDumpTables = (radioButtonTable.IsChecked ?? false) ? textBoxTables.Text : string.Empty;
            info.PgDumpExcludeTables = (radioButtonExcludeTable.IsChecked ?? false) ? textBoxExcludeTables.Text : string.Empty;
            info.PgDumpExcludeTablesData = (radioButtonExcludeTableData.IsChecked ?? false) ? textBoxExcludeTablesData.Text : string.Empty;
            App.Connections.Save(info, null);
        }

        private void DataSetChanged()
        {
            UpdateComboBoxEncoding();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowLocator.AdjustMaxHeightToScreen(this);
            InitComboBoxPgDump();
            UpdateWrapPanelSchemas();
            UpdateComboBoxEncoding();
            LoadSettingsFromRegistry();
            LoadSettingsFromConnectionInfo();
        }

		private AggregatedEventDispatcher _locationChangedDispatcher;

		private void window_LocationChanged(object sender, EventArgs e)
		{
			if (_locationChangedDispatcher == null)
			{
				_locationChangedDispatcher = new AggregatedEventDispatcher(Dispatcher, () => { WindowLocator.AdjustMaxHeightToScreen(this); }, new TimeSpan(0, 0, 3));
			}
			_locationChangedDispatcher.Touch();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(Close, DispatcherPriority.ApplicationIdle);
        }

        private void comboBoxFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (textBoxPath == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(textBoxPath.Text))
            {
                return;
            }
            PgDumpFormatOption opt = comboBoxFormat.SelectedItem as PgDumpFormatOption;
            if (opt == null || !opt.IsFile)
            {
                return;
            }
            textBoxPath.Text = System.IO.Path.ChangeExtension(textBoxPath.Text, opt.DefaultExt);
        }
    }
}