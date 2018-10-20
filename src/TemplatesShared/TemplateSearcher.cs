using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TemplatesShared {
    public class TemplateSearcher : ITemplateSearcher {
        public TemplatePack FindTemplatePackByName(string name, List<TemplatePack> templatePacks) {
            if (string.IsNullOrWhiteSpace(name) || templatePacks == null || templatePacks.Count <= 0) {
                return null;
            }

            foreach (var tp in templatePacks) {
                if (string.Equals(name, tp.Package, StringComparison.OrdinalIgnoreCase)) {
                    return tp;
                }
            }

            return null;
        }
        public TemplatePack FindTemplatePackFor(Template template, List<TemplatePack> templatePacks) {
            if (template == null || templatePacks == null || templatePacks.Count <= 0) {
                return null;
            }

            return FindTemplatePackByName(template.TemplatePackId, templatePacks);
        }
        public Template GetTemplateById(string templateName, IEnumerable<TemplatePack> templatePacks) {
            if (templateName == null || templatePacks == null || templatePacks.Count() <= 0) {
                return null;
            }
            foreach (var tp in templatePacks) {
                foreach (var t in tp.Templates) {
                    if (string.Equals(templateName, t.Identity, StringComparison.OrdinalIgnoreCase)) {
                        return t;
                    }
                }
            }

            return null;
        }
        public Template GetTemplateById(string templateName, TemplatePack templatePack) {
            return GetTemplateById(templateName, new List<TemplatePack> { templatePack });
        }
        public List<Template> Search(string searchTerm, List<TemplatePack> templatePacks) {
            if (searchTerm == null) {
                searchTerm = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(searchTerm)) {
                return GetAllTemplates(templatePacks);
            }
            if (string.IsNullOrWhiteSpace(searchTerm) ||
                            templatePacks == null || templatePacks.Count <= 0) {
                return null;
            }

            searchTerm = searchTerm.Trim();

            var matches = new List<Template>();
            foreach (var tp in templatePacks) {
                foreach (Template template in tp.Templates) {
                    var searchResult = SearchTemplate(searchTerm, template, tp);
                    if (searchResult.IsMatch) {
                        template.SearchScore = searchResult.SearchValue;
                        if (string.IsNullOrEmpty(template.TemplatePackId)) {
                            template.TemplatePackId = tp.Package;
                        }
                        matches.Add(template);
                    }
                }
            }

            if (matches.Count <= 0) {
                return new List<Template>();
            }

            var retv = from m in matches
                       orderby m.SearchScore descending
                       select m;

            // return matches.Keys;
            return retv.ToList();
        }

        public IList<Template> SearchByAuthor(string author, List<TemplatePack> templatePacks) {
            if (author == null) {
                author = string.Empty;
            }
            if (string.IsNullOrWhiteSpace(author) || templatePacks == null || templatePacks.Count <= 0) {
                return GetAllTemplates(templatePacks);
            }
            var matches = new List<Template>();
            foreach (var tp in templatePacks) {
                foreach (var template in tp.Templates) {
                    var searchResult = SearchTemplateByAuthor(author, template, tp);
                    if (searchResult.IsMatch) {
                        template.SearchScore = searchResult.SearchValue;
                        if (string.IsNullOrEmpty(template.TemplatePackId)) {
                            template.TemplatePackId = tp.Package;
                        }
                        matches.Add(template);
                    }
                }
            }


            if (matches.Count <= 0) {
                return new List<Template>();
            }

            var returnResult = (from m in matches
                                orderby m.SearchScore descending
                                select m).ToList();

            return returnResult;
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
        internal TemplateSearchResult SearchTemplate(string searchTerm, Template template, TemplatePack pack) {
            int score = 0;
            // check all fields in template
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

            // to avoid nullref error
            if (template.Tags == null) {
                template.Tags = new Dictionary<string, string>();
            }

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

            // see if there is a match with the TemplatePack itself
            var packAuthorMatch = IsStringMatch(searchTerm, pack.Authors);
            if (packAuthorMatch.IsExactMatch || packAuthorMatch.StartsWith || packAuthorMatch.IsPartialMatch)
                score += 10;

            var copyMatch = IsStringMatch(searchTerm, pack.Copyright);
            if (copyMatch.IsExactMatch || copyMatch.StartsWith || copyMatch.IsPartialMatch)
                score += 10;

            var descMatch = IsStringMatch(searchTerm, pack.Description);
            if (descMatch.IsExactMatch || descMatch.StartsWith || descMatch.IsPartialMatch)
                score += 10;

            var ownerMatch = IsStringMatch(searchTerm, pack.Owners);
            if (ownerMatch.IsExactMatch || ownerMatch.StartsWith || ownerMatch.IsPartialMatch)
                score += 10;

            var pkgMatch = IsStringMatch(searchTerm, pack.Package);
            if (pkgMatch.IsExactMatch || pkgMatch.StartsWith || pkgMatch.IsPartialMatch)
                score += 10;

            var projUrlMatch = IsStringMatch(searchTerm, pack.ProjectUrl);
            if (projUrlMatch.IsExactMatch || projUrlMatch.StartsWith || projUrlMatch.IsPartialMatch)
                score += 10;

            return new TemplateSearchResult {
                IsMatch = (score > 0),
                SearchValue = score
            };
        }

        internal TemplateSearchResult SearchTemplateByAuthor(string author, Template template, TemplatePack pack) {
            int score = 0;

            var authorRes = IsStringMatch(author, template.Author);
            if (authorRes.IsExactMatch)
                score += 500;
            else if (authorRes.StartsWith)
                score += 100;
            else if (authorRes.IsPartialMatch)
                score += 50;

            // see if there is a match with the TemplatePack itself
            var packAuthorMatch = IsStringMatch(author, pack.Authors);
            if (packAuthorMatch.IsExactMatch) {
                score += 75;
            }
            else if (packAuthorMatch.StartsWith) {
                score += 40;
            }
            else if (packAuthorMatch.IsPartialMatch) {
                score += 20;
            }

            var packOwnerMatch = IsStringMatch(author, pack.Owners);
            if (packOwnerMatch.IsExactMatch) {
                score += 70;
            }
            else if (packOwnerMatch.StartsWith) {
                score += 35;
            }
            else if (packOwnerMatch.IsPartialMatch) {
                score += 15;
            }

            return new TemplateSearchResult {
                IsMatch = (score > 0),
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
            foreach (var m in matches) {
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

            if (strToSearch.IndexOf(search, StringComparison.OrdinalIgnoreCase) == 0) {
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
            foreach (var str in strToSearch) {
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
