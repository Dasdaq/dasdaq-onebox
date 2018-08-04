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
        qv.put('/api/instance/' + this.name, {
            uploadMethod: self.method,
            data: self.method === 'Zip' ? fileData : gitUrl
        }).then(x => {
            // TODO
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