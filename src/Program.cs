using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        static void usage()
        {
            Console.WriteLine("Usage: connsearch [ls|password connection_name]");
        }
        static void Main(string[] args)
        {
            SqlDevInstance mySqlDev = new SqlDevInstance();

            if (args.Length == 0)
            {
                Console.Error.WriteLine("Invalid number of arguments.");
                usage();

                Environment.Exit(1);
            }
            else if (args[0] == "about")
            {
                Console.WriteLine("connsearch: Find Oracle SQL Developer connections and their associated passwords");
                usage();
            }
            else if (args[0] == "ls")
            {
                List<Connection> allConnections = mySqlDev.GetAllConnections();
                foreach(Connection c in allConnections)
                {
                    Console.WriteLine(c.ConnectionName);
                }
            }
            else if (args[0] == "password" && args.Length == 2)
            {
                bool connectionNameFound = false;
                List<Connection> allConnections = mySqlDev.GetAllConnections();
                foreach(Connection c in allConnections)
                {
                    if (c.ConnectionName == args[1])
                    {
                        connectionNameFound = true;
                        PasswordDecryptService decryptor = new PasswordDecryptService(mySqlDev.SystemId);
                        decryptor.EncryptedPassword = c.Password;
                        string decryptedPassword = decryptor.GetDecryptedPassword();

                        Console.WriteLine("{0}: {1}/{2}", c.ConnectionName, c.Schema, decryptedPassword);
                    }

                }

                if (!connectionNameFound)
                {
                    Console.Error.WriteLine(String.Format("Could not find connection named \"{0}\"", args[1]));
                }
            }
            else
            {
                Console.WriteLine("Unknown input received");

                Environment.Exit(1);
            }

        }
    }
}
