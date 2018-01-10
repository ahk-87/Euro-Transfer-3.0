using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Euro_Transfer.Classes
{

    [Serializable]
    public class NoInternetException : Exception
    {
        public NoInternetException() { }
        public NoInternetException(string message) : base(message) { }
        public NoInternetException(string message, Exception inner) : base(message, inner) { }
        protected NoInternetException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
