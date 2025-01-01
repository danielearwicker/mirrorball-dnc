import { useState, useEffect, useRef, useCallback, useMemo } from "react";
import { summariseToString } from "../summarise";
import { IssueGroup, IssueGroupProps } from "./IssueGroup";
import { DelogoMode } from "./DelogoMode";
import { IssueInfo, IssueState } from "./Issue";

export function App() {

    const quit = useRef(false);

    const [issues, setIssues] = useState<IssueInfo[]>([]);
    const [search, setSearch] = useState("");
    const [delogoPath, setDelogoPath] = useState("");

    const fetchIssues = useCallback(() => {
        if (quit.current) {
            return;
        }
        
        fetch("api/mirror/issues")
            .then(r => r.json())
            .then((issues: IssueInfo[]) => setIssues(issues))
            .catch(err => console.error(err))
            .then(() => {
                setTimeout(() => fetchIssues(), 1000);
            });
    }, []);

    useEffect(() => { 
        fetchIssues(); 
        return () => { quit.current = true; };
    }, []);

    const refresh = useCallback(() => {
        fetch("api/mirror/diff", { method: "POST" })
            .then(r => r.text())
            .catch(err => console.error(err));
    }, []);

    const searchChanged = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        setSearch(e.target.value);
    }, []);

    const foundIssues = useMemo(() => {
        const unqueued = issues.filter(i => i.state != IssueState.Queued);
        const s = search.trim().toLowerCase();
        return !s ? unqueued : unqueued.filter(i => (
            i.message.toLowerCase().indexOf(s) !== -1 ||
            i.options.some(o => o.toLowerCase().indexOf(s) !== -1)
        ));
    }, [issues, search]);

    const groups: IssueGroupProps[] = useMemo(() => {
        return groupBy(foundIssues, i => i.title).map(g => { 

            const maxOptions = g.items?.filter(i => i.state == IssueState.New)
                                      .map(i => i.options.length)
                                      .reduce((l, r) => Math.max(l, r), 0) ?? 0;

            const options: string[] = [];
            
            for (let n = 0; n < maxOptions; n++) {
                options.push(summariseToString(g.items?.map(i => i.options[n] || "") ?? []));
            }
            
            return { title: g.key, issues: g.items, options, delogo: setDelogoPath };
        });
    }, [foundIssues]);

    if (delogoPath) {
        return <DelogoMode path={delogoPath} cancel={() => setDelogoPath("")} />
    }

    return (
        <div>
            <div className="header">
                <button onClick={refresh}>Refresh</button>
                <span> Search </span>
                <input type="text" value={search} onChange={searchChanged} />
                <span> {issues.filter(i => i.state === IssueState.Queued).length} in queue</span>
            </div>
            <hr/>
        {
            groups.map(g => <IssueGroup {...g} key={g.title} />)
        }
        </div>
    );    
}

function groupBy<T>(ar: T[], keyOf: (i: T) => string) {
    const groups: { [key: string]: T[] } = {};

    for (const i of ar) {
        const key = keyOf(i);
        (groups[key] || (groups[key] = [])).push(i);
    }

    return Object.keys(groups).map(key => ({ key, items: groups[key]! }));
}
