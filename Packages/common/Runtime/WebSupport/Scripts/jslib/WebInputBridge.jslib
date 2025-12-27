mergeInto(LibraryManager.library, {
    JS_MobileKeyboard_SetTextSelection: function(start, length) {
        if (!window.mobile_input || !window.mobile_input.input) return;
        
        var input = window.mobile_input.input;
        
        if (input.selectionStart === undefined) {
            return;
        }
        
        input.setSelectionRange(start, start + length);
    },

    RegisterKeyListeners: function(objNamePtr) {
        var objName = UTF8ToString(objNamePtr);

        //--------------------------------------------
        // IME 狀態管理（簡化版）
        //--------------------------------------------
        window._isComposing = false;

        window._compositionStartHandler = function() {
            window._isComposing = true;
        };

        window._compositionEndHandler = function() {
            window._isComposing = false;
        };

        document.addEventListener("compositionstart", window._compositionStartHandler);
        document.addEventListener("compositionend", window._compositionEndHandler);

        //--------------------------------------------
        // 送 KeyMessage 給 Unity
        //--------------------------------------------
        function sendKeyDown(e) {
            var data = JSON.stringify({
                key: e.key,
                code: e.code,
                keyCode: e.keyCode,
                shift: !!e.shiftKey,
                ctrl: !!e.ctrlKey,
                alt: !!e.altKey,
                meta: !!e.metaKey,
                isComposing: !!e.isComposing,
                imeActive: !!window._isComposing
            });
            SendMessage(objName, "OnKeyDown", data);
        }

        function sendKeyUp(e) {
            var data = JSON.stringify({
                key: e.key,
                code: e.code,
                keyCode: e.keyCode,
                shift: !!e.shiftKey,
                ctrl: !!e.ctrlKey,
                alt: !!e.altKey,
                meta: !!e.metaKey,
                isComposing: !!e.isComposing,
                imeActive: !!window._isComposing
            });
            SendMessage(objName, "OnKeyUp", data);
        }

        //--------------------------------------------
        // keydown
        //--------------------------------------------
        window._keyDownHandler = function(e) {
            if (!e.code) return;

            // 229 = IME 正在處理 → 不送給 Unity
            if (e.keyCode === 229) {
                return;
            }

            sendKeyDown(e);
        };

        //--------------------------------------------
        // keyup
        //--------------------------------------------
        window._keyUpHandler = function(e) {
            if (!e.code) return;

            // 229 = IME 正在處理 → 不送給 Unity
            if (e.keyCode === 229) {
                return;
            }

            sendKeyUp(e);
        };

        //--------------------------------------------
        // 註冊事件
        //--------------------------------------------
        document.addEventListener("keydown", window._keyDownHandler);
        document.addEventListener("keyup", window._keyUpHandler);
    },

    UnregisterKeyListeners: function() {
        if (window._keyDownHandler) {
            document.removeEventListener("keydown", window._keyDownHandler);
            window._keyDownHandler = null;
        }

        if (window._keyUpHandler) {
            document.removeEventListener("keyup", window._keyUpHandler);
            window._keyUpHandler = null;
        }

        if (window._compositionStartHandler) {
            document.removeEventListener("compositionstart", window._compositionStartHandler);
            window._compositionStartHandler = null;
        }

        if (window._compositionEndHandler) {
            document.removeEventListener("compositionend", window._compositionEndHandler);
            window._compositionEndHandler = null;
        }

        window._isComposing = false;
    }
});