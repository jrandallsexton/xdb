var start = null,
    FILTER_IDS = [],
    _maxRss = 10,
    LOAD_MSG = '<div style="width:100%;text-align:center;" >Retrieving data.  Please wait.</div><br/><div style="text-align:center"><img src="images/ajax-loader.gif" alt="spinner" style="text-align:left;" /></div>',
    $innerContent = $("#innerContent"),
    $dialog = $("#messageDialog"),
    $rss = $("#rss"),
    _imgSrc = "images",
    _dialogPos = ["top", 225],
    _dialogW = 650,
    _summary = null,
    _jsFiltering = "js/xdb.filtering.js";

function initPageNotFound() {
    getCurrentUser(false);
    $(document.getElementById("header")).html(buildHeaderContent(_imgSrc));
    if (USER_CURRENT === null || USER_CURRENT == NULL_GUID) {
        $("#loadingImg").css("display", "none");
        $("#noAccessDiv").css("display", "inline");
        $("#searchform").hide();
        return
    }
    getRss();
    displayCurrentUser(_imgSrc);
    $(document.getElementById("navigation")).html(buildMenuHtml(_imgSrc));
    refreshRecent(_imgSrc);
    initMenu();
    $(".hdrLeftLogo").rotate(180);
    $(".hdrLogo").rotate(180);
    $(".hdrSubLogo").rotate(180);
    $("#spanGreeting").rotate(180);
    $("#navigation").rotate(180);
    $("#footer").rotate(180)
}

function initDefaultHtml() {
    _maxRss = parseInt(Math.ceil($(window).height() / 100) * 1.2);
    $("#imgSearch").hide();
    $("#txtSearchField").val("Search...");
    getCurrentUser(false);
    $(document.getElementById("header")).html(buildHeaderContent(_imgSrc));
    if (USER_CURRENT === null || USER_CURRENT == NULL_GUID || USER_CURRENT.Views == undefined || USER_CURRENT.Views.length == 0) {
        $("#loadingImg").css("display", "none");
        var msg = null;
        if (USER_CURRENT !== null) msg = "<div><b>" + USER_CURRENT.FirstName + ", you do not currently have access to XDB.<br/>Please contact your administrator.</b></div>";
        else msg = "<div><b>You do not currently have access to XDB.<br/>Please contact your administrator.</b></div>";
        $("#noAccessDiv").html(msg).css("display", "inline");
        $("#searchform").hide();
        return
    }
    getRss();
    displayCurrentUser(_imgSrc);
    $(document.getElementById("navigation")).html(buildMenuHtml(_imgSrc));
    refreshRecent(_imgSrc);
    initMenu();
    $("#loadingImg").css("display", "none");
    $("#defaultView").css("display", "inline");
    listProcessesAndShortcuts(USER_CURRENT.Processes, USER_CURRENT.shortcuts);
    $innerContent.height($("#items").height());
    var viewId = getParameterByName("viewId"),
        assetId = getParameterByName("assetId");
    if (viewId !== null && viewId !== "")
        if (assetId !== null && assetId !== "") renderView(viewId, assetId);
        else renderView(viewId, null)
}

function getRss() {
    $innerContent.block({
        message: '<p>... <i>Loading Recent Activity</i> ...</p><img src="images/ajax-loader.gif" alt="spinner" />',
        css: {
            border: "none",
            padding: "15px",
            backgroundColor: "#000",
            "-webkit-border-radius": "10px",
            "-moz-border-radius": "10px",
            opacity: .5,
            color: "#fff"
        }
    });
    if (!!window.Worker) {
        var worker = new Worker("js/" + WORKER);
        worker.onmessage = function(event) {
            var feed = event.data;
            displayRss(feed)
        };
        var arg = URL_REST_SVC + "/systemActivityHome?userId=" + USER_CURRENT.Id + "&maxItems=" + _maxRss;
        worker.postMessage(arg)
    } else {
        var rssurl = URL_REST_SVC + "/systemActivityHome?userId=" + USER_CURRENT.Id + "&maxItems=" + _maxRss;
        $.get(rssurl, function(data) {
            displayRss(data)
        })
    }
}

function displayRss(data) {
    var $xml = $(data),
        html = [],
        itemsFound = false;
    if ($xml == null || $xml.length == 0) html.push('<p style="padding-left:5px; padding-top:5px;">No RSS activity found within your roles.</p>');
    else {
        var temp = '<span id="rssIconSpan"><a href="' + URL_REST_SVC + "/systemActivityHome?userId=" + USER_CURRENT.Id + '&maxItems=100" target="_blank">';
        temp += '<img id="rssIcon" height="32" width="32" class="x-rss-32" style="float:right;margin-top:9px;margin-right:15px;border:0px;" title="Get RSS link for your system activity (shown below)" src="images/trans32.png" />';
        temp += "</a></span>";
        $("#navigation").append(temp);
        html.push('<div class="rssFeed"></div>');
        html.push('<div class="rssFeed rssBody"><ul>');
        var index = 0;
        $xml.find("item").each(function() {
            itemsFound = true;
            var $this = $(this),
                item = {
                    title: $this.find("title").text(),
                    link: $this.find("link").text(),
                    description: $this.find("description").text(),
                    pubDate: $this.find("submittalDate").text(),
                    author: $this.find("author").text(),
                    guid: $this.find("guid").text(),
                    atId: $this.find("assetTypeId").text()
                };
            if (index % 2 == 0) html.push('<li class="rssRow even">');
            else html.push('<li class="rssRow odd">');
            var cName = item.atId.replace(/-/gi, "");
            html.push("<div>");
            html.push('<img height="24" width="24" style="margin-left:5px;margin-right:5px;vertical-align:middle;" class="at-' + cName + '-24" src="images/trans.png" />');
            html.push('<a id="' + item.guid + '" href="">' + item.title + "</a>");
            html.push("</div>");
            html.push('<label class="rssPubDate">' + item.pubDate + " (" + item.description + ")</label>");
            html.push("</li>");
            index++
        });
        html.push("</ul></div>")
    }
    if (!itemsFound) {
        html = [];
        html.push('<p style="padding-left:5px; padding-top:5px;">No RSS activity found within your roles.</p>')
    }
    $rss.html(html.join(""));
    $innerContent.height($rss.height()).unblock();
    $(".rssRow a").bind("click", function() {
        showSubmittalSummary(this.id);
        return false
    });
}

function showSubmittalSummary(id) {
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html(LOAD_MSG);
    $dialog.dialog({
        width: 625,
        modal: false,
        draggable: true,
        autoOpen: false,
        title: '<span class="ui-icon ui-icon-clipboard"></span> Submittal Summary',
        position: _dialogPos,
        buttons: {
            "Go to Asset": function() {
                onGoToAssetClicked()
            },
            Close: function() {
                $(this).dialog("close")
            }
        },
        open: function(event, ui) {
            $(this).siblings(".ui-dialog-buttonpane").find("button:eq(1)").focus();
            $(this).siblings(".ui-dialog-buttonpane").find("button:eq(1)").bind("mouseleave", function() {
                $(this).blur()
            });
            setTimeout(function() {
                displaySubmittalData(id)
            }, 50)
        }
    });
    $dialog.dialog("open")
}

function displaySubmittalData(submittalId) {
    _summary = submittal_GetSummary(submittalId);
    $dialog.dialog({
        title: '<span class="ui-icon ui-icon-clipboard"></span> Submittal Summary - ' + _summary.assetName
    });
    var html = [];
    html.push('<table id="subData" style="width:100%">');
    html.push('<tr><th style="width:33%">Property</th><th style="width:33%">Previous Value</th><th style="width:33%">New Value</th></tr>');
    $.each(_summary.values, function(index, item) {
        html.push("<tr><td>" + item.Key + "</td><td>" + item.Value.key + "</td><td>" + item.Value.value + "</td></tr>")
    });
    html.push("</table>");
    $dialog.html(html.join(""))
}

function onGoToAssetClicked() {
    var url = createViewUrlPlain(_summary.viewId, _summary.assetId, _summary.assetTypeId, null, false);
    window.open(url, "_blank")
}

function listProcessesAndShortcuts(processes, shortcuts) {
    var html = [];
    if (processes !== null && processes.length > 0) {
        var ulItems = [];
        for (var i = 0, len = processes.length; i < len; i++) {
            var proc = processes[i],
                procKey = proc.Key,
                procClass = procKey.replace(/-/gi, ""),
                procName = proc.Value;
            ulItems.push('<div class="process" onClick="processSelected(this, \'' + procKey + "');\">");
            ulItems.push('<img width="32" height="32" class="pr' + procClass + '" src="images/trans32.png" /><label class="procLabel">' + procName + "</label></div>")
        }
        html.push(ulItems.join(""))
    }
    if (shortcuts !== null && shortcuts.length > 0) {
        var items = [];
        for (var i = 0, len = shortcuts.length; i < len; i++) {
            var shortcut = shortcuts[i],
                procKey = shortcut.Key,
                name = shortcut.Value;
            items.push('<div class="process" onClick="shortcutSelected(\'' + procKey + "');\">");
            items.push('<img width="32" height="32" class="prShortcut" src="images/trans32.png" /><label class="procLabel">' + name + "</label></div>")
        }
        html.push(items.join(""))
    }
    $("#items").html(html.join(""))
}

function shortcutSelected(shortcutId) {
    var url = createUrlForBookmarkedReport(null, shortcutId, null, true);
    window.open(url, "_blank")
}

function alertCheck() {
    var count = alertCount(USER_CURRENT.Id);
    if (count > 0) {
        var msg = [];
        if (count == 1) msg.push("You have one new alert.<br/><br/>");
        else msg.push("You have " + count + " new alerts.<br/><br/>");
        var element = $("#defaultView"),
            gTop = $(window).height() - element.height() - element.offset().top,
            gRight = $(window).width() / 10;
        $.growl.settings.dockCss.right = gRight + "px";
        $.growl.settings.dockCss.top = gTop + "px";
        $.growl("Alerts", msg.join(""), "images/growl/bgrowl.png")
    }
}

function onSearchDefault() {
    $rss.hide();
    $("#innerContent").css("overflow-y", "auto");
    performSearch($("#txtSearchField").val(), USER_CURRENT.Id)
}

function onSearchChangedDefault(ev) {
    var e = ev || event;
    if (e.keyCode == 13) onSearchDefault()
}

function renderView(viewId, assetId, assetTypeId, newAssetId) {
    if (viewId == null) return;
    window.location = createViewUrlPlain(viewId, assetId, assetTypeId, newAssetId, true)
}

function processSelected(sender, processId) {
    $(sender).effect("highlight", {}, 50, getProcess(processId))
}

function getProcess(id) {
    process_Get(id, renderProcess)
}

function renderProcess(data) {
    var process = data;
    if (process == null) return;
    var procStep = process.steps[0];
    if (procStep == null) return;
    if (procStep.isEdit) return renderProcessEdit(process, procStep);
    else {
        var hasFilters = procStep.filters !== null && procStep.filters.length > 0;
        if (hasFilters) return renderProcessFiltering(process, procStep);
        else return renderView(procStep.viewId, 0, process.assetTypeId, null)
    }
}

function renderProcessEdit(process, procStep) {
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    var assetRequestType = procStep.isInstance ? ART_INSTANCE : ART_DEFINITION;
    $dialog.html(LOAD_MSG);
    $dialog.dialog({
        width: _dialogW,
        height: "auto",
        modal: true,
        autoOpen: false,
        resizable: true,
        draggable: true,
        position: _dialogPos,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove();
            setTimeout(function() {
                createProcessEditUI(process.assetTypeId, assetRequestType, procStep.assetNameLabel)
            }, 50)
        },
        title: process.name,
        buttons: {
            Cancel: function() {
                $(this).dialog("close")
            },
            Select: function() {
                var selectedAssetId = $(document.getElementById("lstAssets")).val();
                if (selectedAssetId !== null && selectedAssetId != "") {
                    $(this).dialog("close");
                    CURRENT_MODE = MODE_EDIT;
                    var url = createViewUrlPlain(procStep.viewId, selectedAssetId, null, null, true);
                    window.location = url
                }
            }
        }
    });
    $dialog.dialog("open")
}

function createProcessEditUI(assetTypeId, assetRequestType, assetNameLabel) {
    $dialog.html(createFilterDialog(assetTypeId, assetRequestType, assetNameLabel))
}

function createProcessEditUiFromFiltering(process, assetRequestType, filters) {
    $dialog.html(createFilterDialogFromFilters(process.assetTypeId, ART_DEFINITION, filters))
}

function renderProcessFiltering(process, procStep) {
    var art = procStep.isInstance ? ART_INSTANCE : ART_DEFINITION;
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html(LOAD_MSG);
    $dialog.dialog({
        width: _dialogW,
        height: "auto",
        modal: true,
        autoOpen: false,
        resizable: true,
        draggable: true,
        position: _dialogPos,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove();
            $(".ui-dialog-buttonpane button:contains('Next')").button("disable");
            setTimeout(function() {
                createProcessFilteringUI(procStep.filters)
            }, 50)
        },
        title: process.name,
        buttons: [{
            text: "Cancel",
            click: function() {
                $(this).dialog("close")
            }
        }, {
            id: "button-next",
            text: "Next",
            click: function() {
                var filters = [];
                $.each(procStep.filters, function(index, value) {
                    var f = {};
                    f.index = value.order;
                    f.propId = value.propertyId;
                    f.propName = "[Set Me]";
                    f.val = $("#ddl" + value.propertyId + " option:selected").val();
                    f.valVal = $("#ddl" + value.propertyId + " option:selected").text();
                    filters.push(f)
                });
                $(this).dialog("close");
                onProcessFilteringComplete(process, procStep, filters)
            }
        }]
    });
    $dialog.dialog("open")
}

function onProcessFilteringComplete(process, procStep, filters) {
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    var assetRequestType = procStep.isInstance ? ART_INSTANCE : ART_DEFINITION;
    $dialog.html(LOAD_MSG);
    $dialog.dialog({
        width: _dialogW,
        height: "auto",
        modal: true,
        autoOpen: false,
        resizable: true,
        draggable: true,
        position: _dialogPos,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove();
            setTimeout(function() {
                createProcessEditUiFromFiltering(process, assetRequestType, filters)
            }, 50)
        },
        title: process.name,
        buttons: {
            Cancel: function() {
                $(this).dialog("close")
            },
            Select: function() {
                var selectedAssetId = $(document.getElementById("lstAssets")).val();
                if (selectedAssetId !== null && selectedAssetId != "") renderView(procStep.viewId, 0, process.assetTypeId, selectedAssetId)
            }
        }
    });
    $dialog.dialog("open")
}

function createProcessFilteringUI(filters) {
    FILTER_IDS = [];
    var html = [];
    $.each(filters, function(index, value) {
        FILTER_IDS[index] = value.propertyId;
        var controlId = "ddl" + value.propertyId;
        if (index == 0) html.push('<select id="' + controlId + '" class="procFilter" onChange="onFilterSelected(' + index + ')"><option value="-1" selected="selected">[Select]</option>');
        else html.push('<select id="' + controlId + '" class="procSubFilter" onChange="onFilterSelected(' + index + ')" disabled="disabled"><option value="-1" selected="selected">[Select]</option>');
        var filterValues = processStep_GetFilterValues(value.propertyId);
        $.each(filterValues, function(subIndex, subValue) {
            html.push('<option value="' + subValue.Key + '">' + subValue.Value + "</option>")
        });
        html.push("</select>")
    });
    $dialog.html(html.join(""))
}

function createAssetTypeSelectorHtml(assetTypeDictionary) {
    try {
        var html = [];
        html.push("<table><tr><td>What type of asset?</td>");
        html.push('<td><select id="assetTypeId"><option value="-1" selected="selected">[Select]</option>');
        $.each(assetTypeDictionary, function(index, value) {
            html.push('<option value="' + value.Key.toLowerCase() + '">' + value.Value + "</option>")
        });
        html.push("</select></td></tr></table>");
        return html.join("")
    } finally {}
}

function onFilterSelected(index) {
    if (FILTER_IDS[index + 1] !== undefined) {
        var selectedPropertyId = FILTER_IDS[index],
            selectedValue = $("#ddl" + selectedPropertyId).val();
        if (selectedValue != "-1") {
            var nextPropertyId = FILTER_IDS[index + 1],
                nextValues = processStep_GetNextFilterValues(selectedPropertyId, selectedValue, nextPropertyId);
            selectOptionsRemove("ddl" + nextPropertyId);
            selectOptionAppend("ddl" + nextPropertyId, -1, "[Select]", true);
            $.each(nextValues, function(index, value) {
                selectOptionAppend("ddl" + nextPropertyId, value.Key, value.Value, false)
            });
            var propertyId = FILTER_IDS[index + 1],
                controlId = "ddl" + propertyId;
            $("#" + controlId).removeAttr("disabled").fadeIn("slow")
        } else $("#button-next").removeAttr("disabled").removeClass("ui-state-disabled").addClass("ui-state-enabled")
    } else $("#button-next").removeAttr("disabled").removeClass("ui-state-disabled").addClass("ui-state-enabled")
}
