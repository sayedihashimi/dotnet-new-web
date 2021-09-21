using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesShared {
    public class VsixAnalyzer {
        /// <summary>
        /// Things to check for:
        /// 1. Ensure there is a pkgdef file with something like the following
        ///     [$RootKey$\TemplateEngine\Templates\sayedha.template.netcoretool.nuspec\1.0.0]
        ///     "InstalledPath"="$PackageFolder$\ProjectTemplates"
        ///    To get the list of .pkgdef files to check look for <assets> tag in the .vsixmanifest
        /// 2. Ensure that there is a .nupkg file that contains 1 or more templates
        /// 
        /// 
        /// </summary>
        /// <param name="pathToVsix"></param>
        /// <returns></returns>
        bool Analyze(string pathToVsix) {
            throw new NotImplementedException();
        }
    }
}
