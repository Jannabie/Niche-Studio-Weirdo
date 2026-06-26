// BurikoScript.cs
// Wrapper for EthornellEditor.dll (class BurikoScript)
//
// Confirmed method signatures (parsed from .NET metadata):
//   string[] Import(byte[] rawFileData)
//   byte[]   Export(string[] translations)
//   byte[]   Export()
//
// Usage:
//   var w = new BurikoWrapper();
//   if (!w.Load("EthornellEditor.dll")) { /* show error */ }
//   string[] originals = w.Import(File.ReadAllBytes(path));
//   // edit originals[] into translations[]
//   byte[] result = w.Export(translations);
//   File.WriteAllBytes(outPath, result);

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace BGITranslator
{
    public class BurikoWrapper
    {
        private object _instance   = null;
        private Type   _type       = null;
        private bool   _loaded     = false;

        public bool IsLoaded { get { return _loaded; } }
        public string LastError { get; private set; }

        // ── Load DLL ─────────────────────────────────────────────────
        public bool Load(string dllPath)
        {
            if (!File.Exists(dllPath))
            {
                LastError = "File tidak ditemukan:\n" + dllPath;
                return false;
            }

            try
            {
                Assembly asm = Assembly.LoadFrom(dllPath);
                Type[] types = asm.GetTypes();
                foreach (Type t in types)
                {
                    if (t.Name == "BurikoScript")
                    {
                        _type = t;
                        break;
                    }
                }

                if (_type == null)
                {
                    LastError = "Class 'BurikoScript' tidak ditemukan di dalam EthornellEditor.dll.";
                    return false;
                }

                _instance = Activator.CreateInstance(_type);
                _loaded   = true;
                LastError = null;
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return false;
            }
        }

        // ── Import(byte[]) → string[] ─────────────────────────────────
        // Reads raw BGI script bytes, returns array of original strings.
        // Internal 'strings' and 'oriStrings' fields are also populated.
        public string[] Import(byte[] fileBytes)
        {
            if (!_loaded) throw new InvalidOperationException("DLL not loaded.");

            MethodInfo m = _type.GetMethod(
                "Import",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[] { typeof(byte[]) },
                null);

            if (m == null)
                throw new MissingMethodException("BurikoScript.Import(byte[]) not found.");

            object result = m.Invoke(_instance, new object[] { fileBytes });
            return result as string[];
        }

        // ── Export(string[]) → byte[] ──────────────────────────────────
        // Takes array of (translated) strings, returns patched file bytes.
        public byte[] Export(string[] translations)
        {
            if (!_loaded) throw new InvalidOperationException("DLL not loaded.");

            MethodInfo m = _type.GetMethod(
                "Export",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[] { typeof(string[]) },
                null);

            if (m == null)
                throw new MissingMethodException("BurikoScript.Export(string[]) not found.");

            object result = m.Invoke(_instance, new object[] { translations });
            if (result == null)
                throw new Exception("Export() returned null.");
            return (byte[])result;
        }

        // ── Export() → byte[] ─────────────────────────────────────────
        // Uses internal 'strings' field as translations.
        public byte[] Export()
        {
            if (!_loaded) throw new InvalidOperationException("DLL not loaded.");

            MethodInfo m = _type.GetMethod(
                "Export",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            if (m == null)
                throw new MissingMethodException("BurikoScript.Export() not found.");

            object result = m.Invoke(_instance, null);
            if (result == null)
                throw new Exception("Export() returned null.");
            return (byte[])result;
        }
    }
}
