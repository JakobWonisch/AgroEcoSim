import { ClassicPreset } from 'rete';
import { h } from 'preact';

export class SwitchControl extends ClassicPreset.Control {
    constructor(public value: boolean, public onChange: (val: boolean) => void) {
        super();
    }
}

export function SwitchControlComponent(props: { data: SwitchControl }) {
    return (
        <label style={{ display: 'flex', alignItems: 'center', gap: '8px', padding: '8px' }}>
            <input 
                type="checkbox" 
                checked={props.data.value} 
                onChange={(e) => {
                    const checked = (e.target as HTMLInputElement).checked;
                    props.data.value = checked;
                    props.data.onChange(checked);
                }}
            />
            <span style={{ color: 'white', fontSize: '14px', fontFamily: 'sans-serif' }}>Toggle</span>
        </label>
    );
}
