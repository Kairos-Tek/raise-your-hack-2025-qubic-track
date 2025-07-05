import { Component, Input } from '@angular/core';
@Component({
    selector: 'icon-test',
    template: `
        <svg width="25" height="25" viewBox="0 0 25 25" fill="none" [ngClass]="class" xmlns="http://www.w3.org/2000/svg">
            <path
                d="M21.5002 7.5L7.32022 21.68C6.78868 22.2057 6.07073 22.4997 5.32316 22.4978C4.57558 22.4959 3.85912 22.1983 3.33022 21.67C2.80017 21.1394 2.50244 20.42 2.50244 19.67C2.50244 18.92 2.80017 18.2006 3.33022 17.67L17.5002 3.5"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
            />
            <path d="M16.5 2.5L22.5 8.5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
            <path d="M12.5 16.5H4.5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
    `,
})
export class IconTestComponent {
    @Input() fill: boolean = false;
    @Input() class: any = '';
}
