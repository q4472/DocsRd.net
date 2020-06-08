if ((typeof Nskd) === 'undefined') Nskd = {};

Nskd.Utility = (Nskd.Utility) ? Nskd.Utility : {};

Nskd.Utility.DataTable = (function () {

    function _getCell(table, rowIndex, column) {
        var cell = null;
        if (table) {
            var cols = table.columns;
            var rows = table.rows;
            if (cols && rows) {
                var ci = -1;
                switch (typeof column) {
                    case 'number':
                        ci = column;
                        break;
                    case 'string':
                        ci = Nskd.Utility.DataTable.getColumnIndexByName(table, column);
                        break;
                    default:
                        break;
                }
                var dataType = cols[ci].dataType;
                var value = rows[rowIndex][ci];
                if (dataType && (typeof value != 'undefined')) {
                    cell = {
                        'dataTable': table,
                        'rowIndex': rowIndex,
                        'columnIndex': ci,
                        'dataType': dataType,
                        'value': value,
                        'setValue': function (v) {
                            this.dataTable.rows[this.rowIndex][this.columnIndex] = v;
                        }
                    };
                }
            }
        }
        return cell;
    }

    function _getValueFromHtmlElement(element, dataType) {
        var v = undefined;
        if (element) {
            v = null;
            var s = _getStringValueFromHtmlElement(element);
            if (s) {
                v = _parseHtmlValue(s, dataType);
            }
        }
        return v;
    }

    function _setValueToHtmlElement(id, v, dataType) {
        var s = _toHtmlString(v, dataType);
        var e = S(id);
        if (e) switch (e.nodeName) {
            case 'INPUT':
                e.value = s;
                break;
            case 'SELECT':
                e.selectedIndex = 0;
                for (var i = 0; i < e.length; i++) {
                    var value = e.options[i].value;
                    if (value) {
                        if (value == s) {
                            e.selectedIndex = i;
                            break;
                        }
                    } else {
                        var text = e.options[i].text;
                        if (text == s) {
                            e.selectedIndex = i;
                            break;
                        }
                    }
                }
                break;
            case 'DIV':
            case 'SPAN':
            case 'TD':
                Nskd.Dom.empty(e);
                e.appendChild(document.createTextNode(s));
                break;
            default:
                break;
        }
    }

    function _toHtmlString(v, dataType) {
        var s = '';
        if (dataType) switch (dataType) {
            case 'System.Byte':
            case 'System.Int16':
            case 'System.Int32':
            case 'System.Int64':
                if (typeof (v) == 'number') {
                    s = v.toFixed(0);
                }
                break;
            case 'System.String':
            case 'System.Guid':
                if (v) s = '' + v;
                break;
            case 'System.Single':
            case 'System.Double':
            case 'System.Decimal':
                if (typeof (v) == 'number') {
                    s = v.toFixed(2);
                }
                break;
            case 'System.Boolean':
                s = (v) ? 'true' : 'false';
                break;
            case 'System.DateTime':
                if (typeof (v) == 'object' && v instanceof Date) {
                    var dd = '' + v.getDate();
                    if (dd.length == 1) dd = '0' + dd;
                    var MM = '' + (v.getMonth() + 1);
                    if (MM.length == 1) MM = '0' + MM;
                    s = '' + dd + '.' + MM + '.' + v.getFullYear();
                }
                break;
            default:
                alert('Error in Nskd.Utility.DataTable._toHtmlString - dataType: ' + dataType);
                break;
        } //else s = '' + (v ? v : '');
        return s;
    }

    function _parseHtmlValue(s, dataType) {
        var v = null;
        switch (dataType) {
            case 'System.Byte':
            case 'System.Int16':
            case 'System.Int32':
            case 'System.Int64':
                v = parseInt(s);
                if (!isFinite(v)) v = null;
                break;
            case 'System.String':
            case 'System.Guid':
                v = s;
                break;
            case 'System.Single':
            case 'System.Double':
            case 'System.Decimal':
                v = parseFloat(s);
                if (!isFinite(v)) v = null;
                break;
            case 'System.Boolean':
                if (s == 'true') v = true;
                else if (s == 'false') v = false;
                else v = null;
                break;
            case 'System.DateTime':
                v = Nskd.parseDate(s);
                break;
            default:
                alert('Error in Nskd.Utility.DataTable._parseHtmlValue - Can not parse dataType: ' + dataType);
                break;
        }
        return v;
    }

    function _getStringValueFromHtmlElement(element) {
        var s = '';
        if (element) {
            var source = S(element);
            if (source) {
                if (source.nodeName == 'INPUT') {
                    s = source.value;
                } else if (source.nodeName == 'SELECT') {
                    var opts = source.options;
                    if (opts) {
                        s = opts[source.selectedIndex].value;
                    }
                } else {
                    s = GetInnerText(source);
                }
                s = s.replace(/^\s+|\s+$/g, '');
            }
        }
        return s;
    }

    return {

        getValue: function (table, ri, ci) {
            var v = null;
            if (table) {
                var rows = table.rows;
                var cols = table.columns;
                if (rows && (0 <= ri) && (ri < rows.length)) {
                    var row = rows[ri];
                    if (row) {
                        if (cols && (0 <= ci) && (ci < cols.length)) {
                            v = row[ci];
                        }
                    }
                }
            }
            return v;
        },

        getValueByColumnName: function (table, ri, columnName) {
            var v = null;
            if (table) {
                var rows = table.rows;
                var cols = table.columns;
                if (rows && (0 <= ri) && (ri < rows.length)) {
                    var row = rows[ri];
                    if (row) {
                        if (cols) {
                            for (var ci = 0; ci < cols.length; ci++) {
                                var col = cols[ci];
                                if (col && (col.columnName == columnName)) {
                                    v = row[ci];
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return v;
        },

        getColumnIndexByName: function (table, columnName) {
            var columnIndex = -1;
            if (table && columnName) {
                var cols = table.columns;
                if (cols) {
                    for (var ci = 0; ci < cols.length; ci++) {
                        var col = cols[ci];
                        if (col && (col.columnName == columnName)) {
                            columnIndex = ci;
                            break;
                        }
                    }
                }
            }
            return columnIndex;
        },

        copyValueFromTableToHtmlElement: function (element, table, rowIndex, column) {
            if (element) {
                var cell = _getCell(table, rowIndex, column);
                if (cell) {
                    var dataType = cell.dataType;
                    var value = cell.value;
                    _setValueToHtmlElement(element, value, dataType);
                }
            }
        },

        copyValueFromHtmlElementToTable: function (element, table, rowIndex, column) {
            if (element) {
                var cell = _getCell(table, rowIndex, column);
                if (cell) {
                    var dataType = cell.dataType;
                    var value = _getValueFromHtmlElement(element, dataType);
                    cell.setValue(value);
                }
            }
        }

    };
})();

//****************************************************
Nskd.parseDate = function (s) {

    s = Nskd.Js.trim(s);
    if (!Nskd.Js.is(s, 'string') || s.length === 0) { return null; }

    let v = null;

    let date = new Date();
    let yyyy = date.getFullYear();
    let MM = date.getMonth() + 1;
    let dd = date.getDate();

    let cs = s.split('.');
    if (cs.length == 3) {
        dd = parseInt(cs[0], 10);
        MM = parseInt(cs[1], 10);
        yyyy = parseInt(cs[2], 10);
        if (isFinite(dd) && isFinite(MM) && isFinite(yyyy)) {
            if (yyyy < 50) yyyy += 2000; else if (yyyy < 100) yyyy += 1900;
            v = new Date(yyyy, MM - 1, dd);
        }
    } else {
        cs = s.split('-');
        if (cs.length == 3) {
            dd = parseInt(cs[2], 10);
            MM = parseInt(cs[1], 10);
            yyyy = parseInt(cs[0], 10);
            if (isFinite(dd) && isFinite(MM) && isFinite(yyyy)) {
                if (yyyy < 50) yyyy += 2000; else if (yyyy < 100) yyyy += 1900;
                v = new Date(yyyy, MM - 1, dd);
            }
        }
    }
    return v;
};

function createOrGetElement(el, ns, attr, st) {
    var res = null;
    if (is(el, 'string'))
        if (is(ns, 'string'))
            if (ns == '') res = document.createElement(el);
            else res = document.createElementNS(ns, el);
        else res = document.getElementById(el);
    else res = el;
    if (res) {
        if (attr) {
            for (var key in attr) if (attr.hasOwnProperty(key)) {
                res.setAttribute(key, String(attr[key]));
            }
        }
        if (st) {
            for (var key in st) if (st.hasOwnProperty(key)) {
                res.style[key] = String(st[key]);
            }
        }
    }
    return res;
    function is(o, t) {
        return ((t == 'undefined') && (o === undefined))
            || ((t == 'null') && (o === null))
            || ((t == 'boolean') && ((typeof o == 'boolean') || (o instanceof Boolean)))
            || ((t == 'string') && ((typeof o == 'string') || (o instanceof String)))
            || ((t == 'number') && ((typeof o == 'number') || (o instanceof Number)))
            || ((t == 'function') && (typeof o == 'function'))
            || ((t == 'object') && (typeof o == 'object') && (o instanceof Object))
            || ((t == 'array') && (typeof o == 'object') && (o instanceof Array))
            || ((t == 'date') && (typeof o == 'object') && (o instanceof Date));
    }
}

function S(el, ns, attr, st) {
    var res = null;
    if (is(el, 'string'))
        if (is(ns, 'string'))
            if (ns == '') res = document.createElement(el);
            else res = document.createElementNS(ns, el);
        else res = document.getElementById(el);
    else res = el;
    if (res) {
        if (attr) {
            for (var key in attr) if (attr.hasOwnProperty(key)) {
                res.setAttribute(key, String(attr[key]));
            }
        }
        if (st) {
            for (var key in st) if (st.hasOwnProperty(key)) {
                res.style[key] = String(st[key]);
            }
        }
    }
    return res;
    function is(o, t) {
        return ((t == 'undefined') && (o === undefined))
            || ((t == 'null') && (o === null))
            || ((t == 'boolean') && ((typeof o == 'boolean') || (o instanceof Boolean)))
            || ((t == 'string') && ((typeof o == 'string') || (o instanceof String)))
            || ((t == 'number') && ((typeof o == 'number') || (o instanceof Number)))
            || ((t == 'function') && (typeof o == 'function'))
            || ((t == 'object') && (typeof o == 'object') && (o instanceof Object))
            || ((t == 'array') && (typeof o == 'object') && (o instanceof Array))
            || ((t == 'date') && (typeof o == 'object') && (o instanceof Date));
    }
}

function GetInnerText(node) {
    var v = '';
    if (node) {
        var cns = node.childNodes;
        if (cns) {
            for (var ni = 0; ni < cns.length; ni++) {
                var cn = cns[ni];
                switch (cn.nodeType) {
                    case 1:
                        v += GetInnerText(cn);
                        break;
                    case 3:
                        v += cn.nodeValue;
                        break;
                    default:
                        break;
                }
            }
        }
    }
    return v;
}

function GetChildElements(node) {
    var ces = null;
    if (node) {
        var ces = new Array();
        var cns = node.childNodes;
        for (var ni = 0; ni < cns.length; ni++) {
            var cn = cns[ni];
            if (cn.nodeType == 1) {
                ces.push(cn);
            }
        }
    }
    return ces;
}

function GetFirstChildElement(node) {
    var fce = null;
    if (node) {
        ces = GetChildElements(node);
        if ((ces) && (ces.length > 0)) {
            fce = ces[0];
        }
    }
    return fce;
}

function GetLocationSearchArgByName(n) {
    var v = null;
    var s = location.search;
    if (s.length > 1) {
        var args = unescape(s.substr(1)).split('&');
        for (var i = 0; i < args.length; i++) {
            var arg = args[i].split('=');
            if (arg.length >= 2) {
                if (arg[0] == n) {
                    v = arg[1];
                    break;
                }
            }
        }
    }
    return v;
}
