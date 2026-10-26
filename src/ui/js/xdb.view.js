var reFetchAssets = true,
    editor = false,
    CTL_CURR = false,
    CTL_DTPS = false,
    CTL_EDITORS = false,
    CTL_FILE = false,
    DELETED_VALUES = [],
    ERR_PROPS = [],
    NEW_VALUES = [],
    PICKLIST_FILTERS = [],
    PROPERTIES = [],
    PROPERTY_VALUES = [],
    PROPVALS = [],
    PROPS_TABS = [],
    REQ_PROPS = [],
    $driver = $("#lstDriver"),
    _contextMenuSrc = "images/ctx/",
    LOAD_MSG = '<div style="width:100%;text-align:center;" >Retrieving data.  Please wait.</div><br/><div style="text-align:center"><img src="images/ajax-loader.gif" alt="spinner" style="text-align:left;" /></div>',
    PRESUBMIT_MSG = '<div style="width:100%;text-align:center;" >Validating your data.  Please wait.</div><br/><div style="text-align:center"><img src="images/ajax-loader.gif" alt="spinner" style="text-align:left;" /></div>',
    _prefetch = false,
    _qFilterActive = false,
    _jsEditor = "ckeditor/ckeditor.js",
    _imgSrc = "images",
    _dialogPos = ["top", 225],
    _dialogPosHi = ["top", 100],
    $tabs = $("#tabs"),
    $toolbar = $("#toolbar"),
    _jsTemplate = "js/jquery.tmpl.min.js",
    _jsTransport = "js/jquery.iframe-transport.min.js",
    _jsFileUpload = "js/jquery.fileupload.min.js",
    _jsFileUploadUI = "js/jquery.fileupload-ui.min.js",
    _jsCkEditor = "ckeditor/ckeditor.js",
    _jsCkEditorJson = "ckeditor/adapters/jquery.js";

function activateHighlighting() {
    $("input").focus(function() {
        $(this).addClass("highlight")
    });
    $("input").blur(function() {
        $(this).removeClass("highlight")
    })
}

function blockUI() {
    $tabs.block({
        message: "<h1>Processing</h1>",
        css: {
            border: "3px solid #a00"
        }
    })
}

function unblockUI() {
    $tabs.unblock()
}

function onSelectedViewChanged(viewId) {
    if (renderView(viewId)) {
        if (SELECTED_VIEW.prefetchAssets) activateAssetSelector(null);
        else activateDynamicAssetSelector(null);
        FILTERS = [];
        reFetchAssets = true;
        if (CURRENT_MODE == MODE_EDIT) {
            activateDatePickers();
            activateAutoCompletes();
            activateCurrencySelector();
            activateHtmlEditor()
        }
        $(document.getElementById("lstDriver")).val("")
    }
}

function initViewHtml() {
    if (!IS_STAND_ALONE_MODE && (document.referrer == null || document.referrer == "")) document.location = URL_DEFAULT_PAGE;
    CURRENT_MODE = MODE_VIEW;
    clearQueryStrings();
    getQueryStrings();
    getCurrentUser();
    displayCurrentUser(_imgSrc);
    $(document.getElementById("header")).html(buildHeaderContent(_imgSrc));
    $(document.getElementById("navigation")).html(buildMenuHtml(_imgSrc));
    refreshRecent(_imgSrc);
    initMenu();
    loadAssetTypesAndDefaultViews();
    if (QS_VIEW_ID !== null && QS_VIEW_ID !== "") {
        authorizePageView(QS_VIEW_ID);
        if (QS_ASSET_ID !== null && QS_ASSET_ID !== "" && QS_ASSET_ID != "null") {
            if (VIEW_FROM_PROCESS === "true") CURRENT_MODE = MODE_EDIT;
            onSelectedViewChanged(QS_VIEW_ID);
            if (QS_ASSETTYPE_ID !== null && QS_ASSETTYPE_ID !== "" && QS_ASSETTYPE_ID != "null") SELECTED_VIEW.AssetTypeId = QS_ASSETTYPE_ID;
            SELECTED_ITEM_ID = QS_ASSET_ID;
            driverChanged(SELECTED_ITEM_ID);
            if (SELECTED_ASSET !== null) $driver.val(SELECTED_ASSET.Name);
            else $driver.val("Search...")
        } else {
            onSelectedViewChanged(QS_VIEW_ID);
            if (SELECTED_VIEW.prefetchAssets && !!window.Worker) {
                var worker = new Worker("js/" + WORKER);
                worker.onmessage = function(event) {
                    var key = "Assets_" + SELECTED_VIEW.AssetTypeId + "_" + SELECTED_VIEW.Id;
                    sessionCache(key, JSON.parse(event.data), false)
                };
                var arg = SVC_ASSETS_GET_FOR_VIEW + "?userId=" + USER_CURRENT.Id + "&assetTypeId=" + SELECTED_VIEW.AssetTypeId + "&viewId=" + SELECTED_VIEW.Id;
                worker.postMessage(arg)
            }
        }
    } else {}
    if (SELECTED_VIEW.filters !== null && SELECTED_VIEW.filters.length > 0) $("#pnl00").html(createQuickFilters(SELECTED_VIEW.Id, SELECTED_VIEW.AssetTypeId, IS_INSTANCE_VIEW, SELECTED_VIEW.filters));
    else $("#pnl00").html(createQuickFilters(SELECTED_VIEW.Id, SELECTED_VIEW.AssetTypeId, IS_INSTANCE_VIEW, SELECTED_VIEW.filters));
    $driver.watermark(" ( begin typing to search ... delete all text to clear search )")
}

function processDictionary(dictionary) {
    try {
        if (dictionary !== null) return dictionary;
        else {
            var array = [],
                obj = {};
            obj.value = "No Records";
            obj.key = NULL_GUID;
            array[array.length] = obj;
            return array
        }
    } finally {}
}

function activateAssetSelector(dataSource) {
    try {
        if (dataSource == undefined || dataSource == null) dataSource = [];
        var minChars = 0;
        if (dataSource.length > MAX_AUTOCOMPLETE_ITEMS) minChars = 2;
        $driver.autocomplete({
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
                setTimeout('driverChanged("' + ui.item.key + '")', 25);
                return false
            }
        }).data("autocomplete")._renderItem = function(ul, item) {
            if ($(ul).children().length == 0)
                if (SELECTED_VIEW.AllowNewValues && SELECTED_VIEW.IsCreation) {
                    var obj = {};
                    obj.value = "[Enter New]";
                    obj.key = 0;
                    $("<li class='ui-widget'></li>").data("item.autocomplete", obj).append("<a>" + obj.value + "</a>").appendTo(ul)
                } return $("<li class='ui-widget'></li>").data("item.autocomplete", item).append("<a>" + item.value + "</a>").appendTo(ul)
        };
        activateAssetSelectorFocus()
    } finally {}
}

function activateAssetSelectorFocus() {
    try {
        $driver.focusin(function() {
            if (reFetchAssets) {
                var imgRandomID = new Date().getTime();
                $(this).after("<img id='img" + imgRandomID + "' src='images/ajax-loader.gif' alt='spinner' style='position:absolute;top:" + $(this).offset().top + "px;left:" + $(this).offset().left + "px' />");
                var dataSource = assets_GetForView(USER_CURRENT.Id, SELECTED_VIEW.AssetTypeId, SELECTED_VIEW.Id),
                    minChars = dataSource.length > MAX_AUTOCOMPLETE_ITEMS ? 2 : 0;
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
                        setTimeout('driverChanged("' + ui.item.key + '")', 25);
                        return false
                    }
                }).data("autocomplete")._renderItem = function(ul, item) {
                    if ($(ul).children().length == 0)
                        if (SELECTED_VIEW.AllowNewValues && SELECTED_VIEW.IsCreation) {
                            var obj = {};
                            obj.value = "[Enter New]";
                            obj.key = 0;
                            $("<li class='ui-widget'></li>").data("item.autocomplete", obj).append("<a>" + obj.value + "</a>").appendTo(ul)
                        } return $("<li class='ui-widget'></li>").data("item.autocomplete", item).append("<a>" + item.value + "</a>").appendTo(ul)
                };
                reFetchAssets = false;
                $("#img" + imgRandomID).remove()
            }
            $(this).autocomplete("search")
        })
    } finally {}
}

function activateAutoCompletes() {
    try {
        $(":text.autocomplete").focus(function() {
            if (this.autoCompleteRendered != true) {
                var imgRandomID = new Date().getTime();
                $(this).after("<img id='img" + imgRandomID + "' src='images/ajax-loader2.gif' style='position:absolute;top:" + $(this).offset().top + "px;left:" + $(this).offset().left + "px' />");
                var dataSource = null;
                if (PICKLIST_FILTERS[this.id] == null) {
                    var assetTypeId = SELECTED_ASSET.AssetTypeId;
                    dataSource = processDictionary(dictionaryValues_GetForProperty(this.id, assetTypeId))
                } else {
                    var triggerInfo = PICKLIST_FILTERS[this.id];
                    dataSource = processDictionary(dictionaryValues_GetForPropertyRestricted(triggerInfo.ParentPropertyId, this.id, triggerInfo.ParentPropertyValue, triggerInfo.SourcePropertyId))
                }
                var minChars = 0;
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
                        if (PROPERTIES[this.id.toLowerCase()].AllowMultiValue == false) {
                            ERR_PROPS[this.id] = null;
                            var item = {
                                id: this.id.toLowerCase(),
                                value: ui.item.key
                            };
                            sendValue(item)
                        }
                        return false
                    },
                    change: function(event, ui) {
                        if (ui.item === null) {
                            var content = $(this).val().toLowerCase();
                            if (content == null || content == "") {
                                $(this).val("");
                                $(this.id + "-id").val("");
                                valueCleared(this.id)
                            } else {
                                var ds = $(this).autocomplete("option", "source");
                                if (ds !== null) {
                                    var itemKey = null;
                                    content = $.trim(content);
                                    $.each(ds, function(index, item) {
                                        if (item.value.toLowerCase() == content) {
                                            content = item.value;
                                            itemKey = item.key;
                                            return false
                                        }
                                    });
                                    if (itemKey !== null) {
                                        $(this).val(content);
                                        ERR_PROPS[this.id] = null;
                                        var item = {
                                            id: this.id,
                                            value: itemKey
                                        };
                                        sendValue(item);
                                        $(this).removeClass("ui-state-error");
                                        return false
                                    }
                                }
                                toggleButtons("submitButtons", false);
                                $(this).addClass("ui-state-error");
                                var tempProperty = PROPERTIES[this.id];
                                ERR_PROPS[this.id] = tempProperty;
                                if (PROPERTIES[this.id].AllowNewValues)
                                    if (PROPERTIES[this.id].DataType == DATA_TYPE_USER) $(this).val("'" + content + "' is invalid. Please use [Enter New] to add a new user and perform validation.");
                                    else $(this).val("'" + content + "' is invalid. Please use [Enter New] to add a new value.");
                                else $(this).val("'" + content + "' is invalid.")
                            }
                            return false
                        }
                    }
                }).data("autocomplete")._renderItem = function(ul, item) {
                    if ($(ul).children().length == 0)
                        if (PROPERTIES[this.element[0].id].AllowNewValues) {
                            var obj = {};
                            obj.value = "[Enter New]";
                            obj.key = 0;
                            $("<li class='ui-widget'></li>").data("item.autocomplete", obj).append("<a>" + obj.value + "</a>").appendTo(ul)
                        } return $("<li class='ui-widget'></li>").data("item.autocomplete", item).append("<a>" + item.value + "</a>").appendTo(ul)
                };
                this.autoCompleteRendered = true;
                $("#img" + imgRandomID).remove()
            }
            $(this).autocomplete("search")
        })
    } finally {}
}

function activateCurrencySelector() {
    try {
        if (!CTL_CURR) return;
        $(".currencySymbol").focus(function() {
            if (this.autoCompleteRendered != true) {
                var imgRandomID = new Date().getTime();
                $(this).after("<img id='img" + imgRandomID + "' src='images/ajax-loader.gif' alt='spinner' style='position:absolute;top:" + $(this).offset().top + "px;left:" + $(this).offset().left + "px' />");
                $(this).autocomplete({
                    delay: 0,
                    minLength: 0,
                    source: getCurrencies(),
                    focus: function(event, ui) {
                        $(this).val(htmlDecode(ui.item.value));
                        $("#" + this.id + "-id").val(ui.item.key);
                        return false
                    },
                    select: function(event, ui) {
                        $(this).val(htmlDecode(ui.item.value));
                        $("#" + this.id + "-id").val(ui.item.key);
                        var newId = this.id.toLowerCase().replace("_curr", ""),
                            decValue = $("#" + newId).val(),
                            item = {
                                id: newId,
                                value: ui.item.key + DELIMITER_MULTI_VALUE + decValue
                            };
                        sendValue(item);
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
    } finally {}
}

function activateDatePickers() {
    if (!CTL_DTPS) return;
    try {
        $(".datepicker").datepicker({
            dateFormat: DPICKER_FORMAT,
            constrainInput: true,
            showButtonPanel: false,
            changeMonth: true,
            changeYear: true,
            showOn: "focus",
            yearRange: "-25:+25",
            onClose: function(dateText, inst) {
                if (dateText === null) return true;
                try {
                    if (dateText != "") jQuery.datepicker.parseDate(DPICKER_FORMAT, dateText);
                    $("#" + this.id).removeClass("ui-state-error");
                    var item = {
                        id: this.id.toLowerCase(),
                        value: dateText
                    };
                    sendValue(item);
                    return true
                } catch (e) {
                    $("#" + this.id).addClass("ui-state-error");
                    $("#" + this.id).focus();
                    $("#" + this.id).val(dateText + " is an invalid date format. Use: dd-Mmm-yyyy");
                    toggleButtons("submitButtons", false);
                    return true
                }
            }
        })
    } finally {}
}

function activateDynamicAssetSelector(dataSource) {
    try {
        if (dataSource == undefined || dataSource == null) dataSource = [];
        var minChars = 0;
        if (dataSource.length > MAX_AUTOCOMPLETE_ITEMS) minChars = 2;
        $driver.autocomplete({
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
                setTimeout('driverChanged("' + ui.item.key + '")', 25);
                return false
            }
        }).data("autocomplete")._renderItem = function(ul, item) {
            if ($(ul).children().length == 0)
                if (SELECTED_VIEW.AllowNewValues && SELECTED_VIEW.IsCreation) {
                    var obj = {};
                    obj.value = "[Enter New]";
                    obj.key = 0;
                    $("<li class='ui-widget'></li>").data("item.autocomplete", obj).append("<a>" + obj.value + "</a>").appendTo(ul)
                } return $("<li class='ui-widget'></li>").data("item.autocomplete", item).append("<a>" + item.value + "</a>").appendTo(ul)
        };
        activateDynamicAssetSelectorFocus()
    } finally {}
}

function activateDynamicAssetSelectorFocus() {
    try {
        $driver.focus(function() {
            if (reFetchAssets) {
                var imgRandomID = new Date().getTime();
                $(this).after("<img id='img" + imgRandomID + "' src='images/ajax-loader.gif' alt='spinner' style='position:absolute;top:" + $(this).offset().top + "px;left:" + $(this).offset().left + "px' />");
                var minChars = 3;
                $(this).autocomplete({
                    delay: 0,
                    minLength: minChars,
                    source: SVC_ASSETS_GET_FOR_VIEW + "?userId=" + USER_CURRENT.Id + "&assetTypeId=" + SELECTED_VIEW.AssetTypeId + "&viewId=" + SELECTED_VIEW.Id,
                    focus: function(event, ui) {
                        $(this).val(ui.item.value);
                        return false
                    },
                    select: function(event, ui) {
                        $(this).val(ui.item.value);
                        $(this.id + "-id").val(ui.item.key);
                        setTimeout('driverChanged("' + ui.item.key + '")', 25);
                        return false
                    }
                }).data("autocomplete")._renderItem = function(ul, item) {
                    if ($(ul).children().length == 0)
                        if (SELECTED_VIEW.AllowNewValues && SELECTED_VIEW.IsCreation) {
                            var obj = {};
                            obj.value = "[Enter New]";
                            obj.key = 0;
                            $("<li class='ui-widget'></li>").data("item.autocomplete", obj).append("<a>" + obj.value + "</a>").appendTo(ul)
                        } return $("<li class='ui-widget'></li>").data("item.autocomplete", item).append("<a>" + item.value + "</a>").appendTo(ul)
                };
                reFetchAssets = false;
                $("#img" + imgRandomID).remove()
            }
            $(this).autocomplete("search")
        })
    } finally {}
}

function activateFileUploader() {
    if (!CTL_FILE) return;
    $(".imgupload").fileupload({
        url: URL_REST_SVC + "/image_upload?imageId=" + getUploadId() + "&userId=" + USER_CURRENT.Id + "&fileName=" + "filename",
        singleFileUploads: true,
        autoUpload: true,
        forceIframeTransport: false
    });
    $(".docupload").fileupload({
        url: URL_REST_SVC + "/UploadDocument?documentId=" + getUploadId() + "&userId=" + USER_CURRENT.Id + "&fileName=" + "filename",
        singleFileUploads: true,
        autoUpload: true
    });
    $(".imgupload").bind("fileuploadadd", function(e, data) {
        fileName = data.files[0].name;
        $(".fileupload").fileupload({
            url: URL_REST_SVC + "/image_upload?imageId=" + getUploadId() + "&userId=" + USER_CURRENT.Id + "&fileName=" + fileName + "&propertyId=" + this.id,
            singleFileUploads: true,
            autoUpload: true,
            complete: function(result, textStatus, jqXHR) {
                if (textStatus == "success" && result.responseText != undefined) {
                    var serverResult = JSON.parse(result.responseText)[0],
                        error = serverResult.error;
                    if (error === null || error == undefined || error == "") {
                        $("#wait").show();
                        var item = {
                            id: serverResult.propertyId,
                            value: serverResult.uploadId
                        };
                        sendValue(item);
                        $("#wait").hide()
                    } else showMessage("Error", "Unknown error occurred", true, 400, true)
                } else showMessage("Error", "Unknown error occurred", true, 400, true)
            }
        })
    });
    $(".docupload").bind("fileuploadadd", function(e, data) {
        fileName = data.files[0].name;
        $(".fileupload").fileupload({
            url: URL_REST_SVC + "/UploadDocument?documentId=" + getUploadId() + "&userId=" + USER_CURRENT.Id + "&fileName=" + fileName + "&propertyId=" + this.id,
            singleFileUploads: true,
            autoUpload: true,
            complete: function(result, textStatus, jqXHR) {
                if (textStatus == "success" && result.responseText != undefined) {
                    var serverResult = JSON.parse(result.responseText)[0],
                        error = serverResult.error;
                    if (error === null || error == undefined || error == "") {
                        $("#wait").show();
                        var item = {
                            id: serverResult.propertyId,
                            value: serverResult.uploadId
                        };
                        sendValue(item);
                        $("#wait").hide()
                    } else showMessage("Error", "Unknown error occurred", true, 400, true)
                } else showMessage("Error", "Unknown error occurred", true, 400, true)
            }
        })
    });
    $(".fileupload .files a:not([target^=_blank])").live("click", function(e) {
        e.preventDefault();
        $('<iframe style="display:none;"></iframe>').prop("src", this.href).appendTo("body")
    })
}

function activateHtmlEditor() {
    if (!CTL_EDITORS) return;
    if (editor) return;
    $(".CKeditor").ckeditor(function() {
        this.on("blur", function() {
            if (this.checkDirty()) {
                var pId = $("." + this.id).prev()[0].id,
                    val = $("#" + pId).val(),
                    item = {
                        id: pId,
                        value: val
                    };
                sendValue(item)
            }
        })
    });
    editor = true
}

function activateTaggers() {}

function onAutoCompleteFocus(id) {
    var ctl = $("#" + id);
    ctl.select();
    ctl.autocomplete("close")
}

function onCurrencySelectorFocus() {}

function getUploadId() {
    try {
        return guidGenerator()
    } finally {}
}

function addBookmark() {
    if (bookmark_Add(SELECTED_VIEW.Id, SELECTED_ITEM_ID)) {
        appCache(LOCAL_STORAGE_BOOKMARKS, bookMarks_Get(USER_CURRENT.Id), true);
        var bookmarkIcon = $("#imgBookmark");
        $(bookmarkIcon).attr("src", "images/favr.png");
        $(bookmarkIcon).attr("alt", "Remove from favorites");
        $(bookmarkIcon).attr("onClick", "removeBookmark()")
    }
}

function addMultiValueItem(controlId) {
    try {
        var sourceText = $("#" + controlId).val(),
            sourceValue = null,
            source = $("#" + controlId).autocomplete("option", "source"),
            l = source.length;
        for (var i = 0; i < l; i++)
            if (source[i].value == sourceText) {
                sourceValue = source[i].key;
                found = true;
                break
            } if (found) {
            var existsInTarget = htmlSelect_ContainsOptionText(sourceValue, controlId + "_tgt", false);
            if (!existsInTarget) {
                selectOptionAppend(controlId + "_tgt", sourceValue, sourceText, true);
                htmlSelect_RemoveOptionByValue(controlId, sourceValue);
                var item = {
                    id: controlId,
                    value: sourceValue
                };
                sendValue(item)
            }
            $("#" + controlId).val("")
        }
    } finally {}
}

function addNewAsset() {
    try {
        var view = SELECTED_VIEW,
            $dialog = $("#messageDialog");
        if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
        var newItemId = generateGuid(DATA_TYPE_ASSET);
        if (newItemId !== null) {
            var newItemName = $("#txtNewItemName").val(),
                newItemParentId = $("#lstNewItemParent").val(),
                newItemTypeId = null;
            if (NEW_ITEM_TYPE_REQUIRED) newItemTypeId = $("#lstNewItemType").val();
            else newItemTypeId = view.AssetTypeId;
            if (newItemParentId == "-1") newItemParentId = NULL_GUID;
            var newItemName_submit = newItemName.replace(/\"/g, '\\"'),
                assetCreated = asset_Create(USER_ACTUAL.Id, newItemId, newItemName_submit, newItemTypeId, newItemParentId),
                dialogMessage;
            if (assetCreated) {
                dialogMessage = view.DriverCaption + " '<i>" + newItemName + "</i>' created successfully.";
                $driver.val(newItemName);
                $("#lstDriver-id").val(newItemId);
                SELECTED_ITEM_ID = newItemId;
                driverChanged(newItemId);
                if (CURRENT_MODE == MODE_VIEW) changeMode(false, false)
            } else dialogMessage = view.DriverCaption + " failed to create.";
            $dialog.html(dialogMessage);
            $dialog.dialog({
                width: 600,
                modal: true,
                autoOpen: false,
                position: _dialogPos,
                title: "Creation Result",
                buttons: {
                    Ok: function() {
                        $(this).dialog("close")
                    }
                }
            })
        }
        $dialog.dialog("open");
        $("#confirmNewItem").dialog("close");
        $("#newItem").dialog("close")
    } finally {}
}

function addNewAssetInstance() {
    try {
        var newItemId = generateGuid(DATA_TYPE_ASSET);
        if (newItemId !== null) {
            var instanceOfId = $("#" + AT_FINAL).val(),
                assetTypeId = assetTypeId_GetByAssetId(instanceOfId),
                newItemInstanceName = $("#txtNewItemInstanceName").val(),
                view = SELECTED_VIEW,
                created = asset_CreateInstance(USER_ACTUAL.Id, newItemId, newItemInstanceName, assetTypeId, instanceOfId, view.Id),
                driverCaption = "Asset";
            if (view.DriverCaption !== null && view.DriverCaption !== "") driverCaption = view.DriverCaption;
            var dialogMessage;
            if (created) {
                dialogMessage = driverCaption + " created successfully.";
                $driver.val(newItemInstanceName);
                $("#lstDriver-id").val(newItemId);
                driverChanged(newItemId);
                SELECTED_ITEM_ID = newItemId
            } else dialogMessage = driverCaption + " failed to create.";
            $("#messageDialog").html(dialogMessage);
            $("#messageDialog").dialog({
                width: 600,
                modal: true,
                stack: true,
                position: _dialogPos,
                title: "Asset Creation Result",
                buttons: {
                    Ok: function() {
                        $(this).dialog("close");
                        $("#confirmNewItem").dialog("close");
                        $("#newItemInstance").dialog("close");
                        if (CURRENT_MODE == MODE_VIEW) changeMode(false, false)
                    }
                }
            })
        }
    } finally {}
}

function addNewPickListValue(property) {
    try {
        var pickListId = property.PickListId,
            newItemId = generateGuid(DATA_TYPE_STRING);
        if (newItemId !== null) {
            var newPLV = $("#txtNewPickListValue").val(),
                pickListValueCreated = pickListValue_create(USER_ACTUAL.Id, newItemId, newPLV, newPLV, pickListId);
            if (pickListValueCreated) {
                dialogMessage = "Value added successfully.";
                var targetControlId = "#" + property.Id.toLowerCase();
                $(targetControlId).val(newPLV);
                $(targetControlId + "-id").val(newItemId);
                $("#messageDialog").html(dialogMessage);
                $("#messageDialog").dialog({
                    width: 350,
                    height: "auto",
                    position: _dialogPos,
                    modal: true,
                    title: "Value Creation Result",
                    buttons: {
                        Ok: function() {
                            $(this).dialog("close")
                        }
                    }
                })
            } else {}
        }
        $("#confirmNewPickListValue").dialog("close");
        $("#newPickListValue").dialog("close");
        return true
    } finally {}
}

function addUrl(propertyId) {
    var url = $("#" + propertyId + "_url").val();
    if (isUrl(url)) sendValueUrl(propertyId);
    else {
        var textToShow = "The URL provided:<br/><b>'" + url + "'</b><br/>is not valid.<br/><i>The URL must begin with either 'http://' or 'https://'.</i>";
        showMessage("Invalid URL", textToShow, true, 400, true)
    }
}

function changeMode(isReadOnly, setSelectedView) {
    if (editor) {
        $(".CKeditor").ckeditor(function() {
            this.destroy()
        });
        editor = false
    }
    if (CURRENT_MODE == MODE_EDIT)
        if (NEW_VALUES !== null && NEW_VALUES != [] && associativeArrayLength(NEW_VALUES) > 0) {
            var asset = SELECTED_ASSET;
            warnUnsavedChanges(asset.Name, asset.Id);
            return
        } var selected = $tabs.tabs("option", "selected");
    if (isReadOnly) CURRENT_MODE = MODE_VIEW;
    else CURRENT_MODE = MODE_EDIT;
    renderView(SELECTED_VIEW.Id);
    if (CURRENT_MODE == MODE_EDIT) {
        activateDatePickers();
        activateFileUploader();
        if (editor) {
            $(".CKeditor").ckeditor(function() {
                this.destroy()
            });
            editor = false
        }
    }
    driverChanged(SELECTED_ITEM_ID);
    $tabs.tabs("select", selected)
}

function clearForm(form, resetSelectedView, resetSelectedDriver) {
    if (SELECTED_VIEW.IsReadOnly || CURRENT_MODE == MODE_VIEW) $(".property-value").html("");
    else {
        var i = 0,
            propIds = [];
        $.each(SELECTED_VIEW.PropertyGroups, function(index, item) {
            $.each(item.Properties, function(subIndex, subItem) {
                propIds[i] = subItem.Id;
                i++
            })
        });
        $.each(propIds, function(index, item) {
            var element = document.getElementById(item.toLowerCase()),
                property = PROPERTIES[item.toLowerCase()];
            if (element === null) {
                if (property.DataType == DATA_TYPE_URL) {
                    var elementName = document.getElementById(item.toLowerCase() + "_name"),
                        elementUrl = document.getElementById(item.toLowerCase() + "_url");
                    elementName.value = "";
                    elementUrl.value = "";
                    selectOptionsRemove(item.toLowerCase() + "_tgt")
                }
            } else {
                var type = element.type,
                    tag = element.tagName.toLowerCase();
                if (type == "text" || type == "password" || tag == "textarea") element.value = "";
                else if (type == "checkbox" || type == "radio") element.checked = false;
                else if (tag == "select") {
                    element.selectedIndex = 0;
                    if (property.AllowMultiValue) selectOptionsRemove(item.toLowerCase() + "_tgt")
                } else if (tag == "div") {} else element.value = ""
            }
        })
    }
    $("#tdAssetName").html("")
}

function clearMultiValueSelections(controlIdAndValue) {
    var tgtControlId = controlIdAndValue.id.toLowerCase() + "_tgt";
    $("#" + tgtControlId + " option:selected").removeAttr("selected")
}

function cloneAsset(assetName) {
    var asset = asset_Clone(SELECTED_ITEM_ID, SELECTED_VIEW.Id, USER_ACTUAL.Id, assetName);
    if (asset !== null && asset != undefined && asset.Name == assetName) {
        $driver.val(assetName);
        $("#lstDriver-id").val(asset.Id);
        if (CURRENT_MODE == MODE_VIEW) {
            CURRENT_MODE = MODE_EDIT;
            renderView(SELECTED_VIEW.Id)
        }
        SELECTED_ASSET = asset;
        driverChanged(asset.Id);
        SELECTED_ITEM_ID = asset.Id
    } else showMessage("Error", "Unknown error occurred", true, 400, true)
}

function cloneAssetSelected() {
    try {
        var newAssetName = "New Asset Name";
        if (SELECTED_VIEW.DriverCaption !== null && SELECTED_VIEW.DriverCaption != "") newAssetName = "New " + SELECTED_VIEW.DriverCaption;
        $("#tdCloneItemName").html(newAssetName);
        $("#cloneItem").dialog({
            width: 450,
            height: "auto",
            position: _dialogPos,
            modal: true,
            autoOpen: false,
            title: "Cloned Item Details",
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            buttons: {
                Cancel: function() {
                    $(this).dialog("close")
                },
                Ok: function() {
                    var clonedAssetName = $("#txtCloneItemName").val(),
                        existingAssetId = assetId_Get(clonedAssetName, SELECTED_VIEW.AssetTypeId);
                    if (existingAssetId != NULL_GUID) {
                        var tempContent = "<p>The item '" + clonedAssetName + "' already exists.</p>";
                        $("#tdCloneItemResult").html(tempContent)
                    } else {
                        cloneAsset(clonedAssetName);
                        $(this).dialog("close")
                    }
                }
            }
        });
        $("#cloneItem").dialog("open")
    } finally {}
}

function confirmDelete() {
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html("Are you sure you would like to delete this item?");
    var title = '<span class="ui-icon ui-icon-circle-minus"></span> Delete ' + $driver.val() + "?";
    $dialog.dialog({
        width: 350,
        height: "auto",
        position: _dialogPos,
        modal: true,
        title: title,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            No: function() {
                $(this).dialog("close")
            },
            Yes: function() {
                deleteSelectedItem()
            }
        }
    })
}

function confirmNewAsset() {
    try {
        var error = false,
            closeNewItem = false,
            tempContent = [],
            newItemName = $("#txtNewItemName").val(),
            newItemParentId = $("#lstNewItemParent").val(),
            newItemParentName = selectOption_GetCurrentText("lstNewItemParent"),
            newItemTypeId = $("#lstNewItemType").val(),
            newItemTypeName = selectOption_GetCurrentText("lstNewItemType"),
            existingAssetId = null;
        if (newItemName == null || newItemName == "") {
            tempContent.push("You must enter a name for the new item.");
            error = true
        } else if (NEW_ITEM_PARENT_REQUIRED && (newItemParentId == null || newItemParentId == "" || newItemParentId == "-1")) {
            tempContent.push("You must select the parent of the new item.");
            error = true
        } else if (NEW_ITEM_TYPE_REQUIRED && (newItemTypeId == null || newItemTypeId == "" || newItemTypeId == "-1")) {
            tempContent.push("You must select the asset type.");
            error = true
        }
        var view = SELECTED_VIEW;
        existingAssetId = assetId_Get(newItemName, view.AssetTypeId);
        if (existingAssetId != NULL_GUID) {
            tempContent.push("<p>The item '" + newItemName + "' already exists.</p></br><p>It is now the currently-selected item.</p>");
            closeNewItem = true;
            error = true
        } else {
            var itemNameCaption = "Item Name";
            if (view.DriverCaption !== null && view.DriverCaption != "") itemNameCaption = view.DriverCaption;
            tempContent.push('<table style="width:100%">');
            tempContent.push('<tr><td style="width:50%"><b>' + itemNameCaption + ':</b></td><td style="width:50%"><b>' + newItemName + "</b></td></tr>");
            if (NEW_ITEM_PARENT_REQUIRED) tempContent.push('<tr><td style="width:50%"><b>Parent Item:</b></td><td style="width:50%"><b>' + newItemParentName + "</b></td></tr>");
            else if (NEW_ITEM_TYPE_REQUIRED) tempContent.push('<tr><td style="width:50%"><b>Asset Type:</b></td><td style="width:50%"><b>' + newItemTypeName + "</b></td></tr>")
        }
        var $dialog = $("#confirmNewItem");
        if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
        $dialog.html(tempContent.join(""));
        if (error)
            if (closeNewItem) $dialog.dialog({
                width: "auto",
                height: "auto",
                position: _dialogPos,
                modal: true,
                title: "Error",
                autoOpen: false,
                open: function() {
                    $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
                },
                buttons: {
                    Ok: function() {
                        $("#txtNewItemName").val("");
                        $("#lstNewItemParent").val(0);
                        $(this).dialog("close");
                        $("#newItem").dialog("close");
                        $driver.val(newItemName);
                        driverChanged(existingAssetId)
                    }
                }
            });
            else $dialog.dialog({
                width: "auto",
                height: "auto",
                position: _dialogPos,
                modal: true,
                title: "Error",
                autoOpen: false,
                open: function() {
                    $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
                },
                buttons: {
                    Ok: function() {
                        $(this).dialog("close")
                    }
                }
            });
        else {
            var title = '<span class="ui-icon ui-icon-circle-plus"></span> Add New Item?';
            if (view.DriverCaption !== null && view.DriverCaption != "") title = '<span class="ui-icon ui-icon-circle-plus"></span> Add New ' + view.DriverCaption + "?";
            $dialog.dialog({
                width: 450,
                height: "auto",
                position: _dialogPos,
                modal: true,
                autoOpen: false,
                title: title,
                open: function() {
                    $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
                },
                buttons: {
                    Cancel: function() {
                        if (VIEW_FROM_PROCESS) window.location = URL_CLIENT_UI + "/default.html";
                        else $(this).dialog("close")
                    },
                    Submit: function() {
                        addNewAsset()
                    }
                }
            })
        }
        $dialog.dialog("open")
    } finally {}
}

function confirmNewAssetFromOtherView(property) {
    var error = false,
        closeNewItem = false,
        tempContent, newItemName = $("#txtNewPickListValue").val();
    if (newItemName == null || newItemName == "") {
        tempContent = "You must enter a name for the new item.";
        error = true
    } else if (htmlSelect_ContainsOptionText(newItemName, property.Id.toLowerCase(), false)) {
        tempContent = "<p>The item '" + newItemName + "' already exists.</p></br><p>It is now the currently-selected item.</p>";
        closeNewItem = true;
        error = true
    } else {
        tempContent = '<table style="width:100%">';
        tempContent += '<tr><td style="width:50%"><b>Item Name:</b></td><td style="width:50%"><b>' + newItemName + "</b></td></tr>"
    }
    $("#confirmNewItem").html(tempContent);
    if (error)
        if (closeNewItem) $("#confirmNewItem").dialog({
            width: 600,
            height: "auto",
            position: _dialogPos,
            modal: true,
            title: "Error",
            autoOpen: false,
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            buttons: {
                Ok: function() {
                    $("#txtNewItemName").val("");
                    $("#lstNewItemParent").val(0);
                    $(this).dialog("close");
                    $("#newItem").dialog("close");
                    setSelectedItem(newItemName, property.Id.toLowerCase());
                    var val = $("#" + property.Id.toLowerCase()).val(),
                        item = {
                            id: property.Id,
                            value: val
                        };
                    sendValue(item)
                }
            }
        });
        else $("#confirmNewItem").dialog({
            width: 600,
            height: "auto",
            position: _dialogPos,
            modal: true,
            title: "Error",
            autoOpen: false,
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            buttons: {
                Ok: function() {
                    $(this).dialog("close")
                }
            }
        });
    else $("#confirmNewItem").dialog({
        width: 600,
        height: "auto",
        position: _dialogPos,
        modal: true,
        autoOpen: false,
        title: "Add new " + property.Name + "?",
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            Cancel: function() {
                $(this).dialog("close")
            },
            Submit: function() {
                var dialogMessage, newItemId = generateGuid(DATA_TYPE_ASSET),
                    assetCreated = asset_Create(USER_ACTUAL.Id, newItemId, newItemName, property.AssetTypeId, NULL_GUID);
                if (assetCreated) {
                    dialogMessage = property.Name + " created successfully.";
                    var targetControlId = property.Id.toLowerCase();
                    autoComplete_Insert(targetControlId, newItemId, newItemName);
                    $("#" + targetControlId).val(newItemName);
                    var item = {
                        id: property.Id,
                        value: newItemId
                    };
                    sendValue(item)
                } else dialogMessage = property.Name + " failed to save.";
                $(this).dialog("close");
                $("#messageDialog").html(dialogMessage);
                $("#messageDialog").dialog({
                    width: 350,
                    modal: true,
                    position: _dialogPos,
                    autoOpen: false,
                    title: property.Name + " Creation Result",
                    buttons: {
                        Ok: function() {
                            $(this).dialog("close")
                        }
                    }
                });
                $("#newPickListValue").dialog("close");
                $("#messageDialog").dialog("open")
            }
        }
    });
    $("#confirmNewItem").dialog("open")
}

function confirmNewAssetInstance() {
    var error = false,
        closeNewItem = false,
        tempContent = [],
        newItemName = $("#txtNewItemInstanceName").val(),
        newItemAssetId = $("#" + AT_FINAL).val(),
        newItemAssetName = selectOption_GetCurrentText(AT_FINAL),
        newItemAssetDesc = asset_GetDescription(newItemAssetId),
        newAssetName = "Asset Name",
        view = SELECTED_VIEW;
    if (view.DriverCaption !== null && view.DriverCaption != "") newAssetName = view.DriverCaption;
    tempContent.push('<table style="width:100%">');
    tempContent.push('<tr><td style="width:50%"><b>' + newAssetName + ':</b></td><td style="width:50%">' + newItemName + "</td></tr>");
    var defLabel = assetType_GetReportingLabel(view.AssetTypeId, ART_DEFINITION);
    if (defLabel !== null && defLabel != "") {
        tempContent.push('<tr><td style="width:50%"><b>' + defLabel + ':</b></td><td style="width:50%">' + newItemAssetName + "</td></tr>");
        tempContent.push("<tr><td><b>" + defLabel + " Description:</b></td><td>" + newItemAssetDesc + "</td></tr>")
    } else {
        tempContent.push('<tr><td style="width:50%"><b>Asset Type:</b></td><td style="width:50%">' + newItemAssetName + "</td></tr>");
        tempContent.push("<tr><td><b>Asset Description:</b></td><td>" + newItemAssetDesc + "</td></tr>")
    }
    var $dialog = $("#confirmNewItem");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html(tempContent.join(""));
    var dialogTitle = "Add New Item?";
    if (view.DriverCaption !== null && view.DriverCaption != "") dialogTitle = "Add New " + view.DriverCaption + "?";
    if (error)
        if (closeNewItem) $dialog.dialog({
            width: 475,
            height: 275,
            position: _dialogPos,
            modal: true,
            title: "Error",
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            buttons: {
                Ok: function() {
                    NEW_ITEM_ID = null;
                    $(this).dialog("close");
                    $("#newItemInstance").dialog("close");
                    var existingIndex = setSelectedItem(newItemName, "lstDriver");
                    driverChanged(existingIndex)
                }
            }
        });
        else $dialog.dialog({
            width: 475,
            height: 275,
            position: _dialogPos,
            modal: true,
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
        width: 475,
        height: "auto",
        position: _dialogPos,
        modal: true,
        title: dialogTitle,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            Cancel: function() {
                $(this).dialog("close")
            },
            Submit: function() {
                addNewAssetInstance();
                $(this).dialog("close")
            }
        }
    })
}

function confirmNewPickListValue(property) {
    var closeNewItem = false,
        error = false,
        tempContent = [],
        newPLV = $("#txtNewPickListValue").val();
    if (newPLV == null || newPLV == "") {
        tempContent.push("You must enter a value.");
        error = true
    } else {
        var plvId = pickListValue_Exists(property.PickListId, newPLV);
        if (plvId !== null) {
            tempContent.push("<p>The value '" + newPLV + "' already exists.</p>");
            closeNewItem = true;
            error = true
        }
    }
    if (error == false) {
        tempContent.push('<table style="width:100%">');
        tempContent.push('<tr><td style="width:50%"><b>New Value:</b></td><td style="width:50%"><b>' + newPLV + "</b></td></tr>");
        tempContent.push("</table>")
    }
    var $dialog = $("#confirmNewPickListValue");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html(tempContent.join(""));
    if (error)
        if (closeNewItem) $dialog.dialog({
            width: 450,
            height: 250,
            position: _dialogPos,
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
            height: 250,
            position: _dialogPos,
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
        height: "auto",
        position: _dialogPos,
        modal: true,
        autoOpen: false,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            Cancel: function() {
                var propertyId = jQuery.data(document.body, "propertyId");
                resetField(propertyId);
                delete NEW_VALUES[propertyId];
                if (associativeArrayLength(NEW_VALUES) == 0) {
                    toggleButtons("submitButtons", false);
                    toggleButtons("resetButtons", false)
                }
                $(this).dialog("close")
            },
            Submit: function() {
                if (addNewPickListValue(property)) {
                    var newPLV = $("#" + property.Id + "-id").val(),
                        item = {
                            id: property.Id,
                            value: newPLV
                        };
                    sendValue(item)
                }
            }
        }
    });
    $dialog.dialog("open")
}

function confirmNewUser(property) {
    var closeNewItem = false,
        error = false,
        tempContent, dataType = property.DataType,
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
            tempContent = "Add '" + lastName + ", " + firstName + "' to " + APP_ACRONYM + "?"
        } else {
            tempContent = "User is already in " + APP_ACRONYM + " and has been set to the selected item.";
            $("#" + property.Id).val(user_GetDisplayName(userId));
            var item = {
                id: property.Id,
                value: userId
            };
            sendValue(item);
            error = true
        }
    }
    $("#confirmNewUser").html(tempContent);
    jQuery.data(document.body, "propertyId", property.Id);
    if (error)
        if (closeNewItem) $("#confirmNewUser").dialog({
            width: 450,
            height: 175,
            position: _dialogPos,
            modal: true,
            autoOpen: false,
            title: "Error",
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            buttons: {
                Ok: function() {
                    $("#confirmNewUser").dialog("close")
                }
            }
        });
        else $("#confirmNewUser").dialog({
            width: 450,
            height: 175,
            position: _dialogPos,
            modal: true,
            autoOpen: false,
            title: "Error",
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            buttons: {
                Ok: function() {
                    $("#confirmNewUser").dialog("close")
                }
            }
        });
    else $("#confirmNewUser").dialog({
        width: 450,
        height: 175,
        position: _dialogPos,
        modal: true,
        autoOpen: false,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            Cancel: function() {
                var propertyId = jQuery.data(document.body, "propertyId");
                resetField(propertyId);
                delete NEW_VALUES[propertyId];
                if (associativeArrayLength(NEW_VALUES) == 0) {
                    toggleButtons("submitButtons", false);
                    toggleButtons("resetButtons", false)
                }
                $("#confirmNewUser").dialog("close")
            },
            Submit: function() {
                var newPLV = $("#txtNewPickListValue").val(),
                    newUserGUID = generateGuid(DATA_TYPE_USER);
                createUserInXdb(newUserGUID, newPLV, lastName, firstName, "x");
                var userName = lastName + ", " + firstName + " [" + newPLV + "]",
                    targetControlId = "#" + property.Id.toLowerCase();
                $(targetControlId).val(userName);
                $(targetControlId + "-id").val(newUserGUID);
                var item = {
                    id: property.Id,
                    value: newUserGUID
                };
                sendValue(item);
                $("#confirmNewUser").dialog("close");
                $("#newPickListValue").dialog("close")
            }
        }
    });
    $("#confirmNewUser").dialog("open")
}

function confirmReset() {
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html("This action cannot be undone.");
    $dialog.dialog({
        width: 350,
        height: "auto",
        position: _dialogPos,
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
                resetFields()
            }
        }
    });
    $dialog.dialog("open")
}

function confirmSubmit() {
    var $dialog = $("#confirmSubmit");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html(PRESUBMIT_MSG);
    $dialog.dialog({
        width: 600,
        height: "auto",
        position: _dialogPos,
        title: "Submit these values?",
        modal: true,
        autoOpen: false,
        stack: true,
        open: function(event, ui) {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove();
            $(".ui-dialog-buttonpane button:contains('Submit')").button("disable");
            setTimeout(function() {
                confirmSubmitData()
            }, 50)
        },
        buttons: [{
            text: "Cancel",
            click: function() {
                $(this).dialog("close")
            }
        }, {
            id: "button-submit",
            text: "Submit",
            click: function() {
                submitNewValues()
            }
        }]
    });
    $dialog.dialog("open")
}

function confirmSubmitData() {
    if (this.hasMissingPropertyValues()) {
        $("#confirmSubmit").dialog("close");
        showMissingDataDialog();
        return
    }
    if (ERR_PROPS !== null && ERR_PROPS.toString() != "") {
        var hasValidationErrors = false;
        for (propertyId in ERR_PROPS) {
            hasValidationErrors = true;
            break
        }
        if (hasValidationErrors) {
            $("#confirmSubmit").dialog("close");
            showValidationErrors();
            return
        }
    }
    var tempContent = [];
    tempContent.push('<table style="width:100%">');
    if (associativeArrayLength(NEW_VALUES) > 0) {
        tempContent.push('<tr style="width:100%">');
        tempContent.push('<td style="width:40%"><b>Attribute</b></td><td style="width:60%"><b>New Value</b></td>');
        tempContent.push("</tr>");
        for (propertyId in NEW_VALUES) {
            var propertyValue, tempProperty = PROPERTIES[propertyId],
                subValues = [];
            if (tempProperty.AllowMultiValue) $.each(NEW_VALUES[propertyId], function(index, value) {
                subValues[index] = value
            });
            else subValues[0] = NEW_VALUES[propertyId];
            $.each(subValues, function(index, value) {
                if (value !== null && value != "")
                    if (tempProperty.DataType == DATA_TYPE_PICKLIST) value = pickListValue_GetDisplayValue(tempProperty.PickListId, value);
                    else if (tempProperty.UsesRole || tempProperty.DataType == DATA_TYPE_USER) value = user_GetDisplayName(value);
                else if (tempProperty.DataType == DATA_TYPE_URL) {} else if (tempProperty.DataType == DATA_TYPE_HTML) {} else if (tempProperty.DataType == DATA_TYPE_ASSET || tempProperty.DataType == DATA_TYPE_RELATION_PARENTCHILD || tempProperty.DataType == DATA_TYPE_RELATION_CHILDPARENT || tempProperty.DataType == DATA_TYPE_DEPENDENCY || tempProperty.DataType == DATA_TYPE_RELATION_OTHER) value = asset_GetName(value);
                else if (tempProperty.IsSystem && tempProperty.SystemType == SYSTEM_TYPE_ASSET_TYPE) value = assetType_GetName(value, false);
                else if (tempProperty.DataType == DATA_TYPE_CURRENCY) {
                    value = propertyValue_GetDisplayValue(tempProperty.Id, value);
                    var vals = value.split(DELIMITER_MULTI_VALUE);
                    value = htmlDecode(vals[0]) + vals[1]
                } else {
                    var decipher = true;
                    switch (tempProperty.DataType) {
                        case DATA_TYPE_DATE:
                        case DATA_TYPE_DATETIME:
                        case DATA_TYPE_FLOAT:
                        case DATA_TYPE_INT:
                        case DATA_TYPE_STRING:
                        case DATA_TYPE_MEMO:
                        case DATA_TYPE_IPv4:
                        case DATA_TYPE_IPv6:
                        case DATA_TYPE_SYSTEM_INSTANCEOF_DESC:
                        case DATA_TYPE_HTML:
                            decipher = false;
                            break
                    }
                    if (decipher) value = propertyValue_GetDisplayValue(tempProperty.Id, value)
                }
                tempContent.push('<tr><td style="width:40%">' + tempProperty.Name + '</td><td style="width:60%">' + value + "</td></tr>")
            })
        }
    }
    if (associativeArrayLength(DELETED_VALUES) > 0) {
        tempContent.push('<tr style="width:100%">');
        tempContent.push('<td style="width:40%"><b>Attribute</b></td><td style="width:60%"><b>Deleted Value</b></td>');
        tempContent.push("</tr>");
        for (propertyValueId in DELETED_VALUES)
            for (pvId in PROPVALS) {
                var item = PROPVALS[pvId];
                if (item.Id.toLowerCase() == propertyValueId.toLowerCase()) {
                    var tempProperty = PROPERTIES[item.PropertyId],
                        displayValue = item.Value,
                        decipher = true;
                    switch (tempProperty.DataType) {
                        case DATA_TYPE_DATE:
                        case DATA_TYPE_DATETIME:
                        case DATA_TYPE_FLOAT:
                        case DATA_TYPE_INT:
                        case DATA_TYPE_STRING:
                        case DATA_TYPE_MEMO:
                        case DATA_TYPE_IPv4:
                        case DATA_TYPE_IPv6:
                        case DATA_TYPE_SYSTEM_INSTANCEOF_DESC:
                        case DATA_TYPE_HTML:
                            decipher = false;
                            break
                    }
                    if (decipher) displayValue = propertyValue_GetDisplayValue(item.PropertyId, item.Value);
                    tempContent.push('<tr><td style="width:40%">' + tempProperty.Name + '</td><td style="width:60%">' + displayValue + "</td></tr>");
                    break
                }
            }
    }
    tempContent.push("</table>");
    var $dialog = $("#confirmSubmit");
    $dialog.html(tempContent.join(""));
    $("#button-submit").removeAttr("disabled").removeClass("ui-state-disabled").addClass("ui-state-enabled").focus()
}

function createContextMenu() {
    var option = null;
    if (SELECTED_VIEW.prefetchAssets) option = {
        width: 150,
        items: [{
            text: "Previous Item",
            icon: _contextMenuSrc + "ctx-prev.png",
            alias: "prev",
            action: menuAction
        }, {
            text: "Next Item",
            icon: _contextMenuSrc + "ctx-next.png",
            alias: "next",
            action: menuAction
        }, {
            text: "Filter Assets",
            icon: _contextMenuSrc + "ctx-filter.png",
            alias: "filter",
            action: menuAction
        }, {
            type: "splitLine"
        }, {
            text: "Print",
            icon: _contextMenuSrc + "ctx-print.png",
            alias: "print",
            action: menuAction
        }, {
            text: "History",
            icon: _contextMenuSrc + "ctx-history.png",
            alias: "history",
            action: menuAction
        }, {
            text: "Unlock",
            icon: _contextMenuSrc + "ctx-unlock.png",
            alias: "unlock",
            action: menuAction
        }]
    };
    else option = {
        width: 150,
        items: [{
            text: "Filter Assets",
            icon: _contextMenuSrc + "ctx-filter.png",
            alias: "filter",
            action: menuAction
        }, {
            type: "splitLine"
        }, {
            text: "Print",
            icon: _contextMenuSrc + "ctx-print.png",
            alias: "print",
            action: menuAction
        }, {
            text: "History",
            icon: _contextMenuSrc + "ctx-history.png",
            alias: "history",
            action: menuAction
        }, {
            text: "Unlock",
            icon: _contextMenuSrc + "ctx-unlock.png",
            alias: "unlock",
            action: menuAction
        }]
    };
    $("#innerContainer").contextmenu(option)
}

function loadEditModeScripts() {
    $.loadScript(_jsTemplate, true, function() {
        $.loadScript(_jsTransport, true, function() {
            $.loadScript(_jsFileUpload, true, function() {
                $.loadScript(_jsFileUploadUI, true, function() {
                    $.loadScript(_jsCkEditor, true, function() {
                        $.loadScript(_jsCkEditorJson, true, function() {
                            return
                        })
                    })
                })
            })
        })
    })
}

function menuAction() {
    switch (this.data.alias) {
        case "filter":
            showFilterDialog();
            break;
        case "history":
            showHistory();
            break;
        case "prev":
            onPreviousSelected();
            break;
        case "next":
            onNextSelected();
            break;
        case "print":
            showPrintDialog();
            break;
        case "unlock":
            changeMode(false, true);
            break;
        default:
            alert(this.data.alias)
    }
}

function createHtmlCurrency(controlId, isReadOnly) {
    CTL_CURR = true;
    var html = [];
    html.push('<div style="width:50%"><input style="width:10%;" id="' + controlId + '_curr" class="ui-widget ui-widget-content currencySymbol"/>');
    html.push('<input type="hidden" id="' + controlId + '_curr-id"/>');
    html.push('<input type="text" id="' + controlId + '" disabled="disabled" style="margin-left:3px;" onChange="sendValue(this)" onKeyPress="return validateFloat(event);" class="ui-widget ui-widget-content"/></div>');
    return html.join("")
}

function createHtmlDatePicker(controlId, isReadOnly) {
    if (isReadOnly) return '<label id="' + controlId + '" class="ui-widget">';
    else {
        CTL_DTPS = true;
        return '<input type="text" style="width:35%" class="ui-widget ui-widget-content datepicker" disabled="true" id="' + controlId + '" />'
    }
}

function createHtmlEditor(controlId, isReadOnly) {
    CTL_EDITORS = true;
    return '<textarea class="CKeditor" id="' + controlId + '"></textarea>'
}

function createHtmlMemo(controlId, isReadOnly) {
    if (isReadOnly) return '<textarea cols="40" rows="10" id="' + controlId + '" style="width:90%" disabled="true" onChange="sendValue(this)" class="ui-widget ui-widget-content read-only"/>';
    else return '<textarea cols="40" rows="10" id="' + controlId + '" style="width:90%" disabled="true" onChange="sendValue(this)" class="ui-widget ui-widget-content"/>'
}

function createHtmlMultiSelect_autoComplete(controlId, isReadOnly) {
    try {
        var tempValue = [];
        tempValue.push('<table style="width:90%;">');
        tempValue.push('<tr style="width:100%;"><td style="width:92%;">');
        tempValue.push(createHtmlSelect_autocomplete(controlId, isReadOnly));
        tempValue.push("</td>");
        tempValue.push('<td style="width:4%;"><input type="button" id="btnAddValue" class="deleteButtons ui-state-disabled ui-corner-all" value="+" onClick="addMultiValueItem(\'' + controlId + "')\"/></td>");
        tempValue.push('<td style="width:4%;"><input type="button" id="btnRemoveValue" class="deleteButtons ui-state-disabled ui-corner-all" value="-" onClick="removeMultiValueItem(\'' + controlId + "')\"/></td>");
        tempValue.push("</tr>");
        tempValue.push('<tr style="width:100%;"><td colspan="3">');
        if (isReadOnly) tempValue.push('<select disabled="disabled" id="' + controlId + '_tgt" size=5 class="ui-widget read-only" style="width:100%;overflow:scroll;"></select>');
        else tempValue.push('<select disabled="disabled" id="' + controlId + '_tgt" size=5 class="ui-widget ui-widget-content" style="width:99%;overflow:scroll;"></select>');
        tempValue.push("</td></tr></table>");
        return tempValue.join("")
    } finally {}
}

function createHtmlMultiSelect(controlId, allowEnterNew, valuesString, isReadOnly) {
    var tempValue = '<table style="width:100%;">';
    tempValue += '<tr style="width:100%;"><td style="width:92%;">';
    if (isReadOnly) tempValue += '<select  style="width:100%;" disabled="disabled" id="' + controlId + '" class="ui-widget read-only">';
    else tempValue += '<select  style="width:100%;" disabled="disabled" id="' + controlId + '" class="ui-widget" onChange="sendValue(this)">';
    tempValue += '<option value="-1" selected="true">[Select]</option>';
    if (allowEnterNew) tempValue += '<option value="0">[Enter New]</option>';
    tempValue += valuesString;
    tempValue += "</select></td>";
    tempValue += '<td style="width:4%;"><input type="button" id="btnAddValue" class="deleteButtons ui-state-disabled ui-corner-all" value="+" onClick="addMultiValueItem(\'' + controlId + "')\"/></td>";
    tempValue += '<td style="width4%;"><input type="button" id="btnRemoveValue" class="deleteButtons ui-state-disabled ui-corner-all" value="-" onClick="removeMultiValueItem(\'' + controlId + "')\"/></td>";
    tempValue += "</tr>";
    tempValue += '<tr style="width:100%;"><td colspan="3">';
    if (isReadOnly) tempValue += '<select disabled="disabled" id="' + controlId + '_tgt" size=5 class="ui-widget read-only" style="width:100%;overflow:scroll;"></select>';
    else tempValue += '<select id="' + controlId + '_tgt" size=5 class="ui-widget" style="width:100%;overflow:scroll;"></select>';
    tempValue += "</td></tr>";
    tempValue += "</table>";
    return tempValue
}

function createHtmlSelect_autocomplete(controlId, isReadOnly) {
    try {
        if (isReadOnly) return '<label id="' + controlId + '" class="ui-widget"/>';
        else return tempValue = '<input disabled="disabled" id="' + controlId + '" class="ui-widget ui-widget-content autocomplete" style="width:90%;" onfocus="onAutoCompleteFocus(this.id)"/>' + '<input type="hidden" id="' + controlId + '-id"/>'
    } finally {}
}

function foo(id) {
    $("#" + id).select();
    $("#" + id).autocomplete({
        minLength: 0
    });
    $("#" + id).autocomplete("search", "")
}

function createHtmlUrl(controlId, isReadOnly, isMultiValue) {
    var html = [];
    if (isReadOnly)
        if (isMultiValue) html.push('<div id="' + controlId + '_tgt" class="ui-widget" style="width:100%;overflow:scroll;"></div>');
        else {
            html.push('<table style="width:100%"><tr>');
            html.push('<td style="width:15%">Name (optional):</td>');
            html.push('<td style="width:85%"><label id="' + controlId + '_name" class="ui-widget"></td>');
            html.push("</tr><tr>");
            html.push('<td style="width:85%"><label id="' + controlId + '_url" class="ui-widget"></td>');
            html.push("</tr></table>")
        }
    else {
        html.push('<table style="width:90%">');
        html.push("<tr>");
        html.push('<td style="width:15%">Name (optional):</td>');
        if (isMultiValue) {
            html.push('<td style="width:85%"><input type="text" id="' + controlId + '_name" disabled="disabled" class="ui-widget ui-widget-content" style="width:100%"/></td>');
            html.push("</tr>");
            html.push("<tr>");
            html.push('<td style="width:15%">URL:</td>');
            html.push('<td style="width:85%"><input type="text" id="' + controlId + '_url" disabled="disabled" class="ui-widget ui-widget-content" style="width:100%"/></td>');
            html.push("</tr>");
            html.push("<tr>");
            html.push("<td>");
            html.push('<input type="button" style="width:75%" id="btnAddValue" class="deleteButtons ui-state-disabled ui-corner-all" value="Add URL" onClick="addUrl(\'' + controlId + "')\"/>");
            html.push("<br/><br/>");
            html.push('<input type="button" style="width:75%" id="btnRemoveValue" class="deleteButtons ui-state-disabled ui-corner-all" value="Remove URL" onClick="removeMultiValueUrl(\'' + controlId + "')\"/>");
            html.push("</td>");
            html.push("<td>");
            html.push('<select disabled="disabled" id="' + controlId + '_tgt" size=5 class="ui-widget" style="width:100%;overflow:scroll;">');
            html.push("</td>");
            html.push("</tr>")
        } else {
            html.push('<td style="width:85%"><input type="text" id="' + controlId + '_name" disabled="disabled" onChange="sendValueUrl(\'' + controlId + '\')" class="ui-widget ui-widget-content" style="width:100%"/></td>');
            html.push("</tr>");
            html.push("<tr>");
            html.push('<td style="width:15%">URL:</td>');
            html.push('<td style="width:85%"><input type="text" id="' + controlId + '_url" disabled="disabled" onChange="sendValueUrl(\'' + controlId + '\')" class="ui-widget ui-widget-content" style="width:100%"/></td>');
            html.push("</tr>")
        }
        html.push("</table>")
    }
    return html.join("")
}

function createHtmlSelect(controlId, allowEnterNew, valuesString, isReadOnly) {
    try {
        var tempValue;
        if (isReadOnly) return '<label id="' + controlId + '" class="ui-widget"/>';
        else tempValue = '<select disabled="disabled" id="' + controlId + '" class="ui-widget ui-state-default" onChange="sendValue(this)" >';
        tempValue += '<option value="-1">[Select]</option>';
        if (allowEnterNew) tempValue += '<option value="0">[Enter New]</option>';
        tempValue += valuesString;
        tempValue += "</select>";
        return tempValue
    } finally {}
}

function createAssetSelectionDialog(controlId, allowEnterNew, valuesString, viewAssetTypeId, propertyId, assetTypeId) {}

function createHtmlText(property) {
    if (property.IsSystem) return createHtmlTextForSystemType(property.Id, property.IsReadOnly, property.SystemType);
    else return createHtmlTextForDataType(property.Id, property.IsReadOnly, property.DataType)
}

function createHtmlTextForDataType(controlId, isReadOnly, dataType) {
    try {
        if (isReadOnly) return '<label id="' + controlId + '" class="ui-widget">';
        else switch (dataType) {
            case DATA_TYPE_INT:
                return '<input type="text" id="' + controlId + '" disabled="disabled" onChange="sendValue(this)" onKeyPress="return validateInteger(event);" class="ui-widget ui-widget-content"/>';
            case DATA_TYPE_FLOAT:
            case DATA_TYPE_CURRENCY:
                return '<input type="text" id="' + controlId + '" disabled="disabled" onChange="sendValue(this)" onKeyPress="return validateFloat(event);" class="ui-widget ui-widget-content"/>';
            case DATA_TYPE_IPv4:
                return '<input type="text" id="' + controlId + '" disabled="disabled" onChange="sendValue(this)" onKeyPress="return validateIPv4(event);" class="ui-widget ui-widget-content"/>';
            default:
                return '<input type="text" id="' + controlId + '" disabled="disabled" onChange="sendValue(this)" class="ui-widget ui-widget-content" style="width:90%;"/>'
        }
    } finally {}
}

function createHtmlTextForSystemType(controlId, isReadOnly, systemType) {
    try {
        if (isReadOnly) return '<label id="' + controlId + '" class="ui-widget">';
        else switch (systemType) {
            case SYSTEM_TYPE_ASSET_NAME:
                return '<input type="text" id="' + controlId + '" disabled="disabled" onChange="sendValueAssetName(this)" class="ui-widget ui-widget-content" style="width:90%;"/>';
            default:
                return '<input type="text" id="' + controlId + '" disabled="disabled" onChange="sendValue(this)" class="ui-widget ui-widget-content" style="width:90%;"/>'
        }
    } finally {}
}

function createFileUpload(property) {
    CTL_FILE = true;
    var html = [];
    if (property.DataType == DATA_TYPE_DOCUMENT) html.push('<div id="' + property.Id + '" class="docupload fileupload" style="width:50%">');
    else html.push('<div id="' + property.Id + '" class="imgupload fileupload" style="width:50%">');
    html.push('<form method="POST" enctype="multipart/form-data">');
    html.push('<div class="fileupload-buttonbar">');
    html.push('<label class="fileinput-button">');
    if (property.DataType == DATA_TYPE_DOCUMENT) html.push("<span>Select Document ...</span>");
    else html.push("<span>Select Image ...</span>");
    html.push('<input type="file" name="files[]">');
    html.push("</label>");
    html.push("</div>");
    html.push("</form>");
    html.push('<div class="fileupload-content">');
    html.push('<table class="files"></table>');
    html.push('<div class="fileupload-progressbar"></div>');
    html.push("</div>");
    html.push('<div id="div' + property.Id + '"></div>');
    return html.join("")
}

function createPropertyInputType(property) {
    if (property.IsSystem) switch (property.SystemType) {
        case SYSTEM_TYPE_ASSET_NAME:
            return createHtmlText(property);
        case SYSTEM_TYPE_ASSET_TYPE:
            return createHtmlSelect_autocomplete(property.Id, property.IsReadOnly);
        case SYSTEM_TYPE_CREATED:
            return createHtmlText(property);
        case SYSTEM_TYPE_CREATED_BY:
            return createHtmlText(property);
        case SYSTEM_TYPE_DELETED:
            return createHtmlText(property);
        case SYSTEM_TYPE_DELETED_BY:
            return createHtmlText(property);
        case SYSTEM_TYPE_DESCRIPTION:
            return createHtmlText(property);
        case SYSTEM_TYPE_INSTANCE_OF:
            return createHtmlText(property);
        case SYSTEM_TYPE_INSTANCE_OF_DESCRIPTION:
            return createHtmlText(property);
        case SYSTEM_TYPE_LAST_MODIFIED:
            return createHtmlText(property);
        case SYSTEM_TYPE_LAST_MODIFIED_BY:
            return createHtmlText(property)
    } else switch (property.DataType) {
        case DATA_TYPE_ASSET:
            if (property.AllowMultiValue) return createHtmlMultiSelect_autoComplete(property.Id, property.IsReadOnly);
            else {
                var readOnly = property.IsReadOnly || property.IsInheritedValue;
                return createHtmlSelect_autocomplete(property.Id, readOnly)
            }
        case DATA_TYPE_CURRENCY:
            return createHtmlCurrency(property.Id, property.IsReadOnly);
        case DATA_TYPE_DATE:
            return createHtmlDatePicker(property.Id, property.IsReadOnly);
        case DATA_TYPE_DATETIME:
            return createHtmlDatePicker(property.Id, property.IsReadOnly);
        case DATA_TYPE_DEPENDENCY:
            if (property.AllowMultiValue) return createHtmlMultiSelect_autoComplete(property.Id, property.IsReadOnly);
            else return createHtmlSelect_autocomplete(property.Id, property.IsReadOnly);
        case DATA_TYPE_FLOAT:
            return createHtmlText(property);
        case DATA_TYPE_DOCUMENT:
        case DATA_TYPE_IMAGE:
            return createFileUpload(property);
        case DATA_TYPE_HTML:
            return createHtmlEditor(property.Id, property.IsReadOnly);
        case DATA_TYPE_INT:
            return createHtmlText(property);
        case DATA_TYPE_MEMO:
            return createHtmlMemo(property.Id, property.IsReadOnly);
        case DATA_TYPE_RELATION_CHILDPARENT:
            if (property.AllowMultiValue) return createHtmlMultiSelect_autoComplete(property.Id, property.IsReadOnly);
            else return createHtmlSelect_autocomplete(property.Id, property.IsReadOnly);
        case DATA_TYPE_RELATION_PARENTCHILD:
            if (property.AllowMultiValue) return createHtmlMultiSelect_autoComplete(property.Id, property.IsReadOnly);
            else return createHtmlSelect_autocomplete(property.Id, property.IsReadOnly);
        case DATA_TYPE_RELATION_OTHER:
            break;
        case DATA_TYPE_PICKLIST:
            if (property.AllowMultiValue) {
                var optionValues = [];
                $.each(pickListValues(property.PickListId), function(index, value) {
                    optionValues.push('<option value="' + value.key.toLowerCase() + '">' + value.value + "</option>")
                });
                return createHtmlMultiSelect(property.Id, false, optionValues.join(""), property.IsReadOnly)
            } else {
                var readOnly = property.IsReadOnly || property.IsInheritedValue;
                return createHtmlSelect_autocomplete(property.Id, readOnly)
            }
        case DATA_TYPE_STRING:
            return createHtmlText(property);
        case DATA_TYPE_URL:
            return createHtmlUrl(property.Id, property.IsReadOnly, property.AllowMultiValue);
        case DATA_TYPE_USER:
            if (property.AllowMultiValue) return createHtmlMultiSelect_autoComplete(property.Id, property.IsReadOnly);
            else return createHtmlSelect_autocomplete(property.Id, property.IsReadOnly);
        case DATA_TYPE_UNDEFINED:
            alert("undefined data type for " + property.Name);
            break;
        default:
            return createHtmlText(property)
    }
}

function createTabContent(propertyGroup, tabIndex) {
    try {
        var tempContent = [];
        tempContent.push('<form style="width:100%;"class="cmxform" id="' + propertyGroup.Id + '" method="get" action="">');
        tempContent.push('<fieldset style="width:100%;border:none;">');
        tempContent.push('<table style="width:100%">');
        var propCount = propertyGroup.Properties.length,
            view = SELECTED_VIEW;
        for (var i = 0; i < propCount; i++) {
            property = propertyGroup.Properties[i];
            PROPS_TABS[property.Id] = propertyGroup.Name;
            if (property.IsRequired) REQ_PROPS[property.Id] = false;
            tempContent.push('<tr class="vwTr">');
            tempContent.push('<td style="width:2%">');
            var toolTipTitle, toolTipText;
            if (property.Description !== null && property.Description != "" && property.Description != PROP_DESC_MISSING) {
                toolTipTitle = $.trim(property.Name.replace(/'/gi, "\\'"));
                toolTipText = $.trim(property.Description.replace(/'/gi, "\\'"));
                if (property.IsReadOnly) {
                    toolTipText += "<br/><br/>This attribute is read-only.";
                    if (property.IsInheritedValue) toolTipTitle += " (Read-Only/Inherited Value)";
                    else toolTipTitle += " (Read-Only)"
                }
                if (property.IsInheritedValue) {
                    toolTipText += "<br/><br/>This attribute value is inherited.";
                    if (!property.IsReadOnly) toolTipTitle += " (Inherited Value)"
                }
                tempContent.push(insertIcon(ICON_TOOLTIP, toolTipTitle, toolTipText))
            } else if (property.IsReadOnly || property.IsInheritedValue) {
                if (property.IsReadOnly) {
                    toolTipText = "This attribute is read-only.";
                    if (property.IsInheritedValue) toolTipTitle = "(Read-Only/Inherited Value)";
                    else toolTipTitle = "(Read-Only)"
                }
                if (property.IsInheritedValue) {
                    toolTipText += "<br/><br/>This attribute value is inherited.";
                    if (!property.IsReadOnly) toolTipTitle = "(Inherited Value)"
                }
                tempContent.push(insertIcon("icon-info", toolTipTitle, toolTipText))
            }
            tempContent.push("</td>");
            if (view.IsReadOnly) tempContent.push('<td style="width:24%">' + property.Name + "</td>");
            else if (property.IsRequired) tempContent.push('<td style="width:24%" class="vwReqLbl">' + property.Name + " *</td>");
            else tempContent.push('<td style="width:24%">' + property.Name + "</td>");
            tempContent.push('<td style="width:74%" id="td' + property.Id + '" class="property-value">');
            PROPERTIES[property.Id] = property;
            if (view.IsReadOnly == false && CURRENT_MODE == MODE_EDIT) {
                if (property.Triggers !== undefined && property.Triggers !== null && property.Triggers.length > 0) {
                    var triggerCount = property.Triggers.length;
                    for (var z = 0; z < triggerCount; z++) {
                        var trigger = property.Triggers[z];
                        switch (trigger.TriggerType) {
                            case 0:
                                var triggerInfo = {};
                                triggerInfo.ParentPropertyId = trigger.ParentPropertyId;
                                triggerInfo.ChildPropertyId = trigger.TargetPropertyId;
                                triggerInfo.ParentPropertyValue = null;
                                triggerInfo.SourcePropertyId = trigger.SourcePropertyId;
                                PICKLIST_FILTERS[trigger.TargetPropertyId] = triggerInfo
                        }
                    }
                }
                tempContent.push(createPropertyInputType(property))
            }
            tempContent.push("</td></tr>")
        }
        tempContent.push("</table>");
        if (view.IsReadOnly == false && CURRENT_MODE == MODE_EDIT) {
            tempContent.push("<table><tr>");
            tempContent.push('<td><input type="button" id="btnDelete" class="deleteButtons ui-state-disabled ui-corner-all" value="Delete Item" onClick="confirmDelete()"/></td>');
            tempContent.push('<td><input type="button" id="btnReset" class="resetButtons ui-state-disabled ui-corner-all" value="Discard Changes" onClick="confirmReset()"/></td>');
            tempContent.push('<td><input type="button" id="btnSubmit" class="submitButtons ui-state-disabled ui-corner-all" value="Save Changes" onClick="confirmSubmit();"/></td>');
            tempContent.push("</tr></table>")
        }
        tempContent.push("</fieldset></form>");
        return tempContent.join("")
    } finally {}
}

function deleteSelectedItem() {
    $("#messageDialog").dialog("close");
    var message;
    if (asset_Delete(USER_ACTUAL.Id, SELECTED_ITEM_ID)) message = "<label>Item deleted successfully</label>";
    else message = "<label>Item failed to delete</label>";
    var $dialog = $("#deletionResultDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html(message);
    $dialog.dialog({
        width: 350,
        height: 200,
        position: _dialogPos,
        modal: true,
        autoOpen: false,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            Ok: function() {
                autoComplete_RemoveByValue("lstDriver", SELECTED_ITEM_ID);
                SELECTED_ITEM_ID = null;
                CURRENT_MODE = MODE_VIEW;
                $driver.val("");
                driverChanged(SELECTED_ITEM_ID);
                clearForm($tabs, true, true);
                $(this).dialog("close")
            }
        }
    });
    $dialog.dialog("open")
}

function displayFilters(id) {
    var filterValue = viewFilter_GetDisplay(id);
    $("#divFilterDisplay").html(filterValue)
}

function displayFiltersBySearchClient(searchClient) {
    var filterValue = viewFilter_GetDisplayViaSearchClient(searchClient);
    $("#divFilterDisplay").html(filterValue)
}

function driverChanged(assetId) {
    refreshRecent(_imgSrc);
    if (NEW_VALUES !== null && NEW_VALUES != [] && associativeArrayLength(NEW_VALUES) > 0) {
        var proposedSelectionValue = $("#lstDriver-id").val();
        if (SELECTED_ITEM_ID !== null && SELECTED_ITEM_ID != proposedSelectionValue) {
            var itemName = null;
            if (SELECTED_ASSET.DisplayValue == null || SELECTED_ASSET.DisplayValue == "") itemName = SELECTED_ASSET.Name;
            else itemName = SELECTED_ASSET.DisplayValue;
            warnUnsavedChanges(itemName, assetId);
            return false
        }
    }
    NEW_VALUES = [];
    ERR_PROPS = [];
    DELETED_VALUES = [];
    var asset = SELECTED_ASSET;
    if (SELECTED_ASSET !== null && SELECTED_ASSET == undefined && SELECTED_ASSET.Id.toLowerCase() != assetId.toLowerCase()) {
        asset = null;
        PROPERTY_VALUES = [];
        PROPVALS = []
    }
    var view = SELECTED_VIEW;
    if (assetId == 0) {
        if (view.AssetTypeId !== null && view.AssetTypeId !== "")
            if (IS_INSTANCE_VIEW) showNewItemInstanceDialog();
            else showNewItemDialog();
        return
    } else if (assetId !== null && assetId != undefined) {
        if (asset === null || asset == undefined || asset.Id.toLowerCase() != assetId.toLowerCase()) {
            asset = asset_Get(assetId, USER_CURRENT.Id, view.Id);
            PROPERTY_VALUES = [];
            PROPVALS = [];
            var pvCount = asset.PropertyValues.length;
            for (var i = 0; i < pvCount; i++) {
                var item = asset.PropertyValues[i];
                PROPERTY_VALUES[item.PropertyId] = item;
                PROPVALS[item.Id] = item
            }
            SELECTED_ASSET = asset
        }
        var assetRequestType;
        if (view.IsIntance) assetRequestType = ART_INSTANCE;
        else assetRequestType = ART_DEFINITION;
        if (!assetType_HasAssets(view.AssetTypeId, assetRequestType, false) || !viewIsCompatible(view.Id, asset.AssetTypeId, asset.IsInstance)) {
            var defaultViewId = null;
            if (view.IsInstance) defaultViewId = ASSET_TYPES_VIEWS_INS[asset.AssetTypeId];
            else defaultViewId = ASSET_TYPES_VIEWS[asset.AssetTypeId];
            if (defaultViewId !== null && defaultViewId != undefined && defaultViewId != view.Id) renderView(defaultViewId)
        }
    }
    if ($tabs.tabs !== null) {
        var tabCount = $tabs.tabs("length");
        for (var i = 0; i < tabCount; i++)
            if (assetId == 0) {
                if (i != 0) $tabs.tabs("disable", i)
            } else $tabs.tabs("enable", i)
    }
    SELECTED_ITEM_ID = assetId;
    if (assetId == null) {
        document.title = APP_ACRONYM + " - " + view.Name;
        clearForm($tabs, true, true);
        $(":input").attr("disabled", "disabled");
        $driver.removeAttr("disabled");
        $toolbar.html(toolbarCreateFilter(view))
    } else {
        if (VIEW_FROM_PROCESS === "true" && NEW_ITEM_ID !== null) asset.AllowEdit = true;
        if (!view.IsReadOnly) view.IsReadOnly = !asset.AllowEdit;
        var reRenderView = false;
        if (CURRENT_MODE == MODE_EDIT && !asset.AllowEdit) {
            CURRENT_MODE = MODE_VIEW;
            reRenderView = true
        }
        var toolbarHtml = toolbarCreate(view, CURRENT_MODE, bookMarksContainsAssetId(SELECTED_ITEM_ID));
        $toolbar.html(toolbarHtml);
        toggleModeIconEnabled(!view.IsReadOnly);
        var assetName = null;
        if (asset.DisplayValue == null || asset.DisplayValue == "") assetName = SELECTED_ASSET.Name;
        else assetName = SELECTED_ASSET.DisplayValue;
        document.title = APP_ACRONYM + " - " + view.Name + " - " + assetName;
        if (reRenderView) {
            renderView(view.Id);
            $driver.val(assetName);
            driverChanged(SELECTED_ITEM_ID)
        } else {
            clearForm($tabs, false, false);
            $(":input").removeAttr("disabled");
            $(".read-only").attr("disabled", "disabled");
            toggleButtons("deleteButtons", asset.AllowDelete);
            toggleButtons("submitButtons", asset.AllowEdit);
            toggleButtons("resetButtons", asset.AllowEdit);
            populateForm(asset)
        }
    }
    $tabs.removeClass("ui-widget-overlay");
    if (CURRENT_MODE == MODE_EDIT) {
        activateAutoCompletes();
        activateCurrencySelector();
        activateFileUploader();
        activateHtmlEditor()
    }
    $driver.autocomplete("close");
    createContextMenu()
}

function viewIsCompatible(viewId, assetTypeId, isInstance) {
    var requestType = ART_DEFINITION;
    if (isInstance) requestType = ART_INSTANCE;
    return viewAndAssetTypeAreCompatible(viewId, assetTypeId, requestType)
}

function getHistory(assetId, viewId, boolOrder) {
    try {
        var retVal = [];
        retVal.push("<table width='95%'>");
        alphaSortLastPropertName = "";
        $.each(historicalValues_Get(assetId, viewId, boolOrder), function(index, item) {
            if (boolOrder == true) retVal.push('<tr><td style="width:35%;vertical-align:top;">' + item.CreationInfo + '</td><td style="width:25%;vertical-align:top;">' + item.PropertyName + '</td><td style="width:40%;vertical-align:top;">' + item.PropertyValue + "</td></tr>");
            else {
                var propName = alphaSortLastPropertName == item.PropertyName ? "" : item.PropertyName;
                retVal.push('<tr><td style="width:25%;vertical-align:top;">' + propName + '</td><td style="width:40%;vertical-align:top;">' + item.PropertyValue + '</td><td style="width:35%;vertical-align:top;">' + item.CreationInfo + "</td></tr>");
                alphaSortLastPropertName = item.PropertyName
            }
        });
        retVal.push("</table>");
        return retVal.join("")
    } finally {}
}

function hasMissingPropertyValues() {
    try {
        var returnValue = false;
        if (REQ_PROPS !== null)
            for (propertyId in REQ_PROPS)
                if (REQ_PROPS[propertyId] == false) {
                    returnValue = true;
                    break
                } return returnValue
    } finally {}
}

function insertDetailsIcon(assetId, assetTypeId, assetName, propertyValue, propertyName) {
    try {
        var dialogTitle = null;
        if (assetName !== null) dialogTitle = assetName;
        else dialogTitle = propertyName;
        dialogTitle = dialogTitle.replace('"', "");
        var textToShow = "<img src='images/ajax-loader.gif' />",
            html = [];
        html.push('<span class="ui-icon ui-' + ICON_DETAILS + '"');
        html.push(" title=\"Find other items related to '" + dialogTitle + "'\" onClick=\"showMessageForDetails('" + dialogTitle + "'");
        if (assetId !== null)
            if (assetTypeId == null) html.push(", true, '" + assetId + "', " + null + ", " + null + ');"');
            else html.push(", true, '" + assetId + "', " + "'" + assetTypeId + "', " + null + ');"');
        else html.push(", true, " + null + ", " + null + ", '" + propertyValue + "');\"");
        html.push(' onClick="hideMessage();"></span>');
        return html.join("")
    } finally {}
}

function insertIcon(iconName, propertyName, toolTip) {
    return '<span class="ui-icon ui-' + iconName + '" onMouseOver="showMessage(\'' + propertyName + "', '" + toolTip + '\', false, 400, false);" onMouseOut="hideMessage();"></span>'
}

function onMigrateAssetClicked() {
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
        position: _dialogPos,
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
                onMigrateAssetSelected(key, val)
            }
        }
    });
    $dialog.dialog("open")
}

function onMigrateAssetSelected(targetKey, targetValue) {
    var display = SELECTED_ASSET.Name;
    if (SELECTED_ASSET.DisplayValue != null && SELECTED_ASSET.DisplayValue != "") display = SELECTED_ASSET.DisplayValue;
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html("Migrate <i>" + display + "</i> to " + targetValue + "?");
    $dialog.dialog({
        width: 350,
        height: "auto",
        position: _dialogPos,
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
                var migrated = asset_Migrate(SELECTED_ASSET.Id, targetKey);
                if (migrated) showMessage("Result", "Asset migrated successfully.", true, 400, true)
            }
        }
    });
    $dialog.dialog("open")
}

function onNextSelected() {
    var found = false,
        nextId = null,
        nextName = null,
        disableNext = false,
        source = $driver.autocomplete("option", "source"),
        l = source.length;
    if (l == undefined) {
        $driver.focus();
        source = $driver.autocomplete("option", "source");
        l = source.length
    }
    for (var i = 0; i < l; i++)
        if (source[i].key == SELECTED_ITEM_ID) {
            if (i == l - 1) disableNext = true;
            var nextIndex = parseInt(i + 1, 10);
            if (nextIndex < l) {
                var nextItem = source[nextIndex];
                nextId = nextItem.key;
                nextName = nextItem.value;
                found = true
            }
            break
        } if (found) {
        $driver.val(nextName);
        SELECTED_ITEM_ID = nextId;
        $driver.autocomplete("close");
        driverChanged(SELECTED_ITEM_ID)
    }
    if (disableNext) toggleButton("imgNext", false, true);
    toggleButton("imgPrev", true, false)
}

function onPreviousSelected() {
    var found = false,
        previousId = null,
        previousName = null,
        disablePrevious = false,
        source = $driver.autocomplete("option", "source"),
        l = source.length;
    if (l == undefined) {
        $driver.focus();
        source = $driver.autocomplete("option", "source");
        l = source.length
    }
    for (var i = 0; i < l; i++)
        if (source[i].key == SELECTED_ITEM_ID) {
            if (i == 0) disablePrevious = true;
            else {
                var previousIndex = parseInt(i - 1, 10);
                previousId = source[previousIndex].key;
                previousName = source[previousIndex].value;
                found = true;
                if (previousId == 0) {
                    disablePrevious = true;
                    found = false
                }
            }
            break
        } if (found) {
        $driver.val(previousName);
        SELECTED_ITEM_ID = previousId;
        $driver.autocomplete("close");
        driverChanged(SELECTED_ITEM_ID)
    }
    if (disablePrevious) toggleButton("imgPrev", false, true);
    toggleButton("imgNext", true, false)
}

function populateForm(assetClient) {
    if (assetClient.PropertyValues == null || assetClient.PropertyValues.length == 0) return;
    $("select[id*='_tgt']").each(function() {
        selectOptionsRemove(this.id)
    });
    var assetNameHtml = [];
    if (SELECTED_VIEW.IsReadOnly || CURRENT_MODE == MODE_VIEW) {
        assetNameHtml.push("<label style='float:left'>" + assetClient.Name + "</label>");
        assetNameHtml.push(insertDetailsIcon(assetClient.Id, assetClient.AssetTypeId, assetClient.Name, assetClient.Id, assetClient.Name));
        var pvCount = assetClient.PropertyValues.length;
        for (var i = 0; i < pvCount; i++) {
            var item = assetClient.PropertyValues[i],
                property = PROPERTIES[item.PropertyId];
            setControlValueNavigable(property, item);
            if (REQ_PROPS[item.PropertyId] !== null && REQ_PROPS[item.PropertyId] != undefined) REQ_PROPS[item.PropertyId] = true
        }
    } else {
        assetNameHtml.push("<label style='float:left'>" + assetClient.Name + "</label>");
        var pvCount = assetClient.PropertyValues.length;
        for (var i = 0; i < pvCount; i++) {
            var item = assetClient.PropertyValues[i],
                property = PROPERTIES[item.PropertyId];
            setControlValue(property, item);
            if (REQ_PROPS[item.PropertyId] !== null && REQ_PROPS[item.PropertyId] != undefined) REQ_PROPS[item.PropertyId] = true
        }
        for (propertyId in PICKLIST_FILTERS) {
            var trigger = PICKLIST_FILTERS[propertyId];
            $.each(assetClient.PropertyValues, function(index, item) {
                if (item.PropertyId == trigger.ParentPropertyId) {
                    trigger.ParentPropertyValue = item.Value;
                    return false
                }
            })
        }
    }
    $("#tdAssetName").html(assetNameHtml.join(""));
    toggleButtons("submitButtons", false);
    toggleButtons("resetButtons", false)
}

function removeBookmark(assetId) {
    if (bookmark_Remove(SELECTED_VIEW.Id, SELECTED_ITEM_ID)) {
        appCache(LOCAL_STORAGE_BOOKMARKS, bookMarks_Get(USER_CURRENT.Id), true);
        var $bookmarkIcon = $("#imgBookmark");
        $bookmarkIcon.attr("src", "images/fava.png");
        $bookmarkIcon.attr("alt", "Add to favorites");
        $bookmarkIcon.attr("onClick", "addBookmark()")
    }
}

function removeDocumentOrImage(propertyValueId) {
    $("#" + propertyValueId).remove();
    DELETED_VALUES[propertyValueId] = true;
    toggleButtons("submitButtons", true);
    toggleButtons("resetButtons", true)
}

function removeMultiValueUrl(controlId) {
    var srcControlId = controlId + "_tgt",
        valueToRemove = $("#" + srcControlId).val();
    if (valueToRemove == null || valueToRemove == "") return;
    var textToRemove = $("#" + srcControlId + " :selected").text(),
        propertyValueId = null;
    for (pvId in PROPVALS) {
        var item = PROPVALS[pvId];
        if (item.Id == valueToRemove) {
            propertyValueId = pvId;
            break
        }
    }
    if (propertyValueId !== null) {
        if (DELETED_VALUES[propertyValueId] == null) {
            DELETED_VALUES[propertyValueId] = true;
            toggleButtons("submitButtons", true);
            toggleButtons("resetButtons", true)
        }
    } else {
        var removalIndex = -1;
        $.each(NEW_VALUES[controlId], function(index, value) {
            if (NEW_VALUES[controlId][index] == valueToRemove) {
                removalIndex = index;
                return false
            }
        });
        if (removalIndex == -1) return;
        if (removalIndex != -1) NEW_VALUES[controlId].splice(removalIndex, 1);
        if (NEW_VALUES[controlId].length == 0) {
            removalIndex = -1;
            $.each(NEW_VALUES, function(index, value) {
                if (NEW_VALUES[index] == controlId) {
                    removalIndex = index;
                    return false
                }
            });
            if (removalIndex != -1) NEW_VALUES.splice(removalIndex, 1)
        }
    }
    htmlSelect_RemoveOptionByValue(srcControlId, valueToRemove)
}

function removeMultiValueItem(controlId) {
    var srcControlId = controlId + "_tgt",
        valueToRemove = $("#" + srcControlId).val();
    if (valueToRemove == null || valueToRemove == "") return;
    var textToRemove = $("#" + srcControlId + " :selected").text(),
        propertyValueId = null;
    for (pvId in PROPVALS) {
        var item = PROPVALS[pvId];
        if (item.Value == valueToRemove) {
            propertyValueId = pvId;
            break
        }
    }
    if (propertyValueId !== null) {
        if (DELETED_VALUES[propertyValueId] == null || DELETED_VALUES[propertyValueId] == undefined) {
            DELETED_VALUES[propertyValueId] = true;
            toggleButtons("submitButtons", true);
            toggleButtons("resetButtons", true)
        }
    } else {
        var removalIndex = -1;
        $.each(NEW_VALUES[controlId], function(index, value) {
            if (NEW_VALUES[controlId][index] == valueToRemove) {
                removalIndex = index;
                return false
            }
        });
        if (removalIndex == -1) return;
        if (removalIndex != -1) NEW_VALUES[controlId].splice(removalIndex, 1);
        if (NEW_VALUES[controlId].length == 0) {
            removalIndex = -1;
            $.each(NEW_VALUES, function(index, value) {
                if (NEW_VALUES[index] == controlId) {
                    removalIndex = index;
                    return false
                }
            });
            if (removalIndex != -1) NEW_VALUES.splice(removalIndex, 1)
        }
    }
    htmlSelect_RemoveOptionByValue(srcControlId, valueToRemove)
}

function renderView(viewId) {
    try {
        PROPERTIES = [];
        PROPS_TABS = [];
        REQ_PROPS = [];
        filtersClear();
        var $tab_title_input = $("#tab_title"),
            $tab_content_input = $("#tab_content"),
            tab_counter = 0,
            tab_content;
        $tabs.tabs({
            tabTemplate: '<li><a href="#{href}">#{label}</a></li>',
            add: function(event, ui) {
                $(ui.panel).append(tab_content)
            }
        });
        $tabs.show();
        if ($tabs.tabs !== null) {
            var tabCount = $tabs.tabs("length");
            for (var i = 0; i < tabCount; i++) $tabs.tabs("remove", 0)
        }
        if (SELECTED_VIEW == null || SELECTED_VIEW.Id != viewId) SELECTED_ITEM_ID = null;
        if (viewId != "-1") {
            if (SELECTED_VIEW === null || SELECTED_VIEW == undefined || SELECTED_VIEW.Id != viewId) SELECTED_VIEW = view_GetClient(viewId, USER_CURRENT.Id)
        } else SELECTED_VIEW = null;
        var view = SELECTED_VIEW;
        if (view !== null) {
            document.title = APP_ACRONYM + " - " + view.Name;
            var driverCaption = null;
            if (view.DriverCaption !== null && view.DriverCaption != "") driverCaption = view.DriverCaption;
            else driverCaption = "Assets";
            $("#driverCaption").html(driverCaption + ": ");
            $("#divAssets").show();
            $driver.show();
            $toolbar.show();
            if (SELECTED_ITEM_ID === null) {
                var toolbarHtml = toolbarCreateFilter(view);
                $toolbar.html(toolbarHtml)
            }
        } else {
            document.title = DEFAULT_PAGE_TITLE;
            selectOptionsRemove("lstDriver");
            selectOptionAppend("lstDriver", "-1", "[Select]", true);
            $("#divAssets").hide();
            $driver.hide();
            $toolbar.hide();
            $tabs.hide()
        }
        if (view !== null) {
            IS_INSTANCE_VIEW = view.IsInstance;
            var pgCount = view.PropertyGroups.length;
            for (var i = 0; i < pgCount; i++) {
                var pg = view.PropertyGroups[i];
                tab_content = createTabContent(pg, tab_counter);
                $tabs.tabs("add", "#tabs-" + tab_counter, pg.Name);
                tab_counter++
            }
            if ($tabs.tabs !== null) {
                var tabLength = $tabs.tabs("length");
                for (var i = 1; i < tabLength; i++) $tabs.tabs("disable", i)
            }
        }
        $tabs.addClass("ui-widget-overlay");
        activateHighlighting();
        return true
    } finally {}
}

function resetField(propertyId) {
    var propertyValue = null;
    $.each(PROPERTY_VALUES, function(index, item) {
        if (item.PropertyId == propertyId) {
            propertyValue = item;
            return false
        }
    });
    var $tgtCtl = $("#" + propertyId);
    if (propertyValue !== null) $tgtCtl.val(propertyValue.DisplayValue);
    else $tgtCtl.val("");
    $tgtCtl.removeClass("ui-state-error")
}

function resetFields() {
    NEW_VALUES = [];
    ERR_PROPS = [];
    PROPERTY_VALUES = [];
    DELETED_VALUES = [];
    clearForm($tabs, true, true);
    populateForm(SELECTED_ASSET);
    toggleButtons("resetButtons", false);
    toggleButtons("submitButtons", false);
    $("#messageDialog").dialog("close")
}

function reSortHist(boolOrder) {
    $("#divHistory").html("<img src='images/ajax-loader.gif' alt='spinner' />");
    $(document).ready(function() {
        $("#divHistory").html("<table cellpadding='0' cellspacing='0' width='400px'>" + getHistory(SELECTED_ITEM_ID, SELECTED_VIEW.Id, boolOrder) + "</table>")
    })
}

function selectedAssetTypeChanged(control) {
    var renderNewSelect = true;
    if (control.value == null || control.value == "") return;
    var selectedAssetTypeId = control.value,
        hasChildren = false,
        incomingNumber = control.id.replace("AT_", ""),
        newIndex = parseInt(incomingNumber) + 1,
        createNew = $("#AT_" + newIndex) !== null;
    if (createNew) {
        var tempContent = '<select style="width:100%" id="AT_' + newIndex + '" class="ui-widget ui-state-default" onChange="selectedAssetTypeChanged(this)"><option value="-1">[Select]</option>';
        $.ajax({
            type: "GET",
            url: SVC_ASSETTYPE_GETCHILDREN + "?parentAssetTypeId=" + selectedAssetTypeId,
            dataType: "json",
            contentType: "json",
            async: false,
            error: function(XMLHttpRequest, textStatus, errorThrown) {
                alert(textStatus + " " + errorThrown)
            },
            success: function(data) {
                if (data !== null && data.length > 0) $.each(data, function(index, item) {
                    tempContent += '<option value="' + item.Key + '">' + item.Value + "</option>"
                });
                else if (data !== null && data.length == 0) $.ajax({
                    type: "GET",
                    url: SVC_ASSETS_GETDICTIONARY + "?assetTypeId=" + selectedAssetTypeId,
                    dataType: "json",
                    contentType: "json",
                    async: false,
                    error: function(XMLHttpRequest, textStatus, errorThrown) {
                        alert(textStatus + " " + errorThrown)
                    },
                    success: function(data) {
                        if (data !== null && data.length > 0) $.each(data, function(index, item) {
                            tempContent += '<option value="' + item.Key + '">' + item.Value + "</option>"
                        });
                        else if (data !== null && data.length == 0) {
                            AT_FINAL = "AT_" + incomingNumber;
                            renderNewSelect = false;
                            var button = getDialogButton(".newItemInstance", "Submit");
                            if (button) {
                                button.attr("disabled", false);
                                button.removeClass("ui-state-disabled");
                                button.addClass("ui-state-default")
                            }
                        }
                        tempContent += "</select>"
                    }
                });
                tempContent += "</select>"
            }
        });
        if (renderNewSelect) $(tempContent).insertAfter($("#" + control.id))
    } else {}
}

function sendValueAssetName(propertyIdAndValue) {
    var tempProperty = PROPERTIES[propertyIdAndValue.id],
        tempValue = propertyIdAndValue.value;
    if (asset_Exists(tempValue, SELECTED_VIEW.AssetTypeId))
        if (tempValue.toLowerCase() == SELECTED_ASSET.Name.toLowerCase()) {
            reFetchAssets = true;
            sendValue(propertyIdAndValue)
        } else {
            showMessage("Error", "Asset name '" + tempValue + "' already in use.", true, 400);
            $("#" + propertyIdAndValue.id).val(SELECTED_ASSET.Name)
        }
    else {
        reFetchAssets = true;
        sendValue(propertyIdAndValue)
    }
}

function sendValueTagger(propertyIdAndValue) {
    var propId = propertyIdAndValue.id;
    $("#" + propId).textntags("val", function(text) {})
}

function sendValueUrl(propertyId) {
    var property = PROPERTIES[propertyId.toLowerCase()],
        urlName = $("#" + propertyId + "_name").val(),
        url = $("#" + propertyId + "_url").val(),
        newValue = null;
    if (urlName != "" && url != "") newValue = urlName + "|" + url;
    else newValue = url;
    var item = {
        id: propertyId,
        value: newValue
    };
    if (property.AllowMultiValue) {
        htmlSelect_InsertOption(propertyId + "_tgt", newValue, newValue, false);
        $("#" + propertyId + "_name").val("");
        $("#" + propertyId + "_url").val("")
    }
    sendValue(item)
}

function sendValue(propertyIdAndValue) {
    try {
        var tempProperty = PROPERTIES[propertyIdAndValue.id],
            tempValue = propertyIdAndValue.value,
            addErrorClass = false;
        if (propertyIdAndValue.value == "0")
            if (tempProperty !== null)
                if (tempProperty.DataType == DATA_TYPE_PICKLIST) showNewPickListValueDialog(tempProperty);
                else if (tempProperty.DataType == DATA_TYPE_USER) showNewUserDialog(tempProperty);
        else if (tempProperty.DataType == DATA_TYPE_ASSET) showNewAssetDialog(tempProperty);
        if (tempProperty.AllowMultiValue)
            if (NEW_VALUES[propertyIdAndValue.id] == null) {
                var newValue = [tempValue];
                NEW_VALUES[propertyIdAndValue.id] = newValue
            } else {
                var length = NEW_VALUES[propertyIdAndValue.id].length;
                NEW_VALUES[propertyIdAndValue.id][length] = tempValue
            }
        else if (tempProperty.DataType == DATA_TYPE_CURRENCY) {
            var currencyControlId = tempProperty.Id.toLowerCase(),
                currSymbolId = $("#" + currencyControlId + "_curr-id").val(),
                amount = $("#" + currencyControlId).val();
            if (currSymbolId != null && currSymbolId != "" && amount != null && amount != "") tempValue = currSymbolId + DELIMITER_MULTI_VALUE + amount;
            else tempValue = null;
            NEW_VALUES[propertyIdAndValue.id] = tempValue
        } else if (PROPERTY_VALUES[propertyIdAndValue.id] !== null && PROPERTY_VALUES[propertyIdAndValue.id] !== undefined && PROPERTY_VALUES[propertyIdAndValue.id].Value == tempValue) return;
        else NEW_VALUES[propertyIdAndValue.id] = tempValue;
        if (tempProperty.Triggers !== undefined && tempProperty.Triggers !== null && tempProperty.Triggers.length > 0) {
            var triggerCount = tempProperty.Triggers.length;
            for (var i = 0; i < triggerCount; i++) {
                var trigger = tempProperty.Triggers[i];
                switch (trigger.TriggerType) {
                    case 0:
                        var newData = dictionaryValues_GetForPropertyRestricted(propertyIdAndValue.id, trigger.TargetPropertyId, tempValue, trigger.SourcePropertyId),
                            $trigCtl = $("#" + trigger.TargetPropertyId);
                        if ($trigCtl.is(":ui-autocomplete")) $trigCtl.autocomplete("destroy");
                        var triggerInfo = {};
                        triggerInfo.ParentPropertyId = propertyIdAndValue.id;
                        triggerInfo.ChildPropertyId = trigger.TargetPropertyId;
                        triggerInfo.ParentPropertyValue = tempValue;
                        triggerInfo.SourcePropertyId = trigger.SourcePropertyId;
                        PICKLIST_FILTERS[trigger.TargetPropertyId] = triggerInfo;
                        var loc = 0,
                            found = false;
                        $.each(PROPERTY_VALUES, function(index, value) {
                            if (value.PropertyId == triggerInfo.ChildPropertyId) {
                                found = true;
                                loc = index;
                                return false
                            }
                        });
                        if (found) {
                            DELETED_VALUES[PROPERTY_VALUES[loc].Id] = true;
                            REQ_PROPS[triggerInfo.ChildPropertyId] = null
                        }
                        break;
                    case 1:
                        showMessage("Error", "Trigger not implemented", true, 400, true);
                        break;
                    default:
                        alert(trigger.Type + " not implemented");
                        break
                }
            }
        }
        var controlId = "#" + propertyIdAndValue.id.toLowerCase();
        if (tempProperty !== null && tempProperty.IsRequired)
            if (tempProperty.DataType == DATA_TYPE_PICKLIST || tempProperty.DataType == DATA_TYPE_ASSET || tempProperty.DataType == DATA_TYPE_ASSET) {
                if (tempValue == "0" || tempValue == "-1" || tempValue == "") addErrorClass = true
            } else if (tempProperty.IsRequired) addErrorClass = tempValue == null || tempValue == "";
        if (REQ_PROPS[tempProperty.Id] !== null && REQ_PROPS[tempProperty.Id] != undefined)
            if (tempValue !== null && tempValue != "") REQ_PROPS[tempProperty.Id] = true;
            else REQ_PROPS[tempProperty.Id] = false;
        if (addErrorClass) {
            $(controlId).addClass("ui-state-error");
            ERR_PROPS[tempProperty.Id] = tempProperty
        } else {
            $(controlId).removeClass("ui-state-error");
            delete ERR_PROPS[tempProperty.Id]
        }
        toggleButtons("submitButtons", associativeArrayLength(ERR_PROPS) <= 0);
        toggleButtons("resetButtons", associativeArrayLength(NEW_VALUES) > 0 || associativeArrayLength(DELETED_VALUES) > 0)
    } finally {}
}

function setControlValue(property, propertyValue) {
    try {
        var valueToShow = propertyValue.DisplayValue;
        if (valueToShow === null || valueToShow == undefined || valueToShow == "") valueToShow = propertyValue.Value;
        var controlId = "#" + propertyValue.PropertyId.toLowerCase(),
            srcControlId = propertyValue.PropertyId.toLowerCase(),
            tgtControlId = null;
        if (property.IsSystem) switch (property.SystemType) {
            case SYSTEM_TYPE_ASSET_NAME:
                if (property.IsReadOnly) $(controlId).text(valueToShow);
                else $(controlId).val(valueToShow);
                break;
            case SYSTEM_TYPE_ASSET_TYPE:
                if (property.IsReadOnly) $(controlId).text(valueToShow);
                else {
                    $(controlId).val(propertyValue.DisplayValue);
                    $(controlId + "-id").val(propertyValue.Value)
                }
                break;
            case SYSTEM_TYPE_INSTANCE_OF:
                if (property.IsReadOnly) $(controlId).text(valueToShow);
                else {
                    $(controlId).val(propertyValue.DisplayValue);
                    $(controlId + "-id").val(propertyValue.Value)
                }
                break;
            case SYSTEM_TYPE_CREATED:
            case SYSTEM_TYPE_CREATED_BY:
            case SYSTEM_TYPE_DELETED:
            case SYSTEM_TYPE_DELETED_BY:
            case SYSTEM_TYPE_DESCRIPTION:
            case SYSTEM_TYPE_INSTANCE_OF_DESCRIPTION:
            case SYSTEM_TYPE_LAST_MODIFIED:
            case SYSTEM_TYPE_LAST_MODIFIED_BY:
                if (property.IsReadOnly) $(controlId).text(valueToShow);
                else $(controlId).val(valueToShow);
                break
        } else switch (property.DataType) {
            case DATA_TYPE_DATE:
            case DATA_TYPE_DATETIME:
                if (propertyValue.Value !== null && propertyValue.Value != undefined && propertyValue.Value != "") $(controlId).datepicker("setDate", propertyValue.Value);
                break;
            case DATA_TYPE_DOCUMENT:
                tgtControlId = "#div" + srcControlId;
                var srcUrl = SVC_IMAGE_GET_THUMBNAIL + "?imageId=" + propertyValue.Value + "&createThumbnail=false&maxSize=0",
                    html = [];
                html.push('<span id="' + propertyValue.Id + '" style="float:left;width:100%;">');
                html.push('<span style="float:left;" class="ui-button-icon-primary ui-icon ui-icon-trash" ');
                html.push("onClick=\"removeDocumentOrImage('" + propertyValue.Id + "')\"></span>");
                html.push('<a href="' + srcUrl + '" target="_new" style="float:left;">' + propertyValue.DisplayValue + "</a></span>");
                var $ctlDoc = $(tgtControlId),
                    currentHtml = $ctlDoc.html();
                if (currentHtml === null || currentHtml === "") $ctlDoc.html(html.join(""));
                else $ctlDoc.html(currentHtml + "<br/>" + html.join(""));
                break;
            case DATA_TYPE_HTML:
                $(controlId).val(propertyValue.Value);
                break;
            case DATA_TYPE_IMAGE:
                tgtControlId = "#div" + srcControlId;
                var srcUrl = SVC_IMAGE_GET_THUMBNAIL + "?imageId=" + propertyValue.Value + "&createThumbnail=true&maxSize=" + THUMB_SIZE,
                    html = [];
                html.push('<div id="' + propertyValue.Id + '">');
                html.push('<img src="' + srcUrl + '" onclick="showFullImage(\'' + propertyValue.Value + "', '" + propertyValue.DisplayValue + "')\"/>");
                html.push('</a><span style="float:left" class="ui-button-icon-primary ui-icon ui-icon-trash" ');
                html.push("onClick=\"removeDocumentOrImage('" + propertyValue.Id + "')\"></span></div>");
                var $ctlImg = $(tgtControlId),
                    currentHtml = $ctlImg.html();
                if (currentHtml === null || currentHtml === "") $ctlImg.html(html.join(""));
                else $ctlImg.html(currentHtml + "<br/>" + html.join(""));
                break;
            case DATA_TYPE_MEMO:
                if (property.IsReadOnly) $(controlId).text(valueToShow);
                else $(controlId).text(valueToShow);
                break;
            case DATA_TYPE_FLOAT:
            case DATA_TYPE_INT:
            case DATA_TYPE_IPv4:
            case DATA_TYPE_IPv6:
            case DATA_TYPE_STRING:
                if (property.IsReadOnly) $(controlId).text(valueToShow);
                else $(controlId).val(valueToShow);
                break;
            case DATA_TYPE_URL:
                var temp = propertyValue.DisplayValue.split("|");
                if (property.AllowMultiValue) htmlSelect_InsertOption(propertyValue.PropertyId + "_tgt", propertyValue.Id, temp[0], false);
                else {
                    $(controlId + "_name").val(temp[0]);
                    $(controlId + "_url").val(temp[1])
                }
                break;
            case DATA_TYPE_USER:
            case DATA_TYPE_ASSET:
            case DATA_TYPE_DEPENDENCY:
            case DATA_TYPE_RELATION_CHILDPARENT:
            case DATA_TYPE_RELATION_OTHER:
            case DATA_TYPE_RELATION_PARENTCHILD:
                if (property.IsReadOnly) $(controlId).text(valueToShow);
                else if (property.AllowMultiValue) {
                    tgtControlId = srcControlId + "_tgt";
                    var tag = $("#" + tgtControlId).get(0);
                    if (tag !== null && tag != undefined) {
                        var tagName = tag.tagName;
                        if (tagName !== null) tagName = tagName.toLowerCase();
                        switch (tagName) {
                            case "select":
                                if (propertyValue.Value.indexOf(",") != -1) $.each(propertyValue.Value.split(","), function(index, value) {
                                    selectOptionAppend(tgtControlId, value, value, false);
                                    htmlSelect_RemoveOptionByValue(srcControlId, value)
                                });
                                else {
                                    selectOptionAppend(tgtControlId, propertyValue.Value, propertyValue.DisplayValue, false);
                                    htmlSelect_RemoveOptionByValue(srcControlId, propertyValue.Value)
                                }
                                break;
                            default:
                        }
                    }
                } else {
                    $(controlId).val(propertyValue.DisplayValue);
                    $(controlId + "-id").val(propertyValue.Value)
                }
                break;
            case DATA_TYPE_CURRENCY:
                if (propertyValue.Value != null && propertyValue.Value != "") {
                    var cv = currencyValue_Get(propertyValue.Value);
                    if (cv != null) {
                        $(controlId + "_curr").val(htmlDecode(cv.SymbolAscii));
                        $(controlId + "_curr-id").val(cv.SymbolId);
                        $(controlId).val(formatCurrency(cv.Amount))
                    } else {
                        $(controlId + "_curr").val("");
                        $(controlId + "_curr-id").val("");
                        $(controlId).val("")
                    }
                } else {
                    $(controlId + "_curr").val("");
                    $(controlId + "_curr-id").val("");
                    $(controlId).val("")
                }
                break;
            case DATA_TYPE_SYSTEM_INSTANCEOF:
            case DATA_TYPE_SYSTEM_INSTANCEOF_DESC:
            case DATA_TYPE_PICKLIST:
                if (property.IsReadOnly) $(controlId).text(valueToShow);
                else {
                    $(controlId).val(propertyValue.DisplayValue);
                    $(controlId + "-id").val(propertyValue.Value)
                }
                break
        }
        var addErrorClass = false;
        if (property !== null && property.IsRequired)
            if (property.DataType == DATA_TYPE_PICKLIST) {
                if (propertyValue.Value == "0" || propertyValue.Value == "-1" || propertyValue.Value == "") addErrorClass = true
            } else if (propertyValue.Value == null || propertyValue.Value == "") addErrorClass = true;
        if (addErrorClass) {
            toggleButtons("submitButtons", false);
            $(controlId).addClass("ui-state-error");
            ERR_PROPS[property.Id] = property
        } else {
            toggleButtons("submitButtons", true);
            $(controlId).removeClass("ui-state-error");
            delete ERR_PROPS[property.Id]
        }
    } finally {}
}

function setControlValueNavigable(property, propertyValue) {
    try {
        var tgtControlId = "td" + propertyValue.PropertyId,
            $tgtCtl = $(document.getElementById(tgtControlId));
        switch (property.DataType) {
            case DATA_TYPE_ASSET:
            case DATA_TYPE_DEPENDENCY:
            case DATA_TYPE_RELATION_PARENTCHILD:
            case DATA_TYPE_RELATION_CHILDPARENT:
            case DATA_TYPE_RELATION_OTHER:
            case DATA_TYPE_USER:
                var assetTypeId = null;
                if (property.IsSystem && property.SystemType == SYSTEM_TYPE_INSTANCE_OF) assetTypeId = SELECTED_ASSET.AssetTypeId;
                else assetTypeId = property.AssetTypeId;
                if (propertyValue.DisplayValue === null) return;
                if (ASSET_TYPES_VIEWS[assetTypeId] != null) {
                    var html = '<div id="' + propertyValue.PropertyId + '" style="display:block; float:left;">';
                    if (propertyValue.Value !== null) {
                        html += createViewUrl(property.Id, ASSET_TYPES_VIEWS[assetTypeId], propertyValue.Value, propertyValue.DisplayValue);
                        html += insertDetailsIcon(propertyValue.Value, property.AssetTypeId, propertyValue.DisplayValue, null, null)
                    }
                    html += "</div>";
                    if (property.AllowMultiValue) $tgtCtl.append("<br/>" + html);
                    else $tgtCtl.html(html)
                } else if (property.AllowMultiValue) $tgtCtl.append("<br/>" + propertyValue.DisplayValue);
                else {
                    var html = '<div id="' + propertyValue.PropertyId + '" style="display:block; float:left;" >' + propertyValue.DisplayValue + "</div>";
                    html += insertDetailsIcon(propertyValue.Value, property.AssetTypeId, propertyValue.DisplayValue, null, null);
                    $tgtCtl.html(html)
                }
                break;
            case DATA_TYPE_CURRENCY:
                if (propertyValue.DisplayValueHtml != null) {
                    var temp = propertyValue.DisplayValueHtml.split(DELIMITER_MULTI_VALUE),
                        symbolAscii = temp[0],
                        currvalue = temp[1],
                        html = htmlDecode(symbolAscii) + currvalue;
                    $tgtCtl.html(html)
                } else $tgtCtl.html("");
                break;
            case DATA_TYPE_DOCUMENT:
                var srcUrl = SVC_IMAGE_GET_THUMBNAIL + "?imageId=" + propertyValue.Value + "&createThumbnail=false&maxSize=0",
                    html = '<a href="' + srcUrl + '" target="_new">' + propertyValue.DisplayValue + "</a>",
                    currentHtml = $tgtCtl.html();
                if (currentHtml === null || currentHtml === "") $tgtCtl.html(html);
                else $tgtCtl.html(currentHtml + "<br/>" + html);
                break;
            case DATA_TYPE_IMAGE:
                var srcUrl = SVC_IMAGE_GET_THUMBNAIL + "?imageId=" + propertyValue.Value + "&createThumbnail=true&maxSize=" + THUMB_SIZE,
                    html = '<img src="' + srcUrl + '" onclick="showFullImage(\'' + propertyValue.Value + "', '" + propertyValue.DisplayValue + "')\"/>",
                    currentHtml = $tgtCtl.html();
                if (currentHtml === null || currentHtml === "") $tgtCtl.html(html);
                else $tgtCtl.html(currentHtml + html);
                break;
            case DATA_TYPE_PICKLIST:
                var displayValue = null;
                if (propertyValue.DisplayValue == null || propertyValue.DisplayValue == "") displayValue = propertyValue.Value;
                else displayValue = propertyValue.DisplayValue;
                var html = [];
                if (displayValue !== null && displayValue != undefined && displayValue != "") {
                    html.push('<div id="' + propertyValue.PropertyId + '" style="display:block; float:left;" >' + displayValue + "</div>");
                    html.push(insertDetailsIcon(null, null, null, propertyValue.Value, displayValue))
                }
                if (property.AllowMultiValue) $tgtCtl.append("<br/>" + html.join(""));
                else $tgtCtl.html(html.join(""));
                break;
            case DATA_TYPE_URL:
                var temp = propertyValue.DisplayValue.split("|"),
                    urlName = temp[0],
                    urlLink = temp[1],
                    html = null;
                if (urlName !== null && urlName != undefined && urlName != "")
                    if (property.AllowMultiValue) html = '<a href="' + urlLink + '" target="_new">' + urlName + "</a><br/>";
                    else html = '<a href="' + urlLink + '" target="_new">' + urlName + "</a>";
                else if (property.AllowMultiValue) html = '<a href="' + urlLink + '" target="_new">' + urlLink + "</a><br/>";
                else html = '<a href="' + urlLink + '" target="_new">' + urlLink + "</a>";
                if (property.AllowMultiValue) $tgtCtl.append(html);
                else $tgtCtl.html(html);
                break;
            case DATA_TYPE_STRING:
                if (property.IsSystem) switch (property.SystemType) {
                    case SYSTEM_TYPE_ASSET_NAME:
                        var html = [],
                            assetName = $.trim(propertyValue.Value.replace(/'/gi, "\\'"));
                        assetName = assetName.replace(/\"/gi, '\\"');
                        html.push("<label style='float:left'>" + propertyValue.Value + "</label>");
                        html.push(insertDetailsIcon(SELECTED_ASSET.Id, SELECTED_ASSET.AssetTypeId, assetName, SELECTED_ASSET.Id, assetName));
                        $tgtCtl.html(html.join(""));
                        break;
                    case SYSTEM_TYPE_ASSET_TYPE:
                        $tgtCtl.html(propertyValue.DisplayValue);
                        break;
                    case SYSTEM_TYPE_DESCRIPTION:
                        $tgtCtl.html(propertyValue.Value);
                        break;
                    default:
                        var div = '<div id="' + propertyValue.PropertyId + '" class="draggable">' + propertyValue.Value + "</div>";
                        $tgtCtl.html(div);
                        break
                } else {
                    var div = '<div id="' + propertyValue.PropertyId + '" class="draggable">' + propertyValue.Value + "</div>";
                    $tgtCtl.html(div)
                }
                break;
            default:
                var div = '<div id="' + propertyValue.PropertyId + '" class="draggable">' + propertyValue.Value + "</div>";
                $tgtCtl.html(div);
                break
        }
    } finally {}
}

function setSelectedItem(displayValue, controlId) {
    var controlName = "#" + controlId,
        found = false,
        index = 0;
    displayValue = displayValue.toLowerCase();
    $(controlName + " > option").each(function() {
        var tempVal = $(this).val(),
            tempText = $(this).text().toLowerCase();
        if (displayValue == tempText) {
            found = true;
            return false
        }
        index++
    });
    if (found) {
        $(controlName + " option:eq(" + index + ")").attr("selected", "selected");
        return index
    } else return -1
}

function showFilterDialog() {
    if (_qFilterActive) {
        $("#effect").animate({
            top: "0px"
        }, "fast").fadeOut("fast");
        _qFilterActive = false;
        return
    }
    $("#qfApply").hide();
    $("#qfReport").hide();
    var html = [];
    $.each(SELECTED_VIEW.filters, function(index, item) {
        html.push('<option value="' + item.Key + '">' + item.Value + "</option>")
    });
    $("#lstQFilters").html(html.join("")).bind("click", function() {
        onQuickFilterSelected($("#lstQFilters :selected").val());
        return false
    });
    $("#effect").fadeIn("fast").animate({
        top: "+=180px"
    }, "fast");
    toggleControl("lstQFilters", true, true);
    _qFilterActive = true
}

function onQuickFilterSelected(id) {
    $("#qfApply").show();
    $("#qfDelete").show();
    if (userHasReportId(id)) $("#qfReport").hide();
    else $("#qfReport").show()
}

function deleteQuickFilter() {
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    var sourceText = $("#lstQFilters :selected").text();
    $dialog.html("Delete the '" + sourceText + "' QuickFilter?");
    $dialog.dialog({
        width: 600,
        height: "auto",
        position: _dialogPos,
        modal: true,
        autoOpen: false,
        resizable: false,
        draggable: false,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        title: "Confirm Delete",
        buttons: {
            No: function() {
                $(this).dialog("close")
            },
            Yes: function() {
                var filterId = $("#lstQFilters :selected").val();
                if (viewFilter_Delete(filterId)) {
                    var html = [];
                    $.each(viewFilters_Get(USER_CURRENT.Id, SELECTED_VIEW.Id), function(index, item) {
                        html.push('<option value="' + item.key + '">' + item.value + "</option>")
                    });
                    $("#lstQFilters").html(html.join(""))
                } else alert("Failed to delete");
                $(this).dialog("close")
            }
        }
    });
    $dialog.dialog("open")
}

function createReport() {
    var filterId = $("#lstQFilters :selected").val();
    if (report_CreateFromMemberFilter(filterId)) {
        expireUser();
        var url = URL_CLIENT_UI + "/reporting/" + PAGE_NAME_REPORTS + "?reportId=" + filterId;
        window.open(url, "_blank")
    } else alert("failed")
}

function userHasReportId(reportId) {
    var val = false;
    $.each(USER_CURRENT.reports, function(index, item) {
        if (item.Key == reportId) {
            val = true;
            return false
        }
    });
    return val
}

function applyQuickFilter() {
    var filterId = $("#lstQFilters").val(),
        filteredAssets = assets_GetByViewFilter(filterId);
    displayFilters(filterId);
    showFilterDialog();
    if (filteredAssets !== null) {
        reFetchAssets = true;
        activateAssetSelector(filteredAssets);
        reFetchAssets = false;
        $driver.val("");
        clearForm($tabs, false, false);
        _prefetch = true;
        driverChanged(null)
    }
}

function showFilterDialogContent(assetTypeId, isInstance, assetNameLabel) {
    var $dialog = $("#messageDialog");
    $dialog.html(createFilterDialog(assetTypeId, isInstance, assetNameLabel))
}

function showMissingDataDialog() {
    var html = [];
    html.push('<div style="width: 100%; height: 100%; overflow: auto;"><table style="width:100%;text-align:left;"><tr><th style="width:33%;">Tab Name</th><th style="width:33%;">Attribute Name</th><th style="width:34%;">Message</th></tr>');
    if (REQ_PROPS !== null)
        for (propertyId in REQ_PROPS)
            if (REQ_PROPS[propertyId] == false) {
                var tabName = PROPS_TABS[propertyId],
                    property = PROPERTIES[propertyId],
                    propertyName = property.DisplayValue;
                if (propertyName == null || propertyName == "") propertyName = property.Name;
                var message = "Required Field";
                html.push("<tr><td>" + tabName + "</td><td>" + propertyName + "</td><td>" + message + "</td></tr>")
            } html.push("</table></div>");
    showMessage("Missing Required Data", html.join(""), true, 800)
}

function showHistory() {
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html('<div style="width:100%;text-align:center;" >Retrieving history.  Please wait.</div><br/><div style="text-align:center"><img src="images/ajax-loader.gif" alt="spinner" style="text-align:left;" /></div>');
    $dialog.dialog({
        autoOpen: false,
        height: "auto",
        position: _dialogPosHi,
        width: 900,
        modal: true,
        buttons: {
            Print: function() {
                var url = URL_CLIENT_UI + "/print.html?viewId=" + SELECTED_VIEW.Id + "&assetId=" + SELECTED_ITEM_ID + "&printType=" + PRINT_HISTORY;
                window.open(url, "XDB_Print_Preview")
            },
            Close: function() {
                $(this).dialog("close")
            }
        },
        open: function(event, ui) {
            setTimeout(function() {
                loadHistory(SELECTED_ITEM_ID, SELECTED_VIEW.Id, true)
            }, 50);
            $(this).siblings(".ui-dialog-buttonpane").find("button:eq(1)").focus()
        },
        title: "History for " + SELECTED_ASSET.Name,
        close: function() {
            $(this).dialog("destroy")
        },
        create: function() {}
    });
    $dialog.dialog("open")
}

function loadHistory(assetId, viewId, sort) {
    var html = [];
    html.push('<form id="frmAssetHist">');
    html.push('<span style="display:block;" >');
    html.push("<label>Sort:</label>");
    html.push('<input name="rdSort" type="radio" onclick="reSortHist(true);" checked /><label>Chronologically</label>');
    html.push('<input name="rdSort" type="radio" onclick="reSortHist(false);"/><label>Alphabetically</label>');
    html.push("</span><br/>");
    html.push('<div id="divHistory" style="overflow-y:none;overflow-x:none;height:100%;width:100%;">');
    html.push("<table>" + getHistory(assetId, viewId, sort) + "</table>");
    html.push("</div>");
    $("#messageDialog").html(html.join(""))
}

function showMessageForDetails(title, isModal, assetId, assetTypeId, propertyValue) {
    var html = [],
        searchClass = "mnuAdminPicklists";
    if (assetTypeId !== null) searchClass = "at-" + assetTypeId.replace(/-/gi, "") + "-24";
    html.push('<span><img width="24" height="24" style="margin-right:3px;vertical-align:middle;" class="' + searchClass + '" src="images/trans.png"/>');
    html.push(title + "</span>");
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html('<div style="width:100%;text-align:center;" >Retrieving data.  Please wait.</div><br/><div style="text-align:center"><img src="images/ajax-loader.gif" alt="spinner" style="text-align:left;" /></div>');
    $dialog.dialog({
        width: 600,
        height: "auto",
        position: _dialogPosHi,
        autoOpen: false,
        open: function(event, ui) {
            setTimeout(function() {
                showMessageForDetailsContent(assetId, propertyValue)
            }, 50);
            $(this).siblings(".ui-dialog-buttonpane").find("button:eq(1)").focus()
        },
        modal: isModal,
        buttons: {
            Print: function() {
                var url = URL_CLIENT_UI + "/print.html?viewId=" + SELECTED_VIEW.Id + "&assetId=" + assetId + "&printType=" + PRINT_RELATED + "&propertyValue=" + propertyValue;
                window.open(url, "XDB_Print_Preview")
            },
            Close: function() {
                $(this).dialog("close")
            }
        },
        title: html.join(""),
        closeOnEscape: true
    });
    $dialog.dialog("open")
}

function showMessageForDetailsContent(assetId, propertyValue) {
    var html = relatedItems_GetHtml(USER_CURRENT.Id, assetId, propertyValue, _imgSrc),
        finalHtml = [];
    if (html !== null) $.each(html, function(index, item) {
        finalHtml.push(item)
    });
    $("#messageDialog").html(finalHtml.join(""));
    $("#accordion").accordion({
        autoHeight: false,
        collapsible: true,
        active: false,
        icons: false
    })
}

function showNewItemDialog() {
    $("#txtNewItemName").val("");
    $("#lstNewItemParent").val("");
    var newItemPrompt = "Submit",
        newItemName = "Item Name:";
    if (SELECTED_VIEW.NewItemCaption !== null && SELECTED_VIEW.NewItemCaption != "") newItemPrompt = '<span class="ui-icon ui-icon-circle-plus"></span> ' + SELECTED_VIEW.NewItemCaption;
    if (SELECTED_VIEW.DriverCaption !== null && SELECTED_VIEW.DriverCaption != "") newItemName = SELECTED_VIEW.DriverCaption + ":";
    else newItemName = "Item Name:";
    $("#tdNewItemName").html(newItemName);
    var $dialog = $("#newItem");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.dialog({
        width: "auto",
        height: "auto",
        position: _dialogPos,
        autoOpen: false,
        modal: true,
        title: newItemPrompt,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            Cancel: function() {
                if (VIEW_FROM_PROCESS) window.location = URL_CLIENT_UI + "/default.html";
                else {
                    $("#lstDriver option:eq(" + 0 + ")").attr("selected", "selected");
                    $(this).dialog("close")
                }
            },
            Submit: function() {
                confirmNewAsset()
            }
        }
    });
    $("#lstNewItemParent").find("option").remove().end().append('<option value="-1">[Select]</option>').val("-1");
    var assetDictionary = assets_GetDictionary(SELECTED_VIEW.AssetTypeId, false);
    if (assetDictionary !== null && assetDictionary.length > 0) {
        var parentAssetType = assetType_GetParent(SELECTED_VIEW.AssetTypeId);
        if (parentAssetType == null) {
            NEW_ITEM_PARENT_REQUIRED = false;
            $("#trNewItemParent").hide();
            var ART = null;
            if (SELECTED_VIEW.IsInstance) ART = ART_INSTANCE;
            else ART = ART_DEFINITION;
            var hasAssets = assetType_HasAssets(SELECTED_VIEW.AssetTypeId, ART, true);
            if (hasAssets) {
                NEW_ITEM_TYPE_REQUIRED = false;
                $("#trNewItemType").hide()
            } else {
                var childAssetTypes = assetType_GetChildren(SELECTED_VIEW.AssetTypeId);
                NEW_ITEM_TYPE_REQUIRED = true;
                $("#trNewItemType").show();
                var $lstNewItemType = $("#lstNewItemType");
                $.each(childAssetTypes, function(index, value) {
                    $lstNewItemType.append($("<option></option>").val(value.Key).html(value.Value))
                })
            }
        } else {
            var parentAssetTypeAssets = assets_GetDictionary(parentAssetType.Id, false);
            if (parentAssetTypeAssets !== null && parentAssetTypeAssets.length > 0) {
                NEW_ITEM_PARENT_REQUIRED = true;
                $("#trNewItemParent").show();
                $("#trNewItemType").hide();
                var $lstNewItemParent = $("#lstNewItemParent");
                $.each(parentAssetTypeAssets, function(index, value) {
                    $lstNewItemParent.append($("<option></option>").val(value.Key).html(value.Value))
                })
            } else {
                NEW_ITEM_PARENT_REQUIRED = false;
                $("#trNewItemParent").hide();
                $("#trNewItemType").hide()
            }
        }
    } else {
        var lowestLevelParents = assetType_GetLowestLevelParents(SELECTED_VIEW.AssetTypeId);
        if (lowestLevelParents == null || lowestLevelParents.length == 0) {
            NEW_ITEM_PARENT_REQUIRED = false;
            $("#trNewItemParent").hide();
            NEW_ITEM_TYPE_REQUIRED = false;
            $("#trNewItemType").hide()
        } else {
            NEW_ITEM_PARENT_REQUIRED = false;
            NEW_ITEM_TYPE_REQUIRED = true;
            $("#trNewItemParent").hide();
            $("#trNewItemType").show();
            $.each(lowestLevelParents, function(index, value) {
                $("#lstNewItemType").append($("<option></option>").val(value.Key).html(value.Value))
            })
        }
    }
    $dialog.dialog("open");
    setTimeout(function() {
        $("#txtNewItemName").focus()
    }, 10)
}

function showNewItemInstanceDialog() {
    var newItemPrompt = "Submit",
        newItemName = "Item";
    if (SELECTED_VIEW.NewItemCaption !== null && SELECTED_VIEW.NewItemCaption != "") newItemPrompt = SELECTED_VIEW.NewItemCaption;
    if (SELECTED_VIEW.DriverCaption !== null && SELECTED_VIEW.DriverCaption != "") newItemName = SELECTED_VIEW.DriverCaption;
    var tempContent = [];
    tempContent.push('<table style="width:100%">');
    tempContent.push('<tr style="width:100%">');
    tempContent.push('<td style="width:25%">' + newItemName + ":</td>");
    tempContent.push('<td style="width:75%">');
    tempContent.push('<input type="text" id="txtNewItemInstanceName" style="width:100%" class="ui-widget ui-state-default" />');
    tempContent.push("</td></tr>");
    tempContent.push('<tr style="width:100%">');
    var defLabel = assetType_GetReportingLabel(SELECTED_VIEW.AssetTypeId, ART_DEFINITION);
    if (defLabel !== null && defLabel != "") tempContent.push('<td style="width:25%">' + defLabel + ":</td>");
    else tempContent.push('<td style="width:25%">' + "Asset" + " Type:</td>");
    tempContent.push('<td style="width:75%">');
    if (NEW_ITEM_ID !== null && NEW_ITEM_ID != "") tempContent.push('<select disabled="disabled" style="width:100%" id="AT_0" class="ui-widget ui-state-default">');
    else tempContent.push('<select style="width:100%" id="AT_0" class="ui-widget ui-state-default" onChange="selectedAssetTypeChanged(this)">');
    tempContent.push('<option value="-1">[Select]</option>');
    var url = SVC_ASSETS_GET + "?assetTypeId=" + SELECTED_VIEW.AssetTypeId + "&assetRequestType=" + ART_DEFINITION + "&includeChildren=true";
    $.ajax({
        type: "GET",
        url: url,
        dataType: "json",
        contentType: "json",
        async: false,
        error: function(XMLHttpRequest, textStatus, errorThrown) {
            alert(textStatus + " " + errorThrown)
        },
        success: function(data) {
            if (data !== null) $.each(data, function(index, item) {
                if (NEW_ITEM_ID !== null && NEW_ITEM_ID == item.Key) tempContent.push('<option selected="selected" value="' + item.Key + '">' + item.Value + "</option>");
                else tempContent.push('<option value="' + item.Key + '">' + item.Value + "</option>")
            })
        }
    });
    tempContent.push("</select></td></tr></table>");
    var $dialog = $("#newItemInstance");
    $dialog.dialog({
        dialogClass: "newItemInstance",
        width: 475,
        height: "auto",
        position: _dialogPos,
        modal: true,
        title: newItemPrompt,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            Cancel: function() {
                if (VIEW_FROM_PROCESS) {
                    SELECTED_VIEW = null;
                    SELECTED_ITEM_ID = null;
                    $(this).dialog("close");
                    VIEW_FROM_PROCESS = false;
                    $tabs.hide();
                    $driver.hide();
                    $("#divAssets").hide();
                    window.location = URL_CLIENT_UI + "/default.html"
                } else $driver.val("");
                $(this).dialog("close")
            },
            Submit: function() {
                var newItemName = $("#txtNewItemInstanceName").val();
                if (newItemName == null || newItemName == "") showMessage("Error", "Missing required data", true);
                else if (asset_Exists(newItemName, SELECTED_VIEW.AssetTypeId)) showMessage("Error", "This value already exists", true);
                else confirmNewAssetInstance()
            }
        }
    });
    $dialog.html(tempContent.join(""));
    if (NEW_ITEM_ID == null) {
        var button = getDialogButton(".newItemInstance", "Submit");
        if (button) {
            button.attr("disabled", true);
            button.addClass("ui-state-disabled")
        }
    } else AT_FINAL = "AT_0";
    setTimeout(function() {
        $("#txtNewItemInstanceName").focus()
    }, 50)
}

function showNewPickListValueDialog(property) {
    var $dialog = $("#newPickListValue");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html("");
    var html = [];
    html.push('<table style="width:100%">');
    html.push("<tr>");
    html.push('<td id="newPLVItemPrompt" style="width:25%">New ' + property.Name + "</td>");
    html.push('<td style="width:75%"><input type="text" id="txtNewPickListValue" style="width:100%" class="ui-widget ui-state-default" /></td>');
    html.push("</tr>");
    html.push("<tr>");
    html.push('<td id="tdNewPLV" colspan=2 style="text-align:center;"></td>');
    html.push("</tr>");
    html.push("</table>");
    $dialog.html(html.join(""));
    jQuery.data(document.body, "propertyId", property.Id);
    $dialog.dialog({
        width: 500,
        height: "auto",
        position: _dialogPos,
        modal: true,
        autoOpen: false,
        open: function() {
            $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
        },
        buttons: {
            Cancel: function() {
                var propertyId = jQuery.data(document.body, "propertyId");
                resetField(propertyId);
                delete NEW_VALUES[propertyId];
                if (associativeArrayLength(NEW_VALUES) == 0) {
                    toggleButtons("submitButtons", false);
                    toggleButtons("resetButtons", false)
                }
                $(this).dialog("close")
            },
            Submit: function() {
                confirmNewPickListValue(property);
                $(this).dialog("close")
            }
        }
    });
    $dialog.dialog("open");
    setTimeout(function() {
        $("#txtNewPickListValue").focus()
    }, 10)
}

function showNewAssetDialog(property) {
    if (assetType_GetParent(property.AssetTypeId) == null) {
        var $dialog = $("#newPickListValue");
        if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
        $dialog.html("");
        var html = [];
        html.push('<table style="width:100%">');
        html.push("<tr>");
        var itemPrompt = assetType_GetName(property.AssetTypeId, false);
        html.push('<td id="newPLVItemPrompt" style="width:25%">New ' + itemPrompt + "</td>");
        html.push('<td style="width:75%"><input type="text" id="txtNewPickListValue" style="width:100%" class="ui-widget ui-state-default" /></td>');
        html.push("</tr>");
        html.push("<tr>");
        html.push('<td id="tdNewPLV" colspan=2 style="text-align:center;"></td>');
        html.push("</tr>");
        html.push("</table>");
        $dialog.html(html.join(""));
        jQuery.data(document.body, "propertyId", property.Id);
        $dialog.dialog({
            width: 450,
            height: 250,
            position: _dialogPos,
            modal: true,
            autoOpen: false,
            title: "Create new " + property.Name,
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            buttons: {
                Cancel: function() {
                    var propertyId = jQuery.data(document.body, "propertyId"),
                        propertyValue = null;
                    $.each(PROPERTY_VALUES, function(index, item) {
                        if (item.PropertyId == propertyId) {
                            propertyValue = item;
                            return false
                        }
                    });
                    if (propertyValue !== null) {
                        $("#" + propertyId).removeClass("ui-state-error");
                        delete ERR_PROPS[propertyId];
                        $("#" + propertyId).val(propertyValue.DisplayValue)
                    } else $("#" + propertyId).val("");
                    delete NEW_VALUES[propertyId];
                    if (associativeArrayLength(NEW_VALUES) == 0) {
                        toggleButtons("submitButtons", false);
                        toggleButtons("resetButtons", false)
                    }
                    $(this).dialog("close")
                },
                Create: function() {
                    confirmNewAssetFromOtherView(property)
                }
            }
        });
        $dialog.dialog("open");
        setTimeout(function() {
            $("#txtNewPickListValue").focus()
        }, 10)
    } else alert("this cannot be done to asset types that are not top-level")
}

function showNewUserDialog(property) {
    try {
        var $dialog = $("#newPickListValue");
        if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
        $dialog.html("");
        var html = [];
        html.push('<table style="width:100%">');
        html.push("<tr>");
        html.push('<td colspan=2 style="text-align:center;"><b>Enter a user name or an email address</b></td>');
        html.push("</tr>");
        html.push("<tr>");
        html.push('<td id="newPLVItemPrompt" style="width:25%">New ' + property.Name + "</td>");
        html.push('<td style="width:75%"><input type="text" id="txtNewPickListValue" style="width:100%" class="ui-widget ui-state-default" /></td>');
        html.push("</tr>");
        html.push("<tr>");
        html.push('<td id="tdNewPLV" colspan=2 style="text-align:center;"></td>');
        html.push("</tr>");
        html.push("</table>");
        $dialog.html(html.join(""));
        jQuery.data(document.body, "propertyId", property.Id);
        $dialog.dialog({
            width: 450,
            height: "auto",
            position: _dialogPos,
            modal: true,
            autoOpen: false,
            title: "Enter User's Email Address",
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            buttons: {
                Cancel: function() {
                    var propertyId = jQuery.data(document.body, "propertyId");
                    resetField(propertyId);
                    delete NEW_VALUES[propertyId];
                    if (associativeArrayLength(NEW_VALUES) == 0) {
                        toggleButtons("submitButtons", false);
                        toggleButtons("resetButtons", false)
                    }
                    $(this).dialog("close")
                },
                Create: function() {
                    $("#Submit:eq(0)").addClass("ui-state-disabled").attr("disabled", true);
                    var newPLV = $("#txtNewPickListValue").val().toLowerCase();
                    if (isDirectoryUser(newPLV)) {
                        confirmNewUser(property);
                        $(this).dialog("close")
                    } else showUserInfoDialog(property)
                }
            }
        });
        $dialog.dialog("open")
    } finally {}
}

function showPrintDialog() {
    var url = URL_CLIENT_UI + "/print.html?viewId=" + SELECTED_VIEW.Id + "&assetId=" + SELECTED_ITEM_ID + "&printType=" + PRINT_CURRENT;
    window.open(url, "XDB_Print_Preview")
}

function showRelations() {
    var url = URL_CLIENT_UI + "/reporting/infovis.html?assetId=" + SELECTED_ITEM_ID;
    window.open(url, "XDB_Asset_Relations")
}

function showUserInfoDialog(property) {
    var emailAddress = $("#txtNewPickListValue").val();
    emailAddress = $.trim(emailAddress);
    var userId = isUserInApp(emailAddress);
    if (userId !== null && userId != undefined && userId != NULL_GUID) {
        $("#newPickListValue").dialog("close");
        var tempContent = "User is already in " + APP_ACRONYM + " and has been set to the selected item.";
        $("#" + property.Id).val(userId);
        showMessage("User Already Exists", tempContent, true, 400)
    } else {
        $("#txtNewPickListValueLastName").val("");
        $("#txtNewPickListValueFirstName").val("");
        var $dialog = $("#newUser");
        $dialog.dialog({
            width: 450,
            height: "auto",
            position: _dialogPos,
            modal: true,
            autoOpen: false,
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            buttons: {
                Cancel: function() {
                    var propertyId = jQuery.data(document.body, "propertyId"),
                        propertyValue = null;
                    $.each(PROPERTY_VALUES, function(index, item) {
                        if (item.PropertyId == propertyId) {
                            propertyValue = item;
                            return false
                        }
                    });
                    if (propertyValue !== null) $("#" + propertyId).val(propertyValue.Value);
                    else $("#" + propertyId).val(-1);
                    delete NEW_VALUES[propertyId];
                    if (associativeArrayLength(NEW_VALUES) == 0) {
                        toggleButtons("submitButtons", false);
                        toggleButtons("resetButtons", false)
                    }
                    $(this).dialog("close")
                },
                Submit: function() {
                    confirmNewUser(property);
                    $(this).dialog("close");
                    $("#newPickListValue").dialog("close")
                }
            }
        });
        $dialog.dialog("open")
    }
}

function showValidationErrors() {
    try {
        var tempContent = '<table style="width:100%">';
        tempContent += '<tr><td style="width:100%"><b>Error</b></td></tr>';
        for (propertyId in ERR_PROPS) {
            var tempProperty = ERR_PROPS[propertyId];
            if (tempProperty !== null) tempContent += '<tr><td style="width:100%">' + tempProperty.Name + " is a required field.</td></tr>"
        }
        tempContent += "</table>";
        var $dialog = $("#messageDialog");
        $dialog.html(tempContent);
        $dialog.dialog({
            width: 700,
            height: 250,
            position: _dialogPos,
            modal: true,
            open: function() {
                $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
            },
            title: "Validation Errors",
            buttons: {
                Ok: function() {
                    $(this).dialog("close")
                }
            }
        })
    } finally {}
}

function submitNewValues() {
    try {
        $(".uploadContent").each(function(index, item) {
            var propertyId = $(item).parent().parent().attr("id"),
                property = PROPERTIES[propertyId],
                uploadObjectId = $(item).parent().attr("id"),
                fileName = $(item).contents().find("input").val(),
                title = fileName,
                description = "[Description Needed]";
            if (prepareUploadObjectPropertyValue(propertyId, uploadObjectId, title, description, fileName)) {
                uploadFiles(this);
                if (property.AllowMultiValue) {
                    var indexToChange = 0;
                    $(NEW_VALUES[propertyId]).each(function(subIndex, subItem) {
                        if (subItem[subIndex] == fileName) {
                            indexToChange = subIndex;
                            return false
                        }
                    });
                    NEW_VALUES[propertyId][indexToChange] = uploadObjectId
                } else NEW_VALUES[propertyId] = uploadObjectId
            }
        });
        var submittal = {
                UserId: USER_ACTUAL.Id,
                ViewId: SELECTED_VIEW.Id,
                AssetId: SELECTED_ITEM_ID
            },
            newValues = [],
            itemCount = 0;
        for (propertyId in NEW_VALUES) itemCount++;
        var currentIndex = 0;
        for (propertyId in NEW_VALUES) {
            var submittedValue;
            if (isArray(NEW_VALUES[propertyId])) {
                var tempLength = NEW_VALUES[propertyId].length;
                $.each(NEW_VALUES[propertyId], function(index, item) {
                    if (index == 0)
                        if (tempLength == 1) submittedValue = item;
                        else submittedValue = item + DELIMITER_MULTI_VALUE;
                    else if (index == tempLength - 1) submittedValue += item;
                    else submittedValue += item + DELIMITER_MULTI_VALUE
                })
            } else submittedValue = $.trim(NEW_VALUES[propertyId]);
            var item = {
                Key: propertyId,
                Value: submittedValue
            };
            newValues.push(item);
            currentIndex++
        }
        var valuesToDelete = [];
        currentIndex = 0;
        for (propertyValueId in DELETED_VALUES) {
            var item = {
                Key: currentIndex,
                Value: propertyValueId
            };
            valuesToDelete.push(item);
            currentIndex++
        }
        submittal.NewValues = newValues;
        submittal.ValuesToDelete = valuesToDelete;
        var saved = propertyValues_Save(submittal),
            tempContent, $dialog = $("#messageDialog");
        if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
        if (saved) {
            $("#confirmSubmit").dialog("close");
            toggleButtons("submitButtons", false);
            toggleButtons("resetButtons", false);
            tempContent = "Values saved successfully.";
            var assetClient = asset_Get(SELECTED_ASSET.Id, USER_CURRENT.Id, SELECTED_VIEW.Id);
            PROPERTY_VALUES = [];
            var pvCount = assetClient.PropertyValues.length;
            for (var i = 0; i < pvCount; i++) {
                var item = assetClient.PropertyValues[i];
                PROPERTY_VALUES[item.PropertyId] = item
            }
            SELECTED_ASSET = assetClient;
            if (reFetchAssets) {
                activateAssetSelectorFocus();
                $("#lstDriver-id").val(SELECTED_ASSET.Id)
            }
        } else {
            $("#confirmSubmit").dialog("close");
            tempContent = "Values failed to save!"
        }
        var createAnother = saved && (VIEW_FROM_PROCESS == true && NEW_ITEM_ID !== null);
        if (!createAnother) {
            $dialog.html(tempContent);
            $dialog.dialog({
                width: 350,
                height: "auto",
                position: _dialogPos,
                modal: true,
                title: "Submittal Result",
                open: function() {
                    $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
                },
                buttons: {
                    Ok: function() {
                        if (saved) {
                            NEW_VALUES = [];
                            ERR_PROPS = [];
                            PROPERTY_VALUES = [];
                            PROPVALS = [];
                            DELETED_VALUES = []
                        }
                        $(this).dialog("close")
                    }
                }
            })
        } else {
            NEW_VALUES = [];
            ERR_PROPS = [];
            PROPERTY_VALUES = [];
            PROPVALS = [];
            DELETED_VALUES = [];
            tempContent += "<br/>Create another?";
            $dialog.html(tempContent);
            $dialog.dialog({
                width: 350,
                height: "auto",
                position: _dialogPos,
                modal: true,
                title: "Submittal Result",
                open: function() {
                    $(this).parents(".ui-dialog:first").find(".ui-dialog-titlebar-close").remove()
                },
                buttons: {
                    No: function() {
                        SELECTED_VIEW = null;
                        SELECTED_ITEM_ID = null;
                        $(this).dialog("close");
                        VIEW_FROM_PROCESS = false;
                        $tabs.hide();
                        $driver.hide();
                        $("#divAssets").hide();
                        window.location = URL_CLIENT_UI + "/default.html"
                    },
                    Yes: function() {
                        showNewItemInstanceDialog();
                        $(this).dialog("close")
                    }
                }
            })
        }
    } finally {}
}

function toggleModeIconEnabled(isEnabled) {
    if (isEnabled) $("#imgMode").attr("disabled", null).removeClass("ui-state-disabled");
    else $("#imgMode").attr("disabled", "disabled").addClass("ui-state-disabled")
}

function toggleButton(buttonId, isEnabled, addClass) {
    if (addClass == undefined) addClass = true;
    var $ctl = $("#" + buttonId);
    if (isEnabled) {
        $ctl.attr("disabled", "");
        if (addClass) $ctl.removeClass("ui-state-disabled").addClass("ui-state-default")
    } else {
        $ctl.attr("disabled", "disabled");
        if (addClass) $ctl.removeClass("ui-state-default").addClass("ui-state-disabled")
    }
}

function toolbarCreate(view, currentModeIsReadOnly, isBookmarked) {
    var html = [],
        driverCaption = "item";
    if (view.DriverCaption !== null && view.DriverCaption != "") driverCaption = view.DriverCaption;
    if (view.IsReadOnly) html.push('<img id="imgMode" width="32" height="32" disabled="disabled" src="images/trans32.png" class="tb-lock" onClick="changeMode(false, true);" title="Edit this asset" />');
    else if (currentModeIsReadOnly) html.push('<img id="imgMode" src="images/trans32.png" width="32" height="32" class="tb-unlock" onclick="changeMode(true, true);" title="Switch to read-only"/>');
    else html.push('<img id="imgMode" src="images/trans32.png" width="32" height="32" class="tb-lock" onclick="changeMode(false, true);" title="Edit this asset"/>');
    if (isBookmarked) html.push('<img id="imgBookmark" width="32" height="32" class="tb-fav-remove" src="images/trans32.png" title="Remove from Favorites" onClick="removeBookmark()" />');
    else html.push('<img id="imgBookmark" width="32" height="32" class="tb-fav-add" src="images/trans32.png" title="Add to Favorites" onClick="addBookmark()" />');
    html.push('<img src="images/trans32.png" width="32" height="32" id="imgHistory" class="tb-history" title="Show item history" onClick="showHistory()"/>');
    html.push('<img src="images/trans32.png" width="32" height="32" id="imgPrint" class="tb-print" title="Print" onClick="showPrintDialog()"/>');
    if (SELECTED_VIEW.filters !== null && SELECTED_VIEW.filters.length > 0) html.push('<img src="images/trans32.png" width="32" height="32" id="imgFilter" class="tb-filter" title="Show QuickFilters" onClick="showFilterDialog()"/>');
    var allowNew = view.AllowNewValues && view.IsCreation;
    if (allowNew) html.push('<img id="imgNewItem" width="32" height="32" src="images/trans32.png" class="tb-add" title="Create a new ' + driverCaption + '" onClick="driverChanged(0)"/>');
    if (USER_CURRENT.IsAdmin) html.push('<img id="imgMigrate" class="tb-migrate" width="32" height="32" src="images/trans32.png" title="Migrate Asset" onClick="onMigrateAssetClicked()" />');
    if (SELECTED_VIEW.prefetchAssets || _prefetch) {
        html.push('<img id="imgNext" width="32" height="32" class="tb-next" src="images/trans32.png" title="Next ' + driverCaption + '" onClick="onNextSelected()" />');
        html.push('<img id="imgPrev" width="32" height="32" class="tb-prev" src="images/trans32.png" title="Previous ' + driverCaption + '" onClick="onPreviousSelected()"/>')
    }
    return html.join("")
}

function toolbarCreateFilter(view) {
    try {
        var html = [],
            driverCaption = "item";
        if (view.DriverCaption !== null && view.DriverCaption != "") driverCaption = view.DriverCaption;
        if (view.AllowNewValues && view.IsCreation) html.push('<img id="imgNewItem" width="32" height="32" src="images/trans32.png" class="tb-add" title="Create a new ' + driverCaption + '" onClick="driverChanged(0)"/>');
        if (SELECTED_VIEW.filters !== null && SELECTED_VIEW.filters.length > 0) html.push('<img src="images/trans32.png" width="32" height="32" id="imgFilter" class="tb-filter" title="Show QuickFilters" onClick="showFilterDialog()"/>');
        return html.join("")
    } finally {}
}

function valueCleared(controlId) {
    var item = {
        id: controlId,
        value: ""
    };
    sendValue(item)
}

function warnUnsavedChanges(currentItemName, nextItemValue) {
    var textToShow = "Save changes for '" + currentItemName + "'?";
    textToShow += "<br/><br/>If you select 'No', your changes will be lost";
    var $dialog = $("#messageDialog");
    if ($dialog.is(":ui-dialog")) $dialog.dialog("destroy");
    $dialog.html(textToShow);
    $dialog.dialog({
        width: 450,
        height: "auto",
        position: _dialogPos,
        autoOpen: false,
        modal: true,
        title: "Save Changes?",
        buttons: {
            No: function() {
                NEW_VALUES = [];
                ERR_PROPS = [];
                $driver.val(asset_GetName(nextItemValue));
                $("#lstDriver-id").val(nextItemValue);
                CURRENT_MODE = MODE_VIEW;
                driverChanged(nextItemValue);
                $(this).dialog("close")
            },
            Yes: function() {
                $(this).dialog("close");
                confirmSubmit()
            }
        }
    });
    $dialog.dialog("open")
}

function showTagDialog() {
    var html = []
}

function monitorLeftRight(event) {
    if (event.type == "keypress") {
        var charCode;
        if (event.charCode) charCode = event.charCode;
        else if (event.charCode !== null && event.charCode == 0 && event.keyCode == ASCII_DECIMAL) charCode = ASCII_DELETE;
        else charCode = event.keyCode;
        if (charCode == ASCII_ARROW_LEFT) {
            onPreviousSelected();
            return false
        }
        if (charCode == ASCII_ARROW_RIGHT) {
            onNextSelected();
            return false
        }
        return true
    }
}
