component.data = function () {
    return {
        active: 'contractList',
        name: null,
        contracts: [],
        views: {
            contract: null
        }
    };
};

component.created = function () {
    var self = this;
    app.control.title = '智能合约';
    app.control.nav = [{ text: '智能合约', to: '/eos/contract' }];
    self.views.contract = qv.createView('/api/eos/contract', {}, 5 * 1000);
    self.views.contract
        .fetch(x => {
            self.contracts = x.data;
        });
};

component.methods = {
    edit: function (contract) {
        this.name = contract.name;
        $('#code-editor')[0].editor.setValue(contract.cpp || '');
        $('#code-editor2')[0].editor.setValue(contract.hpp || '');
        $('#code-editor3')[0].editor.setValue(contract.abi || '');
        this.openTab('upload');
    },
    openTab: function (tab) {
        if (this.active === tab) {
            return;
        }

        $('#' + this.active).slideUp();

        this.active = tab;
        $('#' + tab).slideDown();
    },
    upload: function () {
        var self = this;
        app.notification("pending", "正在部署智能合约" + self.name + "...");
        qv.put('/api/eos/contract/' + this.name, {
            cpp: $('#code-editor')[0].editor.getValue(),
            hpp: $('#code-editor2')[0].editor.getValue(),
            abi: $('#code-editor3')[0].editor.getValue()
        })
            .then(x => {
                app.notification("succeeded", "智能合约" + self.name + "部署成功");
                self.views.contract.refresh();
                self.openTab('contractList');
            })
            .catch(err => {
                app.notification("error", "智能合约" + self.name + "部署失败", err.responseJSON.msg);
            });
    }
};