import { summarise } from "../summarise";

const r = summarise([
    "Quick brown fox jumps over",
    "Slow brown foxes jump over",
    "Quick red fox jumps over"
]);

console.log(r);

console.log(r.map(s => s.length == 1 ? s : "...").join(""));