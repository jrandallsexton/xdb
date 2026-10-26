var ttId = "The internal id of the view.<br/><br/>This value is constant, never changed and cannot be changed.",
    ttName = "The name of the view is an internal identifier available only to admins.  This value is never shown to users.<br/><br/>It may be changed freely",
    ttDisplayValue = "The friendly, user-facing name of the view as it appears within the menu contents of the 'Asset Editor' menu option.<br/><br/>This value may be changed without affecting other system functions.",
    ttDescription = "Short summary of the purpose of the view.<br/><br/>This value may be changed without affecting other system functions.",
    ttDriverCaption = "Defines the text shown within the Asset Editor for the item selector.",
    ttNewItemCaption = "Defines the text shown when creating a new asset from within the Asset Editor.",
    ttConfirmationLabel = "NOT YET DEFINED",
    ttIsCreation = "Determines whether or not new items can be created directly within the view via the toolbar.",
    ttIsReadOnly = "Is Read Only - NOT YET DEFINED",
    ttIsStandAlone = "Is Stand Alone - NOT YET DEFINED",
    ttIsAutoApprove = "Is Auto Approve - NOT YET DEFINED",
    ttAssetType = "The asset type with which this view is associated.<br/><br/>The selected asset type will dictate what property groups can be added to this view.<br/><br/>Once this value has been set, and the view has been saved, it cannot be changed.",
    ttIsInstanceView = "Determines whether this view operates on asset definitions or asset instances.<br/><br/>Works in conjunction with 'Associated Asset Type'.",
    _imgSrc = "../images";

function clearForm_View() {
    $("#txtName").val("");
    $("#txtDisplayValue").val("");
    $("#txtDescription").val("");
    $("#txtDriverCaption").val("");
    $("#txtNewItemCaption").val("");
    checkBox_SetChecked("chkIsInstance", false);
    checkBox_SetChecked("chkIsReadOnly", false);
    checkBox_SetChecked("chkIsStandAlone", false);
    $("#lstAssetTypes").val("-1");
    $("#values").html("");
    $("#tdCreated").html("");
    $("#tdCreatedBy").html("");
    $("#tdApproved").html("");
    $("#tdApprovedBy").html("");
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
    var html = [];
    html.push("<option value='-1'>[Select]</option>");
    $.each(assetTypes_GetAll(), function(index, item) {
        html.push("<option value='" + item.key + "'>" + item.value + "</option>")
    });
    $("#lstAssetTypes").html(html.join(""));
    var itemId = getParameterByName("itemId");
    if (itemId !== null && itemId != undefined && itemId != "") {
        SELECTED_ITEM = view_Get(itemId, USER_ACTUAL.Id);
        displayView(SELECTED_ITEM);
        $("#lstItems").val(SELECTED_ITEM.Name)
    }
    checkBox_SetChecked("chkDeletedItems", false);
    activateAutoCompletes(ITEM_TYPE_VIEW);
    $("#lstItems").focus();
    $("#lstItems").autocomplete("close");
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
            onPropertyGroupDropped(ui)
        }
    });
    $(".draggable").draggable({
        connectToSortable: ".sortable",
        helper: "clone",
        revert: "invalid"
    });
    $("ul, li").disableSelection()
}

function displayView(view) {
    $("#accordion").show();
    $("#lblId").text(view.Id);
    $("#txtName").val(view.Name);
    $("#txtDisplayValue").val(view.DisplayValue);
    $("#txtDescription").val(view.Description);
    $("#txtDriverCaption").val(view.DriverCaption);
    $("#txtNewItemCaption").val(view.NewItemCaption);
    $("#txtConfirmationLabel").val(view.ConfirmationLabel);
    checkBox_SetChecked("chkIsInstance", view.IsInstance);
    checkBox_SetChecked("chkIsReadOnly", view.IsReadOnly);
    checkBox_SetChecked("chkIsStandAlone", view.IsStandAlone);
    checkBox_SetChecked("chkIsAutoApprove", view.IsAutoApprove);
    checkBox_SetChecked("chkIsCreation", view.IsCreation);
    $("#lstAssetTypes").val(view.AssetTypeId);
    var isSet = view.AssetTypeId !== null && view.AssetTypeId != undefined && view.AssetTypeId != "" && view.AssetTypeId != NULL_GUID;
    htmlControl_SetEnabled("lstAssetTypes", !isSet);
    displayPropertyGroupsAvailable(view);
    buildFilterContent();
    displayFilters(view.Filters);
    $("#txtFilterLogic").val(view.FilterLogic);
    displayPropertyGroups(view);
    displayConsumers(view.Id);
    $("#tdCreated").html(getActionDateTime(view.Created));
    $("#tdCreatedBy").html(getActionCreator(view.CreatedBy));
    $("#tdModified").html(getActionDateTime(view.LastModified));
    $("#tdModifiedBy").html(getActionCreator(view.LastModifiedBy));
    $("#tdDeleted").html(getActionDateTime(view.Deleted));
    if (view.Deleted !== null && view.Deleted != "") {
        $("#btnDelete").val("Undelete View");
        $("#btnDelete").attr("onClick", "onViewUndeleteClicked()")
    } else {
        $("#btnDelete").val("Delete View");
        $("#btnDelete").attr("onClick", "onViewDeleteClicked()")
    }
    $("#tdDeletedBy").html(getActionCreator(view.DeletedBy));
    $("#tblButtons").show();
    toggleButtons("deleteButtons", !SELECTED_ITEM.IsNew);
    toggleButtons("cloneButtons", !SELECTED_ITEM.IsNew);
    toggleButtons("killButtons", USER_ACTUAL.IsAdmin && !SELECTED_ITEM.IsNew);
    return true
}

function displayConsumers(viewId) {
    var html = [];
    $.each(roles_GetByViewId(viewId), function(index, item) {
        html.push("<a href='" + URL_CLIENT_UI + "/admin/roles.html?itemId=" + item.key + "' target='_blank'>" + item.value + "</a><br/>")
    });
    $("#divConsumers").html(html.join(""))
}

function displayPropertyGroups(view) {
    var html = [];
    if (view.PropertyGroups != null && view.PropertyGroups.length > 0) {
        $("#lblDrag").show();
        $.each(view.PropertyGroups, function(index, item) {
            var relation = getPropertyGroupRelation(view, item.Id);
            if (relation !== null && (relation.Deleted == null || relation.Deleted == ""))
                if (item.DisplayValue != "") {
                    html.push("<li id='" + item.Id + "' class='ui-state-default'>" + index + ". " + item.Name + " (" + item.DisplayValue + ")");
                    html.push("<div style='float:right'>");
                    html.push("<span class='ui-icon ui-" + ICON_OPEN_TAB + " itemReorder' onClick='onPropertyGroupClicked(\"" + item.Id + "\")' title='Open this property group in a new tab'></span>");
                    html.push("<span class='ui-icon ui-icon-trash itemReorder' onClick='onRemovePropertyGroupClicked(\"" + item.Id + "\")'></span>");
                    html.push("</div>");
                    html.push("</li>")
                } else html.push("<li id='" + item.Id + "' class='ui-state-default' onClick='onRemovePropertyGroupClicked()'>" + index + ". " + item.Name + "</li>")
        })
    } else $("#lblDrag").hide();
    $("#values").html(html.join(""))
}

function displayPropertyGroupsAvailable(view) {
    var html = [];
    html.push("<option value='-1'>[Select]</option>");
    var atId = view.AssetTypeId,
        isValid = atId !== null && atId != undefined && atId != "" && atId != NULL_GUID;
    if (isValid) $.each(propertyGroups_GetByAssetTypeId(view.AssetTypeId), function(index, item) {
        if (!viewContainsPropertyGroup(view, item.key)) html.push("<option value='" + item.key + "'>" + item.value + "</option>")
    });
    $("#lstPropertyGroups").html(html.join(""))
}

function onAddPropertyGroupClicked() {
    var propertyGroupId = $("#lstPropertyGroups").val();
    if (propertyGroupId !== null && propertyGroupId != undefined && propertyGroupId != "") {
        var relation = {};
        relation.Id = generateGuid(null);
        relation.Created = getServerDateTime();
        relation.CreatedBy = USER_ACTUAL.Id;
        relation.Deleted = null;
        relation.DeletedBy = null;
        relation.ViewId = SELECTED_ITEM.Id;
        relation.PropertyGroupId = propertyGroupId;
        relation.Order = SELECTED_ITEM.PropertyGroupMembers.length;
        relation.IsNew = true;
        relation.IsDirty = true;
        SELECTED_ITEM.PropertyGroups.push(propertyGroup_Get(propertyGroupId));
        SELECTED_ITEM.PropertyGroupMembers.push(relation);
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        htmlSelect_RemoveOptionByValue("lstPropertyGroups", propertyGroupId);
        itemModified(true);
        displayPropertyGroups(SELECTED_ITEM);
        $(".sortable").sortable({
            revert: true,
            stop: function(event, ui) {
                onPropertyGroupDropped(ui)
            }
        });
        $(".draggable").draggable({
            connectToSortable: ".sortable",
            helper: "clone",
            revert: "invalid"
        });
        $("ul, li").disableSelection();
        toggleControl("btnAddPropertyGroup", false, true)
    }
}

function onCloneClicked() {
    SELECTED_ITEM = view_Clone(SELECTED_ITEM.Id, USER_ACTUAL.Id);
    displayView(SELECTED_ITEM)
}

function onConfirmationLabelChanged() {
    var newValue = $("#txtConfirmationLabel").val();
    if (newValue != SELECTED_ITEM.ConfirmationLabel) {
        SELECTED_ITEM.ConfirmationLabel = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onDeletedItemsChanged() {
    activateAutoCompletes(ITEM_TYPE_VIEW, checkBox_GetChecked("chkDeletedItems"))
}

function onDescriptionChanged() {
    var newValue = $("#txtDescription").val();
    if (newValue != SELECTED_ITEM.Description) {
        SELECTED_ITEM.Description = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
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
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onDriverCaptionChanged() {
    var newValue = $("#txtDriverCaption").val();
    if (newValue != SELECTED_ITEM.DriverCaption) {
        SELECTED_ITEM.DriverCaption = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onIsAutoApproveChanged() {
    var isChecked = checkBox_GetChecked("chkIsAutoApprove");
    if (isChecked != SELECTED_ITEM.IsAutoApprove) {
        SELECTED_ITEM.IsAutoApprove = isChecked;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onIsCreationChanged() {
    var isChecked = checkBox_GetChecked("chkIsCreation");
    if (isChecked != SELECTED_ITEM.IsCreation) {
        SELECTED_ITEM.IsCreation = isChecked;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onIsInstanceViewChanged() {
    var isChecked = checkBox_GetChecked("chkIsInstance");
    if (isChecked != SELECTED_ITEM.IsInstance) {
        SELECTED_ITEM.IsInstance = isChecked;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onIsReadOnlyChanged() {
    var isChecked = checkBox_GetChecked("chkIsReadOnly");
    if (isChecked != SELECTED_ITEM.IsReadOnly) {
        SELECTED_ITEM.IsReadOnly = isChecked;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onIsStandAloneChanged() {
    var isChecked = checkBox_GetChecked("chkIsStandAlone");
    if (isChecked != SELECTED_ITEM.IsStandAlone) {
        SELECTED_ITEM.IsStandAlone = isChecked;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onKillClicked() {
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html("Permanently destroy the '" + SELECTED_ITEM.Name + "' view?<br/>This action can not be undone");
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
                if (view_Kill(SELECTED_ITEM.Id, USER_ACTUAL.Id)) result = SELECTED_ITEM.Name + " has been permanently deleted from the system";
                else result = "Failed to kill the '" + SELECTED_ITEM.Name + "' view.";
                $(this).dialog("close");
                showMessage("Result", result, true, 400, true)
            }
        }
    });
    $dialog.dialog("open")
}

function onViewNameChanged() {
    var newValue = $("#txtName").val();
    if (newValue != SELECTED_ITEM.Name) {
        SELECTED_ITEM.Name = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onNewItemCaptionChanged() {
    var newValue = $("#txtNewItemCaption").val();
    if (newValue != SELECTED_ITEM.NewItemCaption) {
        SELECTED_ITEM.NewItemCaption = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onPropertyGroupClicked(propertyGroupId) {
    var url = URL_CLIENT_UI + "/admin/propertyGroups.html?itemId=" + propertyGroupId;
    window.open(url)
}

function onPropertyGroupDropped(ui) {
    reOrderPropertyGroups();
    itemModified(true);
    return true
}

function reOrderPropertyGroups() {
    var index = 0,
        viewModified = false;
    $("#values > li").each(function() {
        var tempVal = this.id;
        if (tempVal != "0" && tempVal != "-1") {
            var relation = getPropertyGroupRelation(SELECTED_ITEM, tempVal);
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
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onRemovePropertyGroupClicked(propertyGroupId) {
    var validSelection = propertyGroupId !== null && propertyGroupId != undefined && propertyGroupId != "";
    if (validSelection) {
        var propertyGroup = propertyGroup_Get(propertyGroupId),
            $dialog = $("#messageDialog");
        if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
        $dialog.html("Remove the '" + propertyGroup.Name + "' property group from this view?<br/><br/>This action can be undone if the property group is needed later.");
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
                    $.each(SELECTED_ITEM.PropertyGroupMembers, function(index, item) {
                        if (item.PropertyGroupId == propertyGroupId) {
                            item.Deleted = getServerDateTime();
                            item.DeletedBy = USER_ACTUAL.Id;
                            foundAndDeleted = true;
                            return false
                        }
                    });
                    if (foundAndDeleted) {
                        if (!SELECTED_ITEM.IsNew) {
                            SELECTED_ITEM.LastModified = getServerDateTime();
                            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
                        }
                        itemModified(true);
                        htmlSelect_InsertOption("lstPropertyGroups", propertyGroup.Id, propertyGroup.Name, false);
                        displayPropertyGroups(SELECTED_ITEM);
                        result = "Property group '" + propertyGroup.Name + " removed from the view.<br/><br/>View must be saved to make this change effective."
                    } else result = "Failed to delete the '" + propertyGroup.Name + "' property group from the current view.";
                    showMessage("Result", result, true, 400, true)
                }
            }
        });
        $dialog.dialog("open")
    }
}

function onSelectedPropertyGroupChanged() {
    var propertyGroupId = $("#lstPropertyGroups").val(),
        validSelection = propertyGroupId !== null && propertyGroupId != undefined && propertyGroupId != "";
    toggleControl("btnAddPropertyGroup", validSelection, true)
}

function onSelectedAssetTypeChanged() {
    var newValue = $("#lstAssetTypes").val();
    if (newValue != SELECTED_ITEM.AssetTypeId) {
        SELECTED_ITEM.AssetTypeId = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        displayPropertyGroupsAvailable(SELECTED_ITEM);
        itemModified(true)
    }
}

function onSelectedExistingPropertyGroupChanged(propertyGroupId) {
    var validSelection = propertyGroupId !== null && propertyGroupId != undefined && propertyGroupId != "";
    toggleControl("btnRemovePropertyGroup", validSelection, true)
}

function onViewDeleteClicked() {
    var $dialog = $("#messageDialog");
    $dialog.dialog("destroy");
    $dialog.html("Delete the '" + SELECTED_ITEM.Name + "' view?<br/>This action can be undone if the view is needed later.");
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
                SELECTED_ITEM.DeletedBy = USER_ACTUAL.Id;
                SELECTED_ITEM.LastModified = SELECTED_ITEM.Deleted;
                SELECTED_ITEM.LastModifiedBy = SELECTED_ITEM.DeletedBy;
                if (view_Save(SELECTED_ITEM)) {
                    $("#btnDelete").val("Undelete View");
                    $("#btnDelete").attr("onClick", "onViewUndeleteClicked();");
                    result = SELECTED_ITEM.Name + " has been deleted from the system.<br/>It can be undeleted at a later time if needed."
                } else result = "Failed to delete the '" + SELECTED_ITEM.Name + "' view from the system.";
                $(this).dialog("close");
                showMessage("Result", result, true, 400, true)
            }
        }
    });
    $dialog.dialog("open")
}

function onViewSaveClicked() {
    var wasNewItem = SELECTED_ITEM.IsNew;
    if (view_Save(SELECTED_ITEM)) {
        clearForm_View();
        SELECTED_ITEM = view_Get(SELECTED_ITEM.Id, USER_ACTUAL.Id);
        displayView(SELECTED_ITEM);
        if (wasNewItem) {
            autoComplete_SwapDatasourceAsIs(views_GetDictionary(true, true), "lstItems");
            $("#lstItems").val(SELECTED_ITEM.DisplayValue);
            $("#lstItems-id").val(SELECTED_ITEM.Id)
        }
        toggleButtons("resetButtons", false);
        toggleButtons("submitButtons", false);
        showMessage("Result", "View saved successfully.", true, 400, true)
    } else {}
}

function onViewUndeleteClicked() {
    var result = null;
    SELECTED_ITEM.Deleted = null;
    SELECTED_ITEM.DeletedBy = null;
    SELECTED_ITEM.LastModified = getServerDateTime();
    SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id;
    SELECTED_ITEM.IsDirty = true;
    if (view_Save(SELECTED_ITEM)) {
        displayView(SELECTED_ITEM);
        SELECTED_ITEM.IsDirty = false;
        result = 'The view "' + SELECTED_ITEM.Name + '" has been undeleted from the system."'
    } else result = "Failed to undelete the '" + SELECTED_ITEM.Name + "' view from the system.";
    showMessage("Result", result, true, 400, true)
}

function getPropertyGroupRelation(view, propertyGroupId) {
    var relation = null;
    $.each(view.PropertyGroupMembers, function(index, item) {
        if (item.PropertyGroupId == propertyGroupId) {
            relation = item;
            return false
        }
    });
    return relation
}

function viewContainsPropertyGroup(view, propertyGroupId) {
    var found = false;
    $.each(view.PropertyGroupMembers, function(index, item) {
        if (item.PropertyGroupId == propertyGroupId) {
            found = true;
            return false
        }
    });
    return found
}

function createItem() {
    SELECTED_ITEM = view_Create(USER_ACTUAL.Id);
    if (SELECTED_ITEM !== null && SELECTED_ITEM != undefined) {
        clearForm_View();
        displayView(SELECTED_ITEM)
    } else showMessage("Error", "Request to create a new view failed", true, 400, true)
}
