import React, { useState, useEffect, useRef, ChangeEvent, useMemo, useCallback } from "react";
import { NumberInput } from "./NumberInput";

export interface DelogoModeProps {
    path: string;
    cancel(): void;
}

export function DelogoMode({ path, cancel }: DelogoModeProps) {
    const [image, setImage] = useState<HTMLImageElement>();
    const canvas = useRef<HTMLCanvasElement>(null);

    const [left, setLeft] = useState(20);
    const [top, setTop] = useState(30);
    const [right, setRight] = useState(160);
    const [bottom, setBottom] = useState(80);

    useEffect(() => {
        setImage(undefined);
        const i = document.createElement("img");
        i.onload = () => setImage(i);
        i.src = "/api/mirror/thumbnail/" + path;
    }, [path]);

    useEffect(() => {
        if (!canvas.current) return;

        const ctx = canvas.current.getContext("2d")!;
        ctx.fillStyle = "black";
        ctx.fillRect(0, 0, canvas.current.width, canvas.current.height);

        if (image) {
            ctx.drawImage(image, 0, 0);
        }

        ctx.lineWidth = 2;
        ctx.strokeStyle = "magenta";
        ctx.strokeRect(left, top, right-left, bottom-top);
    }, [image, left, top, right, bottom]);

    const formatted = useMemo(() => {
        if ([left, top, right, bottom].some(x => isNaN(x))
            || right <= left || bottom <= top) {
            return "invalid";
        }

        return `x=${left}:y=${top}:w=${right-left}:h=${bottom-top}`;
    }, [left, top, right, bottom]);

    const onChangeFormatted = useCallback((e: ChangeEvent<HTMLInputElement>) => {
        const parts = Object.fromEntries(e.target.value.split(":").map(x => {
            const s = x.split("=");            
            return [s[0], parseFloat(s[1] ?? "")];
        }));

        if (["x", "y", "w", "h"].some(name => 
            typeof parts[name] !== "number" || isNaN(parts[name])
        )) return;

        setLeft(parts.x);
        setTop(parts.y);
        setRight(parts.x + parts.w);
        setBottom(parts.y + parts.h);
    }, []);

    const start = useCallback(async () => {
        await enqueue(path, formatted);
        cancel();
    }, [path, formatted]);

    const mouseDown = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
        const b = canvas.current?.getBoundingClientRect();    
        setLeft(e.clientX - (b?.left ?? 0));
        setTop(e.clientY - (b?.top ?? 0));
    }, []);

    const mouseMove = useCallback((e: React.MouseEvent<HTMLCanvasElement>) => {
        if (!e.buttons) return;
        const b = canvas.current?.getBoundingClientRect();    
        setRight(e.clientX - (b?.left ?? 0));
        setBottom(e.clientY - (b?.top ?? 0));
    }, []);

    return (
        <div className="delogo">
            <div>
                <button onClick={cancel}>Cancel</button>
                <span>{path}</span>
            </div>
            <div className="settings">
                <label>L: <NumberInput value={left} onChange={setLeft} /></label>
                <label>T: <NumberInput value={top} onChange={setTop} /></label>
                <label>R: <NumberInput value={right} onChange={setRight} /></label>
                <label>B: <NumberInput value={bottom} onChange={setBottom} /></label>
                <label>Option: <input value={formatted} onChange={onChangeFormatted}/></label>
            </div>
            <canvas ref={canvas} width={1000} height={200} onMouseDown={mouseDown} onMouseMove={mouseMove} />
            <div>
                <button onClick={start}>Start</button>
            </div>
        </div>
    );
}

async function enqueue(path: string, option: string) {
    const request = { 
        method: "POST", 
        headers: { "Content-type": "application/json" },
        body: JSON.stringify({ path, option })
    };

    await fetch("api/mirror/delogo", request)
        .then(r => r.text())
        .catch(err => console.error(err));
}
