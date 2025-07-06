import { Component, Input, ViewChild, ViewContainerRef } from '@angular/core';
@Component({
    moduleId: module.id,
    selector: 'icon-file',
    template: `
        <ng-template #template>
            <svg width="24" height="20" viewBox="0 0 24 20" fill="none" [ngClass]="class" xmlns="http://www.w3.org/2000/svg">
                <path
                    d="M13.7778 1.6665H6.11493C5.60685 1.6665 5.11958 1.8421 4.76032 2.15466C4.40105 2.46722 4.19922 2.89114 4.19922 3.33317V16.6665C4.19922 17.1085 4.40105 17.5325 4.76032 17.845C5.11958 18.1576 5.60685 18.3332 6.11493 18.3332H17.6092C18.1173 18.3332 18.6045 18.1576 18.9638 17.845C19.3231 17.5325 19.5249 17.1085 19.5249 16.6665V6.6665L13.7778 1.6665Z"
                    stroke="currentColor"
                    stroke-width="1.5"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                />
                <path d="M13.7778 1.6665V6.6665H19.525" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
                <path d="M15.6931 10.8335H8.03027" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
                <path d="M15.6931 14.1665H8.03027" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
                <path d="M9.94598 7.5H8.98813H8.03027" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
        </ng-template>
    `,
})
export class IconFileComponent {
    @Input() class: any = '';
    @ViewChild('template', { static: true }) template: any;
    constructor(private viewContainerRef: ViewContainerRef) {}
    ngOnInit() {
        this.viewContainerRef.createEmbeddedView(this.template);
        this.viewContainerRef.element.nativeElement.remove();
    }
}
