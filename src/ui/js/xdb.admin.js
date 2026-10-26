var ITEM_TYPE_ASSET_TYPE = 0,
    ITEM_TYPE_PICKLIST = 1,
    ITEM_TYPE_PROPERTY = 2,
    ITEM_TYPE_PROPERTY_GROUP = 3,
    ITEM_TYPE_VIEW = 4,
    ITEM_TYPE_ROLE = 5,
    ITEM_TYPE_SVC_MODULE = 6,
    ITEM_TYPE_REPORT = 7,
    ITEM_TYPE_HELP_SECTION = 8,
    ITEM_TYPE_CHART = 9,
    SELECTED_ITEM_TYPE = null,
    SELECTED_ITEM = null;

function activateAutoCompletes(itemType, includeDeletedItems) {
    if (includeDeletedItems === null || includeDeletedItems == undefined) includeDeletedItems = false;
    $("#lstItems").focusin(function() {
        var imgRandomID = new Date().getTime();
        $(this).after("<img id='img" + imgRandomID + "' src='../images/ajax-loader.gif' alt='spinner' style='position:absolute;top:" + $(this).offset().top + "px;left:" + $(this).offset().left + "px' />");
        var dataSource = getItems(itemType, includeDeletedItems),
            minChars = 0;
        if (dataSource.length > MAX_AUTOCOMPLETE_ITEMS) minChars = 2;
        $(this).autocomplete({
            delay: 0,
            minLength: minChars,
            source: dataSource,
            focus: function(event, ui) {
                $(this).val(ui.item.value);
                return false
            },
            select: function(event, ui) {
                $(this).val(ui.item.value);
                $(this.id + "-id").val(ui.item.key);
                setTimeout("onItemSelected(" + itemType + ', "' + ui.item.key + '")', 25);
                this.blur();
                return false
            }
        }).data("autocomplete")._renderItem = function(ul, item) {
            if ($(ul).children().length == 0) {
                var obj = {};
                obj.value = "[Enter New]";
                obj.key = 0;
                $("<li class='ui-widget'></li>").data("item.autocomplete", obj).append("<a>" + obj.value + "</a>").appendTo(ul)
            }
            return $("<li class='ui-widget'></li>").data("item.autocomplete", item).append("<a>" + item.value + "</a>").appendTo(ul)
        };
        this.autoCompleteRendered = true;
        $("#img" + imgRandomID).remove();
        $(this).autocomplete("search")
    });
    $("#lstItems").val("")
}

function activateUserAutoComplete(roleId) {
    $("#lstUsers").focusin(function() {
        if (this.autoCompleteRendered != true) {
            var imgRandomID = new Date().getTime();
            $(this).after("<img id='img" + imgRandomID + "' src='../images/ajax-loader.gif' alt='spinner' style='position:absolute;top:" + $(this).offset().top + "px;left:" + $(this).offset().left + "px' />");
            var dataSource = users_GetNotInRole(roleId),
                minChars = 0;
            if (dataSource.length > MAX_AUTOCOMPLETE_ITEMS) minChars = 2;
            $(this).autocomplete({
                delay: 0,
                minLength: minChars,
                source: dataSource,
                focus: function(event, ui) {
                    $(this).val(ui.item.value);
                    return false
                },
                select: function(event, ui) {
                    $(this).val(ui.item.value);
                    $(this.id + "-id").val(ui.item.key);
                    onUserSelected(ui.item.key);
                    return false
                }
            }).data("autocomplete")._renderItem = function(ul, item) {
                if ($(ul).children().length == 0) {
                    var obj = {};
                    obj.value = "[Enter New]";
                    obj.key = 0;
                    $("<li class='ui-widget'></li>").data("item.autocomplete", obj).append("<a>" + obj.value + "</a>").appendTo(ul)
                }
                return $("<li class='ui-widget'></li>").data("item.autocomplete", item).append("<a>" + item.value + "</a>").appendTo(ul)
            };
            this.autoCompleteRendered = true;
            $("#img" + imgRandomID).remove()
        }
        $(this).autocomplete("search")
    })
}

function activateReportAutoComplete(roleId) {
    $("#lstReports").focusin(function() {
        if (this.autoCompleteRendered != true) {
            var imgRandomID = new Date().getTime();
            $(this).after("<img id='img" + imgRandomID + "' src='../images/ajax-loader.gif' alt='spinner' style='position:absolute;top:" + $(this).offset().top + "px;left:" + $(this).offset().left + "px' />");
            var dataSource = reports_GetNotInRole(roleId),
                minChars = 0;
            if (dataSource.length > MAX_AUTOCOMPLETE_ITEMS) minChars = 2;
            $(this).autocomplete({
                delay: 0,
                minLength: minChars,
                source: dataSource,
                focus: function(event, ui) {
                    $(this).val(ui.item.value);
                    return false
                },
                select: function(event, ui) {
                    $(this).val(ui.item.value);
                    $(this.id + "-id").val(ui.item.key);
                    onReportSelected(ui.item.key);
                    return false
                }
            }).data("autocomplete")._renderItem = function(ul, item) {
                return $("<li class='ui-widget'></li>").data("item.autocomplete", item).append("<a>" + item.value + "</a>").appendTo(ul)
            };
            this.autoCompleteRendered = true;
            $("#img" + imgRandomID).remove()
        }
        $(this).autocomplete("search")
    })
}

function activatePickListParentAutoComplete(childPickListId) {
    $("#lstPickLists").focusin(function() {
        if (this.autoCompleteRendered != true) {
            var imgRandomID = new Date().getTime();
            $(this).after("<img id='img" + imgRandomID + "' src='../images/ajax-loader.gif' alt='spinner' style='position:absolute;top:" + $(this).offset().top + "px;left:" + $(this).offset().left + "px' />");
            var dataSource = pickLists_GetDictionary(USER_CURRENT.Id),
                minChars = 0;
            if (dataSource.length > MAX_AUTOCOMPLETE_ITEMS) minChars = 2;
            $(this).autocomplete({
                delay: 0,
                minLength: minChars,
                source: dataSource,
                focus: function(event, ui) {
                    $(this).val(ui.item.value);
                    return false
                },
                select: function(event, ui) {
                    $(this).val(ui.item.value);
                    $(this.id + "-id").val(ui.item.key);
                    onParentPickListSelected(ui.item.key);
                    return false
                }
            }).data("autocomplete")._renderItem = function(ul, item) {
                return $("<li class='ui-widget'></li>").data("item.autocomplete", item).append("<a>" + item.value + "</a>").appendTo(ul)
            };
            this.autoCompleteRendered = true;
            $("#img" + imgRandomID).remove()
        }
        $(this).autocomplete("search")
    })
}

function onItemSelected(itemType, itemId) {
    SELECTED_ITEM_TYPE = itemType;
    clearForm(itemType);
    if (itemId != 0) {
        var item = getItem(itemType, itemId);
        SELECTED_ITEM = item;
        itemSelected(item !== null);
        if (item !== null) displayItem(itemType, item)
    } else createItem()
}

function displayItem(itemTypeId, item) {
    var displayed = false;
    switch (itemTypeId) {
        case ITEM_TYPE_ASSET_TYPE:
            display = displayAssetType(item);
            break;
        case ITEM_TYPE_PICKLIST:
            displayed = displayPickList(item);
            break;
        case ITEM_TYPE_PROPERTY:
            clearForm_Property();
            displayed = displayProperty(item);
            break;
        case ITEM_TYPE_PROPERTY_GROUP:
            displayed = displayPropertyGroup(item);
            break;
        case ITEM_TYPE_VIEW:
            displayed = displayView(item);
            break;
        case ITEM_TYPE_ROLE:
            displayed = displayRole(item);
            break;
        case ITEM_TYPE_SVC_MODULE:
            displayed = displayServiceModule(item);
            break;
        case ITEM_TYPE_REPORT:
            displayed = displayReport(item);
            break;
        case ITEM_TYPE_HELP_SECTION:
            displayed = displayHelpSection(item);
            break;
        case ITEM_TYPE_CHART:
            displayed = displayChart(item);
            break
    }
    return displayed
}

function getItems(itemTypeId, includeDeletedItems) {
    if (includeDeletedItems === null || includeDeletedItems == undefined) includeDeletedItems = false;
    var items = null;
    switch (itemTypeId) {
        case ITEM_TYPE_ASSET_TYPE:
            items = assetTypes_GetAll();
            break;
        case ITEM_TYPE_PICKLIST:
            items = pickLists_GetDictionary(USER_CURRENT.Id, includeDeletedItems);
            break;
        case ITEM_TYPE_PROPERTY:
            items = properties_GetDictionary(USER_CURRENT.Id, includeDeletedItems);
            break;
        case ITEM_TYPE_PROPERTY_GROUP:
            items = propertyGroups_GetDictionary(USER_CURRENT.Id, includeDeletedItems);
            break;
        case ITEM_TYPE_VIEW:
            items = views_GetDictionary(includeDeletedItems, true);
            break;
        case ITEM_TYPE_ROLE:
            items = roles_GetDictionary(USER_CURRENT.Id, includeDeletedItems);
            break;
        case ITEM_TYPE_SVC_MODULE:
            items = serviceModules_GetDictionary();
            break;
        case ITEM_TYPE_REPORT:
            items = reports_GetForBuilder(USER_CURRENT.Id);
            break;
        case ITEM_TYPE_HELP_SECTION:
            items = helpSections_GetDictionary(USER_CURRENT.Id);
            break;
        case ITEM_TYPE_CHART:
            items = charts_Get(USER_CURRENT.Id);
            break
    }
    return items
}

function getItem(itemTypeId, itemId) {
    var item = null;
    switch (itemTypeId) {
        case ITEM_TYPE_ASSET_TYPE:
            item = assetType_Get(itemId, false);
            break;
        case ITEM_TYPE_PICKLIST:
            item = pickList_Get(itemId);
            break;
        case ITEM_TYPE_PROPERTY:
            item = property_Get(itemId);
            break;
        case ITEM_TYPE_PROPERTY_GROUP:
            item = propertyGroup_Get(itemId);
            break;
        case ITEM_TYPE_VIEW:
            item = view_Get(itemId, USER_CURRENT.Id);
            break;
        case ITEM_TYPE_ROLE:
            item = role_Get(itemId);
            break;
        case ITEM_TYPE_SVC_MODULE:
            item = serviceModule_Get(itemId);
            break;
        case ITEM_TYPE_REPORT:
            item = report_Get(USER_CURRENT.Id, itemId);
            break;
        case ITEM_TYPE_HELP_SECTION:
            item = helpSection_Get(itemId);
            break;
        case ITEM_TYPE_CHART:
            item = chartDef_Get(itemId);
            break
    }
    return item
}

function clearForm(itemType) {
    var cleared = false;
    switch (itemType) {
        case ITEM_TYPE_ASSET_TYPE:
            cleared = clearForm_AssetType();
            break;
        case ITEM_TYPE_PICKLIST:
            cleared = clearForm_PickList();
            break;
        case ITEM_TYPE_PROPERTY:
            cleared = clearForm_Property();
            break;
        case ITEM_TYPE_PROPERTY_GROUP:
            cleared = clearForm_PropertyGroup();
            break;
        case ITEM_TYPE_VIEW:
            cleared = clearForm_View();
            break;
        case ITEM_TYPE_ROLE:
            cleared = clearForm_Role();
            break;
        case ITEM_TYPE_REPORT:
            cleared = clearForm_Report();
            break;
        case ITEM_TYPE_HELP_SECTION:
            cleared = clearForm_HelpSection();
            break
    }
    return cleared
}

function onDiscardChanges() {
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html("This action cannot be undone.");
    $dialog.dialog({
        width: 350,
        height: "auto",
        modal: true,
        autoOpen: false,
        resizable: false,
        draggable: false,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        title: "Discard Changes?",
        buttons: {
            No: function() {
                $(this).dialog("close")
            },
            Yes: function() {
                SELECTED_ITEM = getItem(SELECTED_ITEM_TYPE, SELECTED_ITEM.Id);
                if (SELECTED_ITEM === null) clearForm(SELECTED_ITEM_TYPE);
                else {
                    displayItem(SELECTED_ITEM_TYPE, SELECTED_ITEM);
                    itemModified(false)
                }
                $(this).dialog("close")
            }
        }
    });
    $dialog.dialog("open")
}

function onMigrateClicked() {
    var html = [];
    html.push('<select id="lstAppInstances" style="width:100%"><option value="-1">[ Select ]</option>');
    $.each(applicationInstances(), function(index, item) {
        if (item.value != CURRENT_NODE) html.push('<option value="' + item.key + '">' + item.value + "</option>")
    });
    html.push("</select>");
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html(html.join(""));
    $dialog.dialog({
        width: 350,
        height: "auto",
        modal: true,
        autoOpen: false,
        title: "Choose Target Application Instance",
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            Cancel: function() {
                $(this).dialog("close")
            },
            Ok: function() {
                var key = $("#lstAppInstances").val(),
                    val = $("#lstAppInstances option:selected").text();
                $(this).dialog("close");
                onMigrateSelected(key, val)
            }
        }
    });
    $dialog.dialog("open")
}

function onMigrateSelected(targetKey, targetValue) {
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    if (SELECTED_ITEM_TYPE == ITEM_TYPE_REPORT) $dialog.html("Migrate <i>" + SELECTED_ITEM.ReportName + "</i> to " + targetValue + "?");
    else $dialog.html("Migrate <i>" + SELECTED_ITEM.Name + "</i> to " + targetValue + "?");
    $dialog.dialog({
        width: 350,
        height: "auto",
        modal: true,
        autoOpen: false,
        title: "Confirm Migration",
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            No: function() {
                $(this).dialog("close")
            },
            Yes: function() {
                $(this).dialog("close");
                var migrated = false;
                switch (SELECTED_ITEM_TYPE) {
                    case ITEM_TYPE_ASSET_TYPE:
                        migrated = assetType_Migrate(SELECTED_ITEM.Id, targetKey);
                        break;
                    case ITEM_TYPE_PICKLIST:
                        migrated = pickList_Migrate(SELECTED_ITEM.Id, targetKey);
                        break;
                    case ITEM_TYPE_PROPERTY:
                        migrated = property_Migrate(SELECTED_ITEM.Id, targetKey);
                        break;
                    case ITEM_TYPE_PROPERTY_GROUP:
                        migrated = propertyGroup_Migrate(SELECTED_ITEM.Id, targetKey);
                        break;
                    case ITEM_TYPE_REPORT:
                        migrated = report_Migrate(SELECTED_ITEM.Id, targetKey);
                        break;
                    case ITEM_TYPE_VIEW:
                        migrated = view_Migrate(SELECTED_ITEM.Id, targetKey);
                        break
                }
                if (migrated) showMessage("Result", "Item migrated successfully.", true, 400, true)
            }
        }
    });
    $dialog.dialog("open")
}

function onReportSelected(reportId) {
    toggleButtons("addReport", true);
    toggleButtons("removeReport", false)
}

function onUserSelected(userId) {
    if (userId == "0") onAddNewUser();
    else {
        toggleButtons("addUser", true);
        toggleButtons("removeUser", false)
    }
}

function itemSelected(wasSelected) {
    toggleButtons("migrateButtons", wasSelected);
    toggleButtons("deleteButtons", wasSelected);
    toggleButtons("resetButtons", !wasSelected);
    toggleButtons("submitButtons", !wasSelected)
}

function itemModified(wasModified) {
    toggleButtons("resetButtons", wasModified && !SELECTED_ITEM.IsNew);
    toggleButtons("submitButtons", wasModified)
}

function getActionDateTime(actionDateTime) {
    var returnValue = null;
    if (actionDateTime !== null && actionDateTime != undefined && actionDateTime != "") {
        returnValue = new Date(parseInt(actionDateTime.substr(6)));
        returnValue = returnValue.toString()
    }
    return returnValue
}

function getActionCreator(actionPerformedBy) {
    var returnValue = null;
    if (actionPerformedBy !== null && actionPerformedBy != undefined && actionPerformedBy != "") returnValue = user_GetDisplayName(actionPerformedBy.toString());
    return returnValue
}
