using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static partial class QcFile
    {
        public static class Encryption
        {

            public class Secret
            {
                public readonly string APP_SECRET;
                public byte Version;
                public string FileExtension;

                public Secret(string secret, byte version, string fileExtension)
                {
                    if (string.IsNullOrEmpty(secret) || secret.Length < 32)
                        throw new ArgumentException("Secret must be at least 32 characters long.");

                    APP_SECRET = secret;
                    Version = version;
                    FileExtension = fileExtension;
                }
            }


            // File header: [ 4 bytes magic ][ 1 byte ver ][ 12 bytes nonce ][ ...ciphertext+tag... ]
            private static readonly byte[] MAGIC = Encoding.ASCII.GetBytes("SSV1"); // SecureSaVe v1

            // Helpers -----------------------------------------------------------------

            public static void Save(string path, byte[] plain, Secret secret)
            {
                var key = DeriveKey(secret);
                // Use V2 universally for maximum compatibility.
                byte[] iv = RandomBytes(16);
                EncryptCbcHmac(plain, key, iv, out var cipher, out var mac, secret);

                using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                fs.Write(MAGIC, 0, MAGIC.Length);
                fs.WriteByte(secret.Version);
                fs.Write(iv, 0, iv.Length);
                fs.Write(cipher, 0, cipher.Length);
                fs.Write(mac, 0, mac.Length);
            }

            public static byte[] Load(string path, Secret secret)
            {
                byte[] all = File.ReadAllBytes(path);
                if (all.Length < 4 + 1 + 16 + 16) throw new InvalidDataException("Corrupt save (too short).");

                // Detect magic
                bool isV1 = HasPrefix(all, MAGIC);
                if (!isV1)
                    throw new InvalidDataException("Unknown save format.");

                byte version = all[4];
                int offset = 5;

                var key = DeriveKey(secret);


                // [magic][ver][IV(16)][cipher...][HMAC(32)]
                if (all.Length < offset + 16 + 32) throw new InvalidDataException("Corrupt v2 save.");
                byte[] iv = Slice(all, offset, 16);
                offset += 16;

                int macLen = 32;
                int cipherLen = all.Length - offset - macLen;
                if (cipherLen <= 0) throw new InvalidDataException("Corrupt v2 save (cipher length).");

                byte[] cipher = Slice(all, offset, cipherLen);
                byte[] mac = Slice(all, offset + cipherLen, macLen);

                // Verify MAC over: magic||ver||iv||cipher
                using var hmac = new HMACSHA256(HkdfExpand(key, "SecureSaves.v2.mac", 32));
                byte[] headerAndCipher = Concat(MAGIC, new[] { version }, iv, cipher);
                byte[] calc = hmac.ComputeHash(headerAndCipher);

                if (!FixedTimeEquals(mac, calc))
                    throw new CryptographicException("Bad MAC (file tampered or wrong key).");

                // Decrypt
                return DecryptCbc(cipher, HkdfExpand(key, "SecureSaves.v2.enc", 32), iv);


            }

            // === Internals =============================================================

            private static byte[] DeriveKey(Secret secret)
            {
                string deviceId = SystemInfo.deviceUniqueIdentifier ?? "unknown-device";
                byte[] salt = Sha256(Encoding.UTF8.GetBytes(deviceId));
                byte[] ikm = Encoding.UTF8.GetBytes(secret.APP_SECRET);
                return Hkdf(ikm, salt, Encoding.UTF8.GetBytes("SecureSaves.master"), 32); // 256-bit master
            }

            // V2: AES-CBC-256 + HMAC-SHA256 (Encrypt-then-MAC)
            private static void EncryptCbcHmac(byte[] plain, byte[] masterKey, byte[] iv, out byte[] cipher, out byte[] mac, Secret secret)
            {
                byte[] encKey = HkdfExpand(masterKey, "SecureSaves.v2.enc", 32);
                byte[] macKey = HkdfExpand(masterKey, "SecureSaves.v2.mac", 32);

                cipher = EncryptCbc(plain, encKey, iv);
                using var hmac = new HMACSHA256(macKey);
                // MAC over magic||ver||iv||cipher
                mac = hmac.ComputeHash(Concat(MAGIC, new[] { secret.Version }, iv, cipher));
            }

            private static byte[] EncryptCbc(byte[] plain, byte[] key, byte[] iv)
            {
                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    cs.Write(plain, 0, plain.Length);
                return ms.ToArray();
            }

            private static byte[] DecryptCbc(byte[] cipher, byte[] key, byte[] iv)
            {
                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    cs.Write(cipher, 0, cipher.Length);
                return ms.ToArray();
            }

#if UNITY_2021_3_OR_NEWER
            // V1: AES-GCM (only if supported)
            private static bool AesGcmSupported()
            {
                try
                {
                    // probe once with dummy key
                    using var _ = new System.Security.Cryptography.AesGcm(new byte[16]); // ctor throws if unsupported
                    return true;
                }
                catch { return false; }
            }

            private static byte[] DecryptAesGcm(byte[] cipherWithTag, byte[] key, byte[] nonce)
            {
                int tagLen = 16;
                if (cipherWithTag.Length < tagLen) throw new InvalidDataException("Cipher too short.");

                int tagOffset = cipherWithTag.Length - tagLen;
                byte[] cipher = Slice(cipherWithTag, 0, tagOffset);
                byte[] tag = Slice(cipherWithTag, tagOffset, tagLen);

                byte[] plain = new byte[cipher.Length];
                using var gcm = new System.Security.Cryptography.AesGcm(key);
                gcm.Decrypt(nonce, cipher, tag, plain, null);
                return plain;
            }
#endif

            // HKDF (RFC 5869) -----------------------------------------------------------
            private static byte[] Hkdf(byte[] ikm, byte[] salt, byte[] info, int length)
            {
                // Extract
                byte[] prk;
                using (var hmac = new HMACSHA256(salt))
                    prk = hmac.ComputeHash(ikm);
                // Expand
                return HkdfExpand(prk, info, length);
            }

            private static byte[] HkdfExpand(byte[] prk, string infoStr, int length)
                => HkdfExpand(prk, Encoding.UTF8.GetBytes(infoStr), length);

            private static byte[] HkdfExpand(byte[] prk, byte[] info, int length)
            {
                using var hmac = new HMACSHA256(prk);
                byte[] t = Array.Empty<byte>();
                int pos = 0;
                byte[] okm = new byte[length];
                byte ctr = 1;

                while (pos < length)
                {
                    hmac.Initialize();
                    hmac.TransformBlock(t, 0, t.Length, null, 0);
                    hmac.TransformBlock(info, 0, info.Length, null, 0);
                    hmac.TransformFinalBlock(new[] { ctr }, 0, 1);
                    t = hmac.Hash!;
                    int toCopy = Math.Min(t.Length, length - pos);
                    Buffer.BlockCopy(t, 0, okm, pos, toCopy);
                    pos += toCopy;
                    ctr++;
                }
                return okm;
            }

            // Utils ---------------------------------------------------------------------
            private static byte[] Sha256(byte[] data)
            {
                using var sha = SHA256.Create();
                return sha.ComputeHash(data);
            }

            private static byte[] RandomBytes(int len)
            {
                var b = new byte[len];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(b);
                return b;
            }

            private static bool HasPrefix(byte[] buf, byte[] prefix)
            {
                if (buf.Length < prefix.Length) return false;
                for (int i = 0; i < prefix.Length; i++) if (buf[i] != prefix[i]) return false;
                return true;
            }

            private static byte[] Slice(byte[] src, int offset, int len)
            {
                var dst = new byte[len];
                Buffer.BlockCopy(src, offset, dst, 0, len);
                return dst;
            }

            private static byte[] Concat(params byte[][] parts)
            {
                int len = 0; foreach (var p in parts) len += p.Length;
                var buf = new byte[len]; int pos = 0;
                foreach (var p in parts) { Buffer.BlockCopy(p, 0, buf, pos, p.Length); pos += p.Length; }
                return buf;
            }

            private static byte[] Concat(byte[] a, byte[] b) => Concat(new[] { a, b });

            private static bool FixedTimeEquals(byte[] a, byte[] b)
            {
                if (a == null || b == null || a.Length != b.Length) return false;
                int diff = 0;
                for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
                return diff == 0;
            }
        }

    }
}
