mergeInto(LibraryManager.library, {
  onUnitySendData: function (jsonPtr) {
    const json = UTF8ToString(jsonPtr);
    if (typeof window.onUnitySendData === "function") {
      window.onUnitySendData(json);
    } else {
      console.warn("[SendData.jslib] window.onUnitySendData no existe aún.");
    }
  }
});
