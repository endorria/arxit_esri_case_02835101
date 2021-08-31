using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ProAppModule1
{
    internal class AppConfig
    {
        private const string FILE_NAME = "Parameters.config";


        private static System.Configuration.Configuration _config;

        static AppConfig()
        {
            ExeConfigurationFileMap map = new ExeConfigurationFileMap();
            map.ExeConfigFilename = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + FILE_NAME;
            _config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
        }

        public static string GetValue(string key)
        {
            if (!_config.AppSettings.Settings.AllKeys.Contains(key)) return null;

            return _config.AppSettings.Settings[key].Value;
        }
    }
}
