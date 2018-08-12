component.data = function () {
    return {
        dapp: [],
        views: {
            dapp: null
        }
    };
};

component.created = function () {
    var self = this;
    app.control.title = 'Dapp列表';
    app.control.nav = [{ text: 'Dapp列表', to: '/dapp' }];
    self.views.dapp = qv.createView('/api/dapp', {}, 10 * 1000)
        .fetch(x => {
            self.dapp = x.data;
        });
};

component.methods = {
    kill: function (id) {
        app.notification("pending", "正在结束实例" + id + "...");
        qv.delete('/api/instance/' + id, {})
            .then(() => {
                app.notification("succeeded", "实例" + id + "结束成功");
            })
            .catch(err => {
                app.notification("error", "实例" + self.name + "结束失败", err.responseJSON.msg);
            });
    }
};