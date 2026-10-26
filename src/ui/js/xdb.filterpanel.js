$(document).ready(function() {
    initPanel();
    $("#open").click(function() {
        $("div#panel").slideDown();
        if (!_attribsLoaded) {
            loadAttributes();
            loadOperators();
            _attribsLoaded = true
        }
    });
    $("#close").click(function() {
        $("div#panel").slideUp()
    });
    $("#toggle a").click(function() {
        $("#toggle a").toggle()
    })
});
var _viewId = null,
    _assetTypeId = null,
    _art = null,
    _userFilters = null,
    _attribsLoaded = false;

function initPanel() {
    var w = $("#container").width();
    $("#toppanel").width(w);
    $("#toppanel").css("z-index", 1001)
}

function beginFilterCreation(loadAttribs) {
    $("#pnl00").html(createFilterPanel_Left());
    if (loadAttribs && !_attribsLoaded) {
        loadAttributes();
        loadOperators();
        _attribsLoaded = true
    }
    $("#pnl01").html(createFilterPanel_Mid()).hide();
    $("#pnl02").html(createFilterPanel_Right()).hide()
}

function createFilterPanel_Left() {
    return createFilterPanelUI(_viewId, _assetTypeId, _art == ART_INSTANCE, _userFilters)
}

function createFilterPanel_Mid() {
    var html = [];
    html.push('<label style="float:left">Filter Condition(s)</label><br/>');
    html.push('<div id="lstFilters" style="position:relative;background-color:white;width:100%;height:auto;border:none;"></div>');
    html.push('<div id="status" style="display:none;width:100%;border:none;text-align:center;margin-top:25px;"><i>... QuickFilter Saved ...</i></div>');
    return html.join("")
}

function createFilterPanel_Right() {
    var html = [];
    html.push('<label id="results" style="float:left">Results</label>');
    html.push('<select id="lstAssets" class="filterResult filterWidget" size="13" style="width:100%;border:none;"></select>');
    html.push('<div style="width:100%;text-align:center;">');
    html.push('<input type="submit" id="btnClear" disabled="disabled" class="ui-widget ui-state-disabled filterResultButton" value="Clear All Filters" onClick="onClearAll()"/>');
    html.push('<input type="submit" id="btnSave" disabled="disabled" class="ui-widget ui-state-disabled filterResultButton" value="Save Filter" onClick="onSave()"/>');
    html.push('<input type="submit" id="btnApply" disabled="disabled" class="ui-widget ui-state-disabled filterResultButton" value="Apply to View" onClick="onApply()"/></div>');
    return html.join("")
}

function createQuickFilters(viewId, assetTypeId, isInstance, userFilters) {
    _viewId = viewId;
    _assetTypeId = assetTypeId;
    _art = isInstance ? ART_INSTANCE : ART_DEFINITION;
    _userFilters = userFilters;
    if (userFilters == null || userFilters.length == 0) {
        var html = [];
        html.push("<h2><i>No previously-saved filters found</i></h2>");
        html.push('<label class="filterInstruction">To begin creating a filter:</label><br/>');
        html.push("<ul>");
        html.push("<li>Select an attribute</li>");
        html.push("<li>Select an operator ( =, !=, etc )</li>");
        html.push("<li>After potential values appear, select one</li>");
        html.push("<li>Select 'Add Filter'</li>");
        html.push("<li>Matching assets will then appear</li>");
        html.push("<li>Your filter can be saved at any time</li>");
        html.push("<li>Previously saved filters can be used later</li>");
        html.push("</ul>");
        html.push('<input id="gotIt" type="submit" class="deleteButtons ui-corner-all ui-state-default" value="Got it! Let\'s Go!" onClick="beginFilterCreation(true)"/>');
        return html.join("")
    } else return beginFilterCreation(false)
}

function getMatchingAssets() {
    return '<label class="filterWidget">No filters applied</label>'
}

function loadAttributes() {
    $("#lstAttributes").html(createAttributeSelectorHtml(_assetTypeId, _art, false));
    toggleControl("lstAttributes", true, true)
}

function loadOperators() {
    $("#lstOperators").html(createOperatorSelectorHtml(false))
}

function onSave() {
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html('<input id="txtFilterName" type="text" style="width:100%"></input>');
    $dialog.dialog({
        width: 600,
        height: "auto",
        position: ["top", 100],
        autoOpen: false,
        modal: true,
        buttons: {
            Cancel: function() {
                $(this).dialog("close")
            },
            Save: function() {
                _searchClient.filterName = $("#txtFilterName").val();
                if (viewFilter_Save(_searchClient)) {
                    expireUser();
                    expireViewClient(_viewId);
                    getCurrentUser(false);
                    $(this).dialog("close");
                    $("div#panel").slideDown();
                    $("#toggle a").toggle();
                    $("#status").fadeIn(2000).fadeOut(500).fadeIn(250).fadeOut(100)
                } else alert("Failed to save QuickFilter")
            }
        },
        title: "Enter a Name for the QuickFilter",
        closeOnEscape: true
    });
    $dialog.dialog("open");
    $("div#panel").slideUp();
    $("#toggle a").toggle()
}

function onClearAll() {
    $("#lstValues").html("");
    toggleControl("lstOperators", false, false);
    toggleControl("lstValues", false, false);
    toggleControl("btnCheckAll", false, true);
    toggleControl("btnUncheckAll", false, true);
    toggleControl("btnAdd", false, true);
    toggleControl("btnAddOr", false, true);
    toggleControl("btnAddAnd", false, true);
    $("#lstFilters").html("");
    $("#lstAssets").html("");
    toggleControl("btnClear", false, true);
    toggleControl("btnSave", false, true);
    toggleControl("btnApply", false, true);
    reFetchAssets = true;
    activateAssetSelector(null);
    reFetchAssets = true;
    $driver.val("");
    clearForm($("#tabs"), false, false);
    _prefetch = true;
    driverChanged(null);
    toggleControl("lstAttributes", true, false);
    $("#lstAttributes").val("-1");
    $("#lstAttribValues").html("");
    _attribId = null;
    _attribName = null;
    _filters = [];
    _filterGroups = [];
    _filterOps = [];
    _opId = null;
    _searchClient = null
}

function onApply() {
    var filteredAssets = htmlSelect_GetAllValues("lstAssets");
    displayFiltersBySearchClient(_searchClient);
    if (filteredAssets !== null) {
        var newDs = autoComplete_CreateDataSource(filteredAssets);
        reFetchAssets = true;
        activateAssetSelector(newDs);
        reFetchAssets = false;
        $driver.val("");
        clearForm($("#tabs"), false, false);
        _prefetch = true;
        driverChanged(null);
        $("div#panel").slideUp();
        $("#toggle a").toggle()
    }
}
