using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace MyORM
{
    internal class Log
    {
        public static readonly ILog loginfo = LogManager.GetLogger("myorm.op"); //操作记录
        public static readonly ILog logerror = LogManager.GetLogger("myorm.er"); //错误记录

        public static void WriteInfo(string info)
        {
            loginfo.Info(info);
        }

        public static void WriteError(string error)
        {
            logerror.Error(error);
        }
    }
}
