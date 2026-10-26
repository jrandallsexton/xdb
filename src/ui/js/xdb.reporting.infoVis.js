var Log = {
        elem: false,
        write: function(text) {
            if (!this.elem) this.elem = document.getElementById("log");
            this.elem.innerHTML = text;
            this.elem.style.left = 500 - this.elem.offsetWidth / 2 + "px"
        }
    },
    _imgPath = "../images";

function initForm() {
    getCurrentUser();
    displayCurrentUser(_imgPath);
    $(document.getElementById("header")).html(buildHeaderContent(_imgPath));
    $(document.getElementById("navigation")).html(buildMenuHtml(_imgPath));
    refreshRecent(_imgSrc);
    initMenu();
    $.blockUI({
        message: '<p>... <i>Loading Relations (this may take some time)</i> ...</p><img src="../images/ajax-loader.gif" alt="spinner" />',
        css: {
            border: "none",
            padding: "15px",
            backgroundColor: "#000",
            "-webkit-border-radius": "10px",
            "-moz-border-radius": "10px",
            opacity: .5,
            color: "#fff"
        }
    });
    var assetId = getParameterByName("assetId"),
        data = ajaxGet(URL_REST_SVC + "/rgraph?assetId=" + assetId),
        rgraph = new $jit.RGraph({
            injectInto: "infovis",
            levelDistance: 375,
            background: {
                CanvasStyles: {
                    strokeStyle: "#555"
                }
            },
            Navigation: {
                enable: true,
                panning: true,
                zooming: 10
            },
            Node: {
                color: "#ddeeff"
            },
            Edge: {
                color: "#3333CC",
                lineWidth: 1.5
            },
            onBeforeCompute: function(node) {
                Log.write("centering " + node.name + "...");
                $jit.id("inner-details").innerHTML = node.data.relation
            },
            onCreateLabel: function(domElement, node) {
                domElement.innerHTML = node.name;
                domElement.onclick = function() {
                    rgraph.onClick(node.id, {
                        onComplete: function() {
                            Log.write("done")
                        }
                    })
                }
            },
            onPlaceLabel: function(domElement, node) {
                var style = domElement.style;
                style.display = "";
                style.cursor = "pointer";
                if (node._depth <= 1) {
                    style.fontSize = "1.1em";
                    style.color = "#ccc"
                } else if (node._depth == 2) {
                    style.fontSize = "1.0em";
                    style.color = "#494949"
                } else style.display = "none";
                var left = parseInt(style.left),
                    w = domElement.offsetWidth;
                style.left = left - w / 2 + "px"
            }
        });
    rgraph.loadJSON(data);
    rgraph.graph.eachNode(function(n) {
        var pos = n.getPos();
        pos.setc(-200, -200)
    });
    rgraph.compute("end");
    rgraph.fx.animate({
        modes: ["polar"],
        duration: 2000
    });
    bindNodeClicks();
    $.unblockUI()
}

function bindNodeClicks() {
    $(".node").click(function() {
        alert("Handler for .click() called.")
    })
}
