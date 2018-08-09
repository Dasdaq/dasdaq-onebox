component.data = function () {
    return {
        values: []
    };
};

component.created = function () {
    var self = this;
    qv.createView('/api/instance', {}, 5 * 1000).fetch(x => {
        self.values = x.data;
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