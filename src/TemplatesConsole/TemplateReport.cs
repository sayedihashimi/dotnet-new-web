using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using TemplatesShared;

namespace TemplatesConsole {
    //public class TemplateReport {
    //    private INuGetHelper _nugetHelper;
    //    private HttpClient _httpClient;
    //    public TemplateReport(INuGetHelper nugetHelper, HttpClient httpClient) {
    //        Debug.Assert(nugetHelper != null);
    //        Debug.Assert(httpClient != null);

    //        _nugetHelper = nugetHelper;
    //        _httpClient = httpClient;
    //    }
    //    public void GenerateTemplateJsonReport(string[]searchTerms,string jsonReportFilepath) {
    //        Debug.Assert(searchTerms != null && searchTerms.Length > 0);
    //        Debug.Assert(!string.IsNullOrEmpty(jsonReportFilepath));

    //        // 1: query nuget for search results
    //        var foundPackages = _nugetHelper.QueryNuGetAsync(_httpClient, searchTerms, null);
    //        // 2: download nuget packages locally
    //        // 3: extract nuget package to local folder
    //        // 4: look into extract folder for a template json file


    //        throw new NotImplementedException();
    //    }

    //    protected string[] GetPackagesToIgnore() {
    //        return 
    //    }
    //}
}
