LazyRouting.SetRoute({
    '/home': null,
    '/contract/all': null,
    '/contract/new': null,
    '/instance/all': null,
    '/instance/new': null,
    '/instance/:id/edit': { id: '[a-zA-Z0-9-_]{4,128}' },
    '/config': null,
    '/currency': null,
    '/404': null
});

LazyRouting.SetMirror({
    '/': '/home',
    '/contract': '/contract/all',
    '/instance': '/instance/all'
});