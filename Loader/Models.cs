using UnityEngine;

namespace Ghost.PluginsLoader.Models {
    public class Plugin {
        public GameObject GameObject;
        public Plugin() { }
        public Plugin(GameObject GameObject) {
            this.GameObject = GameObject;
        }
    }
}