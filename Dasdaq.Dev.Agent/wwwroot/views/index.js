﻿app = new Vue({
    router: router,
    data: {
        control: {
            currentNotification: null,
            notifications: [],
            notificationLock: false,
        }
    },
    created: function () {
    },
    watch: {
    },
    methods: {
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