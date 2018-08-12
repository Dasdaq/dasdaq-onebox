component.data = function () {
    return {
        name: null,
        gitUrl: null
    };
};

component.created = function () {
    var self = this;
    app.control.title = '通过Git安装Dapp';
    app.control.nav = [{ text: '通过Git安装Dapp', to: '/dapp/git' }];
};

component.methods = {
    upload: function () {
        var self = this;
        app.notification("pending", "正在部署实例" + self.name + "...");
        qv.put('/api/dapp/' + this.name, {
            method: 'Git',
            data: self.gitUrl
        })
            .then(x => {
                app.notification("succeeded", "实例" + self.name + "部署成功，正在启动...");
                qv.createView('/api/dapp', {}, 5 * 1000).refresh();
                app.redirect('/dapp');
            })
            .catch(err => {
                app.notification("error", "实例" + self.name + "部署失败", err.responseJSON.msg);
            });
    },
};