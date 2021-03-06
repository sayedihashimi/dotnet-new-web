﻿using System;
using System.Collections.Generic;
using System.Reflection;
using TemplatesShared;

namespace DotnetNewMobile
{
    public class TemplateHelper
    {
        
        public string GetTemplateJsonFileContents(string filename="dotnetnew-template-report.json")
        {
            // if the file exists locally use that file instead of the built in one
            var fileHelper = new SaveAndLoad();
            if (fileHelper.DoesFileExist(filename))
            {
                try
                {
                    return fileHelper.LoadText(filename);
                }
                catch
                {
                }
            }

            // fallback to embedded file
            string resxname = $"DotnetNewMobile.iOS.Assets.template-report.json";
            var assembly = typeof(SearchPageViewModel).GetTypeInfo().Assembly;
            string text = null;
            using (var stream = assembly.GetManifestResourceStream(resxname))
            using (var reader = new System.IO.StreamReader(stream))
            {
                text = reader.ReadToEnd();
            }
            return text;

        }

        public List<TemplatePack> GetTemplatePacks()
        {
            var text = GetTemplateJsonFileContents();
            return TemplatePack.CreateFromText(text);
        }

        public TemplatePack GetTemplatePackById(string package, List<TemplatePack>packs){
            TemplatePack pack = null;
            foreach(var p in packs){
                if(string.Compare(package, p.Package,StringComparison.OrdinalIgnoreCase) == 0){
                    pack = p;
                    break;
                }
            }
            return pack;
        }
    }

}
