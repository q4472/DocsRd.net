Nskd = window.Nskd || {};

Nskd.Json = {

    parse: function (s) {
        return window.eval('(' + s + ')');
    },

    toString: function (o) {
        return writeValue(o);

        function writeValue(v) {
            var s = '';
            if (v == null) s += 'null'; // null
            else {
                switch (typeof v) {
                    case 'object':
                        s += writeObject(v);
                        break;
                    case 'string':
                        s += writeString(v);
                        break;
                    case 'number':
                        s += v;
                        break;
                    case 'boolean':
                        s += (v) ? 'true' : 'false';
                        break
                    default:  // function, undefined.
                        s += 'null';
                        break;
                }
            }
            return s;
        }

        function writeString(v) {
            var s = '"';
            for (var i = 0; i < v.length; i++) {
                var ch = v.charAt(i);
                switch (ch) {
                    case '\"': s += '\\\"'; break;
                    case '\\': s += '\\\\'; break;
                    case '\/': s += '\\/'; break;
                    case '\b': s += '\\b'; break;
                    case '\f': s += '\\f'; break;
                    case '\n': s += '\\n'; break;
                    case '\r': s += '\\r'; break;
                    case '\t': s += '\\t'; break;
                    default:
                        var code = ch.charCodeAt(0);
                        if ((code >= 0x20 && code < 0x80) || (code >= 0x400 && code < 0x460)) {
                            s += ch;
                        }
                        else {
                            s += '\\u' + ('000' + code.toString(16)).slice(-4);
                        }
                        break;
                }
            }
            s += '"';
            return s;
        }

        function writeObject(v) {
            var s = '';
            if (v instanceof Array) {
                s += writeArray(v);
            } else if (v instanceof Date) {
                s += 'new Date(' + v.getTime() + ')';
            } else if (v instanceof Boolean) {
                s += (v) ? 'true' : 'false';
            } else if (v instanceof String) {
                s += writeString(v);
            } else if (v instanceof Number) {
                s += v;
            } else { // Math, RegExp ...
                s += '{';
                for (pr in v) { s += '"' + pr + '":' + writeValue(v[pr]) + ','; }
                if (s.slice(-1) == ',') s = s.slice(0, -1);
                s += '}';
            }
            return s;

            function writeArray(v) {
                var s = '[';
                for (var i = 0; i < v.length; i++) {
                    s += writeValue(v[i]) + ',';
                }
                if (s.slice(-1) == ',') s = s.slice(0, -1);
                s += ']';
                return s;
            }
        }
    }
};

