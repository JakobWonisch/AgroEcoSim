import { h } from 'preact';
import { useEffect, useState } from 'preact/hooks';
import type { Species } from '../../helpers/Species';
import {
    isDuplicateConfigLabel,
    normalizeConfigLabel,
    sortConfigEntries,
    type BehaviorConfigEntry,
    type BehaviorConfigType,
} from './nodes/behaviorConfiguration';
import { BooleanToggleGroup } from './nodes/ConfigurationControls';
import { deleteConfigurationEntry } from './nodes/configurationBridge';
import { getEditorContext } from './nodes/editorContext';
import { graphUpdateTrigger } from './nodes/graphUpdate';

const valueInputStyle: h.JSX.CSSProperties = {
    width: 100,
    maxWidth: 100,
    boxSizing: 'border-box',
    padding: '4px 6px',
    borderRadius: '4px',
    border: '1px solid #555',
    background: '#222',
    color: '#fff',
    fontFamily: 'sans-serif',
    fontSize: '13px',
};

const labelTextStyle: h.JSX.CSSProperties = {
    width: 150,
    maxWidth: 150,
    color: 'rgba(255,255,255,0.85)',
    fontSize: '13px',
    lineHeight: 1.35,
    wordBreak: 'break-word',
    fontFamily: 'sans-serif',
};

const hintStyle: h.JSX.CSSProperties = {
    marginTop: '2px',
    color: 'rgba(255,180,120,0.9)',
    fontSize: '11px',
    fontFamily: 'sans-serif',
    lineHeight: 1.3,
    maxWidth: 150,
};

function stopPropagation(e: Event) {
    e.stopPropagation();
}

function ConfigRow({
    entry,
    allEntries,
    onSaveLabel,
    onChangeValue,
    onDelete,
}: {
    entry: BehaviorConfigEntry;
    allEntries: BehaviorConfigEntry[];
    onSaveLabel: (label: string) => void;
    onChangeValue: (value: number | boolean) => void;
    onDelete: () => void;
}) {
    const [editing, setEditing] = useState(false);
    const [draft, setDraft] = useState(entry.label);

    useEffect(() => {
        if (!editing) setDraft(entry.label);
    }, [entry.label, editing]);

    const trimmed = normalizeConfigLabel(draft);
    const duplicate = trimmed.length > 0 && isDuplicateConfigLabel(allEntries, trimmed, entry.id);
    const canSave = editing && trimmed.length > 0 && !duplicate;

    return h(
        'div',
        {
            style: {
                display: 'flex',
                gap: '12px',
                alignItems: 'start',
                padding: '8px 0',
                borderBottom: '1px solid rgba(255,255,255,0.1)',
            },
        },
        h(
            'div',
            { style: { width: 150, flexShrink: 0 } },
            editing
                ? h('textarea', {
                      value: draft,
                      rows: 2,
                      onInput: (e: Event) => setDraft((e.target as HTMLTextAreaElement).value),
                      onPointerDown: stopPropagation,
                      style: {
                          width: '100%',
                          boxSizing: 'border-box',
                          padding: '4px 6px',
                          borderRadius: '4px',
                          border: '1px solid #555',
                          background: '#222',
                          color: '#fff',
                          fontFamily: 'sans-serif',
                          fontSize: '13px',
                          lineHeight: 1.35,
                          resize: 'vertical',
                          wordBreak: 'break-word',
                      },
                  })
                : h('div', { style: labelTextStyle }, entry.label || '—'),
            editing && duplicate
                ? h('div', { style: hintStyle }, 'This label is already used by another configuration value.')
                : null,
            editing && trimmed.length === 0
                ? h('div', { style: hintStyle }, 'Label cannot be empty.')
                : null,
        ),
        h(
            'button',
            {
                type: 'button',
                disabled: editing && !canSave,
                onClick: () => {
                    if (editing) {
                        if (!canSave) return;
                        onSaveLabel(trimmed);
                        setEditing(false);
                    } else {
                        setDraft(entry.label);
                        setEditing(true);
                    }
                },
                onPointerDown: stopPropagation,
                style: {
                    padding: '4px 8px',
                    borderRadius: '4px',
                    border: '1px solid #555',
                    background: '#333',
                    color: '#fff',
                    cursor: editing && !canSave ? 'not-allowed' : 'pointer',
                    fontFamily: 'sans-serif',
                    fontSize: '12px',
                    flexShrink: 0,
                    opacity: editing && !canSave ? 0.5 : 1,
                },
            },
            editing ? 'Save' : 'Edit',
        ),
        entry.type === 'boolean'
            ? h(BooleanToggleGroup, {
                  value: Boolean(entry.value),
                  onChange: (v: boolean) => onChangeValue(v),
                  compact: true,
              })
            : h('input', {
                  type: 'number',
                  value: Number(entry.value),
                  onInput: (e: Event) => onChangeValue(parseFloat((e.target as HTMLTextAreaElement).value) || 0),
                  onPointerDown: stopPropagation,
                  style: valueInputStyle,
              }),
        h(
            'button',
            {
                type: 'button',
                title: 'Delete configuration value',
                onClick: onDelete,
                onPointerDown: stopPropagation,
                style: {
                    padding: '4px 8px',
                    borderRadius: '4px',
                    border: '1px solid #633',
                    background: '#3a2222',
                    color: '#faa',
                    cursor: 'pointer',
                    fontFamily: 'sans-serif',
                    fontSize: '12px',
                    flexShrink: 0,
                },
            },
            'Delete',
        ),
    );
}

export default function SpeciesConfigurationEditor({ species }: { species: Species }) {
    const entries = sortConfigEntries(species.behaviorConfiguration.value);

    const saveLabel = (id: string, label: string) => {
        const trimmed = normalizeConfigLabel(label);
        if (!trimmed) return;
        if (isDuplicateConfigLabel(species.behaviorConfiguration.peek(), trimmed, id)) return;
        species.behaviorConfiguration.value = species.behaviorConfiguration.peek().map(e =>
            e.id === id ? { ...e, label: trimmed, key: trimmed } : e);
        graphUpdateTrigger.dispatchEvent(new Event('update'));
    };

    const setValue = (id: string, value: number | boolean) => {
        species.behaviorConfiguration.value = species.behaviorConfiguration.peek().map(e =>
            e.id === id ? { ...e, value } : e);
        graphUpdateTrigger.dispatchEvent(new Event('update'));
    };

    const deleteEntry = (id: string) => {
        void deleteConfigurationEntry(species, id, getEditorContext());
    };

    return h(
        'div',
        {
            style: {
                minHeight: 360,
                display: 'flex',
                flexDirection: 'column',
                overflow: 'auto',
                padding: '8px 12px',
                width: 'fit-content',
                maxWidth: '100%',
            },
        },
        h(
            'div',
            {
                style: {
                    display: 'flex',
                    gap: '12px',
                    padding: '4px 0 8px',
                    color: 'rgba(255,255,255,0.55)',
                    fontSize: '12px',
                    fontFamily: 'sans-serif',
                    borderBottom: '1px solid rgba(255,255,255,0.15)',
                },
            },
            h('span', { style: { width: 150 } }, 'Label'),
            h('span', { style: { width: 44 } }, ''),
            h('span', { style: { width: 200 } }, 'Value'),
            h('span', { style: { width: 56 } }, ''),
        ),
        entries.length === 0
            ? h(
                  'p',
                  {
                      style: {
                          color: 'rgba(255,255,255,0.6)',
                          fontFamily: 'sans-serif',
                          fontSize: '13px',
                          marginTop: '12px',
                          maxWidth: 420,
                      },
                  },
                  'No configuration values yet. Add a comment to a Number or Boolean input node, then use “Add to Configuration”.',
              )
            : entries.map(entry =>
                  h(ConfigRow, {
                      key: entry.id,
                      entry,
                      allEntries: entries,
                      onSaveLabel: (label: string) => saveLabel(entry.id, label),
                      onChangeValue: (value: number | boolean) => setValue(entry.id, value),
                      onDelete: () => deleteEntry(entry.id),
                  }),
              ),
    );
}

export type { BehaviorConfigType };
