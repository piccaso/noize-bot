// Smileys - regex must match a single word
register(/:angry:/, './tracks/smileys/angry.mp3');
register(/:blush:/, './tracks/smileys/blush.mp3');
register(/:disappointed:/, './tracks/smileys/disappointed.mp3');
register(/:grimacing:/, './tracks/smileys/grimacing.mp3');
register(/:grin:/, './tracks/smileys/grin.mp3');
register(/:grinning:/, './tracks/smileys/grinning.mp3');
register(/:hankey:|:poop:|:shit:/, './tracks/smileys/hankey.mp3');
register(/:open_mouth:/, './tracks/smileys/open_mouth.mp3');
register(/:rage:/, './tracks/smileys/rage.mp3');
register(/:smile:|:simple_smile:|:slightly_smiling_face:/, './tracks/smileys/smile.mp3');
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
register(/ahmen|chant|:church:/i, 'tracks/soSayWeAll.mp3');
register(/facebook|fb\.com/i, 'tracks/facebook.mp3');
register(/windows/i, 'tracks/windows.mp3');
register(/ibiza|fpö|fpoe|strache/i, 'tracks/ibiza.mp3');
register(/cloud|azure|aws|geklaut|diebstahl|hinterziehung/i, 'tracks/cloud.mp3');
register(/kaelte|kälte|winter|:speaker:|:loud_sound:|:mega:|:loudspeaker:/i, 'tracks/kaelte.mp3');
register(/hoschi/i, 'tracks/hoschi.mp3');
register(/engage/i, 'tracks/engage.mp3');
register(/limit/i, 'tracks/limit.mp3');
register(/space/i, 'tracks/space.mp3');
register(/money/i, 'tracks/money.mp3');
register(/try/i, 'tracks/try.mp3');
register(/yoda/i, 'tracks/yoda.mp3');
register(/:penguin:/i, 'tracks/linus.mp3');
register(/:doughnut:|homer|^doh$/i, 'tracks/doh.mp3');
register(/shorts/i, 'tracks/shorts.mp3');
register(/:alien:/i, 'tracks/alien.mp3');
register(/:metal:/i, 'tracks/metal.mp3');
register(/:broken_heart:/i, 'tracks/glass.mp3');
register(/running/i, 'tracks/running.mp3');
register(/^run(|!)$/i, 'tracks/run.mp3');
register(/^help(|!)|:hospital:$/i, 'tracks/help.mp3');

// Commands - regex must match a full message
match(/do+cksa+l/i, () => reply(':metal:'));
match(/^nb_say ([\s\S]*)$/i, m => tts(m[1]));
match(/^google_say ([\s\S]*)$/i, m => googleTts(m[1], "en"));
match(/^google_(sag|sprich) ([\s\S]*)$/i, m => googleTts(m[2], "de"));
match(/^google_tts_([a-z\-]{2,6}) ([\s\S]*)/i, m => googleTts(m[2], m[1]));
match(/^nb_play_url (http[^ ]+)$/i, m => playUrl(m[1]));
match(/^nb_status/i, () => reply("```json\n" + getStatusJson() + "\n```"));
match(/^nb_yt (http[^ ]+)$/i, m => run('play-youtube', [m[1]]));
match(/^nb_help/i, m => reply(`
|Command                      |Description                                                          |
|-----------------------------|---------------------------------------------------------------------|
|\`nb_say <text>\`            | say something                                                       |
|\`nb_play_url <url>\`        | download and play <url>                                             |
|\`nb_yt <url>\`              | download and play youtube video from <url>                          |
|\`nb_status\`                | show status                                                         |
|\`nb_krp\`                   | try to kill the running background process                          |
|\`nb_kill\`                  | try to commit suicide                                               |
|\`google_say <text>\`        | say something using google translate                                |
|\`google_sag <text>\`        | auf deutsch                                                         |
|\`google_tts_<lang> <text>\` | any (supported) language, like: \`google_tts_en-uk it's tea time!\` |
`));
match(/^nb_list/i, m => {
    var list = "|Regex|Sound|\n|---|---|\n";
    for (var r = 0; r < registeredEntries.length; r++) {
        var reg = registeredEntries[r];
        list += `|\`${reg.regex.toString().replace(/\|/g,'\\|')}\`|${reg.mp3}|\n`;
    }
    reply(list);
});
match(/^nb_sleep ([^ ]+)$/i, m => {
    var r = run('sleep', [m[1]]);
    reply(`done, r=${r}`);
});
