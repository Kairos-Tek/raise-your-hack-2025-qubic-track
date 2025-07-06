import { Component, Input } from '@angular/core';
@Component({
    selector: 'icon-monitor',
    template: `
        <svg width="20" height="20" viewBox="0 0 20 20" fill="none" [ngClass]="class" xmlns="http://www.w3.org/2000/svg">
            <path
                d="M4.1665 14.1667H3.33317C2.89114 14.1667 2.46722 13.9911 2.15466 13.6785C1.8421 13.366 1.6665 12.942 1.6665 12.5V4.16667C1.6665 3.72464 1.8421 3.30072 2.15466 2.98816C2.46722 2.67559 2.89114 2.5 3.33317 2.5H16.6665C17.1085 2.5 17.5325 2.67559 17.845 2.98816C18.1576 3.30072 18.3332 3.72464 18.3332 4.16667V12.5C18.3332 12.942 18.1576 13.366 17.845 13.6785C17.5325 13.9911 17.1085 14.1667 16.6665 14.1667H15.8332"
                stroke="currentColor"
                stroke-width="1.5"
                stroke-linecap="round"
                stroke-linejoin="round"
            />
            <path d="M10.0002 12.5L14.1668 17.5H5.8335L10.0002 12.5Z" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
    `,
})
export class IconMonitorComponent {
    @Input() fill: boolean = false;
    @Input() class: any = '';
}
