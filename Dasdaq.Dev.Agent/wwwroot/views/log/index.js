component.data = function () {
    return {
        id: null,
        logs: []
    };
};

component.created = function () {
    app.control.title = '日志流';
    app.control.nav = [{ text: '日志流' }];
    this.id = router.history.current.params.id;
    qv.get('/api/log/' + this.id, {})
        .then((x) => {
            this.logs = x.data;
            setTimeout(() => {
                $('[data-log-stream]').scrollTop($('[data-log-stream]')[0].scrollHeight);
            }, 100);
        });
};

component.methods = {
};