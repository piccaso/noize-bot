#!/bin/bash
tmp_dir=$(mktemp -d -t yt-XXXXXXXXXX)
cd $tmp_dir
youtube-dl -x --audio-format mp3 "$@"
mpg123 *.mp3
cd -
rm -rf $tmp_dir

