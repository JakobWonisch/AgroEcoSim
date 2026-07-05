import { h } from 'preact';
import { useEffect, useState } from 'preact/hooks';
import { Presets } from 'rete-react-plugin';
import { getEditorContext } from './editorContext';
import { graphUpdateTrigger } from './graphUpdate';
import {
    getConnectedPortKeys,
    isNodeCollapsed,
    refreshNodeAfterCollapse,
} from './nodeCollapse';
import type { Schemes } from './NodeTypes';

type NodePayload = Schemes['Node'] & { width?: number; height?: number; comment?: string; collapsed?: boolean };

const NodeStyles = Presets.classic.NodeStyles as any;
const RefControl = Presets.classic.RefControl as any;
const RefSocket = Presets.classic.RefSocket as any;

const compactRowStyle = { minHeight: 0, height: '12px', padding: 0, margin: '-3px 0', lineHeight: 0, fontSize: 0 };

function sortByIndex(entries: [string, unknown][]) {
    entries.sort((a, b) => ((a[1] as { index?: number })?.index || 0) - ((b[1] as { index?: number })?.index || 0));
}

function stopPropagation(e: Event) {
    e.stopPropagation();
}

function EditIcon() {
    return h(
        'svg',
        { viewBox: '0 0 24 24', width: 14, height: 14, fill: 'currentColor', 'aria-hidden': true },
        h('path', {
            d: 'M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z',
        }),
    );
}

function CollapseToggleIcon({ collapsed }: { collapsed: boolean }) {
    return h(
        'svg',
        {
            viewBox: '0 0 24 24',
            width: 12,
            height: 12,
            fill: 'currentColor',
            'aria-hidden': true,
            style: {
                transform: collapsed ? 'rotate(0deg)' : 'rotate(180deg)',
                transition: 'transform 0.15s ease',
            },
        },
        h('path', { d: 'M7.41 8.59L12 13.17l4.59-4.58L18 10l-6 6-6-6 1.41-1.41z' }),
    );
}

function CommentModal({
    initial,
    onSave,
    onClose,
}: {
    initial: string;
    onSave: (value: string) => void;
    onClose: () => void;
}) {
    const [draft, setDraft] = useState(initial);

    return h(
        'div',
        {
            style: {
                position: 'fixed',
                inset: 0,
                background: 'rgba(0,0,0,0.45)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                zIndex: 10000,
            },
            onPointerDown: stopPropagation,
            onDblClick: stopPropagation,
            onClick: (e: MouseEvent) => {
                if (e.target === e.currentTarget) onClose();
            },
        },
        h(
            'div',
            {
                style: {
                    background: '#2a2a2a',
                    border: '1px solid #555',
                    borderRadius: '8px',
                    padding: '16px',
                    width: 'min(360px, 90vw)',
                    boxShadow: '0 8px 24px rgba(0,0,0,0.4)',
                },
                onPointerDown: stopPropagation,
                onClick: stopPropagation,
            },
            h(
                'div',
                {
                    style: {
                        color: '#fff',
                        fontFamily: 'sans-serif',
                        fontSize: '14px',
                        marginBottom: '8px',
                        fontWeight: 600,
                    },
                },
                'Node comment',
            ),
            h('textarea', {
                value: draft,
                rows: 4,
                placeholder: 'Add a comment…',
                onInput: (e: Event) => setDraft((e.target as HTMLTextAreaElement).value),
                style: {
                    width: '100%',
                    boxSizing: 'border-box',
                    padding: '8px',
                    borderRadius: '4px',
                    border: '1px solid #555',
                    background: '#1a1a1a',
                    color: '#fff',
                    fontFamily: 'sans-serif',
                    fontSize: '13px',
                    resize: 'vertical',
                },
            }),
            h(
                'div',
                { style: { display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '12px' } },
                h(
                    'button',
                    { type: 'button', onClick: onClose },
                    'Cancel',
                ),
                h(
                    'button',
                    {
                        type: 'button',
                        onClick: () => onSave(draft.trim()),
                    },
                    'Save',
                ),
            ),
        ),
    );
}

export function CommentableNodeComponent(props: { data: NodePayload; emit: (data: unknown) => void; styles?: () => unknown }) {
    const node = props.data;
    const [comment, setComment] = useState(typeof node.comment === 'string' ? node.comment : '');
    const [modalOpen, setModalOpen] = useState(false);
    const [, setRevision] = useState(0);

    useEffect(() => {
        const onUpdate = () => setRevision(r => r + 1);
        graphUpdateTrigger.addEventListener('update', onUpdate);
        return () => graphUpdateTrigger.removeEventListener('update', onUpdate);
    }, []);

    const collapsed = isNodeCollapsed(node);
    const ctx = getEditorContext();
    const connected = ctx ? getConnectedPortKeys(ctx.editor, node.id) : { inputs: new Set<string>(), outputs: new Set<string>() };

    let inputs = Object.entries(node.inputs) as [string, any][];
    let outputs = Object.entries(node.outputs) as [string, any][];
    const controls = Object.entries(node.controls) as [string, any][];
    const selected = node.selected || false;
    const { id, label, width, height } = node;

    if (collapsed) {
        inputs = inputs.filter(([key]) => connected.inputs.has(key));
        outputs = outputs.filter(([key]) => connected.outputs.has(key));
    }

    sortByIndex(inputs);
    sortByIndex(outputs);
    sortByIndex(controls);

    const saveComment = (text: string) => {
        if (text) {
            node.comment = text;
            setComment(text);
        } else {
            delete node.comment;
            setComment('');
        }
        setModalOpen(false);
        graphUpdateTrigger.dispatchEvent(new Event('update'));
    };

    const toggleCollapsed = async (e: Event) => {
        e.stopPropagation();
        if (collapsed)
            delete node.collapsed;
        else
            node.collapsed = true;
        setRevision(r => r + 1);
        if (ctx) {
            await refreshNodeAfterCollapse(ctx.area, ctx.editor, id);
            ctx.pushGraph();
        }
        graphUpdateTrigger.dispatchEvent(new Event('update'));
    };

    return h(
        NodeStyles,
        {
            selected,
            width,
            height,
            styles: props.styles,
            'data-testid': 'node',
            style: collapsed ? { paddingBottom: 0 } : undefined,
        },
        h(
            'div',
            {
                className: 'title',
                'data-testid': 'title',
                style: { display: 'flex', alignItems: 'center', gap: '6px', padding: collapsed ? '4px 8px' : undefined },
            },
            h('span', { style: { flex: 1 } }, label),
            h(
                'button',
                {
                    type: 'button',
                    title: 'Edit comment',
                    'aria-label': 'Edit comment',
                    onPointerDown: stopPropagation,
                    onDblClick: stopPropagation,
                    onClick: (e: Event) => {
                        e.stopPropagation();
                        setModalOpen(true);
                    },
                    style: {
                        flexShrink: 0,
                        display: 'inline-flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        padding: '2px 4px',
                        border: 'none',
                        borderRadius: '4px',
                        background: 'transparent',
                        color: 'rgba(255,255,255,0.65)',
                        cursor: 'pointer',
                    },
                },
                h(EditIcon, {}),
            ),
        ),
        !collapsed && comment
            ? h(
                  'div',
                  {
                      className: 'node-comment',
                      style: {
                          color: 'rgba(255,255,255,0.75)',
                          fontFamily: 'sans-serif',
                          fontSize: '12px',
                          padding: '0 8px 6px',
                          lineHeight: 1.35,
                          whiteSpace: 'pre-wrap',
                          wordBreak: 'break-word',
                      },
                  },
                  comment,
              )
            : null,
        outputs.map(([key, output]) =>
            output
                ? h(
                      'div',
                      {
                          className: 'output',
                          key,
                          'data-testid': `output-${key}`,
                          style: collapsed ? compactRowStyle : undefined,
                      },
                      collapsed
                          ? null
                          : h('div', { className: 'output-title', 'data-testid': 'output-title' }, output.label),
                      h(RefSocket, {
                          name: 'output-socket',
                          side: 'output',
                          socketKey: key,
                          nodeId: id,
                          emit: props.emit,
                          payload: output.socket,
                          'data-testid': 'output-socket',
                      }),
                  )
                : null,
        ),
        !collapsed
            ? controls.map(([key, control]) =>
                  control
                      ? h(RefControl, {
                            key,
                            name: 'control',
                            emit: props.emit,
                            payload: control,
                            'data-testid': `control-${key}`,
                        })
                      : null,
              )
            : null,
        inputs.map(([key, input]) =>
            input
                ? h(
                      'div',
                      {
                          className: 'input',
                          key,
                          'data-testid': `input-${key}`,
                          style: collapsed ? compactRowStyle : undefined,
                      },
                      h(RefSocket, {
                          name: 'input-socket',
                          side: 'input',
                          socketKey: key,
                          nodeId: id,
                          emit: props.emit,
                          payload: input.socket,
                          'data-testid': 'input-socket',
                      }),
                      !collapsed && input && (!input.control || !input.showControl)
                          ? h('div', { className: 'input-title', 'data-testid': 'input-title' }, input.label)
                          : null,
                      !collapsed && input.control && input.showControl
                          ? h(RefControl, {
                                key,
                                name: 'input-control',
                                emit: props.emit,
                                payload: input.control,
                                'data-testid': 'input-control',
                            })
                          : null,
                  )
                : null,
        ),
        h(
            'div',
            {
                style: {
                    display: 'flex',
                    width: '100%',
                    padding: collapsed ? '0 0 2px' : '2px 0 4px',
                },
            },
            h(
                'button',
                {
                    type: 'button',
                    title: collapsed ? 'Expand node' : 'Collapse node',
                    'aria-label': collapsed ? 'Expand node' : 'Collapse node',
                    'aria-expanded': !collapsed,
                    onPointerDown: stopPropagation,
                    onDblClick: stopPropagation,
                    onClick: toggleCollapsed,
                    style: {
                        flex: 1,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        padding: '2px 0',
                        border: 'none',
                        borderRadius: 0,
                        background: 'transparent',
                        color: 'rgba(255,255,255,0.55)',
                        cursor: 'pointer',
                        lineHeight: 1,
                    },
                },
                h(CollapseToggleIcon, { collapsed }),
            ),
        ),
        modalOpen
            ? h(CommentModal, {
                  initial: comment,
                  onSave: saveComment,
                  onClose: () => setModalOpen(false),
              })
            : null,
    );
}
