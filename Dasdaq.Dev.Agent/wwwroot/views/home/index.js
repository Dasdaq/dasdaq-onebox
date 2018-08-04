component.data = function () {
    return {
        status: null
    };
};

component.created = function () {
    var self = this;
    qv.createView('/api/eos/status', {}, 10 * 1000)
        .fetch(x => {
            self.status = x.data;
        });
};

component.methods = {
    startEos: function () {
        app.notification("pending", "正在启动EOS测试链...");
        qv.post('/api/eos/init', {})
            .then(x => {
                app.notification("succeeded", "EOS测试链已经启动成功");
            })
            .catch(err => {
                app.notification("error", "测试链已经启动失败", err.responseJSON.msg);
            });
    },
    stopOneBox: function () {
        app.notification("pending", "正在停止OneBox...");
        qv.post('/api/onebox/stop', {})
            .then(x => {
                app.notification("succeeded", "OneBox环境已停止");
                window.close();
            })
            .catch(err => {
                app.notification("error", "OneBox停止失败", err.responseJSON.msg);
            });
    }
};