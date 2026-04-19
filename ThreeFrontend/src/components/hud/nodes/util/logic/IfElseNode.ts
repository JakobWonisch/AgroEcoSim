import { ClassicPreset } from 'rete';
import { numSocket, boolSocket } from '../../sockets';

export class IfElseNode extends ClassicPreset.Node {
    constructor() {
        super('If / Else');
        this.addInput('condition', new ClassicPreset.Input(boolSocket, 'Condition'));
        this.addInput('trueValue', new ClassicPreset.Input(numSocket, 'True Value'));
        this.addInput('falseValue', new ClassicPreset.Input(numSocket, 'False Value'));
        this.addOutput('out', new ClassicPreset.Output(numSocket, 'Out'));
    }

    data(inputs: { condition?: boolean[], trueValue?: number[], falseValue?: number[] }) {
        const cond = inputs.condition ? inputs.condition[0] : false;
        const tVal = inputs.trueValue ? inputs.trueValue[0] : 0;
        const fVal = inputs.falseValue ? inputs.falseValue[0] : 0;
        return { out: cond ? tVal : fVal };
    }
}
