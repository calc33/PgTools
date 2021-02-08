using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class PgsqlSetting
    {
        public string Name { get; set; }
        public string Setting { get; set; }
        public string Unit { get; set; }
        public string Category { get; set; }
        public string ShortDesc { get; set; }
        public string ExtraDesc { get; set; }
        public string Context { get; set; }
        public string BootVal { get; set; }
        public string ResetVal { get; set; }
        public bool PendingRestart { get; set; }
    }

    public class PgsqlSettingCollection: IList<PgsqlSetting>
    {
        private List<PgsqlSetting> _list = new List<PgsqlSetting>();

        public PgsqlSetting this[int index]
        {
            get
            {
                return _list[index];
            }
            set
            {
                _list[index] = value;
            }
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }
        public void Add(PgsqlSetting item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(PgsqlSetting item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(PgsqlSetting[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<PgsqlSetting> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(PgsqlSetting item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, PgsqlSetting item)
        {
            _list.Insert(index, item);
        }

        public bool Remove(PgsqlSetting item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_list).GetEnumerator();
        }
    }
}
