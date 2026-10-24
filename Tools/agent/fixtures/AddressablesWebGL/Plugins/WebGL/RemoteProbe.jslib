// Keep the test result visible in the host page, outside the WebGL canvas.
mergeInto(LibraryManager.library, {
  RemoteProbeReport: function(message) {
    var text = UTF8ToString(message);
    var output = document.getElementById('remote-result');
    if (output) output.textContent = text;
  }
});
