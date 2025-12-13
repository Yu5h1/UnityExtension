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
    RegisterKeyListeners: function (objNamePtr) {
        var objName = UTF8ToString(objNamePtr);

        //--------------------------------------------
        // IME 狀態管理
        //--------------------------------------------
        window._trueIMEActive = false;      // 是否真的在組字（以 compositionstart/end 為準）
        window._imeEnterPhase = 0;          // 0=非IME enter, 1=IME enter #1, 2=IME enter #2 已處理

        document.addEventListener("compositionstart", () => {
            window._trueIMEActive = true;
            window._imeEnterPhase = 0;
        });

        document.addEventListener("compositionend", () => {
            // 不馬上關閉，因為第二個 Process Enter 會出現在 compositionend 之後
            // 留到下一次 keydown 內判斷完，再關閉
            window._trueIMEActive = false;
        });

        //--------------------------------------------
        // 封裝送 KeyMessage 給 Unity
        //--------------------------------------------
        function sendKeyDown(key, code, keyCode, isComposing, imeActive, e) {
            var data = JSON.stringify({
                key: key,
                code: code,
                keyCode: keyCode,
                shift: !!e.shiftKey,
                ctrl: !!e.ctrlKey,
                alt: !!e.altKey,
                meta: !!e.metaKey,
                isComposing: !!isComposing,
                imeActive: !!imeActive
            });
            SendMessage(objName, "OnKeyDown", data);
        }

        function sendKeyUp(key, code, keyCode, isComposing, imeActive, e) {
            var data = JSON.stringify({
                key: key,
                code: code,
                keyCode: keyCode,
                shift: !!e.shiftKey,
                ctrl: !!e.ctrlKey,
                alt: !!e.altKey,
                meta: !!e.metaKey,
                isComposing: !!isComposing,
                imeActive: !!imeActive
            });
            SendMessage(objName, "OnKeyUp", data);
        }

        //--------------------------------------------
        // keydown
        //--------------------------------------------
        var keyDownHandler = function (e) {

            // 過濾無 code
            if (!e.code) return;

            //-----------------------------
            // (A) 非 IME 模式
            //-----------------------------
            if (!window._trueIMEActive) {

                // Chrome 會在「使用中文輸入法，但未組字」時仍送 key=Process/229
                // 非 IME 模式下 → process 229 不應啟動 IME enter 判斷
                if (e.code === "Enter" && e.keyCode === 229) {
                    // 將此視為真正 Enter（使用者按 Enter，只是 Chrome 的 bug）
                    sendKeyDown("Enter", "Enter", 13, false, false, e);
                    return;
                }

                // 其他鍵直接送出
                sendKeyDown(e.key, e.code, e.keyCode, false, false, e);
                return;
            }

            //-----------------------------
            // (B) IME 模式（面板開啟）
            //-----------------------------
            if (e.code === "Enter" && e.keyCode === 229) {

                if (window._imeEnterPhase === 0) {
                    // IME Enter#1 → 選字/關閉面板 → 忽略
                    window._imeEnterPhase = 1;
                    return;
                }

                if (window._imeEnterPhase === 1) {
                    // IME Enter#2 → 上屏/提交 → 視為真正 Enter 丟給 Unity
                    window._imeEnterPhase = 2;

                    sendKeyDown("Enter", "Enter", 13, false, false, e);
                    return;
                }
            }

            //-----------------------------
            // (C) 重置 IME Enter 狀態（其它鍵）
            //-----------------------------
            window._imeEnterPhase = 0;

            // 正常鍵
            sendKeyDown(
                e.key,
                e.code,
                e.keyCode,
                e.isComposing,
                window._trueIMEActive,
                e
            );
        };

        //--------------------------------------------
        // keyup
        //--------------------------------------------
        var keyUpHandler = function (e) {
            if (!e.code) return;

            // IME Process key 不送 keyup
            if (e.code === "Enter" && e.keyCode === 229) {
                return;
            }

            sendKeyUp(
                e.key,
                e.code,
                e.keyCode,
                e.isComposing,
                window._trueIMEActive,
                e
            );
        };

        //--------------------------------------------
        // 註冊事件
        //--------------------------------------------
        window._unityKeyDownHandler = keyDownHandler;
        window._unityKeyUpHandler = keyUpHandler;

        document.addEventListener("keydown", keyDownHandler);
        document.addEventListener("keyup", keyUpHandler);
    },

	UnregisterKeyListeners: function () {

		if (window._unityKeyDownHandler) {
			document.removeEventListener("keydown", window._unityKeyDownHandler);
			window._unityKeyDownHandler = null;
		}

		if (window._unityKeyUpHandler) {
			document.removeEventListener("keyup", window._unityKeyUpHandler);
			window._unityKeyUpHandler = null;
		}

		if (window._unityCompositionStartHandler) {
			document.removeEventListener("compositionstart", window._unityCompositionStartHandler);
			window._unityCompositionStartHandler = null;
		}

		if (window._unityCompositionEndHandler) {
			document.removeEventListener("compositionend", window._unityCompositionEndHandler);
			window._unityCompositionEndHandler = null;
		}

		window._trueIMEActive = false;
		window._imeEnterPhase = 0;
	}
});