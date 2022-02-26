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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// NewDatabaseWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NewPgsqlDatabaseWindow : Window
    {
        public static readonly DependencyProperty DataSetProperty = DependencyProperty.Register("DataSet", typeof(NpgsqlDataSet), typeof(NewPgsqlDatabaseWindow));
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(PgsqlDatabase), typeof(NewPgsqlDatabaseWindow));


        public NpgsqlDataSet DataSet
        {
            get { return (NpgsqlDataSet)GetValue(DataSetProperty); }
            set { SetValue(DataSetProperty, value); }
        }

        public PgsqlDatabase Target
        {
            get { return (PgsqlDatabase)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        private void InitDisplay()
        {
            comboBoxEncoding.ItemsSource = DataSet.GetEncodings();
            DisplayItem[] tablespaces = DisplayItem.ToDisplayItemArray(DataSet.Tablespaces, "(無指定)", new PgsqlTablespace(null), "新規表領域");
            DisplayItem[] users = DisplayItem.ToDisplayItemArray(DataSet.Users, null, new PgsqlUser(null) { }, "新規ユーザー");
            comboBoxTablespace.ItemsSource = tablespaces;
            tablespaceControl.UserItems = users;
            comboBoxOwner.ItemsSource = users;
            PgsqlDatabase tmpl = DataSet.DatabaseTemplates[0];
            Target = (tmpl != null) ? new PgsqlDatabase(tmpl) { Name = null } : new PgsqlDatabase(null, null);
            Target.PropertyChanged += Target_PropertyChanged;
            foreach (DisplayItem item in tablespaces)
            {
                if (item.IsNew)
                {
                    item.Item.PropertyChanged += Target_PropertyChanged;
                }
            }
            foreach (DisplayItem item in users)
            {
                if (item.IsNew)
                {
                    item.Item.PropertyChanged += Target_PropertyChanged;
                }
            }
        }

        //private bool _isUpdateTextBoxSqlPosted;
        private void UpdateTextBoxSql()
        {
            try
            {
                List<string> l = new List<string>();
                PgsqlDatabase db = new PgsqlDatabase(Target);
                bool isValid = true;
                if (db.DbaUserName == DisplayItem.NEWITEM_NAME)
                {
                    DisplayItem sel = comboBoxOwner.SelectedItem as DisplayItem;
                    PgsqlUser u = sel?.Item as PgsqlUser;
                    db.DbaUserName = u?.Name;
                    string[] sql = DataSet.GetSQL(u, string.Empty, ";", 0, false);
                    isValid &= (sql.Length != 0);
                    l.AddRange(sql);
                }
                if (db.DefaultTablespace == DisplayItem.NEWITEM_NAME)
                {
                    DisplayItem sel = comboBoxTablespace.SelectedItem as DisplayItem;
                    PgsqlTablespace ts = sel?.Item as PgsqlTablespace;
                    string[] sql = DataSet.GetSQL(ts, string.Empty, ";", 0, false);
                    isValid &= (sql.Length != 0);
                    l.AddRange(sql);
                }
                if (isValid)
                {
                    l.AddRange(DataSet.GetSQL(db, string.Empty, ";", 0, false));
                }
                StringBuilder buf = new StringBuilder();
                foreach (string s in l)
                {
                    buf.AppendLine(s);
                }
                textBoxSql.Text = buf.ToString();
            }
            finally
            {
                //_isUpdateTextBoxSqlPosted = false;
            }
        }
        private void Target_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //if (_isUpdateTextBoxSqlPosted)
            //{
            //    return;
            //}
            //_isUpdateTextBoxSqlPosted = true;
            //Dispatcher.InvokeAsync(UpdateTextBoxSql);
        }

        private void DataSetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            Dispatcher.InvokeAsync(InitDisplay);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == DataSetProperty)
            {
                DataSetPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public NewPgsqlDatabaseWindow()
        {
            InitializeComponent();
        }

        private void buttonGenerateSQL_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(UpdateTextBoxSql);
        }

        private void LogSQL(object sender, LogEventArgs e)
        {
        }

        private void buttonExecute_Click(object sender, RoutedEventArgs e)
        {
            string s = textBoxSql.Text.Trim();
            SQLParts sqls = DataSet.SplitSQL(s);
            if (sqls.Count == 0)
            {
                return;
            }
            foreach (SQLPart sql in sqls)
            {
                try
                {
                    DataSet.ExecSql(sql.SQL, LogSQL);
                    buttonError.Content = null;
                }
                catch (Exception t)
                {
                    Tuple<int, int> errPos = DataSet.GetErrorPosition(t, sql.SQL, 0);
                    buttonError.Content = DataSet.GetExceptionMessage(t);
                    buttonError.Tag = errPos;
                    Db2SrcDataSetController.ShowErrorPosition(t, textBoxSql, DataSet, sql.Offset);
                    return;
                }
            }
            MessageBoxResult ret = MessageBox.Show(this, string.Format((string)Resources["messageConnectDatabase"], textBoxDbName.Text), Properties.Resources.MessageBoxCaption_Confirm, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ret == MessageBoxResult.Yes)
            {
                App.OpenDatabase(Target);
            }
            DialogResult = true;
        }

        private void buttonError_Click(object sender, RoutedEventArgs e)
        {
            Tuple<int, int> pos = (Tuple<int, int>)buttonError.Tag;
            textBoxSql.Select(pos.Item1, pos.Item2);
            int l = textBoxSql.GetLineIndexFromCharacterIndex(pos.Item1);
            textBoxSql.ScrollToLine(l);
            textBoxSql.Focus();

        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            textBoxDbName.Focus();
        }
    }
}
