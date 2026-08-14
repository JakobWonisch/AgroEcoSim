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

function pick(obj: Record<string, unknown>, ...keys: string[]): unknown {
    for (const key of keys) {
        if (obj[key] !== undefined)
            return obj[key];
    }
    return undefined;
}

export function fromWireEntries(wire: BehaviorConfigWireEntry[] | undefined): BehaviorConfigEntry[] {
    if (!Array.isArray(wire)) return [];
    return wire
        .map(e => e as unknown as Record<string, unknown>)
        .filter(e => e && typeof pick(e, 'Id', 'id') === 'string')
        .map(e => {
            const id = String(pick(e, 'Id', 'id'));
            const rawLabel = pick(e, 'Label', 'label');
            const rawKey = pick(e, 'Key', 'key');
            const label = typeof rawLabel === 'string' && rawLabel.trim()
                ? rawLabel.trim()
                : (typeof rawKey === 'string' ? rawKey.trim() : 'config');
            const rawUsage = pick(e, 'Usage', 'usage');
            const usage = typeof rawUsage === 'string' && rawUsage.trim() ? rawUsage.trim() : undefined;
            const rawType = pick(e, 'Type', 'type');
            const type: BehaviorConfigType =
                rawType === 'boolean' ? 'boolean'
                    : rawType === 'number[]' ? 'number[]'
                        : 'number';
            const rawValue = pick(e, 'Value', 'value');
            let value: number | boolean | number[];
            if (type === 'boolean') {
                value = Boolean(rawValue);
            } else if (type === 'number[]') {
                value = Array.isArray(rawValue)
                    ? rawValue.map(v => Number(v) || 0)
                    : [];
            } else {
                value = Number(rawValue) || 0;
            }
            return {
                id,
                key: typeof rawKey === 'string' && rawKey.trim() ? rawKey.trim() : label,
                label,
                usage,
                type,
                value,
            };
        });
}
