var _imgSrc = "../images";

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
    activateUserAutoComplete()
}

function activateUserAutoComplete() {
    $("#lstItems").focusin(function() {
        if (this.autoCompleteRendered != true) {
            var imgRandomID = new Date().getTime();
            $(this).after("<img id='img" + imgRandomID + "' src='../images/ajax-loader.gif' alt='spinner' style='position:absolute;top:" + $(this).offset().top + "px;left:" + $(this).offset().left + "px' />");
            var dataSource = users_GetAll(USER_CURRENT.Id),
                minChars = 0;
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

function onUserSelected(id) {
    if (id == 0) {
        addUser();
        return
    }
    var html = [];
    html.push("<h1>Roles</h1>");
    $.each(roles_GetForUser(id), function(index, item) {
        html.push(item.value + "</br>")
    });
    html.push("<h1>Activity</h1>");
    $.each(user_GetActivity(id), function(index, item) {
        html.push(item + "</br>")
    });
    $("#detail").html(html.join(""))
}

function addUser() {}
