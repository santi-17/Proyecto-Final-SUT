mergeInto(LibraryManager.library, {
  __onUnitySceneReady: function (ptr) {
    // Convierte el puntero C/IL2CPP a string JS
    var sceneName = UTF8ToString(ptr);
    try {
      // Llama el callback global que setea tu React/Provider
      var cb = (typeof window !== "undefined" && window.__onUnitySceneReady)
            || (typeof globalThis !== "undefined" && globalThis.__onUnitySceneReady);
      if (typeof cb === "function") cb(sceneName);
    } catch (e) {
      if (typeof console !== "undefined" && console.error) {
        console.error("[WebGL] __onUnitySceneReady error:", e);
      }
    }
  }
});