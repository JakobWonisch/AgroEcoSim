export function applyNodeComment(node: { comment?: string }, data: Record<string, unknown>) {
    if (typeof data.comment !== 'string') return;
    const trimmed = data.comment.trim();
    if (trimmed) node.comment = trimmed;
}

export function exportNodeComment(node: { comment?: string }, out: Record<string, unknown>) {
    if (typeof node.comment !== 'string') return;
    const trimmed = node.comment.trim();
    if (trimmed) out.comment = trimmed;
}
