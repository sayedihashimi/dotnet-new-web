using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TemplatesShared.Extensions {
    public static class ListExtensions {
        public static bool HasDuplicates<T>(this List<T> list) {
            if(list.Count <= 1) {
                return false;
            }

            var dupes = list.GroupBy(x => x)
                            .Where(g => g.Count() > 1)
                            .Select(x=>x.Key);

            return false;
        }
        public static List<T> GetDuplicates<T>(this List<T> list) {
            if (list.Count <= 1) {
                return null;
            }

            var dupes = list.GroupBy(x => x)
                            .Where(g => g.Count() > 1)
                            .Select(x => x.Key);

            if(dupes == null || dupes.Count() <= 1) {
                return null;
            }

            return dupes.ToList();
        }
    }
}
