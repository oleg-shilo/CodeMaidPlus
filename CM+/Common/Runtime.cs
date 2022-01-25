using Newtonsoft.Json;
using System;
using System.IO;

// using System.Web.Script.Serialization;

namespace CMPlus
{
    public static class Runtime
    {
        static string settingsFile = Environment.SpecialFolder.LocalApplicationData.PathCombine("CM+", "cm+.json");

        static Settings settings;

        static public Settings Settings
        {
            get
            {
                if (settings == null)
                    settings = LoadSettings();
                return settings;
            }

            set
            {
                settings = value;
            }
        }

        static public Settings LoadSettings()
        {
            try
            {
                if (File.Exists(settingsFile))
                {
                    var json = File.ReadAllText(settingsFile);
                    return JsonConvert.DeserializeObject<Settings>(json);
                    //return new JavaScriptSerializer().Deserialize<Settings>(json);
                }
            }
            catch { }
            return new Settings();
        }

        static public Settings Save(this Settings settings)
        {
            try
            {
                settingsFile.GetDidName().EnsureDirExis();
                File.WriteAllText(settingsFile, JsonConvert.SerializeObject(settings));
                // File.WriteAllText(settingsFile, new JavaScriptSerializer().Serialize(settings));
            }
            catch { }
            return settings;
        }
    }
}