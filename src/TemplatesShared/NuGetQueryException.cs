using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesShared {

    [Serializable]
    public class NuGetQueryException : Exception {
        public NuGetQueryException() { }
        public NuGetQueryException(string message) : base(message) { }
        public NuGetQueryException(string message, Exception inner) : base(message, inner) { }
        protected NuGetQueryException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
