﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    class Program
    {
        static void Main(string[] args)
        {
            ExportTable.AnalyzeArguments(args);
            ExportTable.Execute();
        }
    }
}
