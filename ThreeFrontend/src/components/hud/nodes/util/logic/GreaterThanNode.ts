import { ClassicPreset } from 'rete';
import { numSocket, boolSocket } from '../../Sockets';

export class GreaterThanNode extends ClassicPreset.Node {
    constructor() {
        super('Greater Than (or Equal)');
        this.addInput('a', new ClassicPreset.Input(numSocket, 'A'));
        this.addInput('b', new ClassicPreset.Input(numSocket, 'B'));
        this.addControl('equal', new ClassicPreset.InputControl('number', { initial: 0 })); // 0 = false, 1 = true
        this.addOutput('out', new ClassicPreset.Output(boolSocket, 'Out'));
    }

    data(inputs: { a?: number[], b?: number[] }) {
        const a = inputs.a ? inputs.a[0] : 0;
        const b = inputs.b ? inputs.b[0] : 0;
        const inclusive = (this.controls.equal as ClassicPreset.InputControl<'number'>).value > 0;
        return { out: inclusive ? a >= b : a > b };
    }
}
