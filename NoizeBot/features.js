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
message.match(/:hankey:|:poop:/) && exec('mpg123', './tracks/smileys/hankey.mp3');
message.match(/:open_mouth:/) && exec('mpg123', './tracks/smileys/open_mouth.mp3');
message.match(/:rage:/) && exec('mpg123', './tracks/smileys/rage.mp3');
message.match(/:smile:/) && exec('mpg123', './tracks/smileys/smile.mp3');
message.match(/:stuck_out_tongue:/) && exec('mpg123', './tracks/smileys/stuck_out_tongue.mp3');
message.match(/:stuck_out_tongue_winking_eye:/) && exec('mpg123', './tracks/smileys/stuck_out_tongue_winking_eye.mp3');
message.match(/:sunglasses:/) && exec('mpg123', './tracks/smileys/sunglasses.mp3');
message.match(/:tada:/) && exec('mpg123', './tracks/smileys/tada.mp3');
message.match(/:thumbsdown:/) && exec('mpg123', './tracks/smileys/thumbsdown.mp3');
message.match(/:thumbsup:/) && exec('mpg123', './tracks/smileys/thumbsup.mp3');
message.match(/:wink:/) && exec('mpg123', './tracks/smileys/wink.mp3');

// Smiles Remote:
message.match(/hare|krishna|pray|:innocent:/i) && exec('mpg123', 'http://dl.prokerala.com/downloads/ringtones/files/mp3/hare-krishna-48971.mp3');

// Commands
message.match(/^say /) && exec("espeak", args);
