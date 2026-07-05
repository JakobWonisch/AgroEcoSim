import { newBehaviorGraphId } from './Conversion';

export type BehaviorConfigType = 'number' | 'boolean' | 'number[]';

export interface BehaviorConfigEntry {
    id: string;
    key: string;
    label: string;
    /** Optional documentation shown in the species configuration panel only. */
    usage?: string;
    type: BehaviorConfigType;
    value: number | boolean | number[];
}

export interface BehaviorConfigWireEntry {
    Id: string;
    Key: string;
    Label: string;
    Usage?: string;
    Type: BehaviorConfigType;
    Value: number | boolean | number[];
}

export function newBehaviorConfigId(): string {
    return newBehaviorGraphId();
}

export function uniqueConfigKey(entries: BehaviorConfigEntry[], baseLabel: string): string {
    const trimmed = baseLabel.trim() || 'config';
    let key = trimmed;
    let n = 2;
    while (entries.some(e => e.key === key)) {
        key = `${trimmed} ${n++}`;
    }
    return key;
}

export function sortConfigEntries(entries: BehaviorConfigEntry[]): BehaviorConfigEntry[] {
    return [...entries].sort((a, b) => a.label.localeCompare(b.label, undefined, { sensitivity: 'base' }));
}

export function normalizeConfigLabel(label: string): string {
    return label.trim();
}

export function isDuplicateConfigLabel(
    entries: BehaviorConfigEntry[],
    label: string,
    excludeId?: string,
): boolean {
    const norm = normalizeConfigLabel(label).toLocaleLowerCase();
    if (!norm) return false;
    return entries.some(
        e => e.id !== excludeId && normalizeConfigLabel(e.label).toLocaleLowerCase() === norm,
    );
}

export function toWireEntries(entries: BehaviorConfigEntry[]): BehaviorConfigWireEntry[] {
    return sortConfigEntries(entries).map(e => {
        const wire: BehaviorConfigWireEntry = {
            Id: e.id,
            Key: e.key,
            Label: e.label,
            Type: e.type,
            Value: e.value,
        };
        const usage = e.usage?.trim();
        if (usage) wire.Usage = usage;
        return wire;
    });
}

export function fromWireEntries(wire: BehaviorConfigWireEntry[] | undefined): BehaviorConfigEntry[] {
    if (!Array.isArray(wire)) return [];
    return wire
        .filter(e => e && typeof e.Id === 'string')
        .map(e => {
            const label = typeof e.Label === 'string' && e.Label.trim()
                ? e.Label.trim()
                : (typeof e.Key === 'string' ? e.Key.trim() : 'config');
            const usage = typeof e.Usage === 'string' && e.Usage.trim() ? e.Usage.trim() : undefined;
            const type: BehaviorConfigType =
                e.Type === 'boolean' ? 'boolean'
                    : e.Type === 'number[]' ? 'number[]'
                        : 'number';
            let value: number | boolean | number[];
            if (type === 'boolean') {
                value = Boolean(e.Value);
            } else if (type === 'number[]') {
                value = Array.isArray(e.Value)
                    ? e.Value.map(v => Number(v) || 0)
                    : [];
            } else {
                value = Number(e.Value) || 0;
            }
            return {
                id: e.Id,
                key: typeof e.Key === 'string' && e.Key.trim() ? e.Key.trim() : label,
                label,
                usage,
                type,
                value,
            };
        });
}
