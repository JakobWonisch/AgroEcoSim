import { ClassicPreset } from 'rete';
import { h } from 'preact';
import { useEffect, useState } from 'preact/hooks';
import type { BooleanInputNode } from './input/BooleanInputNode';
import type { NumberInputNode } from './input/NumberInputNode';
import { graphUpdateTrigger, notifyGraphUiUpdate } from './graphUpdate';
import {
    sortConfigEntries,
    type BehaviorConfigEntry,
    type BehaviorConfigType,
} from './behaviorConfiguration';
import type { ConfigurationValueInputNode } from './input/ConfigurationValueInputNode';
import { getEditorContext } from './editorContext';
import { isConfigurationInputConnected, rebindConfigurationInput, replaceConstantWithConfigInput } from './configurationBridge';
import { isDuplicateConfigLabel } from './behaviorConfiguration';

export class AddToConfigControl extends ClassicPreset.Control {
    constructor(public readonly getNode: () => NumberInputNode | BooleanInputNode) {
        super();
    }
}

export class ConfigSelectControl extends ClassicPreset.Control {
    constructor(
        public configId: string,
        public readonly getNode: () => ConfigurationValueInputNode,
        public readonly onBindingChange: (configId: string, type: BehaviorConfigType) => void,
    ) {
        super();
    }
}

function stopPropagation(e: Event) {
    e.stopPropagation();
}

export function toggleButtonStyle(active: boolean, edge: 'left' | 'right'): preact.JSX.CSSProperties {
    return {
        flex: '1 1 0',
        maxWidth: 100,
        padding: '6px 12px',
        border: '1px solid #555',
        borderRight: edge === 'left' ? 'none' : '1px solid #555',
        borderRadius: edge === 'left' ? '4px 0 0 4px' : '0 4px 4px 0',
        background: active ? '#e67e22' : '#333',
        color: active ? '#fff' : '#aaa',
        cursor: 'pointer',
        fontFamily: 'sans-serif',
        fontSize: '13px',
        fontWeight: active ? 600 : 400,
        boxSizing: 'border-box',
    };
}

export function BooleanToggleGroup({
    value,
    onChange,
    compact,
}: {
    value: boolean;
    onChange: (next: boolean) => void;
    compact?: boolean;
}) {
    return h(
        'div',
        {
            role: 'group',
            'aria-label': 'Boolean value',
            onPointerDown: stopPropagation,
            onDblClick: stopPropagation,
            style: { display: 'flex', maxWidth: compact ? 200 : undefined },
        },
        h(
            'button',
            { type: 'button', onClick: () => onChange(true), style: toggleButtonStyle(value, 'left') },
            'True',
        ),
        h(
            'button',
            { type: 'button', onClick: () => onChange(false), style: toggleButtonStyle(!value, 'right') },
            'False',
        ),
    );
}

export function AddToConfigControlComponent(props: { data: AddToConfigControl }) {
    const node = props.data.getNode();
    const ctx = getEditorContext();
    const [revision, setRevision] = useState(0);

    useEffect(() => {
        const onUpdate = () => setRevision(r => r + 1);
        graphUpdateTrigger.addEventListener('update', onUpdate);
        return () => graphUpdateTrigger.removeEventListener('update', onUpdate);
    }, []);

    void revision;
    const comment = typeof (node as { comment?: string }).comment === 'string'
        ? (node as { comment?: string }).comment!.trim()
        : '';
    const entries = ctx?.species.behaviorConfiguration.peek() ?? [];
    const duplicateLabel = comment.length > 0 && isDuplicateConfigLabel(entries, comment);
    const noComment = comment.length === 0;
    const disabled = noComment || duplicateLabel;

    let hint = '';
    if (noComment) hint = 'Add a comment to this node before promoting it to configuration.';
    else if (duplicateLabel) hint = 'This comment is already used as a configuration label.';

    const tooltip = disabled
        ? hint
        : 'Promote this constant to a species configuration value';

    return h(
        'div',
        { style: { padding: '4px 8px 8px' }, onPointerDown: stopPropagation, onDblClick: stopPropagation },
        h(
            'button',
            {
                type: 'button',
                disabled,
                title: tooltip,
                onClick: async () => {
                    if (!ctx || disabled) return;
                    await replaceConstantWithConfigInput(node, ctx);
                },
                style: {
                    width: '100%',
                    padding: '6px 8px',
                    borderRadius: '4px',
                    border: '1px solid #555',
                    background: disabled ? '#2a2a2a' : '#3a4a3a',
                    color: disabled ? '#777' : '#dfe',
                    cursor: disabled ? 'not-allowed' : 'pointer',
                    fontFamily: 'sans-serif',
                    fontSize: '12px',
                },
            },
            'Add to Configuration',
        ),
        hint
            ? h(
                  'div',
                  {
                      style: {
                          marginTop: '4px',
                          color: 'rgba(255,255,255,0.55)',
                          fontSize: '11px',
                          fontFamily: 'sans-serif',
                          lineHeight: 1.3,
                      },
                  },
                  hint,
              )
            : null,
    );
}

function configEntriesForDropdown(
    entries: BehaviorConfigEntry[],
    node: ConfigurationValueInputNode,
    connected: boolean,
) {
    if (connected)
        return sortConfigEntries(entries.filter(e => e.type === node.configType));
    return sortConfigEntries(entries);
}

export function ConfigSelectControlComponent(props: { data: ConfigSelectControl }) {
    const ctx = getEditorContext();
    const [open, setOpen] = useState(false);
    const [configId, setConfigId] = useState(props.data.configId);
    const [revision, setRevision] = useState(0);

    useEffect(() => {
        setConfigId(props.data.configId);
    }, [props.data.configId]);

    useEffect(() => {
        const onUpdate = () => setRevision(r => r + 1);
        graphUpdateTrigger.addEventListener('update', onUpdate);
        return () => graphUpdateTrigger.removeEventListener('update', onUpdate);
    }, []);

    void revision;
    const node = props.data.getNode();
    const isConnected = ctx ? isConfigurationInputConnected(node, ctx) : false;
    const allEntries = ctx?.species.behaviorConfiguration.peek() ?? [];
    const entries = configEntriesForDropdown(allEntries, node, isConnected);

    const selected = entries.find(e => e.id === configId)
        ?? allEntries.find(e => e.id === configId);
    const label = selected?.label?.trim() || '(select configuration value)';

    const pick = async (entry: BehaviorConfigEntry) => {
        setConfigId(entry.id);
        props.data.configId = entry.id;
        setOpen(false);
        if (ctx) {
            await rebindConfigurationInput(node, entry.id, entry.type, ctx);
        } else {
            props.data.onBindingChange(entry.id, entry.type);
            notifyGraphUiUpdate();
        }
    };

    return h(
        'div',
        {
            style: { padding: '8px', position: 'relative' },
            onPointerDown: stopPropagation,
            onDblClick: stopPropagation,
        },
        h(
            'button',
            {
                type: 'button',
                onClick: () => {
                    setRevision(r => r + 1);
                    setOpen(v => !v);
                },
                style: {
                    width: '100%',
                    padding: '6px 8px',
                    borderRadius: '4px',
                    border: '1px solid #555',
                    background: '#222',
                    color: '#fff',
                    cursor: 'pointer',
                    fontFamily: 'sans-serif',
                    fontSize: '13px',
                    textAlign: 'left',
                },
            },
            label,
        ),
        open
            ? h(
                  'div',
                  {
                      style: {
                          position: 'absolute',
                          left: 8,
                          right: 8,
                          top: '100%',
                          zIndex: 20,
                          background: '#2a2a2a',
                          border: '1px solid #555',
                          borderRadius: '4px',
                          maxHeight: 160,
                          overflowY: 'auto',
                          boxShadow: '0 4px 12px rgba(0,0,0,0.35)',
                      },
                  },
                  entries.length === 0
                      ? h(
                            'div',
                            {
                                style: {
                                    padding: '8px',
                                    color: '#999',
                                    fontSize: '12px',
                                    fontFamily: 'sans-serif',
                                },
                            },
                            'No configuration values yet',
                        )
                      : entries.map(e =>
                            h(
                                'button',
                                {
                                    type: 'button',
                                    key: e.id,
                                    onClick: () => pick(e),
                                    style: {
                                        display: 'block',
                                        width: '100%',
                                        padding: '6px 8px',
                                        border: 'none',
                                        borderBottom: '1px solid #444',
                                        background: e.id === configId ? 'rgba(80,160,120,0.35)' : 'transparent',
                                        color: '#fff',
                                        cursor: 'pointer',
                                        fontFamily: 'sans-serif',
                                        fontSize: '13px',
                                        textAlign: 'left',
                                    },
                                },
                                e.label || e.key,
                            ),
                        ),
              )
            : null,
    );
}
