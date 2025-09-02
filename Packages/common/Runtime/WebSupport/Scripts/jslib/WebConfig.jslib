mergeInto(LibraryManager.library, {
    GetConfig: function () {
        var configStr = JSON.stringify(window.config);
        var buffer = allocate(intArrayFromString(configStr), 'i8', ALLOC_STACK);
        return buffer;
    }
});