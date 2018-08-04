component.data = function () {
    return {
    };
};

component.watch = {
};

component.created = function () {
};

component.watch = {
};

component.methods = {
    startEos: function () {
        qv.post('/api/onebox/start', {})
            .then(x => {
                //TODO:
            });
    },
    stopOneBox: function () {
        qv.post('/api/onebox/stop', {})
            .then(x => {
                window.close();
            });
    }
};