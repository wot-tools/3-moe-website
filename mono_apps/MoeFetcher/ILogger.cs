using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeFetcher
{
    interface ILogger
    {
        void VVVerbose(string message);
        void VVVerbose(string format, params object[] args);

        void VVerbose(string message);
        void VVerbose(string format, params object[] args);

        void Verbose(string message);
        void Verbose(string format, params object[] args);

        void Info(string message);
        void Info(string format, params object[] args);

        void Error(string message);
        void Error(string format, params object[] args);

        void CriticalError(string message);
        void CriticalError(string format, params object[] args);
    }
}
