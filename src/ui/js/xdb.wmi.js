var selectedObjects = [],
    queryCount = 0,
    queryTotal = 0,
    accordHtml = [],
    _imgSrc = "../images",
    delims = [];

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
    showQueryableObjects();
    createDelims()
}

function showQueryableObjects() {
    var html = [];
    html.push("<h3>Queryable Objects</h3><div><p>");
    $.each(wmiQueryableObjects(), function(index, item) {
        html.push('<input type="checkbox" name="qObject" value="' + item + '" id="' + item + '" onchange="onObjectSelectChanged(this)"/>' + item + "<br/>")
    });
    html.push('<br/><input type="button" id="btnAll" class="ui-corner-all" value="All" onClick="onCheckAll()"/>');
    html.push('<input type="button" id="btnNone" class="ui-corner-all" value="None" onClick="onCheckNone()"/>');
    html.push('<input type="button" id="queryButton" class="ui-corner-all" value="Query" onClick="onQuery()"/>');
    html.push("</p></div>");
    var $accord = $("#qObjects");
    $accord.html(html.join(""));
    $accord.accordion({
        autoHeight: false,
        collapsible: false,
        icons: {
            header: null,
            headerSelected: null
        }
    })
}

function onCheckAll() {
    $.each(wmiQueryableObjects(), function(index, item) {
        checkBox_SetChecked(item, true);
        selectedObjects[item] = true;
        queryCount += 1
    });
    queryTotal = queryCount
}

function onCheckNone() {
    $.each(wmiQueryableObjects(), function(index, item) {
        checkBox_SetChecked(item, false);
        selectedObjects[item] = false
    });
    queryCount = 0;
    queryTotal = 0
}

function onObjectSelectChanged(sender) {
    var selected = checkBox_GetChecked(sender.id),
        text = sender.value;
    selectedObjects[text] = selected;
    if (selected) queryCount += 1;
    else queryCount -= 1;
    queryTotal = queryCount
}

function onQuery() {
    var html = [];
    html.push('<select style="width:71%; float:right;" id="lstDomains">');
    $.each(domains_Get(), function(index, item) {
        html.push('<option value="' + item + '">' + item + "</option>")
    });
    html.push('</select><label style="width:25%; float:right;">Domain:</label><br/>');
    html.push('<input type="text" style="width:70%; float:right;" id="txtServer" value="localhost" /><label style="width:25%; float:right;">Server:</label><br/>');
    html.push('<input type="text" style="width:70%; float:right;" id="txtUsername" value=""/><label style="width:25%; float:right;">Username:</label><br/>');
    html.push('<input type="password" style="width:70%; float:right;" id="txtPassword" values=""/><label style="width:25%; float:right;">Password:</label>');
    var $dialog = $("#messageDialog");
    if ($dialog.is("dialog")) $dialog.dialog("destroy");
    $dialog.html(html.join(""));
    $dialog.dialog({
        width: 400,
        height: "auto",
        modal: true,
        title: "Server Details for WMI Query",
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            Cancel: function() {
                $(this).dialog("close")
            },
            Login: function() {
                var d = $("#lstDomains").val(),
                    s = $("#txtServer").val(),
                    u = $("#txtUsername").val(),
                    p = $("#txtPassword").val();
                $("#progressbar").progressbar({
                    value: 0
                });
                $.each(wmiQueryableObjects(), function(index, item) {
                    if (selectedObjects[item]) doQuery(d, s, u, p, item)
                });
                $(this).dialog("close")
            }
        }
    })
}

function doQuery(d, s, u, p, qt) {
    if (!!window.Worker) {
        toggleControl("queryButton", false, true);
        var worker2 = new Worker("../js/" + WORKER);
        worker2.onmessage = function(event) {
            var data = JSON.parse(event.data);
            showResults(data, qt)
        };
        var args = SVC_WMI_QUERY + "?queryType=" + qt + "&user=" + u + "&pass=" + p + "&domain=" + d + "&machine=" + s;
        worker2.postMessage(args)
    } else {
        var msg = "Your browser does not support HTML5 WebWorkers.<br/><br/>Please use a fully-compatible HTML5 browser.";
        showMessage("Not supported", msg, true, 400, true)
    }
}

function createDelims() {
    delims.push(createDelim("Software", "AssignmentType", "Name", "Version"));
    delims.push(createDelim("IDEController", "Caption", "Name", "SystemName"));
    delims.push(createDelim("DiskDrive", "BytesPerSector", "Name", "TracksPerCylinder"));
    delims.push(createDelim("NetworkAdapterConfiguration", "Caption", "ServiceName", "SettingID"));
    delims.push(createDelim("DMAChannel", "Caption", "Name", "Status"));
    delims.push(createDelim("NetworkAdapter", "Availability", "Name", "TimeOfLastReset"));
    delims.push(createDelim("CDROMDrive", "Availability", "Name", "TransferRate"));
    delims.push(createDelim("Processor", "AddressWidth", "Name", "VoltageCaps"))
}

function createDelim(queryType, start, name, end) {
    var foo = {};
    foo.qt = queryType;
    foo.start = start;
    foo.name = name;
    foo.end = end;
    return foo
}

function getDelim(queryType) {
    var delim = null;
    $.each(delims, function(index, item) {
        if (item.qt == queryType) {
            delim = item;
            return false
        }
    });
    return delim
}

function showResults(data, qt) {
    var html = [];
    html.push('<h3><a href="#">' + qt + "</a></h3><div>");
    var delim = getDelim(qt);
    if (delim != null) {
        html.push('<div class="accordion">');
        var subHtml = [];
        $.each(data, function(index, item) {
            var key = item.key,
                val = item.value;
            if (key.substring(0, delim.start.length) === delim.start) {
                subHtml = [];
                subHtml.push("<div><p>");
                subHtml.push(key + ": " + val + "<br/>")
            } else if (key.substring(0, delim.name.length) === delim.name) html.push("<h3>" + val + "</h3>");
            else if (key.substring(0, delim.end.length) === delim.end) {
                subHtml.push(key + ": " + val + "<br/>");
                subHtml.push("</p></div>");
                html.push(subHtml.join(""))
            } else subHtml.push(key + ": " + val + "<br/>")
        });
        html.push("</div>")
    } else if (data.length == 0) html.push("<label>No data was returned for this item</label>");
    else $.each(data, function(index, item) {
        html.push(item.key + ": " + item.value + "<br/>")
    });
    html.push("</div>");
    accordHtml.push(html.join(""));
    queryCount -= 1;
    if (queryCount == 0) {
        var $accord = $("#accordion");
        if ($accord.is(":ui-accordion")) $accord.multiAccordion("destroy");
        $accord.html(accordHtml.join(""));
        $accord.multiAccordion({
            autoHeight: false,
            collapsible: true,
            active: "all",
            icons: {
                header: null,
                headerSelected: null
            }
        });
        $(".accordion").accordion({
            active: false,
            heightStyle: "content",
            collapsible: true
        });
        toggleControl("queryButton", true, true)
    } else {
        var percent = (queryTotal - queryCount) / queryTotal * 100;
        $("#progressbar").progressbar("value", percent)
    }
}

function showResultsOld(data, qt) {
    var html = [];
    html.push('<h3><a href="#">' + qt + "</a></h3><div>");
    if (qt == "Software") {
        html.push('<div class="accordion">');
        var subHtml = [];
        $.each(data, function(index, item) {
            var key = item.key,
                val = item.value;
            if (key.substring(0, 14) == "AssignmentType") {
                subHtml = [];
                subHtml.push("<div><p>");
                subHtml.push(key + ": " + val + "<br/>")
            } else if (key.substring(0, 4) == "Name") html.push("<h3>" + val + "</h3>");
            else if (key.substring(0, 7) == "Version") {
                subHtml.push(key + ": " + val + "<br/>");
                subHtml.push("</p></div>");
                html.push(subHtml.join(""))
            } else subHtml.push(key + ": " + val + "<br/>")
        });
        html.push("</div>")
    } else $.each(data, function(index, item) {
        html.push(item.key + ": " + item.value + "<br/>")
    });
    html.push("</div>");
    accordHtml.push(html.join(""));
    queryCount -= 1;
    if (queryCount == 0) {
        var $accord = $("#accordion");
        $accord.html(accordHtml.join(""));
        $accord.accordion({
            autoHeight: false,
            collapsible: true,
            icons: {
                header: null,
                headerSelected: null
            }
        });
        $(".accordion").accordion({
            active: false,
            heightStyle: "content",
            collapsible: true
        });
        showQueryableObjects();
        toggleControl("queryButton", true, true)
    } else {
        var percent = (queryTotal - queryCount) / queryTotal * 100;
        $("#progressbar").progressbar("value", percent)
    }
}
