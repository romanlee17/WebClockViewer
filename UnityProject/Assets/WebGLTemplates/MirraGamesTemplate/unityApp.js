const unityApp = {

    applyCommonFixes: function () {
        // Disable unwanted page scroll.
        window.addEventListener("wheel", (event) => event.preventDefault(), {
            passive: false,
        });

        // Disable unwanted key events.
        window.addEventListener("keydown", (event) => {
            if (["ArrowUp", "ArrowDown"].includes(event.key)) {
                event.preventDefault();
            }
        });

        // Disable context menu appearing after right click outside of the unity canvas.
        window.addEventListener('contextmenu', (event) => event.preventDefault());
        document.addEventListener('contextmenu', (event) => event.preventDefault());
    },

    tryRotationLock() {
        const PORTRAIT_ONLY = "{{{ PORTRAIT_ONLY }}}";
        let isPortraitLocked = false;
        if (!unityApp.isEmpty(PORTRAIT_ONLY)) {
            isPortraitLocked = unityApp.toBoolean(PORTRAIT_ONLY);
        }
        console.log("isPortraitLocked", PORTRAIT_ONLY, isPortraitLocked);

        const LANDSCAPE_ONLY = "{{{ LANDSCAPE_ONLY }}}";
        let isLandscapeLocked = false;
        if (!unityApp.isEmpty(LANDSCAPE_ONLY)) {
            isLandscapeLocked = unityApp.toBoolean(LANDSCAPE_ONLY);
        }
        console.log("isLandscapeLocked", LANDSCAPE_ONLY, isLandscapeLocked);

        if (isPortraitLocked && isLandscapeLocked) {
            throw new Error("Both portrait and landscape lock cannot be enabled at the same time.");
        }

        const root = document.createElement("div");

        // Stretch to full screen.
        root.style.background = 'rgb(10, 10, 10, 0.7)';
        root.style.display = 'flex';
        root.style.position = 'fixed';
        root.style.top = '0';
        root.style.left = '0';
        root.style.width = '100%';
        root.style.height = '100%';

        // Create blur background effect.
        root.style.webkitBackfaceVisibility = 'hidden';
        root.style.webkitPerspective = '1000';
        root.style.webkitTransform = 'translate3d(0,0,0)';
        root.style.webkitTransform = 'translateZ(0)';
        root.style.backfaceVisibility = 'hidden';
        root.style.perspective = '1000';
        root.style.transform = 'translate3d(0,0,0)';
        root.style.transform = 'translateZ(0)';
        root.style.backdropFilter = 'blur(10px)';

        // Create intuitive image instruction for the user.
        const image = document.createElement('img');
        if (isPortraitLocked) {
            image.src = 'TemplateData/portrait-only.png';
        }
        else if (isLandscapeLocked) {
            image.src = 'TemplateData/landscape-only.png';
        }
        image.style.display = 'flex';
        image.style.width = '100px';
        image.style.height = '100px';
        image.style.margin = 'auto';
        root.appendChild(image);

        document.body.appendChild(root);

        function updateRotationLock() {
            let display = 'none';
            if (unityApp.isMobile()) {
                if (isPortraitLocked && isLandscapeLocked) {
                    root.style.display = display;
                    return;
                }
                if (isPortraitLocked) {
                    display = window.innerHeight < window.innerWidth ? 'flex' : 'none';
                }
                else if (isLandscapeLocked) {
                    display = window.innerHeight > window.innerWidth ? 'flex' : 'none';
                }
            }
            root.style.display = display;
        }

        // Subscribe to window and document events.
        window.addEventListener("load", updateRotationLock);
        window.addEventListener("resize", updateRotationLock);
        document.addEventListener("readystatechange", updateRotationLock);
        document.addEventListener("DOMContentLoaded", updateRotationLock);

        // Update rotation lock on start.
        updateRotationLock();
    },

    tryLockAspectRatio() {
        const mobilePortraitAspectRatio = "{{{ MOBILE_PORTRAIT_ASPECT_RATIO }}}";
        const mobileLandscapeAspectRatio = "{{{ MOBILE_LANDSCAPE_ASPECT_RATIO }}}";
        const desktopAspectRatio = "{{{ DESKTOP_ASPECT_RATIO }}}";

        const isMobilePortraitLocked = !this.isEmpty(mobilePortraitAspectRatio);
        const isMobileLandscapeLocked = !this.isEmpty(mobileLandscapeAspectRatio);
        const isDesktopLocked = !this.isEmpty(desktopAspectRatio);

        console.log('tryLockAspectRatio', {
            mobilePortraitAspectRatio: mobilePortraitAspectRatio,
            mobileLandscapeAspectRatio: mobileLandscapeAspectRatio,
            desktopAspectRatio: desktopAspectRatio,
            isMobilePortraitLocked: isMobilePortraitLocked,
            isMobileLandscapeLocked: isMobileLandscapeLocked,
            isDesktopLocked: isDesktopLocked
        });

        const container = document.querySelector("#unity-container");
        const canvas = document.querySelector("#unity-canvas");

        function centerCanvas() {
            canvas.style.margin = "auto";
            canvas.style.top = "0";
            canvas.style.left = "0";
            canvas.style.bottom = "0";
            canvas.style.right = "0";
        }

        function resetAspectRatio() {
            canvas.style.width = "100%";
            canvas.style.height = "100%";
            centerCanvas();
        }

        function isPortraitMode() {
            const containerWidth = container.clientWidth;
            const containerHeight = container.clientHeight;
            return containerHeight > containerWidth;
        }

        function recalculateAspectRatio(aspectRatio) {
            const containerWidth = container.clientWidth;
            const containerHeight = container.clientHeight;
            if (containerWidth / containerHeight > aspectRatio) {
                canvas.style.width = Math.floor(containerHeight * aspectRatio) + "px";
                canvas.style.height = "100%";
            }
            else {
                canvas.style.width = "100%";
                canvas.style.height = Math.floor(containerWidth / aspectRatio) + "px";
            }
        }

        function updateAspectRatio() {
            resetAspectRatio();
            if (unityApp.isMobile()) {
                if (isPortraitMode()) {
                    if (isMobilePortraitLocked) {
                        recalculateAspectRatio(unityApp.toNumber(mobilePortraitAspectRatio));
                    }
                }
                else {
                    if (isMobileLandscapeLocked) {
                        recalculateAspectRatio(unityApp.toNumber(mobileLandscapeAspectRatio));
                    }
                }
            }
            else {
                if (isDesktopLocked) {
                    recalculateAspectRatio(unityApp.toNumber(desktopAspectRatio));
                }
            }
            centerCanvas();
        }

        // Subscribe to window and document events.
        window.addEventListener("load", updateAspectRatio);
        window.addEventListener("resize", updateAspectRatio);
        document.addEventListener("readystatechange", updateAspectRatio);
        document.addEventListener("DOMContentLoaded", updateAspectRatio);

        // Update aspect ratio on start.
        updateAspectRatio();
    },

    startLoading: function () {
        const canvas = document.querySelector("#unity-canvas");
        const loadingBar = document.querySelector("#unity-loading-bar");
        const progressBarFull = document.querySelector("#unity-progress-bar-full");

        const buildUrl = "Build";
        const loaderUrl = buildUrl + "/{{{ LOADER_FILENAME }}}";
        const config = {
            arguments: [],
            dataUrl: buildUrl + "/{{{ DATA_FILENAME }}}",
            frameworkUrl: buildUrl + "/{{{ FRAMEWORK_FILENAME }}}",
            #if USE_THREADS
            workerUrl: buildUrl + "/{{{ WORKER_FILENAME }}}",
            #endif
            #if USE_WASM
            codeUrl: buildUrl + "/{{{ CODE_FILENAME }}}",
            #endif
            #if SYMBOLS_FILENAME
            symbolsUrl: buildUrl + "/{{{ SYMBOLS_FILENAME }}}",
            #endif
            streamingAssetsUrl: "StreamingAssets",
            companyName: "{{{ COMPANY_NAME }}}",
            productName: "{{{ PRODUCT_NAME }}}",
            productVersion: "{{{ PRODUCT_VERSION }}}",
            showBanner: (msg, type) => {
                switch (type) {
                    case 'error': {
                        console.error(msg);
                        break;
                    }
                    default: {
                        console.warn(msg);
                        break;
                    }
                }
            },
        };

        // By default Unity keeps WebGL canvas render target size matched with
        // the DOM size of the canvas element (scaled by window.devicePixelRatio)
        // Set this to false if you want to decouple this synchronization from
        // happening inside the engine, and you would instead like to size up
        // the canvas DOM size and WebGL render target sizes yourself.
        const matchWebGLToCanvasSize = "{{{ MATCH_WEBGL_TO_CANVAS_SIZE }}}";
        console.log("matchWebGLToCanvasSize", matchWebGLToCanvasSize);
        if (!this.isEmpty(matchWebGLToCanvasSize)) {
            config.matchWebGLToCanvasSize = this.toBoolean(matchWebGLToCanvasSize);
        }

        // If you would like all file writes inside Unity Application.persistentDataPath
        // directory to automatically persist so that the contents are remembered when
        // the user revisits the site the next time, uncomment the following line:
        const autoSyncPersistentDataPath = "{{{ AUTO_SYNC_PERSISTENT_DATA_PATH }}}";
        console.log("autoSyncPersistentDataPath", autoSyncPersistentDataPath);
        if (!this.isEmpty(autoSyncPersistentDataPath)) {
            config.autoSyncPersistentDataPath = this.toBoolean(autoSyncPersistentDataPath);
        }
        // This autosyncing is currently not the default behavior to avoid regressing
        // existing user projects that might rely on the earlier manual
        // JS_FileSystem_Sync() behavior, but in future Unity version, this will be
        // expected to change.

        // To lower canvas resolution on mobile devices to gain some
        // performance, uncomment the following line:
        const devicePixelRatio = this.toNumber("{{{ DEVICE_PIXEL_RATIO }}}");
        console.log("devicePixelRatio", devicePixelRatio);
        if (this.isNumber(devicePixelRatio)) {
            config.devicePixelRatio = this.toNumber(devicePixelRatio);
        }

        loadingBar.style.display = "block";
        const script = document.createElement("script");
        script.src = loaderUrl;
        script.onload = () => {
            createUnityInstance(canvas, config, (progress) => {
                progressBarFull.style.width = 100 * progress + "%";
            }).then((unityInstance) => {
                loadingBar.style.display = "none";

                #if SHOW_DIAGNOSTICS
                // Position the diagnostics icon in the corner on the canvas.
                const diagnostics_icon = document.getElementById("diagnostics-icon");
                diagnostics_icon.onclick = () => {
                    unityDiagnostics.openDiagnosticsDiv(unityInstance.GetMetricsInfo);
                };
                canvas.after(diagnostics_icon);
                #endif
            }).catch((message) => {
                alert(message);
            });
        };
        document.body.appendChild(script);
    },

    isMobile: function () {
        return /iPhone|iPad|iPod|Android/i.test(navigator.userAgent);
    },

    isEmpty: function (value) {
        return value === undefined || value === null || value === "";
    },

    toBoolean: function (value) {
        return value === true || value === "true" || value === 1 || value === "1" || value === "True";
    },

    isNumber: function (value) {
        return typeof value === 'number' && !isNaN(value);
    },

    toNumber: function (value) {
        function handleMathExpression(str) {
            const mathMatch = str.match(/^(\d*\.?\d+)\s*([+\-*/])\s*(\d*\.?\d+)$/);
            if (!mathMatch) return null;

            const a = parseFloat(mathMatch[1]);
            const operator = mathMatch[2];
            const b = parseFloat(mathMatch[3]);

            const operators = {
                '+': function (a, b) { return a + b; },
                '-': function (a, b) { return a - b; },
                '*': function (a, b) { return a * b; },
                '/': function (a, b) { return b !== 0 ? a / b : NaN; }
            };
            return operators[operator](a, b);
        }

        function convertToNumber(str) {
            const num = parseFloat(str);
            return isNaN(num) ? 0 : num;
        }

        // Handle basic type checks first.
        if (this.isNumber(value)) return value;
        if (this.isEmpty(value)) return 0;

        // Convert to string and handle empty case.
        var str = String(value).trim();
        if (str === '') return 0;

        // Try math expression or simple number conversion.
        return handleMathExpression(str) || convertToNumber(str);
    },

};

// Apply common fixes.
unityApp.applyCommonFixes();

// Lock rotation.
unityApp.tryRotationLock();

// Lock aspect ratio.
unityApp.tryLockAspectRatio();

// Automatically start after script is loaded.
unityApp.startLoading();