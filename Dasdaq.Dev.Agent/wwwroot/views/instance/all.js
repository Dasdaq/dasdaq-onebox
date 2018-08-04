component.data = function () {
    return {
        values: []
    };
};

component.created = function () {
    var self = this;
    qv.createView('/api/instance', {}, 30 * 1000).fetch(x => {
        self.values = x.data;
    });
};

component.methods = {
};