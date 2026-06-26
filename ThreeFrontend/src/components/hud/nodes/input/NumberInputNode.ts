import { ClassicPreset } from 'rete';
import { numSocket } from '../Sockets';
import { AddToConfigControl } from '../ConfigurationControls';

export class NumberInputNode extends ClassicPreset.Node {
    valueControl: ClassicPreset.InputControl<'number'>;
    addToConfigControl: AddToConfigControl;

    constructor(initialValue: number = 0) {
        super('Number Input');

        this.valueControl = new ClassicPreset.InputControl('number', {
            initial: initialValue
        });

        this.addControl('value', this.valueControl);
        this.addToConfigControl = new AddToConfigControl(() => this);
        this.addControl('addToConfig', this.addToConfigControl);
        this.addOutput('num', new ClassicPreset.Output(numSocket, 'Number'));
    }
}