//log("channel: " + channel);
//log("message: " + message);
//log(`cmd=${cmd}, args=${args}`);

// Smileys
match(/:angry:/, './tracks/smileys/angry.mp3');
match(/:blush:/, './tracks/smileys/blush.mp3');
match(/:disappointed:/, './tracks/smileys/disappointed.mp3');
match(/:grimacing:/, './tracks/smileys/grimacing.mp3');
match(/:grin:/, './tracks/smileys/grin.mp3');
match(/:grinning:/, './tracks/smileys/grinning.mp3');
match(/:hankey:|:poop:|:shit:/, './tracks/smileys/hankey.mp3');
match(/:open_mouth:/, './tracks/smileys/open_mouth.mp3');
match(/:rage:/, './tracks/smileys/rage.mp3');
match(/:smile:|:simple_smile:|':slightly_smiling_face:/, './tracks/smileys/smile.mp3');
match(/:stuck_out_tongue:/, './tracks/smileys/stuck_out_tongue.mp3');
match(/:stuck_out_tongue_winking_eye:/, './tracks/smileys/stuck_out_tongue_winking_eye.mp3');
match(/:sunglasses:/, './tracks/smileys/sunglasses.mp3');
match(/:tada:/, './tracks/smileys/tada.mp3');
match(/:thumbsdown:/, './tracks/smileys/thumbsdown.mp3');
match(/:thumbsup:/, './tracks/smileys/thumbsup.mp3');
match(/:wink:/, './tracks/smileys/wink.mp3');
match(/faster/i, './tracks/faster.mp3');
match(/gaudi/i, './tracks/gaudi.mp3');
match(/:birthday:|geburtstag|birthday/i, './tracks/happyBirthday.mp3');
match(/stress/i, './tracks/stress.mp3');
match(/party/i, './tracks/waynesworld.mp3');
match(/aaa/i, './tracks/wilhelmScream.mp3');
match(/hare|krishna|Kṛṣṇa|Krsna|pray|:innocent:/i, 'tracks/hareKrishna.mp3');
match(/hardcore/i, 'tracks/hardcore.mp3');
match(/egal|:relaxed:/i, 'tracks/egal.mp3');
match(/:middle_finger:/i, 'tracks/fea.mp3');
match(/bathroom|toilet/i, 'tracks/bathroom.mp3');
match(/balls|nukem/i, 'tracks/ballsOfSteel.mp3');
match(/fake news/i, 'tracks/fakeNews.mp3');
match(/get back to work|slacker/i, 'tracks/slacker.mp3');
match(/Schlafn|Schlofn|Hackln/i, 'tracks/hackln.mp3');


// Commands (wip)
match(/^nb_say (.*)$/, m => run("/bin/bash", ['-c', `echo "${m[1].replace('"', '\\"')}" | festival --tts`]));
match(/^nb_play_url (http[^ ]+)$/, m => run('/bin/bash', ['-c', `curl "${m[1].replace('"','\\"')}" | mpg123 -`]));
match(/^nb_fart_attack ([1-9]{1})/, m => {
    var playlist = [];
    for (var i = 0; i < m[1]; i++) {
        playlist.push('./tracks/smileys/hankey.mp3');
    }
    playlist.push('./tracks/smileys/smile.mp3');
    run("mpg123", playlist);
});