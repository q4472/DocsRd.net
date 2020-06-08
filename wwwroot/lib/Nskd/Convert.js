Nskd = window.Nskd || {};

Nskd.Convert = (function () {
    var my = {};

    my.byteArrayToHexString = function (b) {
        var s = '';
        for (var i = 0; i < b.length; i++) {
            var t = b[i].toString(16);
            s += (t.length < 2) ? '0' + t : t;
        }
        return s;
    };

    my.hexStringToByteArray = function (s) {
        var b = [];
        if ((s.length % 2) == 1) s += "0";
        for (var i = 0; i < s.length; i += 2) {
            b[i / 2] = parseInt(s.substring(i, i + 2), 16);
        }
        return b;
    };

    my.byteArrayToBase64String = function (a) {
        var s = '';
        var len = a.length / 3;
        for (var i = 0; i < len; i++) {
            var i3 = i * 3;
            var n0 = (i3 < a.length) ? a[i3++] : 0;
            var n1 = (i3 < a.length) ? a[i3++] : 0;
            var n2 = (i3 < a.length) ? a[i3] : 0;
            s += String.fromCharCode(ntob64cc(n0 >> 2));
            s += String.fromCharCode(ntob64cc(((n0 & 3) << 4) | (n1 >> 4)));
            s += String.fromCharCode(ntob64cc(((n1 & 15) << 2) | (n2 >> 6)));
            s += String.fromCharCode(ntob64cc(n2 & 63));
        }
        var tail = a.length % 3;
        if (tail == 1) {
            s = s.substring(0, s.length - 2) + '==';
        } else if (tail == 2) {
            s = s.substring(0, s.length - 1) + '=';
        }
        return s;

        function ntob64cc(n) {
            return (n >= 0 && n < 26) ? n + 65
            : (n < 52) ? n + 71
            : (n < 62) ? n - 4
            : (n === 62) ? 43
            : (n === 63) ? 47
            : 61;
        }
    };

    my.base64StringToByteArray = function (s) {
        var a = [];
        var len = s.length / 4;
        for (var i = 0; i < len; i++) {
            var i4 = i * 4;
            var c0 = b64ccton(s.charCodeAt(i4++));
            var c1 = b64ccton(s.charCodeAt(i4++));
            var c2 = b64ccton(s.charCodeAt(i4++));
            var c3 = b64ccton(s.charCodeAt(i4));
            a.push(((c0 << 2) | (c1 >> 4)) & 255);
            if (c2 >= 0) {
                a.push(((c1 << 4) | (c2 >> 2)) & 255);
                if (c3 >= 0) {
                    a.push(((c2 << 6) | (c3 >> 0)) & 255);
                }
            }
        }
        return a;

        function b64ccton(b64cc) {
            return ((b64cc > 64 && b64cc < 91) ? b64cc - 65
            : (b64cc > 96 && b64cc < 123) ? b64cc - 71
            : (b64cc > 47 && b64cc < 58) ? b64cc + 4
            : (b64cc === 43) ? 62
            : (b64cc === 47) ? 63
            : -1);
        };
    };

    my.byteArrayToBigInteger = function (bytes) {
        var rBytes = [];
        i = bytes.length;
        while (--i >= 0) rBytes.push(bytes[i]);
        return new BigInteger(rBytes, 256);
    };

    my.bigIntegerToByteArray = function (n) {
        // встроенная функция от BigInteger. За ней надо исправлять порядок и знаки у байтов.
        var bytes = n.toByteArray();
        var rBytes = [];
        var i = bytes.length;
        while (--i >= 0) rBytes.push(bytes[i] & 0xff);
        return rBytes;
    };

    // Convert javascript string to number array with Unicode values
    my.stringToUnicodeArray = function (s) {
        var u = [];
        for (var i = 0; i < s.length; i++) {
            var c = s.charCodeAt(i);
            if (c < 0xd800) {
                u.push(c);
            } else if (c < 0xdc00) {
                if (++i < s.length) {
                    var d = s.charCodeAt(i);
                    if ((d & 0xfc00) == 0xdc00) {
                        u.push((((c & 0x03ff) << 10) | (d & 0x03ff)) + 0x10000);
                    }
                } else break;
            } else if (c < 0xe000) {
            } else {
                u.push(c);
            }
        }
        return u;
    };

    // Convert javascript string to number array with UTF8 values
    my.stringToUtf8Array = function (s, insertBOM) {
        var b = [];
        if (insertBOM) {
            b.push(0xef);
            b.push(0xbb);
            b.push(0xbf);
        }
        var u = Nskd.Convert.stringToUnicodeArray(s);
        for (var i = 0; i < u.length; i++) {
            var c = u[i];
            if (c < 0x80) {
                b.push(c);
            } else if (c < 0x800) {
                b.push(((c >> 06) & 0x1f) | 0xc0);
                b.push(((c >> 00) & 0x3f) | 0x80);
            } else if (c < 0x10000) {
                b.push(((c >> 12) & 0x0f) | 0xe0);
                b.push(((c >> 06) & 0x3f) | 0x80);
                b.push(((c >> 00) & 0x3f) | 0x80);
            } else {
                b.push(((c >> 18) & 0x07) | 0xf0);
                b.push(((c >> 12) & 0x3f) | 0x80);
                b.push(((c >> 06) & 0x3f) | 0x80);
                b.push(((c >> 00) & 0x3f) | 0x80);
            }
        }
        return b;
    };

    my.utf8ArrayToString = function (array) {
        var out, i, len, c;
        var char2, char3;

        out = "";
        len = array.length;
        i = 0;
        while (i < len) {
            c = array[i++];
            switch (c >> 4) {
                case 0: case 1: case 2: case 3: case 4: case 5: case 6: case 7:
                    // 0xxxxxxx
                    out += String.fromCharCode(c);
                    break;
                case 12: case 13:
                    // 110x xxxx   10xx xxxx
                    char2 = array[i++];
                    out += String.fromCharCode(((c & 0x1F) << 6) | (char2 & 0x3F));
                    break;
                case 14:
                    // 1110 xxxx  10xx xxxx  10xx xxxx
                    char2 = array[i++];
                    char3 = array[i++];
                    out += String.fromCharCode(((c & 0x0F) << 12) |
                       ((char2 & 0x3F) << 6) |
                       ((char3 & 0x3F) << 0));
                    break;
            }
        }
        return out;
    };

    return my;
})();

