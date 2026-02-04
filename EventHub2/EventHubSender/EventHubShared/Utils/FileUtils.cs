using System.Security.Cryptography;
using System.Text;

namespace EventHubShared.Utils
{
    public static class FileUtils
    {
        public static string CalculateMd5Hash(byte[] fileData)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(fileData);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static List<byte[]> ChunkFile(byte[] fileData, int chunkSize = 1024 * 1024) // 1MB chunks
        {
            var chunks = new List<byte[]>();
            for (int i = 0; i < fileData.Length; i += chunkSize)
            {
                var remainingBytes = Math.Min(chunkSize, fileData.Length - i);
                var chunk = new byte[remainingBytes];
                Array.Copy(fileData, i, chunk, 0, remainingBytes);
                chunks.Add(chunk);
            }
            return chunks;
        }

        public static bool IsValidMp3File(byte[] fileData)
        {
            // Check for MP3 header (ID3v2 or MPEG frame sync)
            if (fileData.Length < 3) return false;

            // Check for ID3v2 tag
            if (fileData[0] == 0x49 && fileData[1] == 0x44 && fileData[2] == 0x33)
                return true;

            // Check for MPEG frame sync (11 bits set)
            if (fileData.Length >= 2 && (fileData[0] == 0xFF && (fileData[1] & 0xE0) == 0xE0))
                return true;

            return false;
        }
    }
}