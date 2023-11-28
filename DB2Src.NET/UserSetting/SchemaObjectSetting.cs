using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Db2Source
{
    public abstract partial class SchemaObjectSetting
    {
        public long Id { get; set; }
        public string ObjectType { get; set; }
        public string FullIdentifier { get; set; }
        public string Name { get; set; }

        public abstract Type TargetControlClass();
    }

    public abstract partial class SchemaObjectSetting<T> : SchemaObjectSetting where T : UserControl, ISchemaObjectWpfControl
    {
        public override Type TargetControlClass()
        {
            return typeof(T);
        }
        public abstract void Load(T obj);
        public abstract void Save(T obj);
    }

    public class ConditionHistory
    {
        public string Condition { get; set; }
        public DateTime LastExecuted { get; set; }
        internal ConditionHistory(string condition)
        {
            Condition = condition;
            LastExecuted = DateTime.Now;
        }

    }

    public partial class TableJoinSetting
    {
        public string TableJoin { get; set; }
        public string ReferenceTable { get; set; }
        public bool IsSelected { get; set; }
        public string Alias { get; set; }
        internal TableJoinSetting(string tableJoin, string referenceTable, string alias, bool isSelected)
        {
            TableJoin = tableJoin;
            ReferenceTable = referenceTable;
            IsSelected = isSelected;
            Alias = alias;
        }
    }
    public class ConditionHistoryCollection : IReadOnlyList<ConditionHistory>
    {
        private List<ConditionHistory> _list = new List<ConditionHistory>();

        #region IReadOnlyList<ConditionHistory>
        public int Count { get { return _list.Count; } }
        public ConditionHistory this[int index] { get { return _list[index]; } }

        public IEnumerator<ConditionHistory> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_list).GetEnumerator();
        }
        #endregion
        public void Add(string condition)
        {
            string c = condition.Trim();
            for (int i = _list.Count - 1; 0 <= i; i--)
            {
                ConditionHistory h = _list[i];
                if (string.Compare(h.Condition, c) == 0)
                {
                    _list.RemoveAt(i);
                }
            }
            _list.Add(new ConditionHistory(c));
        }
    }

    public class TableJoinSettingCollection : IReadOnlyList<TableJoinSetting>
    {
        private List<TableJoinSetting> _list = new List<TableJoinSetting>();

        #region IReadOnlyList<TableJoinSetting>
        public TableJoinSetting this[int index] { get { return _list[index]; } }

        public int Count { get { return _list.Count; } }

        public IEnumerator<TableJoinSetting> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_list).GetEnumerator();
        }
        #endregion

        public void Add(string tableJoin, string referenceTable, string alias, bool isSelected)
        {
            _list.Add(new TableJoinSetting(tableJoin, referenceTable, alias, isSelected));
        }
        internal void Add(TableJoinSetting setting)
        {
            _list.Add(setting);
        }
    }

    public partial class TableSetting : SchemaObjectSetting<TableControl>
    {

        public bool UseLimit { get; set; } = true;
        public int LimitCount { get; set; } = 100;
        public bool AutoFetch { get; set; } = true;
        public string Alias { get; set; } = "a";

        public ConditionHistoryCollection Histories { get; } = new ConditionHistoryCollection();
        public TableJoinSettingCollection TableJoinSettings { get; } = new TableJoinSettingCollection();

        public override void Load(TableControl obj)
        {
            obj.checkBoxLimitRow.IsChecked = UseLimit;
            obj.textBoxLimitRow.Text = LimitCount.ToString();
            obj.checkBoxAutoFetch.IsChecked = AutoFetch;
            (obj.ListBoxJoinTables.Items[0] as JoinTable).Alias = Alias;
        }
        public override void Save(TableControl obj)
        {
            UseLimit = obj.checkBoxLimitRow.IsChecked ?? false;
            if (int.TryParse(obj.textBoxLimitRow.Text, out int n))
            {
                LimitCount = n;
            }
            AutoFetch = obj.checkBoxAutoFetch.IsChecked ?? false;
            Alias = (obj.ListBoxJoinTables.Items[0] as JoinTable).Alias;
            SaveChanges();
        }
    }

    public partial class ViewSetting : SchemaObjectSetting<ViewControl>
    {

        public bool UseLimit { get; set; } = true;
        public int LimitCount { get; set; } = 100;
        public bool AutoFetch { get; set; } = true;
        public string Alias { get; set; } = "a";

        public ConditionHistoryCollection Histories { get; } = new ConditionHistoryCollection();
        public TableJoinSettingCollection TableJoinSettings { get; } = new TableJoinSettingCollection();

        public override void Load(ViewControl obj)
        {
            obj.checkBoxLimitRow.IsChecked = UseLimit;
            obj.textBoxLimitRow.Text = LimitCount.ToString();
            obj.checkBoxAutoFetch.IsChecked = AutoFetch;
        }

        public override void Save(ViewControl obj)
        {
            UseLimit = obj.checkBoxLimitRow.IsChecked ?? false;
            if (int.TryParse(obj.textBoxLimitRow.Text, out int n))
            {
                LimitCount = n;
            }
            AutoFetch = obj.checkBoxAutoFetch.IsChecked ?? false;
            SaveChanges();
        }
    }

    public partial class StoredProcedureSetting : SchemaObjectSetting<StoredProcedureControl>
    {
        public string[] ParamValues { get; set; }

        internal string ParamValuesCSV
        {
            get
            {
                return StrUtil.DelimitedText(ParamValues, ',', '"', "\r\n");
            }
            set
            {
                ParamValues = StrUtil.SplitDelimitedText(value, ',', '"');
            }
        }

        public override void Load(StoredProcedureControl obj)
        {
            IList<ParamEditor> l = obj.dataGridParameters.ItemsSource as IList<ParamEditor>;
            int n = Math.Min(l.Count, ParamValues.Length);
            for (int i = 0; i < n; i++)
            {
                ParamEditor editor = l[i];
                editor.Value = ParamValues[i];
            }
        }

        public override void Save(StoredProcedureControl obj)
        {
            IList<ParamEditor> l = obj.dataGridParameters.ItemsSource as IList<ParamEditor>;
            string[] vals = new string[l.Count];
            for (int i = 0; i < l.Count; i++)
            {
                ParamEditor editor = l[i];
                vals[i] = editor.Value;
            }
            ParamValues = vals;
        }
    }
}
