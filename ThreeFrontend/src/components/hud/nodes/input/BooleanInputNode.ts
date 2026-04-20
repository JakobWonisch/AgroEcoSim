import { ClassicPreset } from 'rete';
import { boolSocket } from '../Sockets';
import { SwitchControl } from '../Controls';

export class BooleanInputNode extends ClassicPreset.Node {
    switchControl: SwitchControl;

    constructor(initialValue: boolean = false) {
        super('Boolean Input');

        this.switchControl = new SwitchControl(initialValue, (val) => {
            // Updated directly in the component reference
        });

        this.addControl('switch', this.switchControl);
        this.addOutput('bool', new ClassicPreset.Output(boolSocket, 'Boolean'));
    }

    data() {
        return { bool: this.switchControl.value };
    }
}
