using System;
using System.IO;
using System.Text;

namespace Yu5h1Lib.UI.Acceptance
{
    /// <summary>Reads and writes small UTF-8 theme files selected in the settings sample.</summary>
    internal static class SettingsThemeExchangeFiles
    {
        private const int MaxFileBytes = 64 * 1024;
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        internal static bool TryRead(string path, out string json, out string message)
        {
            json = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                message = "請輸入要匯入的檔案路徑。";
                return false;
            }

            try
            {
                using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                var bytes = new byte[MaxFileBytes + 1];
                int length = 0;
                while (length < bytes.Length)
                {
                    int read = file.Read(bytes, length, bytes.Length - length);
                    if (read == 0)
                        break;

                    length += read;
                }

                if (length > MaxFileBytes)
                {
                    message = "主題檔案不得超過 64 KiB。";
                    return false;
                }

                // JSON cannot contain a literal NUL. Reject it here to catch UTF-16 files without a BOM.
                if (Array.IndexOf(bytes, (byte)0, 0, length) >= 0)
                {
                    message = "請使用 UTF-8 編碼的 JSON 檔案。";
                    return false;
                }

                int offset = length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0;
                json = Utf8.GetString(bytes, offset, length - offset);
                message = null;
                return true;
            }
            catch (DecoderFallbackException)
            {
                message = "請使用 UTF-8 編碼的 JSON 檔案。";
            }
            catch (FileNotFoundException)
            {
                message = "找不到要匯入的檔案。";
            }
            catch (DirectoryNotFoundException)
            {
                message = "找不到匯入路徑的資料夾。";
            }
            catch (Exception error) when (IsFileError(error))
            {
                message = "無法讀取主題檔案，請檢查路徑與存取權限。";
            }

            json = null;
            return false;
        }

        internal static bool TryWrite(string path, string json, out string message)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                message = "請輸入要匯出的檔案路徑。";
                return false;
            }

            bool created = false;
            try
            {
                byte[] bytes = Utf8.GetBytes(json);
                using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    created = true;
                    file.Write(bytes, 0, bytes.Length);
                }

                message = null;
                return true;
            }
            catch (DirectoryNotFoundException)
            {
                message = "找不到匯出路徑的資料夾。";
            }
            catch (Exception error) when (!created && IsFileError(error) &&
                (File.Exists(path) || Directory.Exists(path)))
            {
                message = "此路徑已存在，請另選檔名。";
            }
            catch (Exception error) when (IsFileError(error))
            {
                message = created ? "匯出寫入中斷。新建檔案可能不完整，請檢查後另存。"
                    : "無法寫入主題檔案，請檢查路徑與存取權限。";
            }

            return false;
        }

        private static bool IsFileError(Exception error) =>
            error is IOException || error is UnauthorizedAccessException ||
            error is ArgumentException || error is NotSupportedException;
    }
}
