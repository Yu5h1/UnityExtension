mergeInto(LibraryManager.library, {
    JS_MobileKeyboard_SetTextSelection: function(start, length) {
        if (!window.mobile_input || !window.mobile_input.input) return;
        
        var input = window.mobile_input.input;
        
        // 直接檢查是否支援 selectionStart 屬性
        if (input.selectionStart === undefined) {
            return; // 不支援選取的 input type，直接返回
        }
        
        input.setSelectionRange(start, start + length);
    },
    RegisterKeyListeners: function(objNamePtr) {
		var objName = UTF8ToString(objNamePtr);

		var keyDownHandler = function(e) {
        
			if (e.repeat) return;
			
	        if (!e.code) {
                console.warn('[WebInput] keydown event with no e.code:', e);
                return;
            }
                        
            var keyData = JSON.stringify({
                code: e.code,
                shift: e.shiftKey,
                ctrl: e.ctrlKey,
                alt: e.altKey,
                meta: e.metaKey
            });
            
            SendMessage(objName, "OnKeyDown", keyData);		  
		};

		var keyUpHandler = function(e) {
            if (!e.code) {
                console.warn('[WebInput] keyup event with no e.code:', e);
                return;
            }
            
            var keyData = JSON.stringify({
                code: e.code,
                shift: e.shiftKey,
                ctrl: e.ctrlKey,
                alt: e.altKey,
                meta: e.metaKey
            });
            
            SendMessage(objName, "OnKeyUp", keyData);
		};

		window._unityKeyDownHandler = keyDownHandler;
		window._unityKeyUpHandler = keyUpHandler;

		document.addEventListener("keydown", keyDownHandler);
		document.addEventListener("keyup", keyUpHandler);
	},
	UnregisterKeyListeners: function() {
		if (window._unityKeyDownHandler) {
		  document.removeEventListener("keydown", window._unityKeyDownHandler);
		  window._unityKeyDownHandler = null;
		}
		if (window._unityKeyUpHandler) {
		  document.removeEventListener("keyup", window._unityKeyUpHandler);
		  window._unityKeyUpHandler = null;
		}
	}
});