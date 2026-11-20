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
import { syncSpeciesSignalsFromConfiguration } from './nodes/syncConfigurationSignals';
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

const usageTextStyle: h.JSX.CSSProperties = {
    width: 220,
    maxWidth: 220,
    color: 'rgba(255,255,255,0.65)',
    fontSize: '12px',
    lineHeight: 1.35,
    wordBreak: 'break-word',
    fontFamily: 'sans-serif',
    fontStyle: 'italic',
};

const hintStyle: h.JSX.CSSProperties = {
    marginTop: '2px',
    color: 'rgba(255,180,120,0.9)',
    fontSize: '11px',
    fontFamily: 'sans-serif',
    lineHeight: 1.3,
    maxWidth: 150,
};

const editButtonStyle = (disabled: boolean): h.JSX.CSSProperties => ({
    padding: '4px 8px',
    borderRadius: '4px',
    border: '1px solid #555',
    background: '#333',
    color: '#fff',
    cursor: disabled ? 'not-allowed' : 'pointer',
    fontFamily: 'sans-serif',
    fontSize: '12px',
    flexShrink: 0,
    opacity: disabled ? 0.5 : 1,
});

const fieldTextareaStyle: h.JSX.CSSProperties = {
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
};

function stopPropagation(e: Event) {
    e.stopPropagation();
}

function ConfigRow({
    entry,
    allEntries,
    onSaveLabel,
    onSaveUsage,
    onChangeValue,
    onDelete,
}: {
    entry: BehaviorConfigEntry;
    allEntries: BehaviorConfigEntry[];
    onSaveLabel: (label: string) => void;
    onSaveUsage: (usage: string) => void;
    onChangeValue: (value: number | boolean | number[]) => void;
    onDelete: () => void;
}) {
    const [editingLabel, setEditingLabel] = useState(false);
    const [editingUsage, setEditingUsage] = useState(false);
    const [labelDraft, setLabelDraft] = useState(entry.label);
    const [usageDraft, setUsageDraft] = useState(entry.usage ?? '');

    useEffect(() => {
        if (!editingLabel) setLabelDraft(entry.label);
    }, [entry.label, editingLabel]);

    useEffect(() => {
        if (!editingUsage) setUsageDraft(entry.usage ?? '');
    }, [entry.usage, editingUsage]);

    const trimmedLabel = normalizeConfigLabel(labelDraft);
    const duplicateLabel = trimmedLabel.length > 0 && isDuplicateConfigLabel(allEntries, trimmedLabel, entry.id);
    const canSaveLabel = editingLabel && trimmedLabel.length > 0 && !duplicateLabel;

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
            editingLabel
                ? h('textarea', {
                      value: labelDraft,
                      rows: 2,
                      onInput: (e: Event) => setLabelDraft((e.target as HTMLTextAreaElement).value),
                      onPointerDown: stopPropagation,
                      style: fieldTextareaStyle,
                  })
                : h('div', { style: labelTextStyle }, entry.label || '—'),
            editingLabel && duplicateLabel
                ? h('div', { style: hintStyle }, 'This label is already used by another configuration value.')
                : null,
            editingLabel && trimmedLabel.length === 0
                ? h('div', { style: hintStyle }, 'Label cannot be empty.')
                : null,
        ),
        h(
            'button',
            {
                type: 'button',
                disabled: editingLabel && !canSaveLabel,
                onClick: () => {
                    if (editingLabel) {
                        if (!canSaveLabel) return;
                        onSaveLabel(trimmedLabel);
                        setEditingLabel(false);
                    } else {
                        setLabelDraft(entry.label);
                        setEditingLabel(true);
                    }
                },
                onPointerDown: stopPropagation,
                style: editButtonStyle(editingLabel && !canSaveLabel),
            },
            editingLabel ? 'Save' : 'Edit',
        ),
        entry.type === 'boolean'
            ? h(BooleanToggleGroup, {
                  value: Boolean(entry.value),
                  onChange: (v: boolean) => onChangeValue(v),
                  compact: true,
              })
            : entry.type === 'number[]'
                ? h('input', {
                      type: 'text',
                      value: Array.isArray(entry.value) ? entry.value.join(', ') : '',
                      title: 'Comma-separated numbers',
                      onInput: (e: Event) => {
                          const raw = (e.target as HTMLInputElement).value;
                          const parts = raw.split(',').map(s => parseFloat(s.trim())).filter(n => !Number.isNaN(n));
                          onChangeValue(parts);
                      },
                      onPointerDown: stopPropagation,
                      style: { ...valueInputStyle, width: 160, maxWidth: 160 },
                  })
            : h('input', {
                  type: 'number',
                  value: Number(entry.value),
                  onInput: (e: Event) => onChangeValue(parseFloat((e.target as HTMLInputElement).value) || 0),
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
        h(
            'div',
            { style: { width: 220, flexShrink: 0 } },
            editingUsage
                ? h('textarea', {
                      value: usageDraft,
                      rows: 3,
                      placeholder: 'Describe what this value controls…',
                      onInput: (e: Event) => setUsageDraft((e.target as HTMLTextAreaElement).value),
                      onPointerDown: stopPropagation,
                      style: { ...fieldTextareaStyle, fontSize: '12px', fontStyle: 'normal' },
                  })
                : h(
                      'div',
                      { style: usageTextStyle },
                      entry.usage?.trim() ? entry.usage : '—',
                  ),
        ),
        h(
            'button',
            {
                type: 'button',
                onClick: () => {
                    if (editingUsage) {
                        onSaveUsage(usageDraft.trim());
                        setEditingUsage(false);
                    } else {
                        setUsageDraft(entry.usage ?? '');
                        setEditingUsage(true);
                    }
                },
                onPointerDown: stopPropagation,
                style: editButtonStyle(false),
            },
            editingUsage ? 'Save' : 'Edit',
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

    const saveUsage = (id: string, usage: string) => {
        species.behaviorConfiguration.value = species.behaviorConfiguration.peek().map(e =>
            e.id === id ? { ...e, usage: usage || undefined } : e);
        graphUpdateTrigger.dispatchEvent(new Event('update'));
    };

    const setValue = (id: string, value: number | boolean | number[]) => {
        species.behaviorConfiguration.value = species.behaviorConfiguration.peek().map(e =>
            e.id === id ? { ...e, value } : e);
        syncSpeciesSignalsFromConfiguration(species, species.behaviorConfiguration.peek());
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
            h('span', { style: { width: 100 } }, 'Value'),
            h('span', { style: { width: 56 } }, ''),
            h('span', { style: { width: 220 } }, 'Usage'),
            h('span', { style: { width: 44 } }, ''),
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
                      onSaveUsage: (usage: string) => saveUsage(entry.id, usage),
                      onChangeValue: (value: number | boolean | number[]) => setValue(entry.id, value),
                      onDelete: () => deleteEntry(entry.id),
                  }),
              ),
    );
}

export type { BehaviorConfigType };
