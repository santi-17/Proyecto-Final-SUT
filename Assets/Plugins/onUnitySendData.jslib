mergeInto(LibraryManager.library, {
  onUnitySendData: function (jsonPtr) {
    var jsonStr = UTF8ToString(jsonPtr);
    if (typeof window.onUnitySendData === "function") {
      window.onUnitySendData(jsonStr);
    }
  }
});
