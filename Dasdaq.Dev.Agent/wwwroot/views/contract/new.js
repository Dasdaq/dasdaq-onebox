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
        qv.put('/api/eos/contract/' + this.name, {
            cpp: $('#code-editor')[0].editor.getValue(),
            hpp: $('#code-editor2')[0].editor.getValue()
        }).then(x => {
            // TODO
        });
    }
};