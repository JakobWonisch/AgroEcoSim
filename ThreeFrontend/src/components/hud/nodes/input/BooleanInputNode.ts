import { ClassicPreset } from 'rete';
import { boolSocket } from '../Sockets';
import { SwitchControl } from '../Controls';
import { AddToConfigControl } from '../ConfigurationControls';

export class BooleanInputNode extends ClassicPreset.Node {
    switchControl: SwitchControl;
    addToConfigControl: AddToConfigControl;

    constructor(initialValue: boolean = false) {
        super('Boolean Input');

        this.switchControl = new SwitchControl(initialValue, (val) => {
            // Updated directly in the component reference
        });

        this.addControl('switch', this.switchControl);
        this.addToConfigControl = new AddToConfigControl(() => this);
        this.addControl('addToConfig', this.addToConfigControl);
        this.addOutput('bool', new ClassicPreset.Output(boolSocket, 'Boolean'));
    }
}