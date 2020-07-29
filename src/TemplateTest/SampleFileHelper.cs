using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TemplateTest {
    public class SampleFileHelper {
        public string GetSamplesFolder() {
            string asmFilepath = this.GetType().Assembly.Location;
            
            return Path.Combine(
                    Path.GetDirectoryName(asmFilepath),
                    "sample-files");
        }
    }
}
