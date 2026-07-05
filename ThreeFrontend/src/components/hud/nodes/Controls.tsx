import { ClassicPreset } from 'rete';
import { h } from 'preact';
import { useState } from 'preact/hooks';
import { BooleanToggleGroup } from './ConfigurationControls';
import { graphUpdateTrigger } from './graphUpdate';

export class SwitchControl extends ClassicPreset.Control {
    constructor(public value: boolean, public onChange: (val: boolean) => void) {
        super();
    }
}

export function SwitchControlComponent(props: { data: SwitchControl }) {
    const [val, setVal] = useState(props.data.value);

    const setValue = (next: boolean) => {
        setVal(next);
        props.data.value = next;
        props.data.onChange(next);
        graphUpdateTrigger.dispatchEvent(new Event('update'));
    };

    return (
        <BooleanToggleGroup value={val} onChange={setValue} />
    );
}

export function CustomInputComponent(props: { data: ClassicPreset.InputControl<"number" | "text"> }) {
    const [val, setVal] = useState(props.data.value);
    const type = props.data.type;
    return (
        <input
            value={val as any}
            type={type}
            onPointerDown={e => e.stopPropagation()}
            onDblClick={e => e.stopPropagation()}
            onChange={e => {
                const newVal = type === 'number' ? +(e.target as HTMLInputElement).value : (e.target as HTMLInputElement).value;
                setVal(newVal);
                props.data.setValue(newVal as any);
                if (props.data.options && props.data.options.change) {
                    props.data.options.change(newVal as any);
                }
                graphUpdateTrigger.dispatchEvent(new Event('update'));
            }}
            style={{
                width: '100%',
                padding: '4px',
                borderRadius: '4px',
                border: '1px solid #555',
                background: '#222',
                color: '#fff',
                fontFamily: 'sans-serif',
                fontSize: '14px'
            }}
        />
    );
}

export function CustomSocketComponent(props: { data: ClassicPreset.Socket }) {
    const color = props.data.name === 'Number' ? '#3498db' : '#e67e22'; // blue for Number, orange for Boolean
    return (
        <div 
            title={props.data.name} 
            style={{ 
                width: '18px', 
                height: '18px', 
                background: color,
                borderRadius: '50%',
                border: '2px solid rgba(255, 255, 255, 0.8)',
                display: 'inline-block',
                verticalAlign: 'middle',
                boxSizing: 'border-box',
                cursor: 'pointer'
            }} 
        />
    );
}
