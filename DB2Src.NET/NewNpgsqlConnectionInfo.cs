using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class NewNpgsqlConnectionInfo : NpgsqlConnectionInfo
    {
        public NewNpgsqlConnectionInfo(bool fromSetting) : base()
        {
            if (fromSetting)
            {
                ServerName = App.Hostname;
                ServerPort = App.Port;
                DatabaseName = App.Database;
                UserName = App.Username;
            }
            Name = Properties.Resources.NewConnectionTitle;
        }
    }
}
