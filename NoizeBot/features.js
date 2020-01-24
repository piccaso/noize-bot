//log("channel: " + channel);
//log("message: " + message);
//log(`cmd=${cmd}, args=${args}`);

// Smileys local:
message.match(/:angry:/) && exec('mpg123', './tracks/smileys/angry.mp3');
message.match(/:blush:/) && exec('mpg123', './tracks/smileys/blush.mp3');
message.match(/:disappointed:/) && exec('mpg123', './tracks/smileys/disappointed.mp3');
message.match(/:grimacing:/) && exec('mpg123', './tracks/smileys/grimacing.mp3');
message.match(/:grin:/) && exec('mpg123', './tracks/smileys/grin.mp3');
message.match(/:grinning:/) && exec('mpg123', './tracks/smileys/grinning.mp3');
message.match(/:hankey:|:poop:|:shit:/) && exec('mpg123', './tracks/smileys/hankey.mp3');
message.match(/:open_mouth:/) && exec('mpg123', './tracks/smileys/open_mouth.mp3');
message.match(/:rage:/) && exec('mpg123', './tracks/smileys/rage.mp3');
message.match(/:smile:|:simple_smile:|':slightly_smiling_face:/) && exec('mpg123', './tracks/smileys/smile.mp3');
message.match(/:stuck_out_tongue:/) && exec('mpg123', './tracks/smileys/stuck_out_tongue.mp3');
message.match(/:stuck_out_tongue_winking_eye:/) && exec('mpg123', './tracks/smileys/stuck_out_tongue_winking_eye.mp3');
message.match(/:sunglasses:/) && exec('mpg123', './tracks/smileys/sunglasses.mp3');
message.match(/:tada:/) && exec('mpg123', './tracks/smileys/tada.mp3');
message.match(/:thumbsdown:/) && exec('mpg123', './tracks/smileys/thumbsdown.mp3');
message.match(/:thumbsup:/) && exec('mpg123', './tracks/smileys/thumbsup.mp3');
message.match(/:wink:/) && exec('mpg123', './tracks/smileys/wink.mp3');
message.match(/faster/i) && exec('emg123', './tracks/faster.mp3');
message.match(/gaudi/i) && exec('emg123', './tracks/gaudi.mp3');
message.match(/:birthday:|geburtstag|birthday/i) && exec('emg123', './tracks/happyBirthday.mp3');
message.match(/stress/i) && exec('emg123', './tracks/stress.mp3');
message.match(/party/i) && exec('emg123', './tracks/waynesworld.mp3');
message.match(/aaa/i) && exec('emg123', './tracks/wilhelmScream.mp3');

// Smileys Remote:
message.match(/hare|krishna|pray|:innocent:/i) && exec('mpg123', 'http://dl.prokerala.com/downloads/ringtones/files/mp3/hare-krishna-48971.mp3');

// Commands
message.match(/^say /) && exec("espeak", args);

var durchfall = message.match(/^durchfall ([0-9]+)/i);
if (durchfall) {
    for (var i = 0; i < durchfall[1]; i++) {
        exec('mpg123', './tracks/smileys/hankey.mp3');
    }
}
