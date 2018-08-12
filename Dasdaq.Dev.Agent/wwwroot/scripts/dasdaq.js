$(document).bind('DOMNodeInserted', function (e) {
    var dom = $(e.target);

    if (dom.find('#code-editor').length && !dom.find('#code-editor')[0].editor) {
        var editor = ace.edit("code-editor");
        dom.find('#code-editor')[0].editor = editor;
        editor.setTheme("ace/theme/twilight");
        editor.session.setMode('ace/mode/c_cpp');
        editor.setOptions({
            enableBasicAutocompletion: true,
            enableSnippets: true
        });
    }

    if (dom.find('#code-editor2').length && !dom.find('#code-editor2')[0].editor) {
        var editor = ace.edit("code-editor2");
        dom.find('#code-editor2')[0].editor = editor;
        editor.setTheme("ace/theme/twilight");
        editor.session.setMode('ace/mode/c_cpp');
        editor.setOptions({
            enableBasicAutocompletion: true,
            enableSnippets: true
        });
    }
});

function clone(x) {
    var json = JSON.stringify(x);
    return JSON.parse(json);
}