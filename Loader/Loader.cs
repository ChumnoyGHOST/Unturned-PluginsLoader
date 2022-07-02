using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using Rocket.API.Collections;
using Rocket.Core;
using Rocket.Core.Plugins;
using Rocket.Core.Utils;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Ghost.PluginsLoader {
    public class Loader : RocketPlugin<Configuration> {
        public List<Models.Plugin> Plugins = new List<Models.Plugin>();
        WebClient WebClient = new WebClient();
        const string Handler = "https://localhost/loader/handler.php";
        private string GetServerResponse(string LoaderVersion, string License, string ServerPort) {
            string Response = "null";
            System.Collections.Specialized.NameValueCollection RequestData = new System.Collections.Specialized.NameValueCollection() {
                {"LoaderVersion", LoaderVersion},
                {"License", License},
                {"ServerPort", ServerPort},
            };
            byte Attempt = 0;
            bool Success = false;
            while ((Attempt != 10) && !Success) {
                try {
                    Response = Encoding.UTF8.GetString(WebClient.UploadValues(Handler, "POST", RequestData));
                    Success = true;
                } catch (Exception) {
                    Attempt++;
                    Logger.Log(Translate("GetResponse_Attempt", Attempt));
                    Success = false;
                }
                if (Success) {
                    Logger.Log(Translate("GetResponse_Success"));
                }
                if (Attempt == 10) {
                    Logger.Log(Translate("GetResponse_Error"));
                }
            }
            return Response;
        }
        protected override void Load() {
            base.Load();
            LoadPlugins();
            Logger.Log(Translate("LoaderVersion", Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            if (Configuration.Instance.LoaderEnabled) {
                Logger.Log(Translate("LoaderStatus_Enabled"));
                Logger.Log(Translate("LoadingLog"));
                List<Models.Plugin> ListPlugins = new List<Models.Plugin>();
                string ServerResponse = GetServerResponse(Assembly.GetExecutingAssembly().GetName().Version.ToString(), Configuration.Instance.License, SDG.Unturned.Provider.port.ToString());
                if (ServerResponse != "null") {
                    Newtonsoft.Json.Linq.JObject Object = Newtonsoft.Json.Linq.JObject.Parse(ServerResponse);
                    if ((bool)Object["ValidLoaderVersion"]) {
                        Logger.Log(Translate("Validation_LoaderVersion_Success"));
                        if ((bool)Object["ValidLicense"]) {
                            Logger.Log(Translate("Validation_License_Success"));
                            if ((bool)Object["ValidAddress"]) {
                                Logger.Log(Translate("Validation_Address_Success"));
                                Array UserPluginsNames = Object["UserPluginsNames"].ToString().Split(',');
                                Logger.Log(Translate("LoadedPlugins"));
                                foreach (string PluginName in UserPluginsNames) {
                                    Logger.Log(" " + PluginName);
                                }
                                var PluginsBase64 = Object["UserPlugins"].ToString().Split(',');
                                foreach (var PluginBase64 in PluginsBase64) {
                                    var RawPlugin = Convert.FromBase64String(PluginBase64);
                                    if (RawPlugin.Length <= 0) continue;
                                    try {
                                        var PluginAssembly = Assembly.Load(RawPlugin);
                                        List<Type> Types = RocketHelper.GetTypesFromInterface(PluginAssembly, "IRocketPlugin");
                                        foreach (var Item in Types) {
                                            GameObject gameObject = new GameObject(Item.Name, new Type[] {Item});
                                            DontDestroyOnLoad(gameObject);
                                            ListPlugins.Add(new Models.Plugin(gameObject));
                                        }
                                    } catch (Exception) {
                                        Logger.Log(Translate("UnknownError"));
                                    }
                                }
                                Plugins = ListPlugins;
                                try {
                                    var Type = R.Plugins.GetType();
                                    var Field = Type.GetField("plugins", BindingFlags.NonPublic | BindingFlags.Static);
                                    if (Field == null) return;
                                    var RocketPlugins = Field.GetValue(R.Plugins) as List<GameObject>;
                                    foreach (var PluginFromList in ListPlugins) {
                                        RocketPlugins.Add(PluginFromList.GameObject);
                                    }
                                    Field.SetValue(R.Plugins, RocketPlugins);
                                } catch (Exception) {
                                    Logger.Log(Translate("UnknownError"));
                                }
                                Logger.Log(Translate("Operations_Completion"));
                            } else {
                                Logger.Log(Translate("Validation_Address_Error"));
                                Logger.Log(Translate("Operations_Termination"));
                                AllPluginsUnload();
                                UnloadPlugin();
                            }
                        } else {
                            Logger.Log(Translate("Validation_License_Error"));
                            Logger.Log(Translate("Operations_Termination"));
                            AllPluginsUnload();
                            UnloadPlugin();
                        }
                    } else {
                        Logger.Log(Translate("Validation_LoaderVersion_Error"));
                        Logger.Log(Translate("Operations_Termination"));
                        AllPluginsUnload();
                        UnloadPlugin();
                    }
                } else {
                    Logger.Log(Translate("Operations_Termination"));
                    AllPluginsUnload();
                    UnloadPlugin();
                }
            } else {
                Logger.Log(Translate("LoaderStatus_Disabled"));
            }
        }
        protected override void Unload() {
            AllPluginsUnload();
            try {
                var Type = R.Plugins.GetType();
                var Field = Type.GetField("plugins", BindingFlags.NonPublic | BindingFlags.Static);
                if (Field == null) return;
                var RocketPlugins = Field.GetValue(R.Plugins) as List<GameObject>;
                foreach (var PluginFromList in Plugins) {
                    RocketPlugins.Remove(PluginFromList.GameObject);
                }
                Field.SetValue(R.Plugins, RocketPlugins);
            } catch (Exception) {
                Logger.Log(Translate("UnknownError"));
            }
            Plugins.Clear();
            base.Unload();
        }
        public void LoadPlugins() {
            AllPluginsUnload();
        }
        private void AllPluginsUnload() {
            foreach (var Plugin in Plugins) {
                var Object = Plugin.GameObject;
                Destroy(Object);
            }
            Plugins.Clear();
        }
        public override TranslationList DefaultTranslations {
            get {
                return new TranslationList() {
                    {"LoaderVersion", "Loader version: {0}."},
                    {"LoaderStatus_Enabled", "Loader status: Enabled."},
                    {"LoaderStatus_Disabled", "Loader status: Disabled."},
                    {"LoadingLog", "Loader log:"},
                    {"GetIP_Success", " The IP address was successfully obtained."},
                    {"GetIP_Attempt", " Attempting to get IP address #{0}."},
                    {"GetIP_Error", " Failed to get an IP address."},
                    {"GetResponse_Success", " The response from the server is successfully received."},
                    {"GetResponse_Attempt", " Attempting to get a response from server #{0}."},
                    {"GetResponse_Error", " Failed to get a response from the server."},
                    {"Validation_LoaderVersion_Success", " The version of the loader is up to date."},
                    {"Validation_LoaderVersion_Error", " The version of the loader is out of date."},
                    {"Validation_License_Success", " The license has passed inspection."},
                    {"Validation_License_Error", " The license did not pass inspection."},
                    {"Validation_Address_Success", " The IP address of the server has been verified."},
                    {"Validation_Address_Error", " The server's IP address has not been verified."},
                    {"LoadedPlugins", "Downloaded plugins:"},
                    {"Operations_Termination", "Termination of operations."},
                    {"Operations_Completion", "Completion of operations."},
                    {"UnknownError", "Unknown error."}
                };
            }
        }
    }
}
