var fileName = null,
    _imgSrc = "../images/";

function initForm() {
    getCurrentUser();
    displayCurrentUser(_imgSrc);
    $("#header").html(buildHeaderContent(_imgSrc));
    $("#navigation").html(buildMenuHtml(_imgSrc));
    refreshRecent(_imgSrc);
    initMenu();
    initUploader();
    loadImages();
    $("#wait").hide();
    $("#thumbs a").lightBox()
}

function loadImages() {
    var html = [];
    $.each(images_GetSystem(), function(index, item) {
        html.push('<a class="lightbox" href="' + item.RequestUrl + '&amp;createThumbnail=false" title="' + item.Name + " - " + item.RequestUrl + '"><img class="thumb" src="' + item.RequestUrl + '&amp;createThumbnail=true&amp;maxSize=50" /></a>')
    });
    $("#thumbs").html(html.join(""))
}

function initUploader() {
    "use strict";
    appCache(LOCAL_STORAGE_UPLOAD_ID, NULL_GUID, false);
    getCurrentUser();
    displayCurrentUser(_imgSrc);
    $("#fileupload").fileupload({
        url: URL_REST_SVC + "/image_upload?imageId=" + getUploadId() + "&userId=" + USER_ACTUAL.Id + "&fileName=" + fileName,
        singleFileUploads: true,
        autoUpload: false,
        acceptFileTypes: /\.(gif|jpg|png)$/i
    });
    $("#fileupload").bind("fileuploaddone", function(e, data) {
        var result = data.textStatus;
        if (result == "success") {
            var error = data.result[0].error;
            if (error === null || error == undefined || error == "") {
                $("#wait").show();
                loadImages();
                $("#wait").hide()
            } else alert(error)
        } else alert("error")
    });
    $("#fileupload").bind("fileuploadadd", function(e, data) {
        fileName = data.files[0].name;
        $("#fileupload").fileupload({
            url: URL_REST_SVC + "/image_upload?imageId=" + getUploadId() + "&userId=" + USER_ACTUAL.Id + "&fileName=" + fileName
        })
    });
    $.getJSON($("#fileupload form").prop("action"), function(files) {
        var fu = $("#fileupload").data("fileupload");
        fu._adjustMaxNumberOfFiles(-files.length);
        fu._renderDownload(files).appendTo($("#fileupload .files")).fadeIn(function() {
            $(this).show()
        })
    });
    $("#fileupload .files a:not([target^=_blank])").live("click", function(e) {
        e.preventDefault();
        $('<iframe style="display:none;"></iframe>').prop("src", this.href).appendTo("body")
    })
}

function getUploadId() {
    var uploadId = appDecache(LOCAL_STORAGE_UPLOAD_ID, false);
    if (uploadId === null || uploadId == undefined || uploadId == NULL_GUID) {
        uploadId = generateGuid(null);
        appCache(LOCAL_STORAGE_UPLOAD_ID, uploadId, false)
    }
    return uploadId
}
