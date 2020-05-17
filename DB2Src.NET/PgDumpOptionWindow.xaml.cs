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
        public Db2SourceContext DataSet { get; set; }

        public PgDumpOptionWindow()
        {
            InitializeComponent();
        }

        private void buttonSelectPath_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".sql";
            dlg.Filter = "SQL File(*.sql)|*.sql";
            dlg.FileName = textBoxPath.Text;
            bool? b = dlg.ShowDialog(this);
            if (!b.HasValue || !b.Value)
            {
                return;
            }
            textBoxPath.Text = dlg.FileName;
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
            if (IsChecked(checkBoxCompress))
            {
                buf.Append(" -Z ");
                buf.Append(comboBoxCompressLevel.Text);
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

        private void buttonExport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxPath.Text))
            {
                MessageBox.Show(this, "出力先ファイルを指定してください", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string path = textBoxPath.Text;
            _exportDir = IO.Path.GetDirectoryName(path);
            _exportFile = IO.Path.GetFileName(path);
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateWrapPanelSchemas();
            UpdateButtonExportEnabled();
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
    }
}
