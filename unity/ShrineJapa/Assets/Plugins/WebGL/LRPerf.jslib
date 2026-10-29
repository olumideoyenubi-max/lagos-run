// lets the game change how many pixels the browser canvas draws (read every frame by Unity's canvas sizing)
mergeInto(LibraryManager.library, {
  LRGetDPR: function () { return Module.devicePixelRatio || window.devicePixelRatio || 1; },
  LRSetDPR: function (r) { Module.devicePixelRatio = r; }
});
