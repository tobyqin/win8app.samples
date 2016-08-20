﻿//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//// PARTICULAR PURPOSE.
////
//// Copyright (c) Microsoft Corporation. All rights reserved

(function () {
    "use strict";
    var page = WinJS.UI.Pages.define("/html/crossfade.html", {
        ready: function (element, options) {
            runAnimation.addEventListener("click", runCrossfadeAnimation, false);
            element2.style.opacity = "0";
        }
    });

    function runCrossfadeAnimation() {
        var incoming;
        var outgoing;

        // Set incoming and outgoing elements
        if (element1.style.opacity === "0") {
            incoming = element1;
            outgoing = element2;
        } else {
            incoming = element2;
            outgoing = element1;
        }

        // Run crossfade animation
        WinJS.UI.Animation.crossFade(incoming, outgoing);
    }
})();
