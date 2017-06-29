declare namespace MirrorBall {

	export interface FileState {
		path: string;
        hash: string;
		time: string;
        size: number;
    }

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
	}

    export interface IssueResolution {
        id: number;
        choice: string;
    }
}
