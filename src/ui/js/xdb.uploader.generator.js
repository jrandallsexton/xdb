var _imgSrc = "../images";

function initForm() {
    getCurrentUser();
    displayCurrentUser(_imgSrc);
    $("#header").html(buildHeaderContent(_imgSrc));
    $("#navigation").html(buildMenuHtml(_imgSrc));
    refreshRecent(_imgSrc);
    initMenu()
}

function onGenerate() {
    $("#btnGenerate").hide();
    var numRows = $("#numrows").val();
    window.location = URL_REST_SVC + "/GenerateAssetCodeTemplateTest?numRows=" + numRows;
    $("#btnGenerate").show()
}
