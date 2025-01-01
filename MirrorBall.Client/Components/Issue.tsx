export const enum IssueState {
    New,
    Queued,
    Busy,
    Failed
}

export interface IssueInfo {
    id: number;
    title: string;
    options: string[];
    state: IssueState;
    progress: number;
    progressText: string;
    message: string;
    choice: string;
    delogoPath?: string;
}

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

export type IssueProps = IssueInfo & { delogo(path: string): void };

export function Issue(props: IssueProps) {
    const delogoPath = props.delogoPath;

    return (
        <div className="issue">
            <div className="title">{props.title}</div>
            <div className="content">
                <div className={props.state == IssueState.Failed ? "error" : "message"}>
                    {props.message}
                </div>
                {
                    props.state == IssueState.New ? 
                        <div className="options">
                        {
                            props.options.map(option => (
                                <button key={option}
                                    onClick={() => resolve(props.id, option)}>{option}</button>
                            ))
                        }
                        {delogoPath && <button onClick={() => props.delogo(delogoPath)}>De-logo</button>}                            
                        </div> :
                    props.state == IssueState.Failed ?
                        <div className="error">
                            <button onClick={() => resolve(props.id, "")}>Clear</button>
                        </div> :
                    props.state == IssueState.Queued ?
                        <div className="waiting">
                            Queued...
                        </div> :
                    props.state == IssueState.Busy ?
                        <div>
                            <div className="progress">
                                <div className="bar" style={{ width: `${props.progress * 100}%`}} />
                            </div> 
                            <div>{props.progressText}</div>
                        </div> : null
                }                    
            </div>
        </div>
    );
}
