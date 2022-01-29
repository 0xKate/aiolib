using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace aiolib
{

    public static class aioExtensions
    {

        public static async Task<T> CancellableTask<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(
                        s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                if (task != await Task.WhenAny(task, tcs.Task))
                    throw new OperationCanceledException(cancellationToken);
            return await task;
        }

        public static void Clear(this byte[] array)
        {
            Array.Clear(array, 0, array.Length);
            array = null;
        }

        public static async Task<string> GetClientDigest(this RemoteHost client, bool serverSide = true)
        {
            SHA1 sha1 = SHA1.Create();
            string digest = String.Empty;

            IPEndPoint? remoteEnd = (IPEndPoint?)client.ClientSocket.Client.RemoteEndPoint;
            IPEndPoint? localEnd = (IPEndPoint?)client.ClientSocket.Client.LocalEndPoint;

            byte[] localbytes = Encoding.UTF8.GetBytes(localEnd.ToString());
            byte[] remotebytes = Encoding.UTF8.GetBytes(remoteEnd.ToString());

            using (MemoryStream localstream = new MemoryStream(localbytes))
            {
                using (MemoryStream remotestream = new MemoryStream(remotebytes))
                {
                    byte[] localHash = await sha1.ComputeHashAsync(localstream);
                    byte[] remoteHash = await sha1.ComputeHashAsync(remotestream);

                    byte[] bothHash = new byte[localHash.Length + remoteHash.Length];
                    if (serverSide)
                    {
                        Buffer.BlockCopy(localHash, 0, bothHash, 0, localHash.Length);
                        Buffer.BlockCopy(remoteHash, 0, bothHash, localHash.Length, remoteHash.Length);
                    }
                    else
                    {
                        Buffer.BlockCopy(remoteHash, 0, bothHash, 0, remoteHash.Length);
                        Buffer.BlockCopy(localHash, 0, bothHash, remoteHash.Length, localHash.Length);
                    }


                    using (MemoryStream bothstream = new MemoryStream(bothHash))
                    {
                        byte[] hex = await sha1.ComputeHashAsync(bothstream);

                        localbytes.Clear();
                        remotebytes.Clear();
                        localHash.Clear();
                        remoteHash.Clear();
                        bothHash.Clear();
                        digest = Convert.ToHexString(hex);
                    }
                }
            }
            return digest;
        }
    }

    public static class Crypto
    {
        public static void MakeCert(string certFilename, string keyFilename, string subjectName, string ipAddress)
        {
            const string CRT_HEADER = "-----BEGIN CERTIFICATE-----\n";
            const string CRT_FOOTER = "\n-----END CERTIFICATE-----";

            const string KEY_HEADER = "-----BEGIN RSA PRIVATE KEY-----\n";
            const string KEY_FOOTER = "\n-----END RSA PRIVATE KEY-----";

            using var rsa = RSA.Create();
            var certRequest = new CertificateRequest($"cn={subjectName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // Adding SubjectAlternativeNames (SAN)
            var subjectAlternativeNames = new SubjectAlternativeNameBuilder();
            subjectAlternativeNames.AddDnsName(subjectName);
            subjectAlternativeNames.AddIpAddress(IPAddress.Parse(ipAddress));
            certRequest.CertificateExtensions.Add(subjectAlternativeNames.Build());

            // We're just going to create a temporary certificate, that won't be valid for long
            var certificate = certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(365));

            // export the private key
            var privateKey1 = Convert.ToBase64String(rsa.ExportRSAPrivateKey(), Base64FormattingOptions.InsertLineBreaks);

            File.WriteAllText("rsa.key", KEY_HEADER + privateKey1 + KEY_FOOTER);

            //var privateKey2 = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey(), Base64FormattingOptions.InsertLineBreaks);

            //File.WriteAllText("pkcs.key", KEY_HEADER + privateKey2 + KEY_FOOTER);

            // Export the certificate
            var exportData1 = certificate.Export(X509ContentType.Cert);

            var crt1 = Convert.ToBase64String(exportData1, Base64FormattingOptions.InsertLineBreaks);

            File.WriteAllText("rsa.crt", CRT_HEADER + crt1 + CRT_FOOTER);

            //var exportData2 = certificate.Export(X509ContentType.Pkcs7);

            //var crt2 = Convert.ToBase64String(exportData2, Base64FormattingOptions.InsertLineBreaks);

            //File.WriteAllText("x509.Pkcs7", CRT_HEADER + crt2 + CRT_FOOTER);
        }
    }
}
