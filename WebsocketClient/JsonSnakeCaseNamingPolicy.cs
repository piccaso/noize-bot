﻿using System;
using System.Text;
using System.Text.Json;

namespace WebsocketClient {

    //Credit: https://github.com/J0rgeSerran0/JsonNamingPolicy

    public class JsonSnakeCaseNamingPolicy : JsonNamingPolicy {
        private const string Separator = "_";

        public override string ConvertName(string name) {
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name)) return string.Empty;

            ReadOnlySpan<char> spanName = name.Trim();

            var stringBuilder = new StringBuilder();
            var addCharacter = true;

            var isNextLower = false;
            var isNextUpper = false;
            var isNextSpace = false;

            for (var position = 0; position < spanName.Length; position++) {
                if (position != 0) {
                    var isCurrentSpace = spanName[position] == 32;
                    var isPreviousSpace = spanName[position - 1] == 32;
                    var isPreviousSeparator = spanName[position - 1] == 95;

                    if (position + 1 != spanName.Length) {
                        isNextLower = spanName[position + 1] > 96 && spanName[position + 1] < 123;
                        isNextUpper = spanName[position + 1] > 64 && spanName[position + 1] < 91;
                        isNextSpace = spanName[position + 1] == 32;
                    }

                    if (isCurrentSpace &&
                        (isPreviousSpace ||
                         isPreviousSeparator ||
                         isNextUpper ||
                         isNextSpace)) {
                        addCharacter = false;
                    }
                    else {
                        var isCurrentUpper = spanName[position] > 64 && spanName[position] < 91;
                        var isPreviousLower = spanName[position - 1] > 96 && spanName[position - 1] < 123;
                        var isPreviousNumber = spanName[position - 1] > 47 && spanName[position - 1] < 58;

                        if (isCurrentUpper &&
                            (isPreviousLower ||
                             isPreviousNumber ||
                             isNextLower ||
                             isNextSpace)) {
                            stringBuilder.Append(Separator);
                        }
                        else {
                            if (isCurrentSpace) {
                                stringBuilder.Append(Separator);
                                addCharacter = false;
                            }
                        }
                    }
                }

                if (addCharacter)
                    stringBuilder.Append(spanName[position]);
                else
                    addCharacter = true;
            }

            return stringBuilder.ToString().ToLower();
        }
    }
}