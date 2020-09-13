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

namespace Db2Source
{
    /// <summary>
    /// PgDumpOptionWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class PgDumpOptionWindow : Window
    {
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
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Title = "出力先フォルダを選択";
            dlg.SelectedPath = textBoxDir.Text;
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
                return new string[0];
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
        private static string GetEscapedFileName(string path)
        {
            StringBuilder buf = new StringBuilder();
            buf.Append('"');
            foreach (char c in path)
            {
                if (c == '"')
                {
                    buf.Append(c);
                }
                buf.Append(c);
            }
            buf.Append('"');
            return buf.ToString();
        }
        private string _exportDir;
        private string _exportFile;
        private string _exportBat;
        private string GetCommandLineArgs()
        {
            StringBuilder buf = new StringBuilder();
            NpgsqlConnectionInfo info = DataSet.ConnectionInfo as NpgsqlConnectionInfo;
            buf.AppendFormat("-h {0} -p {1} -d {2} -U {3}", info.ServerName, info.ServerPort, info.DatabaseName, info.UserName);
            string s = comboBoxEncoding.SelectedValue.ToString();
            if (!string.IsNullOrEmpty(s))
            {
                buf.Append(" -E ");
                buf.Append(s);
            }
            if (!string.IsNullOrEmpty(_exportFile))
            {
                buf.Append(" -f ");
                buf.Append(GetEscapedFileName(_exportFile));
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
            if (checkBoxCompress.IsEnabled && IsChecked(checkBoxCompress))
            {
                buf.Append(" -Z ");
                buf.Append(comboBoxCompressLevel.Text);
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
                    textBoxLog.AppendText("並列ジョブ数が不正です");
                } else
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
                    textBoxLog.AppendText("テーブルがロックされていた場合の待ち時間が不正です");
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

        private Process _runningProcess;
        private void UpdateButtonExportEnabled()
        {
            buttonExport.IsEnabled = (_runningProcess == null);
            dockPanelInput.Visibility = (_runningProcess == null) ? Visibility.Collapsed : Visibility.Visible;
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
        private void WaitForProcess()
        {
            try
            {
                if (_runningProcess == null)
                {
                    return;
                }
                _runningProcess.OutputDataReceived += RunningProcess_OutputDataReceived;
                _runningProcess.ErrorDataReceived += RunningProcess_OutputDataReceived;
                _runningProcess.BeginOutputReadLine();
                _runningProcess.BeginErrorReadLine();
                _runningProcess.WaitForExit();
                lock (_outputBufferLock)
                {
                    _outputBuffer.Add("*** 終了しました ***" + Environment.NewLine);
                }
                Dispatcher.Invoke(UpdateTextBoxLog, DispatcherPriority.Normal);
            }
            finally
            {
                _runningProcess = null;
                Dispatcher.Invoke(UpdateButtonExportEnabled, DispatcherPriority.Normal);
                IO.File.Delete(_exportBat);
            }
        }
        private async Task WaitForProcessAsync()
        {
            await Task.Run((Action)WaitForProcess);
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
            Dispatcher.Invoke(UpdateTextBoxLog, DispatcherPriority.Normal);
        }

        private bool AnalyzeInput()
        {
            PgDumpFormatOption opt = comboBoxFormat.SelectedItem as PgDumpFormatOption;
            if (opt == null)
            {
                MessageBox.Show(this, "出力方式を指定してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            string path;
            if (opt.IsFile)
            {
                if (string.IsNullOrEmpty(textBoxPath.Text))
                {
                    MessageBox.Show(this, "出力ファイル名を指定してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    textBoxPath.Focus();
                    return false;
                }
                path = textBoxPath.Text;
            }
            else
            {
                if (string.IsNullOrEmpty(textBoxDir.Text))
                {
                    MessageBox.Show(this, "出力先フォルダを指定してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    textBoxDir.Focus();
                    return false;
                }
                path = textBoxDir.Text;
            }
            _exportDir = IO.Path.GetDirectoryName(path);
            _exportFile = IO.Path.GetFileName(path);
            int n;
            if (IsChecked(checkBoxUseJob) && !int.TryParse(textBoxNumJobs.Text, out n))
            {
                MessageBox.Show(this, "並列ジョブ数が不正です", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                textBoxNumJobs.Focus();
                textBoxNumJobs.SelectAll();
                return false;
            }
            if (IsChecked(checkBoxLockTimeout) && !int.TryParse(textBoxLockTimeout.Text, out n))
            {
                MessageBox.Show(this, "ロック待ち秒数が不正です", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
            buttonExport.IsEnabled = false;
            try
            {
                _exportBat = IO.Path.Combine(_exportDir, "_dump.bat");
                string cmd = "pg_dump " + GetCommandLineArgs();
                textBoxLog.Clear();
                textBoxLog.AppendText("cd " + _exportDir + Environment.NewLine);
                textBoxLog.AppendText(cmd + Environment.NewLine);
                IO.File.WriteAllLines(_exportBat, new string[] { "@ECHO OFF", "CHCP 65001 > NUL:", cmd });
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = _exportBat;
                //info.Arguments = cmd;
                info.CreateNoWindow = true;
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                //info.RedirectStandardError = false;
                info.RedirectStandardInput = true;
                info.StandardOutputEncoding = Encoding.UTF8;
                info.StandardErrorEncoding = Encoding.UTF8;
                info.WorkingDirectory = _exportDir;
                _runningProcess = Process.Start(info);
                Task t = WaitForProcessAsync();
            }
            finally
            {
                UpdateButtonExportEnabled();
            }
        }

        private void buttonCheckCommandLine_Click(object sender, RoutedEventArgs e)
        {
            if (!AnalyzeInput())
            {
                return;
            }
            string cmd = "pg_dump " + GetCommandLineArgs();
            textBoxLog.Clear();
            textBoxLog.AppendText("cd " + _exportDir + Environment.NewLine);
            textBoxLog.AppendText(cmd + Environment.NewLine);
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
                CheckBox cb = new CheckBox();
                cb.Tag = sch;
                TextBlock tb = new TextBlock()
                {
                    Text = sch.Name,
                    Background = Brushes.Transparent,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 2, 0),
                    Padding = new Thickness(0)
                };
                cb.Content = tb;
                cb.IsChecked = !sch.IsHidden;
                cb.Margin = new Thickness(2);
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
                l.Add(new Tuple<string, string>(string.Empty, "既定値"));
                return;
            }
            else
            {
                l.Add(new Tuple<string, string>(string.Empty, string.Format("既定値({0})", DataSet.GetServerEncoding())));
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

        private void DataSetChanged()
        {
            UpdateComboBoxEncoding();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateWrapPanelSchemas();
            UpdateButtonExportEnabled();
            UpdateComboBoxEncoding();
        }

        private void textBoxInput_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Process p = _runningProcess;
                if (p != null)
                {
                    p.StandardInput.WriteLine(textBoxInput.Text);
                    textBoxInput.Text = string.Empty;
                }
                e.Handled = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void comboBoxFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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
    public class PgDumpFormatOption: DependencyObject
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(PgDumpFormatOption));
        public static readonly DependencyProperty OptionProperty = DependencyProperty.Register("Option", typeof(string), typeof(PgDumpFormatOption));
        public static readonly DependencyProperty CanCompressProperty = DependencyProperty.Register("CanCompress", typeof(bool), typeof(PgDumpFormatOption));
        public static readonly DependencyProperty IsFileProperty = DependencyProperty.Register("IsFile", typeof(bool), typeof(PgDumpFormatOption));
        public static readonly DependencyProperty DefaultExtProperty = DependencyProperty.Register("DefaultExt", typeof(string), typeof(PgDumpFormatOption));
        public static readonly DependencyProperty DialogFilterProperty = DependencyProperty.Register("DialogFilter", typeof(string), typeof(PgDumpFormatOption));
        public string Text
        {
            get
            {
                return (string)GetValue(TextProperty);
            }
            set
            {
                SetValue(TextProperty, value);
            }
        }
        public string Option
        {
            get
            {
                return (string)GetValue(OptionProperty);
            }
            set
            {
                SetValue(OptionProperty, value);
            }
        }
        public bool CanCompress
        {
            get
            {
                return (bool)GetValue(CanCompressProperty);
            }
            set
            {
                SetValue(CanCompressProperty, value);
            }
        }
        public bool IsFile
        {
            get
            {
                return (bool)GetValue(IsFileProperty);
            }
            set
            {
                SetValue(IsFileProperty, value);
            }
        }
        public bool IsDirectory
        {
            get
            {
                return !IsFile;
            }
            set
            {
                IsFile = !value;
            }
        }
        public string DefaultExt
        {
            get
            {
                return (string)GetValue(DefaultExtProperty);
            }
            set
            {
                SetValue(DefaultExtProperty, value);
            }
        }
        public string DialogFilter
        {
            get
            {
                return (string)GetValue(DialogFilterProperty);
            }
            set
            {
                SetValue(DialogFilterProperty, value);
            }
        }
        public override string ToString()
        {
            return Text;
        }
    }
}
