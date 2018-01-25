using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace connsearch.SqlDev
{
    class SqlDevInstance
    {
        private string systemFolderPath;
        private string sqldevFolder;
        public string ConnectionsFilePath
        {
            get
            {
                return Path.Combine(this.systemFolderPath, "o.jdeveloper.db.connection/connections.xml");
            }
        }

        public string ProductPreferencesFilePath
        {
            get
            {
                return Path.Combine(this.systemFolderPath, "o.sqldeveloper/product-preferences.xml");
            }
        }

        public string SystemId
        {
            get
            {
                XmlDocument SqlDevSystemPrefsDoc = new XmlDocument();
                SqlDevSystemPrefsDoc.Load(this.ProductPreferencesFilePath);
                XmlNamespaceManager systemPrefsNsManager = new XmlNamespaceManager(SqlDevSystemPrefsDoc.NameTable);
                systemPrefsNsManager.AddNamespace("ide", "http://xmlns.oracle.com/ide/hash");

                string systemId =
                    SqlDevSystemPrefsDoc
                        .DocumentElement
                        .SelectSingleNode("/ide:preferences/value[@n='db.system.id']/@v", systemPrefsNsManager)
                        .Value;

                return systemId;
            }
        }


        public SqlDevInstance()
        {
            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            this.sqldevFolder = Path.Combine(homeDirectory, ".sqldeveloper");

            SetSystemFolderPath();
        }

        private void SetSystemFolderPath()
        {
            string[] systemFolders =  Directory.GetDirectories(this.sqldevFolder, "system*");

            List<PrefixedVersion> allSystemFolders = new List<PrefixedVersion>();

            string baseFileName;

            foreach(string s in systemFolders)
            {
                baseFileName = Path.GetFileName(s);
                allSystemFolders.Add(new PrefixedVersion(baseFileName, "system"));
            }

            List<PrefixedVersion> sortedSystemFolders =
                allSystemFolders
                    .OrderByDescending(v => v.Major)
                    .ThenByDescending(v => v.Minor)
                    .ThenByDescending(v => v.Minor)
                    .ThenByDescending(v => v.MajorRevision)
                    .ThenByDescending(v => v.MinorRevision)
                    .ThenByDescending(v => v.Build)
                    .ToList();

            PrefixedVersion latestSystemFolder = sortedSystemFolders[0];

            string systemFolderPath = Path.Combine(this.sqldevFolder, latestSystemFolder.VersionString);

            this.systemFolderPath = systemFolderPath;

        }

        public List<Connection> GetAllConnections()
        {
            XDocument sqlDevCOnnectionsDoc = XDocument.Load(this.ConnectionsFilePath);

            // Create a list of connections, so that we can return any useful
            // data to the user
            List<Connection> SqlDevConnectionList = (
                from connection in sqlDevCOnnectionsDoc.Descendants("RefAddresses")
                select new Connection()
                {
                    Password =
                        (from connectinSetting in connection.Descendants("StringRefAddr")
                        where (string) connectinSetting.Attribute("addrType") == "password"
                        select (string) connectinSetting.Element("Contents")).FirstOrDefault(),
                    ConnectionName =
                        (from connectinSetting in connection.Descendants("StringRefAddr")
                        where (string) connectinSetting.Attribute("addrType") == "ConnName"
                        select (string) connectinSetting.Element("Contents")).FirstOrDefault(),
                    Schema =
                        (from connectinSetting in connection.Descendants("StringRefAddr")
                        where (string) connectinSetting.Attribute("addrType") == "user"
                        select (string) connectinSetting.Element("Contents")).FirstOrDefault()
                }
            ).ToList();

            return SqlDevConnectionList;
        }
    }
}