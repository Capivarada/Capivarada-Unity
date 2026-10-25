mergeInto(LibraryManager.library, {
  CapivaradaExitFullscreenAndReload: function () {
    var documentRef = window.document;
    var reload = function () { window.location.reload(); };
    var exitFullscreen = documentRef.exitFullscreen ||
      documentRef.webkitExitFullscreen ||
      documentRef.mozCancelFullScreen ||
      documentRef.msExitFullscreen;

    if (!documentRef.fullscreenElement || !exitFullscreen) {
      reload();
      return;
    }

    var result = exitFullscreen.call(documentRef);
    if (result && result.then) {
      result.then(reload).catch(reload);
    } else {
      window.setTimeout(reload, 100);
    }
  }
});
