component.data = function () {
    return {
        account: [],
        views: {
            account: null
        },
        addAccount: null
    };
};

component.created = function () {
    var self = this;
    app.control.title = '钱包账号';
    app.control.nav = [{ text: '钱包账号', to: '/eos/account' }];
    self.views.account = qv.createView('/api/eos/account', {}, 10 * 1000)
        .fetch(x => {
            self.account = x.data;
        });
};

component.methods = {
    addAccount: function () {
        var self = this;
        app.notification("pending", "正在创建账号" + self.addAccount + "...");
        qv.put('/api/eos/account/' + self.addAccount, {})
            .then(() => {
                app.notification("succeeded", "账号" + self.addAccount + "创建成功");
                self.addAccount = null;
                self.views.account.refresh();
            })
            .catch(err => {
                app.notification("error", "账号" + self.addAccount + "创建失败", err.responseJSON.msg);
            });
    }
};