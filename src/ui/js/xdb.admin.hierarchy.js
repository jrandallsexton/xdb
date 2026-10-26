var _imgSrc = "../images";

function initForm() {
    getCurrentUser();
    displayCurrentUser(_imgSrc);
    $("#header").html(buildHeaderContent(_imgSrc));
    $("#navigation").html(buildMenuHtml(_imgSrc));
    refreshRecent(_imgSrc);
    initMenu();
    authorizePageViewAdmin();
    $.ajaxSetup({
        cache: false
    });
    initAssetTypeTree()
}

function initAssetTypeTree() {
    var urlBase = URL_CLIENT_UI + "/admin/assetTypes.html?itemId=",
        data = assetTypes_Get();
    $("#treAllItems").jstree({
        json_data: {
            data: [data]
        },
        plugins: ["themes", "json_data", "ui"]
    }).bind("select_node.jstree", function(e, data) {
        var node = data.rslt.obj,
            nodeId = node.data("id");
        window.open(urlBase + nodeId, "_blank")
    })
}
