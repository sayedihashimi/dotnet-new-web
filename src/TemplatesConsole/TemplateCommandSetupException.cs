using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesConsole {

    [Serializable]
    public class TemplateCommandSetupException : Exception {
        public TemplateCommandSetupException() { }
        public TemplateCommandSetupException(string message) : base(message) { }
        public TemplateCommandSetupException(string message, Exception inner) : base(message, inner) { }
        protected TemplateCommandSetupException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
