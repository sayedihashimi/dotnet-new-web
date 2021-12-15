using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using TemplatesShared;

namespace TemplatesConsole {
    public class HashCommand : TemplateCommand {
        public HashCommand() : base(){
            Name = "hash";
            Description = "creates the report of template ids and their hashes";
        }
        public override void Setup(CommandLineApplication command) {
            base.Setup(command);
            var optionTemplateReportFilepath = command.Option<string>(
                "-trf|--template-report-file",
                "path to the template-report.json file",
                CommandOptionType.SingleValue);

            var optionCsvOutputPath = command.Option<string>(
                "-cop|--csv-output-path",
                "output path for the csv file, default is to create the file in the current directory",
                CommandOptionType.SingleValue);

            var optionTemplateCacheFilepath = command.Option<string>(
                "-tcf|--template-cache-file",
                "path to the templatecache.json this is ususally typically found in subfolders under %HOMEPATH%/.templateengine/dotnetcli",
                CommandOptionType.SingleValue);

            OnExecute = () => {
                var reportFilepath = optionTemplateReportFilepath.HasValue() ?
                                        optionTemplateReportFilepath.Value() :
                                        null;
                if (string.IsNullOrEmpty(reportFilepath)) {
                    throw new ArgumentNullException("template-report-file parameter missing");
                }

                var destPath = optionCsvOutputPath.HasValue() ?
                                optionCsvOutputPath.Value() :
                                Path.Combine(Directory.GetCurrentDirectory(), $"template-result.{DateTime.Now.ToString("MM.dd.yy-H.m.s.ffff")}.csv");

                var destFile = destPath;

                if (!Path.HasExtension(destFile)) {
                    destFile = Path.Combine(destFile, $"template-result.{DateTime.Now.ToString("MM.dd.yy-H.m.s.ffff")}.csv");
                }

                //var destFile = Path.Combine(Directory.GetCurrentDirectory(), $"template-result.{DateTime.Now.ToString("MM.dd.yy-H.m.s.ffff")}.csv");
                
                Console.WriteLine($"destFile: {destFile}");
                var sb = new StringBuilder();
                // var destFile = 
                // load the json file
                var templatePack = TemplatePack.CreateFromFile(reportFilepath);
                sb.AppendLine("Identity,hash,hashLower,idhash,idhashLower,groupidhash,groupidHashLower,clihash,cliUpperHash,cliLowerHash");
                foreach(var tp in templatePack) {
                    foreach(var template in tp.Templates) {
                        var hashInfo = new TemplateHashInfo {
                            GroupIdentity = template.GroupIdentity,
                            Identity = template.Identity,
                            Language = template.GetLanguage()
                        };

                        var hash = TemplateHashExtensions.EncodeAndGenerateHash(hashInfo);
                        var hashLower = TemplateHashExtensions.EncodeAndGenerateHash(hashInfo,true);
                        var idhash = TemplateHashExtensions.GenerateHash(template.Identity);
                        var idhashLower = TemplateHashExtensions.GenerateHash(template.Identity != null? template.Identity.ToLowerInvariant():string.Empty);
                        var groupIdHash = TemplateHashExtensions.GenerateHash(template.GroupIdentity);
                        var groupIdHashLower = TemplateHashExtensions.GenerateHash(template.GroupIdentity != null ? template.GroupIdentity.ToLowerInvariant() : string.Empty);
                        var cliHash = TemplateHashExtensions.GenerateHashForCli(template.Identity);
                        var cliUpperHash = TemplateHashExtensions.GenerateHashForCliWithNormalizedCasing(template.Identity);
                        var cliLowerHash = TemplateHashExtensions.GenerateHashForCliWithNormalizedCasing(template.Identity, false);
                        sb.AppendLine($"{template.Identity},{hash},{hashLower},{idhash},{idhashLower},{groupIdHash},{groupIdHashLower},{cliHash},{cliUpperHash},{cliLowerHash}");
                    }
                }

                var templateCacheFilepath = optionTemplateCacheFilepath.HasValue() ?
                                                optionTemplateCacheFilepath.Value() :
                                                null;
                var idsFromTemplateCacheFile = GetIdsFromTemplateCache(templateCacheFilepath);
                if(idsFromTemplateCacheFile == null && idsFromTemplateCacheFile.Count > 0)
                {
                    foreach(var id in idsFromTemplateCacheFile)
                    {
                        var hash = string.Empty;
                        var hashLower = string.Empty;
                        var idhash = TemplateHashExtensions.GenerateHash(id);
                        var idhashLower = TemplateHashExtensions.GenerateHash(id.ToLowerInvariant());
                        var groupIdHash = string.Empty;
                        var groupIdHashLower = string.Empty;
                        var cliHash = TemplateHashExtensions.GenerateHashForCli(id);
                        var cliUpperHash = TemplateHashExtensions.GenerateHashForCliWithNormalizedCasing(id);
                        var cliLowerHash = TemplateHashExtensions.GenerateHashForCliWithNormalizedCasing(id, false);
                        sb.AppendLine($"{id},{hash},{hashLower},{idhash},{idhashLower},{groupIdHash},{groupIdHashLower},{cliHash},{cliUpperHash},{cliLowerHash}");
                    }
                }

                File.WriteAllText(destFile, sb.ToString());
                return 1;
            };
        }
        private List<string> GetIdsFromTemplateCache(string templateCacheFilepath)
        {
            if (templateCacheFilepath != null)
            {
                if (File.Exists(templateCacheFilepath))
                {
                    var tcf = new TemplateCacheFile(templateCacheFilepath);
                    return tcf.GetIdenties();
                }
                else
                {
                    Console.WriteLine($"ERROR: templateCacheFile not found at '{templateCacheFilepath}'");
                }
            }

            return null;
        }
    }
    
    public class TemplateHashInfo {
        public string Identity { get; set; }
        public string GroupIdentity { get; set; }
        public string Language { get; set; }

        public override string ToString() {
            return GroupIdentity ?? Identity;
        }
    }
    public static class TemplateHashExtensions {
        internal static string EncodeAndGenerateHash(TemplateHashInfo template, bool toLowercase=false) {
            // We form up a fake group identity for handling templates which just have an identity but no group id.
            string identity = template.GroupIdentity;
            if (string.IsNullOrEmpty(identity)) {
                identity = $"__{template.Identity}";
            }

            if (toLowercase) {
                return EncodeAndGenerateHash(identity.ToLowerInvariant(), (template.Language ?? string.Empty).ToLowerInvariant());
            }
            else {
                return EncodeAndGenerateHash(identity, template.Language ?? string.Empty);
            }
            
        }

        internal static string EncodeAndGenerateHash(string id, string language) => EncodeTemplateId(id, language).GenerateHash();

        // We encode the identity and language together, since VS wants distinct templates and ids per language
        internal static string EncodeTemplateId(string id, string language) => $"{id}{(!string.IsNullOrEmpty(language) ? "``" + language : string.Empty)}";

        internal static string GenerateHash(this string text) {
            if (text == null) {
                return null;
            }

            using (SHA256 sha = SHA256.Create()) {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                byte[] hash = sha.ComputeHash(bytes);
                StringBuilder hashString = new StringBuilder();
                foreach (byte x in hash) {
                    hashString.AppendFormat("{0:x2}", x);
                }
                return hashString.ToString();
            }
        }

        #region copied from template engine
        // https://github.com/dotnet/templating/blob/e1325e22e2f9cb7c13e5af80eb988a2c5a5dc482/src/Microsoft.TemplateEngine.Cli/TelemetryHelper.cs#L46-L81
        internal static string GenerateHashForCli(this string text)
        {
            var sha256 = SHA256.Create();
            return HashInFormat(sha256, text);

        }
        internal static string GenerateHashForCliWithNormalizedCasing(string text, bool upper = true)
        {
            if (text == null)
            {
                return null;
            }

            return GenerateHashForCli(upper == true ? text.ToUpper() : text.ToLower());
        }

        private static string HashInFormat(SHA256 sha256, string text)
        {
            if (text == null)
            {
                return null;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(text);
            byte[] hash = sha256.ComputeHash(bytes);
            StringBuilder hashString = new StringBuilder();
            foreach (byte x in hash)
            {
                hashString.AppendFormat("{0:x2}", x);
            }
            return hashString.ToString();
        }
        #endregion
    }
}
