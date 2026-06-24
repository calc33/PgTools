using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Db2Source
{
    public class NpgsqlSession : ISession
    {
        public int Pid { get; private set; }
        public string DatabaseName { get; private set; }
        public string UserName { get; private set; }
        public string ApplicationName { get; private set; }
        public string Client { get; private set; }

        public string Hostname { get; private set; }
        public string Address { get; private set; }
        public int? Port { get; private set; }
        public string WaitEventType { get; private set; }
        public string WaitEvent { get; private set; }
        public string State { get; private set; }

        public NpgsqlSession() { }
        internal NpgsqlSession(NpgsqlDataSet.PgStatActivity source)
        {
            Pid = source.pid;
            DatabaseName = source.datname;
            UserName = source.usename;
            ApplicationName = source.application_name;
            Client = source.client_hostname;
            Address = source.client_addr;
            if (!string.IsNullOrEmpty(Client))
            {
                Hostname = string.Format("{0}({1})", Client, Address);
            }
            else
            {
                Hostname = Address;
            }
            Port = source.client_port;
            WaitEventType = source.wait_event_type;
            WaitEvent = source.wait_event;
            State = source.state;
        }

        public bool AbortQuery(IDbConnection connection)
        {
            NpgsqlConnection conn = connection as NpgsqlConnection;
            using (NpgsqlCommand cmd = new NpgsqlCommand(string.Format("select pg_cancel_backend(a.pid) from pg_stat_get_activity({0}) a", Pid), conn))
            {
                return (bool)cmd.ExecuteScalar();
            }
        }

        public bool CanAbortQuery()
        {
            return true;
        }

        public bool CanKill()
        {
            return true;
        }

        public bool Kill(IDbConnection connection)
        {
            NpgsqlConnection conn = connection as NpgsqlConnection;
            using (NpgsqlCommand cmd = new NpgsqlCommand(string.Format("select pg_terminate_backend(a.pid) from pg_stat_get_activity({0}) a", Pid), conn))
            {
                return (bool)cmd.ExecuteScalar();
            }
        }

        public string Description { get { return ToString(); } }

        public override string ToString()
        {
            string app = string.IsNullOrEmpty(ApplicationName) ? string.Empty : string.Format(" by \"{0}\"", ApplicationName);
            return string.Format("Pid:{0}({1}@{2} from {3}:{4}{5})", Pid, UserName, DatabaseName, Hostname, Port, app);
        }
    }


    public enum LockMode
    {
        None = 0,
        AccessShare = 1,
        RowShare = 2,
        RowExclusive = 3,
        ShareUpdateExclusive = 4,
        Share = 5,
        ShareRowExclusive = 6,
        Exclusive = 7,
        AccessExclusive = 8
    }

    public class LockedRecord
    {
        public string[] FieldNames { get; private set; }
        public object[] Values { get; private set; }

        public int[] PrimaryKeyIndex { get; private set; }

        internal LockedRecord(LockEntry entry, IDbConnection connection)
        {
            List<int> keyIdx = new List<int>();
            NpgsqlConnection conn = connection as NpgsqlConnection;
            string sql = string.Format("select * from {0} where ctid={1}", entry.ObjectName, entry._ctid);
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, conn))
            {
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        FieldNames = new string[reader.FieldCount];
                        Values = new object[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            FieldNames[i] = reader.GetName(i);
                            if (entry._primaryKeys.ContainsKey(FieldNames[i]))
                            {
                                keyIdx.Add(i);
                            }
                            try
                            {
                                if (reader.IsDBNull(i))
                                {
                                    Values[i] = null;
                                }
                                else
                                {
                                    Values[i] = reader.GetValue(i);
                                }
                            }
                            catch
                            {
                                Values[i] = "(UNSUPPORTED)";
                            }
                        }
                    }
                }
            }
            PrimaryKeyIndex = keyIdx.ToArray();
        }
    }

    public class LockEntry : IComparable
    {
        private static Dictionary<string, LockMode> _modeToLockMode = new Dictionary<string, LockMode>()
        {
            { "AccessShareLock", LockMode.AccessShare },
            { "RowShareLock", LockMode.RowShare },
            { "RowExclusiveLock", LockMode.RowExclusive },
            { "ShareUpdateExclusiveLock", LockMode.ShareUpdateExclusive },
            { "ShareLock", LockMode.Share },
            { "ShareRowExclusiveLock", LockMode.ShareRowExclusive },
            { "ExclusiveLock", LockMode.Exclusive },
            { "AccessExclusiveLock", LockMode.AccessExclusive },
        };

        private static Dictionary<char, string> _relkindToObjectKind = new Dictionary<char, string>()
        {
            { 'r', "table" },
            { 'i', "index" },
            { 'S', "sequence" },
            { 'v', "view" },
            { 'm', "materialized view" },
            { 'c', "complex type" },
            { 't', "toast table" },
            { 'f', "foreign table" },
        };


        public string ObjectKind { get; private set; }
        public string ObjectName { get; private set; }
        public LockMode LockMode { get; private set; }
        internal string _ctid;
        internal Dictionary<string, bool> _primaryKeys = new Dictionary<string, bool>();
        public LockedRecord WaitFor { get; private set; }
        public string WaitForKeyDesc { get; private set; }

        public void FillWaitingRecord(IDbConnection connection)
        {
            try
            {
                if (string.IsNullOrEmpty(_ctid))
                {
                    return;
                }
                WaitFor = new LockedRecord(this, connection);
                if (WaitFor.PrimaryKeyIndex.Length == 0)
                {
                    WaitForKeyDesc = "(NoKey)";
                }
                else
                {
                    WaitForKeyDesc = "(" + string.Join(", ", WaitFor.PrimaryKeyIndex.Select(i => string.Format("{0}={1}", WaitFor.FieldNames[i], WaitFor.Values[i]))) + ")";
                }
            }
            finally
            {
                _ctid = null;
                _primaryKeys = null;
            }
        }

        public LockEntry(char relkind, string objectName, string[] keyflds, string lockMode, int? page, short? tuple)
        {
            ObjectKind = string.Empty;
            if (_relkindToObjectKind.TryGetValue(relkind, out string kind))
            {
                ObjectKind = kind;
            }
            ObjectName = objectName;
            LockMode = LockMode.None;
            if (_modeToLockMode.TryGetValue(lockMode, out LockMode mode))
            {
                LockMode = mode;
            }
            _ctid = null;
            if (relkind == 'r' && page.HasValue && tuple.HasValue)
            {
                _ctid = string.Format("'({0},{1})'", page.Value, tuple.Value);
            }
            if (!(keyflds is null))
            {
                foreach (string key in keyflds)
                {
                    _primaryKeys[key] = true;
                }
            }
        }

        public bool TryMerge(LockEntry other)
        {
            if (ObjectKind != other.ObjectKind || ObjectName != other.ObjectName || _ctid != other._ctid)
            {
                return false;
            }
            if (LockMode < other.LockMode)
            {
                LockMode = other.LockMode;
            }
            if (_ctid is null && !(other._ctid is null))
            {
                _ctid = other._ctid;
            }
            return true;
        }

        public override string ToString()
        {
            return string.Format("{0}({1})", ObjectName, LockMode);
        }

        public int CompareTo(object obj)
        {
            if (!(obj is LockEntry entry))
            {
                return -1;
            }
            int ret = string.Compare(ObjectName, entry.ObjectName);
            if (ret != 0)
            {
                return ret;
            }
            ret = string.Compare(ObjectKind, entry.ObjectKind);
            if (ret != 0)
            {
                return ret;
            }
            ret = string.Compare(_ctid, entry._ctid);
            if (ret != 0)
            {
                return ret;
            }
            ret = (int)LockMode - (int)entry.LockMode;
            return ret;
        }
    }

    public class NpgsqlLockInfo : ILock
    {
        public int Id { get; set; }
        public int Pid { get; set; }
        public int? WaitingId { get; set; }
        public ILock WaitingLock { get; set; }
        public DateTime? WaitSince { get; set; }
        internal List<ILock> _waited = new List<ILock>();
        public ILock[] Waited { get; private set; }

        public void AddToWaited(ILock lockInfo)
        {
            _waited.Add(lockInfo);
        }
        public string Query { get; set; }

        internal List<LockEntry> _entries = new List<LockEntry>();
        internal void Seal(IDbConnection connection)
        {
            Waited = _waited.ToArray();
            _waited = null;
            _entries.Sort();
            for (int i = _entries.Count - 1; 0 < i; i--)
            {
                if (_entries[i - 1].TryMerge(_entries[i]))
                {
                    _entries.RemoveAt(i);
                }
            }
            Locks = _entries.ToArray();
            _entries = null;
            foreach (LockEntry entry in Locks)
            {
                entry.FillWaitingRecord(connection);
            }
        }

        private LockEntry[] _locks;
        private string _lockDesc;
        public LockEntry[] Locks
        {
            get { return _locks; }
            private set
            {
                _locks = value;
                _lockDesc = string.Join(", ", _locks.Select(l => l.ToString()));
            }
        }
        public string LockDesc { get { return _lockDesc; } }

        public ISession Session { get; set; }

        public override string ToString()
        {
            return string.Format("Pid:{0} Query:{1} WaitingId:{2} WaitSince:{3}", Pid, Query, WaitingId, WaitSince);
        }
    }

    partial class NpgsqlDataSet
    {
        internal class PgStatActivity
        {
#pragma warning disable 0649
            public int pid;
            public string datname;
            public string usename;
            public string application_name;
            public string client_hostname;
            public string client_addr;
            public int? client_port;
            public string wait_event_type;
            public string wait_event;
            public string state;
#pragma warning restore 0649

            public PgStatActivity() { }

        }
        public override ISession[] GetSessions(IDbConnection connection)
        {
            List<ISession> l = new List<ISession>();
            NpgsqlConnection conn = connection as NpgsqlConnection;
            using (NpgsqlCommand cmd = new NpgsqlCommand(DataSet.Properties.Resources.PgStatActivity_SQL, conn))
            {
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    FieldInfo[] mapper = CreateMapper(reader, typeof(PgStatActivity));
                    while (reader.Read())
                    {
                        PgStatActivity obj = new PgStatActivity();
                        ReadObject(obj, reader, mapper);
                        l.Add(new NpgsqlSession(obj));
                    }
                }
            }
            return l.ToArray();
        }
        public Tuple<string, DateTime?> GetLastExecutedQuery(int pid, IDbConnection connection)
        {
            NpgsqlConnection conn = connection as NpgsqlConnection;
            using (NpgsqlCommand cmd = new NpgsqlCommand(DataSet.Properties.Resources.GetLastExecutedQuery_SQL, conn))
            {
                cmd.Parameters.Add(new NpgsqlParameter("pid", pid));
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string sql = reader.GetString(0);
                        DateTime? ts = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1).ToLocalTime();
                        return Tuple.Create(sql, ts);
                    }
                }
            }
            return new Tuple<string, DateTime?>(null, null);
        }
        public override async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
        {
            NpgsqlCommand cmd = command as NpgsqlCommand;
            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);
            return reader;
        }

        public class PgLock
        {
#pragma warning disable 0649
            public string locktype;
            public uint? database;
            public uint? relation;
            public string relationname;
            public char? relkind;
            public string[] keyflds;
            public int? page;
            public short? tuple;
            public string virtualxid;
            public uint? transactionid;
            public string virtualtransaction;
            public int? pid;
            public string mode;
            public bool granted;
            public DateTime? waitstart;
            public string query;
#pragma warning restore 0649

            public PgLock() { }

            public void ApplyTo(NpgsqlLockInfo info, Dictionary<int, NpgsqlLockInfo> txnToLock, Dictionary<int, ISession> pidToSession)
            {
                if (pid.HasValue)
                {
                    info.Pid = pid.Value;
                    info.Query = query;
                    if (pidToSession.TryGetValue(pid.Value, out ISession session))
                    {
                        info.Session = session;
                    }
                }
                switch (locktype)
                {
                    case "relation":
                    case "tuple":
                        if (!string.IsNullOrEmpty(relationname))
                        {
                            info._entries.Add(new LockEntry(relkind ?? '\0', relationname, keyflds, mode, page, tuple));
                        }
                        break;
                    case "transactionid":
                        if (transactionid.HasValue)
                        {
                            if (granted)
                            {
                                info.Id = (int)transactionid.Value;
                                txnToLock[info.Id] = info;
                            }
                            else
                            {
                                info.WaitingId = (int)transactionid.Value;
                                info.WaitSince = waitstart;
                            }
                        }
                        break;
                }
            }
        }

        public override ILock[] GetLocks(bool asTree, IDbConnection connection)
        {
            Dictionary<int, ISession> pidToSession = GetSessions(connection).ToDictionary(s => ((NpgsqlSession)s).Pid);
            List<ILock> l = new List<ILock>();
            Dictionary<string, NpgsqlLockInfo> vtxnToLock = new Dictionary<string, NpgsqlLockInfo>();
            Dictionary<int, NpgsqlLockInfo> txnToLock = new Dictionary<int, NpgsqlLockInfo>();
            NpgsqlConnection conn = connection as NpgsqlConnection;
            using (NpgsqlCommand cmd = new NpgsqlCommand(DataSet.Properties.Resources.PgLocks_SQL, conn))
            {
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    FieldInfo[] mapper = CreateMapper(reader, typeof(PgLock));
                    while (reader.Read())
                    {
                        PgLock obj = new PgLock();
                        ReadObject(obj, reader, mapper);
                        if (!vtxnToLock.TryGetValue(obj.virtualtransaction, out NpgsqlLockInfo info))
                        {
                            info = new NpgsqlLockInfo();
                            l.Add(info);
                            vtxnToLock.Add(obj.virtualtransaction, info);
                        }
                        obj.ApplyTo(info, txnToLock, pidToSession);
                    }
                }
            }
            List<ILock> result = new List<ILock>();
            foreach (NpgsqlLockInfo info in l)
            {
                bool isTop = true;
                if (info.WaitingId.HasValue && txnToLock.TryGetValue(info.WaitingId.Value, out NpgsqlLockInfo refinfo))
                {
                    info.WaitingLock = refinfo;
                    refinfo.AddToWaited(info);
                    if (asTree)
                    {
                        isTop = false;
                    }
                }
                if (isTop)
                {
                    result.Add(info);
                }
            }
            foreach (NpgsqlLockInfo info in l)
            {
                info.Seal(connection);
            }
            return result.ToArray();
        }
    }
}
