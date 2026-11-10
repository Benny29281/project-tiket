import { Component, input } from '@angular/core';
@Component({ selector: 'hd-icon', template: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path [attr.d]="paths[name()] || paths['ticket']" /></svg>`, styles: [':host{display:inline-flex;width:20px;height:20px;flex-shrink:0}svg{width:100%;height:100%}'] })
export class Icon {
  name = input('ticket');
  paths: Record<string, string> = {
    grid: 'M3 3h7v7H3z M14 3h7v7h-7z M3 14h7v7H3z M14 14h7v7h-7z',
    ticket: 'M3 5h18v5a2 2 0 0 0 0 4v5H3v-5a2 2 0 0 0 0-4z M15 5v3m0 3v2m0 3v3',
    queue: 'M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01',
    users: 'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2 M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8 M20 21v-2a4 4 0 0 0-3-3.87 M16 3.13a4 4 0 0 1 0 7.75',
    folder: 'M3 7V4h6l2 3h10v13H3z',
    plus: 'M12 5v14M5 12h14',
    arrow: 'M5 12h14m-5-5 5 5-5 5',
    back: 'M19 12H5m5-5-5 5 5 5',
    search: 'M21 21l-5-5 M18 10a8 8 0 1 0-16 0 8 8 0 0 0 16 0',
    clock: 'M12 8v5l3 2 M22 12a10 10 0 1 0-20 0 10 10 0 0 0 20 0',
    check: 'M5 12l4 4L19 6',
    alert: 'M12 8v5m0 3h.01 M10 3L1 20h22L14 3z',
    logout: 'M9 5H3v14h6M9 12h12m-4-4 4 4-4 4',
    headset: 'M4 13v-2a8 8 0 0 1 16 0v2 M4 11H2v7h4v-7z M20 11h2v7h-4v-7z M20 18v3h-8',
    menu: 'M3 6h18M3 12h18M3 18h18',
    lock: 'M6 10V7a6 6 0 0 1 12 0v3 M4 10h16v12H4z M12 15v3',
    chart: 'M4 3v18h17 M8 15v3M13 10v8M18 6v12'
  };
}
