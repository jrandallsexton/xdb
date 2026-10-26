var SELECTED_ASSETTYPE_RELATION = null,
    ROLES_PROCESSES = null,
    ROLES_VIEWS = null,
    PROMPT_NEW_USER = "Enter a user name or an email address",
    PROMPT_ADD_USER = " added to role.<br/><br/>Role must be saved to make this change effective.",
    PROMPT_ADD_USER_SHOW = true,
    _imgSrc = "../images",
    _assetTypeName = null,
    _treeInit = null,
    _assetRequestType = ART_DEFINITION;

function clearForm_Role() {
    $("#lblId").text("");
    $("#txtName").val("");
    $("#txtDescription").val("");
    $("#divFilters").html("");
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
    $.each(permissions_Get(), function(index, item) {
        html.push("<input type='checkbox' id='" + item.key + "' value='" + item.key + "' class='ui-state-default' onchange='onRolePermissionCheckChange(\"" + item.key + "\")'>" + item.value + "</input><br/>")
    });
    $("#divPermissions").html(html.join(""));
    var itemId = getParameterByName("itemId");
    if (itemId !== null && itemId != undefined && itemId != "") {
        SELECTED_ITEM = role_Get(itemId);
        displayRole(SELECTED_ITEM);
        $("#lstItems").val(SELECTED_ITEM.Name)
    }
    activateAutoCompletes(ITEM_TYPE_ROLE);
    $("#lstItems").focus();
    $("#lstItems").autocomplete("close");
    $("#accordion").accordion({
        autoHeight: false,
        collapsible: true,
        icons: {
            header: null,
            headerSelected: null
        }
    })
}

function displayRole(role) {
    var html = [];
    $("#lblId").text(role.Id);
    $("#txtName").val(role.Name);
    $("#txtDescription").val(role.Description);
    checkBox_SetChecked("chkIsActive", role.IsActive);
    if (role.Name == "Admin") {
        $("#treAssetTypes").html("<b>All asset types available to the Admin role</b>");
        $("#divFilters").html("<b>No filter restrictions on the Admin role</b>");
        $("#divPermissions").html("<b>All permissions available to the Admin role</b>");
        $("#divViews").html("<b>All views are available to the Admin role</b>");
        $("#divProcesses").html("<b>All processes are available to the Admin role</b>")
    } else {
        html = [];
        html.push("<ul>");
        html.push(buildAssetTypeUl(role));
        html.push("</ul>");
        var $tree = $("#treAssetTypes");
        if (_treeInit != null && _treeInit) $tree.dynatree("destroy");
        _treeInit = true;
        $tree.html(html.join(""));
        $tree.dynatree({
            checkbox: true,
            selectMode: 3,
            onActivate: function(node) {
                onAssetTypeActivated(node.bSelected || node.hasSubSel, node.data.key, node.data.title, node.childList)
            },
            onSelect: function(select, node) {
                onAssetTypeSelected(select, node.data.key, node.data.title, node.childList)
            }
        })
    }
    $("#accordion1").accordion({
        autoHeight: false,
        collapsible: false
    });
    $("#accordion2").accordion({
        autoHeight: false,
        collapsible: false,
        icons: {
            header: null,
            headerSelected: null
        }
    });
    $("#accordion3").accordion({
        autoHeight: false,
        collapsible: true,
        active: false,
        icons: {
            header: null,
            headerSelected: null
        }
    });
    $("#accordion2").accordion("disable");
    $("#accordion3").accordion("disable");
    if (autoComplete_HasData("lstUsers")) {
        var users = users_GetNotInRole(role.Id);
        $("#lstUsers").autocomplete("option", {
            source: users
        })
    } else activateUserAutoComplete(role.Id);
    if (autoComplete_HasData("lstReports")) {
        var reports = reports_GetNotInRole(role.Id);
        $("#lstReports").autocomplete("option", {
            source: reports
        })
    } else activateReportAutoComplete(role.Id);
    html = [];
    $.each(role.Members, function(index, item) {
        html.push("<option value='" + item.MemberId + "'>" + item.MemberName + "</option>")
    });
    $("#lstRoleMembers").html(html.join(""));
    html = [];
    $.each(role.Reports, function(index, item) {
        html.push("<option value='" + item.ReportId + "'>" + item.ReportName + "</option>")
    });
    $("#lstRoleReports").html(html.join(""));
    $("#tdCreated").html(getActionDateTime(role.Created));
    $("#tdCreatedBy").html(getActionCreator(role.CreatedBy));
    $("#tdModified").html(getActionDateTime(role.LastModified));
    $("#tdModifiedBy").html(getActionCreator(role.LastModifiedBy));
    $("#tdDeleted").html(getActionDateTime(role.Deleted));
    if (role.Deleted !== null && role.Deleted != "") {
        $("#btnDelete").val("Undelete Role");
        $("#btnDelete").attr("onClick", "onRoleUndeleteClicked()")
    } else {
        $("#btnDelete").val("Delete Role");
        $("#btnDelete").attr("onClick", "onRoleDeleteClicked()")
    }
    $("#tdDeletedBy").html(getActionCreator(role.DeletedBy));
    $("#tblButtons").show();
    toggleButtons("deleteButtons", true);
    toggleButtons("cloneButtons", true);
    toggleButtons("killButtons", USER_ACTUAL.IsAdmin);
    $("#accordion").show();
    return true
}

function buildAssetTypeUl(role) {
    var html = [];
    $.each(assetTypes_GetForDynaTree(), function(index, item) {
        var relation = getRoleAssetTypeRelation(role, item.key);
        if (relation !== null && relation != undefined) html.push("<li id='" + item.key + "' class='selected'>" + item.value);
        else html.push("<li id='" + item.key + "'>" + item.value);
        var children = assetType_GetChildrenOnly(item.key);
        if (children !== null && children.length > 0) {
            html.push("<ul>");
            $.each(children, function(subIndex, subItem) {
                relation = getRoleAssetTypeRelation(role, subItem.key);
                if (relation !== null && relation != undefined) html.push("<li id='" + subItem.key + "' class='selected'>" + subItem.value);
                else html.push("<li id='" + subItem.key + "'>" + subItem.value)
            });
            html.push("</ul>")
        }
    });
    return html.join("")
}

function onKillClicked() {
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html("Permanently destroy the '" + SELECTED_ITEM.Name + "' role?<br/><br/>This action can not be undone.");
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
                if (role_Kill(SELECTED_ITEM.Id, USER_ACTUAL.Id)) result = SELECTED_ITEM.Name + " has been permanently deleted from the system";
                else result = "Failed to kill the '" + SELECTED_ITEM.Name + "' role.";
                $(this).dialog("close");
                showMessage("Result", result, true, 400, true)
            }
        }
    });
    $dialog.dialog("open")
}

function permissions_SetEnabled(isEnabled) {
    $.each(permissions_Get(), function(index, item) {
        htmlControl_SetEnabled(item.key, isEnabled)
    })
}

function buildFilterPanel(role, assetTypeId) {
    var roleAssetTypeRelation = getRoleAssetTypeRelation(role, assetTypeId),
        html = [];
    html.push('<input id="chkInherit" type="checkbox" class="ui-widget" onChange="onInheritAccessCheckChanged()"></input><label id="lblInherit" class="ui-widget">Inherit filter from parent?</label>');
    html.push('<table id="tblFilters" style="width:100%">');
    html.push("<tr>");
    html.push("<td colspan=5><b>Filter Builder</b></td>");
    html.push("</tr>");
    html.push("<tr>");
    html.push("<td colspan=1>Attribute</td>");
    html.push("<td colspan=1>Operator</td>");
    html.push("<td colspan=3>Value</td>");
    html.push("</tr>");
    html.push("<tr>");
    html.push('<td style="width:40%">');
    html.push('<select id="lstAttributes" style="width:100%" onChange="onFilterAttributeChanged();"><option value="-1">Select</option>');
    $.each(properties_GetByAssetTypeId(assetTypeId, ART_INSTANCE), function(index, item) {
        html.push('<option value="' + item.key + '">' + item.value + "</option>")
    });
    html.push("</select>");
    html.push("</td>");
    html.push('<td style="width:10%">');
    html.push('<select id="lstOperators" style="width:100%;" disabled="disabled" class="ui-state-disabled" onChange="onFilterOperatorChanged();">');
    html.push('<option value="-1">Select</option>');
    $.each(operators_Get(), function(index, item) {
        html.push('<option value="' + item.key + '">' + item.value + "</option>")
    });
    html.push("</select>");
    html.push("</td>");
    html.push('<td style="width:40%">');
    html.push('<select id="lstAttributeValues" style="width:100%;" disabled="disabled" class="ui-state-disabled" onChange="onFilterAttributeValueChanged()"></select>');
    html.push("</td>");
    html.push('<td style="width:5%">');
    html.push('<input type="button" id="btnRemoveFilter" class="deleteButtons ui-state-disabled ui-corner-all" value="-" onClick="onFilterRemoveClicked()"/>');
    html.push("</td>");
    html.push('<td style="width:5%">');
    html.push('<input type="button" id="btnAddFilter" class="deleteButtons ui-state-disabled ui-corner-all" value="+" onClick="onFilterAddClicked()"/>');
    html.push("</td>");
    html.push("</tr>");
    html.push("<tr>");
    html.push("<td colspan=5>");
    html.push('<select id="lstFilters" size=10 style="width:100%" onChange="onFilterSelected()"></select>');
    html.push("</td>");
    html.push("</tr>");
    html.push("</tr>");
    html.push("<tr>");
    html.push("<td colspan=5><div>Filter Logic:");
    html.push('<input id="txtFilterLogic" type="text" style="width:100%" class="ui-widget" onChange="onFilterLogicChanged()"></input></div>');
    html.push("</td>");
    html.push("</tr>");
    html.push("</table>");
    $("#divFilters").html(html.join(""))
}

function displayAssetTypeDetails() {}

function displayFilters(role, assetTypeId) {
    var relation = getRoleAssetTypeRelation(role, assetTypeId),
        parentAssetTypeId = assetType_GetParentId(assetTypeId, true);
    if (parentAssetTypeId !== null && parentAssetTypeId != undefined && parentAssetTypeId != NULL_GUID) {
        $("#chkInherit").show();
        $("#lblInherit").show();
        var inheritAccess = relation.InheritAccess;
        checkBox_SetChecked("chkInherit", inheritAccess);
        htmlControl_SetEnabled("lstAttributes", !inheritAccess);
        htmlControl_SetEnabled("lstOperators", !inheritAccess);
        htmlControl_SetEnabled("lstAttributeValues", !inheritAccess);
        htmlControl_SetEnabled("lstFilters", !inheritAccess);
        htmlControl_SetEnabled("txtFilterLogic", !inheritAccess);
        relation = getRoleAssetTypeRelation(role, parentAssetTypeId)
    } else {
        $("#chkInherit").hide();
        $("#lblInherit").hide()
    }
    if (relation !== null && relation != undefined)
        if (relation.Filters !== null && relation.Filters != undefined) {
            var filterHtml = [];
            $.each(relation.Filters, function(index, filter) {
                var filterLine = [],
                    filterIsActive = filter.Deleted == null || filter.Deleted == undefined || filter.Deleted == "";
                if (filterIsActive && filter.PropertyId !== null) {
                    var propertyName = selectOptionText("lstAttributes", filter.PropertyId);
                    if (propertyName !== null && propertyName != "") filterLine.push(propertyName);
                    var operatorName = selectOptionText("lstOperators", filter.OperatorId);
                    if (operatorName !== null && operatorName != "") filterLine.push(" " + operatorName);
                    var propertyValue = propertyValue_GetDisplayValue(filter.PropertyId, filter.Value);
                    if (propertyValue !== null && propertyValue != "") filterLine.push(" " + propertyValue)
                }
                filterHtml.push('<option value="' + filter.Id + '">' + filterLine.join("") + "</option>")
            });
            $("#lstFilters").html(filterHtml.join(""))
        }
}

function getRoleAssetTypePermission(roleAssetTypeRelation, permissionId) {
    var returnValue = null;
    $.each(roleAssetTypeRelation.Permissions, function(index, item) {
        if (item.PermissionId == permissionId) {
            returnValue = item;
            return false
        }
    });
    return returnValue
}

function getRoleAssetTypeProcess(roleAssetTypeRelation, processId) {
    var returnValue = null;
    $.each(roleAssetTypeRelation.Processes, function(index, item) {
        if (item.ProcessId == processId) {
            returnValue = item;
            return false
        }
    });
    return returnValue
}

function getRoleAssetTypeRelation(role, assetTypeId) {
    var returnValue = null;
    $.each(role.AssetTypes, function(index, item) {
        if (item.AssetTypeId == assetTypeId) {
            returnValue = item;
            return false
        }
    });
    return returnValue
}

function insertIcon(iconName, propertyName, toolTip) {
    return '<span class="ui-icon ui-' + iconName + '" onMouseOver="showMessage(\'' + propertyName + "', '" + toolTip + '\', false, 400, false);" onMouseOut="hideMessage();" style="float: left; margin-right: .2em;"></span>'
}

function roleAssetTypeRelations_Equalize(parentRelation, childRelation) {}

function getRoleViewRelation(viewId) {
    var relations = ROLES_VIEWS;
    if (relations == null || relations.length == 0) return null;
    var relationCount = relations.length;
    for (var i = 0; i < relationCount; i++) {
        var relation = relations[i];
        if (relation.ViewId == viewId) return relation
    }
    return null
}

function getFilter(roleAssetTypeRelation, filterId) {
    var returnValue = null;
    $.each(roleAssetTypeRelation.Filters, function(index, item) {
        if (item.Id == filterId) {
            returnValue = item;
            return false
        }
    });
    return returnValue
}

function getActiveFilterCount(roleAssetTypeRelation) {
    var returnValue = 0;
    $.each(roleAssetTypeRelation.Filters, function(index, item) {
        if (item.Deleted == null || item.Deleted == undefined || item.Deleted == "") returnValue++
    });
    return returnValue
}

function onAddNewUser() {
    var $dialog = $("#newPickListValue");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    var html = [];
    html.push('<table style="width:100%">');
    html.push("<tr>");
    html.push('<td colspan=2 style="text-align:center;"><b>' + PROMPT_NEW_USER + "</b></td>");
    html.push("</tr>");
    html.push("<tr>");
    html.push('<td id="newPLVItemPrompt" style="width:25%">New User</td>');
    html.push('<td style="width:75%"><input type="text" id="txtNewPickListValue" style="width:100%" class="ui-widget ui-state-default" /></td>');
    html.push("</tr>");
    html.push("<tr>");
    html.push('<td id="tdNewPLV" colspan=2 style="text-align:center;"></td>');
    html.push("</tr>");
    html.push("</table>");
    $dialog.html(html.join(""));
    $dialog.dialog({
        width: 450,
        height: 250,
        modal: true,
        autoOpen: false,
        title: "Enter User's Email Address",
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            Cancel: function() {
                $("#newPickListValue").dialog("close")
            },
            Create: function() {
                toggleControl("Submit:eq(0)", false, true);
                var newPLV = $("#txtNewPickListValue").val().toLowerCase();
                if (isDirectoryUser(newPLV)) {
                    confirmNewUser();
                    $(this).dialog("close")
                } else showUserInfoDialog()
            }
        }
    });
    $dialog.dialog("open")
}

function onAssetTypeActivated(selected, assetTypeId, assetTypeName, childNodes) {
    _assetTypeName = assetTypeName;
    onAssetTypeSelectedOrActivated(selected, assetTypeId, assetTypeName, childNodes)
}

function onAssetTypeSelected(selected, assetTypeId, assetTypeName, childNodes) {
    _assetTypeName = assetTypeName;
    onAssetTypeSelectedOrActivated(selected, assetTypeId, assetTypeName, childNodes)
}

function onAssetTypeSelectedOrActivated(selected, assetTypeId, assetTypeName, childNodes) {
    if (SELECTED_ITEM == null || assetTypeId == null) return;
    var hasDef = true,
        hasIns = assetType_HasAssets(assetTypeId, ART_INSTANCE, true),
        defLabel = null,
        insLabel = null;
    if (hasDef && hasIns) {
        defLabel = assetType_GetReportingLabel(assetTypeId, ART_DEFINITION);
        insLabel = assetType_GetReportingLabel(assetTypeId, ART_INSTANCE);
        $("#lblDef").text(defLabel);
        $("#lblIns").text(insLabel);
        $("#defIns").show()
    } else {
        $("#defIns").hide();
        _assetRequestType = ART_DEFINITION
    }
    updateAssetTypeAccordionHeader(_assetTypeName, defLabel);
    if (selected) {
        $("#accordion2").accordion("enable");
        $("#accordion3").accordion("enable")
    } else {
        $("#accordion2").accordion("disable");
        $("#accordion3").accordion("disable")
    }
    var currentDateTime = getServerDateTime();
    SELECTED_ASSETTYPE_RELATION = getRoleAssetTypeRelation(SELECTED_ITEM, assetTypeId);
    var modified = false;
    if (SELECTED_ASSETTYPE_RELATION !== null) {
        if (selected) {
            if (SELECTED_ASSETTYPE_RELATION.Deleted !== null) modified = true
        } else if (SELECTED_ASSETTYPE_RELATION.Deleted == null) modified = true
    } else modified = true;
    if (modified) {
        if (SELECTED_ASSETTYPE_RELATION == null) {
            SELECTED_ASSETTYPE_RELATION = roleAssetTypeRelation_Create(SELECTED_ITEM.Id, assetTypeId, USER_ACTUAL.Id);
            SELECTED_ASSETTYPE_RELATION.InheritAccess = false;
            SELECTED_ITEM.AssetTypeMembers.push(SELECTED_ASSETTYPE_RELATION)
        }
        if (selected) {
            SELECTED_ASSETTYPE_RELATION.Deleted = null;
            SELECTED_ASSETTYPE_RELATION.DeletedBy = null
        } else {
            SELECTED_ASSETTYPE_RELATION.Deleted = currentDateTime;
            SELECTED_ASSETTYPE_RELATION.DeletedBy = USER_ACTUAL.Id
        }
        if (childNodes !== null && childNodes != undefined) $.each(childNodes, function(index, item) {
            var relation = getRoleAssetTypeRelation(SELECTED_ITEM, item.data.key);
            if (relation == null) {
                relation = roleAssetTypeRelation_Create(SELECTED_ITEM.Id, item.data.key, USER_ACTUAL.Id);
                SELECTED_ITEM.AssetTypeMembers.push(relation)
            }
            if (item.bSelected) {
                relation.Deleted = null;
                relation.DeletedBy = null
            } else {
                relation.Deleted = currentDateTime;
                relation.DeletedBy = USER_ACTUAL.Id
            }
        });
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = currentDateTime;
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
    if (selected) {
        var relationToShow = SELECTED_ASSETTYPE_RELATION,
            inheritAccess = relationToShow.InheritAccess;
        if (inheritAccess) {
            var parentAssetTypeId = assetType_GetParentId(assetTypeId, true);
            if (parentAssetTypeId !== null && parentAssetTypeId != undefined && parentAssetTypeId != NULL_GUID) {
                relationToShow = getRoleAssetTypeRelation(SELECTED_ITEM, parentAssetTypeId);
                permissions_SetEnabled(false)
            }
        } else permissions_SetEnabled(true);
        $("#accordion1").show();
        $("#accordion2").show();
        $("#accordion3").show();
        buildFilterPanel(SELECTED_ITEM, SELECTED_ASSETTYPE_RELATION.AssetTypeId);
        displayFilters(SELECTED_ITEM, SELECTED_ASSETTYPE_RELATION.AssetTypeId);
        if (relationToShow.FilterLogic !== null && relationToShow.FilterLogic != "") $("#txtFilterLogic").val(relationToShow.FilterLogic);
        else $("#txtFilterLogic").val("");
        $("#divPermissions > input").each(function() {
            checkBox_SetChecked(this.id, false)
        });
        if (relationToShow.Permissions !== null && relationToShow.Permissions != undefined) $.each(relationToShow.Permissions, function(index, item) {
            checkBox_SetChecked(item.PermissionId, true)
        });
        var availableViews = null;
        if (inheritAccess) availableViews = views_GetByAssetType(assetTypeId, ART_BOTH, false);
        else availableViews = views_GetByAssetType(relationToShow.AssetTypeId, ART_BOTH, false);
        html = [];
        html.push("<table style=width:100%>");
        html.push("<tr>");
        html.push('<th style="width:25%; text-align: center;">Auto-Approve?</th>');
        html.push('<th style="width:75%; text-align: left;">View Name</th>');
        $.each(availableViews, function(index, item) {
            html.push("<tr>");
            var roleViewRelation = getRoleViewRelation(item.key);
            if (roleViewRelation !== null && roleViewRelation.Deleted == null)
                if (inheritAccess) {
                    html.push('<td style="width:25%; text-align: center;"');
                    html.push('<input type="checkbox" checked="' + roleViewRelation.IsAutoApprove + '"/></td>');
                    html.push('<td style="width:75%">');
                    html.push("<input type='checkbox' disabled='disabled' id='" + item.key + "' value='" + item.key + "' checked='checked' class='ui-state-default' onchange='onRoleViewCheckChange(\"" + item.key + "\")'>" + item.value + "</input></td>")
                } else {
                    html.push('<td style="width:25%; text-align: center;">');
                    html.push('<input type="checkbox" checked="' + roleViewRelation.IsAutoApprove + '"/></td>');
                    html.push("<td style='width:75%'>");
                    html.push("<input type='checkbox' id='" + item.key + "' value='" + item.key + "' checked='checked' class='ui-state-default' onchange='onRoleViewCheckChange(\"" + item.key + "\")'>" + item.value + "</input><br/>")
                }
            else {
                html.push('<td style="width:25%; text-align: center;">');
                html.push('<input type="checkbox"/></td>');
                html.push("<td style='width:75%'>");
                html.push("<input type='checkbox' id='" + item.key + "' value='" + item.key + "' class='ui-state-default' onchange='onRoleViewCheckChange(\"" + item.key + "\")'>" + item.value + "</input><br/>")
            }
            html.push("</tr>")
        });
        html.push("</table>");
        $("#divViews").html(html.join(""));
        html = [];
        $.each(processes_GetByAssetTypeId(relationToShow.AssetTypeId, _assetRequestType), function(index, item) {
            var roleProcessRelation = getRoleAssetTypeProcess(relationToShow, item.key);
            if (roleProcessRelation !== null && roleProcessRelation.Deleted == null)
                if (inheritAccess) html.push("<input type='checkbox' disabled='disabled' id='" + item.key + "' value='" + item.key + "' checked='checked' class='ui-state-default' onchange='onRoleProcessCheckChange(\"" + item.key + "\")'>" + item.value + "</input><br/>");
                else html.push("<input type='checkbox' id='" + item.key + "' value='" + item.key + "' checked='checked' class='ui-state-default' onchange='onRoleProcessCheckChange(\"" + item.key + "\")'>" + item.value + "</input><br/>");
            else if (inheritAccess) html.push("<input type='checkbox' disabled='disabled' id='" + item.key + "' value='" + item.key + "' class='ui-state-default' onchange='onRoleProcessCheckChange(\"" + item.key + "\")'>" + item.value + "</input><br/>");
            else html.push("<input type='checkbox' id='" + item.key + "' value='" + item.key + "' class='ui-state-default' onchange='onRoleProcessCheckChange(\"" + item.key + "\")'>" + item.value + "</input><br/>")
        });
        if (html.length == 0) html.push("<label><i>No processes available for the selected asset type</i></label>");
        $("#divProcesses").html(html.join(""))
    } else {
        SELECTED_ASSETTYPE_RELATION = null;
        $("#lblFilters").html("Filters not applicable");
        $("#accordion2").hide();
        $("#accordion3").hide()
    }
}

function onCloneClicked() {
    var srcId = SELECTED_ITEM.Id;
    SELECTED_ITEM = role_Clone(SELECTED_ITEM.Id, USER_ACTUAL.Id);
    ROLES_VIEWS = roleViewRelationList_Clone(srcId, SELECTED_ITEM.Id, USER_ACTUAL.Id);
    displayRole(SELECTED_ITEM)
}

function confirmNewUser() {
    var closeNewItem = false,
        error = false,
        tempContent, dataType = DATA_TYPE_USER,
        lastName = "",
        firstName = "",
        newPLV = $("#txtNewPickListValue").val(),
        userData;
    if (isDirectoryUser(newPLV)) {
        var content = newPLV.split("@");
        userData = userData_Get(content[0]);
        if (!userData) {
            tempContent = "Invalid Id.";
            error = true
        } else $("#txtNewPickListValue").val(content[0].toLowerCase())
    }
    if (!error) {
        if (isDirectoryUser(newPLV)) {
            var content = newPLV.split("@");
            newPLV = content[0]
        }
        var userId = isUserInApp(newPLV);
        if (userId == null || userId == NULL_GUID) {
            if (isDirectoryUser(newPLV)) {
                lastName = userData.LastName;
                firstName = userData.FirstName
            } else {
                lastName = $("#txtNewPickListValueLastName").val();
                firstName = $("#txtNewPickListValueFirstName").val()
            }
            tempContent = "Add '" + lastName + ", " + firstName + "' to XDB?"
        } else {
            tempContent = "User is already in XDB and has been set to the selected item.";
            error = true
        }
    }
    var $dialog = $("#confirmNewUser");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html(tempContent);
    if (error)
        if (closeNewItem) $dialog.dialog({
            width: 450,
            height: 175,
            modal: true,
            autoOpen: false,
            title: "Error",
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            buttons: {
                Ok: function() {
                    $(this).dialog("close")
                }
            }
        });
        else $dialog.dialog({
            width: 450,
            height: 175,
            modal: true,
            autoOpen: false,
            title: "Error",
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            buttons: {
                Ok: function() {
                    $(this).dialog("close")
                }
            }
        });
    else $dialog.dialog({
        width: 450,
        height: 175,
        modal: true,
        autoOpen: false,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            Cancel: function() {
                $("#confirmNewUser").dialog("close")
            },
            Submit: function() {
                var newPLV = $("#txtNewPickListValue").val(),
                    newUserGUID = generateGuid(DATA_TYPE_USER);
                createUserInXdb(newUserGUID, newPLV, lastName, firstName, "x");
                var userDisplay = lastName + ", " + firstName + " [" + newPLV + "]";
                autoComplete_Insert("lstUsers", newUserGUID, userDisplay);
                $("#lstUsers-id").val(newUserGUID);
                $("#lstUsers").val(userDisplay);
                onRoleMemberAddClicked();
                $("#confirmNewUser").dialog("close");
                $("#newPickListValue").dialog("close")
            }
        }
    });
    $dialog.dialog("open")
}

function onAssetRequestTypeChanged(value) {
    var defInsLabel = null;
    if (value == 0) {
        defInsLabel = $("#lblDef").text();
        _assetRequestType = ART_DEFINITION
    } else {
        defInsLabel = $("#lblIns").text();
        _assetRequestType = ART_INSTANCE
    }
    updateAssetTypeAccordionHeader(_assetTypeName, defInsLabel)
}

function onDeletedItemsChanged() {
    activateAutoCompletes(ITEM_TYPE_ROLE, checkBox_GetChecked("chkDeletedItems"))
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

function onFilterAddClicked() {
    if (SELECTED_ASSETTYPE_RELATION == null) return;
    var propertyId = $("#lstAttributes").val(),
        operatorId = $("#lstOperators").val(),
        propertyValue = $("#lstAttributeValues").val();
    if (propertyId == null || propertyId == "" || propertyId == "-1") {
        showMessage("Error", "No attribute selected", true, 400, true);
        return false
    }
    if (operatorId == null || operatorId == "" || operatorId == "-1") {
        showMessage("Error", "No operator selected", true, 400, true);
        return false
    }
    if (propertyValue == null || propertyValue == "" || propertyValue == "-1") {
        showMessage("Error", "No attribute value selected", true, 400, true);
        return false
    }
    var filter = {
            Id: generateGuid(null),
            Created: getServerDateTime(),
            CreatedBy: USER_ACTUAL.Id,
            Deleted: null,
            DeletedBy: null,
            RoleId: SELECTED_ITEM.Id,
            AssetTypeId: SELECTED_ASSETTYPE_RELATION.AssetTypeId,
            PropertyId: propertyId,
            OperatorId: operatorId,
            Value: propertyValue,
            IsIncludesFilter: true,
            LogicalOperatorId: null,
            Order: SELECTED_ASSETTYPE_RELATION.Filters.length,
            IsNew: true,
            IsDirty: true
        },
        propertyText = selectOptionText("lstAttributes", propertyId),
        operatorText = selectOptionText("lstOperators", operatorId),
        valueText = selectOptionText("lstAttributeValues", propertyValue),
        filterText = propertyText + " " + operatorText + " " + valueText,
        html = [];
    html.push('<option value="' + filter.Id + '">' + filterText + "</option>");
    $("#lstFilters").append(html.join(""));
    SELECTED_ASSETTYPE_RELATION.Filters.push(filter);
    updateFilterLogic(SELECTED_ASSETTYPE_RELATION);
    SELECTED_ASSETTYPE_RELATION.IsDirty = true;
    SELECTED_ITEM.IsDirty = true;
    if (!SELECTED_ITEM.IsNew) {
        SELECTED_ITEM.LastModified = getServerDateTime();
        SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
    }
    itemModified(true)
}

function onFilterAttributeChanged() {
    var selectedAttributeId = $("#lstAttributes").val(),
        property = property_Get(selectedAttributeId);
    toggleControl("lstOperators", true, false);
    var html = [];
    html.push('<option value="-1">[Select]</option>');
    if (property.RoleId !== null) html.push('<option value="USER_CURRENT">USER_CURRENT</option>');
    $.each(dictionaryValues_GetForProperty(selectedAttributeId), function(index, item) {
        html.push('<option value="' + item.key + '">' + item.value + "</option>")
    });
    $("#lstAttributeValues").html(html.join(""))
}

function onFilterLogicChanged() {
    if (SELECTED_ASSETTYPE_RELATION == null || SELECTED_ASSETTYPE_RELATION == undefined) return;
    var newValue = $("#txtFilterLogic").val();
    if (SELECTED_ASSETTYPE_RELATION.FilterLogic != newValue) {
        SELECTED_ASSETTYPE_RELATION.FilterLogic = newValue;
        SELECTED_ASSETTYPE_RELATION.IsDirty = true;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onFilterOperatorChanged() {
    toggleControl("lstAttributeValues", true, false)
}

function onFilterRemoveClicked() {
    if (SELECTED_ASSETTYPE_RELATION == null) return;
    var filterId = $("#lstFilters").val();
    if (filterId !== null && filterId != undefined && filterId != "") {
        var filter = getFilter(SELECTED_ASSETTYPE_RELATION, filterId);
        if (filter !== null && filter != undefined) {
            htmlSelect_RemoveOptionByValue("lstFilters", filterId);
            filter.Deleted = getServerDateTime();
            filter.DeletedBy = USER_ACTUAL.Id;
            filter.IsDirty = true;
            updateFilterLogic(SELECTED_ASSETTYPE_RELATION);
            SELECTED_ASSETTYPE_RELATION.IsDirty = true;
            SELECTED_ITEM.IsDirty = true;
            if (!SELECTED_ITEM.IsNew) {
                SELECTED_ITEM.LastModified = getServerDateTime();
                SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
            }
            itemModified(true)
        }
    }
}

function onFilterSelected() {
    toggleControl("btnRemoveFilter", true, false)
}

function onFilterAttributeValueChanged() {
    toggleControl("btnAddFilter", true, false)
}

function onInheritAccessCheckChanged() {
    if (SELECTED_ASSETTYPE_RELATION == null) return;
    var checked = checkBox_GetChecked("chkInherit");
    if (checked != SELECTED_ASSETTYPE_RELATION.InheritAccess) {
        SELECTED_ASSETTYPE_RELATION.InheritAccess = checked;
        SELECTED_ASSETTYPE_RELATION.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
    htmlControl_SetEnabled("lstAttributes", !checked);
    htmlControl_SetEnabled("lstOperators", !checked);
    htmlControl_SetEnabled("lstAttributeValues", !checked);
    htmlControl_SetEnabled("lstFilters", !checked)
}

function onIsActiveChanged() {
    var isChecked = checkBox_GetChecked("chkIsActive");
    if (isChecked != SELECTED_ITEM.IsActive) {
        SELECTED_ITEM.IsActive = isChecked;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
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
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onRoleMemberSelected() {
    toggleButtons("removeUser", true)
}

function onRoleMemberAddClicked() {
    var userName = $("#lstUsers").val(),
        userId = null,
        source = $("#lstUsers").autocomplete("option", "source"),
        l = source.length;
    for (var i = 0; i < l; i++)
        if (source[i].value == userName) {
            userId = source[i].key;
            found = true;
            break
        } if (userId !== null && userId != undefined && userId != "") {
        var currentDateTime = getServerDateTime(),
            roleMember = {
                Id: generateGuid(null),
                MemberId: userId,
                MemberName: userName,
                Created: currentDateTime,
                CreatedBy: USER_ACTUAL.Id,
                IsNew: true,
                IsDirty: true,
                IsDeleted: false
            };
        SELECTED_ITEM.Members.push(roleMember);
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        autoComplete_RemoveByValue("lstUsers", userId);
        $("#lstUsers").val("");
        $("#lstUsers-id").val("");
        toggleButtons("addUser", false);
        htmlSelect_InsertOption("lstRoleMembers", userId, userName, false);
        itemModified(true);
        if (!SELECTED_ITEM.IsNew && PROMPT_ADD_USER_SHOW) {
            var result = userName + PROMPT_ADD_USER;
            showMessage("Result", result, true, 400, true);
            PROMPT_ADD_USER_SHOW = false
        }
    }
}

function onRoleMemberRemoveClicked() {
    var memberId = $("#lstRoleMembers").val(),
        memberName = selectOptionText("lstRoleMembers", memberId),
        $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html("Delete " + memberName + " from this role?");
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
                if (removeMemberFromRole(memberId)) result = memberName + " removed from role.<br/><br/>Role must be saved to make this change effective.";
                else result = "Failed to remove " + memberName + " from the role";
                $(this).dialog("close");
                showMessage("Result", result, true, 400, true)
            }
        }
    });
    $dialog.dialog("open")
}

function onRolePermissionCheckChange(permissionId) {
    var modified = false;
    if (permissionId !== null && permissionId != "") {
        var isChecked = checkBox_GetChecked(permissionId);
        if (isChecked) {
            var relation = {
                Id: generateGuid(null),
                RoleId: SELECTED_ITEM.Id,
                AssetTypeId: SELECTED_ASSETTYPE_RELATION.AssetTypeId,
                PermissionId: permissionId,
                Created: getServerDateTime(),
                CreatedBy: USER_ACTUAL.Id,
                Deleted: null,
                DeletedBy: null,
                IsDirty: true,
                IsNew: true
            };
            SELECTED_ASSETTYPE_RELATION.Permissions.push(relation);
            modified = true
        } else {
            var relation = getRoleAssetTypePermission(SELECTED_ASSETTYPE_RELATION, permissionId);
            if (relation !== null && relation != undefined) {
                relation.Deleted = getServerDateTime();
                relation.DeletedBy = USER_ACTUAL.Id;
                relation.IsDirty = true;
                modified = true
            }
        }
        if (modified) {
            SELECTED_ASSETTYPE_RELATION.IsDirty = true;
            SELECTED_ITEM.IsDirty = true;
            if (!SELECTED_ITEM.IsNew) {
                SELECTED_ITEM.LastModified = getServerDateTime();
                SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
            }
            itemModified(true)
        }
    }
}

function onRoleDeleteClicked() {
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html("Delete the '" + SELECTED_ITEM.Name + "' role?<br/><br/>This action can be undone if the role is needed later.");
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
                if (role_Save(SELECTED_ITEM)) {
                    $("#btnDelete").val("Undelete Role");
                    $("#btnDelete").attr("onClick", "onRoleUndeleteClicked();");
                    result = SELECTED_ITEM.Name + " has been deleted from the system.<br/><br/>It can be undeleted at a later time if needed.";
                    showMessage("Result", result, true, 400, true)
                } else if (ERR_ID != null) displayError(ERR_ID);
                else {
                    result = "Failed to delete the '" + SELECTED_ITEM.Name + "' role from the system.";
                    showMessage("Result", result, true, 400, true)
                }
                $(this).dialog("close")
            }
        }
    });
    $dialog.dialog("open")
}

function onRoleReportAddClicked() {
    var reportName = $("#lstReports").val(),
        reportId = null,
        source = $("#lstReports").autocomplete("option", "source");
    for (var i = 0; i < source.length; i++)
        if (source[i].value == reportName) {
            reportId = source[i].key;
            found = true;
            break
        } if (reportId !== null && reportId != undefined && reportId != "") {
        var roleReport = {
            Id: generateGuid(null),
            ReportId: reportId,
            ReportName: "",
            Created: getServerDateTime(),
            CreatedBy: USER_ACTUAL.Id,
            IsNew: true,
            IsDirty: true,
            IsDeleted: false
        };
        SELECTED_ITEM.Reports.push(roleReport);
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        autoComplete_RemoveByValue("lstReports", reportId);
        $("#lstReports").val("");
        $("#lstReports-id").val("");
        toggleButtons("addReport", false);
        htmlSelect_InsertOption("lstRoleReports", reportId, reportName, false);
        itemModified(true);
        var result = '<i>"' + reportName + '"</i> added to role.<br/><br/>Role must be saved to make this change effective.';
        showMessage("Result", result, true, 400, true)
    }
}

function onRoleReportRemoveClicked() {
    var reportId = $("#lstRoleReports").val(),
        reportName = selectOptionText("lstRoleReports", reportId),
        $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html('Delete <i>"' + reportName + '"</i> from this role?');
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
                if (removeReportFromRole(reportId)) result = '<i>"' + reportName + '"</i> removed from role.<br/><br/>Role must be saved to make this change effective.';
                else result = 'Failed to remove <i>"' + reportName + '"</i> from the role';
                $(this).dialog("close");
                showMessage("Result", result, true, 400, true)
            }
        }
    });
    $dialog.dialog("open")
}

function onRoleReportSelected() {
    toggleButtons("addReport", false);
    toggleButtons("removeReport", true)
}

function onRoleSaveClicked() {
    if (!roleIsValid(SELECTED_ITEM)) return false;
    var wasNewItem = SELECTED_ITEM.IsNew,
        saved = role_Save(SELECTED_ITEM) && role_SaveViewRelations(ROLES_VIEWS) && role_SaveViewRelations(ROLES_PROCESSES);
    if (saved) {
        clearForm_Role();
        SELECTED_ITEM = role_Get(SELECTED_ITEM.Id);
        displayRole(SELECTED_ITEM);
        if (wasNewItem) {
            autoComplete_SwapDatasourceAsIs(roles_GetDictionary(USER_ACTUAL.Id), "lstItems");
            $("#lstItems").val(SELECTED_ITEM.Name);
            $("#lstItems-id").val(SELECTED_ITEM.Id)
        }
        toggleButtons("resetButtons", false);
        toggleButtons("submitButtons", false);
        showMessage("Result", "Role saved successfully", true, 400, true)
    } else if (ERR_ID != null) displayError(ERR_ID);
    else showMessage("Result", "An error occured while saving the role", true, 400, true)
}

function onRoleUndeleteClicked() {
    var result = null;
    SELECTED_ITEM.Deleted = null;
    SELECTED_ITEM.DeletedBy = null;
    SELECTED_ITEM.LastModified = getServerDateTime();
    SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id;
    if (role_Save(SELECTED_ITEM)) {
        $("#btnDelete").val("Delete Role");
        $("#btnDelete").attr("onClick", "onRoleDeleteClicked();");
        result = SELECTED_ITEM.Name + " has been undeleted from the system."
    } else result = "Failed to undelete the '" + SELECTED_ITEM.Name + "' role from the system.";
    showMessage("Result", result, true, 400, true)
}

function onRoleViewCheckChange(viewId) {
    if (SELECTED_ITEM == null) return;
    if (viewId !== null && viewId != "") {
        var isChecked = checkBox_GetChecked(viewId),
            relation = getRoleViewRelation(viewId);
        if (relation == null) {
            relation = {
                Id: generateGuid(null),
                RoleId: SELECTED_ITEM.Id,
                ViewId: viewId,
                IsAutoApprove: true,
                Created: getServerDateTime(),
                CreatedBy: USER_ACTUAL.Id,
                IsNew: true
            };
            ROLES_VIEWS.push(relation)
        }
        if (isChecked) {
            relation.Deleted = null;
            relation.DeletedBy = null
        } else {
            relation.Deleted = getServerDateTime();
            relation.DeletedBy = USER_ACTUAL.Id
        }
        relation.IsDirty = true;
        SELECTED_ITEM.IsDirty = true;
        if (!SELECTED_ITEM.IsNew) {
            SELECTED_ITEM.LastModified = getServerDateTime();
            SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
        }
        itemModified(true)
    }
}

function onRoleProcessCheckChange(processId) {
    var modified = false;
    if (processId !== null && processId != "") {
        var isChecked = checkBox_GetChecked(processId);
        if (isChecked) {
            var relation = {
                Id: generateGuid(null),
                RoleId: SELECTED_ITEM.Id,
                AssetTypeId: SELECTED_ASSETTYPE_RELATION.AssetTypeId,
                ProcessId: processId,
                Created: getServerDateTime(),
                CreatedBy: USER_ACTUAL.Id,
                Deleted: null,
                DeletedBy: null,
                IsDirty: true,
                IsNew: true
            };
            ROLES_PROCESSES.push(relation);
            modified = true
        } else {
            var relation = getRoleAssetTypeProcess(SELECTED_ASSETTYPE_RELATION, processId);
            if (relation !== null && relation != undefined) {
                relation.Deleted = getServerDateTime();
                relation.DeletedBy = USER_ACTUAL.Id;
                relation.IsDirty = true;
                modified = true
            }
        }
        if (modified) {
            SELECTED_ASSETTYPE_RELATION.IsDirty = true;
            SELECTED_ITEM.IsDirty = true;
            if (!SELECTED_ITEM.IsNew) {
                SELECTED_ITEM.LastModified = getServerDateTime();
                SELECTED_ITEM.LastModifiedBy = USER_ACTUAL.Id
            }
            itemModified(true)
        }
    }
}

function removeMemberFromRole(memberId) {
    var deleted = false,
        count = 0;
    $.each(SELECTED_ITEM.Members, function(index, item) {
        if (item.MemberId == memberId) {
            item.IsDeleted = true;
            item.IsDirty = true;
            SELECTED_ITEM.IsDirty = true;
            deleted = true;
            return false
        }
        count++
    });
    if (deleted) {
        htmlSelect_RemoveOptionByValue("lstRoleMembers", memberId);
        toggleButtons("submitButtons", true);
        toggleButtons("resetButtons", true);
        return true
    } else return false
}

function removeReportFromRole(reportId) {
    var deleted = false,
        count = 0;
    $.each(SELECTED_ITEM.Reports, function(index, item) {
        if (item.ReportId == reportId) {
            item.IsDeleted = true;
            item.IsDirty = true;
            SELECTED_ITEM.IsDirty = true;
            deleted = true;
            return false
        }
        count++
    });
    if (deleted) {
        htmlSelect_RemoveOptionByValue("lstRoleReports", reportId);
        toggleButtons("submitButtons", true);
        toggleButtons("resetButtons", true);
        return true
    } else return false
}

function roleIsValid(role) {
    $.each(role.AssetTypes, function(index, item) {
        if (item.Filters !== null && item.FilterLogic == "") {
            var textToShow = "Filter defined for " + item.Id + ", but no filter logic defined.";
            showMessage("Error", textToShow, true, 400, true);
            return false
        }
    });
    return true
}

function createItem() {
    SELECTED_ITEM = role_Create(USER_ACTUAL.Id);
    if (SELECTED_ITEM !== null && SELECTED_ITEM != undefined) {
        ROLES_VIEW = roleViewRelationList();
        displayRole(SELECTED_ITEM)
    } else showMessage("Error", "Request to create a new role failed", true, 400, true)
}

function updateFilterLogic(roleAssetTypeRelation) {
    var filterCount = 0;
    if (SELECTED_ASSETTYPE_RELATION.Filters !== null && SELECTED_ASSETTYPE_RELATION.Filters != "") filterCount = getActiveFilterCount(SELECTED_ASSETTYPE_RELATION);
    var html = [];
    if (filterCount == 0) SELECTED_ASSETTYPE_RELATION.FilterLogic = "({0})";
    else {
        html.push("(");
        for (var i = 0; i < filterCount; i++)
            if (i == filterCount - 1) html.push("({" + i + "})");
            else html.push("({" + i + "}) AND ");
        html.push(")");
        SELECTED_ASSETTYPE_RELATION.FilterLogic = html.join("")
    }
    $("#txtFilterLogic").val(SELECTED_ASSETTYPE_RELATION.FilterLogic)
}

function updateAssetTypeAccordionHeader(assetTypeName, defInsLabel) {
    if (defInsLabel != null && defInsLabel != "") $("#atLabel").html("Asset Types, Role Filters, & Permissions - " + assetTypeName + " - " + defInsLabel);
    else $("#atLabel").html("Asset Types, Role Filters, & Permissions - " + assetTypeName)
}
