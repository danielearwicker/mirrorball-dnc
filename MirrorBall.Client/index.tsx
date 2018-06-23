import * as React from "react";
import * as ReactDOM from "react-dom";

interface IssueState {
    resolving: boolean;
}

class Issue extends React.Component<MirrorBall.IssueInfo, IssueState> {

    constructor(props: MirrorBall.IssueInfo) {
        super(props);
        this.state = { resolving: false };
    }

    resolve(choice: string) {

        const request = { 
            method: "POST", 
            headers: {
                "Content-type": "application/json"
            },
            body: JSON.stringify({
                id: this.props.id,
                choice
            })
        };

        this.setState({ resolving: true });

        fetch("api/mirror/resolve", request)
            .then(r => r.text())
            .catch(err => console.error(err))
            .then(() => this.setState({ resolving: false }));
    }

    render() {        
        return (
            <div className="issue">
                <div className="title">{this.props.title}</div>
                <div className="content">
                    <div className={this.props.state == MirrorBall.IssueState.Failed ? "error" : "message"}>
                        {this.props.message}
                    </div>
                    {
                        this.props.state == MirrorBall.IssueState.New ? 
                            <div className="options">
                            {
                                this.props.options.map(option => (
                                    <button key={option} 
                                        disabled={this.state.resolving} 
                                        onClick={() => this.resolve(option)}>{option}</button>
                                ))
                            }
                            </div> :
                        this.props.state == MirrorBall.IssueState.Failed ?
                            <div className="error">
                                <button onClick={() => this.resolve("")}>Clear</button>
                            </div> :
                        this.props.state == MirrorBall.IssueState.Queued ?
                            <div className="waiting">
                                Queued...
                            </div> :
                        this.props.state == MirrorBall.IssueState.Busy ?
                            <div>
                                <div className="progress">
                                    <div className="bar" style={{ width: `${this.props.progress * 100}%`}} />
                                </div> 
                                <div>{this.props.progressText}</div>
                            </div> : null
                    }                    
                </div>
            </div>
        );
    }
}

interface MirrorBallAppState {
    issues: MirrorBall.IssueInfo[];
    search: string;
}

class App extends React.Component<{}, MirrorBallAppState> {

    quit = false;

    constructor(props: {}) {
        super(props);
        this.state = { issues: [], search: "" };
    }

    fetchIssues() {
        if (this.quit) {
            return;
        }
        
        fetch("api/mirror/issues")
            .then(r => r.json())
            .then((issues: MirrorBall.IssueInfo[]) => this.setState({ issues }))
            .catch(err => console.error(err))
            .then(() => {
                setTimeout(() => this.fetchIssues(), 1000);
            });
    }

    componentDidMount() {
        this.fetchIssues();
    }

    refresh = () => {
        fetch("api/mirror/diff", { method: "POST" })
            .then(r => r.text())
            .catch(err => console.error(err));
    }

    componentWillUnmount() {
        this.quit = true;
    }

    searchChanged = (e: React.ChangeEvent<HTMLInputElement>) => {
        this.setState({ search: e.target.value });
    }

    get foundIssues() {
        const s = this.state.search.trim();
        if (!s) {
            return this.state.issues;
        }
        return this.state.issues.filter(i => (
            i.message.indexOf(s) !== -1 ||
            i.options.some(o => o.indexOf(s) !== -1)
        ));
    }

    render() {
        return (
            <div>
                <div>
                    <button onClick={this.refresh}>Refresh</button>
                </div>
                <div>
                    <input type="text" value={this.state.search} onChange={this.searchChanged} />
                </div>
                <hr/>
            {
                this.foundIssues.map(issue => (
                    <Issue key={issue.id} {...issue}></Issue>                    
                ))
            }
            </div>
        );
    }
}

ReactDOM.render(<App/>, document.querySelector("#root"));

