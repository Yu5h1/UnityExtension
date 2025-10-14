mergeInto(LibraryManager.library, {
    GetConfig: function () {
		var configStr = JSON.stringify(window.config);
		var buffer = stringToUTF8OnStack(configStr);
        return buffer;
    }
});