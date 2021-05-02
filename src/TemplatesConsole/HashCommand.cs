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
                CommandOptionType.SingleValue
                );

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
                sb.AppendLine("Identity,hash,hashLower,idhash,idhashLower,groupidhash,groupidHashLower");
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
                        sb.AppendLine($"{template.Identity},{hash},{hashLower},{idhash},{idhashLower},{groupIdHash},{groupIdHashLower}");
                    }
                }
                File.WriteAllText(destFile, sb.ToString());
                return 1;
            };
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
    }
}
