using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Db2Source
{
    public class PgsqlInstallation: IComparable
    {
        public string Version { get; private set; }
        public string Name { get; private set; }
        public string BaseDirectory { get; private set; }
        public string BinDirectory { get; private set; }
        public string DataDirectory { get; private set; }
        private PgsqlInstallation(RegistryKey key)
        {
            Version = key.GetValue("CLT_Version")?.ToString();
            if (Version == null)
            {
                Version = key.GetValue("Version")?.ToString();
            }
            Name = key.GetValue("Branding")?.ToString();
            BaseDirectory = key.GetValue("Base Directory")?.ToString();
            BinDirectory = Path.Combine(BaseDirectory, "bin");
            DataDirectory = key.GetValue("Data Directory")?.ToString();
        }
        public int CompareTo(object obj)
        {
            if (!(obj is PgsqlInstallation))
            {
                return -1;
            }
            PgsqlInstallation pi = (PgsqlInstallation)obj;
            return CompareVersionStr(Version, pi.Version);
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PgsqlInstallation))
            {
                return false;
            }
            return CompareVersionStr(Version, ((PgsqlInstallation)obj).Version) == 0;
        }

        public override int GetHashCode()
        {
            return Version.GetHashCode();
        }

        private static PgsqlInstallation[] InitInstallations()
        {
            RegistryKey root = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\PostgreSQL\Installations");
            if (root == null)
            {
                return new PgsqlInstallation[0];
            }
            try
            {
                List<PgsqlInstallation> l = new List<PgsqlInstallation>();
                {
                    foreach (string key in root.GetSubKeyNames())
                    {
                        using (RegistryKey reg = root.OpenSubKey(key))
                        {
                            l.Add(new PgsqlInstallation(reg));
                        }
                    }
                }
                l.Sort();
                return l.ToArray();
            }
            finally
            {
                root.Dispose();
            }
        }

        /// <summary>
        /// 数値部分と非数値部分を分割
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string[] SplitNum(string value)
        {
            if (value == null)
            {
                return new string[0];
            }
            if (value == string.Empty)
            {
                return new string[] { value };
            }
            List<string> l = new List<string>();
            int n = value.Length;
            int i = 0;
            while (i < n)
            {
                int i0;
                for (i0 = i; i < n && '0' <= value[i] && value[i] <= '9'; i++) ;
                if (i0 < i)
                {
                    l.Add(value.Substring(i0, i - i0));
                }
                for (i0 = i; i < n && !('0' <= value[i] && value[i] <= '9'); i++) ;
                if (i0 < i)
                {
                    l.Add(value.Substring(i0, i - i0));
                }
            }
            return l.ToArray();
        }
        /// <summary>
        /// 数字部分は数字として大小を比較
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <returns></returns>
        private static int CompareVersionStr(string item1, string item2)
        {
            string[] s1 = SplitNum(item1);
            string[] s2 = SplitNum(item2);
            int n = Math.Min(s1.Length, s2.Length);
            for (int i = 0; i < n; i++)
            {
                int ret;
                long v1, v2;
                if (long.TryParse(s1[i], out v1) && long.TryParse(s2[i], out v2))
                {
                    ret = v1.CompareTo(v2);
                    if (ret != 0)
                    {
                        return ret;
                    }
                    // 数値として同じだった場合、文字列として違いがないか(1と01等)を以下で判定
                }
                ret = string.Compare(s1[i], s2[i]);
                if (ret != 0)
                {
                    return ret;
                }
            }
            return s1.Length.CompareTo(s2.Length);
        }

        public static readonly PgsqlInstallation[] Installations = InitInstallations();
    }
}
