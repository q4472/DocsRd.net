Nskd = window.Nskd || {};

Nskd.Js = (function () {
    var lut = []; for (var i = 0; i < 256; i++) { lut[i] = (i < 16 ? '0' : '') + (i).toString(16); }
    function toPostBodyString(dataId) {
        var data = null;
        var hasError = false;
        if (dataId) {
            data = '';
            var $data = $('#' + dataId);
            $data.find('input').each(function (index, element) {
                if (!hasError) {
                    if ($(element).hasClass('error')) {
                        hasError = true;
                    } else {
                        if (element.name !== '') {
                            if (element.type === 'checkbox') {
                                data += '&' + element.name + '=' + escape(element.checked);
                            } else {
                                data += '&' + element.name + '=' + escape(element.value);
                            }
                        }
                    }
                }
            });
            $data.find('select').each(function (index, element) {
                if (!hasError) {
                    if ($(element).hasClass('error')) {
                        hasError = true;
                    } else {
                        if ((element.name !== '') && (element.value !== '')) {
                            data += '&' + element.name + '=' + escape($(element).val());
                        }
                    }
                }
            });
            data = data.substring(1, data.length);
            //alert(data);
        }
        return { hasError: hasError, data: data };
    }
    return {
        is: function (o, t) {
            return ((t === 'undefined') && (o === undefined))
                || ((t === 'null') && (o === null))
                || ((t === 'boolean') && ((typeof o === 'boolean') || (o instanceof Boolean)))
                || ((t === 'string') && ((typeof o === 'string') || (o instanceof String)))
                || ((t === 'number') && ((typeof o === 'number') || (o instanceof Number)))
                || ((t === 'function') && (typeof o === 'function'))
                || ((t === 'object') && (typeof o === 'object') && (o instanceof Object))
                || ((t === 'array') && (typeof o === 'object') && (o instanceof Array))
                || ((t === 'date') && (typeof o === 'object') && (o instanceof Date));
        },
        guid: function () {
            var d0 = Math.random() * 0xffffffff | 0;
            var d1 = Math.random() * 0xffffffff | 0;
            var d2 = Math.random() * 0xffffffff | 0;
            var d3 = Math.random() * 0xffffffff | 0;
            return lut[d0 & 0xff] + lut[d0 >> 8 & 0xff] + lut[d0 >> 16 & 0xff] + lut[d0 >> 24 & 0xff] + '-' +
                lut[d1 & 0xff] + lut[d1 >> 8 & 0xff] + '-' + lut[d1 >> 16 & 0x0f | 0x40] + lut[d1 >> 24 & 0xff] + '-' +
                lut[d2 & 0x3f | 0x80] + lut[d2 >> 8 & 0xff] + '-' + lut[d2 >> 16 & 0xff] + lut[d2 >> 24 & 0xff] +
                lut[d3 & 0xff] + lut[d3 >> 8 & 0xff] + lut[d3 >> 16 & 0xff] + lut[d3 >> 24 & 0xff];
        },
        post: function (url, dataId, success, dest, loadingId) {
            var b = toPostBodyString(dataId);
            if (!b.hasError) {
                //alert('post ' + url + ' ' + b.data);
                $.post(url, b.data, function (data) {
                    //alert(data);

                    if (dest !== null) {
                        if (Nskd.Js.is(dest, 'string')) {
                            $('#' + dest).html(data);
                        } else {
                            dest.html(data);
                        }
                    }

                    if (loadingId) $('#' + loadingId).hide();
                    if (success) success();
                });
                if (loadingId) $('#' + loadingId).show();
            } else {
                alert("В форме есть ошибки. Они выделены красной рамкой.");
                Nskd.Validator.parse('#' + dataId);
            }
        },
        trim: function (str) {
            if (Nskd.Js.is(str, 'string')) {
                return str.replace(/^[\s\uFEFF\xA0]+|[\s\uFEFF\xA0]+$/g, '');
            } else {
                return str;
            }
        }
    };
})();
Nskd.Validator = (function () {
    return {
        parse: function (formSelector) {
            var form = $(formSelector);
            // общее правило - не выводить сообщений об ошибках
            form.validate({ errorPlacement: function () { } });
            // правила для каждого элемента формы
            form.find('input[data-val="true"]').each(function (index, element) {
                if (element.name === '') { element.name = Nskd.Js.guid(); }
                var $e = $(element);
                var v;
                v = element.getAttribute('data-val-required'); if (v !== null) { $e.rules('add', { required: true }); }
                v = element.getAttribute('data-val-digits'); if (v !== null) { $e.rules('add', { digits: true }); }
                v = element.getAttribute('data-val-date'); if (v !== null) { $e.rules('add', { date: true }); }
                v = element.getAttribute('data-val-max'); if (v !== null) { $e.rules('add', { max: v }); }
                v = element.getAttribute('data-val-maxlength'); if (v !== null) { $e.rules('add', { maxlength: v }); }
                v = element.getAttribute('data-val-min'); if (v !== null) { $e.rules('add', { min: v }); }
                v = element.getAttribute('data-val-minlength'); if (v !== null) { $e.rules('add', { minlength: v }); }
                v = element.getAttribute('data-val-number'); if (v !== null) { $e.rules('add', { number: true }); }
            });
        },
        dateNorm: function (str) {

            if (!Nskd.Js.is(str, 'string') || str.length === 0) { return ''; }

            var date = new Date();
            var y = date.getFullYear();
            var m = date.getMonth() + 1;
            var d = date.getDate();

            var order = str.match(/[\.,/]/);
            if (order) {
                str = str.replace(/[\.,/]/g, '-');
            }
            str = str.replace(/[^\d-]/g, '');
            str = str.replace(/^-+|-+$/g, '');

            var ps = str.split('-');
            if (ps.length > 0) {
                var p0 = parseInt(ps[0], 10);
                if (isNaN(p0)) p0 = 1;
                if (ps.length === 1) { // есть только одна часть
                    d = p0;
                } else { // ps.length > 1
                    var p1 = parseInt(ps[1], 10);
                    if (isNaN(p1)) p1 = 1;
                    if (ps.length === 2) {  // есть только две части
                        d = p0;
                        m = p1;
                    } else {  // ps.length > 2
                        var p2 = parseInt(ps[2], 10);
                        if (isNaN(p2)) p2 = 1;
                        if (p0 > 31) {
                            y = p0;
                            m = p1;
                            d = p2;
                        } else {
                            if (p2 > 31) {
                                d = p0;
                                m = p1;
                                y = p2;
                            } else {
                                if (order) {
                                    d = p0;
                                    m = p1;
                                    y = p2;
                                } else {
                                    y = p0;
                                    m = p1;
                                    d = p2;
                                }
                            }
                        }
                    }
                }
            }
            y = ((y < 50) ? 2000 + y : ((y < 100) ? 1900 + y : y));
            return ('' + y + '-' + ((m < 10) ? '0' + m : m) + '-' + ((d < 10) ? '0' + d : d));
        },
        dNorm: function (element, format) {
            if (element.nodeName == 'INPUT') {
                element.value = Nskd.Validator.dateNorm(element.value);
            }
        },
        numberNorm: function (str, min, max) {
            str = str.replace(/,/g, '.');
            str = str.replace(/[^\d\.-]/g, '');
            var sign = (str[0] === '-') ? '-' : '';
            str = str.replace(/^\.+|\.+$/g, '');
            str = sign + str.replace(/-/g, '');
            var val = parseFloat(str);
            if (isNaN(val)) {
                str = '';
            } else {
                if (Nskd.Js.is(min, 'number') && val < min) { val = min; }
                if (Nskd.Js.is(max, 'number') && val > max) { val = max; }
                str = val.toString();
            }
            return str;
        },
        nNorm: function (element, fractionDigits) {
            if (element.nodeName === 'INPUT') {
                var v = element.value;
                v = v.replace(/,/g, '.').replace(/[^\d\.\+\-eE]/g, '');
                var f = parseFloat(v);
                if (isNaN(f)) {
                    v = '';
                } else {
                    if (!Nskd.Js.is(fractionDigits, 'number')) { fractionDigits = 0; }
                    v = f.toFixed(fractionDigits);
                }
                var len = v.length;
                var pointIndex = v.indexOf('.');
                if (pointIndex < 0) {
                    for (var g = 3; g <= 9; g += 3) {
                        if (len > g) {
                            v = v.substring(0, len - g) + ' ' + v.substring(len - g);
                        }
                    }
                } else {
                    for (var g = 3; g <= 9; g += 3) {
                        if (pointIndex > g) {
                            v = v.substring(0, pointIndex - g) + ' ' + v.substring(pointIndex - g);
                        }
                    }
                }
                element.value = v;
            }
        }
    };
})();
