using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesShared {

    [Serializable]
    public class TemplateInitException : Exception {
        public TemplateInitException() { }
        public TemplateInitException(string message) : base(message) { }
        public TemplateInitException(string message, Exception inner) : base(message, inner) { }
        protected TemplateInitException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
