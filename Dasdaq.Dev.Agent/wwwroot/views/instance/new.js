component.data = function () {
    return {
        name: null,
        method: 'Zip',
        gitUrl: null,
        fileData: null
    };
};

component.created = function () {
    var self = this;
};

component.methods = {
    upload: function () {
        var self = this;
        app.notification("pending", "正在部署实例" + self.name + "...");
        qv.put('/api/instance/' + this.name, {
            method: self.method,
            data: self.method === 'Zip' ? self.fileData : self.gitUrl
        })
        .then(x => {
            app.notification("succeeded", "实例" + self.name + "部署成功，正在启动...");
            qv.createView('/api/instance', {}, 5 * 1000).refresh();
            app.redirect('/instance');
        })
        .catch(err => {
            app.notification("error", "实例" + self.name + "部署失败", err.responseJSON.msg);
        });
    },
    selectFile: function () {
        var self = this;
        $('#file')
            .unbind()
            .change(function (e) {
                var file = $('#file')[0].files[0];
                var reader = new FileReader();
                reader.onload = function (e) {
                    self.fileData = e.target.result;
                };
                reader.readAsDataURL(file);
            });
        $('#file').click();
    }
};