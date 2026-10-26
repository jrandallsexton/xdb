var ICON_SAVE = "icon-check",
    ICON_ADD = "icon-plusthick",
    ICON_IGNORE = "icon-closethick",
    ICON_RESTRICT = "icon-locked",
    MAP_KEY = null,
    _imgSrc = "../images";

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
    var html = [];
    html.push("<table>");
    $.each(mappingsUnmapped(), function(index, item) {
        var icons = insertIcon(ICON_SAVE, item.key, "Save Mapping") + insertIcon(ICON_ADD, item.key, "Add");
        icons += insertIcon(ICON_IGNORE, item.key, "Ignore") + insertIcon(ICON_RESTRICT, item.key, "Restrict Application");
        html.push('<tr id="tr' + item.key + '"><td>' + item.value + '</td><td style="width:500px;">' + autoCompleteHtml(item.key) + '</td><td><div style="float:left">' + icons + "</div></td></tr>")
    });
    html.push("</table>");
    $("#content").html(html.join(""));
    activateAuto()
}

function autoCompleteHtml(key) {
    var tempValue = '<input id="' + key + '" class="ui-widget ui-widget-content autocomplete" style="width:98%;"/>';
    tempValue += '<input type="hidden" id="' + key + '-id"/>';
    return tempValue
}

function insertIcon(iconName, key, toolTip) {
    var spanId = null;
    switch (iconName) {
        case ICON_SAVE:
            spanId = "s";
            break;
        case ICON_ADD:
            spanId = "a";
            break;
        case ICON_IGNORE:
            spanId = "i";
            break;
        case ICON_RESTRICT:
            spanId = "r";
            break
    }
    spanId = spanId + key;
    return '<span id="' + spanId + '" style="float:right" title="' + toolTip + '" class="ui-icon ui-' + iconName + '" onClick="onc(\'' + iconName + "', '" + key + "')\"></span>"
}

function onc(iconName, key) {
    switch (iconName) {
        case ICON_SAVE:
            if (MAP_KEY == null) {
                showMessage("Nothing to Map", "Nothing selected for a mapping.", true, 400, true);
                return
            } else {
                mapping_Set(key, MAP_KEY);
                $("#tr" + key).fadeOut()
            }
            break;
        case ICON_ADD:
            break;
        case ICON_IGNORE:
            break;
        case ICON_RESTRICT:
            break
    }
}

function activateAuto() {
    $(":text.autocomplete").focusin(function() {
        var imgRandomID = new Date().getTime();
        $(this).after("<img id='img" + imgRandomID + "' src='../images/ajax-loader.gif' alt='spinner' style='position:absolute;top:" + $(this).offset().top + "px;left:" + $(this).offset().left + "px' />");
        var dataSource = MAPPINGS_ASSET_TYPE_ID && MAPPINGS_VIEW_ID ? assets_GetForView("6CACA132-7827-4DE6-81E6-2B7D8DB939B9", MAPPINGS_ASSET_TYPE_ID, MAPPINGS_VIEW_ID) : [],
            key = "Assets_" + MAPPINGS_ASSET_TYPE_ID + "_" + MAPPINGS_VIEW_ID;
        sessionCache(key, null, false);
        var minChars = 0;
        if (dataSource.length > MAX_AUTOCOMPLETE_ITEMS) minChars = 2;
        $(this).autocomplete({
            minLength: minChars,
            source: dataSource,
            focus: function(event, ui) {
                $(this).val(ui.item.value);
                return false
            },
            select: function(event, ui) {
                $(this).val(ui.item.value);
                $(this.id + "-id").val(ui.item.key);
                MAP_KEY = ui.item.key;
                setTimeout('onItemSelected("' + ui.item.key + '")', 25);
                this.blur();
                return false
            }
        }).data("autocomplete")._renderItem = function(ul, item) {
            return $("<li class='ui-widget'></li>").data("item.autocomplete", item).append("<a>" + item.value + "</a>").appendTo(ul)
        };
        this.autoCompleteRendered = true;
        $("#img" + imgRandomID).remove();
        $(this).autocomplete("search")
    })
}

function onItemSelected(key) {}

function process(dictionary) {
    if (dictionary !== null) return dictionary;
    else {
        var array = [],
            obj = {};
        obj.value = "No Records";
        obj.key = NULL_GUID;
        array[array.length] = obj;
        return array
    }
}
