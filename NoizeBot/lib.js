function match(regex, par) {
    var m = message.match(regex);
    if (m && par) {
        if (typeof par === 'function') par(m);
        if (typeof par === 'string') run('mpg123', [par]);
    };
    return m;
}