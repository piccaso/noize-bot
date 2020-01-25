function match(regex, par) {
    var m = message.match(regex);
    if (m && par) {
        if (typeof par === 'function') par(m);
        if (typeof par === 'string') run('mpg123', [par]);
    };
    return m;
}

var registeredEntries = [];
function register(regex, mp3) {
    registeredEntries.push({ regex, mp3 });
}

function processMatches() {
    if (verbose) {
        log(`message=${message}`);
    }
    var queue = [];
    var parts = message.split(/[\s]+/);
    if (parts.length < 1 | registeredEntries.length < 1) return;
    for (var p = 0; p < parts.length; p++) {
        var part = parts[p];
        for (var r = 0; r < registeredEntries.length; r++) {
            var reg = registeredEntries[r];
            if (part.match(reg.regex)) queue.push(reg.mp3);
        }
    }
    if (queue.length > 0) run("mpg123", queue);
}