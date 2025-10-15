using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUNCTION_FEMCO_BDI.Funcionalidades
{
    public class Funcionalidad_Encriptacion
    {


        public static void EncryptCsvToStream(string csvContent, Stream outputStream, PgpPublicKey publicKey, string nombre)
        {
            using (MemoryStream bOut = new MemoryStream())
            {
                PgpCompressedDataGenerator comData = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
                using (Stream cos = comData.Open(bOut))
                {
                    PgpLiteralDataGenerator lData = new PgpLiteralDataGenerator();
                    using (Stream pOut = lData.Open(cos, PgpLiteralData.Binary, nombre, DateTime.UtcNow, new byte[4096]))
                    {
                        //    byte[] csvBytes = Encoding.UTF8.GetBytes(csvContent);
                        //  pOut.Write(csvBytes, 0, csvBytes.Length);
                        using (var writer = new StreamWriter(pOut, Encoding.UTF8, bufferSize: 8192, leaveOpen: true))
                        {
                            writer.Write(csvContent);
                            writer.Flush(); // Asegura que los datos lleguen al stream
                        }

                    }
                }
                comData.Close();

                PgpEncryptedDataGenerator encGen = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Aes256, true, new SecureRandom());
                encGen.AddMethod(publicKey);
                using (Stream encOut = encGen.Open(outputStream, bOut.Length))
                {
                    bOut.Position = 0;
                    bOut.CopyTo(encOut);
                }
            }
        }


        public static PgpPublicKey ReadPublicKey(Stream inputStream)
        {
            PgpPublicKeyRingBundle pgpPub = new PgpPublicKeyRingBundle(PgpUtilities.GetDecoderStream(inputStream));
            foreach (PgpPublicKeyRing keyRing in pgpPub.GetKeyRings())
            {
                foreach (PgpPublicKey key in keyRing.GetPublicKeys())
                {
                    if (key.IsEncryptionKey) return key;
                }
            }
            throw new ArgumentException("No encryption key found in public key ring.");
        }


        public static void DecryptFileWithPgp(Stream inputStream, Stream outputStream, Stream privateKeyStream)
        {
            Stream decoderStream = PgpUtilities.GetDecoderStream(inputStream);
            PgpObjectFactory pgpF = new PgpObjectFactory(decoderStream);
            //  PgpObjectFactory pgpF = new PgpObjectFactory(PgpUtilities.GetDecoderStream(inputStream));

            PgpEncryptedDataList enc = null;
            PgpObject o;

            // 🔹 Buscar el bloque de datos encriptados
            while ((o = pgpF.NextPgpObject()) != null)
            {
                if (o is PgpEncryptedDataList dataList)
                {
                    enc = dataList;
                    break;
                }
            }

            if (enc == null) throw new ArgumentException("No se encontró una lista de datos encriptados en el archivo.");


            // 🔹 Buscar los datos encriptados correctos
            PgpPublicKeyEncryptedData pbe = null;
            foreach (PgpEncryptedData ed in enc.GetEncryptedDataObjects())
            {
                if (ed is PgpPublicKeyEncryptedData data)
                {
                    pbe = data;
                    break;
                }
            }

            if (pbe == null) throw new ArgumentException("No se encontraron datos encriptados con clave pública.");


            //// 🔹 Obtener la clave privada válida
            //PgpPrivateKey privateKey = GetPrivateKey(privateKeyStream);
            //if (privateKey == null) throw new ArgumentException("Clave privada inválida.");


            PgpPrivateKey privateKey = GetMatchingPrivateKey(privateKeyStream, pbe.KeyId);


            // Desencriptar los datos
            Stream clear = pbe.GetDataStream(privateKey);

            PgpObjectFactory plainFact = new PgpObjectFactory(clear);
            PgpObject message = plainFact.NextPgpObject();

            // 🔹 Manejo de archivos comprimidos PGP
            if (message is PgpCompressedData compressedData)
            {
                Stream compDataStream = compressedData.GetDataStream();
                PgpObjectFactory pgpFact = new PgpObjectFactory(compDataStream);
                message = pgpFact.NextPgpObject();
            }

            // 🔹 Si es un archivo literal, escribirlo en el output
            if (message is PgpLiteralData literalData)
            {
                using (StreamReader reader = new StreamReader(literalData.GetInputStream(), Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(outputStream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
                {
                    string contenido = reader.ReadToEnd();
                    writer.Write(contenido);
                }
            }
            else
            {
                throw new ArgumentException("Formato de archivo no reconocido después de la desencriptación.");
            }


        }


        private static PgpPrivateKey GetMatchingPrivateKey(Stream privateKeyStream, long keyId)
        {
            privateKeyStream.Position = 0;
            PgpSecretKeyRingBundle pgpSec = new PgpSecretKeyRingBundle(PgpUtilities.GetDecoderStream(privateKeyStream));

            foreach (PgpSecretKeyRing keyRing in pgpSec.GetKeyRings())
            {
                foreach (PgpSecretKey key in keyRing.GetSecretKeys())
                {
                    if (key.KeyId == keyId)
                    {
                        try
                        {
                            PgpPrivateKey privateKey = key.ExtractPrivateKey(new char[0]);
                            if (privateKey != null)
                            {
                                return privateKey;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error extrayendo clave {key.KeyId}: {ex.Message}");
                        }
                    }
                }
            }

            throw new ArgumentException("No se encontró una clave privada válida que coincida con la clave pública.");
        }
    }
}
