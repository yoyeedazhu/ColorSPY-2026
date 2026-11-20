using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace ColorSpy
{
    public class SpySettings
    {
        public int X;
        public int Y;
        public bool HasPos;
        public bool TopMost = true;
        public int LastColor;
    }
    public static class Store
    {
        public static string Dir
        {
            get
            {
                string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ColorSPY2026");
                Directory.CreateDirectory(d);
                return d;
            }
        }

        public static string PathFile
        {
            get { return Path.Combine(Dir, "settings.json"); }
        }

        public static SpySettings Load()
        {
            try
            {
                if (!File.Exists(PathFile)) return new SpySettings();
                JavaScriptSerializer ser = new JavaScriptSerializer();
                SpySettings s = ser.Deserialize<SpySettings>(File.ReadAllText(PathFile, Encoding.UTF8));
                return s ?? new SpySettings();
            }
            catch
            {
                return new SpySettings();
            }
        }

        public static void Save(SpySettings s)
        {
            try
            {
                JavaScriptSerializer ser = new JavaScriptSerializer();
                string tmp = PathFile + ".tmp";
                File.WriteAllText(tmp, ser.Serialize(s), new UTF8Encoding(false));
                if (File.Exists(PathFile)) File.Delete(PathFile);
                File.Move(tmp, PathFile);
            }
            catch
            {
            }
        }
    }
}
