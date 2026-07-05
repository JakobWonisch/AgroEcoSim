import { ClassicPreset } from 'rete';
import { numSocket } from '../Sockets';
import { ConfigArraySelectControl } from '../ConfigurationControls';

export class ConfigurationArrayInputNode extends ClassicPreset.Node {
    configId: string;
    configType: 'number[]' = 'number[]';
    configControl: ConfigArraySelectControl;

    constructor(configId: string = '') {
        super('Configuration Array Input');
        this.configId = configId;
        this.addInput('index', new ClassicPreset.Input(numSocket, 'Index'));
        this.addOutput('out', new ClassicPreset.Output(numSocket, 'Number'));

        this.configControl = new ConfigArraySelectControl(
            configId,
            () => this,
            (id) => {
                this.configId = id;
            },
        );
        this.addControl('config', this.configControl);
    }
}
