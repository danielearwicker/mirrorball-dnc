function pushAll<T>(target: T[], source: T[]) {
    for (const i of source) {
        target.push(i);
    }
}

export function summarise(strings: string[]) {

    if (strings.length == 0) {
        return [];
    }

    if (strings.length == 1) {
        return [strings];
    }

    // Reduce to distinct: sort and remove adjacent if same
    strings = strings.slice().sort();
    for (let n = 1; n < strings.length; n++) {
        if (strings[n] === strings[n - 1]) {
            strings.splice(n - 1, 1);
        }
    }

    // Try substrings, longest first
    const first = strings[0];
    const len = first.length;
    for (let count = len; count > 0; count--) {
        for (let start = 0; start <= len - count; start++) {
            const sub = first.substr(start, count);

            const positions = strings.map(s => s.indexOf(sub));
            if (positions.some(p => p === -1)) {
                // Substring is not common to all
                continue;
            }

            const result: string[][] = [];

            if (positions.some(p => p !== 0)) {
                pushAll(result, summarise(positions.map((p, i) => strings[i].substr(0, p))));
            }

            result.push([sub]);
            
            if (positions.some((p, i) => p + sub.length < strings[i].length)) {
                pushAll(result, summarise(positions.map((p, i) => strings[i].substr(p + count))));
            }

            return result;
        }
    }

    // No common substring found
    return [strings];
}
