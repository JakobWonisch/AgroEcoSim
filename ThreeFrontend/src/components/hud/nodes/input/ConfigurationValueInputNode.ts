import { ClassicPreset } from 'rete';
import { boolSocket, numSocket } from '../Sockets';
import { ConfigSelectControl } from '../ConfigurationControls';
import type { BehaviorConfigType } from '../behaviorConfiguration';

export class ConfigurationValueInputNode extends ClassicPreset.Node {
    configId: string;
    configType: BehaviorConfigType;
    configControl: ConfigSelectControl;

    constructor(configId: string = '', configType: BehaviorConfigType = 'number') {
        super('Configuration Value Input');
        this.configId = configId;
        this.configType = configType;
        this.addOutputForType(configType);

        this.configControl = new ConfigSelectControl(
            configId,
            () => this,
            (id, type) => this.applyConfigBinding(id, type),
        );
        this.addControl('config', this.configControl);
    }

    private addOutputForType(type: BehaviorConfigType) {
        if (type === 'boolean') {
            this.addOutput('bool', new ClassicPreset.Output(boolSocket, 'Boolean'));
        } else {
            this.addOutput('num', new ClassicPreset.Output(numSocket, 'Number'));
        }
    }

    applyConfigBinding(configId: string, type: BehaviorConfigType) {
        this.configId = configId;
        if (this.configType === type) return;

        if (this.configType === 'number') this.removeOutput('num');
        else this.removeOutput('bool');

        this.configType = type;
        this.addOutputForType(type);
    }
}
