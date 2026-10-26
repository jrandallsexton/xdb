var propertiesModified = false,
    CRFT = null,
    REMOVE_PROPERTY = false;

function buildFilterContent() {
    var html = [];
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
    var art = null;
    if (SELECTED_ITEM.IsInstance) art = ART_INSTANCE;
    else art = ART_DEFINITION;
    $.each(properties_GetByAssetTypeId(SELECTED_ITEM.AssetTypeId, art), function(index, item) {
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
    html.push('<input type="text" style="width:100%;display:none;" class="datepicker" id="dtpValue" />');
    html.push('<select id="lstAttributeValues" style="width:100%;" disabled="disabled" class="ui-state-disabled" onChange="onFilterAttributeValueChanged()"></select>');
    html.push("</td>");
    html.push('<td style="width:5%">');
    html.push('<input type="button" disabled="disabled" id="btnRemoveFilter" class="deleteButtons ui-state-disabled ui-corner-all" value="-" onClick="onFilterRemoveClicked()"/>');
    html.push("</td>");
    html.push('<td style="width:5%">');
    html.push('<input type="button" disabled="disabled" id="btnAddFilter" class="deleteButtons ui-state-disabled ui-corner-all" value="+" onClick="onFilterAddClicked()"/>');
    html.push("</td>");
    html.push("</tr>");
    html.push("<tr>");
    html.push("<td colspan=5>");
    html.push('<select id="lstFilters" size=10 style="width:100%" onChange="onFilterSelected()"></select>');
    html.push("</td>");
    html.push("</tr>");
    html.push("</tr>");
    html.push("<tr>");
    html.push("<td colspan=1>Filter Logic:</td>");
    html.push("<td colspan=4>");
    html.push('<input id="txtFilterLogic" type="text" style="width:100%" class="ui-widget" onChange="onFilterLogicChanged()"></input>');
    html.push("</td>");
    html.push("</tr>");
    html.push("</table>");
    $("#divFilters").html(html.join(""))
}

function clearForm_Report() {
    $("#lblId").text("");
    $("#txtName").val("");
    $("#lstAssetTypes").val("-1");
    $("#lstProperties").html("");
    $("#values").html("");
    $("#lstGroupBy").val("");
    $("#lstFilters").html("");
    $("#txtFilterLogic").val("");
    $("#tdCreated").html("");
    $("#tdCreatedBy").html("")
}

function displayFilters(filters) {
    if (filters !== null && filters != undefined) {
        var filterHtml = [];
        $.each(filters, function(index, filter) {
            var filterLine = [],
                propertyName = selectOptionText("lstAttributes", filter.PropertyId);
            if (propertyName !== null && propertyName != "") filterLine.push(propertyName);
            var operatorName = selectOptionText("lstOperators", filter.OperatorId);
            if (operatorName !== null && operatorName != "") filterLine.push(" " + operatorName);
            var propertyValue = null;
            if (filter.Value == "") propertyValue = "NULL";
            else propertyValue = propertyValue_GetDisplayValue(filter.PropertyId, filter.Value);
            if (propertyValue !== null && propertyValue != "") filterLine.push(" " + propertyValue);
            filterHtml.push('<option value="' + filter.Id + '">' + filterLine.join("") + "</option>")
        });
        $("#lstFilters").html(filterHtml.join(""))
    }
}

function createGroupingContent(report) {}

function createPropertiesContent() {
    if (SELECTED_ITEM.AssetTypeId !== null && SELECTED_ITEM.AssetTypeId !== NULL_GUID) {
        var html = [],
            htmlGroupBy = [];
        html.push("<option value='-1'>[Select]</option>");
        html.push("<option value='0'>[Custom]</option>");
        htmlGroupBy.push("<option value='-1'>[Select]</option>");
        var art = null;
        if (SELECTED_ITEM.IsInstance) art = ART_INSTANCE;
        else art = ART_DEFINITION;
        if (SELECTED_ITEM.IsNew) $.each(properties_GetByAssetTypeId(SELECTED_ITEM.AssetTypeId, art), function(index, item) {
            html.push('<option value="' + item.key + '">' + item.value + "</option>")
        });
        else {
            $.each(report_GetUnusedProperties(SELECTED_ITEM.Id), function(index, item) {
                html.push('<option value="' + item.key + '">' + item.value + "</option>")
            });
            $.each(SELECTED_ITEM.ReportProperties, function(index, item) {
                htmlGroupBy.push('<option value="' + item.PropertyId + '">' + item.PropertyName + "</option>")
            })
        }
        $("#lstProperties").html(html.join(""));
        $("#lstGroupBy").html(htmlGroupBy.join(""));
        createPropertyPanels()
    }
}

function createPropertyPanels() {
    var html = [],
        count = SELECTED_ITEM.ReportProperties.length;
    for (var outerIndex = 0; outerIndex < count; outerIndex++) $.each(SELECTED_ITEM.ReportProperties, function(index, item) {
        if (item.Index == outerIndex) {
            var displayValue = null;
            if (item.CustomReportFieldType == CRFT_NOT_APPLICABLE)
                if (item.SubPropertyName !== null && item.SubPropertyName != "")
                    if (item.Label !== null && item.Label != "") displayValue = item.PropertyName + "." + item.SubPropertyName + " AS [" + item.Label + "]";
                    else displayValue = item.PropertyName + "." + item.SubPropertyName;
            else displayValue = item.Label == null || item.Label == "" ? item.PropertyName : item.PropertyName + " AS [" + item.Label + "]";
            else {
                displayValue = item.Label;
                if (displayValue == null || displayValue == "") displayValue = item.PropertyName
            }
            var itemId = item.Id.toLowerCase();
            html.push("<li id='" + itemId + "' class='ui-state-default'>" + displayValue);
            html.push("<div style='float:right'>");
            html.push('<span title="Remove this property from the report" style="float:right;" class="ui-icon ui-icon-trash" onclick="onRemovePropertyClicked(\'' + itemId + "')\"></span>");
            html.push('<span title="Create a label (alias) for this property in the report" style="float:left;" class="ui-icon ui-icon-tag" onclick="onLabelPropertyClicked(\'' + itemId + "')\"></span>");
            html.push("</div></li>");
            return false
        }
    });
    $("#values").html(html.join(""))
}

function onLabelPropertyClicked(reportPropertyId) {}

function createCustomProperty_Calculated(propertyIds, label, logic, index) {
    var property = {};
    property.Id = generateGuid(null);
    property.PropertyId = NULL_GUID;
    property.SubPropertyId = NULL_GUID;
    property.PropertyName = null;
    property.SubPropertyName = null;
    property.CustomReportFieldType = CRFT_CALCULATED;
    property.TimeElapsedType = TET_NOT_APPLICABLE;
    property.TimeElapsedFormat = 0;
    property.TimeElapsedSpecificDate = null;
    property.Label = label;
    property.Logic = logic;
    property.PropertyIds = propertyIds;
    property.Index = index;
    property.IsGrouping = false;
    return property
}

function createCustomProperty_Elapsed(timeElapsedType, timeElapsedFormat, specificDate, label, index, propertyId) {
    var property = {};
    property.Id = generateGuid(null);
    property.PropertyId = propertyId;
    property.SubPropertyId = NULL_GUID;
    property.PropertyName = null;
    property.SubPropertyName = null;
    property.CustomReportFieldType = CRFT_TIME_ELAPSED;
    property.TimeElapsedType = timeElapsedType;
    property.TimeElapsedFormat = timeElapsedFormat;
    property.TimeElapsedSpecificDate = specificDate;
    property.Label = label;
    property.Logic = null;
    property.PropertyIds = null;
    property.Index = index;
    property.IsGrouping = false;
    return property
}

function createNewReportProperty(reportId, propertyId, propertyName, index) {
    var property = {};
    property.Id = generateGuid(null);
    property.PropertyId = propertyId;
    property.SubPropertyId = NULL_GUID;
    property.PropertyName = propertyName;
    property.SubPropertyName = null;
    property.CustomReportFieldType = CRFT_NOT_APPLICABLE;
    property.TimeElapsedType = TET_NOT_APPLICABLE;
    property.TimeElapsedFormat = 0;
    property.TimeElapsedSpecificDate = null;
    property.Label = null;
    property.Logic = null;
    property.PropertyIds = null;
    property.Index = index;
    property.IsGrouping = false;
    return property
}

function createNewReportProperty_SubProperty(propertyId, propertyName, subPropertyId, subPropertyName, index) {
    var property = {};
    property.Id = generateGuid(null);
    property.PropertyId = propertyId;
    property.SubPropertyId = subPropertyId;
    property.PropertyName = propertyName;
    property.SubPropertyName = subPropertyName;
    property.CustomReportFieldType = CRFT_NOT_APPLICABLE;
    property.TimeElapsedType = TET_NOT_APPLICABLE;
    property.TimeElapsedFormat = 0;
    property.TimeElapsedSpecificDate = null;
    property.Label = null;
    property.Logic = null;
    property.PropertyIds = null;
    property.Index = index;
    property.IsGrouping = false;
    return property
}

function onAddCustomPropertyClicked() {
    var selectedValue = $("#lstFieldTypes").val();
    CRFT = parseInt(selectedValue);
    var refresh = false;
    switch (CRFT) {
        case CRFT_NOT_APPLICABLE:
            break;
        case CRFT_CALCULATED:
            var propertyIds = htmlSelect_GetAllIds("lstSelectedFields"),
                label = $("#txtReportLabel").val(),
                logic = $("#txtCalculationLogic").val(),
                index = SELECTED_ITEM.ReportProperties.length,
                reportProperty = createCustomProperty_Calculated(propertyIds, label, logic, index);
            SELECTED_ITEM.ReportProperties.push(reportProperty);
            refresh = true;
            break;
        case CRFT_CONCATENATION:
            break;
        case CRFT_TIME_ELAPSED:
            var sourcePropertyId = $("#lstFields").val(),
                timeElapsedType = $("input:radio[name=elapsedType]:checked").val(),
                timeElapsedFormat = $("input:radio[name=elapsedFormat]:checked").val(),
                specificDate = null;
            if (timeElapsedType == TET_SPECIFIC) specificDate = $("#dpTimeElapsed").val();
            var label = $("#txtReportLabel").val(),
                index = SELECTED_ITEM.ReportProperties.length,
                reportProperty = createCustomProperty_Elapsed(timeElapsedType, timeElapsedFormat, specificDate, label, index, sourcePropertyId);
            SELECTED_ITEM.ReportProperties.push(reportProperty);
            refresh = true;
            break;
        case CRFT_RANGE_LABEL:
            break;
        case CRFT_VALUE_LABEL:
            break
    }
    if (refresh) {
        createPropertiesContent(SELECTED_ITEM);
        propertiesModified = true;
        itemModified(true)
    }
}

function onAddPropertyClicked() {
    var propertyId = $("#lstProperties").val();
    if (propertyId !== null && propertyId != undefined && propertyId != "") {
        var propertyName = selectOption_GetCurrentText("lstProperties"),
            reportProperty = createNewReportProperty(SELECTED_ITEM.Id, propertyId.toLowerCase(), propertyName, SELECTED_ITEM.ReportProperties.length);
        SELECTED_ITEM.ReportProperties.push(reportProperty);
        if (REMOVE_PROPERTY) htmlSelect_RemoveOptionByValue("lstProperties", propertyId);
        htmlSelect_InsertOption("lstGroupBy", propertyId.toLowerCase(), propertyName, false);
        createPropertyPanels();
        propertiesModified = true;
        toggleControl("btnAddProperty", false, true);
        itemModified(true)
    }
}

function onAddSubPropertyClicked() {
    var propertyId = $("#lstProperties").val(),
        subPropertyId = $("#lstSubProperties").val(),
        validPropertyId = propertyId !== null && propertyId != undefined && propertyId != "",
        validSubPropId = subPropertyId !== null && subPropertyId != undefined && subPropertyId != "";
    if (validPropertyId && validSubPropId) {
        var propertyName = selectOption_GetCurrentText("lstProperties"),
            subPropertyName = selectOption_GetCurrentText("lstSubProperties"),
            rp = createNewReportProperty_SubProperty(propertyId, propertyName, subPropertyId, subPropertyName, SELECTED_ITEM.ReportProperties.length);
        SELECTED_ITEM.ReportProperties.push(rp);
        $("#lstSubProperties").val(null);
        $("#trSubProperties").hide();
        createPropertiesContent(SELECTED_ITEM);
        propertiesModified = true;
        itemModified(true)
    }
}

function onAssetTypeSelected() {
    var selectedAssetTypeId = $("#lstAssetTypes").val();
    if (selectedAssetTypeId == "-1") showMessage("Error", "You must select an asset type", true, 400, true);
    else {
        SELECTED_ITEM.AssetTypeId = selectedAssetTypeId;
        var hasDefinitions = assetType_HasAssets(selectedAssetTypeId, ART_DEFINITION, true),
            hasInstances = assetType_HasAssets(selectedAssetTypeId, ART_INSTANCE, true),
            defLabel = assetType_GetReportingLabel(selectedAssetTypeId, ART_DEFINITION),
            insLabel = assetType_GetReportingLabel(selectedAssetTypeId, ART_INSTANCE),
            html = [];
        if (hasDefinitions && hasInstances) {
            html.push('<input type="radio" id="opDef" name="defins" value="0" onChange="onDefinitionInstanceChanged()" />' + defLabel);
            html.push("<br/>");
            html.push('<input type="radio" id="opIns" name="defins" value="1" onChange="onDefinitionInstanceChanged()" />' + insLabel)
        } else if (hasDefinitions) html.push("<label><em>" + defLabel + " available only</em></label>");
        else html.push("<label><em>" + insLabel + " available only</em></label>");
        SELECTED_ITEM.IsInstance = hasInstances;
        $("#divRadio").html(html.join(""));
        $("#divAttributes").html(createPropertiesContent());
        buildFilterContent()
    }
}

function onCustomFieldAttributeSelected() {
    switch (CRFT) {
        case CRFT_CALCULATED:
            $("#trTimeElapsed").hide();
            $("#trTimeElapsedFormat").hide();
            $("#txtCalculationLogic").text("");
            $("#trCalculationLogic").show();
            $("#txtReportLabel").text("");
            $("#trReportLabel").show();
            $("#trSourceAttributes").show();
            break;
        case CRFT_CONCATENATION:
            $("#txtCalculationLogic").text("");
            $("#trCalculationLogic").hide();
            $("#trTimeElapsed").hide();
            $("#trTimeElapsedFormat").hide();
            $("#txtConcatenationLogic").text("");
            $("#trConcatenationLogic").show();
            $("#txtReportLabel").text("");
            $("#trReportLabel").show();
            $("#trSourceAttributes").show();
            break;
        case CRFT_TIME_ELAPSED:
            $("#txtConcatenationLogic").text("");
            $("#trConcatenationLogic").hide();
            $("#txtCalculationLogic").text("");
            $("#trCalculationLogic").hide();
            $("#trSourceAttributes").hide();
            $("#trTimeElapsed").show();
            $("#trTimeElapsedFormat").show();
            $("#txtReportLabel").text("");
            $("#trReportLabel").show();
            break;
        case CRFT_RANGE_LABEL:
            $("#txtConcatenationLogic").text("");
            $("#trConcatenationLogic").hide();
            $("#txtCalculationLogic").text("");
            $("#trCalculationLogic").hide();
            $("#trSourceAttributes").hide();
            $("#trTimeElapsed").hide();
            $("#trTimeElapsedFormat").hide();
            $("#txtReportLabel").text("");
            $("#trReportLabel").show();
            break;
        case CRFT_VALUE_LABEL:
            $("#txtCalculationLogic").text("");
            $("#trCalculationLogic").hide();
            $("#trSourceAttributes").hide();
            $("#txtReportLabel").text("");
            $("#trReportLabel").show();
            $("#trTimeElapsed").hide();
            break
    }
}

function onCustomFieldTypeSelected() {
    var selectedValue = $("#lstFieldTypes").val();
    CRFT = parseInt(selectedValue);
    if (selectedValue != -1) {
        var art = null;
        if (SELECTED_ITEM.IsInstance) art = ART_INSTANCE;
        else art = ART_DEFINITION;
        var html = [];
        html.push('<select id="lstFields" style="width:100%" onchange="onCustomFieldAttributeSelected()" ><option value="-1">[Select]</option>');
        $.each(properties_GetByECustomReportFieldType(selectedValue, SELECTED_ITEM.AssetTypeId, art), function(index, item) {
            html.push('<option value="' + item.key + '">' + item.value + "</option>")
        });
        html.push("</select>");
        $("#tdSourceAttributes").html(html.join(""));
        $("#trSourceAttribute").show()
    }
}

function onCustomSourceAttrChanged() {
    onCustomFieldAttributeSelected()
}

function onDefinitionInstanceChanged() {
    var value = $("input[name='defins']:checked").val();
    if (value == 0) SELECTED_ITEM.IsInstance = false;
    else SELECTED_ITEM.IsInstance = true;
    createPropertiesContent();
    $("#accordion").accordion("option", "active", 2)
}

function onFilterAddClicked() {
    if (SELECTED_ITEM == null) return;
    var propertyId = $("#lstAttributes").val(),
        operatorId = $("#lstOperators").val(),
        propertyValue = null,
        propertyDataType = property_GetDataType(propertyId);
    if (propertyDataType == DATA_TYPE_DATE) propertyValue = $("#dtpValue").val();
    else propertyValue = $("#lstAttributeValues").val();
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
    var filter = {};
    filter.Id = generateGuid(null);
    filter.PropertyId = propertyId;
    filter.OperatorId = operatorId;
    filter.Value = propertyValue;
    filter.Order = SELECTED_ITEM.Filters.length;
    filter.DisplayValue = "";
    filter.Property = "";
    filter.Operator = "";
    SELECTED_ITEM.Filters.push(filter);
    var propertyText = selectOptionText("lstAttributes", propertyId),
        operatorText = selectOptionText("lstOperators", operatorId),
        valueText = null;
    if (propertyDataType == DATA_TYPE_DATE) valueText = $("#dtpValue").val();
    else valueText = selectOptionText("lstAttributeValues", propertyValue);
    var filterText = propertyText + " " + operatorText + " " + valueText,
        html = [];
    html.push('<option value="' + filter.Id + '">' + filterText + "</option>");
    $("#lstFilters").append(html.join(""));
    updateFilterLogic();
    propertiesModified = true;
    itemModified(true)
}

function onFilterAttributeChanged() {
    var selectedAttributeId = $("#lstAttributes").val();
    if (selectedAttributeId == -1) {
        $("#lstOperators").val(-1);
        $("#lstAttributeValues").val(-1);
        toggleControl("lstOperators", false, false);
        toggleControl("lstAttributeValues", false, false)
    } else {
        toggleControl("lstOperators", false, false);
        toggleControl("lstAttributeValues", false, false);
        var html = [];
        html.push('<option value="-1">[Select]</option>');
        html.push('<option value="' + NULL_GUID + '">NULL</option>');
        var searchRequestType = ART_DEFINITION;
        if (SELECTED_ITEM.IsInstance) searchRequestType = ART_INSTANCE;
        if (property_GetDataType(selectedAttributeId) == DATA_TYPE_DATE) {
            $("#lstAttributeValues").hide();
            $("#dtpValue").show();
            $(".datepicker").datepicker({
                dateFormat: "dd-M-yy",
                constrainInput: true,
                showButtonPanel: true,
                changeMonth: true,
                changeYear: true,
                onClose: function(dateText, inst) {
                    if (dateText === null || dateText == "") return true;
                    try {
                        jQuery.datepicker.parseDate("dd-M-yy", dateText);
                        return true
                    } catch (e) {
                        this.focus();
                        return false
                    }
                }
            })
        } else {
            $("#lstAttributeValues").show();
            $("#dtpValue").hide();
            var potentialValues = dictionaryValues_GetForProperty(selectedAttributeId, SELECTED_ITEM.AssetTypeId);
            if (potentialValues !== null) {
                var length = potentialValues.length;
                for (var i = 0; i < length; i++) {
                    var displayValue = potentialValues[i].value;
                    html.push('<option value="' + potentialValues[i].key + '" title="' + displayValue + '">' + displayValue + "</option>")
                }
            }
        }
        $("#lstAttributeValues").html(html.join(""));
        toggleControl("lstOperators", true, false);
        toggleControl("lstAttributeValues", true, false)
    }
}

function onFilterAttributeValueChanged() {
    toggleControl("btnAddFilter", true, false)
}

function onFilterLogicChanged() {
    if (SELECTED_ITEM == null || SELECTED_ITEM == undefined) return;
    var newValue = $("#txtFilterLogic").val(),
        errors = filterLogicIsValid(SELECTED_ITEM.Filters, newValue);
    if (errors.length == 0) {
        if (SELECTED_ITEM.FilterLogic != newValue) {
            SELECTED_ITEM.FilterLogic = newValue;
            propertiesModified = true;
            itemModified(true)
        }
    } else {
        var msg = [];
        msg.push("Filter logic is invalid<br/>");
        $.each(errors, function(index, item) {
            msg.push(item + "<br/>")
        });
        toggleButtons("submitButtons", false);
        showMessage("Error", msg.join(""), true, 400, true);
        $("#txtFilterLogic").focus()
    }
}

function onFilterOperatorChanged() {
    toggleControl("lstAttributeValues", true, false)
}

function onFilterRemoveClicked() {
    var filterId = $("#lstFilters").val();
    if (filterId !== null && filterId != undefined && filterId != "") {
        var filter = getFilter(SELECTED_ITEM.Filters, filterId);
        if (filter !== null && filter != undefined) {
            htmlSelect_RemoveOptionByValue("lstFilters", filterId);
            var filterIndex = getFilterIndex(SELECTED_ITEM.Filters, filterId);
            SELECTED_ITEM.Filters.splice(filterIndex, 1);
            updateFilterLogic();
            propertiesModified = true;
            itemModified(true)
        }
    }
}

function onFilterSelected() {
    toggleControl("btnRemoveFilter", true, false)
}

function onKillReportClicked() {
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html("Permanently destroy the '" + SELECTED_ITEM.ReportName + "' report?<br/><br/>This action can not be undone.");
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
                if (report_Kill(SELECTED_ITEM.Id, USER_CURRENT.Id)) result = SELECTED_ITEM.ReportName + " has been permanently deleted from the system.";
                else result = "Failed to kill the '" + SELECTED_ITEM.ReportName + "' report.";
                $(this).dialog("close");
                clearForm_Report();
                SELECTED_ITEM = null;
                $("#lstItems").val("");
                $("#lstItems-id").val("");
                showMessage("Result", result, true, 400, true)
            }
        }
    });
    $dialog.dialog("open")
}

function onNameChanged() {
    var newValue = $("#txtName").val();
    if (newValue != SELECTED_ITEM.Name) {
        SELECTED_ITEM.ReportName = newValue;
        SELECTED_ITEM.IsDirty = true;
        itemModified(true)
    }
}

function onPropertyDropped(ui) {
    reOrderProperties()
}

function onReportDeleteClicked() {
    $("#messageDialog").dialog("destroy");
    $("#messageDialog").html("Delete the '" + SELECTED_ITEM.ReportName + "' report?<br/><br/>This action can be undone if the report is needed again.");
    $("#messageDialog").dialog({
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
                if (report_Save(SELECTED_ITEM)) {
                    $("#btnDelete").val("Undelete Report");
                    $("#btnDelete").attr("onClick", "onPropertyUndeleteClicked();");
                    result = SELECTED_ITEM.ReportName + " has been deleted from the system.<br/>It can be undeleted at a later time if needed."
                } else result = "Failed to delete the '" + SELECTED_ITEM.ReportName + "' report from the system.";
                $(this).dialog("close");
                showMessage("Result", result, true, 400, true)
            }
        }
    });
    $("#messageDialog").dialog("open")
}

function onReportSaveClicked() {
    var wasNewItem = SELECTED_ITEM.IsNew;
    if (report_Save(SELECTED_ITEM)) {
        if (propertiesModified) {
            report_ClearData(SELECTED_ITEM.Id);
            propertiesModified = false
        }
        SELECTED_ITEM = report_Get(USER_CURRENT.Id, SELECTED_ITEM.Id);
        displayReport(SELECTED_ITEM);
        if (wasNewItem) {
            autoComplete_SwapDatasourceAsIs(views_GetDictionary(true, true), "lstItems");
            $("#lstItems").val(SELECTED_ITEM.DisplayValue);
            $("#lstItems-id").val(SELECTED_ITEM.Id)
        }
        showMessage("Result", "Report saved successfully.", true, 400, true);
        toggleButtons("resetButtons", false);
        toggleButtons("submitButtons", false)
    } else {}
}

function onReportSelected(reportId) {
    var report = report_Get(USER_CURRENT.Id, reportId);
    SELECTED_ITEM = report;
    $("#accordion").show();
    displayReport(report)
}

function onRemovePropertyClicked(reportPropertyId) {
    var validSelection = reportPropertyId !== null && reportPropertyId != undefined && reportPropertyId != "";
    if (validSelection) {
        $("#" + reportPropertyId).remove();
        var found = false,
            loc = null;
        $.each(SELECTED_ITEM.ReportProperties, function(index, item) {
            if (item.Id == reportPropertyId) {
                found = true;
                loc = index;
                return false
            }
        });
        if (found) SELECTED_ITEM.ReportProperties.splice(loc, 1);
        reOrderProperties()
    }
}

function onSelectedGroupByChanged() {
    var selectedGroupByPropertyId = $("#lstGroupBy").val();
    SELECTED_ITEM.GroupColumns.length = 0;
    if (selectedGroupByPropertyId !== null && selectedGroupByPropertyId != undefined && selectedGroupByPropertyId != "" && selectedGroupByPropertyId != -1) SELECTED_ITEM.GroupColumns.push(selectedGroupByPropertyId);
    else SELECTED_ITEM.GroupColumns.pop();
    propertiesModified = true;
    itemModified(true)
}

function onSelectedPropertyChanged() {
    var propertyId = $("#lstProperties").val(),
        validSelection = propertyId !== null && propertyId != undefined && propertyId != "" && propertyId != -1;
    if (!validSelection) return;
    if (propertyId == 0) {
        toggleControl("btnAddProperty", false, true);
        showCustomPropertyDialog()
    } else {
        var property = property_Get(propertyId);
        if (property.SystemType != SYSTEM_TYPE_INSTANCE_OF && (property.DataType == DATA_TYPE_ASSET || property.DataType == DATA_TYPE_USER)) {
            var art = ART_DEFINITION;
            if (property.IsInstance) art = ART_INSTANCE;
            var html = [];
            html.push("<option value='-1'>[Select]</option>");
            var includeDef = art == ART_DEFINITION,
                includeIns = art == ART_INSTANCE;
            $.each(properties_Get(property.AssetTypeId, art, includeDef, includeIns, false), function(index, item) {
                html.push("<option value='" + item.key + "'>" + item.value + "</option>")
            });
            $("#lstSubProperties").html(html.join(""));
            $("#trSubProperties").show();
            REMOVE_PROPERTY = false
        } else REMOVE_PROPERTY = true;
        toggleControl("btnAddProperty", true, true)
    }
}

function onSelectedSubPropertyChanged() {
    var propertyId = $("#lstSubProperties").val(),
        validSelection = propertyId !== null && propertyId != undefined && propertyId != "";
    toggleControl("btnAddSubProperty", validSelection, true)
}

function onSourceAdd() {
    var val = $("#lstFields").val(),
        txt = selectOption_GetCurrentText("lstFields");
    htmlSelect_InsertOption("lstSelectedFields", val, txt, true);
    $("#lstFields").val(-1)
}

function onSourceRemove() {}

function onTimeElapsedChanged(value) {
    if (value == 1) $("#dtp").hide();
    else {
        $("#dtp").show();
        $(".datepicker").datepicker();
        $(".datepicker").datepicker("option", {
            dateFormat: "dd-M-yy"
        })
    }
}

function reOrderProperties() {
    var outerIndex = 0;
    $("#values > li").each(function() {
        var tempVal = this.id.toLowerCase(),
            found = false,
            foundIndex = null;
        $.each(SELECTED_ITEM.ReportProperties, function(subIndex, subItem) {
            if (subItem.Id.toLowerCase() == tempVal) {
                foundIndex = subIndex;
                found = true
            }
            if (found) return false
        });
        if (found) SELECTED_ITEM.ReportProperties[foundIndex].Index = outerIndex;
        outerIndex++
    });
    createPropertiesContent(SELECTED_ITEM);
    propertiesModified = true;
    itemModified(true)
}

function reportContainsProperty(report, propertyId) {
    var found = false;
    $.each(report.ReportProperties, function(index, item) {
        if (item.PropertyId == propertyId) {
            found = true;
            return false
        }
    });
    return found
}

function showCustomPropertyDialog() {
    var $dialog = $("#customPropertyDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $("#tdSourceAttributes").html("");
    $("#trSourceAttribute").hide();
    $("#trReportLabel").hide();
    $("#lstFieldTypes").val(-1);
    $dialog.dialog({
        width: 500,
        height: 350,
        modal: true,
        autoOpen: false,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            Cancel: function() {
                $("#lstProperties").val(-1);
                $(this).dialog("close")
            },
            Submit: function() {
                onAddCustomPropertyClicked();
                $(this).dialog("close")
            }
        }
    });
    $dialog.dialog("open")
}

function updateFilterLogic() {
    var filterCount = SELECTED_ITEM.Filters.length,
        html = [];
    if (filterCount == 0) SELECTED_ITEM.FilterLogic = null;
    else {
        html.push("(");
        for (var i = 0; i < filterCount; i++)
            if (i == filterCount - 1) html.push("({" + i + "})");
            else html.push("({" + i + "}) AND ");
        html.push(")");
        SELECTED_ITEM.FilterLogic = html.join("")
    }
    $("#txtFilterLogic").val(SELECTED_ITEM.FilterLogic)
}

function getFilter(filters, filterId) {
    var returnValue = null;
    $.each(filters, function(index, item) {
        if (item.Id == filterId) {
            returnValue = item;
            return false
        }
    });
    return returnValue
}

function getFilterIndex(filters, filterId) {
    var returnValue = -1;
    $.each(filters, function(index, item) {
        if (item.Id == filterId) {
            returnValue = index;
            return false
        }
    });
    return returnValue
}
