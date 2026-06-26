import { ClassicPreset } from 'rete';
import { numSocket } from '../Sockets';

export class NumberInputNode extends ClassicPreset.Node {
    valueControl: ClassicPreset.InputControl<'number'>;

    constructor(initialValue: number = 0) {
        super('Number Input');

        this.valueControl = new ClassicPreset.InputControl('number', {
            initial: initialValue
        });

        this.addControl('value', this.valueControl);
        this.addOutput('num', new ClassicPreset.Output(numSocket, 'Number'));
    }
}