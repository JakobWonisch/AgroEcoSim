import { ClassicPreset } from 'rete';
import { numSocket, boolSocket } from '../../Sockets';

export class IfElseNode extends ClassicPreset.Node {
    constructor() {
        super('If / Else');
        this.addInput('condition', new ClassicPreset.Input(boolSocket, 'Condition'));
        this.addInput('trueValue', new ClassicPreset.Input(numSocket, 'True Value'));
        this.addInput('falseValue', new ClassicPreset.Input(numSocket, 'False Value'));
        this.addOutput('out', new ClassicPreset.Output(numSocket, 'Out'));
    }
}