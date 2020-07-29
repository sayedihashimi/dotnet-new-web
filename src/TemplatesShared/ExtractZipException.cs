using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesShared {

    [Serializable]
    public class ExtractZipException : Exception {
        public ExtractZipException() { }
        public ExtractZipException(string message) : base(message) { }
        public ExtractZipException(string message, Exception inner) : base(message, inner) { }
        protected ExtractZipException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
