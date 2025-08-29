using Collector.Common.Enums;
using Collector.Common.Extensions.Strings;

namespace Collector.Common
{
    public static class Downloads
    {
        public static QueueFileType GetFileType(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return QueueFileType.Unknown;

            string extension = filename.GetFileExtension().ToLower();

            // Web page extensions
            if (extension == "html" || extension == "htm" || extension == "aspx" || extension == "php" || extension == "jsp")
                return QueueFileType.Webpage;

            // Image extensions
            if (extension == "jpg" || extension == "jpeg" || extension == "png" || extension == "gif" ||
                extension == "bmp" || extension == "webp" || extension == "svg" || extension == "ico")
                return QueueFileType.Image;

            // Document extensions
            if (extension == "pdf" || extension == "doc" || extension == "docx" || extension == "xls" ||
                extension == "xlsx" || extension == "ppt" || extension == "pptx" || extension == "txt" ||
                extension == "rtf" || extension == "csv" || extension == "md" || extension == "json")
                return QueueFileType.Document;

            // Zip/archive extensions
            if (extension == "zip" || extension == "rar" || extension == "7z" || extension == "tar" ||
                extension == "gz" || extension == "bz2")
                return QueueFileType.Zip;

            // Video extensions
            if (extension == "mp4" || extension == "avi" || extension == "mov" || extension == "wmv" ||
                extension == "flv" || extension == "mkv" || extension == "webm")
                return QueueFileType.Video;

            // Audio extensions
            if (extension == "mp3" || extension == "wav" || extension == "ogg" || extension == "flac" ||
                extension == "aac" || extension == "wma")
                return QueueFileType.Audio;

            // App/executable extensions
            if (extension == "exe" || extension == "msi" || extension == "apk" || extension == "dmg" ||
                extension == "app" || extension == "deb" || extension == "rpm")
                return QueueFileType.App;

            // Default to Unknown for any other extension
            return QueueFileType.Unknown;
        }
    }
}
