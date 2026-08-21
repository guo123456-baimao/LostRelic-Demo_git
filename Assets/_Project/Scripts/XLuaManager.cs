using System.IO;
using System.Text;
using UnityEngine;
using XLua;

namespace LostRelic
{
    public static class XLuaManager
    {
        private const string LuaFolder = "Assets/_Project/Lua/";
        private static LuaEnv _env;

        public static bool IsReady
        {
            get { return _env != null; }
        }

        public static void Initialize(string entryModule)
        {
            if (_env != null)
            {
                return;
            }

            _env = new LuaEnv();
            _env.AddLoader((ref string module) =>
            {
                var address = LuaFolder + module + ".lua.txt";
                var text = string.Empty;

                // In the editor, prefer the source files on disk so Play mode
                // never runs stale Addressables bundles during development.
                if (Application.isEditor)
                {
                    var filePath = Path.Combine(
                        Application.dataPath,
                        "_Project",
                        "Lua",
                        module + ".lua.txt");
                    if (File.Exists(filePath))
                    {
                        text = File.ReadAllText(filePath);
                    }
                }

                if (string.IsNullOrEmpty(text))
                {
                    text = ResService.LoadText(address);
                }

                if (string.IsNullOrEmpty(text))
                {
                    Debug.LogError("[XLua] Failed to load module " + address);
                    return null;
                }

                return Encoding.UTF8.GetBytes(text);
            });

            var entry = "main = require '" + entryModule + "'; main.start()";
            _env.DoString(entry, "LostRelicBoot");
            Debug.Log("[XLua] Game logic loaded from Addressables: " + LuaFolder + entryModule + ".lua.txt");
        }

        public static void Tick()
        {
            if (_env == null)
            {
                return;
            }

            _env.Tick();
            var update = _env.Global.Get<LuaFunction>("on_update");
            if (update != null)
            {
                update.Call(Time.deltaTime);
            }
        }

        public static void Dispose()
        {
            if (_env == null)
            {
                return;
            }

            var shutdown = _env.Global.Get<LuaFunction>("on_shutdown");
            if (shutdown != null)
            {
                try
                {
                    shutdown.Call();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[XLua] Shutdown callback failed: " + ex.Message);
                }
            }

            try
            {
                _env.Dispose();
            }
            catch (System.InvalidOperationException ex)
            {
                Debug.LogWarning("[XLua] LuaEnv disposed with pending callbacks: " + ex.Message);
            }
            _env = null;
        }
    }
}
