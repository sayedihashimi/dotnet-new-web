using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesShared {

    [Serializable]
    public class JsonException : Exception {
        public JsonException() { }
        public JsonException(string message) : base(message) { }
        public JsonException(string message, Exception inner) : base(message, inner) { }
        protected JsonException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
