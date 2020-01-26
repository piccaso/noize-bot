#!/bin/bash
tmp_dir=$(mktemp -d -t yt-XXXXXXXXXX)
cd $tmp_dir
youtube-dl -x --audio-format wav "$@"
aplay *.wav
cd -
rm -rf $tmp_dir

