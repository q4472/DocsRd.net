var Nskd = window.Nskd || {};
Nskd.Http = {};
Nskd.Http.post = function (args) {
    var jqXHR = null;
    var wMsg = (typeof args.wMsg === 'undefined') ? 'Заргузка данных с сервера ...' : args.wMsg;
    var dMsg = (typeof args.dMsg === 'undefined') ? 'OK' : args.dMsg;
    var fMsg = (typeof args.fMsg === 'undefined') ? '<font color="red">Ошибка при заргузке данных с сервера.</font>' : args.fMsg;
    if (args != null && typeof args === 'object' && args.url) {
        var msgElement = (args.msgElement) ? $(args.msgElement) : $('#_layout_footer_msg');
        // пробуем отправить стандартный пакет если он есть
        if (args.rqp) {
            jqXHR = $.post(args.url, Nskd.Json.toString(args.rqp));
        } else {
            // если пакета нет, то отправляем данные без пакета
            jqXHR = $.post(args.url, args.data);
        }
        jqXHR.done(function (data) {
            if (wMsg) { msgElement.hide(); }
            if (dMsg) { msgElement.html(dMsg).show().fadeOut(3000); }
            // если в ответе пришел пакет то его распаковывыем
            //var rsp = null;
            //alert(data);
            if (typeof args.done === 'function') { args.done(data); }
        });
        jqXHR.fail(function () {
            if (wMsg) { msgElement.hide(); }
            if (fMsg) { msgElement.html(fMsg).show().fadeOut(3000); }
            if (typeof args.fail === 'function') { args.fail(); }
        });
        if (wMsg) { msgElement.html(wMsg).show(); }
    }
    return jqXHR;
};
