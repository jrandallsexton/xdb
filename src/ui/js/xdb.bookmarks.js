var _imgSrc = "images",
    _bookmarks = null;

function initForm() {
    $.ajaxSetup({
        cache: false
    });
    getCurrentUser();
    displayCurrentUser(_imgSrc);
    $("#header").html(buildHeaderContent(_imgSrc));
    $("#navigation").html(buildMenuHtml(_imgSrc));
    refreshRecent(_imgSrc);
    initMenu();
    buildBookmarks()
}

function buildBookmarks() {
    var $bookmarks = $("#bookMarks");
    if ($bookmarks.is(":ui-accordion")) $bookmarks.accordion("destroy");
    $bookmarks.html(showBookmarks());
    $bookmarks.accordion({
        autoHeight: false,
        collapsible: true,
        active: false,
        icons: false
    })
}

function showBookmarks() {
    appCache(LOCAL_STORAGE_BOOKMARKS, null, false);
    _bookmarks = bookMarks_Get(USER_CURRENT.Id);
    if (_bookmarks == null || _bookmarks.length == 0) return "No bookmarks found.";
    var html = [],
        sections = [];
    $.each(_bookmarks, function(index, bookMark) {
        var inArray = $.inArray(bookMark.AssetType, sections);
        if (inArray == -1) {
            if (sections.length !== 0) html.push("</ul></div>");
            var imgClass = null;
            if (bookMark.AssetType == "Reports") imgClass = "mnuReportsReports";
            else imgClass = "at-" + bookMark.AssetTypeId.replace(/-/gi, "") + "-24";
            html.push('<h3><img src="images/trans.png" height="24" width="24" style="vertical-align:middle; margin-right: 8px;" class="' + imgClass + '"/>');
            html.push('<a href="#">' + bookMark.AssetType + "</a></h3>");
            html.push("<div><ul>");
            if (bookMark.AssetType == "Reports") {
                html.push('<li id="' + bookMark.Id + '"><span style="float:left">' + createUrlForBookmarkedReport(index, bookMark.ReportId, bookMark.ReportName) + "</span>");
                if (!bookMark.ShowOnHome) {
                    html.push('<span style="float:left" class="ui-icon ui-icon-trash" title="Delete bookmark" onclick="removeBookmark(\'' + bookMark.Id + "');\"></span>");
                    html.push('<span id="sp' + bookMark.Id + '" style="float:left" class="ui-icon ui-icon-home" title="Add shortcut to home page" onclick="toggleHome(\'' + bookMark.Id + "');\"></span></li>")
                } else {
                    html.push('<span style="float:left" class="ui-icon ui-icon-trash" title="Delete bookmark" onclick="removeBookmark(\'' + bookMark.Id + "');\"></span>");
                    html.push('<span id="sp' + bookMark.Id + '" style="float:left" class="ui-icon ui-icon-closethick" title="Remove shortcut from home page" onclick="toggleHome(\'' + bookMark.Id + "');\"></span></li>")
                }
            } else {
                html.push('<li id="' + bookMark.Id + '"><span style="float:left">' + createUrlForBookmark(index, bookMark.ViewId, bookMark.AssetId, bookMark.Asset) + "</span>");
                html.push('<span style="float:left" class="ui-icon ui-icon-trash" title="Delete bookmark" onclick="removeBookmark(\'' + bookMark.Id + "');\"></span></li>")
            }
            sections.push(bookMark.AssetType)
        } else if (bookMark.AssetType == "Reports") {
            html.push('<li id="' + bookMark.Id + '"><span style="float:left">' + createUrlForBookmarkedReport(index, bookMark.ReportId, bookMark.ReportName) + "</span>");
            if (!bookMark.ShowOnHome) {
                html.push('<span style="float:left" class="ui-icon ui-icon-trash" title="Delete bookmark" onclick="removeBookmark(\'' + bookMark.Id + "');\"></span>");
                html.push('<span id="sp' + bookMark.Id + '" style="float:left" class="ui-icon ui-icon-home" title="Add shortcut to home page" onclick="toggleHome(\'' + bookMark.Id + "');\"></span></li>")
            } else {
                html.push('<span style="float:left" class="ui-icon ui-icon-trash" title="Delete bookmark" onclick="removeBookmark(\'' + bookMark.Id + "');\"></span>");
                html.push('<span id="sp' + bookMark.Id + '" style="float:left" class="ui-icon ui-icon-closethick" title="Remove shortcut from home page" onclick="toggleHome(\'' + bookMark.Id + "');\"></span></li>")
            }
        } else {
            html.push('<li id="' + bookMark.Id + '"><span style="float:left">' + createUrlForBookmark(index, bookMark.ViewId, bookMark.AssetId, bookMark.Asset) + "</span>");
            html.push('<span style="float:left" class="ui-icon ui-icon-trash" title="Delete bookmark" onclick="removeBookmark(\'' + bookMark.Id + "');\"></span></li>")
        }
    });
    return html.join("")
}

function removeBookmark(id) {
    if (bookmark_Delete(id)) {
        $("#" + id).remove();
        expireUser()
    }
}

function toggleHome(id) {
    if (bookmark_ToggleHome(id))
        for (var i = 0, len = _bookmarks.length; i < len; i++) {
            var bm = _bookmarks[i];
            if (bm.Id.toLowerCase() == id.toLowerCase()) {
                var newValue = bm.ShowOnHome ? false : true;
                bm.ShowOnHome = newValue;
                expireUser();
                if (newValue) {
                    $("#sp" + id).removeClass("ui-icon-home").addClass("ui-icon-closethick");
                    $("#" + id).append('<div id="tmpMsg">Added to your home page.</div>');
                    $("#tmpMsg").fadeOut(1500, function() {
                        $(this).remove()
                    })
                } else {
                    $("#sp" + id).removeClass("ui-icon-closethick").addClass("ui-icon-home");
                    $("#" + id).append('<div id="tmpMsg">Removed from your home page.</div>');
                    $("#tmpMsg").fadeOut(1500, function() {
                        $(this).remove()
                    })
                }
                break
            }
        }
}
