using Plugable.io.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Plugable.io
{
    public static class Extensions
    {
        private static Dictionary<string, Dictionary<string, IPlugable>> _cache = new Dictionary<string, Dictionary<string, IPlugable>>();
        private static string current = null;

        #region ResolveAssembly
        public static void ResolveAssembly(string path, dynamic plugin, dynamic server, string[] args)
        {
            current = path;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            plugin.Initialize(args, server);
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
            current = null;
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyFile = (args.Name.Contains(','))
                    ? args.Name.Substring(0, args.Name.IndexOf(','))
                    : args.Name;

            if (!string.IsNullOrEmpty(assemblyFile) && !string.IsNullOrEmpty(current))
            {
                using (ZipArchive archive = ZipFile.OpenRead(current))
                {
                    var entry = archive.GetEntry(assemblyFile + ".dll");
                    if (entry != null)
                    {
                        try
                        {
                            var stream = entry.Open();
                            byte[] bytes;
                            using (var ms = new MemoryStream())
                            {
                                stream.CopyTo(ms);
                                bytes = ms.ToArray();
                            }
                            stream.Close();

                            return Assembly.Load(bytes);
                        }
                        catch (Exception ex)
                        {
                            if (ex is ReflectionTypeLoadException)
                            {
                                var typeLoadException = ex as ReflectionTypeLoadException;
                                var loaderExceptions = typeLoadException.LoaderExceptions;

                                foreach (var exx in loaderExceptions)
                                {
                                    var stringBuilder = new StringBuilder();
                                    stringBuilder.Append(entry.FullName);
                                    stringBuilder.Append(Environment.NewLine);
                                    stringBuilder.Append(exx.Message);
                                    stringBuilder.Append(Environment.NewLine);
                                    stringBuilder.Append(exx.StackTrace);
                                    Error(stringBuilder.ToString());
                                }
                            }
                            else
                            {
                                var stringBuilder = new StringBuilder();
                                stringBuilder.Append(entry.FullName);
                                stringBuilder.Append(Environment.NewLine);
                                stringBuilder.Append(ex.Message);
                                stringBuilder.Append(Environment.NewLine);
                                stringBuilder.Append(ex.StackTrace);
                                Error(stringBuilder.ToString());
                            }
                        }
                    }
                }
            }

            return null;
        }
        #endregion

        #region Log/Error
        private static string LogMessage(DateTime timestamp, string message, string title,
            [CallerMemberName] string callingMethod = null,
            [CallerFilePath] string callingFilePath = null,
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            Console.ForegroundColor = ConsoleColor.White;

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(timestamp.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture));
            stringBuilder.Append(" ");
            stringBuilder.Append(("[" + title.ToUpper() + "]").PadRight(8));
            stringBuilder.Append("(" + callingMethod + " : " + callingFileLineNumber + ") ");
            stringBuilder.Append(message);
            return stringBuilder.ToString();
        }

        private static string LogMessage(string message, string title, bool writeToFile, string callingMethod, string callingFilePath, int callingFileLineNumber, bool includeHeader = true)
        {
            var stringBuilder = new StringBuilder();
            if (includeHeader)
            {
                if (writeToFile && !string.IsNullOrEmpty(message))
                {
                    if (!Directory.Exists("logs")) Directory.CreateDirectory("logs");

                    var info = callingMethod + Environment.NewLine;
                    info += callingFilePath + Environment.NewLine;
                    info += callingFileLineNumber + Environment.NewLine;
                    info += Environment.NewLine + Environment.NewLine;
                    info += message;

                    var file = DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss_", CultureInfo.InvariantCulture) + title.ToLower() + ".txt";
                    File.WriteAllText(Path.Combine("logs", file), info);
                }
                stringBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture));
                stringBuilder.Append(" ");


                // Append a readable representation of the log level
                stringBuilder.Append(("[" + title.ToUpper() + "]").PadRight(8));
            }
            stringBuilder.Append("(" + callingMethod + " : " + callingFileLineNumber + ") ");

            // Append the message
            stringBuilder.Append(message);

            return stringBuilder.ToString();
        }

        public static void Log(string message,
            bool writeToFile = false,
            [CallerMemberName] string callingMethod = null,
            [CallerFilePath] string callingFilePath = null,
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            //lock (errorState)
            {
                Console.ForegroundColor = ConsoleColor.White;
                var msg = LogMessage(message, "INFO", writeToFile, callingMethod, callingFilePath, callingFileLineNumber);
                Console.WriteLine(msg);
                Console.ResetColor();
            }
        }

        public static void Error(string message,
            bool writeToFile = true,
            [CallerMemberName] string callingMethod = null,
            [CallerFilePath] string callingFilePath = null,
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            //lock (lockState)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                var msg = LogMessage(message, "ERROR", writeToFile, callingMethod, callingFilePath, callingFileLineNumber);
                Console.WriteLine(msg);
                Console.ResetColor();
            }
        }
        #endregion

        #region GetPlugableByFile
        public static List<T> GetPlugableByFile<T>(this string path)
        {
            if (!File.Exists(path)) { return null; } //make sure file exists

            var implementors = new List<T>();

            try
            {
                //Log("Loading: " + Path.GetFileName(path));

                var name = AssemblyName.GetAssemblyName(path);
                Assembly.Load(name)
                    .GetTypes()
                    .Where(t => t != typeof(T) && typeof(T).IsAssignableFrom(t))
                    .ToList()
                    .ForEach(x => implementors.Add((T)Activator.CreateInstance(x)));
            }
            catch (Exception ex)
            {
                if (ex is ReflectionTypeLoadException)
                {
                    var typeLoadException = ex as ReflectionTypeLoadException;
                    var loaderExceptions = typeLoadException.LoaderExceptions;

                    foreach (var exx in loaderExceptions)
                    {
                        var stringBuilder = new StringBuilder();
                        stringBuilder.Append(path);
                        stringBuilder.Append(Environment.NewLine);
                        stringBuilder.Append(exx.Message);
                        stringBuilder.Append(Environment.NewLine);
                        stringBuilder.Append(exx.StackTrace);

                        Error(stringBuilder.ToString());
                    }
                }
                else
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append(path);
                    stringBuilder.Append(Environment.NewLine);
                    stringBuilder.Append(ex.Message);
                    stringBuilder.Append(Environment.NewLine);
                    stringBuilder.Append(ex.StackTrace);

                    Error(stringBuilder.ToString());
                }
            }

            return implementors;
        }
        #endregion

        #region GetPlugableByDirectory
        public static List<T> GetPlugableByDirectory<T>(this string path, string searchPattern = "*.dll")
        {
            if (!Directory.Exists(path)) { return null; } //make sure directory exists

            var implementors = new List<T>();
            var files = Directory.GetFiles(path, searchPattern);

            int index = 0;
            int total = files.Length;
            var timestamp = DateTime.Now;

            foreach (var file in files) //loop through all dll files in directory
            {
                int percent = (int)Math.Round((double)(100 * (++index)) / total);
                var msg = LogMessage(timestamp, string.Format("[{0}%] {1}", percent, Path.GetFileName(path)), "INFO");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("\r{0}", msg);
                Console.ResetColor();

                implementors.AddRange(GetPlugableByFile<T>(file));
                Console.WriteLine();
            }
            return implementors;
        }
        #endregion

        #region GetPlugable
        public static List<T> GetPlugable<T>(this string path, string searchPattern = "*.dll")
        {
            if (string.IsNullOrEmpty(path)) { return null; } //sanity check

            var implementors = new List<T>();
            if (File.Exists(path) && 
                Path.GetExtension(path) == (searchPattern.StartsWith("*") ? searchPattern.TrimStart('*') : searchPattern)) 
                    implementors.AddRange(GetPlugableByFile<T>(path));
            else if (Directory.Exists(path)) implementors.AddRange(GetPlugableByDirectory<T>(path, searchPattern));

            return implementors;
        }

        public static List<IPlugable> GetPlugable(this string path, string name)
        {
            if (string.IsNullOrEmpty(path)) { return null; } //sanity check

            var files = new List<string>();
            if (File.Exists(path) && Path.GetExtension(path) == ".zip") files.Add(path);
            else if (Directory.Exists(path)) files.AddRange(Directory.GetFiles(path, "*.zip"));

            Log("[" + name + "] Plugins path: " + path);

            var implementors = new List<IPlugable>();

            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            foreach (var f in files)
            {
                current = f;
                if (_cache.ContainsKey(Path.GetFileName(f)))
                {
                   var timestamp = DateTime.Now;
                    foreach (var key in _cache[Path.GetFileName(f)].Keys)
                    {
                        var msg = LogMessage(timestamp, string.Format("[CACHE] {0}", Path.GetFileName(f)), "INFO");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("\r{0}", msg);
                        Console.ResetColor();

                        implementors.Add(_cache[Path.GetFileName(f)][key]);
                    }
                    Console.WriteLine();
                }
                else
                {
                    _cache[Path.GetFileName(f)] = new Dictionary<string, IPlugable>();

                    using (ZipArchive archive = ZipFile.OpenRead(f))
                    {
                        int index = 0;
                        int total = archive.Entries.Count;
                        var timestamp = DateTime.Now;

                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            int percent = (int)Math.Round((double)(100 * (++index)) / total);
                            var msg = LogMessage(timestamp, string.Format("[{0}%] {1}", percent, Path.GetFileName(f)), "INFO");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write("\r{0}", msg);
                            Console.ResetColor();

                            if (Path.GetExtension(entry.FullName) == ".dll")
                            {
                                try
                                {
                                    var stream = entry.Open();
                                    byte[] bytes;
                                    using (var ms = new MemoryStream())
                                    {
                                        stream.CopyTo(ms);
                                        bytes = ms.ToArray();
                                    }
                                    stream.Close();
                                    
                                    foreach (var i in Assembly.Load(bytes)
                                        .GetTypes()
                                        .Where(t => t != typeof(IPlugable) && typeof(IPlugable).IsAssignableFrom(t)))
                                    {
                                        var o = (IPlugable)Activator.CreateInstance(i);

                                        implementors.Add(o);

                                        _cache[Path.GetFileName(f)].Add(Path.GetFileName(entry.FullName), o);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (ex is ReflectionTypeLoadException)
                                    {
                                        var typeLoadException = ex as ReflectionTypeLoadException;
                                        var loaderExceptions = typeLoadException.LoaderExceptions;

                                        foreach (var exx in loaderExceptions)
                                        {
                                            var stringBuilder = new StringBuilder();
                                            stringBuilder.Append(entry.FullName);
                                            stringBuilder.Append(Environment.NewLine);
                                            stringBuilder.Append(exx.Message);
                                            stringBuilder.Append(Environment.NewLine);
                                            stringBuilder.Append(exx.StackTrace);
                                            Error(stringBuilder.ToString());
                                        }
                                    }
                                    else
                                    {
                                        var stringBuilder = new StringBuilder();
                                        stringBuilder.Append(entry.FullName);
                                        stringBuilder.Append(Environment.NewLine);
                                        stringBuilder.Append(ex.Message);
                                        stringBuilder.Append(Environment.NewLine);
                                        stringBuilder.Append(ex.StackTrace);
                                        Error(stringBuilder.ToString());
                                    }
                                }

                            }
                        }

                        Console.WriteLine();
                    }
                }
                current = null;
            }
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
            return implementors;
        }
        #endregion

        #region GetAssemblyFromPlugin
        public static Assembly GetAssemblyFromPlugin(this string plugin, string assembly)
        {
            if (!string.IsNullOrEmpty(assembly) && !string.IsNullOrEmpty(plugin))
            {
                using (ZipArchive archive = ZipFile.OpenRead(plugin))
                {
                    var entry = archive.GetEntry(assembly + ".dll");
                    if (entry != null)
                    {
                        try
                        {
                            var stream = entry.Open();
                            byte[] bytes;
                            using (var ms = new MemoryStream())
                            {
                                stream.CopyTo(ms);
                                bytes = ms.ToArray();
                            }
                            stream.Close();

                            return Assembly.Load(bytes);
                        }
                        catch (Exception ex)
                        {
                            if (ex is ReflectionTypeLoadException)
                            {
                                var typeLoadException = ex as ReflectionTypeLoadException;
                                var loaderExceptions = typeLoadException.LoaderExceptions;

                                foreach (var exx in loaderExceptions)
                                {
                                    var stringBuilder = new StringBuilder();
                                    stringBuilder.Append(entry.FullName);
                                    stringBuilder.Append(Environment.NewLine);
                                    stringBuilder.Append(exx.Message);
                                    stringBuilder.Append(Environment.NewLine);
                                    stringBuilder.Append(exx.StackTrace);
                                    Error(stringBuilder.ToString());
                                }
                            }
                            else
                            {
                                var stringBuilder = new StringBuilder();
                                stringBuilder.Append(entry.FullName);
                                stringBuilder.Append(Environment.NewLine);
                                stringBuilder.Append(ex.Message);
                                stringBuilder.Append(Environment.NewLine);
                                stringBuilder.Append(ex.StackTrace);
                                Error(stringBuilder.ToString());
                            }
                        }
                    }
                }
            }

            return null;
        }
        #endregion
    }
}
