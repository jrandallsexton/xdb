var ttId = "The internal id of the asset type.<br/><br/>This value is constant, never changed and cannot be changed.",
    ttName = "The name of the asset type as it is to be known throughout the system.<br/><br/>It may be changed freely",
    ttPlural = "The pluralized form of the asset type's name.<br/><br/>It may be changed freely.",
    ttParent = "The asset type which is the parent of this asset type.  If a parent asset type is selected, the properties that are currently associated with the parent will be inherited by this asset type.<br/><br/>Optional but cannot be changed once set.",
    ttDVD = "The default view for definitions of this asset type.",
    ttDVI = "The default view for instances of this asset type.",
    ttImage = "The icon to be used for this asset type in the favorites area.<br/><br/>Upload the image using the System Image Uploader (under the Utilities menu) then return here to select the image.",
    ttCreated = "When the asset type was created",
    ttCreatedBy = "Who created the asset type",
    ttDeleted = "When the asset type was deleted",
    ttDeletedBy = "Who deleted the asset type",
    ttAllowAssets = "Whether or not this asset type can have assets. i.e. if false, it's a hierarchical placeholder",
    _imgSrc = "../images";

function clearForm_AssetType() {
    $("#txtName").val("");
    $("#txtPluralization").val("");
    $("#lstAssetTypes").val("-1");
    $("#lstAllPropertiesDef").val("");
    $("#lstDefinitionProperties").html("");
    $("#lstAllPropertiesInstance").val("");
    $("#lstInstanceProperties").html("");
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
    activateAutoCompletes(ITEM_TYPE_ASSET_TYPE);
    var html = [];
    html.push('<option value="-1">[Select]</option>');
    $.each(assetTypes_GetAll(), function(index, item) {
        html.push('<option value="' + item.key + '">' + item.value + "</option>")
    });
    $("#lstAssetTypes").html(html.join(""));
    html = [];
    html.push('<select id="lstImages" style="width:100%;" class="ui-widget ui-widget-content" onChange="onImageChanged()">');
    html.push('<option value="' + NULL_GUID + '">[Select]</option>');
    $.each(imageDictionary_GetSystem(), function(index, item) {
        html.push('<option value="' + item.key + '">' + item.value + "</option>")
    });
    html.push("</select>");
    $("#tdImages").html(html.join(""));
    var itemId = getParameterByName("itemId");
    if (itemId !== null && itemId != undefined && itemId != "") {
        SELECTED_ITEM = assetType_Get(itemId);
        displayAssetType(SELECTED_ITEM);
        $("#lstItems").val(SELECTED_ITEM.Name)
    }
    $("#accordion").accordion({
        autoHeight: false,
        collapsible: true,
        icons: {
            header: null,
            headerSelected: null
        }
    })
}

function createItem() {
    SELECTED_ITEM = assetType_Create(USER_CURRENT.Id);
    if (SELECTED_ITEM !== null && SELECTED_ITEM != undefined) {
        clearForm_AssetType();
        displayAssetType(SELECTED_ITEM)
    } else showMessage("Error", "Request to create a new asset type failed", true, 400, true)
}

function displayAssetType(assetType) {
    $("#lblId").text(assetType.Id);
    $("#txtName").val(assetType.Name);
    $("#txtPluralization").val(assetType.Pluralization);
    $("#txtLabelDefinitions").val(assetType.DefinitionLabel);
    $("#txtLabelInstances").val(assetType.InstanceLabel);
    checkBox_SetChecked("chkAllowAssets", assetType.AllowAssets);
    var hasParent = false;
    if (assetType.ParentId !== null && assetType.ParentId != undefined && assetType.ParentId != "") {
        $("#lstAssetTypes").val(assetType.ParentId);
        hasParent = true
    } else $("#lstAssetTypes").val("-1");
    var html = [];
    html.push('<option value="-1">[Select]</option>');
    $.each(views_GetByAssetType(assetType.Id, ART_DEFINITION, true), function(index, item) {
        html.push('<option value="' + item.key + '">' + item.value + "</option>")
    });
    $("#lstViewsDefinitions").html(html.join(""));
    if (assetType.DefaultViewId !== null) $("#lstViewsDefinitions").val(assetType.DefaultViewId);
    else $("#lstViewsDefinitions").val("-1");
    html = [];
    html.push('<option value="-1">[Select]</option>');
    $.each(views_GetByAssetType(assetType.Id, ART_INSTANCE, true), function(index, item) {
        html.push('<option value="' + item.key + '">' + item.value + "</option>")
    });
    $("#lstViewsInstances").html(html.join(""));
    if (assetType.DefaultInstanceViewId !== null) $("#lstViewsInstances").val(assetType.DefaultInstanceViewId);
    else $("#lstViewsInstances").val("-1");
    displayPropertyMembers(assetType);
    $("#lstImages").val(assetType.ImageId);
    $("#tdCreated").html(getActionDateTime(assetType.Created));
    $("#tdCreatedBy").html(getActionCreator(assetType.CreatedBy));
    $("#tdDeleted").html(getActionDateTime(assetType.Deleted));
    if (assetType.Deleted !== null && assetType.Deleted != undefined && assetType.Deleted != "") {
        $("#btnDelete").val("Undelete Asset Type");
        $("#btnDelete").attr("onClick", "onAssetTypeUnDeleteClicked();")
    } else {
        $("#btnDelete").val("Delete Asset Type");
        $("#btnDelete").attr("onClick", "onAssetTypeDeleteClicked();")
    }
    $("#tdDeletedBy").html(getActionCreator(assetType.DeletedBy));
    $("#tblButtons").show();
    toggleButtons("deleteButtons", true);
    toggleButtons("killButtons", USER_CURRENT.IsAdmin);
    $("#accordion").show();
    if (!hasParent) $("#accordion").find("h3").filter(":contains(Inherited)").hide();
    else $("#accordion").find("h3").filter(":contains(Inherited)").show();
    return true
}

function displayPropertyMembers(assetType) {
    var availablePropertiesHtml = [],
        defPropInheritHtml = [],
        defPropHtml = [],
        instancePropInheritHtml = [],
        instancePropHtml = [];
    availablePropertiesHtml.push('<option value="-1">[Select]</option>');
    $.each(properties_GetDictionary(USER_CURRENT.Id), function(index, item) {
        var propertyMember = getPropertyMember(assetType, item.key);
        if (propertyMember == null) availablePropertiesHtml.push('<option value="' + item.key + '">' + item.value + "</option>");
        else if (propertyMember.IsInstance)
            if (propertyMember.IsInherited) instancePropInheritHtml.push('<option value="' + item.key + '">' + item.value + "</option>");
            else instancePropHtml.push('<option value="' + item.key + '">' + item.value + "</option>");
        else if (propertyMember.IsInherited) defPropInheritHtml.push('<option value="' + item.key + '">' + item.value + "</option>");
        else defPropHtml.push('<option value="' + item.key + '">' + item.value + "</option>")
    });
    $("#lstAllPropertiesDef").html(availablePropertiesHtml.join(""));
    $("#lstAllPropertiesInstance").html(availablePropertiesHtml.join(""));
    $("#lstDefPropsInherited").html(defPropInheritHtml.join(""));
    $("#lstDefinitionProperties").html(defPropHtml.join(""));
    $("#lstInstPropsInherited").html(instancePropInheritHtml.join(""));
    $("#lstInstanceProperties").html(instancePropHtml.join(""))
}

function getPropertyMember(assetType, propertyId) {
    var returnValue = null;
    $.each(assetType.PropertyMembers, function(index, item) {
        if (item.PropertyId == propertyId) {
            returnValue = item;
            return false
        }
    });
    return returnValue
}

function onAvailableDefinitionPropertySelected() {
    var selectedPropertyId = $("#lstAllPropertiesDef").val();
    if (selectedPropertyId != "-1") {
        toggleControl("btnAddDefinitionProperty", true, false);
        $("#lstDefinitionProperties").val(null);
        toggleControl("btnDeleteDefinitionProperty", false, false)
    } else toggleControl("btnAddDefinitionProperty", false, false)
}

function onAvailableInstancePropertySelected() {
    var selectedPropertyId = $("#lstAllPropertiesInstance").val();
    if (selectedPropertyId != "-1") toggleControl("btnAddInstanceProperty", true, false);
    else toggleControl("btnAddInstanceProperty", false, false)
}

function onDefinitionPropertySelected() {
    var selectedPropertyId = $("#lstDefinitionProperties").val();
    if (selectedPropertyId != "-1") {
        toggleControl("btnDeleteDefinitionProperty", true, false);
        $("#lstAllPropertiesDef").val("-1");
        toggleControl("btnAddDefinitionProperty", false, false)
    } else toggleControl("btnDeleteDefinitionProperty", false, false)
}

function onInstancePropertySelected() {
    var selectedPropertyId = $("#lstInstanceProperties").val();
    if (selectedPropertyId != "-1") toggleControl("btnDeleteInstanceProperty", true, false);
    else toggleControl("btnDeleteInstanceProperty", false, false)
}

function onLabelDefinitionChanged() {
    var newValue = $("#txtLabelDefinitions").val();
    if (newValue != SELECTED_ITEM.DefinitionLabel) {
        SELECTED_ITEM.DefinitionLabel = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
        }
        itemModified(true)
    }
}

function onLabelInstanceChanged() {
    var newValue = $("#txtLabelInstances").val();
    if (newValue != SELECTED_ITEM.InstanceLabel) {
        SELECTED_ITEM.InstanceLabel = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
        }
        itemModified(true)
    }
}

function createNewAssetTypePropertyRelation(assetTypeId, propertyId, isInstanceProperty) {
    var relation = {};
    relation.Id = generateGuid(null);
    relation.Created = getServerDateTime();
    relation.CreatedBy = USER_CURRENT.Id;
    relation.Deleted = null;
    relation.DeletedBy = null;
    relation.AssetTypeId = assetTypeId;
    relation.PropertyId = propertyId;
    relation.IsInstance = isInstanceProperty;
    relation.IsInherited = false;
    relation.IsInheritedValue = false;
    relation.IsNew = true;
    relation.IsDirty = true;
    return relation
}

function createNewAssetTypePropertyRelationFromParent(assetTypeId, propertyId, isInstanceProperty) {
    var relation = {};
    relation.Id = generateGuid(null);
    relation.Created = getServerDateTime();
    relation.CreatedBy = USER_CURRENT.Id;
    relation.Deleted = null;
    relation.DeletedBy = null;
    relation.AssetTypeId = assetTypeId;
    relation.PropertyId = propertyId;
    relation.IsInstance = isInstanceProperty;
    relation.IsInherited = true;
    relation.IsInheritedValue = false;
    relation.IsNew = true;
    relation.IsDirty = true;
    return relation
}

function onAddDefinitionPropertyClicked() {
    var selectedPropertyId = $("#lstAllPropertiesDef").val(),
        relation = createNewAssetTypePropertyRelation(SELECTED_ITEM.Id, selectedPropertyId, false),
        property = property_Get(selectedPropertyId);
    SELECTED_ITEM.PropertyMembers.push(relation);
    htmlSelect_InsertOption("lstDefinitionProperties", selectedPropertyId, property.Name, false);
    htmlSelect_RemoveOptionByValue("lstAllPropertiesDef", selectedPropertyId);
    toggleControl("btnAddDefinitionProperty", false, true);
    SELECTED_ITEM.IsDirty = true;
    if (!SELECTED_ITEM.IsNew) {
        SELECTED_ITEM.LastModified = getServerDateTime();
        SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
    }
    itemModified(true)
}

function onAddInstancePropertyClicked() {
    var selectedPropertyId = $("#lstAllPropertiesInstance").val(),
        relation = createNewAssetTypePropertyRelation(SELECTED_ITEM.Id, selectedPropertyId, true),
        property = property_Get(selectedPropertyId);
    SELECTED_ITEM.PropertyMembers.push(relation);
    htmlSelect_InsertOption("lstInstanceProperties", selectedPropertyId, property.Name, false);
    htmlSelect_RemoveOptionByValue("lstAllPropertiesInstance", selectedPropertyId);
    toggleControl("btnAddInstanceProperty", false, true);
    SELECTED_ITEM.IsDirty = true;
    if (!SELECTED_ITEM.IsNew) {
        SELECTED_ITEM.LastModified = getServerDateTime();
        SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
    }
    itemModified(true)
}

function onAllowAssetsChanged() {
    var isChecked = checkBox_GetChecked("chkAllowAssets");
    if (isChecked != SELECTED_ITEM.AllowAssets) {
        SELECTED_ITEM.AllowAssets = isChecked;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function assetTypeSafeToDeleteOrKill() {
    var children = assetType_GetChildrenOnly(SELECTED_ITEM.Id);
    if (children !== null && children.length > 0) {
        var html = [];
        html.push("<label>Cannot be deleted.  Has the following child asset type(s).</label><br/><br/>");
        html.push('<table style="width:100%;" >');
        $.each(children, function(index, item) {
            var newRow = index % 5 == 0,
                closeRow = (index + 1) % 5 == 0;
            if (newRow) html.push('<tr style="width:100%;">');
            html.push('<td style="width:20%;">' + item.value + "</td>");
            if (closeRow) html.push("</tr>")
        });
        html.push("</table>");
        showMessage("Cannot Delete", html.join(""), true, 600, true);
        return false
    }
    var assets = assets_Get(SELECTED_ITEM.Id, ART_BOTH, true);
    if (assets !== null && assets.length > 0) {
        var html = [];
        html.push("<label>Cannot be deleted.  Has the following asset(s).</label><br/><br/>");
        html.push('<table style="width:100%;" >');
        $.each(assets, function(index, item) {
            var newRow = index % 5 == 0,
                closeRow = (index + 1) % 5 == 0;
            if (newRow) html.push('<tr style="width:100%;">');
            html.push('<td style="width:20%;">' + item.Value + "</td>");
            if (closeRow) html.push("</tr>")
        });
        showMessage("Cannot Delete", html.join(""), true, 600, true);
        return false
    }
    return true
}

function onAssetTypeDeleteClicked() {
    if (!assetTypeSafeToDeleteOrKill()) return;
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html('Delete the <i>"' + SELECTED_ITEM.Name + '"</i> asset type?<br/><br/>This action can be undone if the asset type is needed later.');
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
                SELECTED_ITEM.LastModified = SELECTED_ITEM.Deleted;
                SELECTED_ITEM.LastModifiedBy = SELECTED_ITEM.DeletedBy;
                if (assetType_Save(SELECTED_ITEM)) {
                    $("#btnDelete").val("Undelete Picklist");
                    $("#btnDelete").attr("onClick", "onPickListUndeleteClicked();");
                    result = SELECTED_ITEM.Name + " has been deleted from the system.<br/>It can be undeleted at a later time if needed."
                } else result = "Failed to delete the '" + SELECTED_ITEM.Name + "' picklist from the system.";
                $(this).dialog("close");
                showMessage("Result", result, true, 400, true)
            }
        }
    });
    $dialog.dialog("open")
}

function onAssetTypeSaveClicked() {
    var wasNewItem = SELECTED_ITEM.IsNew;
    if (assetType_Save(SELECTED_ITEM)) {
        SELECTED_ITEM = assetType_Get(SELECTED_ITEM.Id);
        clearForm_AssetType();
        displayAssetType(SELECTED_ITEM);
        if (wasNewItem) {
            autoComplete_SwapDatasourceAsIs(assetTypes_GetAll(), "lstItems");
            $("#lstItems").val(SELECTED_ITEM.Name);
            $("#lstItems-id").val(SELECTED_ITEM.Id)
        }
        toggleControl("btnReset", false, true);
        toggleControl("btnSubmit", false, true);
        toggleButtons("resetButtons", false);
        toggleButtons("submitButtons", false);
        showMessage("Result", "Asset Type saved successfully", true, 400, true)
    } else {}
}

function onAssetTypeUnDeleteClicked() {
    showMessage("Error", "Not yet implemented", true, 400, true)
}

function onDefaultDefinitionViewChanged() {
    var newValue = $("#lstViewsDefinitions").val();
    if (newValue != SELECTED_ITEM.DefaultViewId) {
        SELECTED_ITEM.DefaultViewId = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
        }
        itemModified(true)
    }
}

function onDefaultInstanceViewChanged() {
    var newValue = $("#lstViewsInstances").val();
    if (newValue != SELECTED_ITEM.DefaultInstanceViewId) {
        SELECTED_ITEM.DefaultInstanceViewId = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
        }
        itemModified(true)
    }
}

function onImageChanged() {
    var newValue = $("#lstImages").val();
    if (newValue != SELECTED_ITEM.ImageId) {
        SELECTED_ITEM.ImageId = newValue;
        SELECTED_ITEM.IsDirty = true;
        itemModified(true)
    }
}

function onKillClicked() {
    if (!assetTypeSafeToDeleteOrKill()) return;
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html("Permanently destroy the '" + SELECTED_ITEM.Name + "' asset type?<br/>This action can not be undone");
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
                if (assetType_Kill(USER_CURRENT.Id, SELECTED_ITEM.Id)) result = SELECTED_ITEM.Name + " has been permanently deleted from the system";
                else result = "Failed to kill the '" + SELECTED_ITEM.Name + "' asset type.";
                $(this).dialog("close");
                showMessage("Result", result, true, 400, true)
            }
        }
    });
    $dialog.dialog("open")
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

function onParentAssetTypeChanged() {
    SELECTED_ITEM.ParentId = $("#lstAssetTypes").val();
    var html = [];
    html.push('<option value="-1">[Select]</option>');
    $.each(views_GetByAssetType(SELECTED_ITEM.ParentId, ART_DEFINITION, true), function(index, item) {
        html.push('<option value="' + item.key + '">' + item.value + "</option>")
    });
    $("#lstViewsDefinitions").html(html.join(""));
    html = [];
    html.push('<option value="-1">[Select]</option>');
    $.each(views_GetByAssetType(SELECTED_ITEM.ParentId, ART_INSTANCE, true), function(index, item) {
        html.push('<option value="' + item.key + '">' + item.value + "</option>")
    });
    $("#lstViewsInstances").html(html.join(""));
    var parentAssetType = assetType_Get(SELECTED_ITEM.ParentId);
    $.each(parentAssetType.PropertyMembers, function(index, item) {
        SELECTED_ITEM.PropertyMembers.push(createNewAssetTypePropertyRelationFromParent(SELECTED_ITEM.Id, item.PropertyId, item.IsInstance))
    });
    displayPropertyMembers(SELECTED_ITEM)
}

function onPluralizationChanged() {
    var newValue = $("#txtPluralization").val();
    if (newValue != SELECTED_ITEM.Pluralization) {
        SELECTED_ITEM.Pluralization = newValue;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_CURRENT.Id
        }
        itemModified(true)
    }
}

function onRemoveDefinitionPropertyClicked() {
    var selectedPropertyId = $("#lstDefinitionProperties").val(),
        property = property_Get(selectedPropertyId),
        relation = getAssetTypePropertyRelation(SELECTED_ITEM, selectedPropertyId);
    if (relation !== null) {
        relation.Deleted = getServerDateTime();
        relation.DeletedBy = USER_CURRENT.Id;
        htmlSelect_InsertOption("lstAllPropertiesDef", selectedPropertyId, property.Name, false);
        htmlSelect_RemoveOptionByValue("lstDefinitionProperties", selectedPropertyId)
    }
    toggleControl("btnDeleteDefinitionProperty", false, true);
    itemModified(true)
}

function onRemoveInstancePropertyClicked() {
    var selectedPropertyId = $("#lstInstanceProperties").val(),
        property = property_Get(selectedPropertyId),
        relation = getAssetTypePropertyRelation(SELECTED_ITEM, selectedPropertyId);
    if (relation !== null) {
        relation.Deleted = getServerDateTime();
        relation.DeletedBy = USER_CURRENT.Id;
        htmlSelect_InsertOption("lstAllPropertiesInstance", selectedPropertyId, property.Name, false);
        htmlSelect_RemoveOptionByValue("lstInstanceProperties", selectedPropertyId)
    }
    toggleControl("btnDeleteInstanceProperty", false, true);
    itemModified(true)
}

function getAssetTypePropertyRelation(assetType, propertyId) {
    var relation = null;
    $.each(assetType.PropertyMembers, function(index, item) {
        if (item.PropertyId == propertyId) {
            relation = item;
            return false
        }
    });
    return relation
}
