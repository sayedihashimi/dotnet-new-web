using System;
namespace DotnetNewMobile
{
    public interface ISaveAndLoad
    {
        void SaveText(string filename, string text);
        string LoadText(string filename);
        void DownloadAndSave(string url, string filename);
        bool DoesFileExist(string filename);
    }
}
