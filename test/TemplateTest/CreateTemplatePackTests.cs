using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TemplatesShared;
using Xunit;

namespace TemplateTest
{
    public class CreateTemplatePackTests
    {
        private string GetTestFilepath(string filename)
        {
            string rundir = new FileInfo(new System.Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory.FullName;
            string filepath = Path.Combine(rundir, @"files", filename);
            return filepath;
        }
        [Fact]
        public void TestCreateFromFile()
        {
            string filepath = GetTestFilepath(@"template-report01.json");
            IList<TemplatePack> result = TemplatePack.CreateFromFile(filepath);
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }
        
        [Fact]
        public void TestCreateFromText()
        {
            string filepath = GetTestFilepath(@"template-report01.json");
            string str = File.ReadAllText(filepath);
            IList<TemplatePack> result = TemplatePack.CreateFromText(str);
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
        }
    }
}
