using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scissors
{
    [Serializable]
    public class FatalEngineException : Exception
    {
        public ErrorCode Code { get; private set; }

        public FatalEngineException(string message, ErrorCode code)
            : base(message)
        {
            Code = code;
        }

        public override string ToString()
        {
            return "\{GetType().FullName}: [\{Code}] \{Message}";
        }
    }
}
