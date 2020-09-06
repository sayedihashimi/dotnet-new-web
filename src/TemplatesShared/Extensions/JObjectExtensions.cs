using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesShared.Extensions
{
    public static class JObjectExtensions
    {
        public static T GetValueOrDefault<T>(this JObject obj, string key, T defaultValue)
        {
            if (obj.ContainsKey(key))
            {
                return obj[key].Value<T>();
            }
            return defaultValue;
        }
    }
}
