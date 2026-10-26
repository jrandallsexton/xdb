var ttId = "The internal id of the picklist.<br/><br/>This value is constant, never changed, and cannot be changed.",
    ttName = "The name of the picklist as it is to be known throughout the system.<br/><br/>It may be changed freely",
    ttDisplay = "The user-friendly name of the picklist as it will appear in other locations (i.e. within the Properties admin page)<br/><br/>It may be changed freely",
    ttDesc = "Short summary of the types of values the picklist contains.<br/><br/>This value may be changed without affecting other system functions.",
    ttValue = "Name of the value to appear in the picklist",
    ttDisplayValue = "Display value (if differenct) from the value (optional).  Used to allow a code/text combination, ex: 1, High; 2, Medium; 3, Low",
    ttParent = "",
    ttCreated = "When the picklist was created",
    ttCreatedBy = "Who created the picklist",
    ttModified = "When the picklist was last modified",
    ttModifiedBy = "Who last modified the picklist",
    ttDeleted = "When the picklist was deleted",
    ttDeletedBy = "Who deleted the picklist",
    _imgSrc = "../images";

function clearForm_PickList() {
    $("#txtName").val("");
    $("#txtDisplayValue").val("");
    $("#txtDescription").val("");
    $("#txtNewValue").val("");
    $("#txtNewDisplayValue").val("");
    $("#values").html("");
    return true
}

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
    activateAutoCompletes(ITEM_TYPE_PICKLIST);
    $("#lstItems").focus();
    $("#lstItems").autocomplete("close");
    var itemId = getParameterByName("itemId");
    if (itemId !== null && itemId != undefined && itemId != "") {
        SELECTED_ITEM = pickList_Get(itemId);
        SELECTED_ITEM_TYPE = ITEM_TYPE_PICKLIST;
        displayPickList(SELECTED_ITEM);
        $("#lstItems").val(SELECTED_ITEM.Name);
        itemSelected(true)
    }
    checkBox_SetChecked("chkDeletedItems", false);
    $("#accordion").accordion({
        autoHeight: false,
        collapsible: true,
        icons: {
            header: null,
            headerSelected: null
        }
    });
    $(".sortable").sortable({
        revert: true,
        stop: function(event, ui) {
            onPickListValueDropped(ui)
        }
    });
    $(".draggable").draggable({
        connectToSortable: ".sortable",
        helper: "clone",
        revert: "invalid"
    });
    $("ul, li").disableSelection()
}

function displayConsumers(pickList) {
    var html = [];
    $.each(properties_GetByPickListId(pickList.Id), function(index, item) {
        html.push("<a href='" + URL_CLIENT_UI + "/admin/properties.html?itemId=" + item.key + "' target='_blank'>" + item.value + "</a><br/>")
    });
    $("#divConsumers").html(html.join(""))
}

function displayPickList(pickList) {
    $("#lblId").text(pickList.Id);
    $("#txtName").val(pickList.Name);
    $("#txtDisplayValue").val(pickList.DisplayValue);
    $("#txtDescription").val(pickList.Description);
    displayPickListValues(pickList);
    displayConsumers(pickList);
    $("#tdCreated").html(getActionDateTime(pickList.Created));
    $("#tdCreatedBy").html(getActionCreator(pickList.CreatedBy));
    $("#tdLastModified").html(getActionDateTime(pickList.LastModified));
    $("#tdLastModifiedBy").html(getActionCreator(pickList.LastModifiedBy));
    if (pickList.Deleted !== null && pickList.Deleted != undefined && pickList.Deleted != "") {
        $("#tdDeleted").html(getActionDateTime(pickList.Deleted));
        $("#tdDeletedBy").html(getActionCreator(pickList.DeletedBy));
        $("#trDeleted").show();
        $("#trDeletedBy").show();
        $("#btnDelete").val("Undelete Picklist");
        $("#btnDelete").attr("onClick", "onPickListUndeleteClicked();")
    } else {
        $("#trDeleted").hide();
        $("#trDeletedBy").hide();
        $("#btnDelete").val("Delete Picklist");
        $("#btnDelete").attr("onClick", "onPickListDeleteClicked();")
    }
    activatePickListParentAutoComplete(pickList.Id);
    displayPickListDependents(pickList, null);
    $("#tblButtons").show();
    toggleButtons("deleteButtons", true);
    $("#accordion").show();
    return true
}

function displayPickListValues(pickList) {
    if (pickList.PickListValues !== null) {
        var html = [];
        $.each(pickList.PickListValues, function(index, item) {
            if (item.Deleted == null) {
                if (item.DisplayValue != "") html.push("<li id='" + item.Id + "' class='ui-state-default'>" + item.Order + ". " + item.Value + " (" + item.DisplayValue + ")");
                else html.push("<li id='" + item.Id + "' class='ui-state-default'>" + item.Order + ". " + item.Value);
                html.push("<div style='float:right'>");
                html.push("<img src='../images/delete.png' onClick='onRemovePickListValueClicked(\"" + item.Id + "\")'>");
                html.push("</div></li>")
            }
        });
        $("#values").html(html.join(""))
    } else $("#values").html("")
}

function displayPickListDependents(pickList, parentPickListId) {
    if (pickList.Dependencies === null || pickList.Dependencies == undefined || pickList.Dependencies.length == 0 && parentPickListId === null) {
        $("#lstPickLists").val("[ Select ]");
        $("#lstPickLists-id").val("-1");
        if ($("#divPLTabs").is(":ui-tabs")) $("#divPLTabs").tabs("destroy");
        $("#divPLTabs").html("");
        return
    }
    if (parentPickListId === null) parentPickListId = pickList.Dependencies[0].ParentPickListId;
    var parentPickList = pickList_Get(parentPickListId);
    $("#lstPickLists").val(parentPickList.DisplayValue);
    $("#lstPickLists-id").val(parentPickListId);
    var ulHtml = [],
        divHtml = [];
    ulHtml.push("<ul>");
    $.each(parentPickList.PickListValues, function(index, item) {
        var valueToShow = item.DisplayValue;
        if (valueToShow === null || valueToShow == undefined || valueToShow == "") valueToShow = item.Value;
        ulHtml.push('<li><a href="#tabs-' + index.toString() + '">' + valueToShow + "</a></li>");
        divHtml.push('<div id="tabs-' + index.toString() + '">');
        $.each(pickList.PickListValues, function(subIndex, subItem) {
            var subValueToShow = subItem.DisplayValue;
            if (subValueToShow === null || subValueToShow == undefined || subValueToShow == "") subValueToShow = subItem.Value;
            var checked = pickListDependencyExists(pickList.Dependencies, item.Id, subItem.Id),
                tempHtml = null,
                chkBoxId = item.Id + "_" + subItem.Id;
            if (checked) tempHtml = '<label><input type="checkbox" id="' + chkBoxId + '" style="float:none;" class="ui-widget ui-widget-content" checked=true onChange="onDependencyCheckedChanged(this.id)" />' + subValueToShow + "</label><br/>";
            else tempHtml = '<label><input type="checkbox" id="' + chkBoxId + '" style="float:none;" class="ui-widget ui-widget-content"  onChange="onDependencyCheckedChanged(this.id)" />' + subValueToShow + "</label><br/>";
            divHtml.push(tempHtml)
        });
        divHtml.push("</div>")
    });
    ulHtml.push("</ul>");
    $("#divPLTabs").tabs("destroy");
    $("#divPLTabs").html(ulHtml.join("") + divHtml.join(""));
    $("#divPLTabs").tabs()
}

function onDependencyCheckedChanged(checkBoxId) {
    var isChecked = checkBox_GetChecked(checkBoxId),
        ids = checkBoxId.split("_"),
        exists = pickListDependencyExists(SELECTED_ITEM.Dependencies, ids[0], ids[1]),
        modified = false;
    if (isChecked)
        if (exists) {} else {
            var parentPickListId = $("#lstPickLists-id").val(),
                parentPickListValueId = ids[0],
                childPickListId = SELECTED_ITEM.Id,
                childPickListValueId = ids[1],
                pld = pickListDependency_Create(parentPickListId, parentPickListValueId, childPickListId, childPickListValueId);
            SELECTED_ITEM.Dependencies.push(pld);
            modified = true
        }
    else if (exists) {
        pickListDependency_Delete(SELECTED_ITEM.Dependencies, ids[0], ids[1]);
        modified = true
    } else {}
    if (modified) {
        SELECTED_ITEM.IsDirty = true;
        itemModified(true)
    }
}

function pickListDependencyExists(dependencyList, parentPickListValueId, childPickListValueId) {
    var found = false;
    $.each(dependencyList, function(index, item) {
        if (item.ParentPickListValueId.toLowerCase() == parentPickListValueId.toLowerCase() && item.ChildPickListValueId.toLowerCase() == childPickListValueId.toLowerCase()) {
            found = true;
            return false
        }
    });
    return found
}

function pickListDependency_Create(parentPickListId, parentPickListValueId, childPickListId, childPickListValueId) {
    var pld = {};
    pld.Id = generateGuid(null);
    pld.ParentPickListId = parentPickListId;
    pld.ParentPickListValueId = parentPickListValueId;
    pld.ChildPickListId = childPickListId;
    pld.ChildPickListValueId = childPickListValueId;
    pld.Created = getServerDateTime();
    pld.CreatedBy = USER_CURRENT.Id;
    pld.Deleted = null;
    pld.DeletedBy = null;
    return pld
}

function pickListDependency_Delete(dependencyList, parentPickListValueId, childPickListValueId) {
    var found = false,
        intLoc = -1;
    $.each(dependencyList, function(index, item) {
        if (item.ParentPickListValueId.toLowerCase() == parentPickListValueId.toLowerCase() && item.ChildPickListValueId.toLowerCase() == childPickListValueId.toLowerCase()) {
            found = true;
            intLoc = index;
            return false
        }
    });
    if (found) {
        dependencyList[intLoc].Deleted = getServerDateTime();
        dependencyList[intLoc].DeletedBy = USER_CURRENT.Id
    }
    return found
}

function showColorPicker(id) {
    $("#" + id).ColorPicker()
}

function onAddNewPickListValueClicked() {
    var $txtVal = $("#txtNewValue"),
        $txtDis = $("#txtNewDisplayValue"),
        value = $txtVal.val(),
        display = $txtDis.val();
    if (isValid(value, display)) {
        var newPLV = {};
        newPLV.Id = generateGuid(null);
        newPLV.PickListId = SELECTED_ITEM.Id;
        newPLV.Created = getServerDateTime();
        newPLV.CreatedBy = USER_CURRENT.Id;
        newPLV.Approved = newPLV.Created;
        newPLV.ApprovedBy = USER_CURRENT.Id;
        newPLV.BGColor = null;
        newPLV.LastModified = null;
        newPLV.LastModifiedBy = null;
        newPLV.Deleted = null;
        newPLV.DeletedBy = null;
        newPLV.Value = value;
        newPLV.DisplayValue = display;
        newPLV.Order = SELECTED_ITEM.PickListValues.length;
        newPLV.IsNew = true;
        newPLV.IsDirty = true;
        SELECTED_ITEM.PickListValues.push(newPLV);
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
        }
        displayPickListValues(SELECTED_ITEM);
        toggleControl("btnAddPickListValue", false, true);
        itemModified(true);
        $txtVal.val("");
        $txtDis.val("");
        $txtVal.focus()
    }
}

function isValid(value, displayValue) {
    var valueIsValid = value !== null && value != undefined && value != "",
        displayValueIsValid = displayValue !== null && displayValue != undefined && displayValue != "";
    if (!valueIsValid && !displayValueIsValid) return false;
    var isValid = true;
    $.each(SELECTED_ITEM.PickListValues, function(index, item) {
        if (item.Deleted == null) {
            if (item.Value.toLowerCase() == value.toLowerCase()) {
                showMessage("Duplicate Value", 'The value "' + value + '" already exists.', true, 400, true);
                isValid = false;
                return false
            }
            if (item.DisplayValue !== null && item.DisplayValue != undefined && item.DisplayValue != "")
                if (item.DisplayValue.toLowerCase() == displayValue.toLowerCase()) {
                    showMessage("Duplicate Display Value", 'The display value "' + displayValue + '" already exists.', true, 400, true);
                    isValid = false;
                    return false
                }
        }
    });
    return isValid
}

function onDeletedItemsChanged() {
    activateAutoCompletes(ITEM_TYPE_PICKLIST, checkBox_GetChecked("chkDeletedItems"))
}

function onDescriptionChanged() {
    var newValue = $("#txtDescription").val();
    if (newValue != SELECTED_ITEM.Description) {
        SELECTED_ITEM.Description = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
        }
        itemModified(true)
    }
}

function onDisplayValueChanged() {
    var newValue = $("#txtDisplayValue").val();
    if (newValue != SELECTED_ITEM.DisplayValue) {
        SELECTED_ITEM.DisplayValue = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
        }
        itemModified(true)
    }
}

function onNameChanged() {
    var newValue = $("#txtName").val();
    if (newValue != SELECTED_ITEM.Name) {
        SELECTED_ITEM.Name = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
        }
        itemModified(true)
    }
}

function onNewPickListValueChanged() {
    var value = $("#txtNewValue").val(),
        display = $("#txtNewDisplayValue").val(),
        valueIsValid = value !== null && value != undefined && value != "",
        displayValueIsValid = display !== null && display != undefined && display != "",
        enableAddButton = valueIsValid || displayValueIsValid;
    toggleControl("btnAddPickListValue", enableAddButton, true)
}

function onParentPickListSelected(parentPickListId) {
    displayPickListDependents(SELECTED_ITEM, parentPickListId)
}

function onPickListDeleteClicked() {
    var consumers = properties_GetByPickListId(SELECTED_ITEM.Id);
    if (consumers.length == 0) {
        var $dialog = $("#messageDialog");
        $dialog.dialog("destroy");
        $dialog.html("Delete the '" + SELECTED_ITEM.Name + "' picklist?<br/><br/>This action can be undone if the picklist is needed later.");
        $dialog.dialog({
            width: 350,
            height: "auto",
            modal: true,
            autoOpen: false,
            title: "Confirm Action",
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            buttons: {
                No: function() {
                    $(this).dialog("close")
                },
                Yes: function() {
                    var result = null;
                    SELECTED_ITEM.Deleted = getServerDateTime();
                    SELECTED_ITEM.DeletedBy = USER_CURRENT.Id;
                    SELECTED_ITEM.LastModified = getServerDateTime();
                    SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id;
                    if (pickList_Save(SELECTED_ITEM)) {
                        displayPickList(SELECTED_ITEM);
                        $("#btnDelete").val("Undelete Picklist");
                        $("#btnDelete").attr("onClick", "onPickListUndeleteClicked();");
                        result = "The picklist '" + SELECTED_ITEM.Name + "' has been deleted from the system.<br/>It can be undeleted at a later time if needed."
                    } else result = "Failed to delete the '" + SELECTED_ITEM.Name + "' picklist from the system.";
                    $(this).dialog("close");
                    showMessage("Result", result, true, 400, true)
                }
            }
        });
        $dialog.dialog("open")
    } else {
        var consumerLabel = "properties";
        if (consumers.length == 1) consumerLabel = "property";
        var msg = "This picklist is being used by " + consumers.length + " " + consumerLabel + ".<br/><br/>See 'Usage by Properties' (on this page) for details.";
        showMessage("Cannot Delete PickList", msg, true, 400, true);
        $("#accordion").accordion("option", "active", 3)
    }
}

function onPickListSaveClicked() {
    var wasNewItem = SELECTED_ITEM.IsNew,
        saved = pickList_Save(SELECTED_ITEM),
        msg = null;
    if (saved) {
        SELECTED_ITEM = pickList_Get(SELECTED_ITEM.Id);
        clearForm_PickList();
        displayPickList(SELECTED_ITEM);
        if (wasNewItem) {
            autoComplete_SwapDatasourceAsIs(pickLists_GetDictionary(USER_CURRENT.Id), "lstItems");
            $("#lstItems").val(SELECTED_ITEM.Name);
            $("#lstItems-id").val(SELECTED_ITEM.Id)
        }
        toggleControl("btnReset", false, true);
        toggleControl("btnSubmit", false, true);
        toggleButtons("resetButtons", false);
        toggleButtons("submitButtons", false);
        msg = "Picklist saved successfully."
    } else msg = "An error occured while saving the picklist.";
    showMessage("Result", msg, true, 400, true)
}

function onPickListSelected(pickListId) {
    var pickList = pickList_Get(pickListId);
    clearForm();
    if (pickList !== null) {
        displayPickList(pickList);
        itemSelected(true);
        $("#accordion").show()
    } else itemSelected(false)
}

function onPickListUndeleteClicked() {
    SELECTED_ITEM.Deleted = null;
    SELECTED_ITEM.DeletedBy = null;
    SELECTED_ITEM.LastModified = getServerDateTime();
    SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id;
    itemModified(true)
}

function onPickListValueDropped(ui) {
    reorderPropertyValues();
    itemModified(true);
    return true
}

function pickListValue_Get(pickListValueId) {
    pickListValueId = pickListValueId.toLowerCase();
    var pickListValue = null;
    $.each(SELECTED_ITEM.PickListValues, function(index, item) {
        if (item.Id.toLowerCase() == pickListValueId) {
            pickListValue = item;
            return false
        }
    });
    return pickListValue
}

function pickListValue_Kill(pickListValueId) {
    pickListValueId = pickListValueId.toLowerCase();
    var found = false,
        loc = 0;
    $.each(SELECTED_ITEM.PickListValues, function(index, item) {
        if (item.Id.toLowerCase() == pickListValueId) {
            found = true;
            loc = index;
            return false
        }
    });
    if (found) SELECTED_ITEM.PickListValues.splice(loc, 1)
}

function pickListValues_Reindex(pickList) {
    var newIndex = 0;
    $.each(pickList.PickListValues, function(index, item) {
        if (item.Deleted == null || item.Deleted == undefined || item.Deleted == "") {
            item.Order = newIndex;
            newIndex++
        }
    })
}

function onAutoSort() {
    toggleControl("btnSort", false, true);
    var sortedValues = [],
        itemNames = [];
    $.each(SELECTED_ITEM.PickListValues, function(index, item) {
        if (item.Deleted == null) {
            var itemName = null;
            if (item.DisplayValue != null && item.DisplayValue != undefined && item.DisplayValue != "") {
                itemName = item.DisplayValue;
                sortedValues[item.DisplayValue] = item
            } else {
                itemName = item.Value;
                sortedValues[item.Value] = item
            }
            itemNames.push(itemName)
        }
    });
    itemNames.sort();
    var lastMod = getServerDateTime(),
        length = itemNames.length;
    for (var i = 0; i < length; i++) {
        var itemName = itemNames[i],
            val = sortedValues[itemName];
        val.Order = i;
        if (!val.IsNew) {
            val.IsDirty = true;
            val.LastModified = lastMod;
            val.LastModifiedBy = USER_CURRENT.Id
        }
    }
    displayPickListValues(SELECTED_ITEM);
    itemModified(true);
    toggleControl("btnSort", true, true)
}

function onRemovePickListValueClicked(pickListValueId) {
    var validSelection = pickListValueId !== null && pickListValueId != undefined && pickListValueId != "";
    if (validSelection) {
        var pickListValue = pickListValue_Get(pickListValueId);
        if (pickListValue == null) return;
        var valueToShow = pickListValue.DisplayValue;
        if (valueToShow == "") valueToShow = pickListValue.Value;
        var $dialog = $("#messageDialog");
        $dialog.dialog("destroy");
        $dialog.html("Remove the '" + valueToShow + "' picklist value from this picklist?");
        $dialog.dialog({
            width: 350,
            height: "auto",
            modal: true,
            autoOpen: false,
            title: "Confirm Action",
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            buttons: {
                No: function() {
                    $(this).dialog("close")
                },
                Yes: function() {
                    if (SELECTED_ITEM.IsNew) pickListValue_Kill(pickListValueId);
                    else {
                        pickListValue.Deleted = getServerDateTime();
                        pickListValue.DeletedBy = USER_CURRENT.Id;
                        SELECTED_ITEM.LastModified = getServerDateTime();
                        SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
                    }
                    displayPickListValues(SELECTED_ITEM);
                    reorderPropertyValues();
                    itemModified(true);
                    var result = "Picklist value '" + valueToShow + "' removed from the picklist.<br/><br/>Picklist must be saved to make this change effective.";
                    showMessage("Result", result, true, 400, true)
                }
            }
        });
        $dialog.dialog("open")
    }
}

function createItem() {
    SELECTED_ITEM = pickList_Create(USER_CURRENT.Id);
    displayPickList(SELECTED_ITEM)
}

function getPropertyValue(id) {
    id = id.toLowerCase();
    var value = null,
        location = -1;
    $.each(SELECTED_ITEM.PickListValues, function(index, item) {
        if (item.Id.toLowerCase() == id) {
            location = index;
            return false
        }
    });
    if (location != -1) value = SELECTED_ITEM.PickListValues[location];
    return value
}

function reorderPropertyValues() {
    var index = 0,
        pickListModified = false,
        lastMod = getServerDateTime(),
        userId = USER_CURRENT.Id;
    $("#values > li").each(function() {
        var tempVal = this.id;
        if (tempVal != "0" && tempVal != "-1") {
            var propVal = getPropertyValue(tempVal);
            if (propVal != null)
                if (propVal.Order != index) {
                    propVal.Order = index;
                    propVal.IsDirty = true;
                    pickListModified = true;
                    if (!propVal.IsNew) {
                        propVal.LastModified = lastMod;
                        propVal.LastModifiedBy = userId
                    }
                }
        }
        index++
    });
    if (pickListModified) {
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = lastMod;
            SELECTED_ITEM.LastModifiedBy = userId
        }
        itemModified(true)
    }
}
