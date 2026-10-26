var ttId = "The internal id of the property group.<br/><br/>This value is constant, never changed, and cannot be changed.",
    ttName = "The name of the property group is an internal identifier available only to admins.  This value is never shown to users.<br/><br/>It may be changed freely.",
    ttDisplayValue = "The friendly, user-facing name of the property group as it appears in views displayed within Asset Editor.<br/><br/>This value may be changed without affecting other system functions.",
    ttAssetType = "The type of asset this property group will be working with.<br/><br/>The asset type controls which properties will be available to the property group.",
    ttIsInstance = "",
    ttCreated = "When the property group was created",
    ttCreatedBy = "Who created the property group",
    ttModified = "When the property group was last modified",
    ttModifiedBy = "Who last modified the property group",
    ttDeleted = "When the property group was deleted",
    ttDeletedBy = "Who deleted the property group",
    _imgSrc = "../images";

function clearForm_PropertyGroup() {
    $("#txtName").val("");
    $("#txtDisplayValue").val("");
    $("#lstAssetTypes").html("");
    checkBox_SetChecked("chkIsInstance", false);
    $("#tdCreated").html("");
    $("#tdCreatedBy").html("");
    $("#tdLastModified").html("");
    $("#tdLastModifiedBy").html("");
    $("#tdDeleted").html("");
    $("#tdDeletedBy").html("");
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
    activateAutoCompletes(ITEM_TYPE_PROPERTY_GROUP);
    var itemId = getParameterByName("itemId");
    if (itemId !== null && itemId != undefined && itemId != "") {
        SELECTED_ITEM = propertyGroup_Get(itemId);
        SELECTED_ITEM_TYPE = ITEM_TYPE_PROPERTY_GROUP;
        displayPropertyGroup(SELECTED_ITEM);
        $("#lstItems").val(SELECTED_ITEM.Name);
        itemSelected(true)
    } else {
        $("#lstItems").focus();
        $("#lstItems").autocomplete("close")
    }
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
            onPropertyDropped(ui)
        }
    });
    $(".draggable").draggable({
        connectToSortable: ".sortable",
        helper: "clone",
        revert: "invalid"
    });
    $("ul, li").disableSelection()
}

function displayPropertyGroup(propertyGroup) {
    $("#lblId").text(propertyGroup.Id);
    $("#txtName").val(propertyGroup.Name);
    $("#txtDisplayValue").val(propertyGroup.DisplayValue);
    var html = [];
    html.push("<option value='-1'>[Select]</option>");
    $.each(assetTypes_GetAll(), function(index, item) {
        html.push("<option value='" + item.key + "'>" + item.value + "</option>")
    });
    $("#lstAssetTypes").html(html.join(""));
    if (propertyGroup.AssetTypeId != NULL_GUID) {
        $("#lstAssetTypes").val(propertyGroup.AssetTypeId);
        $("#lstAssetTypes").attr("disabled", "disabled");
        checkBox_SetChecked("chkIsInstance", propertyGroup.IsInstance)
    } else {
        $("#lstAssetTypes").val("-1");
        $("#lstAssetTypes").removeAttr("disabled");
        checkBox_SetChecked("chkIsInstance", false)
    }
    displayAllowableProperties(propertyGroup);
    displayProperties(propertyGroup);
    displayConsumers(propertyGroup);
    $("#tdCreated").html(getActionDateTime(propertyGroup.Created));
    $("#tdCreatedBy").html(getActionCreator(propertyGroup.CreatedBy));
    $("#tdLastModified").html(getActionDateTime(propertyGroup.LastModified));
    $("#tdLastModifiedBy").html(getActionCreator(propertyGroup.LastModifiedBy));
    $("#tdDeleted").html(getActionDateTime(propertyGroup.Deleted));
    if (propertyGroup.Deleted !== null && propertyGroup.Deleted != undefined && propertyGroup.Deleted != "") {
        $("#btnDelete").val("Undelete Property Group");
        $("#btnDelete").attr("onClick", "onPropertyGroupUndeleteClicked();")
    } else {
        $("#btnDelete").val("Delete PropertyGroup");
        $("#btnDelete").attr("onClick", "onPropertyGroupDeleteClicked();")
    }
    $("#tdDeletedBy").html(getActionCreator(propertyGroup.DeletedBy));
    $("#tblButtons").show();
    toggleButtons("deleteButtons", !SELECTED_ITEM.IsNew);
    toggleButtons("cloneButtons", !SELECTED_ITEM.IsNew);
    toggleButtons("killButtons", USER_ACTUAL.IsAdmin && !SELECTED_ITEM.IsNew);
    $("#accordion").show();
    return true
}

function displayAllowableProperties(propertyGroup) {
    var html = [];
    if (propertyGroup.AssetTypeId !== null && propertyGroup.AssetTypeId != undefined && propertyGroup.AssetTypeId != "") {
        html.push("<option value='-1' selected='selected'>[Select]</option>");
        var art = null;
        if (SELECTED_ITEM.IsInstance) art = ART_INSTANCE;
        else art = ART_DEFINITION;
        $.each(properties_GetByAssetTypeId(propertyGroup.AssetTypeId, art), function(index, item) {
            if (!propertyGroupContainsProperty(propertyGroup, item.key)) html.push("<option value='" + item.key + "'>" + item.value + "</option>")
        });
        $("#lstProperties").removeAttr("disabled")
    } else {
        html.push("<option value='-1' selected='selected'>[Select]</option>");
        $("#lstProperties").attr("disabled", "disabled")
    }
    $("#lstProperties").html(html.join(""))
}

function displayProperties(propertyGroup) {
    var html = [];
    if (propertyGroup.Properties !== null) $.each(propertyGroup.Properties, function(index, item) {
        var relation = getPropertyRelation(propertyGroup, item.Id);
        if (relation.Deleted == null || relation.Deleted == undefined || relation.Deleted == "")
            if (item.DisplayValue != "") {
                html.push("<li id='" + item.Id + "' class='ui-state-default' style='width:50%'>" + index + ". " + item.Name + " (" + item.DisplayValue + ")");
                html.push("<div style='float:right'>");
                html.push("<span class='ui-icon ui-" + ICON_OPEN_TAB + "' onClick='onPropertyClicked(\"" + item.Id + "\")' title='Open this property in a new tab' style='float:left;margin-right:.2em;'></span>");
                html.push("<input id='chk" + item.Id + "Required' type='checkbox'");
                if (relation.IsRequired) html.push(" checked='checked'");
                html.push(" class='ui-widget' onChange='onPropertyIsRequiredCheckChanged(\"" + item.Id + "\")'>");
                html.push("<label>Is Required?</label></input>");
                html.push("<input id='chk" + item.Id + "ReadOnly' type='checkbox'");
                if (relation.IsReadOnly) html.push(" checked='checked'");
                html.push(" class='ui-widget' onChange='onPropertyIsReadOnlyCheckChanged(\"" + item.Id + "\")'>");
                html.push("<label>Is Read-Only?</label></input>");
                html.push("<span class='ui-icon ui-icon-trash itemReorder' onClick='onRemovePropertyClicked(\"" + item.Id + "\")'/></span>");
                html.push("</li>")
            } else html.push("<li id='" + item.Id + "' class='ui-state-default' style='float:right'>" + index + ". " + item.Name + "</li>")
    });
    $("#values").html(html.join(""))
}

function displayConsumers(propertyGroup) {
    var html = [];
    $.each(views_GetByPropertyGroupId(propertyGroup.Id), function(index, item) {
        html.push("<a href='" + URL_CLIENT_UI + "/admin/views.html?itemId=" + item.key + "' target='_blank'>" + item.value + "</a><br/>")
    });
    $("#divConsumers").html(html.join(""))
}

function onAddPropertyClicked() {
    var propertyId = $("#lstProperties").val();
    if (propertyId !== null && propertyId != undefined && propertyId != "") {
        var relation = {};
        relation.Id = generateGuid(null);
        relation.Created = getServerDateTime();
        relation.CreatedBy = USER_CURRENT.Id;
        relation.Deleted = null;
        relation.DeletedBy = null;
        relation.PropertyGroupId = SELECTED_ITEM.Id;
        relation.PropertyId = propertyId;
        relation.Order = SELECTED_ITEM.PropertyMembers.length;
        relation.DefaultValue = null;
        relation.IsRequired = false;
        relation.IsReadOnly = false;
        relation.IsNew = true;
        relation.IsDirty = true;
        SELECTED_ITEM.Properties.push(property_Get(propertyId));
        SELECTED_ITEM.PropertyMembers.push(relation);
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
        }
        htmlSelect_RemoveOptionByValue("lstProperties", propertyId);
        $("#lstProperties").val(0);
        itemModified(true);
        displayProperties(SELECTED_ITEM);
        $(".sortable").sortable({
            revert: true,
            stop: function(event, ui) {
                onPropertyDropped(ui)
            }
        });
        $(".draggable").draggable({
            connectToSortable: ".sortable",
            helper: "clone",
            revert: "invalid"
        });
        $("ul, li").disableSelection();
        toggleControl("btnAddProperty", false, true)
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

function onAssetTypeChanged() {
    var newValue = $("#lstAssetTypes").val();
    if (newValue != SELECTED_ITEM.AssetTypeId) {
        if (newValue != "-1") SELECTED_ITEM.AssetTypeId = newValue;
        else SELECTED_ITEM.AssetTypeId = null;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
        }
        displayAllowableProperties(SELECTED_ITEM);
        itemModified(true)
    }
}

function onCloneClicked() {
    SELECTED_ITEM = propertyGroup_Clone(SELECTED_ITEM.Id, USER_CURRENT.Id);
    displayPropertyGroup(SELECTED_ITEM)
}

function onDeletedItemsChanged() {
    activateAutoCompletes(ITEM_TYPE_PROPERTY_GROUP, checkBox_GetChecked("chkDeletedItems"))
}

function onIsInstanceChanged() {
    var isChecked = checkBox_GetChecked("chkIsInstance");
    if (isChecked != SELECTED_ITEM.IsInstance) {
        SELECTED_ITEM.IsInstance = isChecked;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
        }
        displayAllowableProperties(SELECTED_ITEM);
        itemModified(true)
    }
}

function onKillClicked() {
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html("Permanently destroy the '" + SELECTED_ITEM.Name + "' property group?<br/>This action can not be undone");
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
                if (propertyGroup_Kill(SELECTED_ITEM.Id, USER_CURRENT.Id)) result = SELECTED_ITEM.Name + " has been permanently deleted from the system";
                else result = "Failed to kill the '" + SELECTED_ITEM.Name + "' property group.";
                $(this).dialog("close");
                showMessage("Result", result, true, 400, true)
            }
        }
    });
    $dialog.dialog("open")
}

function onPropertyClicked(propertyId) {
    var url = URL_CLIENT_UI + "/admin/properties.html?itemId=" + propertyId;
    window.open(url)
}

function onPropertyDropped(ui) {
    reOrderProperties();
    itemModified(true);
    return true
}

function onPropertyGroupDeleteClicked() {
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html("Delete the '" + SELECTED_ITEM.Name + "' property group?<br/><br/>This action can be undone if the property group is needed later.");
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
                SELECTED_ITEM.IsDirty = true;
                if (propertyGroup_Save(SELECTED_ITEM)) {
                    displayPropertyGroup(SELECTED_ITEM);
                    SELECTED_ITEM.IsDirty = false;
                    $("#btnDelete").val("Undelete Property Group");
                    $("#btnDelete").attr("onClick", "onPropertyGroupUndeleteClicked();");
                    result = 'The property group "' + SELECTED_ITEM.Name + '" has been deleted from the system.<br/><br/>It can be undeleted at a later time if needed.'
                } else result = "Failed to delete the '" + SELECTED_ITEM.Name + "' property group from the system.";
                $(this).dialog("close");
                showMessage("Result", result, true, 400, true)
            }
        }
    });
    $dialog.dialog("open")
}

function onPropertyGroupSaveClicked() {
    var wasNewItem = SELECTED_ITEM.IsNew,
        saved = propertyGroup_Save(SELECTED_ITEM),
        msg = null;
    if (saved) {
        SELECTED_ITEM = propertyGroup_Get(SELECTED_ITEM.Id);
        clearForm_PropertyGroup();
        displayPropertyGroup(SELECTED_ITEM);
        if (wasNewItem) {
            autoComplete_SwapDatasourceAsIs(propertyGroups_GetDictionary(USER_CURRENT.Id), "lstItems");
            $("#lstItems").val(SELECTED_ITEM.Name);
            $("#lstItems-id").val(SELECTED_ITEM.Id)
        }
        toggleControl("btnReset", false, true);
        toggleControl("btnSubmit", false, true);
        toggleButtons("resetButtons", false);
        toggleButtons("submitButtons", false);
        msg = "Property Group saved successfully"
    } else msg = "An error occured while saving the Property Group";
    showMessage("Result", msg, true, 400, true)
}

function onPropertyGroupUndeleteClicked() {
    var result = null;
    SELECTED_ITEM.Deleted = null;
    SELECTED_ITEM.DeletedBy = null;
    SELECTED_ITEM.LastModified = getServerDateTime();
    SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id;
    SELECTED_ITEM.IsDirty = true;
    if (propertyGroup_Save(SELECTED_ITEM)) {
        displayPropertyGroup(SELECTED_ITEM);
        SELECTED_ITEM.IsDirty = false;
        $("#btnDelete").val("Delete Property Group");
        $("#btnDelete").attr("onClick", "onPropertyGroupDeleteClicked();");
        result = 'The property group "' + SELECTED_ITEM.Name + '" has been undeleted from the system.<br/><br/>It can now be used within a view.'
    } else result = "Failed to undelete the '" + SELECTED_ITEM.Name + "' property group from the system.";
    showMessage("Result", result, true, 400, true)
}

function onPropertyIsReadOnlyCheckChanged(propertyId) {
    if (propertyId !== null && propertyId != undefined && propertyId != "") {
        var relation = getPropertyRelation(SELECTED_ITEM, propertyId);
        if (relation !== null && relation != undefined) {
            var wasChecked = checkBox_GetChecked("chk" + propertyId + "ReadOnly");
            if (relation.IsReadOnly != wasChecked) {
                relation.IsReadOnly = wasChecked;
                relation.IsDirty = true;
                SELECTED_ITEM.IsDirty = true;
                if (!SELECTED_ITEM.IsNew) {
                    SELECTED_ITEM.LastModified = getServerDateTime();
                    SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
                }
                itemModified(true)
            }
        }
    }
}

function onPropertyIsRequiredCheckChanged(propertyId) {
    if (propertyId !== null && propertyId != undefined && propertyId != "") {
        var relation = getPropertyRelation(SELECTED_ITEM, propertyId);
        if (relation !== null && relation != undefined) {
            var wasChecked = checkBox_GetChecked("chk" + propertyId + "Required");
            if (relation.IsRequired != wasChecked) {
                relation.IsRequired = wasChecked;
                relation.IsDirty = true;
                SELECTED_ITEM.IsDirty = true;
                if (!SELECTED_ITEM.IsNew) {
                    SELECTED_ITEM.LastModified = getServerDateTime();
                    SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
                }
                itemModified(true)
            }
        }
    }
}

function reOrderProperties() {
    var index = 0,
        viewModified = false;
    $("#values > li").each(function() {
        var tempVal = this.id;
        if (tempVal != "0" && tempVal != "-1") {
            var relation = getPropertyRelation(SELECTED_ITEM, tempVal);
            if (relation != null)
                if (relation.Order != index) {
                    relation.Order = index;
                    relation.IsDirty = true;
                    viewModified = true
                }
        }
        index++
    });
    if (viewModified) {
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
        }
        itemModified(true)
    }
}

function onRemovePropertyClicked(propertyId) {
    var validSelection = propertyId !== null && propertyId != undefined && propertyId != "";
    if (validSelection) {
        var property = property_Get(propertyId),
            $dialog = $("#messageDialog");
        if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
        $dialog.html("Remove the '" + property.Name + "' property from this property group?<br/><br/>This action can be undone if the property is needed later.");
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
                    var foundAndDeleted = false;
                    $.each(SELECTED_ITEM.PropertyMembers, function(index, item) {
                        if (item.PropertyId == propertyId) {
                            item.Deleted = getServerDateTime();
                            item.DeletedBy = USER_CURRENT.Id;
                            foundAndDeleted = true;
                            return false
                        }
                    });
                    if (foundAndDeleted) {
                        if (!SELECTED_ITEM.IsNew) {
                            SELECTED_ITEM.LastModified = getServerDateTime();
                            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
                        }
                        htmlSelect_InsertOption("lstProperties", property.Id, property.Name, false);
                        displayProperties(SELECTED_ITEM);
                        itemModified(true);
                        result = "Property '" + property.Name + " removed from the property group.<br/><br/>Property group must be saved to make this change effective."
                    } else result = "Failed to delete the '" + propertyGroup.Name + "' property from the current property group.";
                    showMessage("Result", result, true, 400, true)
                }
            }
        });
        $dialog.dialog("open")
    }
}

function onSelectedPropertyChanged() {
    var propertyId = $("#lstProperties").val(),
        validSelection = propertyId !== null && propertyId != undefined && propertyId != "";
    toggleControl("btnAddProperty", validSelection, true)
}

function getPropertyRelation(propertyGroup, propertyId) {
    var relation = null;
    $.each(propertyGroup.PropertyMembers, function(index, item) {
        if (item.PropertyId == propertyId) {
            relation = item;
            return false
        }
    });
    return relation
}

function propertyGroupContainsProperty(propertyGroup, propertyId) {
    var found = false;
    $.each(propertyGroup.PropertyMembers, function(index, item) {
        if (item.PropertyId == propertyId) {
            found = true;
            return false
        }
    });
    return found
}

function createItem() {
    SELECTED_ITEM = propertyGroup_Create(USER_CURRENT.Id);
    if (SELECTED_ITEM !== null && SELECTED_ITEM != undefined) {
        clearForm_PropertyGroup();
        displayPropertyGroup(SELECTED_ITEM)
    } else showMessage("Error", "Request to create a new property group failed", true, 400, true)
}
