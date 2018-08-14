app = new Vue({
    router: router,
    data: {
        control: {
            currentNotification: null,
            notifications: [],
            notificationLock: false,
            nav: [],
            title: ''
        },
        signalr: null,
        pause: false
    },
    created: function () {
        var self = this;
        self.signalr = new signalR.HubConnectionBuilder()
            .configureLogging(signalR.LogLevel.Error)
            .withUrl('/signalr/agent', {})
            .withHubProtocol(new signalR.JsonHubProtocol())
            .build();
        self.signalr.on('onLogReceived', (id, isError, text) => {
            if (!self.pause) {
                if ($('[data-log-stream="' + id + '"]').length > 0) {
                    $('[data-log-stream="' + id + '"]').append("\r\n" + text);
                    setTimeout(() => {
                        $('[data-log-stream]').scrollTop($('[data-log-stream]')[0].scrollHeight);
                    }, 100);
                }
            }
        });
        self.signalr.start();
    },
    watch: {
    },
    methods: {
        tryRedirect: function (to) {
            if (to) {
                app.redirect(to);
            }
        },
        resolveUrl: function (to) {
            if (typeof to === 'string')
                return to;
            if (to.name && !to.path)
                return to.name;
            if (!to.query)
                return to.path;
            var baseUrl = to.path + (to.path.indexOf('?') >= 0 ? '&' : '?');
            var args = [];
            for (var x in to.query) {
                args.push(x + '=' + encodeURIComponent(to.query[x]));
            }
            return baseUrl += args.join('&');
        },
        redirect: function (name, path, params, query) {
            if (name && !path)
                path = name;
            LazyRouting.RedirectTo(name, path, params, query);
        },
        notification: function (level, title, detail, button) {
            var item = { level: level, title: title, detail: detail };
            if (level === 'important') {
                item.button = button;
            }
            this.control.notifications.push(item);
            if (this.control.currentNotification && this.control.currentNotification.level === 'pending') {
                this.control.notificationLock = false;
            }
            this._showNotification(level === 'important' ? true : false);
        },
        clickNotification: function () {
            this._releaseNotification();
        },
        viewLogStream: function (id) {
            this.redirect('/log/:id', '/log/' + id, { id: id });
        },
        stop: function () {
            var self = this;
            if (confirm("您确定要关闭OneBox吗？")) {
                self.notification("pending", "正在停止OneBox...");
                qv.post('/api/onebox/stop', {})
                    .then(x => {
                        self.notification("succeeded", "OneBox环境已停止");
                        window.close();
                    })
                    .catch(err => {
                        self.notification("error", "OneBox停止失败", err.responseJSON.msg);
                    });
            }
        },
        _showNotification: function (manualRelease) {
            var self = this;
            if (!this.control.notificationLock && this.control.notifications.length) {
                this.control.notificationLock = true;
                var notification = this.control.notifications[0];
                this.control.notifications = this.control.notifications.slice(1);
                this.control.currentNotification = notification;
                if (!manualRelease) {
                    setTimeout(function () {
                        self._releaseNotification();
                    }, 5000);
                }
            }
        },
        _releaseNotification: function () {
            var self = this;
            self.control.currentNotification = null;
            setTimeout(function () {
                self.control.notificationLock = false;
                if (self.control.notifications.length) {
                    self._showNotification();
                }
            }, 250);
        },
    },
    computed: {
    }
});