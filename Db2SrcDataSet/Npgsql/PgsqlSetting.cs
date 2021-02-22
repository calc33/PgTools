using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class PgsqlSetting : ICloneable
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

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool ContentEquals(object obj)
        {
            if (GetType() != obj.GetType())
            {
                return false;
            }
            PgsqlSetting s = (PgsqlSetting)obj;
            return Name == s.Name
                && Setting == s.Setting;
                //&& Unit == s.Unit
                //&& Category == s.Category
                //&& ShortDesc == s.ShortDesc
                //&& ExtraDesc == s.ExtraDesc
                //&& Context == s.Context
                //&& BootVal == s.BootVal
                //&& ResetVal == s.ResetVal
                //&& PendingRestart == s.PendingRestart;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is PgsqlSetting))
            {
                return false;
            }
            PgsqlSetting o = (PgsqlSetting)obj;
            return string.Equals(Name, o.Name);
        }
        public override int GetHashCode()
        {
            if (string.IsNullOrEmpty(Name))
            {
                return 0;
            }
            return Name.GetHashCode();
        }
        public override string ToString()
        {
            return string.Format("{0}: {1}", Name, Setting);
        }
    }

    public class PgsqlSettingCollection: IList<PgsqlSetting>, ICloneable
    {
        private List<PgsqlSetting> _list = new List<PgsqlSetting>();

        #region IListの実装
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
        #endregion

        #region IClonableの実装
        public object Clone()
        {
            PgsqlSettingCollection ret = new PgsqlSettingCollection();
            foreach (PgsqlSetting s in this)
            {
                ret.Add((PgsqlSetting)(s.Clone()));
            }
            return ret;
        }
        #endregion

        public bool ContentEquals(PgsqlSettingCollection obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (Count != obj.Count)
            {
                return false;
            }
            for (int i = 0; i < Count; i++)
            {
                PgsqlSetting a = this[i];
                PgsqlSetting b = obj[i];
                if (!a.ContentEquals(b))
                {
                    return false;
                }
            }
            return true;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is PgsqlSettingCollection))
            {
                return false;
            }
            PgsqlSettingCollection l = (PgsqlSettingCollection)obj;
            if (Count != l.Count)
            {
                return false;
            }
            for (int i = 0; i < Count; i++)
            {
                PgsqlSetting a = this[i];
                PgsqlSetting b = l[i];
                if (!a.Equals(b))
                {
                    return false;
                }
            }
            return true;
        }
        public override int GetHashCode()
        {
            int h = base.GetHashCode();
            foreach (PgsqlSetting s in this)
            {
                h = h * 13 + s.GetHashCode();
            }
            return h;
        }
    }
}
