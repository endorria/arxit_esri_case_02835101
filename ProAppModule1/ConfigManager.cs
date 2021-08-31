using ArcGIS.Core.Data;
using System;

namespace ProAppModule1
{
    internal class ConfigManager
    {
        private static DatabaseConnectionFile _dbConnectionOriginal;
        private static FileGeodatabaseConnectionPath _gdbConnectionOriginal;
        private static DatabaseConnectionFile _dbConnectionHisto;
        private static FileGeodatabaseConnectionPath _gdbConnectionHisto;
        public static Geodatabase ConnectToGeoDatabase(bool isOriginalDb)
        {
            if (isOriginalDb)
            {
                if (_dbConnectionOriginal != null)
                {
                    return new Geodatabase(_dbConnectionOriginal);
                }
                else if (_gdbConnectionOriginal != null)
                {
                    return new Geodatabase(_gdbConnectionOriginal);
                }
            }
            else
            {
                if (_dbConnectionHisto != null)
                {
                    return new Geodatabase(_dbConnectionHisto);
                }
                else if (_gdbConnectionHisto != null)
                {
                    return new Geodatabase(_gdbConnectionHisto);
                }

            }

            string gdbPath = null;
            string text = null;
            if (isOriginalDb)
            {
                text = "originale";
                gdbPath = $@"{AppConfig.GetValue("BDD_PROD_SDE")}";
            }
            else
            {
                text = "historisée";
                gdbPath = $@"{AppConfig.GetValue("BDD_TEMPORELLE_SDE")}";
            }

            if (isOriginalDb)
            {
                if (System.IO.Path.GetExtension(gdbPath).ToLower().Equals(".sde"))
                {
                    _dbConnectionOriginal = new DatabaseConnectionFile(new Uri(gdbPath));
                }
                else
                {
                    _gdbConnectionOriginal = new FileGeodatabaseConnectionPath(new Uri(gdbPath));
                }
                return _dbConnectionOriginal != null ?
                    new Geodatabase(_dbConnectionOriginal) :
                    new Geodatabase(_gdbConnectionOriginal);
            }
            else
            {
                if (System.IO.Path.GetExtension(gdbPath).ToLower().Equals(".sde"))
                {
                    _dbConnectionHisto = new DatabaseConnectionFile(new Uri(gdbPath));
                }
                else
                {
                    _gdbConnectionHisto = new FileGeodatabaseConnectionPath(new Uri(gdbPath));
                }
                return _dbConnectionHisto != null ?
                    new Geodatabase(_dbConnectionHisto) :
                    new Geodatabase(_gdbConnectionHisto);
            }
        }

        public static string GetOriginalSchemaName()
        {
            return _dbConnectionOriginal != null ?
                DatabaseClient.GetDatabaseConnectionProperties(_dbConnectionOriginal).User :
                string.Empty;
        }
    }
}
