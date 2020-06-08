
Nskd = window.Nskd || {};

Nskd.Server = {};

Nskd.Server.SessionId = null;
Nskd.Server.EncryptedKeyMessage = null;
Nskd.Server.CryptServiceProvider = null;

Nskd.Server.HttpRequest = {
    post: function (url, data, done, fail) {
        var xhr = new XMLHttpRequest();
        if (xhr != null) {
            xhr.open('POST', url, true);
            xhr.onreadystatechange = function () {
                if (xhr.readyState == 4) {
                    if (xhr.status == 200) {
                        //alert(xhr.responseText.charAt(0));
                        done(xhr.responseText);
                    } else {
                        fail(xhr.status.toString());
                    }
                }
            };
            xhr.send(data);
        }
    }
};

Nskd.Server.SessionRequest = {
    post: function (url, data, done, fail) {
        // добавляем заголовок
        data = Nskd.Server.SessionId + '\r\n' + data;
        // отправляем
        Nskd.Server.HttpRequest.post(url, data, done, fail);
    }
};

Nskd.Server.CryptRequest = {
    post: function (url, data, done, fail) {
        // шифрование
        data = Nskd.Convert.stringToUtf8Array(data);
        data = Nskd.Server.CryptServiceProvider.encrypt(data);
        data = Nskd.Convert.byteArrayToBase64String(data);
        // добавляем заголовок с ключём (нужен только первый раз.)
        if (Nskd.Server.EncryptedKeyMessage != null) {
            data = Nskd.Convert.byteArrayToBase64String(Nskd.Server.EncryptedKeyMessage) + '\r\n' + data;
        }
        // отправляем
        Nskd.Server.SessionRequest.post(url, data, __done, fail);
        // обрабатываем ответ
        function __done(data) {
            if (data.charAt(0) != '<') { // некоторые сообщения приходят не зашифрованными
                // дешифрование
                data = Nskd.Convert.base64StringToByteArray(data);
                data = Nskd.Server.CryptServiceProvider.decrypt(data);
                data = Nskd.Convert.utf8ArrayToString(data);
            }
            done(data);
        }
    }
};



Nskd.Server.JsonRequest = {
    post: function (url, data, done, fail) {
        var json = Nskd.Json.toString(data);

        //alert(json);

        // Замена для Фарм-Сиб 2015-10-02 Соколов
        //Nskd.Server.CryptRequest.post(url, json, _done, fail);
        Nskd.Server.HttpRequest.post(url, json, _done, fail);

        function _done(text) {

            //alert(text);

            if (text.charAt(0) == '{') {
                var object = Nskd.Json.parse(text);
                done(object);
            } else done(text);
        }
    }
};

Nskd.Server.gotoTheNewPage = function (data, targetSelector) {
    // это новое (для Фарм-сиб 2015-09-03)
    var url = Nskd.Client.EnvVars.selectedMenuNodeUrl;
    if (url && url != 'null') {
        var sessionId = Nskd.Server.SessionId;
        if (!sessionId) sessionId = '00000000-0000-0000-0000-000000000000';
        var jqXHR = $.post(url, 'sessionId=' + sessionId, function (data) {
            //alert(data);
            $(targetSelector).html(data);
            //$.validator.unobtrusive.parse(targetSelector);
        });
        jqXHR.fail(function () { alert(jqXHR.responseText); });
    }
    return;
    // это пока обходим (для Фарм-сиб 2015-09-03)
    if (Nskd.Js.is(data, 'object')) {
        data.cmdType = 'GotoTheNewPage';
        data.envVars = Nskd.Client.EnvVars;
        // отправляем пакет на сервер с указанием что делать с ответом (_done) и с ошибкой (_fail)
        Nskd.Server.JsonRequest.post('/', data, _done, _fail);

        // пока ждём - считаем секунды
        _showTheWaitMessage();
    }
    return;

    function _done(pack) {

        if ((typeof pack) === 'object') {
            __gotoNewPage(pack.data);
        } else if ((typeof pack) === 'string') __gotoNewPage(pack);

        function __gotoNewPage(html) {
            // добавляем данные для передачи на следующую страницу
            var i = html.indexOf('</head>');
            var content = html.substring(0, i) +
            '\n<script>\n' +
            ' window.onload = function () { \n' +
            '  var key = [' + Nskd.Server.CryptServiceProvider.getKey().toString() + '];\n' +
            '  if ((typeof Nskd) != \'undefined\') {\n' +
            '    Nskd.Server.SessionId = \'' + Nskd.Server.SessionId + '\';\n' +
            '    Nskd.Server.EncryptedKeyMessage = null;\n' +
            '    Nskd.Server.CryptServiceProvider = new Nskd.Crypt.aes(key);\n' +
            '  }\n' +
            ' };\n' +
            '</script>\n' +
            html.substring(i);
            // готово
            document.write(content);
            document.close();
        }
    }

    function _fail(status) {
        alert('Error: XMLHttpRequest.status = ' + status);
    }

    function _showTheWaitMessage() {
        var body = document.body;
        $(body).empty();
        var div = document.createElement('div');
        body.appendChild(div);
        $(div).text('Запрос отправлен на сервер. Ожидается ответ. ');
        var span = document.createElement('span');
        div.appendChild(span);
        var count = 0;
        __showCount();
        return;

        function __showCount() {
            $(span).empty();
            $(span).text((count++).toString());
            if (count < 100) setTimeout(__showCount, 1000);
        }
    }
};

Nskd.Server.execute = function (data, done) {
    if (Nskd.Js.is(data, 'object')) {
        data.cmdType = 'Execute';
        data.envVars = Nskd.Client.EnvVars;
        Nskd.Server.JsonRequest.post('/Order/Api', data, _done, _fail);
    }
    function _done(pack) {
        if (Nskd.Js.is(pack, 'object')) {
            if (pack.err) {
                _fail(pack.data);
            } else {
                done(pack.data);
            }
        } else if (Nskd.Js.is(pack, 'string')) {
            if (pack.length == 0) {
                alert('Nskd.Server.execute: Response type is "string" with zero length.');
            } else {
                alert('Nskd.Server.execute: Response type is "string". ' + pack);
            }
        } else {
            alert('Nskd.Server.execute: Response type is "' + (typeof pack) + '". ' + pack.toString());
        }
    }
    function _fail(msg) {
        alert('Nskd.Server.execute: ' + msg);
    }
};

Nskd.Server.downloadFile = function (id) {
    var body = document.body;
    var guid = Nskd.Js.guid();

    var iframe = $('<iframe name="' + guid + '" style="display: none">').appendTo(body);
    iframe.load(function () {
        body.removeChild(iframe[0]);
        iframe = null;
    });

    var form = $('<form method="post" action="/" target="' + guid + '" enctype="multipart/form-data" style="display: none">').appendTo(body);
    $('<input type="hidden" name="cmd" value="download">').appendTo(form);
    $('<input type="hidden" name="id" value="' + id + '">').appendTo(form);
    form.submit(function () { return false; });

    form[0].submit();
    body.removeChild(form[0]);
    form = null;
};

Nskd.Server.getSessionId = function () {
    if ((typeof (Nskd) !== 'undefined') && (Nskd.Server) && (Nskd.Server.SessionId)) {
        return Nskd.Server.SessionId;
    } else {
        return '00000000-0000-0000-0000-000000000000';
    }
};
