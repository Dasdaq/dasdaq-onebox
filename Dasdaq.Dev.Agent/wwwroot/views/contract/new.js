component.data = function () {
    return {
        name: null
    };
};

component.created = function () {
    var self = this;
};

component.methods = {
    upload: function () {
        app.notification("pending", "正在部署智能合约" + self.name + "...");
        qv.put('/api/eos/contract/' + this.name, {
            cpp: $('#code-editor')[0].editor.getValue(),
            hpp: $('#code-editor2')[0].editor.getValue()
        }).then(x => {
            app.notification("succeeded", "智能合约" + self.name + "部署成功");
            app.redirect('/contract');
        })
        .catch(err => {
            app.notification("error", "智能合约" + self.name + "部署失败", err.responseJSON.msg);
        });
    }
};