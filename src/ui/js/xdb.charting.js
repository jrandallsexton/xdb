var CHART_TYPE_BAR = 0,
    CHART_TYPE_PIE = 1,
    _chartTitle = null,
    TAB_IDX = 0,
    chart00 = null,
    chart01 = null,
    _assetTypeId = null,
    _assetRequestType = null,
    _chartOptions = null,
    _props = [],
    _accordionIndex = null,
    _chartClient = null,
    FILTER_PROP_ID = null,
    OPERATOR_ID = null,
    FILTER_VALUE = null,
    MAX_RECORDS = 0,
    CHART = null,
    chart, options, INIT_MAP = true,
    INIT_GRID = true,
    map = null,
    markersArray = [],
    IS_AUTOWIDTH = true,
    GROUPBY_CLEAR = "clear",
    currPage = 1,
    recordsPerPage = 25,
    colModel = "",
    colHeader = "",
    start = null,
    _imgSrc = "../images",
    _loadMsg = '<br/><br/><br/><br/><br/><br/><br/><br/><div style="width:100%;text-align:center;" ><i>Refreshing filter options & chart.<br/>Please wait.</i></div><br/><div style="text-align:center"><img src="../images/ajax-loader.gif" alt="spinner" style="text-align:left;" /></div>',
    _loadMsg2 = '<div style="width:100%;text-align:center;" >Retrieving data.  Please wait.</div><br/><div style="text-align:center"><img src="../images/ajax-loader.gif" alt="spinner" style="text-align:left;" /></div>',
    _dialogPos = ["top", 225],
    $lstAssetTypes = $("#lstAssetTypes"),
    $lstRequestTypes = $("#lstRequestTypes"),
    $filters = $("#filters"),
    $accordion = $("#accordion");

function initForm() {
    getCurrentUser();
    displayCurrentUser(_imgSrc);
    $("#header").html(buildHeaderContent(_imgSrc));
    $("#navigation").html(buildMenuHtml(_imgSrc));
    refreshRecent(_imgSrc);
    initMenu();
    loadAssetTypes();
    loadAssetTypesAndDefaultViews()
}

function buildChart(chartClient) {
    _chartClient = chartClient;
    var categories = [],
        series = {
            data: []
        };
    if (chartClient.recordCount > 5) {
        var html = [];
        html.push("<option value='0'>[ All ]</option>");
        if (chartClient.recordCount <= 10)
            for (var i = 1; i < chartClient.recordCount; i++) html.push("<option value='" + i + "'>Top " + i + "</option>");
        else
            for (var i = 1; i < chartClient.recordCount; i++)
                if (i <= 10 || i % 5 == 0) html.push("<option value='" + i + "'>Top " + i + "</option>");
        $("#lstMax").show();
        $("#lstMax").html(html.join(""));
        $("#lstMax").val(MAX_RECORDS)
    } else {
        $("#lstMax").html("");
        $("#lstMax").hide()
    }
    $.each(chartClient.data, function(index, item) {
        series.data.push(parseInt(item.Value));
        categories.push(item.Key)
    });
    options.series.push(series);
    options.xAxis.categories = categories;
    chart = new Highcharts.Chart(options);
    if (chartClient.subtitle != "") {
        _chartTitle = chartClient.title + " - " + chartClient.subtitle;
        chart.setTitle({
            text: chartClient.title
        }, {
            text: chartClient.subtitle
        })
    } else {
        _chartTitle = chartClient.title;
        chart.setTitle({
            text: chartClient.title
        })
    }
}

function buildDashDialog() {
    var $dialog = $("#messageDialog"),
        html = [];
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html(_loadMsg2);
    $dialog.dialog({
        width: 600,
        modal: true,
        autoOpen: false,
        title: "Save Chart To Dashboard",
        position: _dialogPos,
        buttons: {
            Cancel: function() {
                $(this).dialog("close")
            },
            Save: function() {
                var saved = onSaveDash(),
                    textToShow = null;
                if (saved) {
                    textToShow = "<p>Saved successfully.</p>";
                    expireUser()
                } else textToShow = "<p>Failed to save.</p>";
                $(this).dialog("close");
                showMessage("Result", textToShow, true, 400, true, _dialogPos)
            }
        },
        open: function(event, ui) {
            $(this).siblings(".ui-dialog-buttonpane").find("button:eq(1)").focus();
            setTimeout(function() {
                showDashDialogContent()
            }, 50)
        }
    });
    $dialog.dialog("open")
}

function chartOptionsBar(divId) {
    var temp = {
        chart: {
            renderTo: divId,
            defaultSeriesType: "bar"
        },
        title: {
            text: "<i>Retrieving data</i>"
        },
        subtitle: {
            text: "<i>please wait</i>"
        },
        xAxis: {
            allowDecimals: false,
            categories: [],
            title: {
                text: null
            }
        },
        yAxis: {
            allowDecimals: false,
            min: 0,
            title: {
                text: "Number of Assets",
                align: "high"
            }
        },
        tooltip: {
            formatter: function() {
                getTooltipData(this.x)
            }
        },
        plotOptions: {
            bar: {
                dataLabels: {
                    enabled: true
                }
            }
        },
        legend: {
            enabled: false,
            layout: "vertical",
            align: "right",
            verticalAlign: "top",
            x: -100,
            y: 100,
            floating: true,
            borderWidth: 1,
            shadow: true
        },
        credits: {
            enabled: false
        },
        series: []
    };
    return temp
}

function clearOverlays() {
    for (var i = 0; i < markersArray.length; i++) markersArray[i].setMap(null)
}

function createChartOptionsObject(chartId, assetTypeId, assetRequestType, propertyId) {
    var temp = {};
    temp.id = chartId;
    temp.assetTypeId = assetTypeId;
    temp.assetRequestType = assetRequestType;
    temp.filters = [];
    temp.filterExpression = "";
    temp.filterName = "";
    temp.maxRecords = -1;
    temp.propertyId = propertyId;
    temp.isDirty = true;
    return temp
}

function createFilterExpression(filters) {
    var html = [],
        filterCount = filters.length;
    html.push("(");
    for (var i = 0; i < filterCount; i++)
        if (i == filterCount - 1) html.push("({" + i + "})");
        else html.push("({" + i + "}) AND ");
    html.push(")");
    return html.join("")
}

function createFilterObject(propertyId, operatorId, value, order, propName, propValue) {
    var filter = {};
    filter.propertyId = propertyId;
    filter.operatorId = operatorId;
    filter.value = value;
    filter.order = order;
    filter.propertyName = propName;
    filter.propertyValue = propValue;
    return filter
}

function createFilterPanel(chartOptions, accordionIndex) {
    _accordionIndex = accordionIndex;
    $accordion.html(_loadMsg);
    var filterClient = assetFilterClient_Get(chartOptions, onFilterPanelReturned)
}

function getChartClient() {
    var url = URL_REST_SVC + "/Chart?chartId=" + _chartOptions.id;
    return ajaxGet(url)
}

function getTooltipData(x) {
    if (_chartClient == null || _chartOptions.id === null) return;
    var msg = [],
        max = _chartClient.dataDictionary.length,
        lookupId = null;
    for (var i = 0; i < max; i++) {
        var item = _chartClient.dataDictionary[i];
        if (item.Key == x) {
            lookupId = item.Value;
            break
        }
    }
    var data = chart_GetSupportData(_chartOptions.id, lookupId);
    if (data == null) return;
    var dataLength = data.length - 1,
        viewId = ASSET_TYPES_VIEWS[_chartOptions.assetTypeId];
    if (_chartOptions.assetRequestType == 1) viewId = ASSET_TYPES_VIEWS_INS[_chartOptions.assetTypeId];
    $.each(data, function(index, item) {
        if (index > 20) {
            msg.push("...");
            return false
        } else {
            var url = createViewUrlPlain(viewId, item.Key, null, null, false);
            if (index == dataLength) msg.push('<a href="' + url + '" target="_blank" class="growlLink">' + (index + 1) + ". " + item.Value);
            else msg.push('<a href="' + url + '" target="_blank" class="growlLink">' + (index + 1) + ". " + item.Value + "<br/>")
        }
    });
    $.growl(x, msg.join(""))
}

function initGrid() {
    var grouping = false,
        groupColParam = null,
        chartClient = getChartClient(),
        rList = [25, 50, 100, "All"];
    if (grouping) {
        recordsPerPage = 1000000;
        rList = ["All"]
    }
    jQuery("#refData").jqGrid({
        contentType: "json",
        url: URL_REST_SVC + "/ChartDataForGrid?chartId=" + _chartOptions.id,
        jsonReader: {
            repeatitems: false,
            root: "rows",
            cell: "",
            id: "0"
        },
        datatype: "json",
        mtype: "GET",
        colNames: eval(colHeader),
        colModel: eval(colModel),
        caption: chartClient.Name,
        pager: jQuery("#pager"),
        rowList: rList,
        pginput: "true",
        rowNum: recordsPerPage,
        viewrecords: true,
        autowidth: true,
        grouping: grouping,
        groupingView: {
            groupField: [groupColParam],
            groupColumnShow: [true],
            groupText: ["<b>{0}</b>"],
            groupCollapse: true,
            groupOrder: ["asc"],
            groupSummary: [false]
        },
        shrinkToFit: true,
        height: 200,
        loadonce: true,
        gridview: true,
        loadError: function(xhr, status, error) {
            return
        },
        loadtext: 'Retrieving data for <br/><i>"' + chartClient.Name + '"</i><br/>Please wait.',
        onPaging: function(pgButton) {},
        ondblClickRow: function(rowid, iRow, iCol, e) {
            onRowDoubleClick(rowid)
        },
        gridComplete: function() {
            var objRows = $("#refData tr"),
                objHeader = $("#refData .jqgfirstrow td");
            if (objRows.length > 1) {
                var objFirstRowColumns = $(objRows[1]).children("td"),
                    colLength = objFirstRowColumns.length;
                for (i = 0; i < colLength; i++) $(objFirstRowColumns[i]).css("width", $(objHeader[i]).css("width"))
            }
            jQuery(".ui-jqgrid-bdiv").css("height", jQuery("#refData").css("height"))
        }
    });
    if (!grouping) $("#pager .ui-pg-selbox option[value='All']").val(1000000)
}

function initMap() {
    if (typeof google === "undefined" || !google.maps) return;
    if (map == null) {
        var mapHeight = $(window).height() * .69;
        $("#map_canvas").height(mapHeight);
        var mapOptions = {
            center: new google.maps.LatLng(0, 0),
            zoom: 2,
            mapTypeId: google.maps.MapTypeId.ROADMAP,
            draggable: true
        };
        map = new google.maps.Map(document.getElementById("map_canvas"), mapOptions)
    } else clearOverlays();
    $.each(mapData(), function(index, item) {
        var marker = new google.maps.Marker({
            title: item.title,
            position: new google.maps.LatLng(item.position.lat, item.position.lng),
            map: map
        });
        markersArray.push(marker);
        marker.setMap(map);
        marker.setClickable(true);
        marker.data = item.data;
        google.maps.event.addListener(marker, "click", function(e) {
            map.setCenter(new google.maps.LatLng(marker.position.lat(), marker.position.lng()));
            map.setZoom(map.getZoom() + 1);
            onItemClick(e, marker)
        })
    })
}

function loadAssetTypes() {
    var html = [];
    html.push('<option value="-1">[Select]</option>');
    $.each(assetTypes_GetDictionary(), function(index, item) {
        html.push('<option value="' + item.key + '">' + item.value + "</option>")
    });
    $lstAssetTypes.html(html.join(""))
}

function mapData() {
    return ajaxGet(URL_REST_SVC + "/MapData?chartId=" + _chartOptions.id)
}

function mapMarkerData(itemId) {
    var url = URL_REST_SVC + "/MapMarkerData?chartId=" + _chartOptions.id + "&itemId=" + itemId;
    return ajaxGet(url)
}

function onAssetTypeChanged(data) {
    _assetTypeId = data.value;
    _assetRequestType = null;
    FILTER_PROP_ID = null;
    OPERATOR_ID = null;
    FILTER_VALUE = null;
    $lstRequestTypes.html("");
    var hasDefs = assetType_HasAssets(data.value, ART_DEFINITION, true),
        hasIns = assetType_HasAssets(data.value, ART_INSTANCE, true);
    if (hasDefs && hasIns) {
        var tempVal = assetType_GetReportingLabel(data.value, ART_DEFINITION);
        htmlSelect_InsertOption("lstRequestTypes", ART_DEFINITION, tempVal, false);
        tempVal = assetType_GetReportingLabel(data.value, ART_INSTANCE);
        htmlSelect_InsertOption("lstRequestTypes", ART_INSTANCE, tempVal, false);
        htmlSelect_InsertOption("lstRequestTypes", -1, "[ Select ]", true);
        $lstRequestTypes.show();
        $("#lblBy").hide();
        $("#lstProperties").hide()
    } else {
        $lstRequestTypes.hide();
        var tempVal = null,
            tempData = {};
        if (hasDefs) tempData.requestType = ART_DEFINITION;
        else tempData.requestType = ART_INSTANCE;
        tempData.assetTypeId = data.value;
        onRequestTypeChanged(tempData);
        $("#lblBy").show();
        $("#lstProperties").show()
    }
    $("#lstMax").hide();
    $("#lblWhere").hide();
    $("#lstProperties2").hide();
    $("#lstOperators").hide();
    $("#lstValues").hide()
}

function onFilterPanelReturned(data) {
    var filterClient = data,
        html = [];
    if (_chartOptions.filters.length > 0) {
        var html2 = [];
        html2.push('<label style="float:left">Filter Condition(s)</label><br/>');
        html2.push('<div id="lstFilters" style="position:relative;background-color:white;width:100%;height:auto;border:none;"></div>');
        $filters.html(html2.join(""));
        var filterCount = _chartOptions.filters.length,
            html3 = [];
        for (var i = 0; i < filterCount; i++) {
            var f = _chartOptions.filters[i];
            html3.push('<div style="float:left;"><span class="ui-icon ui-icon-circle-close" style="float:left; margin-right: .2em;" onClick="removeFilter(' + i + ')"/>');
            html3.push("(" + f.propertyName + " " + filterDisplayFromId(f.operatorId) + " " + f.propertyValue + ")</div><br/>")
        }
        $("#lstFilters").html(html3.join(""));
        $filters.show()
    } else $filters.hide();
    _props = [];
    $.each(filterClient.Properties, function(index, item) {
        if (filterClient.Values[index] == undefined) return;
        _props[item.Value] = item.Key;
        html.push("<h3>" + item.Key + "</h3>");
        html.push("<div>");
        var filterOptions = filterClient.Values[index].Value;
        if (filterOptions.length > 1) {
            html.push('<span id="' + item.Value + '" style="float:left" title="Uncheck All" class="ui-icon ui-icon-closethick" onClick="onUncheckAll(\'' + item.Key + "')\"></span>");
            html.push('<span id="' + item.Value + '" style="float:right" title="Check All" class="ui-icon ui-icon-check" onClick="onCheckAll(\'' + item.Key + "')\"></span><br/>");
            $.each(filterOptions, function(subindex, subitem) {
                var val = subitem.key;
                val = val.replace(/"/g, "");
                val = val.replace(/'/g, "");
                if (subitem.enabled) html.push('<input type="checkbox" id="chk' + subitem.value + '" checked="checked" onchange="onPropCheckChange(\'' + item.Value + "', '" + subitem.value + '\')" value="' + val + '"/>' + val + "<br/>");
                else html.push('<input type="checkbox" id="chk' + subitem.value + '" disabled="disabled" value="' + val + '"/>' + val + "<br/>")
            })
        } else $.each(filterOptions, function(subindex, subitem) {
            var val = subitem.key;
            val = val.replace(/"/g, "");
            val = val.replace(/'/g, "");
            html.push("<p>" + val + "</p>")
        });
        html.push("</div>")
    });
    if ($accordion.is(":ui-accordion")) $accordion.accordion("destroy");
    $accordion.html(html.join(""));
    $accordion.accordion({
        autoHeight: false,
        collapsible: true,
        active: false,
        icons: {
            header: null,
            headerSelected: null
        }
    });
    if (_accordionIndex !== null) $accordion.accordion("option", "active", _accordionIndex)
}

function onItemClick(event, pin) {
    var html = [];
    html.push("<h4>" + pin.title + '</h4><table style="width:100%">');
    var data = mapMarkerData(pin.data[0].id);
    $.each(data, function(index, item) {
        html.push('<tr style="width:100%"><td style="width:40px"><a href="http://www.google.com">' + item.Value + "</a></td></tr>")
    });
    html.push("</table>");
    var infowindow = new google.maps.InfoWindow();
    infowindow.setContent(html.join(""));
    infowindow.setPosition(pin.position);
    infowindow.open(map)
}

function onMaxChanged(data) {
    MAX_RECORDS = $("#lstMax").val();
    renderChart(_assetTypeId, _assetRequestType, NULL_GUID, NULL_GUID, NULL_GUID, null)
}

function onOperatorChanged(data) {
    $("#lstMax").hide();
    $("#lstValues").show();
    OPERATOR_ID = $("#lstOperators").val()
}

function onPropertyChanged(data) {
    var propId = $("#lstProperties").val();
    FILTER_PROP_ID = null;
    OPERATOR_ID = null;
    FILTER_VALUE = null;
    MAX_RECORDS = 0;
    _chartOptions = createChartOptionsObject(generateGuid(), _assetTypeId, _assetRequestType, propId);
    $accordion.html(_loadMsg);
    assetFilterClient_Get(_chartOptions, onFilterPanelReturned);
    renderChart(_chartOptions);
    INIT_GRID = true;
    $("#tabs").tabs({
        selected: 0,
        show: function(event, ui) {
            switch (ui.index) {
                case 2:
                    if (INIT_GRID) {
                        renderReport();
                        INIT_GRID = false
                    }
                    break
            }
        },
        select: function(event, ui) {
            TAB_IDX = ui.index;
            switch (ui.index) {
                case 0:
                    break;
                case 1:
                    if (INIT_MAP) {
                        initMap();
                        INIT_MAP = false
                    }
                    break
            }
        }
    });
    $("#dash").show()
}

function onPropCheckChange(propertyId, valueId) {
    var ctlId = "chk" + valueId,
        checked = checkBox_GetChecked(ctlId),
        val = $("#" + ctlId).val(),
        filter = createFilterObject(propertyId, OP_NOTEQUAL, valueId, _chartOptions.filters.length, _props[propertyId], val);
    _chartOptions.filters[_chartOptions.filters.length] = filter;
    _chartOptions.filterExpression = createFilterExpression(_chartOptions.filters);
    _chartOptions.isDirty = true;
    _accordionIndex = $accordion.accordion("option", "active");
    createFilterPanel(_chartOptions, _accordionIndex);
    renderChart(_chartOptions);
    $("#tabs").tabs({
        selected: 0
    });
    INIT_MAP = true;
    INIT_GRID = true
}

function onProperty2Changed(data) {
    $("#lstValues").html("");
    var propId2 = $("#lstProperties2").val();
    FILTER_PROP_ID = propId2;
    var html = [];
    html.push("<option value='-1'>[ Select ]</option>");
    $.each(propertyValues_GetPotentialByAssetType(_assetTypeId, propId2, _assetRequestType), function(index, item) {
        html.push("<option value='" + item.Key + "'>" + item.Value + "</option>")
    });
    $("#lstValues").html(html.join(""));
    $("#lstOperators").show()
}

function onRequestTypeChanged(data) {
    $("#lstProperties").html("");
    $("#lstProperties2").html("");
    $("#lstOperators").html("");
    $("#lstValues").html("");
    var atId = null,
        art = null;
    if (data == null) {
        atId = $("#lstAssetTypes").val();
        art = $("#lstRequestTypes").val()
    } else {
        atId = data.assetTypeId;
        art = data.requestType
    }
    _assetRequestType = art;
    FILTER_PROP_ID = null;
    OPERATOR_ID = null;
    FILTER_VALUE = null;
    var html = [];
    html.push("<option value='-1'>[ Select ]</option>");
    $.each(properties_GetNonFreeformByAssetTypeId(atId, art), function(index, item) {
        html.push("<option value='" + item.key + "'>" + item.value + "</option>")
    });
    $("#lstProperties").html(html.join(""));
    $("#lstProperties2").html(html.join(""));
    html = [];
    html.push("<option value='-1'>[ Select ]</option>");
    $.each(operators_Get(), function(index, item) {
        html.push("<option value='" + item.key + "'>" + item.value + "</option>")
    });
    $("#lstOperators").html(html.join(""));
    $("#lstMax").hide();
    $("#lblBy").show();
    $("#lstProperties").show()
}

function onSaveDash() {
    var dashId = $("#dashes :selected").val(),
        saved = null;
    if (dashId == -1) {
        var dash = {};
        dash.id = generateGuid();
        dash.caption = $("#newDash").val();
        dash.index = 0;
        dash.items = [];
        dash.memberId = USER_CURRENT.Id;
        var dashItem = {};
        dashItem.id = generateGuid();
        dashItem.dashboardId = dash.id;
        dashItem.index = 0;
        dashItem.title = "";
        dashItem.subtitle = "";
        switch (TAB_IDX) {
            case 0:
                dashItem.itemType = 1;
                break;
            case 1:
                dashItem.itemType = 3;
                break;
            case 2:
                dashItem.itemType = 2;
                break
        }
        dashItem.chartId = _chartOptions.id;
        dash.items.push(dashItem);
        saved = dashboard_Save(dash)
    } else {
        var dashItem = {};
        dashItem.id = generateGuid();
        dashItem.dashboardId = dashId;
        dashItem.index = 0;
        dashItem.title = "";
        dashItem.subtitle = "";
        switch (TAB_IDX) {
            case 0:
                dashItem.itemType = 1;
                break;
            case 1:
                dashItem.itemType = 3;
                break;
            case 2:
                dashItem.itemType = 2;
                break
        }
        dashItem.chartId = _chartOptions.id;
        saved = dashboardItem_Add(dashItem)
    }
    return saved
}

function onValueChanged(data) {
    var propId = $("#lstProperties").val(),
        propId2 = $("#lstProperties2").val(),
        opId = $("#lstOperators").val(),
        value = $("#lstValues").val();
    FILTER_VALUE = value;
    renderChart(_chartOptions)
}

function refreshData(chartOptions) {
    var chartClient = chart_Get(chartOptions);
    buildChart(chartClient)
}

function renderChart(chartOptions) {
    $("#innerContent").show();
    var chartHeight = $(window).height() * .69;
    $("#chart00").height(chartHeight);
    options = chartOptionsBar("chart00");
    chart = new Highcharts.Chart(options);
    chart.showLoading("Loading data from server...");
    var chartClient = chart_Get(chartOptions);
    buildChart(chartClient)
}

function renderMap() {
    var map = new OpenLayers.Map("map"),
        ol_wms = new OpenLayers.Layer.WMS("OpenLayers WMS", "http://vmap0.tiles.osgeo.org/wms/vmap0", {
            layers: "basic"
        }),
        ol_wms_nobuffer = new OpenLayers.Layer.WMS("OpenLayers WMS (no tile buffer)", "http://vmap0.tiles.osgeo.org/wms/vmap0", {
            layers: "basic"
        }, {
            buffer: 0
        });
    map.addLayers([ol_wms, ol_wms_nobuffer]);
    map.addControl(new OpenLayers.Control.LayerSwitcher());
    map.setCenter(new OpenLayers.LonLat(0, 0), 6)
}

function renderReport() {
    start = new Date();
    jQuery("#refData").jqGrid("GridUnload");
    var chart = getChartClient(),
        propCount = chart.PropertyNames.length,
        html = [];
    colModel = "[";
    colHeader = "[";
    $.each(chart.PropertyNames, function(index, item) {
        if (index > 0) htmlSelect_InsertOption("lstGroupBy", item, item, false);
        colModel += '{"name":"' + item + '","index":"' + item + '"}';
        colHeader += '"' + item + '"';
        if (index < propCount - 1) {
            colHeader += ",";
            colModel += ","
        }
    });
    colModel += "]";
    colHeader += "]";
    htmlSelect_InsertOption("lstGroupBy", GROUPBY_CLEAR, "[ Group by ... ]", true);
    initGrid()
}

function showDashDialogContent() {
    var dashes = dashboards_Get(USER_CURRENT.Id),
        html = [];
    html.push("<p>Add To Existing Dashboard Tab:</p>");
    html.push('<select id="dashes" style="width:100%;"><option value=-1 selected="selected">[Select]</option>');
    $.each(dashes, function(index, value) {
        html.push('<option value="' + value.key + '">' + value.value + "</option>")
    });
    html.push("</select>");
    html.push('<p id="newDashText" >Create New Dashboard Tab:</p>');
    html.push('<input id="newDash" type="text" style="width:100%;margin-top:3px;"></input>');
    $("#messageDialog").html(html.join(""));
    $("#dashes").bind("change", function() {
        if ($("#dashes :selected").val() == -1) {
            $("#newDashText").show();
            $("#newDash").show()
        } else {
            $("#newDashText").hide();
            $("#newDash").hide()
        }
        return false
    })
}
Highcharts.setOptions({
    colors: ["#DDDF0D", "#7798BF", "#55BF3B", "#DF5353", "#aaeeee", "#ff0066", "#eeaaee", "#55BF3B", "#DF5353", "#7798BF", "#aaeeee"],
    chart: {
        backgroundColor: {
            linearGradient: [0, 0, 0, 400],
            stops: [
                [0, "rgb(96, 96, 96)"],
                [1, "rgb(16, 16, 16)"]
            ]
        },
        borderWidth: 0,
        borderRadius: 15,
        plotBackgroundColor: null,
        plotShadow: false,
        plotBorderWidth: 0
    },
    title: {
        style: {
            color: "#FFF",
            font: "16px Lucida Grande, Lucida Sans Unicode, Verdana, Arial, Helvetica, sans-serif"
        }
    },
    subtitle: {
        style: {
            color: "#DDD",
            font: "12px Lucida Grande, Lucida Sans Unicode, Verdana, Arial, Helvetica, sans-serif"
        }
    },
    xAxis: {
        gridLineWidth: 0,
        allowDecimals: false,
        lineColor: "#999",
        tickColor: "#999",
        labels: {
            style: {
                color: "#999",
                fontWeight: "bold"
            }
        },
        title: {
            style: {
                color: "#AAA",
                font: "bold 12px Lucida Grande, Lucida Sans Unicode, Verdana, Arial, Helvetica, sans-serif"
            }
        }
    },
    yAxis: {
        alternateGridColor: null,
        minorTickInterval: null,
        gridLineColor: "rgba(255, 255, 255, .1)",
        lineWidth: 0,
        tickWidth: 0,
        labels: {
            style: {
                color: "#999",
                fontWeight: "bold"
            }
        },
        title: {
            style: {
                color: "#AAA",
                font: "bold 12px Lucida Grande, Lucida Sans Unicode, Verdana, Arial, Helvetica, sans-serif"
            }
        }
    },
    legend: {
        itemStyle: {
            color: "#CCC"
        },
        itemHoverStyle: {
            color: "#FFF"
        },
        itemHiddenStyle: {
            color: "#333"
        }
    },
    credits: {
        style: {
            right: "50px"
        }
    },
    labels: {
        style: {
            color: "#CCC"
        }
    },
    tooltip: {
        backgroundColor: {
            linearGradient: [0, 0, 0, 50],
            stops: [
                [0, "rgba(96, 96, 96, .8)"],
                [1, "rgba(16, 16, 16, .8)"]
            ]
        },
        borderWidth: 0,
        style: {
            color: "#FFF"
        }
    },
    plotOptions: {
        line: {
            dataLabels: {
                color: "#CCC"
            },
            marker: {
                lineColor: "#333"
            }
        },
        spline: {
            marker: {
                lineColor: "#333"
            }
        },
        scatter: {
            marker: {
                lineColor: "#333"
            }
        }
    },
    toolbar: {
        itemStyle: {
            color: "#CCC"
        }
    }
});
$.growl.settings.defaultStylesheet = "../css/xdb.growl.css";
$.growl.settings.displayTimeout = 8000;
$.growl.settings.dockCss.width = "275px";
$.growl.settings.noticeTemplate = "" + '<table width="225" border="0" cellpadding="0" cellspacing="0">' + "<tr>" + '<td style="background-image: url(../images/growl/dm_top.png); width: 225px; height: 49px; background-repeat: no-repeat; color: #fff;">' + '<img src="%image%" style="border-style:none; max-width:25px; max-height:25px; text-align:center; margin-left:19px; margin-top:19px;" />' + '<h1 style="font-size: 12px; margin: 0pt; margin-left: 5px; margin-bottom: 10px; display: inline;">%title%</h1>' + "</td>" + "</tr>" + '<tr style="font-size: 12px;">' + '<td style="background-image: url(../images/growl/dm_repeat.png); width: 225px; background-repeat: repeat-y; color: #ddd; font-size: 12px;">' + '<p style="margin: 20px;">%message%</p>' + "</td>" + "</tr>" + "<tr>" + '<td style="background-image: url(../images/growl/dm_bottom.png); background-repeat: no-repeat; width: 225px; height: 27px;" valign="top" align="right" >' + '<a style="margin-right: 25px; font-size: 10px; color: #fff; text-align: right;" href="" onclick="return false;" rel="close">Close</a>' + "</td>" + "</tr>" + "</table>"
