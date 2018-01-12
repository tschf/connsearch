using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace connsearch.SqlDev
{
    class PasswordDecryptService
    {
        private string systemId;
        public string EncryptedPassword { get; set; }
        private const string BASE_DECRYPTION_KEY = "051399429372e8ad";
        public PasswordDecryptService(string systemId)
        {
            this.systemId = systemId;
        }

        private byte[] getDecryptionHexKey(string decryptionKey)
        {
            byte[] decryptionHex = new byte[decryptionKey.Length / 2];

            for(int i = 0; i < decryptionHex.Length; i++)
            {
                decryptionHex[i] = Convert.ToByte(decryptionKey.Substring(i*2, 2), 16);
            }

            return decryptionHex;
        }

        private byte[] calculateActualDecryptionKey(string systemId, byte[] baseKey)
        {
            const int HASH_ITERATIONS = 42;
            // The decryption key will be the length of both the systemId and
            // the baseKey
            byte[] decryptionKey = new byte[systemId.Length + baseKey.Length];

            // Copy the systemId into that start of decryptionKey
            Encoding.UTF8.GetBytes(systemId, 0, systemId.Length, decryptionKey, 0);

            // Then copy the baseKey over to the end of the decryptionKey
            Buffer.BlockCopy(baseKey, 0, decryptionKey, systemId.Length, baseKey.Length);

            // We need to repeatedly calculate the a hash on the bytes until
            // we can determine the secret and iv.
            MD5 md5 = new MD5CryptoServiceProvider();
            for (int i = 0; i < HASH_ITERATIONS; i++)
            {
                decryptionKey = md5.ComputeHash(decryptionKey);
            }

            return decryptionKey;

        }

        public string GetDecryptedPassword()
        {
            // This is where we store the password to return
            string password;

            // First, we need to decode the password which is stored in base64
            byte[] passwordBase64Decoded = Convert.FromBase64String(this.EncryptedPassword);

            // We also need the DECRYPTION_KEY in Hex
            byte[] baseDecryptionKeyHex = getDecryptionHexKey(BASE_DECRYPTION_KEY);

            byte[] decryptionKey = calculateActualDecryptionKey(this.systemId, baseDecryptionKeyHex);

            // The secret will be the first 8 bytes of our hashed key, where the
            // IV will be the last 8 bytes
            byte[] secretKey = new byte[8];
            byte[] iv = new byte[8];

            Buffer.BlockCopy(decryptionKey, 0, secretKey, 0, 8);
            Buffer.BlockCopy(decryptionKey, 8, iv, 0, decryptionKey.Length-8);

            // Example of decryption process adapted from that that is documented
            // here: https://msdn.microsoft.com/en-us/library/system.security.cryptography.des(v=vs.110).aspx
            // whilst also learning from the existing projects out there that
            // show how to decrypt a SQL Developer password, named:
            // - https://github.com/ReneNyffenegger/Oracle-SQL-developer-password-decryptor
            // - https://github.com/maaaaz/sqldeveloperpassworddecryptor/blob/master/sqldeveloperpassworddecryptor.py
            DESCryptoServiceProvider DesDecryptProvider = new DESCryptoServiceProvider();
            DesDecryptProvider.Mode = CipherMode.CBC;
            DesDecryptProvider.IV = iv;
            DesDecryptProvider.Key = secretKey;

            // Create a DES Decryption provider service
            ICryptoTransform decryptor =
                DesDecryptProvider.CreateDecryptor(
                    DesDecryptProvider.Key,
                    DesDecryptProvider.IV);

            // Set up streams for the encrypted password (from connections.xml)
            // and cryptography, before finally returning the connections password
            // into: `connectionPassword`.
            using (MemoryStream passwordStream = new MemoryStream(passwordBase64Decoded))
            {
                using (CryptoStream cryptographyStream = new CryptoStream(passwordStream, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader descryptStream = new StreamReader(cryptographyStream))
                    {

                        // We have all the cryptography pieces in place to decrypt
                        // the password now. Let's move the bytes to a string
                        // to return to the user.
                        password = descryptStream.ReadToEnd();
                    }
                }
            }

            return password;
        }
    }
}