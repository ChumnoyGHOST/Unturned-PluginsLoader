using Rocket.API;

namespace Ghost.PluginsLoader {
    public class Configuration : IRocketPluginConfiguration {
        public bool LoaderEnabled;
        public string License;
        public void LoadDefaults() {
            LoaderEnabled = false;
            License = "XXXX-XXXX-XXXX-XXXX";
        }
    }
}