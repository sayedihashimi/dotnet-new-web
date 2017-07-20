using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TemplatesShared {
    public class TemplateSearcher : ITemplateSearcher {
        public List<Template> Search(string searchTerm, List<TemplatePack> templatePacks) {
            if (string.IsNullOrWhiteSpace(searchTerm) ||
                            templatePacks == null || templatePacks.Count <= 0) {
                return null;
            }
            var matches = new List<Template>();
            foreach(var tp in templatePacks) {
                foreach(Template t in tp.Templates) {
                    var r = SearchTemplate(searchTerm, t);
                    if (r.IsMatch) {
                        t.SearchScore = r.SearchValue;
                        matches.Add(t);
                    }
                }
            }

            if(matches.Count <= 0) {
                return new List<Template>();
            }

            var retv = from m in matches
                       orderby m.SearchScore
                       select m;

            // TODO: Needs sorting

            // var sortedDict = from entry in myDict 
            //                  orderby entry.Value 
            //                  ascending select entry;



            // return matches.Keys;
            return retv.ToList();
        }

        /// <summary>
        /// This function will search the template for the given term.
        /// If there is a match the returned object will have IsMatch set to true.
        /// The SearchValue represents the "quality" of the match. The higher the number
        /// the better the match.
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        internal TemplateSearchResult SearchTemplate(string searchTerm, Template template) {
            // check all fields

            int score = 0;
            var nameRes = IsStringMatch(searchTerm, template.Name);
            if (nameRes.IsExactMatch)
                score += 1000;
            else if (nameRes.StartsWith)
                score += 200;
            else if (nameRes.IsPartialMatch)
                score += 100;

            var authorRes = IsStringMatch(searchTerm, template.Author);
            if (authorRes.IsExactMatch)
                score += 500;
            else if (authorRes.StartsWith)
                score += 100;
            else if (authorRes.IsPartialMatch)
                score += 50;

            var shortNameRes = IsStringMatch(searchTerm, template.ShortName);
            if (shortNameRes.IsExactMatch)
                score += 500;
            else if (shortNameRes.StartsWith)
                score += 100;
            else if (shortNameRes.IsPartialMatch)
                score += 50;

            var idRes = IsStringMatch(searchTerm, template.Identity);
            if (idRes.IsExactMatch)
                score += 500;
            else if (idRes.StartsWith)
                score += 100;
            else if (idRes.IsPartialMatch)
                score += 50;

            var groupIdRes = IsStringMatch(searchTerm, template.GroupIdentity);
            if (groupIdRes.IsExactMatch)
                score += 500;
            else if (groupIdRes.StartsWith)
                score += 100;
            else if (groupIdRes.IsPartialMatch)
                score += 50;

            return new TemplateSearchResult {
                IsMatch =  (score > 0),
                SearchValue = score
            };
        }
        internal TemplateMatchResult IsStringMatch(string search, string strToSearch) {
            if (search == null) {
                return new TemplateMatchResult();
            }
            if (strToSearch == null) {
                return new TemplateMatchResult();
            }

            if (search.Equals(strToSearch, StringComparison.OrdinalIgnoreCase)) {
                return new TemplateMatchResult {
                    IsExactMatch = true,
                    StartsWith = true
                };
            }

            if(strToSearch.IndexOf(search,StringComparison.OrdinalIgnoreCase) == 0) {
                var res = new TemplateMatchResult {
                    IsPartialMatch = true
                };
                if (strToSearch.StartsWith(search, StringComparison.OrdinalIgnoreCase)) {
                    res.StartsWith = true;
                }
                return res;
            }

            return new TemplateMatchResult();
        }
    }

    internal class TemplateSearchResult : ITemplateSearchResult {
        public bool IsMatch { get; set; }
        public int SearchValue { get; set; } = 0;
    }
    internal class TemplateMatchResult {
        public bool IsExactMatch { get; set; } = false;
        public bool IsPartialMatch { get; set; } = false;
        public bool StartsWith { get; set; }
    }
}
