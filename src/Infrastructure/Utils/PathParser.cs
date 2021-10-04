using System.IO;

namespace Infrastructure.Utils
{
    public static class PathParser
    {
        public static string GetAbsolutePath(string path)
        {
            return GetAbsolutePath(null, path);
        }

        public static string GetAbsolutePath(string basePath, string path)
        {
            //Thanks to: https://stackoverflow.com/questions/1399008/how-to-convert-a-relative-path-to-an-absolute-path-in-a-windows-application/35218619

            if (path == null)
                return null;
            if (basePath == null)
                basePath = Path.GetFullPath("."); // quick way of getting current working directory
            else
                basePath = GetAbsolutePath(null, basePath); // to be REALLY sure ;)
            string finalPath;
            // specific for windows paths starting on \ - they need the drive added to them.
            // I constructed this piece like this for possible Mono support.
            if (!Path.IsPathRooted(path) || "\\".Equals(Path.GetPathRoot(path)))
            {
                if (path.StartsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    var basePathRoot = Path.GetPathRoot(basePath);
                    finalPath = string.IsNullOrEmpty(basePathRoot)
                        ? path.TrimStart(Path.DirectorySeparatorChar)
                        : Path.Combine(basePathRoot, path.TrimStart(Path.DirectorySeparatorChar));
                }
                else
                    finalPath = Path.Combine(basePath, path);
            }
            else
                finalPath = path;

            // resolves any internal "..\" to get the true full path.
            return Path.GetFullPath(finalPath);
        }
    }
}
