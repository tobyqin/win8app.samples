﻿//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//// PARTICULAR PURPOSE.
////
//// Copyright (c) Microsoft Corporation. All rights reserved

(function () {
    "use strict";
    var page = WinJS.UI.Pages.define("/html/allowFormSubmission.html", {
        ready: function (element, options) {
            document.getElementById("leftButton").addEventListener("click", SdkSample.showLeftHost, false);
            document.getElementById("rightButton").addEventListener("click", SdkSample.showRightHost, false);
        }
    });

})();
