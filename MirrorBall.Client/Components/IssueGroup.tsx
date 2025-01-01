import { Issue, IssueInfo, IssueState } from "./Issue";

async function resolve(id: number, choice: string) {

    const request = { 
        method: "POST", 
        headers: { "Content-type": "application/json" },
        body: JSON.stringify({ id, choice })
    };

    await fetch("api/mirror/resolve", request)
        .then(r => r.text())
        .catch(err => console.error(err));
}

async function resolveAll(issues: IssueInfo[], choice: number) {
    for (const issue of issues.filter(i => i.state == IssueState.New)) {
        await resolve(issue.id, issue.options[choice]!);
    }
}

export interface IssueGroupProps {
    title: string;
    issues: IssueInfo[];
    options: string[];
    delogo(path: string): void;
}

export function IssueGroup({title, issues, options, delogo}: IssueGroupProps) {
    return (
        <div className="group">
            <h2>{title}</h2>
            {
                options.map((option, i) => (
                    <button onClick={() => resolveAll(issues, i)}>{option}</button>
                ))
            }
            {
                issues.map(issue => (
                    <Issue key={issue.id} {...issue} delogo={delogo}></Issue>                    
                ))
            }
        </div>
    );
}
