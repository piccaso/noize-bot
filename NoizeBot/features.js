﻿// Smileys
register(/:angry:/, './tracks/smileys/angry.mp3');
register(/:blush:/, './tracks/smileys/blush.mp3');
register(/:disappointed:/, './tracks/smileys/disappointed.mp3');
register(/:grimacing:/, './tracks/smileys/grimacing.mp3');
register(/:grin:/, './tracks/smileys/grin.mp3');
register(/:grinning:/, './tracks/smileys/grinning.mp3');
register(/:hankey:|:poop:|:shit:/, './tracks/smileys/hankey.mp3');
register(/:open_mouth:/, './tracks/smileys/open_mouth.mp3');
register(/:rage:/, './tracks/smileys/rage.mp3');
register(/:smile:|:simple_smile:|':slightly_smiling_face:/, './tracks/smileys/smile.mp3');
register(/:stuck_out_tongue:/, './tracks/smileys/stuck_out_tongue.mp3');
register(/:stuck_out_tongue_winking_eye:/, './tracks/smileys/stuck_out_tongue_winking_eye.mp3');
register(/:sunglasses:/, './tracks/smileys/sunglasses.mp3');
register(/:tada:/, './tracks/smileys/tada.mp3');
register(/:thumbsdown:/, './tracks/smileys/thumbsdown.mp3');
register(/:thumbsup:/, './tracks/smileys/thumbsup.mp3');
register(/:wink:/, './tracks/smileys/wink.mp3');
register(/faster/i, './tracks/faster.mp3');
register(/gaudi/i, './tracks/gaudi.mp3');
register(/:birthday:|geburtstag|birthday/i, './tracks/happyBirthday.mp3');
register(/stress/i, './tracks/stress.mp3');
register(/party/i, './tracks/waynesworld.mp3');
register(/aaa|argh|javascript/i, './tracks/wilhelmScream.mp3');
register(/hare|krishna|Kṛṣṇa|Krsna|pray|:innocent:/i, 'tracks/hareKrishna.mp3');
register(/hardcore/i, 'tracks/hardcore.mp3');
register(/egal|:relaxed:/i, 'tracks/egal.mp3');
register(/:middle_finger:/i, 'tracks/fea.mp3');
register(/bathroom|toilet|klo|badezimmer/i, 'tracks/bathroom.mp3');
register(/balls|nukem/i, 'tracks/ballsOfSteel.mp3');
register(/fake/i, 'tracks/fakeNews.mp3');
register(/slacker/i, 'tracks/slacker.mp3');
register(/Schlafn|Schlofn|Hackln/i, 'tracks/hackln.mp3');
register(/ahmen|:church:/i, 'tracks/soSayWeAll.mp3');
register(/facebook|fb\.com/i, 'tracks/facebook.mp3');
register(/windows/i, 'tracks/windows.mp3');
register(/ibiza|fpö|fpoe|strache/i, 'tracks/ibiza.mp3');
register(/cloud|azure|aws|geklaut|diebstahl|hinterziehung/i, 'tracks/cloud.mp3');
register(/kaelte|kälte|winter/i, 'tracks/kaelte.mp3');
register(/hoschi/i, 'tracks/hoschi.mp3');
register(/engage/i, 'tracks/engage.mp3');


// Commands (wip)
match(/so say we all/i, 'tracks/soSayWeAll.mp3');
match(/^nb_say (.*)$/, m => tts(m[1]));
match(/^nb_play_url (http[^ ]+)$/, m => playUrl(m[1]));