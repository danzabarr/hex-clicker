using System;
using System.IO;

namespace HexClicker.IO
{
    [System.Serializable]
    public sealed class SaveData
    {
        public string Path { get; private set; }

        public bool Save()
        {
            return Utils.Save(this, Path);
        }

        public bool SaveAs(string path, bool overwrite)
        {
            if (!overwrite)
            {
                if (File.Exists(path))
                    return false;
            }

            if (!Utils.Save(this, path))
                return false;

            Path = path;
            return true;
        }

        public static bool Load(string path, out SaveData data)
        {
            if (!Utils.Load(path, out data))
                return false;

            data.Path = path;
            return true;
        }
    }
}
