LazyRouting.SetRoute({
    '/eos': null,
    '/eos/contract': null,
    '/eos/account': null,
    '/eos/currency': null,
    '/dapp': null,
    '/dapp/git': null,
    '/dapp/zip': null,
    '/log/:id': { id: '[a-zA-Z0-9]{8}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{4}-[a-zA-Z0-9]{12}' },
    '/404': null
});

LazyRouting.SetMirror({
    '/': '/eos',
    '/contract': '/contract/all',
    '/instance': '/instance/all'
});