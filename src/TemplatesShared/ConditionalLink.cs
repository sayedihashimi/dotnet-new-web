using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesShared {
    public class ConditionalLink {
        public ConditionalLink() :this(false, null) { }
        public ConditionalLink(bool condition, string linkHref) {
            IsEnabled = condition;
            LinkHref = linkHref;
        }
        public bool IsEnabled { get; set; }
        public string LinkHref { get; set; } = "#";
    }
}
