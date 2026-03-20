import * as ReactDOM from 'preact/compat';

export function createRoot(container: Element) {
    return {
        render: (vnode: any) => ReactDOM.render(vnode, container),
        unmount: () => ReactDOM.render(null, container)
    };
}