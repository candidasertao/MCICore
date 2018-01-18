using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace MoneoTaskEnvio
{
    public static class EventViewLog
    {
        static string source = "MoneoTaskEnvio";

        public static void Write(Exception err)
        {
            /*
            if (!EventLog.SourceExists(source))
                EventLog.CreateEventSource(source, "Fornecedor");
            
            EventLog.WriteEntry(source, err.Message, EventLogEntryType.Warning, 234);*/
        }
    }
}
