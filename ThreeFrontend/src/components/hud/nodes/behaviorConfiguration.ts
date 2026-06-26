import { newBehaviorGraphId } from './Conversion';

export type BehaviorConfigType = 'number' | 'boolean';

export interface BehaviorConfigEntry {
    id: string;
    key: string;
    label: string;
    type: BehaviorConfigType;
    value: number | boolean;
}

export interface BehaviorConfigWireEntry {
    Id: string;
    Key: string;
    Label: string;
    Type: BehaviorConfigType;
    Value: number | boolean;
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
    return sortConfigEntries(entries).map(e => ({
        Id: e.id,
        Key: e.key,
        Label: e.label,
        Type: e.type,
        Value: e.value,
    }));
}

export function fromWireEntries(wire: BehaviorConfigWireEntry[] | undefined): BehaviorConfigEntry[] {
    if (!Array.isArray(wire)) return [];
    return wire
        .filter(e => e && typeof e.Id === 'string')
        .map(e => {
            const label = typeof e.Label === 'string' && e.Label.trim()
                ? e.Label.trim()
                : (typeof e.Key === 'string' ? e.Key.trim() : 'config');
            return {
                id: e.Id,
                key: typeof e.Key === 'string' && e.Key.trim() ? e.Key.trim() : label,
                label,
                type: e.Type === 'boolean' ? 'boolean' : 'number',
                value: e.Type === 'boolean' ? Boolean(e.Value) : Number(e.Value) || 0,
            };
        });
}
