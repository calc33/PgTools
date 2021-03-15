using System;
using System.Collections.Generic;
using System.IO;
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
//using System.Windows.Shapes;

namespace Db2Source
{
    /// <summary>
    /// ExportSchema.xaml の相互作用ロジック
    /// </summary>
    public partial class ExportSchema: Window
    {
        private Db2SourceContext _dataSet;
        public Db2SourceContext DataSet
        {
            get { return _dataSet; }
            set
            {
                _dataSet = value;
                UpdateWrapPanelSchemas();
            }
        }
        public ExportSchema()
        {
            InitializeComponent();
            InitComboBoxEncoding();
        }

        private void InitComboBoxEncoding()
        {
            comboBoxEncoding.Items.Add(new ComboBoxItem() { Content = "ANSI", Tag = Encoding.Default });
            comboBoxEncoding.Items.Add(new ComboBoxItem() { Content = "UTF-8", Tag = Encoding.UTF8 });
            comboBoxEncoding.Items.Add(new ComboBoxItem() { Content = "Unicode(UCS-2)", Tag = Encoding.Unicode });
            comboBoxEncoding.Items.Add(new ComboBoxItem() { Content = "Unicode(UCS-4)", Tag = Encoding.UTF32 });
            comboBoxEncoding.SelectedIndex = 0;
        }

        //private static int CompareSchemaByName(Schema item1, Schema item2)
        //{
        //    if (item1 == null || item2 == null)
        //    {
        //        return (item1 != null ? 1 : 0) - (item2 != null ? 1 : 0);
        //    }
        //    int ret;
        //    ret = (item1.IsHidden ? 1 : 0) - (item2.IsHidden ? 1 : 0);
        //    if (ret != 0) {
        //        return ret;
        //    }
        //    ret = string.Compare(item1.Name, item2.Name);
        //    return ret;
        //}
        private void UpdateWrapPanelSchemas()
        {
            wrapPanelSchemas.Children.Clear();
            if (_dataSet == null)
            {
                return;
            }
            List<Schema> l = new List<Schema>(_dataSet.Schemas);
            //l.Sort(CompareSchemaByName);
            l.Sort();
            foreach (Schema sc in l)
            {
                CheckBox cb = new CheckBox();
                cb.Content = sc;
                cb.IsChecked = !sc.IsHidden;
                cb.Padding = new Thickness(2, 2, 4, 2);
                if (sc.IsHidden)
                {
                    cb.Foreground = SystemColors.GrayTextBrush;
                }
                cb.VerticalContentAlignment = VerticalAlignment.Center;
                wrapPanelSchemas.Children.Add(cb);
            }
        }
        private void buttonSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Title = "エクスポート先を選択";
            dlg.SelectedPath = textBoxFolder.Text;
            Db2Source.DialogResult ret = dlg.ShowDialog(this);
            if (ret != Db2Source.DialogResult.OK)
            {
                return;
            }
            textBoxFolder.Text = dlg.SelectedPath;
        }
        private void ExportTable(StringBuilder buffer, Table table)
        {
            if (table == null)
            {
                return;
            }
            foreach (string s in DataSet.GetSQL(table, string.Empty, ";", 0, true, true))
            {
                buffer.AppendLine(s);
            }
            List<Constraint> list = new List<Constraint>(table.Constraints);
            list.Sort();
            int lastLength = buffer.Length;
            foreach (Constraint c in list)
            {
                switch (c.ConstraintType)
                {
                    case ConstraintType.Primary:
                        // 本体ソース内で出力している
                        break;
                    case ConstraintType.Unique:
                        buffer.Append(DataSet.GetSQL(c, string.Empty, ";", 0, true, true));
                        break;
                    case ConstraintType.ForeignKey:
                        buffer.Append(DataSet.GetSQL(c, string.Empty, ";", 0, true, true));
                        break;
                    case ConstraintType.Check:
                        buffer.Append(DataSet.GetSQL(c, string.Empty, ";", 0, true, true));
                        break;
                }
            }
            if (lastLength < buffer.Length)
            {
                buffer.AppendLine();
            }
            lastLength = buffer.Length;
            if (!string.IsNullOrEmpty(table.CommentText))
            {
                buffer.Append(DataSet.GetSQL(table.Comment, string.Empty, ";", 0, true));
            }
            foreach (Column c in table.Columns)
            {
                if (!string.IsNullOrEmpty(c.CommentText))
                {
                    buffer.Append(DataSet.GetSQL(c.Comment, string.Empty, ";", 0, true));
                }
            }
            if (lastLength < buffer.Length)
            {
                buffer.AppendLine();
            }
            lastLength = buffer.Length;
            foreach (Trigger t in table.Triggers)
            {
                buffer.Append(DataSet.GetSQL(t, string.Empty, ";", 0, true));
                buffer.AppendLine();
            }
            if (lastLength < buffer.Length)
            {
                buffer.AppendLine();
            }
            lastLength = buffer.Length;
            foreach (Index i in table.Indexes)
            {
                buffer.Append(DataSet.GetSQL(i, string.Empty, ";", 0, true));
            }
            if (lastLength < buffer.Length)
            {
                buffer.AppendLine();
            }
        }
        private void ExportView(StringBuilder buffer, View view)
        {
            if (view == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(view, string.Empty, ";", 0, true));
            if (!string.IsNullOrEmpty(view.CommentText))
            {
                buffer.Append(DataSet.GetSQL(view.Comment, string.Empty, ";", 0, true));
            }
            foreach (Column c in view.Columns)
            {
                if (!string.IsNullOrEmpty(c.CommentText))
                {
                    buffer.Append(DataSet.GetSQL(c.Comment, string.Empty, ";", 0, true));
                }
            }
            buffer.AppendLine();
        }
        private void ExportStoredFunction(StringBuilder buffer, StoredFunction function)
        {
            if (function == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(function, string.Empty, ";", 0, true));
            if (!string.IsNullOrEmpty(function.CommentText))
            {
                buffer.Append(DataSet.GetSQL(function.Comment, string.Empty, ";", 0, true));
            }
        }
        private void ExportSequence(StringBuilder buffer, Sequence sequence)
        {
            if (sequence == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(sequence, string.Empty, ";", 0, true, true, true));
        }
        private void ExportComplexType(StringBuilder buffer, ComplexType type)
        {
            if (type == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(type, string.Empty, ";", 0, true));
        }
        private void ExportEnumType(StringBuilder buffer, PgsqlEnumType type)
        {
            if (type == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(type, string.Empty, ";", 0, true));
        }
        private void ExportBasicType(StringBuilder buffer, PgsqlBasicType type)
        {
            if (type == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(type, string.Empty, ";", 0, true));
        }
        private void ExportRangeType(StringBuilder buffer, PgsqlRangeType type)
        {
            if (type == null)
            {
                return;
            }
            buffer.Append(DataSet.GetSQL(type, string.Empty, ";", 0, true));
        }
        private async Task ExportAsync(Db2SourceContext dataSet, IEnumerable<Schema> schemas, string baseDir)
        {
            Dictionary<string, bool> exported = new Dictionary<string, bool>();
            await dataSet.LoadSchemaAsync();
            foreach (Schema s in schemas) 
            {
                string schemaDir = Path.Combine(baseDir, s.Name);
                foreach (SchemaObject obj in s.Objects)
                {
                    StringBuilder buf = new StringBuilder();
                    if (obj is Table)
                    {
                        ExportTable(buf, (Table)obj);
                    }
                    else if (obj is View)
                    {
                        ExportView(buf, (View)obj);
                    }
                    else if (obj is StoredFunction)
                    {
                        ExportStoredFunction(buf, (StoredFunction)obj);
                    }
                    else if (obj is Sequence)
                    {
                        ExportSequence(buf, (Sequence)obj);
                    }
                    else if (obj is ComplexType)
                    {
                        ExportComplexType(buf, (ComplexType)obj);
                    }
                    else if (obj is PgsqlEnumType)
                    {
                        ExportEnumType(buf, (PgsqlEnumType)obj);
                    }
                    else if (obj is PgsqlBasicType)
                    {
                        ExportBasicType(buf, (PgsqlBasicType)obj);
                    }
                    else if (obj is PgsqlRangeType)
                    {
                        ExportRangeType(buf, (PgsqlRangeType)obj);
                    }
                    if (buf.Length != 0)
                    {
                        string dir = Path.Combine(schemaDir, obj.GetExportFolderName());
                        string path = Path.Combine(dir, obj.Name + ".sql");
                        Directory.CreateDirectory(dir);
                        bool append = exported.ContainsKey(path);
                        Encoding encoding = (comboBoxEncoding.SelectedItem as ComboBoxItem)?.Tag as Encoding;
                        encoding = encoding ?? Encoding.UTF8;
                        using (StreamWriter sw = new StreamWriter(path, append, encoding))
                        {
                            if (append)
                            {
                                sw.WriteLine();
                            }
                            sw.Write(buf.ToString());
                        }
                        exported[path] = true;
                    }
                }
            }
        }
        private void buttonExport_Click(object sender, RoutedEventArgs e)
        {
            List<Schema> list = new List<Schema>();
            string baseDir = textBoxFolder.Text;
            foreach (CheckBox cb in wrapPanelSchemas.Children)
            {
                if (!cb.IsChecked.HasValue || !cb.IsChecked.Value)
                {
                    continue;
                }
                Schema s = cb.Content as Schema;
                list.Add(s);
            }
            Task t = ExportAsync(DataSet, list, baseDir);
            AwaitWindow win = new AwaitWindow();
            win.Owner = this;
            win.WaitTask(t);
            MessageBox.Show(this, "出力完了", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
