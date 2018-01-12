using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using connsearch.SqlDev;

namespace connsearch
{
    // Using https://github.com/ReneNyffenegger/Oracle-SQL-developer-password-decryptor/blob/master/Decrypt_V4.java as reference point
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: read real path from $HOME/.sqldeveloper
            string pathToConnections = "/home/trent/.sqldeveloper/system17.2.0.188.1159/o.jdeveloper.db.connection/connections.xml";
            string pathToPreferences = "/home/trent/.sqldeveloper/system17.2.0.188.1159/o.sqldeveloper/product-preferences.xml";

            XDocument sqlDevCOnnectionsDoc = XDocument.Load(pathToConnections);

            // Create a list of connections, so that we can return any useful
            // data to the user
            var SqlDevConnectionList = (
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
                        select (string) connectinSetting.Element("Contents")).FirstOrDefault()
                }
            ).ToList();

            // Determine the system id of the current installation so we can
            // successfully decrypt the password stored in the connections file
            XmlDocument SqlDevSystemPrefsDoc = new XmlDocument();
            SqlDevSystemPrefsDoc.Load(pathToPreferences);
            XmlNamespaceManager systemPrefsNsManager = new XmlNamespaceManager(SqlDevSystemPrefsDoc.NameTable);
            systemPrefsNsManager.AddNamespace("ide", "http://xmlns.oracle.com/ide/hash");

            var sqlDevSystemId =
                SqlDevSystemPrefsDoc
                    .DocumentElement
                    .SelectSingleNode("/ide:preferences/value[@n='db.system.id']/@v", systemPrefsNsManager)
                    .Value;

            PasswordDecryptService decryptor = new PasswordDecryptService(sqlDevSystemId);

            string decryptedPassword;

            foreach (var conn in SqlDevConnectionList)
            {
                if (!String.IsNullOrEmpty(conn.Password))
                {
                    decryptor.EncryptedPassword = conn.Password;
                    decryptedPassword = decryptor.GetDecryptedPassword();
                    Console.WriteLine(String.Format("Connection {0} has encrypted password {1} which is really {2}", conn.ConnectionName, conn.Password, decryptedPassword));
                }
            }
        }
    }
}
