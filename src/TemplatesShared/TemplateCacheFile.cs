using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplatesShared
{
    public class TemplateCacheFile
    {
        public TemplateCacheFile() { }
        public TemplateCacheFile(string filepath) : this()
        {
            Filepath = filepath;
        }

        private JObject _cachefileobj;
        public string Filepath { get; set; }


        private void LoadJsonIfNeeded()
        {
            // TODO: Not thread safe, but it's not currently being used in an unsafe manner.
            if(_cachefileobj == null)
            {
                _cachefileobj = JObject.Parse(File.ReadAllText(Filepath));
            }
        }

        public List<string>GetIdenties()
        {
            LoadJsonIfNeeded();

            var foundIdList = new List<string>();

            JArray templateInfo = null;
            templateInfo = _cachefileobj != null ? (JArray)_cachefileobj["TemplateInfo"] : null;
            if (templateInfo != null)
            {
                foreach(JObject child in templateInfo)
                {
                    if (child.ContainsKey("Identity"))
                    {
                        var id = child["Identity"]?.ToString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            foundIdList.Add(id);
                        }
                    }
                }
            }

            return foundIdList;
            // throw new NotImplementedException();
        }
    }
}
