component.data = function () {
    return {
        status: null,
        chainId: null,
        active: 'nodeControl',
        plugins: [],
        logStreamId: null,
        views: {
            config: null,
            status: null
        },
        addPluginName: null,
        keys: {
            public: null,
            private: null
        }
    };
};

component.created = function () {
    var self = this;
    app.control.title = '测试链节点';
    app.control.nav = [{ text: '测试链节点', to: '/' }];
    self.views.status = qv.createView('/api/eos/status', {}, 10 * 1000)
        .fetch(x => {
            self.status = x.data.status;
            self.chainId = x.data.chainId;
            self.logStreamId = x.data.logStreamId;
        });
    self.views.config = qv.createView('/api/onebox/config', {})
        .fetch(x => {
            self.plugins = x.data.eos.plugins;
            self.keys.public = x.data.eos.keyPair.publicKey;
            self.keys.private = x.data.eos.keyPair.privateKey;
        });
};

component.methods = {
    addPlugin: function () {
        var self = this;
        var plugin = addPluginName;
        self.plugins.push(plugin);
        addPluginName = null;
        app.notification("pending", `正在添加插件${plugin}...`);
        qv.patch('/api/onebox/config/plugins', self.plugins)
            .then(() => {
                app.notification("succeeded", `插件${plugin}添加成功`);
                self.views.config.refresh();
            })
            .catch(err => {
                app.notification("error", "插件添加失败", err.responseJSON.msg);
            });
    },
    removePlugin: function (x) {
        var self = this;
        var index = self.plugins.indexOf(x);
        self.plugins.splice(index, 1);
        app.notification("pending", `正在删除插件${x}...`);
        qv.patch('/api/onebox/config/plugins', self.plugins)
            .then(() => {
                app.notification("succeeded", `插件${x}删除成功`);
                self.views.config.refresh();
            })
            .catch(err => {
                app.notification("error", "插件删除失败", err.responseJSON.msg);
            });
    },
    openTab: function (tab) {
        if (this.active === tab) {
            return;
        }

        $('#' + this.active).slideUp();

        this.active = tab;
        $('#' + tab).slideDown();
    },
    launch: function (safe) {
        app.notification("pending", "正在启动EOS测试链...");
        qv.post('/api/eos/init?safeMode=' + (safe ? 'true' : 'false'), {})
            .then(x => {
                app.notification("succeeded", "EOS测试链已经启动成功");
                self.views.status.refresh();
            })
            .catch(err => {
                app.notification("error", "测试链已经启动失败", err.responseJSON.msg);
            });
    },
    stop: function (safe) {
        app.notification("pending", "正在停止EOS测试链...");
        qv.post('/api/eos/stop?safeMode=' + (safe ? 'true' : 'false'), {})
            .then(x => {
                app.notification("succeeded", "EOS测试链已经停止成功");
                self.views.status.refresh();
            })
            .catch(err => {
                app.notification("error", "测试链停止失败", err.responseJSON.msg);
            });
    }
};