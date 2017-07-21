using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TemplatesShared {
    public class TemplateSearcher : ITemplateSearcher {
        public List<Template> Search(string searchTerm, List<TemplatePack> templatePacks) {
            if (string.IsNullOrWhiteSpace(searchTerm)) {
                return GetAllTemplates(templatePacks);
            }
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
        private List<Template> GetAllTemplates(IEnumerable<TemplatePack> templatePacks) {
            List<Template> result = new List<Template>();
            foreach (var tp in templatePacks) {
                result.AddRange(tp.Templates);
            }
            return result;
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

            var tagKeysRes = IsStringMatch(searchTerm, template.Tags.Keys);
            var tagValuesRes = IsStringMatch(searchTerm, template.Tags.Values);

            var tagRes = Combine(
                            IsStringMatch(searchTerm, template.Tags.Keys), 
                            IsStringMatch(searchTerm, template.Tags.Values));
            if (tagRes.IsExactMatch)
                score += 250;
            else if (tagRes.StartsWith)
                score += 50;
            else if (tagRes.IsPartialMatch)
                score += 25;

            var classRes = IsStringMatch(searchTerm, template.Classifications);
            if (classRes.IsExactMatch)
                score += 250;
            else if (classRes.StartsWith)
                score += 50;
            else if (classRes.IsPartialMatch)
                score += 25;

            return new TemplateSearchResult {
                IsMatch =  (score > 0),
                SearchValue = score
            };
        }
        internal TemplateMatchResult Combine(TemplateMatchResult match1, TemplateMatchResult match2) {
            if (match1 == null)
                throw new ArgumentNullException("match1");
            if (match2 == null)
                throw new ArgumentNullException("match2");
            
            return new TemplateMatchResult {
                IsExactMatch = (match1.IsExactMatch || match2.IsExactMatch),
                IsPartialMatch = (match1.IsPartialMatch || match2.IsPartialMatch),
                StartsWith = (match1.StartsWith || match2.StartsWith)
            };
        }
        internal TemplateMatchResult Combine(IEnumerable<TemplateMatchResult> matches) {
            if (matches == null || matches.Count() <= 0)
                throw new ArgumentNullException("matches");

            var result = new TemplateMatchResult();
            foreach(var m in matches) {
                result = Combine(result, m);
            }
            return result;
        }
        internal TemplateMatchResult IsStringMatch(string search, string strToSearch) {
            if (search == null || strToSearch == null) {
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

        internal TemplateMatchResult IsStringMatch(string search, IEnumerable<string> strToSearch) {
            if (string.IsNullOrWhiteSpace(search) || strToSearch == null || strToSearch.Count() <= 0) {
                return new TemplateMatchResult();
            }

            var result = new TemplateMatchResult();
            List<TemplateMatchResult> matches = new List<TemplateMatchResult>();
            foreach(var str in strToSearch) {
                result = Combine(result, IsStringMatch(search, str));
            }

            return result;
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
