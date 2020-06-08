
Nskd = window.Nskd || {};

Nskd.Menu = function (domMenu, jsoMenu, selectedNodePath, targetSelector) {

    // private fields and constructor
    var selectedNode = null;

    // всё очистить
    $(domMenu).empty();

    // рисуем меню (рекурсивно), если найдётся selectedNodePath, то будет заполнен selectedNode
    addDomNode(domMenu, jsoMenu, ''); //[' + jsoMenu.name + ']

    // пункт меню выбираем сразу
    if (selectedNode) {
        getDomHead(selectedNode).onclick();
    }

    // добавляем кнопку перехода
    var div = $('<div style="margin-top: 20px; padding: 2px; text-align: right">').appendTo(domMenu);
    var button = $('<input type="button" value="------>>>">').appendTo(div);
    button.click(function () {
        Nskd.Server.gotoTheNewPage({ cmd: 'GotoFromMenu' }, targetSelector);
    });

    return;

    // private functions

    function addDomNode(domCont, jsoNode, nodePath) {
        var domNode = document.createElement('div');
        if ((nodePath == '') || (nodePath == '.' + selectedNodePath)) {
            selectedNode = domNode;
            //alert(selectedNodePath + ' == ' + nodePath);
        }
        domNode.className = 'nskdMenuNode ';
        domCont.appendChild(domNode);
        {
            addDomNodeHead(domNode, jsoNode, nodePath);
            addDomNodeUrl(domNode, jsoNode, nodePath);
            addDomNodeCont(domNode, jsoNode, nodePath);
        }
        //return domNode;
    }

    function addDomNodeHead(domNode, jsoNode, nodePath) {
        var domHead = document.createElement('div');
        domHead.className = 'nskdMenuNodeHead ';
        domHead.onclick = menuNodeHeadOnclick;
        {
            addDomNodeHeadMark(domHead, jsoNode);
            addDomNodeHeadName(domHead, jsoNode);
        }
        domNode.appendChild(domHead);
    }

    function addDomNodeUrl(domNode, jsoNode, nodePath) {
        $('<input type="hidden" value="' + jsoNode.url + '">').appendTo(domNode);
    }

    function menuNodeHeadOnclick() {
        var domNode = this.parentNode;
        resetDomNodes();
        selectedNodePath = selectDomNode(domNode);
        // регистрируем выбор в параметрах среды
        Nskd.Client.EnvVars.selectedMenuNodePath = selectedNodePath;
        Nskd.Client.EnvVars.selectedMenuNodeUrl = $(getDomUrl(domNode)).val();
    };

    function addDomNodeHeadMark(domHead, jsoNode) {
        var domMark = document.createElement('div');
        domMark.className = ((jsoNode.cont) && (jsoNode.cont.length > 0)) ?
            'nskdMenuNodeHeadMark nskdMenuNodeHeadMark_plus' :
            'nskdMenuNodeHeadMark nskdMenuNodeHeadMark_leaf';
        domHead.appendChild(domMark);
    }

    function addDomNodeHeadName(domHead, jsoNode) {
        var domName = document.createElement('div');
        domName.className = 'nskdMenuNodeHeadName ';
        {
            $('<span>').text(jsoNode.name).appendTo(domName);
        }
        domHead.appendChild(domName);
    }

    function addDomNodeCont(domNode, jsoNode, nodePath) {
        var domCont = document.createElement('div');
        domCont.className = 'nskdMenuNodeCont ';
        if (jsoNode.cont) {
            for (var i = 0; i < jsoNode.cont.length; i++) {
                var node = jsoNode.cont[i];
                addDomNode(domCont, node, nodePath + '.[' + node.name + ']');
            }
        }
        domNode.appendChild(domCont);
    }

    function resetDomNodes() {
        var divs = domMenu.getElementsByTagName('div');
        for (var i = 0; i < divs.length; i++) {
            var div = divs[i];
            if (div.className.indexOf('nskdMenuNode ') >= 0) {
                var head = getDomHead(div);
                head.style.backgroundColor = 'transparent';
                var cont = getDomCont(div);
                if (cont) {
                    cont.style.paddingLeft = '0px';
                }
                hide(div);
            }
        }
    }

    function selectDomNode(domNode) {
        var head = getDomHead(domNode);
        //var url = getDomUrl(domNode);
        //var cont = getDomCont(domNode);
        head.style.backgroundColor = '#ffff88';
        var selectedNodePath = showParentDomNodeChain(domNode);
        //showSiblingDomNodes(domNode);
        showChildDomNodes(domNode);
        return selectedNodePath;
    };

    function showParentDomNodeChain(domNode) {
        var selectedNodePath = '';
        var node = domNode;
        while (node) {
            var segment = node.firstChild.childNodes[1].firstChild.firstChild.nodeValue;
            selectedNodePath = '[' + segment + '].' + selectedNodePath;
            var cont = getDomCont(node);
            if (cont) {
                cont.style.paddingLeft = '0px';
            }
            show(node);
            node = getParentDomNode(node);
        }
        selectedNodePath = selectedNodePath.substring(0, (selectedNodePath.length - 1));
        return selectedNodePath;
    }

    function showSiblingDomNodes(domNode) {
        var node = getParentDomNode(domNode);
        if (node) {
            showChildDomNodes(node);
        }
    }

    function showChildDomNodes(domNode) {
        var cont = getDomCont(domNode);
        if (cont) {
            cont.style.paddingLeft = '8px';
            var nodes = cont.childNodes;
            for (var i = 0; i < nodes.length; i++) {
                show(nodes[i]);
            }
        }
    }

    function show(domNode) { domNode.style.display = 'block'; }

    function hide(domNode) { domNode.style.display = 'none'; }

    function getDomHead(domNode) { return domNode.childNodes[0]; }

    function getDomUrl(domNode) { return domNode.childNodes[1]; }

    function getDomCont(domNode) { return domNode.childNodes[2]; }

    function getParentDomNode(domNode) {
        p = domNode.parentNode.parentNode;
        return ((p.className.indexOf('nskdMenuNode ') >= 0) ? p : null);
    }
};
