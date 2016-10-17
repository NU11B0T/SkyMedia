﻿var _fileUploader, _fileUploaderStartTime, _manifestId, _storageContainer;
function SetUploadService(transferService) {
    switch (transferService.value) {
        case "signiantFlight":
            window.location.href = "/upload/signiant";
            break;
        case "asperaFasp":
            window.location.href = "/upload/aspera";
            break;
        default:
            window.location.href = "/upload";
            break;
    }
}
function SetUploadManifest(storageContainer) {
    $.post("/upload/manifest",
        {
            manifestId: _manifestId,
            assetName: $("#inputAssetName").val(),
            storageAccount: $("#storageAccount").val(),
            storageEncryption: $("#storageEncryption").prop("checked"),
            multipleFileAsset: $("#multipleFileAsset").prop("checked"),
            uploadFileWatcher: $("#uploadFileWatcher").prop("checked"),
            fileNames: GetFileNames(_fileUploader.files)
        },
        function (result) {
            if (result == null) {
                _manifestId = null;
                _storageContainer = storageContainer;
            } else {
                _manifestId = result.id;
                _storageContainer = result.blobStorageUriForUpload;
            }
        }
    );
}
function GetElapsedTime() {
    var elapsedTime = new Date() - _fileUploaderStartTime;
    return MapElapsedTime(elapsedTime);
}
function MapElapsedTime(elapsedTime) {
    var elapsedSeconds = Math.floor(elapsedTime / 1000);
    if (elapsedSeconds == 1)
        return elapsedSeconds + " Second";
    else if (elapsedSeconds < 59) {
        return elapsedSeconds + " Seconds";
    }
    var elapsedMinutes = elapsedSeconds / 60;
    return elapsedMinutes.toFixed(2) + " Minutes";
}
function GetUploaderFile(fileName) {
    var file = null;
    var files = _fileUploader.files;
    for (var i = 0; i < files.length; i++) {
        if (files[i].name == fileName) {
            file = files[i];
        }
    }
    return file;
}
function AddSelectedFiles(files) {
    for (var i = 0; i < files.length; i++) {
        var fileName = files[i].name;
        var file = GetUploaderFile(fileName);
        if (file == null) {
            var fileInfo = {
                name: fileName,
                size: files[i].size
            };
            file = new plupload.File(fileInfo);
            _fileUploader.addFile(file);
        }
    }
}
function AddSigniantFiles(e, files) {
    var filePaths = new Array();
    for (var i = 0; i < files.length; i++) {
        filePaths.push(files[i].path);
    }
    _signiantUploader.setFilesToUpload(filePaths);
    AddSelectedFiles(files);
}
function AddAsperaFiles(e) {
    var files = e.dataTransfer.files;
    AddSelectedFiles(files);
}
function SigniantEnabled() {
    return $("#uploadService[value='signiantFlight']").prop("checked");
}
function AsperaEnabled() {
    return $("#uploadService[value='asperaFasp']").prop("checked");
}
function WatcherEnabled() {
    return $("#uploadFileWatcher").prop("checked");
}
function CreateUploader(storageContainer, blockChunkSize, maxFileSize, maxRetryCount, appUrl) {
    _storageContainer = storageContainer;
    var eventHandlers = {
        PostInit: function (uploader) {
            _fileUploader = uploader;
        },
        Browse: function (uploader) {
            if (SigniantEnabled() || AsperaEnabled()) {
                $("input[type=file][id^='html5']").prop("disabled", true);
            }
            if (SigniantEnabled()) {
                _signiantUploader.chooseUploadFiles(AddSigniantFiles);
            } else if (AsperaEnabled()) {
                _asperaUploader.showSelectFileDialog(
                    { success: AddAsperaFiles }
                );
            }
        },
        BeforeUpload: function (uploader, file) {
            uploader.settings.multipart_params = {
                storageAccount: $("#storageAccount").val(),
                storageContainer: _storageContainer
            };
        },
        StateChanged: function (uploader) {
            if (uploader.state == plupload.STARTED) {
                if (SigniantEnabled() || AsperaEnabled()) {
                    uploader.stop();
                    StartUpload(appUrl);
                } else {
                    _fileUploaderStartTime = new Date();
                }
            }
        },
        UploadComplete: function (uploader, files) {
            if (uploader.total.failed == 0) {
                if (!SigniantEnabled() && !AsperaEnabled()) {
                    var elapsedTime = GetElapsedTime();
                    $("#transferMessage").text(elapsedTime);
                }
                if (!WatcherEnabled()) {
                    StartWorkflow(files);
                }
            }
        },
        FilesAdded: function (uploader, files) {
            if (WatcherEnabled()) {
                SetUploadManifest(storageContainer);
            }
        },
        FilesRemoved: function (uploader, files) {
            if (WatcherEnabled()) {
                SetUploadManifest(storageContainer);
            }
            if (!SigniantEnabled() && !AsperaEnabled()) {
                $("#transferMessage").text("Elapsed Time");
            }
        },
        Error: function (uploader, error) {
            var title = "Error Message";
            var message = error.message;
            if (error.response != null && error.response != "") {
                message = error.response;
            }
            DisplayMessage(title, message);
        }
    };
    $("#fileUploader").plupload({
        url: "/upload/file",
        runtimes: "html5",
        chunk_size: blockChunkSize,
        max_file_size: maxFileSize,
        max_retries: maxRetryCount,
        multipart: true,
        dragdrop: false,
        sortable: true,
        rename: true,
        filters: {
            prevent_duplicates: true
        },
        init: eventHandlers
    });
    if (SigniantEnabled() || AsperaEnabled()) {
        $(".plupload_file_status").hide();
        $(".plupload_file_size").hide();
    }
}
