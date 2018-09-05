﻿function FindAsset() {
    SetCursor(true);
    $.post("/asset/find",
        {
            assetName: $("#inputAssetName").val()
        },
        function (asset) {
            SetCursor(false);
            if (asset != null) {
                $("#inputAssetName").removeClass("mediaEntityNotFound").addClass("mediaEntityFound");
                SetTipVisible("inputAssetName", false);
            } else {
                $("#inputAssetName").removeClass("mediaEntityFound").addClass("mediaEntityNotFound");
                CreateTipRight("inputAssetName", "Media Asset<br><br>Not Found");
                SetTipVisible("inputAssetName", true);
            }
        }
    );
}
function CreateJob() {
    var transformName = $("#transforms").val();
    var inputAssetName = $("#inputAssetName").val();
    var inputFileUrl = $("#inputFileUrl").val();
    if (transformName == "" || (inputAssetName == "" && inputFileUrl == "")) {
        var tipText = "Required Field";
        if (transformName == "") {
            CreateTipRight("transforms", tipText);
            SetTipVisible("transforms", true);
        } else {
            SetTipVisible("transforms", false);
        }
        if (inputAssetName == "" && inputFileUrl == "") {
            CreateTipRight("inputAssetName", tipText);
            CreateTipRight("inputFileUrl", tipText);
            SetTipVisible("inputAssetName", true);
            SetTipVisible("inputFileUrl", true);
        } else {
            SetTipVisible("inputAssetName", false);
            SetTipVisible("inputFileUrl", false);
        }
        if (inputFileUrl == "" && $("#inputAssetName").hasClass("entityNotFound")) {
            SetTipVisible("inputAssetName", true);
        }
    } else {
        var title = "Confirm Create Job";
        var message = "Are you sure you want to create a new job?";
        var onConfirm = function () {
            SetCursor(true);
            $.post("/job/create",
                {
                    transformName: transformName,
                    jobName: $("#name").val(),
                    jobDescription: $("#description").val(),
                    jobPriority: $("#jobPriority:checked").val(),
                    jobData: _jsonEditor.getText(),
                    inputAssetName: inputAssetName,
                    inputFileUrl: inputFileUrl,
                    streamingPolicyName: $("#streamingPolicies").val()
                },
                function (entity) {
                    SetCursor(false);
                    window.location = window.location.href;
                }
            );
            $(this).dialog("close");
        };
        ConfirmMessage(title, message, onConfirm);
    }
}
function SetJobRefresh(gridId, columns) {
    var reloadGrid = function () {
        ReloadGrid(gridId, "/job/list", columns);
    };
    setInterval(reloadGrid, _refreshInterval);
}