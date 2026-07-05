export const graphUpdateTrigger = new EventTarget();

export function notifyGraphUiUpdate() {
    graphUpdateTrigger.dispatchEvent(new Event('update'));
}
