import { useCallback, ChangeEvent } from "react";

export interface NumberInputProps {
    value: number;
    onChange(value: number): void;
} 

export function NumberInput({ value, onChange }: NumberInputProps) {

    const change = useCallback((e: ChangeEvent<HTMLInputElement>) => {
        onChange(parseFloat(e.target.value));
    }, [onChange]);

    return <input type="number" value={value} onChange={change} />
}
