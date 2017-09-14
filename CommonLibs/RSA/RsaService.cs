using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace CommonLibs
{
    public class RsaService
    {
        private static string _PubKeyXML = string.Empty;
        public static string PubKeyXML
        {
            get
            {
                if (string.IsNullOrEmpty(_PubKeyXML))
                {
                    string curpath = Directory.GetCurrentDirectory();
                    Console.WriteLine("PubKeyXML path = " + curpath);
                    string filename = curpath + @"/public.key";

                    using (var fs = File.OpenRead(filename))
                    {
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            _PubKeyXML = sr.ReadToEnd().Replace("\n", "");
                            Console.WriteLine("PubKeyXML, length=" + PubKeyXML.Length);
                        }
                    }
                }
                return _PubKeyXML;
            }
        }
        private static string _PrivKeyXml = string.Empty;
        public static string PrivKeyXml
        {
            get
            {
                if (string.IsNullOrEmpty(_PrivKeyXml))
                {
                    string curpath = Directory.GetCurrentDirectory();
                    string filename = curpath + @"/private.key";

                    using (var fs = File.OpenRead(filename))
                    {
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            _PrivKeyXml = sr.ReadToEnd().Replace("\n", "");
                            Console.WriteLine("PrivKeyXml, length=" + PubKeyXML.Length);
                        }
                    }
                }
                return _PrivKeyXml;
            }
        }

        public static byte[] EncryptFromString(string input)
        {
            byte[] cipherBytes = null;
            try
            {
                using (var rsa = CreateRsaFromPublicKey(PubKeyXML))
                {
                    var plainTextBytes = Encoding.UTF8.GetBytes(input);
                    //cipherBytes = rsa.Encrypt(plainTextBytes, RSAEncryptionPadding.Pkcs1);

                    //每段的长度
                    int bufferSize = (rsa.KeySize / 8) - 11;
                    var buffer = new byte[bufferSize];
                    using (MemoryStream inputStream = new MemoryStream(plainTextBytes),
                         outputStream = new MemoryStream())
                    {
                        while (true)
                        {
                            //分段加密
                            int readSize = inputStream.Read(buffer, 0, bufferSize);
                            if (readSize <= 0)
                            {
                                break;
                            }

                            var temp = new byte[readSize];
                            Array.Copy(buffer, 0, temp, 0, readSize);
                            var encryptedBytes = rsa.Encrypt(temp, RSAEncryptionPadding.Pkcs1);
                            outputStream.Write(encryptedBytes, 0, encryptedBytes.Length);
                        }
                        cipherBytes = outputStream.ToArray();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("EncryptFromString Error: ", e.Message));
            }
            return cipherBytes;
        }

        public static string DecryptToString(byte[] input)
        {
            string plainText = "";
            try
            {
                using (var rsa = CreateRsaFromPrivateKey(PrivKeyXml))
                {
                    //var plainTextBytes = rsa.Decrypt(input, RSAEncryptionPadding.Pkcs1);
                    //plainText = Encoding.UTF8.GetString(plainTextBytes);

                    //每段的长度
                    int bufferSize = (rsa.KeySize / 8);
                    var buffer = new byte[bufferSize];
                    using (MemoryStream inputStream = new MemoryStream(input),
                         outputStream = new MemoryStream())
                    {
                        while (true)
                        {
                            int readSize = inputStream.Read(buffer, 0, bufferSize);
                            if (readSize <= 0)
                            {
                                break;
                            }

                            var temp = new byte[readSize];
                            Array.Copy(buffer, 0, temp, 0, readSize);
                            var rawBytes = rsa.Decrypt(temp, RSAEncryptionPadding.Pkcs1);
                            outputStream.Write(rawBytes, 0, rawBytes.Length);
                        }
                        plainText = Encoding.UTF8.GetString(outputStream.ToArray());
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("DecryptToString, Error={0}", e.Message);
            }
            return plainText;
        }

        public static string DecryptToString(string cipherText)
        {
            var input = Convert.FromBase64String(cipherText);
            return DecryptToString(input);
        }

        private static RSA CreateRsaFromPrivateKey(string privateKey)
        {
            var privateKeyBits = System.Convert.FromBase64String(privateKey);
            var rsa = RSA.Create();
            var RSAparams = new RSAParameters();

            using (MemoryStream outStream = new MemoryStream(privateKeyBits))
            {
                using (var binr = new BinaryReader(outStream))
                {
                    byte bt = 0;
                    ushort twobytes = 0;
                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8130)
                        binr.ReadByte();
                    else if (twobytes == 0x8230)
                        binr.ReadInt16();
                    else
                        throw new Exception("Unexpected value read binr.ReadUInt16()");

                    twobytes = binr.ReadUInt16();
                    if (twobytes != 0x0102)
                        throw new Exception("Unexpected version");

                    bt = binr.ReadByte();
                    if (bt != 0x00)
                        throw new Exception("Unexpected value read binr.ReadByte()");

                    RSAparams.Modulus = binr.ReadBytes(GetIntegerSize(binr));
                    RSAparams.Exponent = binr.ReadBytes(GetIntegerSize(binr));
                    RSAparams.D = binr.ReadBytes(GetIntegerSize(binr));
                    RSAparams.P = binr.ReadBytes(GetIntegerSize(binr));
                    RSAparams.Q = binr.ReadBytes(GetIntegerSize(binr));
                    RSAparams.DP = binr.ReadBytes(GetIntegerSize(binr));
                    RSAparams.DQ = binr.ReadBytes(GetIntegerSize(binr));
                    RSAparams.InverseQ = binr.ReadBytes(GetIntegerSize(binr));
                }
            }
            rsa.ImportParameters(RSAparams);
            return rsa;
        }

        private static int GetIntegerSize(BinaryReader binr)
        {
            byte bt = 0;
            byte lowbyte = 0x00;
            byte highbyte = 0x00;
            int count = 0;
            bt = binr.ReadByte();
            if (bt != 0x02)
                return 0;
            bt = binr.ReadByte();

            if (bt == 0x81)
                count = binr.ReadByte();
            else
            {
                if (bt == 0x82)
                {
                    highbyte = binr.ReadByte();
                    lowbyte = binr.ReadByte();
                    byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                    count = BitConverter.ToInt32(modint, 0);
                }
                else
                {
                    count = bt;
                }
            }

            while (binr.ReadByte() == 0x00)
            {
                count -= 1;
            }
            binr.BaseStream.Seek(-1, SeekOrigin.Current);
            return count;
        }

        private static RSA CreateRsaFromPublicKey(string publicKeyString)
        {
            byte[] SeqOID = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
            byte[] x509key;
            byte[] seq = new byte[15];
            int x509size;

            x509key = Convert.FromBase64String(publicKeyString);
            x509size = x509key.Length;

            using (var mem = new MemoryStream(x509key))
            {
                using (var binr = new BinaryReader(mem))
                {
                    byte bt = 0;
                    ushort twobytes = 0;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8130)
                        binr.ReadByte();
                    else if (twobytes == 0x8230)
                        binr.ReadInt16();
                    else
                        return null;

                    seq = binr.ReadBytes(15);
                    if (!CompareBytearrays(seq, SeqOID))
                        return null;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8103)
                        binr.ReadByte();
                    else if (twobytes == 0x8203)
                        binr.ReadInt16();
                    else
                        return null;

                    bt = binr.ReadByte();
                    if (bt != 0x00)
                        return null;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8130)
                        binr.ReadByte();
                    else if (twobytes == 0x8230)
                        binr.ReadInt16();
                    else
                        return null;

                    twobytes = binr.ReadUInt16();
                    byte lowbyte = 0x00;
                    byte highbyte = 0x00;

                    if (twobytes == 0x8102)
                        lowbyte = binr.ReadByte();
                    else if (twobytes == 0x8202)
                    {
                        highbyte = binr.ReadByte();
                        lowbyte = binr.ReadByte();
                    }
                    else
                        return null;
                    byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                    int modsize = BitConverter.ToInt32(modint, 0);

                    int firstbyte = binr.PeekChar();
                    if (firstbyte == 0x00)
                    {
                        binr.ReadByte();
                        modsize -= 1;
                    }

                    byte[] modulus = binr.ReadBytes(modsize);

                    if (binr.ReadByte() != 0x02)
                        return null;
                    int expbytes = (int)binr.ReadByte();
                    byte[] exponent = binr.ReadBytes(expbytes);

                    var rsa = RSA.Create();
                    var rsaKeyInfo = new RSAParameters
                    {
                        Modulus = modulus,
                        Exponent = exponent
                    };
                    rsa.ImportParameters(rsaKeyInfo);
                    return rsa;
                }
            }
        }

        private static bool CompareBytearrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            int i = 0;
            foreach (byte c in a)
            {
                if (c != b[i])
                    return false;
                i++;
            }
            return true;
        }

        //#region Methods
        //public static byte[] Encrypt(byte[] input)
        //{
        //    byte[] data = encrypt(input);
        //    return MarkData(data);
        //}
        //private static byte[] encrypt(byte[] sou)
        //{
        //    byte[] data = null;
        //    using (var rsa = RSA.Create())
        //    {
        //        try
        //        {
        //            RsaExtention.FromXmlString(rsa, PubKeyXML);
        //            //rsa.FromXmlString(PubKeyXML);
        //            int maxBlockSize = rsa.KeySize / 8 - 11;
        //            if (sou.Length <= maxBlockSize)
        //            {
        //                data = rsa.Encrypt(sou, RSAEncryptionPadding.Pkcs1);
        //            }

        //            if (data == null)
        //            {
        //                using (MemoryStream plaiStream = new MemoryStream(sou))
        //                {
        //                    using (MemoryStream crypStream = new MemoryStream())
        //                    {
        //                        byte[] buffer = new byte[maxBlockSize];
        //                        int blockSize = plaiStream.Read(buffer, 0, maxBlockSize);

        //                        while (blockSize > 0)
        //                        {
        //                            byte[] toEncrypt = new byte[blockSize];
        //                            Array.Copy(buffer, 0, toEncrypt, 0, blockSize);

        //                            byte[] cryptograph = rsa.Encrypt(toEncrypt, RSAEncryptionPadding.Pkcs1);
        //                            crypStream.Write(cryptograph, 0, cryptograph.Length);

        //                            blockSize = plaiStream.Read(buffer, 0, maxBlockSize);
        //                        }

        //                        data = crypStream.ToArray();
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine("encrypt Error: " + e.Message);
        //        }
        //    }
        //    return data;
        //}

        //public static byte[] Decrypt(byte[] input)
        //{
        //    if (IsEncrypt(input))
        //    {
        //        byte[] data = ClearDataMark(input);
        //        return decrypt(data);
        //    }
        //    return input;
        //}
        //private static byte[] decrypt(byte[] sou)
        //{
        //    byte[] data = null;
        //    using (var rsa = RSA.Create())
        //    {
        //        int maxBlockSize = 0;
        //        try
        //        {
        //            RsaExtention.FromXmlString(rsa, PrivKeyXml);
        //            //rsa.FromXmlString(PrivKeyXml);

        //            maxBlockSize = rsa.KeySize / 8;

        //            if (sou.Length <= maxBlockSize)
        //                data = rsa.Decrypt(sou, RSAEncryptionPadding.Pkcs1);

        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine("decrypt Error: " + e.Message);
        //        }

        //        if (data == null)
        //        {
        //            using (MemoryStream crypStream = new MemoryStream(sou))
        //            {
        //                using (MemoryStream plaiStream = new MemoryStream())
        //                {
        //                    byte[] buffer = new byte[maxBlockSize];
        //                    int blockSize = crypStream.Read(buffer, 0, maxBlockSize);

        //                    while (blockSize > 0)
        //                    {
        //                        byte[] toDecrypt = new byte[blockSize];
        //                        Array.Copy(buffer, 0, toDecrypt, 0, blockSize);

        //                        byte[] plaintext = rsa.Decrypt(toDecrypt, RSAEncryptionPadding.Pkcs1);
        //                        plaiStream.Write(plaintext, 0, plaintext.Length);

        //                        blockSize = crypStream.Read(buffer, 0, maxBlockSize);
        //                    }

        //                    data = plaiStream.ToArray();
        //                }
        //            }
        //        }
        //    }
        //    return data;
        //}

        //#endregion

        //#region Utilities
        //private static byte[] MarkData(byte[] input)
        //{
        //    if (input != null)
        //    {
        //        int length = input.Length + 200;
        //        byte[] newBytes = new byte[length];
        //        for (int i = 0; i < newBytes.Length; i++)
        //        {
        //            if (i < 100 || i > newBytes.Length - 100 - 1)
        //            {
        //                newBytes[i] = 0;
        //            }
        //            else
        //            {
        //                newBytes[i] = input[i - 100];
        //            }
        //        }
        //        return newBytes;
        //    }
        //    else
        //        return null;
        //}

        //private static byte[] ClearDataMark(byte[] input)
        //{
        //    if (input != null)
        //    {
        //        int length = input.Length - 200;
        //        byte[] newBytes = new byte[length];
        //        for (int i = 0; i < length; i++)
        //        {
        //            newBytes[i] = input[i + 100];
        //        }
        //        return newBytes;
        //    }
        //    else
        //        return null;
        //}
        //private static bool IsEncrypt(byte[] input)
        //{
        //    if (input != null)
        //    {
        //        for (int i = 0; i < 100; i++)
        //        {
        //            if (input[i] != 0 || input[input.Length - i - 1] != 0)
        //            {
        //                return false;
        //            }
        //        }
        //        return true;
        //    }
        //    else
        //        return false;
        //}

        //#endregion

    }
}
