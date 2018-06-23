"use strict";
exports.__esModule = true;
var summarise_1 = require("../summarise");
var r = summarise_1.summarise([
    "Quick brown fox jumps over",
    "Slow brown foxes jump over",
    "Quick red fox jumps over"
]);
console.log(r);
console.log(r.map(function (s) { return s.length == 1 ? s : "..."; }).join(""));
