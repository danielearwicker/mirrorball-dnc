"use strict";
exports.__esModule = true;
function pushAll(target, source) {
    for (var _i = 0, source_1 = source; _i < source_1.length; _i++) {
        var i = source_1[_i];
        target.push(i);
    }
}
function summarise(strings) {
    if (strings.length == 0) {
        return [];
    }
    if (strings.length == 1) {
        return [strings];
    }
    // Reduce to distinct: sort and remove adjacent if same
    strings = strings.slice().sort();
    for (var n = 1; n < strings.length; n++) {
        if (strings[n] === strings[n - 1]) {
            strings.splice(n - 1, 1);
        }
    }
    // Try substrings, longest first
    var first = strings[0];
    var len = first.length;
    var _loop_1 = function (count) {
        var _loop_2 = function (start) {
            var sub = first.substr(start, count);
            var positions = strings.map(function (s) { return s.indexOf(sub); });
            if (positions.some(function (p) { return p === -1; })) {
                return "continue";
            }
            var result = [];
            if (positions.some(function (p) { return p !== 0; })) {
                pushAll(result, summarise(positions.map(function (p, i) { return strings[i].substr(0, p); })));
            }
            result.push([sub]);
            if (positions.some(function (p, i) { return p + sub.length < strings[i].length; })) {
                pushAll(result, summarise(positions.map(function (p, i) { return strings[i].substr(p + count); })));
            }
            return { value: result };
        };
        for (var start = 0; start <= len - count; start++) {
            var state_1 = _loop_2(start);
            if (typeof state_1 === "object")
                return state_1;
        }
    };
    for (var count = len; count > 0; count--) {
        var state_2 = _loop_1(count);
        if (typeof state_2 === "object")
            return state_2.value;
    }
    // No common substring found
    return [strings];
}
exports.summarise = summarise;
var digits = /^\d+$/;
var alpha = /^\w+$/;
function describeSection(strings) {
    if (strings.every(function (s) { return !s || !!s.match(digits); })) {
        return "[0-9]";
    }
    if (strings.every(function (s) { return !s || !!s.match(alpha); })) {
        return "[a-z]";
    }
    return "[...]";
}
exports.describeSection = describeSection;
function summariseToString(strings) {
    return summarise(strings).map(function (s) { return s.length == 1 ? s : describeSection(s); }).join("");
}
exports.summariseToString = summariseToString;
