component.data = function () {
    return {
        value: null
    };
};

component.watch = {
};

component.created = function () {
    var self = this;
    qv.createView('/api/onebox/config', {}, 60 * 1000).fetch(x => {
        self.value = JSON.stringify(x.data, null, 2);
        $('#code-editor')[0].editor.setValue(this.value);
    });
};

component.watch = {
};

component.methods = {
    save: function () {
        var jsonString = $('#code-editor')[0].editor.getValue();
        qv.patch('/api/onebox/config', JSON.parse(jsonString))
            .then(x => {
                alert("OneBox配置信息保存成功");
            });
    }
};