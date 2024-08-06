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
            using (NpgsqlCommand cmd = new NpgsqlCommand(string.Format("select pg_cancel_backend({0})", Pid), conn))
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
            using (NpgsqlCommand cmd = new NpgsqlCommand(string.Format("select pg_terminate_backend({0})", Pid), conn))
            {
                return (bool)cmd.ExecuteScalar();
            }
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
                NpgsqlDataReader reader = cmd.ExecuteReader();
                FieldInfo[] mapper = CreateMapper(reader, typeof(PgStatActivity));
                while (reader.Read())
                {
                    PgStatActivity obj = new PgStatActivity();
                    ReadObject(obj, reader, mapper);
                    l.Add(new NpgsqlSession(obj));
                }
            }
            return l.ToArray();
        }
        public string GetLastExecutedQuery(int pid, IDbConnection connection)
        {
            NpgsqlConnection conn = connection as NpgsqlConnection;
            using (NpgsqlCommand cmd = new NpgsqlCommand(DataSet.Properties.Resources.GetLastExecutedQuery_SQL, conn))
            {
                cmd.Parameters.Add(new NpgsqlParameter("pid", pid));
                NpgsqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return reader.GetString(0);
                }
            }
            return null;
        }
        public override async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
        {
            NpgsqlCommand cmd = command as NpgsqlCommand;
            NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);
            return reader;
        }
    }
}
