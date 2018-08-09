component.data = function () {
    return {
        new: {
            name: null,
            amount: "1000000000.0000",
            account: "eosio.token"
        },
        currencies: [],
        accounts: [],
        currencyView: null
    };
};

component.created = function () {
    var self = this;
    self.currencyView = qv.createView('/api/eos/currency', {}, 5 * 1000);
    self.currencyView.fetch(x => {
        self.currencies = x.data;
    });
    qv.createView('/api/eos/account', {}, 5 * 1000).fetch(x => {
        self.accounts = x.data;
    });
};

component.methods = {
    supply: function (currency) {
        var account = $('#currency-' + currency).find('select').val();
        var amount = $('#currency-' + currency).find('input[type="text"]').val();
        app.notification("pending", `正在为${account}发放${amount} ${currency}...`);
        qv.post('/api/eos/currency/' + currency + '/account/' + account, { amount: amount })
            .then(x => {
                app.notification("succeeded", `已经成功为${account}发放${amount} ${currency}`);
                app.redirect('/contract');
            })
            .catch(err => {
                app.notification("error", currency + "发放失败", err.responseJSON.msg);
            });
    },
    new: function () {
        var self = this;
        app.notification("pending", `正在创建${self.new.amount} ${self.new.name}...`);
        qv.put('/api/eos/currency/' + self.new.name, {
            account: self.new.account,
            amount: self.new.amount
        })
            .then(x => {
                self.currencyView.refresh();
                app.notification("succeeded", `${amount} ${currency}已经创建成功`);
                self.new.name = null;
                self.new.amount = "1000000000.0000";
                self.new.account = "eosio.token";
            })
            .catch(err => {
                app.notification("error", `${amount} ${currency}创建失败`, err.responseJSON.msg);
            });
    }
};